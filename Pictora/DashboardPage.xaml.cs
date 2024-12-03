using System.Diagnostics;

namespace Pictora
{
    public partial class DashboardPage : ContentPage
    {
        int idx;
        public DashboardPage(int idx=0)
        {
            InitializeComponent();
            this.idx = idx;
            Debug.WriteLine("User ID: " + idx.ToString());
        }

        private async void OnButtonPressed(object sender, EventArgs e)
        {
            var button = (Button)sender;
            await button.ScaleTo(0.95, 50);
        }

        private async void OnButtonReleased(object sender, EventArgs e)
        {
            var button = (Button)sender;
            await button.ScaleTo(1, 50);
        }

        private void OnImageEditClicked(object sender, EventArgs e)
        {
            Navigation.PushAsync(new EditImagePage(null,idx));
        }

        private void OnImageGeneratedClicked(object sender, EventArgs e)
        {
            Navigation.PushAsync(new ImageGeneratePage(idx));
        }

        private void OnImageGalleryClicked(object sender, EventArgs e)
        {
            Navigation.PushAsync(new ImageGalleryPage(idx));
        }
    }
}