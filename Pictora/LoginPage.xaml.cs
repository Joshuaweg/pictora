using Microsoft.Maui.Controls;
using Pictora.Services;
using System.Security.Cryptography;
using System.Text;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;


namespace Pictora
{
    public partial class LoginPage : ContentPage
    {
        MongoDBService mgdbs;
        public LoginPage()
        {
            InitializeComponent();
            mgdbs = new MongoDBService();
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
<<<<<<< HEAD
            if (UsernameEntry.Text == "admin" && PasswordEntry.Text == "password") // Replace with actual authentication later
=======
            // Add your authentication logic here
            var users = await mgdbs.GetAllAsync<User>("users");
            var usernames = new List<string>();
            var passwords = new List<string>();
            foreach (var user in users)
>>>>>>> fe091be79f0499cb892a7c68cf2dc123aae3ff14
            {
                usernames.Add(user.Username);
                passwords.Add(user.Password);
            }

            if (usernames.Contains(UsernameEntry.Text)) // Replace with actual authentication
            {
                int idx =usernames.IndexOf(UsernameEntry.Text);
                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(PasswordEntry.Text));
                   StringBuilder builder = new StringBuilder();
                    for (int i = 0; i < bytes.Length; i++)
                    {
                        builder.Append(bytes[i].ToString("x2"));
                    }
                    string password = builder.ToString();
                    if (password == passwords[idx])
                    {
                        await Navigation.PushAsync(new MainPage());
                    }
                    else
                    {
                        MessageLabel.Text = "Invalid username or password";
                    }
                }
            }
            else
            {
                MessageLabel.Text = "Invalid username or password";
            }
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

            [BsonElement("password")]         // Ensure case matching with MongoDB field
            public string Password { get; set; }
        }
    }
}