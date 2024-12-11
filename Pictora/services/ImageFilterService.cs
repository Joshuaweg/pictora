using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Numerics;

namespace Pictora.Services
{
    public class ImageFilterService
    {
        public async Task<byte[]> ApplyBlackAndWhiteFilter(byte[] imageBytes)
        {
            using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(imageBytes);
            image.Mutate(x => x
                .Grayscale()
                .Contrast(1.1f));

            using var outputStream = new MemoryStream();
            await image.SaveAsJpegAsync(outputStream);
            return outputStream.ToArray();
        }

        public async Task<byte[]> ApplyBlueShiftFilter(byte[] imageBytes)
        {
            using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(imageBytes);
            image.Mutate(x => x.ProcessPixelRowsAsVector4(row =>
            {
                for (int x = 0; x < row.Length; x++)
                {
                    row[x] = new Vector4(
                        row[x].X * 0.8f,
                        row[x].Y * 0.9f,
                        row[x].Z * 1.2f,
                        row[x].W
                    );
                }
            }));

            using var outputStream = new MemoryStream();
            await image.SaveAsJpegAsync(outputStream);
            return outputStream.ToArray();
        }

        public async Task<byte[]> ApplyVintageFilter(byte[] imageBytes)
        {
            using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(imageBytes);
            
            image.Mutate(x => x.ProcessPixelRowsAsVector4(row =>
            {
                for (int x = 0; x < row.Length; x++)
                {
                    float r = row[x].X;
                    float g = row[x].Y;
                    float b = row[x].Z;

                    row[x] = new Vector4(
                        Math.Min(r * 1.2f, 1.0f),
                        g * 0.9f,
                        b * 0.8f,
                        row[x].W
                    );
                }
            }));

            image.Mutate(x => x
                .Contrast(1.1f)
                .Sepia(0.2f));

            using var outputStream = new MemoryStream();
            await image.SaveAsJpegAsync(outputStream);
            return outputStream.ToArray();
        }

        public async Task<byte[]> LoadImageBytes(string imagePath)
        {
            if (imagePath.Contains("http"))
            {
                using var client = new HttpClient();
                return await client.GetByteArrayAsync(imagePath);
            }
            return await File.ReadAllBytesAsync(imagePath);
        }
    }
}