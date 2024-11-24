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

        // Why is the only way of getting info out of a database an async methoid?
        // And why is there no documentation on how this methoid works?

        // mgdbs.GetAllAsync<Images>("images")

        // var collect = Task.Run(async () => await mgdbs.GetAllAsync<Image>("images")).Result;
        
        // Debug.WriteLine("DEBUG " + collect);

        // I'm uncertain if the following code would've worked, but I can't get an async methoid to finish in the codes currernt area,
        // and using 'wait()' basicly freezes the applcation. This is just so I can tell Joshua that I know what I have to do to get images
        // in theory, but not in practice. (Why did you have to write the 'Get' functions in a async way?)

        /*
        var urls = new List<String>();
        foreach (var i in collect)
        {
            urls.Add(i.BaseImage);
        }

        
        // Check to see how many urls there are
        Debug.WriteLine("URL COUNT ", urls.Count);

        // First three for now, add the rest when I'm certain about the image count.
        Generated_Image_1.Source = urls[0];
        Generated_Image_2.Source = urls[1];
        Generated_Image_3.Source = urls[2];

         */

    }

}