using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetEnv;

namespace Pictora
{
    public partial class ImageGeneratePage : ContentPage
    {
        string API_KEY = "";
        public ImageGeneratePage()
        { 
            InitializeComponent();
            API_KEY = Env.GetString("FAL_API_KEY");


        }
    }
    
}
