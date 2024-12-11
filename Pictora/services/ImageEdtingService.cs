using System.Buffers;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using Pictora.Models;


namespace Pictora.Services
{
    using SharpImage = SixLabors.ImageSharp.Image;

    public class ImageEditingService : IImageEditingService
    {
        private readonly HttpClient _client;
        private const string BASE_URL = "https://queue.fal.run/fal-ai/fast-sdxl";
        private const string INPAINT_URL = "https://queue.fal.run/fal-ai/fast-sdxl/inpainting";
        public ImageEditingService(string apiKey)
        {
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Add("Authorization", $"Key {apiKey}");
        }

        public async Task<string> ConvertImageToBase64WithCompression(byte[] img)
        {
            try
            {
                // Create a memory stream from the byte array
                using var inputStream = new MemoryStream(img);
                // Load the image from the memory stream
                using var image = await SharpImage.LoadAsync(inputStream);

                // Calculate new dimensions while maintaining aspect ratio
                int maxDimension = 1024; // Max dimension for either width or height
                double scale = Math.Min((double)maxDimension / image.Width, (double)maxDimension / image.Height);
                int newWidth = (int)(image.Width * scale);
                int newHeight = (int)(image.Height * scale);

                // Resize the image
                image.Mutate(x => x.Resize(newWidth, newHeight));

                // Compress to JPEG with quality setting
                var jpegEncoder = new JpegEncoder
                {
                    Quality = 80 // Adjust quality (0-100) to balance size and quality
                };

                using var outputStream = new MemoryStream();
                await image.SaveAsync(outputStream, jpegEncoder);

                // Get the compressed size for debugging
                var compressedSize = outputStream.Length;
                Debug.WriteLine($"Compressed Image Size: {compressedSize}");

                return Convert.ToBase64String(outputStream.ToArray());
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to convert and compress image: {ex.Message}", ex);
            }
        }
        private async Task<string> ConvertImageToBase64WithCompression(string imagePath)
        {
            try
            {
                using var image = await SharpImage.LoadAsync(imagePath);

                // Calculate new dimensions while maintaining aspect ratio
                int maxDimension = 1024; // Max dimension for either width or height
                double scale = Math.Min((double)maxDimension / image.Width, (double)maxDimension / image.Height);
                int newWidth = (int)(image.Width * scale);
                int newHeight = (int)(image.Height * scale);

                // Resize the image
                image.Mutate(x => x.Resize(newWidth, newHeight));

                // Compress to JPEG with quality setting
                var jpegEncoder = new JpegEncoder
                {
                    Quality = 80 // Adjust quality (0-100) to balance size and quality
                };

                using var memoryStream = new MemoryStream();
                await image.SaveAsync(memoryStream, jpegEncoder);

                // Get the compressed size for debugging
                var compressedSize = memoryStream.Length;
                Debug.WriteLine($"Compressed Image Size: {compressedSize}");

                return Convert.ToBase64String(memoryStream.ToArray());
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to convert and compress image: {ex.Message}", ex);
            }
        }

        private async Task<string> SubmitInpaintRequest(string imageDataUri, string maskDataUri, string prompt, string negativePrompt)
        {
            var request = new InpaintRequest
            {
                ImageUrl = imageDataUri,
                MaskUrl = maskDataUri,
                Prompt = prompt,
                NegativePrompt = negativePrompt
            };

            var content = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _client.PostAsync(INPAINT_URL, content);
            var responseText = await response.Content.ReadAsStringAsync();

            Debug.WriteLine($"Inpaint Initial Response: {responseText}");

            using JsonDocument document = JsonDocument.Parse(responseText);
            return document.RootElement.GetProperty("request_id").GetString()
                ?? throw new Exception("No request ID in response");
        }

        public async Task<ImageEditResponse> InpaintImageAsync(
            string imageDataUri,
            string maskDataUri,
            string prompt,
            string negativePrompt = "")
        {
            try
            {
                // Submit the initial inpainting request
                string requestId = await SubmitInpaintRequest(imageDataUri, maskDataUri, prompt, negativePrompt);
                Debug.WriteLine($"Got inpaint request ID: {requestId}");

                // Poll for completion using existing status check method
                while (true)
                {
                    string status = await CheckRequestStatus(requestId);
                    Debug.WriteLine($"Inpaint Status: {status}");

                    switch (status.ToUpper())
                    {
                        case "COMPLETED":
                            return await GetRequestResult(requestId);
                        case "FAILED":
                            throw new Exception("Inpainting request failed");
                        case "PENDING":
                        case "PROCESSING":
                        case "IN_PROGRESS":
                        case "IN_QUEUE":
                            await Task.Delay(1000); // Wait 1 second before checking again
                            continue;
                        default:
                            throw new Exception($"Unknown status: {status}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in InpaintImageAsync: {ex}");
                throw;
            }
        }

        public async Task<byte[]> CreateMaskImage(byte[] drawingData, int width, int height)
        {
            try
            {
                using var image = SharpImage.Load(drawingData);

                // Ensure the image is in the correct format for the mask
                image.Mutate(x => x
                    .Resize(width, height)
                    .Grayscale()  // Convert to grayscale
                    .BinaryThreshold(0.5f)); // Convert to binary black and white

                using var memStream = new MemoryStream();
                await image.SaveAsPngAsync(memStream);
                return memStream.ToArray();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating mask image: {ex}");
                throw;
            }
        }

        public async Task<string> ConvertToBase64DataUri(byte[] imageData, string mimeType = "image/png")
        {
            try
            {
                string base64String = Convert.ToBase64String(imageData);
                return $"data:{mimeType};base64,{base64String}";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error converting to base64: {ex}");
                throw;
            }
        }

        private async Task<string> SubmitRequest(string imagePath, string prompt, string negativePrompt)
        {
            // Convert image to Base64
            string uri = imagePath;
            if (!uri.StartsWith("https://"))
            {
                string base64Image = await ConvertImageToBase64WithCompression(imagePath);
                Debug.WriteLine($"Base64 Image Length: {base64Image.Length}");
                uri = $"data:image/jpeg;base64,{base64Image}";
            }

            var requestBody = new Dictionary<string, object>
            {
                ["image_url"] = uri,  // Direct Base64 string with data URL prefix or URL to image
                ["prompt"] = prompt,
                ["negative_prompt"] = negativePrompt,
                ["image_size"] = "square_hd",
                ["num_inference_steps"] = 25,
                ["guidance_scale"] = 7.5,
                ["strength"] = 0.95,
                ["num_images"] = 1,
                ["loras"] = new object[] { },
                ["embeddings"] = new object[] { },
                ["enable_safety_checker"] = true,
                ["safety_checker_version"] = "v1",
                ["format"] = "jpeg"
            };

            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _client.PostAsync($"{BASE_URL}/image-to-image", content);
            var responseText = await response.Content.ReadAsStringAsync();

            // Log the response for debugging
            Debug.WriteLine($"Initial Response: {responseText}");

            // Parse the response to get request_id
            using JsonDocument document = JsonDocument.Parse(responseText);
            return document.RootElement.GetProperty("request_id").GetString()
                ?? throw new Exception("No request ID in response");
        }
        private async Task<string> CheckRequestStatus(string requestId)
        {
            var response = await _client.GetAsync($"{BASE_URL}/requests/{requestId}/status");
            var statusResponse = await response.Content.ReadAsStringAsync();

            // Log status for debugging
            Debug.WriteLine($"Status Response: {statusResponse}");

            using JsonDocument document = JsonDocument.Parse(statusResponse);
            return document.RootElement.GetProperty("status").GetString()
                ?? throw new Exception("No status in response");
        }
        private async Task<ImageEditResponse> GetRequestResult(string requestId)
        {
            var response = await _client.GetAsync($"{BASE_URL}/requests/{requestId}");
            var resultText = await response.Content.ReadAsStringAsync();

            // Log result for debugging
            Debug.WriteLine($"Result Response: {resultText}");

            return JsonSerializer.Deserialize<ImageEditResponse>(resultText)
                ?? throw new Exception("Failed to deserialize response");
        }

        public async Task<ImageEditResponse> EditImageAsync(string imagePath, string prompt, string negativePrompt = "")
        {
            try
            {
                // Submit the initial request
                string requestId = await SubmitRequest(imagePath, prompt, negativePrompt);
                Debug.WriteLine($"Got request ID: {requestId}");

                // Poll for completion
                while (true)
                {
                    string status = await CheckRequestStatus(requestId);
                    Debug.WriteLine($"Status: {status}");

                    switch (status.ToUpper())
                    {
                        case "COMPLETED":
                            return await GetRequestResult(requestId);
                        case "FAILED":
                            throw new Exception("Request failed");
                        case "PENDING":
                        case "PROCESSING":
                        case "IN_PROGRESS":
                        case "IN_QUEUE":
                            await Task.Delay(1000); // Wait 1 second before checking again
                            continue;
                        default:
                            throw new Exception($"Unknown status: {status}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in EditImageAsync: {ex}");
                throw;
            }
        }

    }
}