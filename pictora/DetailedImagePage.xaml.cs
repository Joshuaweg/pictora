namespace Pictora;

using Pictora.Services;
using Pictora.Models;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
public partial class DetailedImagePage : ContentPage
{
	private int idx;
	private MongoDBService _mgdbs = new();

	// [!] I don't know who did whatever happend to the Image Gallery, but DON'T DO THAT FOR THIS PAGE!
    public DetailedImagePage(ImageSource ImageUrl, int idx = 0)
	{
		InitializeComponent();
		this.idx = idx;
        Generated_Image.Source = ImageUrl;

		// Get all of the images and find the correct one by url.

		// _mgdbs.GetByIdAsync<Image>();

    }
}