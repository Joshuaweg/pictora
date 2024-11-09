using Microsoft.Maui.Controls;

namespace Pictora
{
    public partial class SignUpPage : ContentPage
    {
        public SignUpPage()
        {
            InitializeComponent();
        }
        // Replace with actual authentication later
        private async void OnSignUpClicked(object sender, EventArgs e)
        {
            // Simple validation check for empty fields
            if (string.IsNullOrWhiteSpace(EmailEntr.Text) ||
                string.IsNullOrWhiteSpace(UsernameEntr.Text) ||
                string.IsNullOrWhiteSpace(PasswordEntr.Text) ||
                string.IsNullOrWhiteSpace(ConfirmPasswordEntr.Text))
            {
                MessageLabell.Text = "Please fill in all fields.";
                return;
            }

            // Check if passwords match
            if (PasswordEntr.Text != ConfirmPasswordEntr.Text)
            {
                MessageLabell.Text = "Passwords do not match.";
                return;
            }

            //shows a success message.
            MessageLabell.TextColor = Colors.Green;
            MessageLabell.Text = "Sign-up successful!";
            
            // Navigate back to login page after sign-up
            await Navigation.PopAsync();
        }
    }
}
