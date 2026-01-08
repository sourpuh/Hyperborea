using Hyperborea.Guides; // For Fill and GraphicsOptions
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Hyperborea.Screenshot;

public static class ImageMasker
{
    public static async Task ApplyMaskAsync(string originalImagePath, Guide guide, string outputPath)
    {
        try
        {
            using (var originalImage = await Image.LoadAsync<Rgba32>(originalImagePath))
            {
                float[,] mask = new float[originalImage.Width, originalImage.Height];
                guide.AddToMask(guide.Center, mask);

                originalImage.Mutate(c => c.ProcessPixelRowsAsVector4((row, point) =>
                {
                    //Svc.Log.Info($"mutate " + point);

                    int y = point.Y;
                    for (int x = 0; x < row.Length; x++)
                    {
                        float maskLuminance = mask[x, y];
                        row[x] = row[x].WithAlpha(maskLuminance);
                    }
                }));

                var encoder = new PngEncoder
                {
                    ColorType = PngColorType.RgbWithAlpha,
                    BitDepth = PngBitDepth.Bit8
                };
                await originalImage.SaveAsPngAsync(outputPath, encoder);
            }
        }
        catch (Exception ex)
        {
            Svc.Log.Error(ex.ToString());
        }
    }
    //public static void ApplyMask(string originalImagePath, Guide guide, string outputPath)
    //{
    //    try
    //    {
    //        using (var originalImage = Image.Load<Rgba32>(originalImagePath))
    //        {
    //            Svc.Log.Info($"{originalImage.Width} {originalImage.Height}");
    //            bool[,] mask = new bool[originalImage.Width, originalImage.Height];
    //            Svc.Log.Info($"{mask.GetLength(0)} {mask.GetLength(1)}");

    //            guide.AddToMask(guide.Center, mask);
    //            Svc.Log.Info($"added to mask");

    //            originalImage.Mutate(c => c.ProcessPixelRowsAsVector4((row, point) =>
    //            {
    //                //Svc.Log.Info($"mutate " + point);

    //                int y = point.Y;
    //                for (int x = 0; x < row.Length; x++)
    //                {
    //                    float maskLuminance = mask[x, y] ? 1f : 0f;

    //                    //float maskLuminance = x / 5 % 10 == 0 || y / 5 % 10 == 0 ? 1f : 0f;
    //                    var modified = row[x];
    //                    //modified.X = x / 5 % 10 == 0 ? 1f : 0f;
    //                    //modified.Y = y / 5 % 10 == 0 ? 1f : 0f;
    //                    modified.W = maskLuminance;
    //                    //modified.Y = 255;
    //                    row[x] = modified;
    //                }
    //            }));

    //            var encoder = new PngEncoder
    //            {
    //                ColorType = PngColorType.RgbWithAlpha, // Explicitly set the desired color type
    //                BitDepth = PngBitDepth.Bit8 // Optional: specify bit depth (e.g., 8-bit)
    //            };
    //            originalImage.SaveAsPng(outputPath, encoder);
    //            Svc.Log.Info("saved to " + outputPath);
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        Svc.Log.Error(ex.ToString());
    //    }
    //}
}
