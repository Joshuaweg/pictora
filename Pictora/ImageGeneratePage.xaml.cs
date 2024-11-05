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


namespace Pictora
{
    public partial class ImageGeneratePage : ContentPage
    {
        private string API_KEY = "";
        //private ArrayList envFile = new ArrayList();
        public ImageGeneratePage()
        {
            InitializeComponent();
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string envPath = Path.Combine(baseDirectory, ".env");

            DotNetEnv.Env.Load(envPath);
            API_KEY = DotNetEnv.Env.GetString("FAL_API_KEY", "");

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
            Debug.WriteLine(progress_JSON.status);
            // Have a while loop that rechecks the status every second until it is complete.
            while (!progress_JSON.status.Equals("COMPLETED"))
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
                Debug.WriteLine(progress_JSON.status);
            }


            // Check to see what the result was after exiting the while loop.
            
            // This doesn't seem to match what was on the page in the "Get the Result" section.
            // It currently looks like this:
            // {
            //   "detail": [{
            //     "type":"json_invalid",
            //     "loc":["body",11],
            //     "msg":"JSON decode error"m
            //     "input"={},
            //     "ctx":{
            //       "error":"Expecting value"
            //     }
            //   }]
            // }

            
            requestUrl = progress_JSON.response_url;

                httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                httpRequestMessage.Headers.Add("Authorization", $"Key {API_KEY}");

            responce = httpClient.Send(httpRequestMessage);
            body = responce.Content.ReadAsStream();

            reader = new StreamReader(body);
            result = reader.ReadToEnd();
            Debug.WriteLine(result);
            var result_JSON = JsonSerializer.Deserialize<Result>(result);

            Debug.WriteLine(result_JSON);
            Debug.WriteLine(result_JSON.images[0].url);
            string generatedURL = result_JSON.images[0].url;

            Generated_Image.Source = generatedURL;
            
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


    }
    
}
