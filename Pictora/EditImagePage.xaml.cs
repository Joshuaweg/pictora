using Pictora.Services;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp;
using System.Diagnostics;
using PointFt = Microsoft.Maui.Graphics.PointF;
using ColorM = Microsoft.Maui.Graphics.Color;
using Pictora.Models;



namespace Pictora
{
    public partial class EditImagePage : ContentPage
    {
        private readonly ImageEditingService _imageService;
        private readonly string _editImagesDirectory;
        private string _currentImagePath;
        private bool _isCaptionMode = false;
        private List<DraggableCaption> _captions = new List<DraggableCaption>();
        private bool _isInpaintingMode = false;
        private List<PointFt> _currentPath = new List<PointFt>();
        private List<List<PointFt>> _paths = new List<List<PointFt>>();
        private IDrawable _maskDrawable;

        private class MaskDrawable : IDrawable
        {
            private readonly List<List<PointFt>> _paths;
            private float _strokeWidth = 20f;

            public MaskDrawable(List<List<PointFt>> paths)
            {
                _paths = paths;
            }

            public void Draw(ICanvas canvas, RectF dirtyRect)
            {
                canvas.StrokeColor = Colors.White;
                canvas.StrokeSize = _strokeWidth;
                canvas.StrokeLineCap = LineCap.Round;
                canvas.StrokeLineJoin = LineJoin.Round;

                foreach (var path in _paths)
                {
                    if (path.Count < 2) continue;

                    PathF pathF = new PathF();
                    pathF.MoveTo(path[0].X, path[0].Y);

                    for (int i = 1; i < path.Count; i++)
                    {
                        pathF.LineTo((float)path[i].X, (float)path[i].Y);
                    }

                    canvas.DrawPath(pathF);
                }
            }
        }
        // Move DraggableCaption class outside of constructor but keep it inside EditImagePage
        private class DraggableCaption : Grid
        {
            private double _originalX;
            private double _originalY;
            private double _totalX;
            private double _totalY;
            private readonly Label _captionLabel;
            private readonly Button _resizeHandle;
            private double _startWidth;
            private double _startHeight;
            private EditImagePage _parentPage;


            public DraggableCaption(string text, EditImagePage parentPage)
            {
                _parentPage = parentPage;
                MinimumWidthRequest = 50;
                MinimumHeightRequest = 20;
                WidthRequest = 200; // Default width
                HeightRequest = 40;  // Default height
                Padding = new Thickness(5);

                RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
                ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
                ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                Label _captionLabel = new Label
                {
                    Text = text,
                    TextColor = Colors.White,
                    FontSize = 18,
                    BackgroundColor = Colors.Black.WithAlpha(0.2f),
                    HorizontalOptions = LayoutOptions.Fill,
                    VerticalOptions = LayoutOptions.Fill,
                    LineBreakMode = LineBreakMode.WordWrap
                };

                _resizeHandle = new Button
                {
                    Text = "⋰",
                    FontSize = 14,
                    TextColor = Colors.White,
                    BackgroundColor = Colors.Transparent,
                    WidthRequest = 24,
                    HeightRequest = 24,
                    Padding = new Thickness(0),
                    Margin = new Thickness(0)
                };

                Add(_captionLabel);
                Add(_resizeHandle);

                // Enable drag gesture
                var panGesture = new PanGestureRecognizer();
                panGesture.PanUpdated += OnPanUpdated;
                GestureRecognizers.Add(panGesture);

                // Add tap gesture for editing/deleting
                var resizePanGesture = new PanGestureRecognizer();
                resizePanGesture.PanUpdated += OnResizePanUpdated;
                _resizeHandle.GestureRecognizers.Add(resizePanGesture);

                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += OnCaptionTapped;
                _captionLabel.GestureRecognizers.Add(tapGesture);

                // Add a double tap gesture for font size adjustment
                var doubleTapGesture = new TapGestureRecognizer { NumberOfTapsRequired = 2 };
                doubleTapGesture.Tapped += OnDoubleTapped;
                _captionLabel.GestureRecognizers.Add(doubleTapGesture);
            }

            private async void OnDoubleTapped(object sender, EventArgs e)
            {
                
                if (_parentPage == null) return;  // Exit if parent page is not found

                string action = await _parentPage.DisplayActionSheet(
                    "Adjust Font Size",
                    "Cancel",
                    null,
                    "Small (14)",
                    "Medium (18)",
                    "Large (24)",
                    "Extra Large (32)");

                if (string.IsNullOrEmpty(action) || action == "Cancel")
                    return;

                switch (action)
                {
                    case "Small (14)":
                        _captionLabel.FontSize = 14;
                        break;
                    case "Medium (18)":
                        _captionLabel.FontSize = 18;
                        break;
                    case "Large (24)":
                        _captionLabel.FontSize = 24;
                        break;
                    case "Extra Large (32)":
                        _captionLabel.FontSize = 32;
                        break;
                }
            }

            private void OnPanUpdated(object sender, PanUpdatedEventArgs e)
            {
                switch (e.StatusType)
                {
                    case GestureStatus.Started:
                        _originalX = TranslationX;
                        _originalY = TranslationY;
                        break;

                    case GestureStatus.Running:
                        TranslationX = _originalX + e.TotalX;
                        TranslationY = _originalY + e.TotalY;
                        _totalX = e.TotalX;
                        _totalY = e.TotalY;
                        break;

                    case GestureStatus.Completed:
                        _originalX = TranslationX;
                        _originalY = TranslationY;
                        break;
                }
            }

            private void OnResizePanUpdated(object sender, PanUpdatedEventArgs e)
            {
                switch (e.StatusType)
                {
                    case GestureStatus.Started:
                        _startWidth = WidthRequest;
                        _startHeight = HeightRequest;
                        break;

                    case GestureStatus.Running:
                        // Calculate new size
                        double newWidth = Math.Max(_startWidth + e.TotalX, MinimumWidthRequest);
                        double newHeight = Math.Max(_startHeight + e.TotalY, MinimumHeightRequest);

                        // Ensure we don't exceed image boundaries (assuming 1024x1024 max)
                        newWidth = Math.Min(newWidth, 1024);
                        newHeight = Math.Min(newHeight, 1024);

                        WidthRequest = newWidth;
                        HeightRequest = newHeight;
                        break;
                }
            }

            private async void OnCaptionTapped(object sender, EventArgs e)
            {
                if (_parentPage == null) return;
                
                    string action = await _parentPage.DisplayActionSheet(
                        "Caption Options",
                        "Cancel",
                        "Delete",
                        "Edit Text",
                        "Change Color");

                    switch (action)
                    {
                        case "Delete":
                            _parentPage.DeleteCaption(this);
                            break;
                        case "Edit Text":
                            await _parentPage.EditCaption(this);
                            break;
                        case "Change Color":
                            await ChangeTextColor();
                            break;
                    }
            }


            private async Task ChangeTextColor()
            {
                if (_parentPage == null) return;

                var action = await _parentPage.DisplayActionSheet(
                    "Select Text Color",
                    "Cancel",
                    null,
                    "White",
                    "Black",
                    "Red",
                    "Blue",
                    "Green",
                    "Yellow");

                ColorM newColor = action switch
                {
                    "White" => Colors.White,
                    "Black" => Colors.Black,
                    "Red" => Colors.Red,
                    "Blue" => Colors.Blue,
                    "Green" => Colors.Green,
                    "Yellow" => Colors.Yellow,
                    _ => _captionLabel.TextColor
                };

                _captionLabel.TextColor = newColor;
                // Adjust background color for better contrast
                _captionLabel.BackgroundColor = IsLightColor(newColor) ?
                    Colors.Black.WithAlpha(0.7f) :
                    Colors.White.WithAlpha(0.7f);
            }

            private bool IsLightColor(ColorM color)
            {
                return (color.Red * 0.299 + color.Green * 0.587 + color.Blue * 0.114) > 0.5;
            }

            public string GetText()
            {
                return (Children[0] as Label)?.Text ?? "";
            }

            public void SetText(string newText)
            {
                if (Children[0] is Label label)
                {
                    label.Text = newText;
                }
            }
        }

        public EditImagePage()
        {
            InitializeComponent();

            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string envPath = Path.Combine(baseDirectory, ".env");

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await DisplayAlert("Debug Info",
                    $"Looking for .env at: {envPath}\n" +
                    $"File exists: {File.Exists(envPath)}", "OK");
            });

            DotNetEnv.Env.Load(envPath);
            string apiKey = DotNetEnv.Env.GetString("FAL_API_KEY");
            _imageService = new ImageEditingService(apiKey);

            // Set up the edit images directory
            string appDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _editImagesDirectory = Path.Combine(appDirectory, "EditImages");
            string _editImage = Path.Combine(_editImagesDirectory, "test.png");

            // Debug the path being used
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await DisplayAlert("Path Info",
                    $"Using directory: {_editImagesDirectory}", "OK");
            });

            SetupImageDirectory();

            // Wire up button click handlers
            EditButton.Clicked += OnEditButtonClicked;
            SaveButton.Clicked += OnSaveButtonClicked;
            Filter1Button.Clicked += (s, e) => ApplyFilter("vintage style, sepia tones, classic photography");
            Filter2Button.Clicked += (s, e) => ApplyFilter("neon lights, cyberpunk style, vibrant colors");
            Filter3Button.Clicked += (s, e) => ApplyFilter("watercolor painting style, artistic, soft colors");
        }

        public EditImagePage(Pictora.Models.Image url)
        {
            InitializeComponent();

            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string envPath = Path.Combine(baseDirectory, ".env");

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await DisplayAlert("Debug Info",
                    $"Looking for .env at: {envPath}\n" +
                    $"File exists: {File.Exists(envPath)}", "OK");
            });

            DotNetEnv.Env.Load(envPath);
            string apiKey = DotNetEnv.Env.GetString("FAL_API_KEY");
            _imageService = new ImageEditingService(apiKey);

            // Set up the edit images directory
            //string appDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            //_editImagesDirectory = Path.Combine(appDirectory, "EditImages");
            string _editImage = url.ImageUrl;
            var uri = new Uri(_editImage);
            _currentImagePath = url.ImageUrl;

            //SetupImageDirectory();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                EditableImage.Source = ImageSource.FromUri(uri);
            });

            // Wire up button click handlers
            EditButton.Clicked += OnEditButtonClicked;
            SaveButton.Clicked += OnSaveButtonClicked_Upload;
            Filter1Button.Clicked += (s, e) => ApplyFilter("vintage style, sepia tones, classic photography");
            Filter2Button.Clicked += (s, e) => ApplyFilter("neon lights, cyberpunk style, vibrant colors");
            Filter3Button.Clicked += (s, e) => ApplyFilter("watercolor painting style, artistic, soft colors");
        }

        // ... (rest of your methods remain the same)
        private async void OnAddCaptionClicked(object sender, EventArgs e)
        {
            _isCaptionMode = !_isCaptionMode;
            CaptionEditorPanel.IsVisible = _isCaptionMode;
            CaptionOverlay.IsVisible = _isCaptionMode;
            PromptEditor.IsVisible = !_isCaptionMode;
            EditButton.IsVisible = !_isCaptionMode;

            if (_isCaptionMode)
            {
                AddCaptionButton.Text = "Exit Captions";
            }
            else
            {
                AddCaptionButton.Text = "Add Captions";
            }
        }

        private void OnAddTextButtonClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CaptionTextEntry.Text))
                return;

            var caption = new DraggableCaption(CaptionTextEntry.Text, this);
            _captions.Add(caption);
            CaptionOverlay.Children.Add(caption);
            CaptionTextEntry.Text = string.Empty;
        }

        private void OnDoneCaptioningClicked(object sender, EventArgs e)
        {
            _isCaptionMode = false;
            CaptionEditorPanel.IsVisible = false;
            CaptionOverlay.IsVisible = true; // Keep overlay visible but not interactive
            PromptEditor.IsVisible = true;
            EditButton.IsVisible = true;
            AddCaptionButton.Text = "Add Captions";
        }

        private void DeleteCaption(DraggableCaption caption)
        {
            _captions.Remove(caption);
            CaptionOverlay.Children.Remove(caption);
        }

        private async Task EditCaption(DraggableCaption caption)
        {
            string result = await DisplayPromptAsync(
                "Edit Caption",
                "Enter new text:",
                initialValue: caption.GetText());

            if (!string.IsNullOrEmpty(result))
            {
                caption.SetText(result);
            }
        }
        private void SetupImageDirectory()
        {
            try
            {
                // Create the EditImages directory if it doesn't exist
                if (!Directory.Exists(_editImagesDirectory))
                {
                    Directory.CreateDirectory(_editImagesDirectory);
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        await DisplayAlert("Info", $"Created directory at: {_editImagesDirectory}", "OK");
                    });
                }

                _currentImagePath = Path.Combine(_editImagesDirectory, "test.png");

                if (!File.Exists(_currentImagePath))
                {
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        await DisplayAlert("Info",
                            $"Please place test.png at:\n{_currentImagePath}", "OK");
                    });
                }
                else
                {
                    // Update the image source
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        EditableImage.Source = ImageSource.FromFile(_currentImagePath);
                    });
                }
            }
            catch (Exception ex)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await DisplayAlert("Error", $"Failed to setup image directory: {ex.Message}", "OK");
                });
            }
        }
        public static async Task<bool> CopyImageToEditDirectory(string sourceImagePath)
        {
            try
            {
                string editDirectory = Path.Combine(FileSystem.AppDataDirectory, "EditImages");
                string fileName = $"edit_{Path.GetFileName(sourceImagePath)}";
                string destinationPath = Path.Combine(editDirectory, fileName);

                // Create directory if it doesn't exist
                if (!Directory.Exists(editDirectory))
                {
                    Directory.CreateDirectory(editDirectory);
                }

                // Copy the file
                File.Copy(sourceImagePath, destinationPath, true);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task ProcessImageEdit(string prompt)
        {
            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsRunning = true;
                EditButton.IsEnabled = false;
                string _directory = "C:\\Users\\joshu\\AppData\\Local\\EditImages";
                string negativePrompt = "cartoon, illustration, animation, face, male, female";
                string fileName = "test.png";
                _currentImagePath = Path.Combine(_directory, fileName);
                var result = await _imageService.EditImageAsync(
                    _currentImagePath,
                    prompt,
                    negativePrompt
                );

                if (result.Images.Count > 0)
                {
                    var image = result.Images[0];

                    // Log the result details
                    Debug.WriteLine($"Generated image URL: {image.Url}");
                    Debug.WriteLine($"Image dimensions: {image.Width}x{image.Height}");
                    Debug.WriteLine($"Processing time: {result.Timings.Inference} seconds");

                    using var httpClient = new HttpClient();
                    byte[] imageData = await httpClient.GetByteArrayAsync(image.Url);

                    string tempImagePath = Path.Combine(_editImagesDirectory, "edited_image.jpg");
                    await File.WriteAllBytesAsync(tempImagePath, imageData);

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        EditableImage.Source = ImageSource.FromFile(tempImagePath);
                        // You might want to show some of these details in the UI
                        // For example, processing time or image dimensions
                    });
                    _currentImagePath = tempImagePath;
                }
                else
                {
                    await DisplayAlert("Error", "No image was generated", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to edit image: {ex.Message}", "OK");
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;
                EditButton.IsEnabled = true;
            }
        }

        private async void SetupInitialImage()
        {
            try
            {
                // Set up the paths
                string fileName = "test.png";
                _currentImagePath = Path.Combine(FileSystem.CacheDirectory, fileName);

                // If the file doesn't exist in cache, copy it from resources
                if (!File.Exists(_currentImagePath))
                {
                    using var stream = await FileSystem.OpenAppPackageFileAsync("Resources/Images/edit/test.png");
                    if (stream == null)
                    {
                        await DisplayAlert("Error", "Could not load initial image", "OK");
                        return;
                    }

                    using var fileStream = File.Create(_currentImagePath);
                    await stream.CopyToAsync(fileStream);
                }

                // Update the image source
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    EditableImage.Source = ImageSource.FromFile(_currentImagePath);
                });
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to setup initial image: {ex.Message}", "OK");
            }
        }

        private async void OnEditButtonClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(PromptEditor.Text))
            {
                await DisplayAlert("Error", "Please enter a prompt", "OK");
                return;
            }

            await ProcessImageEdit(PromptEditor.Text);
        }

        private async void ApplyFilter(string filterPrompt)
        {
            await ProcessImageEdit(filterPrompt);
        }
        private async Task SaveImageWithCaptions() // EDIT HERE OR SAVE FOR TRANSFER.
        {
            try
            {
                if (_currentImagePath == null)
                {
                    await DisplayAlert("Error", $"Cannot find the image.", "OK");
                    return;
                }
                bool wasLoadingVisible = LoadingIndicator.IsVisible;
                LoadingIndicator.IsVisible = false;

                IScreenshotResult screenshot;
                // Create a screenshot of the entire image container including captions
                if (Microsoft.Maui.Devices.DeviceInfo.Current.Platform == DevicePlatform.WinUI)
                {
                    screenshot = await ImageContainer.CaptureAsync();
                }
                else
                {
                    screenshot = await ImageContainer.CaptureAsync();

                }

                if (screenshot == null)
                {
                    await DisplayAlert("Error", "Failed to capture image", "OK");
                }

                string picturesFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                string fileName = $"pictora_captioned_{DateTime.Now:yyyyMMddHHmmss}.png";

                using (var stream = File.OpenWrite(Path.Combine(picturesFolder, fileName)))
                {
                    await screenshot.CopyToAsync(stream);
                }

                LoadingIndicator.IsVisible = wasLoadingVisible;

                await DisplayAlert("Success", "Image saved to Pictures folder", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to save image: {ex.Message}", "OK");
            }
        }


            private async void OnSaveButtonClicked(object sender, EventArgs e)
        {
            await SaveImageWithCaptions();
        }

        private async void OnSaveButtonClicked_Upload(object sender, EventArgs e)
        {
            await SaveImageWithCaptions();
            await Shell.Current.GoToAsync(".."); // Go back a page. (Doesn't send the image back with it though.)
        }

        private void InitializeInpainting()
        {
            var inpaintButton = this.FindByName<Button>("InpaintButton");
            var undoButton = this.FindByName<Button>("UndoButton");
            var clearButton = this.FindByName<Button>("ClearButton");
            var applyButton = this.FindByName<Button>("ApplyButton");
            var maskCanvas = this.FindByName<GraphicsView>("MaskCanvas");

            inpaintButton.Clicked += OnInpaintButtonClicked;
            undoButton.Clicked += OnUndoButtonClicked;
            clearButton.Clicked += OnClearButtonClicked;
            applyButton.Clicked += OnApplyInpaintingClicked;

            var panGesture = new PanGestureRecognizer();
            panGesture.PanUpdated += OnMaskCanvasPanUpdated;
            maskCanvas.GestureRecognizers.Add(panGesture);
        }

        private void OnInpaintButtonClicked(object sender, EventArgs e)
        {
            _isInpaintingMode = !_isInpaintingMode;
            InpaintingCanvas.IsVisible = _isInpaintingMode;

            if (_isInpaintingMode)
            {
                // Create a darkened copy of the current image
                DarkenedImage.Source = EditableImage.Source;

                // Clear any existing paths
                _paths.Clear();
                _currentPath = null;

                // Make sure the canvas is ready
                MaskCanvas.Drawable = null;
                MaskCanvas.Invalidate();

                // Initialize with empty drawable
                UpdateMaskCanvas();
            }
            else
            {
                // Clean up when exiting inpainting mode
                DarkenedImage.Source = null;
                MaskCanvas.Drawable = null;
                _paths.Clear();
                _currentPath = null;
            }
        }

        private void OnMaskCanvasPanUpdated(object sender, PanUpdatedEventArgs e)
        {
            var canvas = sender as GraphicsView;
            if (canvas == null) return;

            // Get touch point relative to the canvas
            var touchPoint = new PointFt((float)e.TotalX, (float)e.TotalY);

            switch (e.StatusType)
            {
                case GestureStatus.Started:
                    _currentPath = new List<PointFt>();
                    _currentPath.Add(touchPoint);
                    _paths.Add(_currentPath);
                    break;

                case GestureStatus.Running:
                    if (_currentPath != null)
                    {
                        // Add the new point
                        _currentPath.Add(touchPoint);
                        UpdateMaskCanvas();
                    }
                    break;

                case GestureStatus.Completed:
                    _currentPath = null;
                    break;
            }
        }

        private void UpdateMaskCanvas()
        {
            _maskDrawable = new MaskDrawable(_paths);
            MaskCanvas.Drawable = _maskDrawable;
            MaskCanvas.Invalidate();
        }

        private void OnUndoButtonClicked(object sender, EventArgs e)
        {
            if (_paths.Count > 0)
            {
                _paths.RemoveAt(_paths.Count - 1);
                UpdateMaskCanvas();
            }
        }

        private void OnClearButtonClicked(object sender, EventArgs e)
        {
            _paths.Clear();
            UpdateMaskCanvas();
        }

        private async void OnApplyInpaintingClicked(object sender, EventArgs e)
        {
            if (_paths.Count == 0)
            {
                await DisplayAlert("Error", "Please draw on the areas you want to inpaint", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(PromptEditor.Text))
            {
                await DisplayAlert("Error", "Please enter a prompt", "OK");
                return;
            }

            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsRunning = true;

 
                byte[] maskData = await CreateMaskImageAsync();
                // Load the image data
                byte[] imageData = await File.ReadAllBytesAsync(_currentImagePath);

                // Process the inpainting
                await ProcessInpaintingEdit(imageData,maskData, PromptEditor.Text);

                // Reset inpainting mode
                _isInpaintingMode = false;
                InpaintingCanvas.IsVisible = false;
                _paths.Clear();
                UpdateMaskCanvas();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to process inpainting: {ex.Message}", "OK");
                Debug.WriteLine($"Inpainting error: {ex}");
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;
            }
        }

        private async Task<byte[]> CreateMaskImageAsync()
        {
            try
            {
                // Capture the GraphicsView content
                IScreenshotResult screenshot = await MaskCanvas.CaptureAsync();

                // Convert the screenshot to a byte array
                using var stream = await screenshot.OpenReadAsync();
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);

                // Get the byte array
                byte[] maskData = memoryStream.ToArray();

                // Use ImageSharp to process the mask image
                using var image = SixLabors.ImageSharp.Image.Load(maskData);

                // Ensure the image is in the correct format for the mask
                image.Mutate(x => x
                    .Grayscale()  // Convert to grayscale
                    .BinaryThreshold(0.5f)); // Convert to binary black and white

                // Save the processed mask to a new memory stream
                using var outputStream = new MemoryStream();
                await image.SaveAsPngAsync(outputStream);
                return outputStream.ToArray();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating mask image: {ex}");
                throw;
            }
        }

        private async Task<string> GetImageAsBase64(string imagePath)
        {
            byte[] imageBytes = await File.ReadAllBytesAsync(imagePath);
            return Convert.ToBase64String(imageBytes);
        }

        private async Task<string> GetBase64FromBitmap(byte[] bitmapData)
        {
            return Convert.ToBase64String(bitmapData);
        }

        private async Task ProcessInpaintingEdit(byte[] imageBase64, byte[] maskBase64, string prompt)
        {
            string img_uri = await _imageService.ConvertImageToBase64WithCompression(imageBase64);
            string mask_uri = await _imageService.ConvertImageToBase64WithCompression(maskBase64);
            var imageDataUri = $"data:image/png;base64,{img_uri}";
            var maskDataUri = $"data:image/png;base64,{mask_uri}";

            try
            {
                string negativePrompt = "cartoon, illustration, animation, face, male, female";

                var result = await _imageService.InpaintImageAsync(
                    imageDataUri,
                    maskDataUri,
                    prompt,
                    negativePrompt
                );

                if (result.Images.Count > 0)
                {
                    var image = result.Images[0];

                    using var httpClient = new HttpClient();
                    byte[] imageData = await httpClient.GetByteArrayAsync(image.Url);

                    string tempImagePath = Path.Combine(_editImagesDirectory, "inpainted_image.jpg");
                    await File.WriteAllBytesAsync(tempImagePath, imageData);

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        EditableImage.Source = ImageSource.FromFile(tempImagePath);
                    });
                    _currentImagePath = tempImagePath;
                }
                else
                {
                    await DisplayAlert("Error", "No image was generated", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to process inpainting: {ex.Message}", "OK");
            }
        }
    }
}