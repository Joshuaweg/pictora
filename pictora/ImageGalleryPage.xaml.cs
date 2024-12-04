namespace Pictora;

using Pictora.Services;
using Pictora.Models;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.Diagnostics;
using System;
using System.ComponentModel;

public partial class ImageGalleryPage : ContentPage
{
    private readonly MongoDBService _mgdbs = new();
    private List<Image> _images = new();
    private readonly Random _random = new Random();
    private int idx;

    public ImageGalleryPage(int idx = 0)
    {
        InitializeComponent();
        LoadImages();
        this.idx = idx;
    }

    private async void LoadImages()
    {
        try
        {
            _images = await GetGalleryImages();

            // Create a list to hold our arranged images
            var arrangedImages = _images.Select((img, index) => new
            {
                Image = img,
                Size = GetImageSize(index)
            }).ToList();

            foreach (var item in arrangedImages)
            {
                try
                {
                    if (string.IsNullOrEmpty(item.Image.ImageUrl))
                        continue;

                    var frame = CreateImageFrame(item.Image, item.Size);
                    MosaicGallery.Children.Add(frame);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error loading image: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading gallery: {ex.Message}");
            await DisplayAlert("Error", "Failed to load image gallery", "OK");
        }
    }

    private (double width, double height) GetImageSize(int index)
    {
        // Create varying sizes for the mosaic effect
        double baseWidth = 140;
        double baseHeight = 140;

        // Every third image is larger
        int pattern = index % 8;
        return pattern switch
        {
            0 => (baseWidth * 2, baseHeight * 2),  // Large square
            1 => (baseWidth, baseHeight * 2),      // Tall
            2 => (baseWidth * 2, baseHeight),      // Wide
            3 => (baseWidth * 1.5, baseHeight),    // Medium wide
            4 => (baseWidth, baseHeight * 1.5),    // Medium tall
            _ => (baseWidth, baseHeight)           // Standard
        };
    }

    private Frame CreateImageFrame(Image imageData, (double width, double height) size)
    {
        var frame = new Frame
        {
            Padding = 0,
            CornerRadius = 4,
            IsClippedToBounds = true,
            BorderColor = Colors.Transparent,
            BackgroundColor = Colors.Transparent,
            Margin = 2,
            WidthRequest = size.width,
            HeightRequest = size.height
        };

        var image = new Microsoft.Maui.Controls.Image
        {
            Source = ImageSource.FromUri(new Uri(imageData.ImageUrl)),
            Aspect = Aspect.AspectFill,
            WidthRequest = size.width,
            HeightRequest = size.height
        };

        // Add shadow effect
        frame.Shadow = new Shadow
        {
            Brush = Colors.Black,
            Offset = new Point(0, 1),
            Opacity = 0.2f,
            Radius = 2
        };

        // Add loading indicator
        var loadingIndicator = new ActivityIndicator
        {
            IsRunning = true,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Color = Colors.White
        };

        var overlay = new Grid
        {
            BackgroundColor = Colors.Black.WithAlpha(0.3f),
            IsVisible = false
        };

        var details = new Label
        {
            Text = imageData.Prompt,
            TextColor = Colors.White,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Margin = new Thickness(10),
            LineBreakMode = LineBreakMode.WordWrap
        };

        overlay.Children.Add(details);

        var content = new Grid();
        content.Children.Add(image);
        content.Children.Add(loadingIndicator);
        content.Children.Add(overlay);

        frame.Content = content;

        // Handle image loading
        image.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(Microsoft.Maui.Controls.Image.IsLoading))
            {
                loadingIndicator.IsVisible = image.IsLoading;
            }
        };

        // Add hover/tap effect
        var hoverGesture = new PointerGestureRecognizer();


        hoverGesture.PointerEntered += (s, e) => { overlay.IsVisible = true; };
        hoverGesture.PointerExited += (s, e) => { overlay.IsVisible = false; };
        hoverGesture.PointerPressed += (s, e) =>
        {
            Debug.WriteLine(image.Source);
            Debug.WriteLine(idx);
            Navigation.PushAsync(new DetailedImagePage(image.Source, idx));
        };

        frame.GestureRecognizers.Add(hoverGesture);

        return frame;
    }

    private async Task<List<Image>> GetGalleryImages()
    {
        try
        {
            return await _mgdbs.GetAllAsync<Image>("images");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error fetching images from MongoDB: {ex.Message}");
            throw;
        }
    }
}