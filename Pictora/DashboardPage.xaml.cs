namespace Pictora
{
    public partial class DashboardPage : ContentPage
    {
        public DashboardPage()
        {
            InitializeComponent();
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
            Navigation.PushAsync(new EditImagePage());
        }

        private void OnImageGeneratedClicked(object sender, EventArgs e)
        {
            Navigation.PushAsync(new ImageGeneratePage());
        }

        private void OnImageGalleryClicked(object sender, EventArgs e)
        {
            Navigation.PushAsync(new ImageGalleryPage());
        }
    }
}