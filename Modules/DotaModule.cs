using System.Text;
using Discord;
using Discord.Interactions;
using DotaHead.Services;
using DNet_V3_Tutorial.Log;
using OpenDotaApi;
using OpenDotaApi.Api.Matches.Model;

namespace DotaHead.Modules;

public class DotaModule : InteractionModuleBase<SocketInteractionContext>
{
    public InteractionService Commands { get; set; }
    private readonly Logger _logger;
    private readonly PlayersService _playersService;
    private readonly HeroesService _heroesService;

    public DotaModule(ConsoleLogger logger, PlayersService playersService, HeroesService heroesService)
    {
        _logger = logger;
        _playersService = playersService;
        _heroesService = heroesService;
    }

    [SlashCommand("lm", "Get data about last match")]
    public async Task LastMatch()
    {
        await DeferAsync();
        var replayParsed = true;

        var currentUser = _playersService.PlayerIds.FirstOrDefault(p => (ulong)p.DiscordId == Context.User.Id);

        var openDotaClient = new OpenDota();
        var recentMatches = await openDotaClient.Players.GetRecentMatchesAsync(currentUser.DotaId);

        var match = recentMatches[0];
        var matchId = recentMatches[0].MatchId.GetValueOrDefault();
        if (match.Version == null)
        {
            await _logger.Log(new LogMessage(LogSeverity.Info, nameof(DotaModule), "Match not parsed, requested parse."));
            var parseResponse = await openDotaClient.Request.SubmitNewParseRequestAsync(matchId);
            replayParsed = await WaitForParseCompletion(openDotaClient, parseResponse.Job.JobId);
        }

        await _logger.Log(new LogMessage(LogSeverity.Info, nameof(DotaModule), $"Getting match details - matchId: {matchId}"));
        var matchDetails = await openDotaClient.Matches.GetMatchAsync(matchId);

        var parsed = matchDetails.Version != null;

        var minutes = matchDetails.Duration.GetValueOrDefault() / 60;
        var seconds = matchDetails.Duration.GetValueOrDefault() % 60;

        var playersBySide = matchDetails.Players.GroupBy(p => p.IsRadiant).ToDictionary(q => GetTeam(q.Key), q => q
            .Select(
                p => new PlayerRecord
                {
                    Lane = GetLane(p),
                    Team = GetTeam(p.IsRadiant),
                    Player = p
                }).ToList());
        FillPlayerRoles(playersBySide);


        var ourPlayers =
            playersBySide.Values.SelectMany(p => p).Where(p =>
                _playersService.PlayerIds.Select(q => q.DotaId).Contains(p.Player.AccountId.GetValueOrDefault()));
        var winBool = ourPlayers.First().Player.Win == 1;

        var gameLinks =
            $"More info at [DotaBuff](https://www.dotabuff.com/matches/{matchId}), " +
            $"[OpenDota](https://www.opendota.com/matches/{matchId}), or " +
            $"[STRATZ](https://www.stratz.com/match/{matchId})";

        var embed = new EmbedBuilder
        {
            Author = new EmbedAuthorBuilder().WithName(string.Join(',', ourPlayers.Select(p => p.Player.Personaname))),
            Description = $"{(winBool ? "Won" : "Lost")} a match in {minutes} minutes and {seconds} seconds. {gameLinks}",
            Color = winBool ? Color.Green : Color.Red
        };

        foreach (var playerRecord in ourPlayers)
        {
            var player = playerRecord.Player;
            var builder = new StringBuilder($"Hero: **{_heroesService.Heroes[player.HeroId.Value].LocalizedName}**\n" +
                                            $"Role: {playerRecord.Lane} {playerRecord.Role}\n" +
                                            $"KDA: **{player.Kills}**/**{player.Deaths}**/**{player.Assists}**\n" +
                                            $"Hero Damage: {player.HeroDamage}\n" +
                                            $"Hero Healing: {player.HeroHealing}\n" +
                                            $"Tower Damage: {player.TowerDamage}\n" +
                                            $"Net Worth: {player.TotalGold}\n" +
                                            $"Last Hits: {player.LastHits}\n" +
                                            $"Denies: {player.Denies}\n" +
                                            $"Level: {player.Level}\n");

            if (playerRecord.Role == Role.Core)
            {
                builder.Append(FormatCoreData(playerRecord));
            }

            if (playerRecord.Role == Role.Support)
            {
                builder.Append(FormatSupportData(playerRecord));
            }

            embed.AddField(player.Personaname, builder.ToString(), true);
        }

        embed.AddField("Cores head to head in 10m:",
            FormatHeadToHeadTable(playersBySide, ourPlayers.First().Team));

        await ModifyOriginalResponseAsync(r => r.Embed = embed.Build());
    }

    private async Task<bool> WaitForParseCompletion(OpenDota openDotaClient, long jobId)
    {
        var waitTime = 2;
        while (waitTime <= 10)
        {
            var response = await openDotaClient.Request.GetParseRequestStateAsync(jobId);

            if (response == null)
            {
                return true;
            }

            await _logger.Log(new LogMessage(LogSeverity.Info, nameof(DotaModule), $"Parse not finished. Waiting for {waitTime} seconds."));
            await Task.Delay(waitTime * 1000);
            waitTime += 2;
        }

        await _logger.Log(new LogMessage(LogSeverity.Info, nameof(DotaModule), $"Parse failed."));
        return false;
    }

    private string FormatCoreData(PlayerRecord player)
    {
        var builder = new StringBuilder();

        builder.Append($"\n");
        builder.Append($"GPM: {player.Player.GoldPerMin}\n" +
                       $"Gold 10m: {player.Player.GoldEachMinute[10]}\n" +
                       $"Gold 20m: {player.Player.GoldEachMinute[20]}\n" +
                       $"LH 5m: {player.Player.LastHitsEachMinute[5]}/{player.Player.DeniesAtDifferentTimes[5]}\n" +
                       $"LH 10m: {player.Player.LastHitsEachMinute[10]}/{player.Player.DeniesAtDifferentTimes[10]}\n" +
                       $"LH 20m: {player.Player.LastHitsEachMinute[20]}/{player.Player.DeniesAtDifferentTimes[20]}\n");


        return builder.ToString();
    }

    private string FormatSupportData(PlayerRecord player)
    {
        var builder = new StringBuilder();

        player.Player.ItemUses.TryGetValue("ward_sentry", out var sentryWards);
        player.Player.ItemUses.TryGetValue("ward_observer", out var observerWards);

        builder.Append($"\n");
        builder.Append($"GPM: {player.Player.GoldPerMin}\n");
        builder.Append($"Camps stacked: {player.Player.CampsStacked}\n");
        builder.Append(
            $"Wisdom runes: {(player.Player.Runes.TryGetValue("8", out var runeVal2) ? runeVal2 : 0)}\n");
        builder.Append($"Sentry wards: {sentryWards}\n");
        builder.Append($"Observer wards: {observerWards}\n");


        return builder.ToString();
    }

    private string FormatHeadToHeadTable(Dictionary<Team, List<PlayerRecord>> playersBySide, Team ourTeamSide)
    {
        var ourTeamPlayers = playersBySide[ourTeamSide];
        var builder = new StringBuilder();
        foreach (var player in ourTeamPlayers.Where(p => p.Role == Role.Core))
        {
            var enemy = playersBySide[GetOppositeTeam(ourTeamSide)]
                .First(p => p.Role == Role.Core && p.Lane == GetOppositeLane(player.Lane));

            var differenceInGold = player.Player.GoldEachMinute[10] - enemy.Player.GoldEachMinute[10];
            var differenceInGoldFormatted = $"{(differenceInGold > 0 ? "+" : string.Empty)}{differenceInGold}";

            builder.Append(
                $"{GetHeroName(player.Player.HeroId.Value)} | {player.Player.LastHitsEachMinute[10]}/{player.Player.DeniesAtDifferentTimes[10]} " +
                $"| {player.Player.GoldEachMinute[10]} " +
                $"(**{differenceInGoldFormatted}**)" +
                $" {enemy.Player.GoldEachMinute[10]} | {enemy.Player.LastHitsEachMinute[10]}/{enemy.Player.DeniesAtDifferentTimes[10]}" +
                $" | {GetHeroName(enemy.Player.HeroId.Value)}\n");
        }

        return builder.ToString();
    }

    private string GetHeroName(int heroId)
    {
        return _heroesService.Heroes[heroId].LocalizedName;
    }

    private static Team GetOppositeTeam(Team ourTeamSide)
    {
        return ourTeamSide == Team.Radiant ? Team.Dire : Team.Radiant;
    }

    private static Lane GetOppositeLane(Lane lane)
    {
        return lane switch
        {
            Lane.Mid => Lane.Mid,
            Lane.Off => Lane.Safe,
            Lane.Safe => Lane.Off,
            _ => Lane.Unknown
        };
    }

    private static void FillPlayerRoles(IReadOnlyDictionary<Team, List<PlayerRecord>> playersBySide)
    {
        foreach (var group in playersBySide[Team.Radiant].GroupBy(p => p.Lane))
        {
            foreach (var player in group)
            {
                var maxLastHits = group.Max(q => q.Player.LastHitsEachMinute[10]);
                player.Role = player.Player.LastHitsEachMinute[10] == maxLastHits ? Role.Core : Role.Support;
            }
        }

        foreach (var group in playersBySide[Team.Dire].GroupBy(p => p.Lane))
        {
            foreach (var player in group)
            {
                var maxLastHits = group.Max(q => q.Player.LastHitsEachMinute[10]);
                player.Role = player.Player.LastHitsEachMinute[10] == maxLastHits ? Role.Core : Role.Support;
            }
        }
    }

    public static Lane GetLane(MatchPlayer player)
    {
        return player.LaneRole switch
        {
            1 => Lane.Safe,
            2 => Lane.Mid,
            3 => Lane.Off,
            4 => Lane.Jungle,
            _ => Lane.Unknown
        };
    }

    private static Team GetTeam(bool? isRadiant)
    {
        if (isRadiant == null) return Team.Unknown;
        return isRadiant.Value ? Team.Radiant : Team.Dire;
    }
}