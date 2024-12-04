using Microsoft.Maui.Controls;
using Pictora.Services;
using System.Security.Cryptography;
using System.Text;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace Pictora
{
    public partial class SignUpPage : ContentPage
    {
        MongoDBService mgdbs;
        public SignUpPage()
        {
            InitializeComponent();
            mgdbs = new MongoDBService();
        }
        // Replace with actual authentication later
        private async void OnSignUpClicked(object sender, EventArgs e)
        {
            var Users = await mgdbs.GetAllAsync<User>("users");
            var Usernames = new List<string>();
            var ids = new List<int>();
            var emails = new List<string>();
            foreach (var user in Users)
            {
                Usernames.Add(user.Username);
                emails.Add(user.Email);
            }
            foreach (var user in Users)
            {
                ids.Add(user.NumericId);
            }
            int maxId = ids.Max();
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
            if (Usernames.Contains(UsernameEntr.Text))
            {
                MessageLabell.Text = "Username already exists.";
                return;
            }
            if (emails.Contains(EmailEntr.Text)) {
                MessageLabell.Text = "Email already exists. ";
                return;
            }
            int nextId = maxId + 1;
            // create hash password using sha256
            string hashedPassword = "";
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(PasswordEntr.Text));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                hashedPassword = builder.ToString();
            }
            User newUser = new User();
            newUser.NumericId = nextId;
            newUser.Email = EmailEntr.Text;
            newUser.Username = UsernameEntr.Text;
            newUser.Password = hashedPassword;
            await mgdbs.CreateAsync("users", newUser);


            //shows a success message.
            MessageLabell.TextColor = Colors.Green;
            MessageLabell.Text = "Sign-up successful!";
            
            // Navigate back to login page after sign-up
            await Navigation.PushAsync(new LoginPage());
        }

        private async void OnButtonPressed(object sender, EventArgs e)
        {
            var button = (Button)sender;
            await button.ScaleTo(0.95, 50); // Slightly shrink the button
        }

        private async void OnButtonReleased(object sender, EventArgs e)
        {
            var button = (Button)sender;
            await button.ScaleTo(1, 50); // Restore to original size
        }
        public class User
        {
            [BsonId]
            [BsonRepresentation(BsonType.ObjectId)]
            public string Id { get; set; }    // Changed from int to string to match MongoDB's ObjectId

            [BsonElement("id")]               // Maps to the numeric 'id' field
            public int NumericId { get; set; }
            [BsonElement("username")]
            public string Username { get; set; }
            [BsonElement("email")]
            public string Email { get; set; }

            [BsonElement("password")]         // Ensure case matching with MongoDB field
            public string Password { get; set; }
        }
    }
}
