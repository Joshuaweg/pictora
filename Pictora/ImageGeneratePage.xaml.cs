using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http;
using DotNetEnv;
using System.Diagnostics;
using System.Net;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;


namespace Pictora
{
    public partial class ImageGeneratePage : ContentPage
    {
        private string API_KEY = "";
        private string GeneratedURL;
        int count = 0;
        //private ArrayList envFile = new ArrayList();
        public ImageGeneratePage()
        {
            InitializeComponent();
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string envPath = Path.Combine(baseDirectory, ".env");

            DotNetEnv.Env.Load(envPath);
            API_KEY = DotNetEnv.Env.GetString("FAL_API_KEY", "");

        }

        private void Test(object sender, EventArgs e)
        {
            Debug.WriteLine(sender);
        }
        private void ButtonShareClicked(object sender, EventArgs e)
        {
            ShareFile();
        }

        private void ButtonSaveClicked(object sender, EventArgs e)
        {
            DownloadFile();
        }

        private async Task ShareFile()
        {

            // If there is a way to do this with the HttpClient class (or anything else), so the complier doesn't send me a obsolite message, implement it that way.
            WebClient webClient = new();

            // This should just lead to the systems user folder (The one with your name on it.) If there is a better directory to use, please let me know ASAP.
            string location = (Environment.GetFolderPath(Environment.SpecialFolder.Personal).ToString()) + $"\\test.jpeg"; 
            webClient.DownloadFile(GeneratedURL, location);

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Share generated image",
                File = new ShareFile(location)
            });
        }

        private async Task DownloadFile()
        {
            
            // If there is a way to do this with the HttpClient class (or anything else), so the complier doesn't send me a obsolite message, implement it that way.
            WebClient webClient = new();

            // This should just lead to the systems user folder (The one with your name on it.) If there is a better directory to use, please let me know ASAP.
            //
            // Count is temperary, replace with a date and timestamp for the filename
            string location = (Environment.GetFolderPath(Environment.SpecialFolder.Personal).ToString()) + $"\\test{count}.jpeg";
            webClient.DownloadFile(GeneratedURL, location);

            count++;

            // This isn't the proper way to implement this, but it should work for now.
            // Look into what the actual way to do this is, assuming I'm actually alowed to.
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await DisplayAlert("Prompt", $"File downloaded to {location}", "Ok");
            });
        }
        private void GenerateButtonClicked(object sender, EventArgs e)
        {
            if (API_KEY.Equals("")) // If there isn't a key, display an error
                return;

            String prompt = Prompt.Text;

            if (prompt.Equals("")) // If there isn't a prompt, display an error
            {
                return;
            }

            prompt = ("\"prompt\": \"" + prompt + "\"");

            String size = ",\"image_size\": ";

            switch (Size.SelectedIndex)
            {
                /*case 0:
                    size += "square";
                    break;
                case 1:
                    size += "square_hd";
                    break;*/
                // TODO - Add the rest later.
                // Ignore the property for now
                default:
                    size += "\"square_hd\"";
                    break;     
            }

            // TODO - Add the options for handling models and loras

            string finalPrompt = $"{{{prompt}";

            if (!size.Equals(""))
            {
                finalPrompt += $"{size}";
            }

            // End
            finalPrompt += $"}}";

            Debug.WriteLine(finalPrompt);

            string requestUrl = "https://queue.fal.run/fal-ai/fast-sdxl";

            /*
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await DisplayAlert("Prompt", $"{finalPrompt}", "Ok");
            });
            */

            HttpRequestMessage httpRequestMessage = new(HttpMethod.Post, requestUrl);

            httpRequestMessage.Content = new StringContent(finalPrompt, Encoding.UTF8, "application/json");
            httpRequestMessage.Headers.Add("Authorization", $"Key {API_KEY}");

            HttpClient httpClient = new();
            HttpResponseMessage responce = httpClient.Send(httpRequestMessage);
            Stream body = responce.Content.ReadAsStream();

            StreamReader reader = new(body);
            string result = reader.ReadToEnd();

            Progress progress_JSON = JsonSerializer.Deserialize<Progress>(result);

            // Have a while loop that rechecks the status every second until it is complete.
            while ((progress_JSON.status).Equals("IN_QUEUE"))
            {
                Task.Delay(1000).Wait(); // Delay for 1 seconds

                // Check the request to see if it is done.
                requestUrl = progress_JSON.status_url;

                httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                httpRequestMessage.Headers.Add("Authorization", $"Key {API_KEY}");

                responce = httpClient.Send(httpRequestMessage);
                body = responce.Content.ReadAsStream();

                reader = new StreamReader(body);
                result = reader.ReadToEnd();
                progress_JSON = JsonSerializer.Deserialize<Progress>(result);
            }


            requestUrl = progress_JSON.response_url;
            Result result_JSON;

            // This do-while makes sure the object being returned is the actual result.
            do
            {
                Task.Delay(1000).Wait(); // Delay for 1 seconds

                httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                httpRequestMessage.Headers.Add("Authorization", $"Key {API_KEY}");

                responce = httpClient.Send(httpRequestMessage);

                result = new StreamReader(responce.Content.ReadAsStream()).ReadToEnd();
                result_JSON = JsonSerializer.Deserialize<Result>(result);
                Debug.WriteLine(result_JSON.detail);
            } while (result_JSON.detail != "");
            

            Debug.WriteLine(result_JSON);
            Debug.WriteLine(result_JSON.images[0].url);

            GeneratedURL = result_JSON.images[0].url;

            Generated_Image.Source = GeneratedURL;
            GenerateButton.Text = "Regenerate";
            SaveControls.IsVisible = true;

            // TODO - Update the image and edit the page to add in more user elements

        }

        private class Progress
        {
            public string status { get; set; } = string.Empty;
            //public string request_id { get; set; } = string.Empty;
            public string response_url { get; set; } = string.Empty; // Result
            public string status_url { get; set; } = string.Empty; // Status Check
            //string cancel_url { get; set; } = string.Empty;
            //string logs { get; set; } = string.Empty;
            //string[] metrics;
            //int queue_position;
        }

        private class Result
        {
            // This is used to check if the actual result JSON object is the one that is being returned, should be blank if so.
            public string detail { get; set; } = string.Empty; 
            public List<Images> images { get; set; } = new();
            // public Timings timings { get; set; } = new();
            //public long seed { get; set; } = 0;
            // public List<Bool> has_nsfw_concepts = new();
            //public string prompt { get; set; } = string.Empty;
        }

        private class Images
        {
            public string url { get; set; } = string.Empty;
            public int width { get; set; } = 0;
            public int height { get; set; } = 0;
            //public string content_type { get; set; } = string.Empty;

        }

        public class Image
        {
            [BsonId]
            [BsonRepresentation(BsonType.ObjectId)]
            public string Id { get; set; }

            [BsonElement("id")]
            public int NumericId { get; set; }

            [BsonElement("name")]
            public string Name { get; set; }

            [BsonElement("model")]
            public string Model { get; set; }

            [BsonElement("style")]
            public string Style { get; set; }

            [BsonElement("prompt")]
            public string Prompt { get; set; }

            [BsonElement("description")]
            public string Description { get; set; }

            [BsonElement("userid")]
            public int UserId { get; set; }

            [BsonElement("created")]
            public DateTime Created { get; set; }

            [BsonElement("baseImage")]
            public int BaseImage { get; set; }

            [BsonElement("upvotes")]
            public int Upvotes { get; set; }

            [BsonElement("downvotes")]
            public int Downvotes { get; set; }

            [BsonElement("tags")]
            public List<string> Tags { get; set; }

            [BsonElement("image_size")]
            public ImageSize ImageSize { get; set; }

            [BsonElement("image_url")]
            public string ImageUrl { get; set; }
        }

        public class ImageSize
        {
            [BsonElement("name")]
            public string Name { get; set; }

            [BsonElement("height")]
            public int Height { get; set; }

            [BsonElement("width")]
            public int Width { get; set; }
        }


    }
    
}
