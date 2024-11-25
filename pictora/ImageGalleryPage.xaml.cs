namespace Pictora;

using Pictora.Services;
using Pictora.Models;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.Diagnostics;
using System;

public partial class ImageGalleryPage : ContentPage
{

    MongoDBService mgdbs = new();

    public ImageGalleryPage()
	{
		InitializeComponent();
        mgdbs = new();

        // Async is the most incovenient thing, thanks for making me have to learn lambda.
        var collect = Task.Run(async () => await mgdbs.GetAllAsync<Image>("images")).Result;
        
        Debug.WriteLine("DEBUG " + collect);

        var urls = new List<String>();
        foreach (var i in collect)
        {
            urls.Add(i.ImageUrl.ToString());
            Debug.WriteLine(i.ImageUrl);
        }

        
        // Check to see how many urls there are, so I know how many images I need to set up.
        Debug.WriteLine("URL COUNT " + urls.Count);

        Generated_Image_1.Source = urls[1];
        Generated_Image_2.Source = urls[2];
        Generated_Image_3.Source = urls[3];

        // There is only 3 images, so hide the rest.
        // The list actually had four, but the first one is blank.
        // If you are designing a UI, disable this block to see the rows.
        Generated_Image_4.IsVisible = false;
        Generated_Image_5.IsVisible = false;
        Generated_Image_5.IsVisible = false;
        Generated_Image_6.IsVisible = false;
        Generated_Image_7.IsVisible = false;
        Generated_Image_8.IsVisible = false;
        Generated_Image_9.IsVisible = false;

    }

}