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
using Pictora.Services;
using Pictora.Models;


namespace Pictora
{
    public partial class ImageGeneratePage : ContentPage
    {
        private string API_KEY = "";
        private string GeneratedURL;
        int count = 0;
        Result result_JSON;

        // TEMP: just in case you want to fill the database.
        Pictora.Models.Image generated_image; 
        MongoDBService mgdbs = new();

        //private ArrayList envFile = new ArrayList();
        public ImageGeneratePage()
        {
            InitializeComponent();
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string envPath = Path.Combine(baseDirectory, ".env");

            DotNetEnv.Env.Load(envPath);
            API_KEY = DotNetEnv.Env.GetString("FAL_API_KEY", "");

            mgdbs = new(); // TEMP

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

            //prompt = ("\"prompt\": \"" + prompt + "\"");



            // A safty check if the user uses the custom size option.
            if (Size.SelectedIndex == 7)
            {
                try // Checks for numerical input (Valid size range is checked in the Models.cs file)
                {
                    var a = Int32.Parse(CustomWidth.Text);
                    var b = Int32.Parse(CustomHeight.Text);
                } catch // Non-valid number input
                {
                    Debug.Print("Empty or invalid input");
                    return;
                }
                    
            }

            // TODO - Add the options for handling models and loras

            Prompt inputPrompt = new();

            inputPrompt.prompt = prompt;

            if (Size.SelectedIndex == 7)
            {
                inputPrompt.image_size.SetValues(Int32.Parse(CustomWidth.Text), Int32.Parse(CustomHeight.Text));
            }
            else
            {
                inputPrompt.image_size.SetValues(Size.SelectedIndex);
                
            }
            
            /*
            string finalPrompt = $"{{{prompt}";

            if (!size.Equals(""))
            {
                finalPrompt += $"{size}";
            }

            // End
            finalPrompt += $"}}";*/

            string finalPrompt = JsonSerializer.Serialize<Prompt>(inputPrompt);

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

                if (result_JSON.detail == "Internal Server Error")
                {
                    Debug.WriteLine("Something went wrong.");
                    return;
                }
            } while (result_JSON.detail != "");

            result_JSON.model = "Fast-SDXL"; // Replace with the value selected in model.
            result_JSON.style = "None"; // Relpace with the value selected in style.

            Debug.WriteLine(result_JSON);
            Debug.WriteLine(result_JSON.images[0].url);
            GeneratedURL = result_JSON.images[0].url;
            Generated_Image.Source = GeneratedURL;
            GenerateButton.Text = "Regenerate";
            SaveControls.IsVisible = true;
            DevUpload.IsVisible = true;

            // Implementing this here would spam the database with generated images and upload images the user may not be happy with without their consent.
            generated_image = new Pictora.Models.Image();
            generated_image.ImageSize = new ImageSize();
            generated_image.ImageUrl = GeneratedURL;
            generated_image.ImageSize.Height = result_JSON.images[0].height; // 1024
            generated_image.ImageSize.Width = result_JSON.images[0].width; // 1024
            generated_image.Created = result_JSON.created;
            generated_image.Prompt = result_JSON.prompt;
            generated_image.Model = result_JSON.model;
            generated_image.Style = result_JSON.style;
            generated_image.Tags = new List<string>(); // Defined on save page
            generated_image.UserId = 0;                // Defined on login page
            generated_image.Upvotes = 0;
            generated_image.Downvotes = 0;
            generated_image.Description = "Generated image";  // Defined on save page
            generated_image.Name = "Generated Image";  // Defined on save page
            generated_image.NumericId = 0;             // Defined by the database

            // Move this to when the user clicks on the 'Save' button in that layout.
            
            // This is only temperary. This is so the database doesn't get spamed with regenerated images.
            //mgdbs.CreateAsync("images", generated_image);

        }

        private void ButtonUploadClicked(object sender, EventArgs e)
        {
            //string json = JsonSerializer.Serialize<Result>(result_JSON);
            //Navigation.PushAsync(new ImageUploadPage(json));

            Navigation.PushAsync(new ImageUploadPage(result_JSON));
        }

        // TEMPERARY: In case you want to want to upload the image without going through the save page. DEV USE ONLY!
        private void ButtonDevUploadClicked(object sender, EventArgs e)
        {
            // Database gets assigned at the page creation.
            Debug.WriteLine(generated_image);
            Debug.WriteLine($"G:{generated_image.Model}");

            Debug.WriteLine(generated_image.Style);


            Debug.WriteLine(generated_image.Created);

            Debug.WriteLine(generated_image.ImageSize.Height);
            Debug.WriteLine(generated_image.ImageSize.Width);
            mgdbs.CreateAsync("images", generated_image);
        }

        /*
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
            public string prompt { get; set; } = string.Empty;
            public string style { get; set; } = string.Empty; // Set by applcation
            public string model { get; set; } = string.Empty; // Set by applcation
            public DateTime created { get; set; } = DateTime.Now; // Set by applcation

        }

        private class Images
        {
            public string url { get; set; } = string.Empty;
            public int width { get; set; } = 0;
            public int height { get; set; } = 0;
            //public string content_type { get; set; } = string.Empty;

        } */


    }
    
}
