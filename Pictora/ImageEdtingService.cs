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


//Update this to Utility Request


namespace Pictora.Services
{
    using SharpImage = SixLabors.ImageSharp.Image;
    public class ImageEditRequest
    {
        [JsonPropertyName("image_url")]
        public ImageUrlInfo ImageUrl { get; set; }

        [JsonPropertyName("prompt")]
        public string Prompt { get; set; } = "";

        [JsonPropertyName("negative_prompt")]
        public string NegativePrompt { get; set; } = "";

        [JsonPropertyName("image_size")]
        public string ImageSize { get; set; } = "square_hd";

        [JsonPropertyName("num_inference_steps")]
        public int NumInferenceSteps { get; set; } = 25;

        [JsonPropertyName("guidance_scale")]
        public float GuidanceScale { get; set; } = 7.5f;

        [JsonPropertyName("strength")]
        public float Strength { get; set; } = 0.95f;

        [JsonPropertyName("num_images")]
        public int NumImages { get; set; } = 1;

        [JsonPropertyName("loras")]
        public List<object> Loras { get; set; } = new();

        [JsonPropertyName("embeddings")]
        public List<object> Embeddings { get; set; } = new();

        [JsonPropertyName("enable_safety_checker")]
        public bool EnableSafetyChecker { get; set; } = true;

        [JsonPropertyName("safety_checker_version")]
        public string SafetyCheckerVersion { get; set; } = "v1";

        [JsonPropertyName("format")]
        public string Format { get; set; } = "jpeg";
    }
    public class ImageUrlInfo
    {
        [JsonPropertyName("path")]
        public string Path { get; set; } = "";
    }

    public class ImageEditingService
    {
        private readonly HttpClient _client;
        private const string BASE_URL = "https://queue.fal.run/fal-ai/fast-sdxl";
        public ImageEditingService(string apiKey)
        {
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Add("Authorization", $"Key {apiKey}");
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



        private async Task<RequestStatus> GetRequestStatus(string requestId)
        {
            var response = await _client.GetAsync($"{BASE_URL}/requests/{requestId}/status");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<RequestStatus>()
                ?? throw new Exception("Failed to get request status");
        }

        private async Task<ImageEditResponse> GetResult(string requestId)
        {
            var response = await _client.GetAsync($"{BASE_URL}/requests/{requestId}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ImageEditResponse>()
                ?? throw new Exception("Failed to get result");
        }


        public class RequestResponse
        {
            [JsonPropertyName("request_id")]
            public string RequestId { get; set; } = "";
        }

        public class RequestStatus
        {
            [JsonPropertyName("status")]
            public string Status { get; set; } = "";

            [JsonPropertyName("error")]
            public string? Error { get; set; }
        }

        public class ImageEditResponse
        {
            [JsonPropertyName("images")]
            public List<ImageInfo> Images { get; set; } = new();

            [JsonPropertyName("timings")]
            public Timings Timings { get; set; } = new();

            [JsonPropertyName("seed")]
            [JsonConverter(typeof(SeedConverter))]
            public ulong Seed { get; set; }  // Changed to ulong


            [JsonPropertyName("has_nsfw_concepts")]
            public List<bool> HasNsfwConcepts { get; set; } = new();

            [JsonPropertyName("prompt")]
            public string Prompt { get; set; } = "";
        }


        public class ImageInfo
        {
            [JsonPropertyName("url")]
            public string Url { get; set; } = "";

            [JsonPropertyName("width")]
            public int Width { get; set; }

            [JsonPropertyName("height")]
            public int Height { get; set; }

            [JsonPropertyName("content_type")]
            public string ContentType { get; set; } = "";
        }

        public class Timings
        {
            [JsonPropertyName("inference")]
            public double Inference { get; set; }
        }
        public class SeedConverter : JsonConverter<ulong>
        {
            public override ulong Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.Number)
                {
                    try
                    {
                        return reader.GetUInt64();
                    }
                    catch
                    {
                        return 0;
                    }
                }
                return 0;
            }

            public override void Write(Utf8JsonWriter writer, ulong value, JsonSerializerOptions options)
            {
                writer.WriteNumberValue(value);
            }
        }
    }
}