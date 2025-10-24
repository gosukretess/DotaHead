using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Drawing;
using Color = SixLabors.ImageSharp.Color;
using Path = System.IO.Path;
using Point = SixLabors.ImageSharp.Point;
using PointF = SixLabors.ImageSharp.PointF;
using Rectangle = SixLabors.ImageSharp.Rectangle;
using Size = SixLabors.ImageSharp.Size;

namespace TestApp
{
    internal class Program
    {
        private static Font _headerFont;
        private static int _width = 1043;
        private static int _height = 600;
        private const int Margin = 4;
        private static Font _font;
        const int QrCodeSize = 25;

        static void Main(string[] args)
        {
            Console.WriteLine("Started building results table image.");
            SetFont();

            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "table.png");
            using var image = new Image<Rgba32>(_width, _height);

            image.Mutate(ctx =>
            {
                SetBackground(ctx);
                // DrawLines(ctx);
                // SetHeaderTexts(ctx);
                // HeaderAndFirstRow(ctx, image);
                // MultipleTestImages(ctx, imageToAdd);
                // TextCtxMethods(ctx);

                bool[,] pattern = GetQrPattern();
                using Image<L8> image = RenderQrCodeToImage(pattern, 10);
                ctx.DrawImage(image, new Point(100, 100), 1);

                // ctx.Fill(new PatternBrush(
                //     Color.Black,
                //     Color.White,
                //     GetQrPatternBool()
                // ), new RectangularPolygon(0, 0, 500, 500));


            });

            image.Save(path);

            Console.WriteLine("Finished building results table image.");
        }

        static bool[,] GetQrPatternBool()
        {
            return new[,]
            {
        { true, true, true, true, true, true, true, false, true, true, false, false, false, true, false, true, true, false, true, true, true, true, true, true, true },
        { true, false, false, false, false, false, true, false, false, false, false, false, false, false, false, false, false, false, true, false, false, false, false, false, true },
        { true, false, true, true, true, false, true, false, true, false, false, true, true, true, false, true, true, false, true, false, true, true, true, false, true },
        { true, false, true, true, true, false, true, false, true, true, false, true, false, true, true, true, false, false, true, false, true, true, true, false, true },
        { true, false, true, true, true, false, true, false, true, true, true, true, false, false, true, false, true, false, true, false, true, true, true, false, true },
        { true, false, false, false, false, false, true, false, false, true, false, false, true, false, true, false, false, false, true, false, false, false, false, false, true },
        { true, true, true, true, true, true, true, false, true, false, true, false, true, false, true, false, true, false, true, true, true, true, true, true, true },
        { false, false, false, false, false, false, false, false, false, true, false, true, false, false, true, true, false, false, false, false, false, false, false, false, false },
        { true, true, true, true, false, false, true, false, true, false, false, false, false, true, true, false, false, true, false, false, true, true, true, false, true },
        { true, true, false, true, false, false, false, true, true, true, false, false, false, false, true, false, false, false, false, true, false, false, false, true, false },
        { false, false, false, false, true, false, true, true, true, false, false, false, false, true, true, false, true, true, true, false, false, false, false, false, false },
        { false, true, false, false, false, true, false, false, false, false, false, true, true, true, false, true, false, true, false, true, false, true, true, false, false },
        { false, true, false, false, false, false, true, true, false, true, false, true, false, true, true, false, false, true, true, false, true, false, true, true, true },
        { false, true, false, true, false, false, false, false, true, false, true, true, false, false, false, true, false, false, true, true, true, false, false, false, true },
        { false, true, false, false, false, true, true, true, false, true, true, false, true, false, false, false, false, true, false, false, true, false, true, true, false },
        { true, false, true, false, true, false, false, false, true, false, false, false, true, false, false, true, false, false, true, true, true, false, false, false, true },
        { false, false, true, false, false, false, true, false, false, false, true, true, false, false, false, true, true, true, true, true, true, true, true, true, true },
        { false, false, false, false, false, false, false, false, true, false, true, false, true, false, true, true, true, false, false, false, true, false, true, false, true },
        { true, true, true, true, true, true, true, false, false, false, false, true, true, true, false, false, true, false, true, false, true, false, true, true, true },
        { true, false, false, false, false, false, true, false, false, false, true, false, true, false, true, true, true, false, false, false, true, false, false, true, true },
        { true, false, true, true, true, false, true, false, false, true, true, true, true, true, false, false, true, true, true, true, true, true, false, true, false },
        { true, false, true, true, true, false, true, false, true, false, false, false, true, true, false, true, false, false, true, false, true, true, true, true, true },
        { true, false, true, true, true, false, true, false, true, true, true, true, true, true, false, true, true, true, true, false, true, false, true, true, false },
        { true, false, false, false, false, false, true, false, true, false, true, false, false, false, false, false, false, true, true, false, true, false, true, false, false },
        { true, true, true, true, true, true, true, false, true, true, false, false, true, false, true, false, false, false, false, true, true, true, true, true, true },
    };
        }


        static Image<L8> RenderQrCodeToImage(bool[,] pattern, int pixelSize)
        {
            int imageSize = pixelSize * QrCodeSize;
            Image<L8> image = new(imageSize, imageSize);

            L8 black = new(0);
            L8 white = new(255);

            image.ProcessPixelRows(pixelAccessor =>
            {
                for (int yQr = 0; yQr < QrCodeSize; yQr++)
                {
                    for (int y = yQr * pixelSize; y < (yQr + 1) * pixelSize; y++)
                    {
                        Span<L8> pixelRow = pixelAccessor.GetRowSpan(y);
                        for (int xQr = 0; xQr < QrCodeSize; xQr++)
                        {
                            L8 color = pattern[xQr, yQr] ? white : black;

                            for (int x = xQr * pixelSize; x < (xQr + 1) * pixelSize; x++)
                            {
                                pixelRow[x] = color;
                            }
                        }
                    }
                }
            });

            return image;
        }


        static bool[,] GetQrPattern()
        {
            const bool _ = true;
            const bool x = false;
            return new[,]
            {
                { x, x, x, x, x, x, x, _, x, x, _, _, _, x, _, x, x, _, x, x, x, x, x, x, x },
                { x, _, _, _, _, _, x, _, _, _, _, _, _, _, _, _, _, _, x, _, _, _, _, _, x },
                { x, _, x, x, x, _, x, _, x, _, _, x, x, x, _, x, x, _, x, _, x, x, x, _, x },
                { x, _, x, x, x, _, x, _, x, x, _, x, _, x, x, x, _, _, x, _, x, x, x, _, x },
                { x, _, x, x, x, _, x, _, x, x, x, x, _, _, x, _, x, _, x, _, x, x, x, _, x },
                { x, _, _, _, _, _, x, _, _, x, _, _, x, _, x, _, _, _, x, _, _, _, _, _, x },
                { x, x, x, x, x, x, x, _, x, _, x, _, x, _, x, _, x, _, x, x, x, x, x, x, x },
                { _, _, _, _, _, _, _, _, _, x, _, x, _, _, x, x, _, _, _, _, _, _, _, _, _ },
                { x, x, x, x, _, _, x, _, x, _, _, _, _, x, x, _, _, x, _, _, x, x, x, _, x },
                { x, x, _, x, _, _, _, x, x, x, _, _, _, _, x, _, _, _, _, x, _, _, _, x, _ },
                { _, _, _, _, x, _, x, x, x, _, _, _, _, x, x, _, x, x, x, _, _, _, _, _, _ },
                { _, x, _, _, _, x, _, _, _, _, _, x, x, x, _, x, _, x, _, x, _, x, x, _, _ },
                { _, x, _, _, _, _, x, x, _, x, _, x, _, x, x, _, _, x, x, _, x, _, x, x, x },
                { _, x, _, x, _, _, _, _, x, _, x, x, _, _, _, x, _, _, x, x, x, _, _, _, x },
                { _, x, _, _, _, x, x, x, _, x, x, _, x, _, _, _, _, x, _, _, x, _, x, x, _ },
                { x, _, x, _, x, _, _, _, x, _, _, _, x, _, _, x, _, _, x, x, x, _, _, _, x },
                { _, _, x, _, _, _, x, _, _, _, x, x, _, _, _, x, x, x, x, x, x, x, x, x, x },
                { _, _, _, _, _, _, _, _, x, _, x, _, x, _, x, x, x, _, _, _, x, _, x, _, x },
                { x, x, x, x, x, x, x, _, _, _, _, x, x, x, _, _, x, _, x, _, x, _, x, x, x },
                { x, _, _, _, _, _, x, _, _, _, x, _, x, _, x, x, x, _, _, _, x, _, _, x, x },
                { x, _, x, x, x, _, x, _, _, x, x, x, x, x, _, _, x, x, x, x, x, x, _, x, _ },
                { x, _, x, x, x, _, x, _, x, _, _, _, x, x, _, x, _, _, x, _, x, x, x, x, x },
                { x, _, x, x, x, _, x, _, x, x, x, x, x, x, _, x, x, x, x, _, x, _, x, x, _ },
                { x, _, _, _, _, _, x, _, x, _, x, _, _, _, _, _, _, x, x, _, x, _, x, _, _ },
                { x, x, x, x, x, x, x, _, x, x, _, _, x, _, x, _, _, _, _, x, x, x, x, x, x },
            };
        }

        private static void TextCtxMethods(IImageProcessingContext ctx)
        {
            var imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "aegis.png");
            using var imageToAdd = Image.Load(imagePath);

            imageToAdd.Mutate(img =>
            {
                img.GaussianBlur(5f);
            });

            ctx.DrawImage(imageToAdd, new Point(20, 100), 1);
            ctx.DrawText("GaussianBlur", _font, Color.Gold, new Point(120, 120));

            using var imageToAdd2 = Image.Load(imagePath);
            imageToAdd2.Mutate(img =>
            {
                img.BlackWhite();
            });
            ctx.DrawImage(imageToAdd2, new Point(20, 200), 1);
            ctx.DrawText("BlackWhite", _font, Color.Gold, new Point(120, 220));

            using var imageToAdd3 = Image.Load(imagePath);
            imageToAdd3.Mutate(img =>
            {
                img.Sepia();
            });
            ctx.DrawImage(imageToAdd3, new Point(20, 300), 1);
            ctx.DrawText("Sepia", _font, Color.Gold, new Point(120, 320));

            using var imageToAdd4 = Image.Load(imagePath);
            imageToAdd4.Mutate(img =>
            {
                img.Vignette(Color.Black);
            });
            ctx.DrawImage(imageToAdd4, new Point(20, 400), 1);
            ctx.DrawText("Vignette", _font, Color.Gold, new Point(120, 420));

            using var imageToAdd5 = Image.Load(imagePath);
            imageToAdd5.Mutate(img =>
            {
                img.Flip(FlipMode.Vertical);
            });
            ctx.DrawImage(imageToAdd5, new Point(20, 500), 1);
            ctx.DrawText("Flip", _font, Color.Gold, new Point(120, 520));



            using var imageToAdd6 = Image.Load(imagePath);
            imageToAdd6.Mutate(img =>
            {
                img.Skew(45f, 135f);
            });
            ctx.DrawImage(imageToAdd6, new Point(490, 10), 1);
            ctx.DrawText("Skew", _font, Color.Gold, new Point(650, 80));

            using var imageToAdd7 = Image.Load(imagePath);
            imageToAdd7.Mutate(img =>
            {
                img.Invert(new Rectangle(new Point(10, 10), new Size(50, 40)));
            });
            ctx.DrawImage(imageToAdd7, new Point(500, 200), 1);
            ctx.DrawText("Invert", _font, Color.Gold, new Point(600, 220));

            using var imageToAdd8 = Image.Load(imagePath);
            imageToAdd8.Mutate(img =>
            {
                img.Glow(Color.Yellow);
            });
            ctx.DrawImage(imageToAdd8, new Point(500, 300), 1);
            ctx.DrawText("Glow", _font, Color.Gold, new Point(600, 320));

            using var imageToAdd9 = Image.Load(imagePath);
            imageToAdd9.Mutate(img =>
            {
                img.Resize(new ResizeOptions
                {
                    Size = new Size(88, 88),
                    Mode = ResizeMode.Crop
                });
                ApplyRoundedCorners(img, 44);
            });
            ctx.DrawImage(imageToAdd9, new Point(500, 400), 1);
            ctx.DrawText("Crop", _font, Color.Gold, new Point(600, 420));

            using var imageToAdd10 = Image.Load(imagePath);
            imageToAdd10.Mutate(img =>
            {
                img.ColorBlindness(ColorBlindnessMode.Protanopia);
            });
            ctx.DrawImage(imageToAdd10, new Point(500, 520), 1);
            ctx.DrawText("Color Blindness", _font, Color.Gold, new Point(600, 540));
        }


        // This method can be seen as an inline implementation of an `IImageProcessor`:
        // (The combination of `IImageOperations.Apply()` + this could be replaced with an `IImageProcessor`)
        private static IImageProcessingContext ApplyRoundedCorners(IImageProcessingContext context, float cornerRadius)
        {
            Size size = context.GetCurrentSize();
            IPathCollection corners = BuildCorners(size.Width, size.Height, cornerRadius);

            context.SetGraphicsOptions(new GraphicsOptions()
            {
                Antialias = true,

                // Enforces that any part of this shape that has color is punched out of the background
                AlphaCompositionMode = PixelAlphaCompositionMode.DestOut
            });

            // Mutating in here as we already have a cloned original
            // use any color (not Transparent), so the corners will be clipped
            foreach (IPath path in corners)
            {
                context = context.Fill(Color.Red, path);
            }

            return context;
        }

        private static PathCollection BuildCorners(int imageWidth, int imageHeight, float cornerRadius)
        {
            // First create a square
            var rect = new RectangularPolygon(-0.5f, -0.5f, cornerRadius, cornerRadius);

            // Then cut out of the square a circle so we are left with a corner
            IPath cornerTopLeft = rect.Clip(new EllipsePolygon(cornerRadius - 0.5f, cornerRadius - 0.5f, cornerRadius));

            // Corner is now a corner shape positions top left
            // let's make 3 more positioned correctly, we can do that by translating the original around the center of the image.

            float rightPos = imageWidth - cornerTopLeft.Bounds.Width + 1;
            float bottomPos = imageHeight - cornerTopLeft.Bounds.Height + 1;

            // Move it across the width of the image - the width of the shape
            IPath cornerTopRight = cornerTopLeft.RotateDegree(90).Translate(rightPos, 0);
            IPath cornerBottomLeft = cornerTopLeft.RotateDegree(-90).Translate(0, bottomPos);
            IPath cornerBottomRight = cornerTopLeft.RotateDegree(180).Translate(rightPos, bottomPos);

            return new PathCollection(cornerTopLeft, cornerBottomLeft, cornerTopRight, cornerBottomRight);
        }

        private static void MultipleTestImages(IImageProcessingContext ctx, Image imageToAdd)
        {
            var y = 10;
            var i = 0;
            var x = 20;
            foreach (PixelAlphaCompositionMode type in Enum.GetValues(typeof(PixelAlphaCompositionMode)))
            {
                ctx.DrawImage(imageToAdd, new Point(x, y), PixelColorBlendingMode.Normal, 
                    PixelAlphaCompositionMode.DestIn, 1);
                
                ctx.DrawText(type.ToString(), _font, Color.Gold, new PointF(x + 150, y+25));
                y += 100;
                i++;
                
                if (i == 6)
                {
                    y = 10;
                    x = 520;
                }
            }
        }

        private static string HeaderAndFirstRow(IImageProcessingContext ctx, Image<Rgba32> image)
        {
            var colX = new[]
            {
                11, 100 + Margin, 137 + Margin, 337 + Margin, 
                377 + Margin, 417 + Margin, 457 + Margin, 537 + Margin, 
                571 + Margin, 989 + Margin
            };

            var headersY = 60;
            ctx.DrawText("K", _font, Color.White, new PointF(colX[3], headersY));
            ctx.DrawText("D", _font, Color.White, new PointF(colX[4], headersY));
            ctx.DrawText("A", _font, Color.White, new PointF(colX[5], headersY));
            ctx.DrawText("Net", _font, Color.Yellow, new PointF(colX[6], headersY));
            ctx.DrawText("Items", _font, Color.White, new PointF(colX[8], headersY));

            var marginY = 13;
            var lineY = 90;
            var imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "hero.png");
            AddImageToImage(image, imagePath, colX[0], lineY, new Size(89, 50));
            ctx.DrawText("25", _font, Color.Gold, new PointF(colX[1], lineY + marginY));
            ctx.DrawText("Player Name", _font, Color.White, new PointF(colX[2], lineY + marginY));
            ctx.DrawText("10", _font, Color.White, new PointF(colX[3], lineY + marginY));
            ctx.DrawText("0", _font, Color.White, new PointF(colX[4], lineY + marginY));
            ctx.DrawText("99", _font, Color.White, new PointF(colX[5], lineY + marginY));
            ctx.DrawText("25k", _font, Color.Yellow, new PointF(colX[6], lineY + marginY));


            var shardImgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "aghsstatus_shard_on_psd.png");
            AddImageToImage(image, shardImgPath, colX[7] + 5, lineY + 30, new Size(20, 20));
            var scepterImgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "aghsstatus_scepter_psd.png");
            AddImageToImage(image, scepterImgPath, colX[7], lineY, new Size(30, 30));

            var playerItems = new[] { "1.png", "2.png", "3.png", "4.png", "5.png", "6.png" };
            var imageX = colX[8];
            foreach (var itemId in playerItems)
            {
                var itemImage = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, itemId);
                AddImageToImage(image, itemImage, imageX, lineY, new Size(69, 50));
                imageX += 69;
            }

            var neutralImage = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "neutral.png");
            AddImageToImage(image, neutralImage, colX[9], lineY + 9, new Size(44, 32));
                
            lineY += 50;
            return imagePath;
        }

        private static void AddImageToImage(Image<Rgba32> baseImage, string imagePath, 
            int destinationX, int destinationY, Size size)
        {
            using var imageToAdd = Image.Load(imagePath);
            imageToAdd.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = size,
                Mode = ResizeMode.Stretch
            }));

            baseImage.Mutate(ctx => ctx.DrawImage(imageToAdd, 
                new Point(destinationX, destinationY), 1));

            
        }

        private static void SetHeaderTexts(IImageProcessingContext ctx)
        {
            ctx.DrawText("Radiant Victory!", _headerFont, Color.ForestGreen, new PointF(10, 5));
            ctx.DrawText("15", _headerFont, Color.ForestGreen, new PointF(420, 5));
            ctx.DrawText(":", _headerFont, Color.White, new PointF(470, 5));
            ctx.DrawText("0", _headerFont, Color.Crimson, new PointF(490, 5));
        }

        private static void SetFont()
        {
            var fontCollection = new FontCollection();
            var fontFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Roboto-Bold.ttf");
            var fontFamily = fontCollection.Add(fontFilePath);
            _headerFont = fontFamily.CreateFont(40, FontStyle.Bold);
            _font = fontFamily.CreateFont(26, FontStyle.Bold);
        }

        private static void DrawLines(IImageProcessingContext ctx)
        {
            ctx.DrawLine(Pens.Solid(Color.Green, 6), new PointF(8, 90), new PointF(8, 340));
            ctx.DrawLine(Pens.Solid(Color.Crimson, 6), new PointF(8, 340), new PointF(8, 590));
        }

        private static void SetBackground(IImageProcessingContext ctx)
        {
            var size = new Size(_width, _height);
            var imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bg.png");
            using var imageToAdd = Image.Load(imagePath);
            imageToAdd.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = size,
                Mode = ResizeMode.Stretch
            }));
            ctx.DrawImage(imageToAdd, new Point(0, 0), 1);
        }
    }
}
