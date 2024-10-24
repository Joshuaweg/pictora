using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DotNetEnv;

namespace Pictora
{
    public partial class ImageGeneratePage : ContentPage
    {
        private string API_KEY = "";
        public ImageGeneratePage()
        {
            InitializeComponent();
            DotNetEnv.Env.Load();
            API_KEY = Env.GetString("FAL_API_KEY","TEST");

            // There is a debug label at the top of the page.
            // Debug_Label.Text = File.Exists(".env") ? API_KEY : "Cannot find the .env file";

            // This is just for testing to see if the app is even able to find files at all. It should find the file and return true.
            Debug_Label.Text = System.IO.Directory.GetCurrentDirectory();
            //File.Exists("TextFile1.txt").ToString();

            //File.Create("test.txt");
            //
        }
    }
    
}
