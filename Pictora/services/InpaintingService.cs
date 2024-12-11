using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using Microsoft.Maui.Graphics;
using Pictora.Models;
using PointFt = Microsoft.Maui.Graphics.PointF;
using Microsoft.Maui.Media;

namespace Pictora.Services
{
    public interface IInpaintingService
    {
        Task<byte[]> CreateMaskFromPaths(List<List<PointFt>> paths, GraphicsView maskCanvas);
        Task<(string path, ImageEditResponse response)> ProcessInpainting(byte[] imageData, byte[] maskData, string prompt, string negativePrompt);
        Task<byte[]> GetImageData(string imagePath);
    }

    public class InpaintingService : IInpaintingService
    {
        private const string INPAINT_URL = "https://queue.fal.run/fal-ai/fast-sdxl/inpainting";
        private readonly IImageEditingService _imageEditingService;
        private readonly string _editImagesDirectory;

        public InpaintingService(IImageEditingService imageEditingService, string editImagesDirectory)
        {
            _imageEditingService = imageEditingService;
            _editImagesDirectory = editImagesDirectory;
        }

        public async Task<byte[]> CreateMaskFromPaths(List<List<PointFt>> paths, GraphicsView maskCanvas)
        {
            var screenshot = await maskCanvas.CaptureAsync();
            using var stream = await screenshot.OpenReadAsync();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);

            byte[] maskData = memoryStream.ToArray();
            using var image = SixLabors.ImageSharp.Image.Load(maskData);
            image.Mutate(x => x
                .Grayscale()
                .BinaryThreshold(0.5f));

            using var outputStream = new MemoryStream();
            await image.SaveAsPngAsync(outputStream);
            return outputStream.ToArray();
        }

        public async Task<(string path, ImageEditResponse response)> ProcessInpainting(
            byte[] imageData,
            byte[] maskData,
            string prompt,
            string negativePrompt)
        {
            string img_uri = await _imageEditingService.ConvertImageToBase64WithCompression(imageData);
            string mask_uri = await _imageEditingService.ConvertImageToBase64WithCompression(maskData);

            var imageDataUri = $"data:image/png;base64,{img_uri}";
            var maskDataUri = $"data:image/png;base64,{mask_uri}";

            var result = await _imageEditingService.InpaintImageAsync(
                imageDataUri,
                maskDataUri,
                prompt,
                negativePrompt);

            if (result.Images.Count == 0)
                throw new Exception("No image was generated");

            var image = result.Images[0];
            using var httpClient = new HttpClient();
            byte[] resultImageData = await httpClient.GetByteArrayAsync(image.Url);

            string tempImagePath = Path.Combine(_editImagesDirectory, "inpainted_image.jpg");
            await File.WriteAllBytesAsync(tempImagePath, resultImageData);

            return (tempImagePath, result);
        }

        public async Task<byte[]> GetImageData(string imagePath)
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