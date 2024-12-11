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
using Pictora.Models;
using Pictora.services;


namespace Pictora
{
    public partial class ImageGeneratePage : ContentPage
    {
        private string API_KEY = "";
        private string GeneratedURL = "";
        int count = 0;
        private int idx;
        Result result_JSON = new();

        // TEMP: just in case you want to fill the database.
        Pictora.Models.Image generated_image = new(); 
        MongoDBService mgdbs = new();

        //private ArrayList envFile = new ArrayList();
        public ImageGeneratePage(int idx=0)
        {
            InitializeComponent();
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string envPath = Path.Combine(baseDirectory, ".env");

            DotNetEnv.Env.Load(envPath);
            API_KEY = DotNetEnv.Env.GetString("FAL_API_KEY", "");

            mgdbs = new(); // TEMP
            this.idx = idx;
        }

        private void Test(object sender, EventArgs e)
        {
            Debug.WriteLine(sender);
        }
        private void ButtonShareClicked(object sender, EventArgs e)
        {
            Task.Run(async () => await ShareFile());
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

        private Task DownloadFile()
        {

            // If there is a way to do this with the HttpClient class (or anything else), so the complier doesn't send me a obsolite message, implement it that way.
            WebClient webClient = new();

            // This should just lead to the systems user folder (The one with your name on it.) If there is a better directory to use, please let me know ASAP.
            //
            // Count is temperary, replace with a date and timestamp for the filename
            string location = Environment.GetFolderPath(Environment.SpecialFolder.Personal).ToString() + $"\\test{count}.jpeg";
            webClient.DownloadFile(GeneratedURL, location);

            count++;

            // This isn't the proper way to implement this, but it should work for now.
            // Look into what the actual way to do this is, assuming I'm actually alowed to.
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await DisplayAlert("Prompt", $"File downloaded to {location}", "Ok");
            });
            return Task.CompletedTask;
        }

        private async void GenerateButtonClicked(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(API_KEY))
                {
                    await DisplayAlert("Error", "API key is not set", "OK");
                    return;
                }

                string prompt = Prompt.Text;
                if (string.IsNullOrEmpty(prompt))
                {
                    await DisplayAlert("Error", "Please enter a prompt", "OK");
                    return;
                }

                // Disable generate button and show loading indicator
                GenerateButton.IsEnabled = false;
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsRunning = true;

                // A safety check if the user uses the custom size option.
                if (Size.SelectedIndex == 7)
                {
                    try
                    {
                        var width = Int32.Parse(CustomWidth.Text);
                        var height = Int32.Parse(CustomHeight.Text);
                    }
                    catch
                    {
                        await DisplayAlert("Error", "Please enter valid width and height values", "OK");
                        return;
                    }
                }

                // Create the prompt object
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

                string finalPrompt = JsonSerializer.Serialize<Prompt>(inputPrompt);
                string requestUrl = "https://queue.fal.run/fal-ai/fast-sdxl";

                using (HttpClient httpClient = new HttpClient())
                {
                    // Initial request
                    var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, requestUrl);
                    httpRequestMessage.Content = new StringContent(finalPrompt, Encoding.UTF8, "application/json");
                    httpRequestMessage.Headers.Add("Authorization", $"Key {API_KEY}");

                    var response = await httpClient.SendAsync(httpRequestMessage);
                    var result = await response.Content.ReadAsStringAsync();
                    var progress_JSON = JsonSerializer.Deserialize<Progress>(result);

                    // Check queue status
                    while ((progress_JSON!.status).Equals("IN_QUEUE"))
                    {
                        await Task.Delay(1000);

                        httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, progress_JSON.status_url);
                        httpRequestMessage.Headers.Add("Authorization", $"Key {API_KEY}");

                        response = await httpClient.SendAsync(httpRequestMessage);
                        result = await response.Content.ReadAsStringAsync();
                        progress_JSON = JsonSerializer.Deserialize<Progress>(result);
                    }

                    // Get final result
                    requestUrl = progress_JSON.response_url;
                    

                    do
                    {
                        await Task.Delay(1000);

                        httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                        httpRequestMessage.Headers.Add("Authorization", $"Key {API_KEY}");

                        response = await httpClient.SendAsync(httpRequestMessage);
                        result = await response.Content.ReadAsStringAsync();
                        result_JSON = JsonSerializer.Deserialize<Result>(result)!;

                        if (result_JSON.detail == "Internal Server Error")
                        {
                            await DisplayAlert("Error", "An error occurred while generating the image", "OK");
                            return;
                        }
                    } while (result_JSON.detail != "");

                    // Set the result properties
                    result_JSON.model = "Fast-SDXL";
                    result_JSON.style = "None";

                    GeneratedURL = result_JSON.images[0].url;

                    // Update UI on main thread
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        Generated_Image.Source = GeneratedURL;
                        GenerateButton.Text = "Regenerate";
                        SaveControls.IsVisible = true;
                        DevUpload.IsVisible = true;
                    });

                    // Create image object
                    generated_image = new Pictora.Models.Image
                    {
                        ImageUrl = GeneratedURL,
                        ImageSize = new ImageSize
                        {
                            Height = result_JSON.images[0].height,
                            Width = result_JSON.images[0].width
                        },
                        Created = result_JSON.created,
                        Prompt = result_JSON.prompt,
                        Model = result_JSON.model,
                        Style = result_JSON.style,
                        Tags = new List<string>(),
                        UserId = idx,
                        Upvotes = 0,
                        Downvotes = 0,
                        Description = "Generated image",
                        Name = "Generated Image",
                        NumericId = 0
                    };
                    Debug.WriteLine("User Id: " + generated_image.UserId.ToString());
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
            finally
            {
                // Always hide loading indicator and enable button
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    LoadingIndicator.IsRunning = false;
                    LoadingIndicator.IsVisible = false;
                    GenerateButton.IsEnabled = true;
                });
            }
        }

        private void ButtonUploadClicked(object sender, EventArgs e)
        {
            //string json = JsonSerializer.Serialize<Result>(result_JSON);
            //Navigation.PushAsync(new ImageUploadPage(json));

            Debug.WriteLine(result_JSON);
            Navigation.PushAsync(new ImageUploadPage(result_JSON,idx));
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
            _ = mgdbs.CreateAsync("images", generated_image);
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
