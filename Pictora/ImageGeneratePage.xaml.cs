using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pictora
{
    public partial class ImageGeneratePage : ContentPage
    {
        public ImageGeneratePage()
        {

            InitializeComponent();

            var sizeList = new List<String>();
            sizeList.Add("Test");
            Picker picker = new Picker { Title = "Select a size" };
            picker.ItemsSource = sizeList;

            

        }
    }
    
}
