namespace Pictora;

using Pictora.Services;
using Pictora.Models;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Diagnostics;

public partial class DetailedImagePage : ContentPage
{
	int idx;
	MongoDBService _mgdbs = new();
	Image image;

	// [!] I don't know who did whatever happend to the Image Gallery, but DON'T DO THAT FOR THIS PAGE!
    public DetailedImagePage(ImageSource ImageUrl, int idx = 0)
	{
		InitializeComponent();
		this.idx = idx;
        Generated_Image.Source = ImageUrl;

        image = FindImage(ImageUrl);

		if (image == null)
		{
			// DON'T EVEN THINK ABOUT IT! I KNOW YOU WANT TO, BUT DON'T!
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await DisplayAlert("Error", $"Could not find info for this image", "OK");
				await Shell.Current.GoToAsync("..");
			});
        }

        Debug.WriteLine(image);
        Debug.WriteLine(idx);

        // Edit tools become visible for uploader or admin.
        UploaderTools.IsVisible = (image.UserId == idx) || (idx == 0);

		Debug.WriteLine(image.Tags.Count);

		if (image.Tags.Count > 0)
		{
			TagGroup.IsVisible = true;

			foreach (var item in image.Tags)
			{
				Label label = new()
                {
					Text = "#" + item,
					FontSize = 18,
					TextColor = Color.FromRgb(0, 0, 255), // Blue
					TextDecorations = TextDecorations.Underline
				};
				TagGroup.Children.Add(label);
            }

        }

		LikeCount.Text = (image.Upvotes - image.Downvotes).ToString();
		Title.Text = image.Name;
		Uploader.Text = "Uploader: " + GetUploader(image.UserId);

		ImageID.Text = image.ImageUrl;
		Description.Text = "Description:\n\n" + image.Description;
		Date.Text = "Date Uploaded: " + image.Created;
		Model.Text = "Model Used: " + image.Model;
		Style.Text = "Style Used: " + image.Style;
		Prompt.Text = "Prompt: " + image.Prompt;
		Size.Text = "Image Size: " + image.ImageSize.Width.ToString() + "×" + image.ImageSize.Height.ToString();


    }

    private string GetUploader(int UserID)
    {
		List<User> userList = Task.Run(async () => await _mgdbs.GetAllAsync<User>("users")).Result;

		foreach (User u in userList)
		{
			if (UserID == u.NumericId)
				return u.Username;
		}

		return "Anonymous";
    }

    // Get all of the images and find the correct one by url.
    private Image? FindImage(ImageSource imageUrl)
    {

		// Again, Lambda to enforce syncronus behavoir.
		List<Image> imageList = Task.Run( async () => await _mgdbs.GetAllAsync<Image>("images")).Result;

		foreach (Image image in imageList)
		{			
			// Substring of the imageUrl is needed to remove the "Url: " at the start.
			if (image.ImageUrl == imageUrl.ToString()[5..])
				return image;

        }

		// This happens if the image is not found in the database.
		return null;
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

        [BsonElement("email")]
        public string Email { get; set; }

    }
}