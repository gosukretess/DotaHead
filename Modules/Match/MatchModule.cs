using Discord;
using Discord.Interactions;
using DotaHead.Database;
using DotaHead.Infrastructure;
using DotaHead.MatchMonitor;
using Microsoft.Extensions.Logging;
using OpenDotaApi;
using SixLabors.Fonts;
using SixLabors.ImageSharp.Drawing.Processing;
using Color = SixLabors.ImageSharp.Color;
using Image = SixLabors.ImageSharp.Image;

namespace DotaHead.Modules.Match;

public class MatchModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly DataContext _dataContext;
    private readonly MatchDetailsBuilder _matchDetailsBuilder;
    private ILogger Logger => StaticLoggerFactory.GetStaticLogger<MatchModule>();

    public MatchModule(DataContext dataContext, MatchDetailsBuilder matchDetailsBuilder)
    {
        _dataContext = dataContext;
        _matchDetailsBuilder = matchDetailsBuilder;
    }

    [SlashCommand("last-match", "Get data about last match")]
    public async Task LastMatch()
    {
        await DeferAsync();

        var currentUser =
            _dataContext.Players.FirstOrDefault(p => p.DiscordId == Context.User.Id && p.GuildId == Context.Guild.Id);
        if (currentUser == null) return;

        var openDotaClient = new OpenDota();
        var recentMatches = await openDotaClient.Players.GetRecentMatchesAsync(currentUser.DotaId);
        var lastMatch = recentMatches.FirstOrDefault();

        if (lastMatch?.MatchId == null) return;
        var playerDbos = _dataContext.Players.Where(p => p.GuildId == Context.Guild.Id).ToList();
        var embed = await _matchDetailsBuilder.Build(lastMatch.MatchId!.Value, playerDbos);

        await ModifyOriginalResponseAsync(r =>
        {
            r.Embed = embed.Embed;
            r.Attachments = new Optional<IEnumerable<FileAttachment>>(new[] { new FileAttachment(embed.ImagePath) });
        });
    }

    [SlashCommand("icon-test", "Get data about last match")]
    public async Task IconTEst()
    {
        await DeferAsync();

        var path = DrawTable();
        await Context.Channel.SendFileAsync(path);

        var embed = new EmbedBuilder
        {
        };
        embed.WithImageUrl(@$"attachment://{path}");

        var embedB = embed.Build();


        await ModifyOriginalResponseAsync(q => q.Embed = embedB);
    }

    public string DrawTable()
    {
        int width = 1000;
        int height = 600;
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "table.png");
        var margin = 5;

        using (var image = new Image<Rgba32>(width, height))
        {
            var headerFont = SystemFonts.CreateFont("Arial", 40, FontStyle.Bold);
            var font = SystemFonts.CreateFont("Arial", 26, FontStyle.Bold);

            image.Mutate(ctx =>
            {
                ctx.Fill(Color.DarkSlateGray);

                // Winner header
                ctx.DrawText("Ktośtam wins", headerFont, Color.Red, new PointF(10, 10));

                // Radiant and Dire colors
                ctx.DrawLines(Pens.Solid(Color.Green, 10), new PointF(15, 90), new PointF(15, 340));
                ctx.DrawLines(Pens.Solid(Color.Crimson, 10), new PointF(15, 340), new PointF(15, 590));

                var marginY = 13;
                var colX = new[]
                {
                    20, 109 + margin, 150 + margin, 350 + margin, 390 + margin, 430 + margin, 470 + margin, 560 + margin
                };

                var headersY = 60;
                ctx.DrawText("K", font, Color.White, new PointF(colX[3], headersY));
                ctx.DrawText("D", font, Color.White, new PointF(colX[4], headersY));
                ctx.DrawText("A", font, Color.White, new PointF(colX[5], headersY));
                ctx.DrawText("Net", font, Color.Yellow, new PointF(colX[6], headersY));
                ctx.DrawText("Items", font, Color.White, new PointF(colX[7], headersY));

                var lineY = 90;

                var dummyPlayers = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
                foreach (var player in dummyPlayers)
                {
                    string imagePath = "C:/workspace/DotaHead/Assets/panorama/images/heroes/npc_dota_hero_antimage_png.png";
                    string itemImage = "C:/workspace/DotaHead/Assets/panorama/images/items/ancient_janggo_png.png";
                    AddImageToImage(image, imagePath, colX[0], lineY, ImageType.Hero);
                    ctx.DrawText("25", font, Color.Gold, new PointF(colX[1], lineY + marginY));
                    ctx.DrawText("Kretess", font, Color.White, new PointF(colX[2], lineY + marginY));
                    ctx.DrawText("18", font, Color.White, new PointF(colX[3], lineY + marginY));
                    ctx.DrawText("11", font, Color.White, new PointF(colX[4], lineY + marginY));
                    ctx.DrawText("25", font, Color.White, new PointF(colX[5], lineY + marginY));
                    ctx.DrawText("13.5k", font, Color.Yellow, new PointF(colX[6], lineY + marginY));
                    // ctx.DrawText("BLABLABLBALABLABLBLABL", font, Color.White, new PointF(colX[7], lineY + marginY));

                    var imageX = colX[7];
                    foreach (var item in new[]{1,2,3,4,5,6})
                    {
                        AddImageToImage(image, itemImage, imageX, lineY, ImageType.Item);
                        imageX += 69;
                    }

                    lineY += 50;
                }
            });

            image.Save(path);
        }

        return path;
    }

    private void AddImageToImage(Image<Rgba32> baseImage, string imagePath, int destinationX, int destinationY, ImageType imageType)
    {
        var size = imageType switch
        {
            ImageType.Hero => new Size(89, 50),
            ImageType.Item => new Size(69, 50),
            _ => throw new ArgumentOutOfRangeException(nameof(imageType), imageType, null)
        };

        using var imageToAdd = Image.Load(imagePath);
        imageToAdd.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = size,
            Mode = ResizeMode.Stretch
        }));

        baseImage.Mutate(ctx => ctx.DrawImage(imageToAdd, new Point(destinationX, destinationY), 1));
    }

    private enum ImageType
    {
        Hero,
        Item
    }
}