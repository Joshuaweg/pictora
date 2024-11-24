namespace Pictora;

using Pictora.Services;
using Pictora.Models;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.Diagnostics;

public partial class ImageGalleryPage : ContentPage
{

    MongoDBService mgdbs = new();

    public ImageGalleryPage()
	{
		InitializeComponent();
        mgdbs = new();

        //var collect = mgdbs.GetAllAsync<Images>("images");

        //Debug.WriteLine(collect);
        
        
    }
}