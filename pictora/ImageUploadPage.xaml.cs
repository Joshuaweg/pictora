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
using Microsoft.Maui.Devices.Sensors;

[QueryProperty(nameof(EditedImage), "file")]
public partial class ImageUploadPage : ContentPage
{
    Pictora.Models.Image generated_image;
    string edited_image; // Parameter, need to be converted to a url either by code or uploading to a private service.
	public ImageUploadPage(Result image, int idx = 0)
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
        generated_image.UserId = idx;                // Defined on login page
        generated_image.Upvotes = 0;
        generated_image.Downvotes = 0;
        generated_image.Description = "";            // Defined on save page
        generated_image.Name = "";                  // Defined on save page
        generated_image.NumericId = 0;             // Defined by the database

        Generated_Image.Source = generated_image.ImageUrl;

    }

    public string EditedImage
    {
        set
        {
            edited_image = value;
            OnUpdateImage(edited_image);
        }
    }

    private void OnUpdateImage(string value)
    {
        Generated_Image.Source = value;
        Debug.Print(generated_image.ImageUrl);
        Debug.Print(value);
    }

    private void ButtonEditClicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(new EditImagePage(generated_image));
    }
    private void ButtonUploadClicked(object sender, EventArgs e) {

        generated_image.Tags = getTags(Tags.Text);
        generated_image.Description = Description.Text;
        generated_image.Name = Title.Text;

        // If the image has been edited, use this url.
        if (edited_image != null)
        {
            // generated_image.ImageUrl = edited_image
            Debug.Print("I need to convert the generated file to a url.");
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await DisplayAlert("Notice", "I still need to convert the edited file to a url. \n\n Nothing has been uploaded", "Ok");
            });
            return;
        }

        Debug.Print("Upload should be fine if it gets to this point.");

        // * Set up a NumericID
        // Go through the image database, and find the next unsued NumericId.

        // Do not uncoment this until both of the unset fields have been set properly.
       
        MongoDBService mgdbs = new();
        mgdbs.CreateAsync("images", generated_image);
        

        // Display a prompt, to confirm the image has been uploaded sucessfully
        // Switch view back to home page, because otherwise the user may spam the database with duplcates
    }

    private List<string> getTags(string text)
    {

        if (text == null) return [];
        List<string> tags = text.Split(' ').ToList();
        return tags;
    }

    // Move this and "ImageSize" into UploadPage when page is finished.

}