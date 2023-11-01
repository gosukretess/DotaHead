using System.Globalization;
using DotaHead.Infrastructure;
using DotaHead.Services;
using Microsoft.Extensions.Logging;
using OpenDotaApi.Api.Matches.Model;
using SixLabors.Fonts;
using SixLabors.ImageSharp.Drawing.Processing;

namespace DotaHead.MatchMonitor;

public class ResultsTableBuilder
{
    private const int Margin = 4;
    private const int Height = 600;
    private const int Width = 1043;
    private readonly DotabaseService _dotabaseService;
    private readonly Font _headerFont;
    private readonly Font _font;
    private const string ScepterDir = "Assets/panorama/images/hud/reborn";

    private ILogger Logger => StaticLoggerFactory.GetStaticLogger<MatchDetailsBuilder>();

    public ResultsTableBuilder(DotabaseService dotabaseService)
    {
        _dotabaseService = dotabaseService;
        var fontCollection = new FontCollection();
        var fontFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets/fonts", "Roboto-Bold.ttf");
        var fontFamily = fontCollection.Add(fontFilePath);
        _headerFont = fontFamily.CreateFont(40, FontStyle.Bold);
        _font = fontFamily.CreateFont(26, FontStyle.Bold);
}

    public string DrawTable(Dictionary<Team, List<PlayerRecord>> playersBySide, Match matchDetails)
    {
        Logger.LogTrace("Started building results table image.");
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "table.png");

        var player = playersBySide[Team.Radiant].First();
        var winTeam = player.Player.Win == 1 ? Team.Radiant : Team.Dire;
        

        using var image = new Image<Rgba32>(Width, Height);
        image.Mutate(ctx =>
        {
            ctx.Fill(Color.Black);
            var bgImage = winTeam == Team.Radiant
                ? "postgameherobackground_psd.png"
                : "postgameherobackground_dire_psd.png";
            AddImageToImage(image, Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "Assets/panorama/images/backgrounds", bgImage), 0, 0, ImageType.Background);

            AddHeader(image, matchDetails, winTeam);

            // Radiant and Dire colors
            ctx.DrawLines(Pens.Solid(Color.Green, 6), new PointF(8, 90), new PointF(8, 340));
            ctx.DrawLines(Pens.Solid(Color.Crimson, 6), new PointF(8, 340), new PointF(8, 590));

            var marginY = 13;
            var colX = new[]
            {
                11, 100 + Margin, 137 + Margin, 337 + Margin, 377 + Margin, 417 + Margin, 457 + Margin, 537 + Margin, 571 + Margin, 989 + Margin
            };

            var headersY = 60;
            ctx.DrawText("K", _font, Color.White, new PointF(colX[3], headersY));
            ctx.DrawText("D", _font, Color.White, new PointF(colX[4], headersY));
            ctx.DrawText("A", _font, Color.White, new PointF(colX[5], headersY));
            ctx.DrawText("Net", _font, Color.Yellow, new PointF(colX[6], headersY));
            ctx.DrawText("Items", _font, Color.White, new PointF(colX[8], headersY));

            var lineY = 90;
            foreach (var player in playersBySide[Team.Radiant].Concat(playersBySide[Team.Dire]))
            {
                var hero = _dotabaseService.Heroes[player.Player.HeroId.Value];
                var imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", hero.Image.TrimStart('/'));
                AddImageToImage(image, imagePath, colX[0], lineY, ImageType.Hero);
                ctx.DrawText(player.Player.Level.ToString(), _font, Color.Gold,
                    new PointF(colX[1], lineY + marginY));
                DrawPlayerName(ctx, player, colX, lineY, marginY);
                ctx.DrawText(player.Player.Kills.ToString(), _font, Color.White,
                    new PointF(colX[3], lineY + marginY));
                ctx.DrawText(player.Player.Deaths.ToString(), _font, Color.White,
                    new PointF(colX[4], lineY + marginY));
                ctx.DrawText(player.Player.Assists.ToString(), _font, Color.White,
                    new PointF(colX[5], lineY + marginY));
                ctx.DrawText(FormatThousands(player.Player.TotalGold), _font, Color.Yellow,
                    new PointF(colX[6], lineY + marginY));


                var shardImgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ScepterDir, "aghsstatus_shard_psd.png");
                if (
                    (player.Player.ItemUsage.TryGetValue("aghanims_shard", out var shard) && shard is > 0)
                    || player.Player.PermanentBuffs.Any(b => b.PermanentBuffPermanentBuff == 12)
                )
                {
                    shardImgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ScepterDir, "aghsstatus_shard_on_psd.png");
                }
                AddImageToImage(image, shardImgPath, colX[7] + 5, lineY + 30, ImageType.Shard);


                var scepterImgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ScepterDir, "aghsstatus_scepter_psd.png");

                var isScepterUsage = player.Player.ItemUsage.TryGetValue("ultimate_scepter", out var scepter);
                var isScepterBlessingUsage =
                    player.Player.ItemUsage.TryGetValue("ultimate_scepter_2", out var scepterBlessing);
                if ((isScepterUsage || isScepterBlessingUsage) && (scepter is > 0 || scepterBlessing is > 0))
                {
                    scepterImgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ScepterDir, "aghsstatus_scepter_on_psd.png");
                }
                AddImageToImage(image, scepterImgPath, colX[7], lineY, ImageType.Scepter);


                var playerItems = new[]
                {
                    player.Player.Item0, player.Player.Item1, player.Player.Item2,
                    player.Player.Item3, player.Player.Item4, player.Player.Item5
                };
                var imageX = colX[8];
                foreach (var itemId in playerItems.Where(i => i != null && i != 0))
                {
                    var item = _dotabaseService.Items[itemId!.Value];
                    var itemImage = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets",
                        item.Icon.TrimStart('/'));

                    AddImageToImage(image, itemImage, imageX, lineY, ImageType.Item);
                    imageX += 69;
                }

                if (player.Player.NeutralItem != null && player.Player.NeutralItem.Value != 0)
                {
                    var neutralItem = _dotabaseService.Items[player.Player.NeutralItem.Value];
                    var neutralImage = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets",
                        neutralItem.Icon.TrimStart('/'));

                    AddImageToImage(image, neutralImage, colX[9], lineY + 9, ImageType.NeutralItem);
                }

                lineY += 50;
            }
        });

        image.Save(path);

        Logger.LogTrace("Finished building results table image.");
        return path;
    }

    private void DrawPlayerName(IImageProcessingContext ctx, PlayerRecord player, int[] colX, int lineY, int marginY)
    {
        var color = player.Name != null ? Color.White : Color.LightGray;
        var playerName = player.Name ?? player.Player.Personaname ?? "Anonymous";
        if (playerName.Length > 14)
        {
            playerName = playerName.Substring(0, 12) + "...";
        }
        
        ctx.DrawText(playerName, _font, color, new PointF(colX[2], lineY + marginY));
    }

    private void AddImageToImage(Image<Rgba32> baseImage, string imagePath, int destinationX, int destinationY,
        ImageType imageType)
    {
        var size = imageType switch
        {
            ImageType.Hero => new Size(89, 50),
            ImageType.Item => new Size(69, 50),
            ImageType.NeutralItem => new Size(44, 32),
            ImageType.Background => new Size(1043, 600),
            ImageType.Scepter => new Size(30, 30),
            ImageType.Shard => new Size(20, 20),
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
    
    private void AddHeader(Image<Rgba32> baseImage, Match matchDetails, Team winTeam)
    {
        baseImage.Mutate(ctx =>
        {
            ctx.DrawText(winTeam == Team.Radiant ? "Radiant Victory!" : "Dire Victory!", _headerFont,
                winTeam == Team.Radiant ? Color.ForestGreen : Color.Crimson, new PointF(10, 5));
            ctx.DrawText(matchDetails.RadiantScore.ToString(), _headerFont, Color.ForestGreen, new PointF(420, 5));
            ctx.DrawText(":", _headerFont, Color.White, new PointF(470, 5));
            ctx.DrawText(matchDetails.DireScore.ToString(), _headerFont, Color.Crimson, new PointF(490, 5));
        });
    }

    private static string FormatThousands(long? value)
    {
        switch (value)
        {
            case null:
                return "0";
            case >= 1000:
            {
                var result = Math.Round((double)value / 1000, 1);
                return result.ToString("0.0",CultureInfo.InvariantCulture) + "k";
            }
            default:
                return value.Value.ToString("0.0", CultureInfo.InvariantCulture);
        }
    }

    private enum ImageType
    {
        Hero,
        Item,
        Background, 
        NeutralItem, 
        Scepter,
        Shard
    }
}