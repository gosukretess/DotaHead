using System.Text;
using Discord;
using DotaHead.Database;
using DotaHead.Infrastructure;
using DotaHead.Services;
using Microsoft.Extensions.Logging;
using OpenDotaApi.Api.Matches.Model;

namespace DotaHead.MatchMonitor;

public class MatchDetailsBuilder
{
    private readonly DotabaseService _dotabaseService;
    private ILogger Logger => StaticLoggerFactory.GetStaticLogger<MatchDetailsBuilder>();

    public MatchDetailsBuilder(DotabaseService dotabaseService)
    {
        _dotabaseService = dotabaseService;
    }

    public async Task<MatchDetailsMessage> Build(Match matchDetails, List<PlayerDbo> playerDbos)
    {
        Logger.LogInformation($"Building match details for matchId {matchDetails.MatchId}");

        var playersBySide = GetPlayersBySide(matchDetails);
        var ourPlayers = GetOurPlayers(playerDbos, playersBySide);

        if (ourPlayers.Count == 0)
            return HandleWarning($"No saved players found when building match stats. MatchId: {matchDetails.MatchId}");

        var isWin = ourPlayers.First().Player.Win == 1;
        var isParsed = matchDetails.Version.HasValue;

        var embed = new EmbedBuilder
        {
            Author = new EmbedAuthorBuilder().WithName(string.Join(", ", ourPlayers.Select(p => p.Name))),
            Description = CreateDescription(isWin, matchDetails),
            Color = isWin ? Discord.Color.Green : Discord.Color.Red,
            Footer = CreateFooter(matchDetails.MatchId!.Value, matchDetails)
        };

        FillOurPlayersStats(isParsed, ourPlayers, embed);
        FillHeadToHeadStats(isParsed, embed, playersBySide, ourPlayers);

        var tableBuilder = new ResultsTableBuilder(_dotabaseService);
        var path = tableBuilder.DrawTable(playersBySide, matchDetails);
        embed.WithImageUrl(@"attachment://table.png");

        return new MatchDetailsMessage
        {
            Embed = embed.Build(),
            ImagePath = path
        };
    }

    private void FillHeadToHeadStats(bool isParsed, EmbedBuilder embed,
        Dictionary<Team, List<PlayerRecord>> playersBySide,
        List<PlayerRecord> ourPlayers)
    {
        if (!isParsed) return;

        var ourTeamPlayers = playersBySide[ourPlayers.First().Team];
        var builder = new StringBuilder();
        foreach (var player in ourTeamPlayers.Where(p => p.Role == Role.Core))
        {
            var enemy = playersBySide[GetOppositeTeam(ourPlayers.First().Team)]
                .First(p => p.Role == Role.Core && p.Lane == GetOppositeLane(player.Lane));

            var differenceInGold = player.Player.GoldEachMinute[10] - enemy.Player.GoldEachMinute[10];
            var differenceInGoldFormatted = $"{(differenceInGold > 0 ? "+" : string.Empty)}{differenceInGold}";

            builder.Append(
                $"{GetHeroName(player.Player.HeroId!.Value)} | {player.Player.LastHitsEachMinute[10]}/{player.Player.DeniesAtDifferentTimes[10]} " +
                $"| {player.Player.GoldEachMinute[10]} " +
                $"(**{differenceInGoldFormatted}**)" +
                $" {enemy.Player.GoldEachMinute[10]} | {enemy.Player.LastHitsEachMinute[10]}/{enemy.Player.DeniesAtDifferentTimes[10]}" +
                $" | {GetHeroName(enemy.Player.HeroId!.Value)}\n");
        }

        embed.AddField("Cores head to head in 10m:",
            builder.ToString());
    }

    private void FillOurPlayersStats(bool isParsed, List<PlayerRecord> ourPlayers, EmbedBuilder embed)
    {
        foreach (var playerRecord in ourPlayers)
        {
            var player = playerRecord.Player;
            var hero = _dotabaseService.Heroes[player.HeroId!.Value];
            var builder = new StringBuilder(
                $"{_dotabaseService.GetEmoji(hero.FullName)} **{GetHeroName(hero.Id)}** ({player.Level})\n" +
                $"{_dotabaseService.GetEmoji(GetRoleName(playerRecord.Role, playerRecord.Lane))} {playerRecord.Lane} {playerRecord.Role}\n" +
                $"KDA: **{player.Kills}**/**{player.Deaths}**/**{player.Assists}**\n" +
                $"Hero Damage: {player.HeroDamage}\n" +
                $"Hero Healing: {player.HeroHealing}\n" +
                $"Tower Damage: {player.TowerDamage}\n" +
                // $"Damage Taken: {player.DamageInflictorReceived.Values.Sum()}\n" +
                $"Net Worth: {player.TotalGold}\n" +
                $"LH/DN: {player.LastHits} / {player.Denies}");

            if (isParsed)
            {
                if (playerRecord.Role == Role.Core)
                {
                    builder.Append(FormatCoreData(playerRecord));
                }

                if (playerRecord.Role == Role.Support)
                {
                    builder.Append(FormatSupportData(playerRecord));
                }
            }

            embed.AddField(playerRecord.Name, builder.ToString(), true);
        }
    }

    // TODO: Optimize inserting name of our players
    private static List<PlayerRecord> GetOurPlayers(List<PlayerDbo> playerDbos,
        Dictionary<Team, List<PlayerRecord>> playersBySide)
    {
        var ourPlayers = playersBySide.Values.SelectMany(p => p)
            .Where(p => playerDbos.Select(p => p.DotaId).Contains(p.Player.AccountId.GetValueOrDefault())).ToList();
        foreach (var playerRecord in ourPlayers)
        {
            var playerDbo = playerDbos.FirstOrDefault(p => p.DotaId == playerRecord.Player.AccountId);
            if (playerDbo != null)
            {
                playerRecord.Name = playerDbo.Name;
            }
        }

        return ourPlayers;
    }

    private static string CreateDescription(bool isWin, Match matchDetails)
    {
        var (minutes, seconds) = GetMatchDuration(matchDetails);

        var gameLinks =
            $"More info at [DotaBuff](https://www.dotabuff.com/matches/{matchDetails.MatchId}), " +
            $"[OpenDota](https://www.opendota.com/matches/{matchDetails.MatchId}), or " +
            $"[STRATZ](https://www.stratz.com/match/{matchDetails.MatchId})";

        return $"{(isWin ? "Won" : "Lost")} a match in {minutes} minutes and {seconds} seconds. {gameLinks}";
    }

    private static Dictionary<Team, List<PlayerRecord>> GetPlayersBySide(Match matchDetails)
    {
        var playersBySide = matchDetails.Players.GroupBy(p => p.IsRadiant).ToDictionary(q => GetTeam(q.Key), q => q
            .Select(
                p => new PlayerRecord
                {
                    Lane = GetLane(p),
                    Team = GetTeam(p.IsRadiant),
                    Player = p
                }).ToList());

        // Fill player roles
        foreach (var side in new[] { Team.Radiant, Team.Dire })
        {
            foreach (var group in playersBySide[side].GroupBy(p => p.Lane))
            {
                foreach (var player in group)
                {
                    var maxLastHits = group.Max(q => q.Player.LastHitsEachMinute[10]);
                    player.Role = player.Player.LastHitsEachMinute[10] == maxLastHits ? Role.Core : Role.Support;
                }
            }
        }

        return playersBySide;
    }

    private static (int minutes, int seconds) GetMatchDuration(Match matchDetails)
    {
        var minutes = matchDetails.Duration.GetValueOrDefault() / 60;
        var seconds = matchDetails.Duration.GetValueOrDefault() % 60;
        return (minutes, seconds);
    }

    private string FormatCoreData(PlayerRecord player)
    {
        var builder = new StringBuilder();

        builder.Append("\n\n");
        builder.Append($"GPM: {player.Player.GoldPerMin}\n" +
                       $"Gold 10m: {player.Player.GoldEachMinute[10]}\n" +
                       $"Gold 20m: {player.Player.GoldEachMinute[20]}\n" +
                       $"LH 10m: {player.Player.LastHitsEachMinute[10]}/{player.Player.DeniesAtDifferentTimes[10]}\n" +
                       $"LH 20m: {player.Player.LastHitsEachMinute[20]}/{player.Player.DeniesAtDifferentTimes[20]}\n");


        return builder.ToString();
    }

    private static string FormatSupportData(PlayerRecord player)
    {
        var builder = new StringBuilder();

        builder.Append("\n\n");
        builder.Append($"GPM: {player.Player.GoldPerMin}\n");
        builder.Append($"Camps stacked: {player.Player.CampsStacked}\n");
        builder.Append(
            $"Wisdom runes: {(player.Player.Runes.TryGetValue("8", out var runeVal2) ? runeVal2 : 0)}\n");
        builder.Append($"Sentry wards: {player.Player.PurchaseWardSentry}\n");
        builder.Append($"Observer wards: {player.Player.PurchaseWardObserver}\n");


        return builder.ToString();
    }

    private static EmbedFooterBuilder CreateFooter(long matchId, Match matchDetails)
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Warsaw");
        var dateTime = TimeZoneInfo.ConvertTimeFromUtc(matchDetails.StartTime!.Value, timeZone);
        return new EmbedFooterBuilder().WithText(
            $"MatchId: {matchId} • {dateTime}");
    }

    private string GetHeroName(int heroId) =>
        _dotabaseService.Heroes.TryGetValue(heroId, out var hero) ? hero.LocalizedName : "Unknown";

    private static Team GetOppositeTeam(Team ourTeamSide) => ourTeamSide == Team.Radiant ? Team.Dire : Team.Radiant;

    private static Lane GetOppositeLane(Lane lane) =>
        lane switch
        {
            Lane.Mid => Lane.Mid,
            Lane.Off => Lane.Safe,
            Lane.Safe => Lane.Off,
            _ => Lane.Unknown
        };

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

    private MatchDetailsMessage HandleWarning(string errorMessage)
    {
        Logger.LogWarning(errorMessage);
        return new MatchDetailsMessage
        {
            Embed =
                new EmbedBuilder
                {
                    Description = errorMessage
                }.Build()
        };
    }

    private static string GetRoleName(Role role, Lane lane)
    {
        if (role == Role.Core)
        {
            if (lane == Lane.Mid) return "midlane";
            if (lane == Lane.Off) return "offlane";
            if (lane == Lane.Safe) return "safelane";
        }

        if (role == Role.Support)
        {
            if (lane == Lane.Safe) return "hardsupport";
            if (lane == Lane.Off) return "softsupport";
        }

        return string.Empty;
    }

    private static Team GetTeam(bool? isRadiant)
    {
        if (isRadiant == null) return Team.Unknown;
        return isRadiant.Value ? Team.Radiant : Team.Dire;
    }
}