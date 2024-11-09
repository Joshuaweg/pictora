using Microsoft.Maui.Controls;

namespace Pictora
{
    public partial class LoginPage : ContentPage
    {
        public LoginPage()
        {
            InitializeComponent();
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            if (UsernameEntry.Text == "admin" && PasswordEntry.Text == "password") // Replace with actual authentication later
            {
                MessageLabel.Text = "";
                await Navigation.PushAsync(new MainPage());
            }
            else
            {
                MessageLabel.Text = "Invalid username or password";
            }
        }
    }
}