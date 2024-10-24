using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using DotNetEnv;


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

            prompt = ("\"prompt\": " + prompt);

            String size = "\"image_size\": ";

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
                    size = "";
                    break;     
            }

            // TODO - Add the options for handling models and loras

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await DisplayAlert("Prompt", $"{prompt}, \n {size}", "Ok");
            });

            string finalPrompt = $"{{{prompt}";

            if (!size.Equals(""))
            {
                // Add the size
            }

            // End
            finalPrompt += $"}}";

            string requestUrl = "https://queue.fal.run/fal-ai/fast-sdxl";

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await DisplayAlert("Prompt", $"{finalPrompt}", "Ok");
            });

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            httpRequestMessage.Content = new StringContent(finalPrompt, Encoding.UTF8, "application/json");
            httpRequestMessage.Headers.Add("Authorization", $"Key {API_KEY}");
            
            var httpClient = new HttpClient();
            var responce = httpClient.Send(httpRequestMessage);
            var body = responce.Content.ReadAsStream();

            StreamReader reader = new StreamReader(body);
            var result = reader.ReadToEnd();

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await DisplayAlert("Prompt", result, "Ok");
            });
            
            // Have a while loop that rechecks the status every second until it is complete.

            //httpClient.BaseAddress = new Uri("http://localhost:8080/api/v1");
            //await client.PostAsync(requestUrl, httpRequest.Content);

        }



    }
    
}
