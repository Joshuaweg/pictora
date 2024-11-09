using Pictora.Services;
using System.Diagnostics;

namespace Pictora
{
    public partial class EditImagePage : ContentPage
    {
        private readonly ImageEditingService _imageService;
        private readonly string _editImagesDirectory;
        private string _currentImagePath;
        private bool _isCaptionMode = false;
        private List<DraggableCaption> _captions = new List<DraggableCaption>();

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


            public DraggableCaption(string text)
            {
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
                    BackgroundColor = Colors.Black.WithAlpha(0.7f),
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
                var action = await (Parent as EditImagePage)?.DisplayActionSheet(
                    "Adjust Font Size",
                    "Cancel",
                    null,
                    "Small (14)",
                    "Medium (18)",
                    "Large (24)",
                    "Extra Large (32)");

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
                var parent = Parent as EditImagePage;
                if (parent != null)
                {
                    string action = await parent.DisplayActionSheet(
                        "Caption Options",
                        "Cancel",
                        "Delete",
                        "Edit Text",
                        "Change Color");

                    switch (action)
                    {
                        case "Delete":
                            parent.DeleteCaption(this);
                            break;
                        case "Edit Text":
                            await parent.EditCaption(this);
                            break;
                        case "Change Color":
                            await ChangeTextColor();
                            break;
                    }
                }
            }


            private async Task ChangeTextColor()
            {
                var parent = Parent as EditImagePage;
                if (parent == null) return;

                var action = await parent.DisplayActionSheet(
                    "Select Text Color",
                    "Cancel",
                    null,
                    "White",
                    "Black",
                    "Red",
                    "Blue",
                    "Green",
                    "Yellow");

                Color newColor = action switch
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

            private bool IsLightColor(Color color)
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

            var caption = new DraggableCaption(CaptionTextEntry.Text);
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
        private async Task SaveImageWithCaptions()
        {
            try
            {
                if (_currentImagePath == null || !File.Exists(_currentImagePath))
                {
                    await DisplayAlert("Error", "No image to save", "OK");
                    return;
                }

                // Create a screenshot of the entire image container including captions
                IView container = ImageContainer;
                if (container == null) return;

                // Take the screenshot without using statement
                var screenshot = await container.CaptureAsync();
                if (screenshot == null) return;

                // Save the screenshot
                string picturesFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                string fileName = $"pictora_captioned_{DateTime.Now:yyyyMMddHHmmss}.jpg";
                string destinationPath = Path.Combine(picturesFolder, fileName);

                // Use FileStream to write the screenshot data
                using (var stream = File.OpenWrite(destinationPath))
                {
                    await screenshot.CopyToAsync(stream);
                }

                await DisplayAlert("Success", $"Image saved with captions to Pictures folder as {fileName}", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to save image with captions: {ex.Message}", "OK");
            }
        }


        private async void OnSaveButtonClicked(object sender, EventArgs e)
        {
            await SaveImageWithCaptions();
        }
    }
}