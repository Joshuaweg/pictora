namespace Pictora;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http;
using DotNetEnv;
using System.Diagnostics;
using System.Net;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using Pictora.Services;
using Pictora.Models;

public partial class ImageUploadPage : ContentPage
{
    Pictora.Models.Image generated_image;
	public ImageUploadPage(Result image)
	{
        InitializeComponent();

        generated_image = new Image();
        generated_image.ImageSize = new ImageSize();
        generated_image.ImageUrl = image.images[0].url;
        generated_image.ImageSize.Height = image.images[0].height; // 1024
        generated_image.ImageSize.Width = image.images[0].width; // 1024
        generated_image.Created = image.created;
        generated_image.Prompt = image.prompt;
        generated_image.Model = image.model;
        generated_image.Style = image.style;
        generated_image.Tags = new List<string>();  // Defined on save page
        generated_image.UserId = 0;                // Defined on login page
        generated_image.Upvotes = 0;
        generated_image.Downvotes = 0;
        generated_image.Description = "";            // Defined on save page
        generated_image.Name = "";                  // Defined on save page
        generated_image.NumericId = 0;             // Defined by the database

        Generated_Image.Source = generated_image.ImageUrl;

    }

    private void ButtonEditClicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(new EditImagePage(generated_image));
    }
    private void ButtonUploadClicked(object sender, EventArgs e) {

        generated_image.Tags = getTags(Tags.Text);
        generated_image.Description = Description.Text;
        generated_image.Name = Title.Text;

        // Figure out how to get the UserID
        // Set up a NumericID

        /*
        MongoDBService mgdbs = new();
        mgdbs.CreateAsync("images", generated_image);
        */

        // Display a prompt, to confirm the image has been uploaded sucessfully
        // Switch view back to home page, because otherwise the user may spam the database with duplcates
    }

    private List<string> getTags(string text)
    {
        List<string> tags = text.Split(' ').ToList();
        return tags;
    }

    // Move this and "ImageSize" into UploadPage when page is finished.

}