namespace Pictora
{
    public partial class MainPage : ContentPage
    {
        int count = 0;

        public MainPage()
        {
            InitializeComponent();

        }
        //Left below code alone; not sure if okay to delete
        //private void OnCounterClicked(object sender, EventArgs e)
        //{
        //    count++;

        //    if (count == 1)
        //        CounterBtn.Text = $"Clicked {count} time";
        //    else
        //        CounterBtn.Text = $"Clicked {count} times";

        //    SemanticScreenReader.Announce(CounterBtn.Text);
        //}
        //private void OnCounterDouble(object sender, EventArgs e){
        //    count *=2;
        //    if (count == 1)
        //        CounterBtn.Text = $"Clicked {count} time";
        //    else
        //        CounterBtn.Text = $"Clicked {count} times";

        //    SemanticScreenReader.Announce(CounterBtn.Text);
        //}
        private async void OnLoginClicked(object sender, EventArgs e)
        {
            // Await the navigation to LoginPage to keep the UI responsive.
            // This prevents blocking the main thread, allowing a smoother user experience.
            await Navigation.PushAsync(new LoginPage());
        }
        private async void OnSignUpClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new SignUpPage());
        }
        private void OnImageEditClicked(object sender, EventArgs e)
        {
            Navigation.PushAsync(new EditImagePage());
        }
        private void OnImageGeneratedClicked(object sender, EventArgs e)
        {
            Navigation.PushAsync(new ImageGeneratePage());
        }

    }

}
