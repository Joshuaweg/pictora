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

		// Filling out the fields for each image.


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
}