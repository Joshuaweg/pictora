using Pictora.Models;

namespace Pictora.Services
{
    public interface IImageEditingService
    {
        Task<string> ConvertImageToBase64WithCompression(byte[] img);
        Task<ImageEditResponse> InpaintImageAsync(string imageDataUri, string maskDataUri, string prompt, string negativePrompt = "");
        Task<ImageEditResponse> EditImageAsync(string imagePath, string prompt, string negativePrompt = "");
    }
}