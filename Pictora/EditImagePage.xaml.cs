using Pictora.Services;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp;
using System.Diagnostics;
using PointFt = Microsoft.Maui.Graphics.PointF;
using ColorM = Microsoft.Maui.Graphics.Color;
using SixLabors.ImageSharp.PixelFormats;
using System.Numerics;


namespace Pictora
{
    public partial class EditImagePage : ContentPage
    {
        private readonly ImageEditingService _imageService;
        private readonly string _editImagesDirectory;
        private string _currentImagePath;
        private bool _isCaptionMode = false;
        private bool _isBlackAndWhite = false;
        private bool _isBlueShift = false;
        private bool _isVintage = false;
        private string _originalImagePath;
        private List<DraggableCaption> _captions = new List<DraggableCaption>();
        private bool _isInpaintingMode = false;
        private List<PointFt> _currentPath = new List<PointFt>();
        private List<List<PointFt>> _paths = new List<List<PointFt>>();
        private IDrawable _maskDrawable;
        private PointFt _lastTouchPoint;
        private ScrollView _inpaintScrollView;
        private GraphicsView _maskCanvas;

        private class MaskDrawable : IDrawable
        {
            private readonly List<List<PointFt>> _paths;
            private const float STROKE_WIDTH = 20f;

            public MaskDrawable(List<List<PointFt>> paths)
            {
                _paths = paths;
            }

            public void Draw(ICanvas canvas, RectF dirtyRect)
            {
                if (_paths == null || !_paths.Any()) return;

                // Set up drawing parameters
                canvas.StrokeColor = Colors.White;
                canvas.StrokeSize = STROKE_WIDTH;
                canvas.StrokeLineCap = LineCap.Round;
                canvas.StrokeLineJoin = LineJoin.Round;
                canvas.FillColor = Colors.White;

                foreach (var path in _paths)
                {
                    if (path.Count < 2) continue;

                    // Create and draw the path
                    var pathF = new PathF();
                    pathF.MoveTo(path[0].X, path[0].Y);

                    for (int i = 1; i < path.Count; i++)
                    {
                        pathF.LineTo(path[i].X, path[i].Y);
                    }

                    // Draw the stroke
                    canvas.DrawPath(pathF);

                    // Draw dots at each point for better visibility
                    foreach (var point in path)
                    {
                        canvas.FillCircle(point.X, point.Y, STROKE_WIDTH / 2);
                    }
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
                _captionLabel = new Label
                {
                    Text = text,
                    TextColor = Colors.White,
                    FontSize = 18,
                    BackgroundColor = Colors.Black.WithAlpha(0.0f),
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
                    Colors.Black.WithAlpha(0.0f) :
                    Colors.White.WithAlpha(0.0f);
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

        public EditImagePage(Pictora.Models.Image generated_image = null)
        {
            InitializeComponent();

            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string envPath = Path.Combine(baseDirectory, ".env");
            string uri = "";

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await DisplayAlert("Debug Info",
                    $"Looking for .env at: {envPath}\n" +
                    $"File exists: {File.Exists(envPath)}", "OK");
            });
            if (generated_image == null)
            {
                UploadButton.IsVisible = false;
            }
            else
            {
                uri = generated_image.ImageUrl;
                UploadButton.Clicked += OnUploadButtonClicked;
            }
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

            SetupImageDirectory(uri);


            // Wire up button click handlers
            EditButton.Clicked += OnEditButtonClicked;
            SaveButton.Clicked += OnSaveButtonClicked;
            Filter1Button.Clicked += OnFilter1ButtonClicked;
            Filter2Button.Clicked += OnFilter2ButtonClicked;
            Filter2Button.Text = "Blue Shift";
            Filter3Button.Clicked += OnFilter3ButtonClicked;
            Filter3Button.Text = "Vintage";
            MaskCanvas.StartInteraction += OnStartDrawing;
            MaskCanvas.DragInteraction += OnDrawing;
            MaskCanvas.EndInteraction += OnEndDrawing;

        }

        private void OnStartDrawing(object sender, TouchEventArgs e)
        {
            try
            {
                var point = e.Touches.FirstOrDefault();
                if (point.IsEmpty) return;

                _currentPath = new List<PointFt>();
                _currentPath.Add(new PointFt((float)point.X, (float)point.Y));
                _paths.Add(_currentPath);
                UpdateMaskCanvas();
                Debug.WriteLine($"Started drawing at: {point.X}, {point.Y}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnStartDrawing: {ex.Message}");
            }
        }

        private void OnDrawing(object sender, TouchEventArgs e)
        {
            if (_currentPath == null) return;

            try
            {
                var point = e.Touches.FirstOrDefault();
                if (point.IsEmpty) return;

                var newPoint = new PointFt((float)point.X, (float)point.Y);
                _currentPath.Add(newPoint);
                UpdateMaskCanvas();
                Debug.WriteLine($"Drawing at: {point.X}, {point.Y}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnDrawing: {ex.Message}");
            }
        }

        private void OnEndDrawing(object sender, TouchEventArgs e)
        {
            _currentPath = null;
            Debug.WriteLine("Completed drawing");
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
        private void SetupImageDirectory(string path = "")
        {
            Debug.WriteLine("image path: " + path);
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
                if (!path.Equals(""))
                {
                    _currentImagePath = path;
                }

                // Update the image source
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (path.Equals(""))
                    {
                        EditableImage.Source = ImageSource.FromFile(_currentImagePath);
                    }
                    else
                    {
                        Uri web_image = new Uri(path);
                        EditableImage.Source = ImageSource.FromUri(web_image);
                    }
                });

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
        private async Task<String> SaveImageWithCaptions()
        {
            try
            {
                if (_currentImagePath == null)
                {
                    await DisplayAlert("Error", "No image to save", "OK");
                    return null;
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

                return Path.Combine(picturesFolder, fileName);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to save image: {ex.Message}", "OK");
                return null;
            }
        }

        private async void OnSaveButtonClicked(object sender, EventArgs e)
        {
            await SaveImageWithCaptions();
        }

        private async void OnUploadButtonClicked(object? sender, EventArgs e)
        {
            string _fileToSendBack = await SaveImageWithCaptions();
            await Shell.Current.GoToAsync($"..?file={_fileToSendBack}");
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

                // Match the size of the darkened image to the original
                InpaintContainer.WidthRequest = ImageContainer.Width;
                InpaintContainer.HeightRequest = ImageContainer.Height;

                // Set the GraphicsView to match the image size
                MaskCanvas.WidthRequest = ImageContainer.Width;
                MaskCanvas.HeightRequest = ImageContainer.Height;

                // Clear any existing paths
                _paths.Clear();
                _currentPath = null;

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
            try
            {
                var canvas = sender as GraphicsView;
                if (canvas == null) return;

                PointFt point;
                if (e.StatusType == GestureStatus.Started)
                {
                    // Store the initial position
                    point = new PointFt((float)e.TotalX, (float)e.TotalY);
                    _lastTouchPoint = point;

                    _currentPath = new List<PointFt>();
                    _currentPath.Add(new PointFt((float)point.X, (float)point.Y));
                    _paths.Add(_currentPath);
                    Debug.WriteLine($"Started drawing at: {point.X}, {point.Y}");
                }
                else if (e.StatusType == GestureStatus.Running && _currentPath != null)
                {
                    // Calculate the new position based on the delta from last position
                    var newX = _lastTouchPoint.X + e.TotalX;
                    var newY = _lastTouchPoint.Y + e.TotalY;

                    var newPoint = new PointFt((float)newX, (float)newY);
                    _currentPath.Add(newPoint);
                    _lastTouchPoint = new PointFt((float)newX, (float)newY);

                    UpdateMaskCanvas();
                    Debug.WriteLine($"Drawing at: {newX}, {newY}");
                }
                else if (e.StatusType == GestureStatus.Completed)
                {
                    Debug.WriteLine($"Completed drawing at: {_lastTouchPoint.X}, {_lastTouchPoint.Y}");
                    _currentPath = null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnMaskCanvasPanUpdated: {ex.Message}");
            }
        }
        private void OnDragStarting(object sender, DragStartingEventArgs e)
        {
            var canvas = sender as GraphicsView;
            if (canvas == null) return;

            try
            {
                var position = e.GetPosition(canvas);
                if (position.HasValue)
                {
                    _currentPath = new List<PointFt>();
                    _currentPath.Add(new PointFt((float)position.Value.X, (float)position.Value.Y));
                    _paths.Add(_currentPath);
                    Debug.WriteLine($"Started drawing at: {position.Value.X}, {position.Value.Y}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnDragStarting: {ex.Message}");
            }
        }

        private void OnPointerMoved(object sender, PointerEventArgs e)
        {
            var canvas = sender as GraphicsView;
            if (canvas == null || _currentPath == null) return;

            try
            {
                var position = e.GetPosition(canvas);
                if (position.HasValue)
                {
                    var newPoint = new PointFt((float)position.Value.X, (float)position.Value.Y);
                    _currentPath.Add(newPoint);
                    UpdateMaskCanvas();
                    Debug.WriteLine($"Drawing at: {position.Value.X}, {position.Value.Y}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnPointerMoved: {ex.Message}");
            }
        }

        private void OnDropCompleted(object sender, DropCompletedEventArgs e)
        {
            Debug.WriteLine("Completed drawing");
            _currentPath = null;
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
                byte[] imageData = null;
                // Load the image data
                Debug.WriteLine(_currentImagePath);
                if (_currentImagePath.Contains("http"))
                {
                    //get bytes from uri
                    imageData = await GetBase64fromUrl(_currentImagePath);
                    Debug.WriteLine(imageData);
                    // Process the inpainting
                }
                else
                {
                    //get bytes from file
                    imageData = await File.ReadAllBytesAsync(_currentImagePath);
                }
                Debug.WriteLine(imageData);
                // Process the inpainting
                await ProcessInpaintingEdit(imageData, maskData, PromptEditor.Text);

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
        private async Task<byte[]> GetBase64fromUrl(string url)
        {

            HttpClient _client = new HttpClient(); ;
            byte[] imageBytes = await _client.GetByteArrayAsync(url);
            return imageBytes;

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
                Debug.WriteLine("Inpainting:\n");
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
        private void OnFilter1ButtonClicked(object sender, EventArgs e)
        {
            // Call the async method from the event handler
            _ = ToggleBlackAndWhiteFilter();
        }

        private async Task ToggleBlackAndWhiteFilter()
        {
            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsRunning = true;

                if (!_isBlackAndWhite)
                {
                    _originalImagePath = _currentImagePath;

                    byte[] imageBytes;
                    if (_currentImagePath.Contains("http"))
                    {
                        using var client = new HttpClient();
                        imageBytes = await client.GetByteArrayAsync(_currentImagePath);
                    }
                    else
                    {
                        imageBytes = await File.ReadAllBytesAsync(_currentImagePath);
                    }

                    using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(imageBytes);

                    image.Mutate(x => x
                        .Grayscale()
                        .Contrast(1.1f));

                    string tempImagePath = Path.Combine(_editImagesDirectory, "bw_image.jpg");
                    await using var fileStream = File.Create(tempImagePath);
                    await image.SaveAsJpegAsync(fileStream);

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        EditableImage.Source = ImageSource.FromFile(tempImagePath);
                        Filter1Button.Text = "Remove B&W";
                    });
                    _currentImagePath = tempImagePath;
                    _isBlackAndWhite = true;
                }
                else
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (_originalImagePath.Contains("http"))
                        {
                            EditableImage.Source = ImageSource.FromUri(new Uri(_originalImagePath));
                        }
                        else
                        {
                            EditableImage.Source = ImageSource.FromFile(_originalImagePath);
                        }
                        Filter1Button.Text = "B&W";
                    });
                    _currentImagePath = _originalImagePath;
                    _isBlackAndWhite = false;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to toggle black and white filter: {ex.Message}", "OK");
                Debug.WriteLine($"Error in ToggleBlackAndWhiteFilter: {ex}");
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;
            }
        }
        private void OnFilter2ButtonClicked(object sender, EventArgs e)
        {
            _ = ToggleBlueShiftFilter();
        }

        private async Task ToggleBlueShiftFilter()
        {
            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsRunning = true;

                if (!_isBlueShift)
                {
                    _originalImagePath = _currentImagePath;

                    byte[] imageBytes;
                    if (_currentImagePath.Contains("http"))
                    {
                        using var client = new HttpClient();
                        imageBytes = await client.GetByteArrayAsync(_currentImagePath);
                    }
                    else
                    {
                        imageBytes = await File.ReadAllBytesAsync(_currentImagePath);
                    }

                    using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(imageBytes);

                    // Apply blue shift effect
                    image.Mutate(x => x.ProcessPixelRowsAsVector4(row =>
                    {
                        for (int x = 0; x < row.Length; x++)
                        {
                            row[x] = new Vector4(
                                row[x].X * 0.8f,     // Reduce red
                                row[x].Y * 0.9f,     // Reduce green slightly
                                row[x].Z * 1.2f,     // Enhance blue
                                row[x].W              // Keep alpha the same
                            );
                        }
                    }));

                    string tempImagePath = Path.Combine(_editImagesDirectory, "blue_shift_image.jpg");
                    await using var fileStream = File.Create(tempImagePath);
                    await image.SaveAsJpegAsync(fileStream);

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        EditableImage.Source = ImageSource.FromFile(tempImagePath);
                        Filter2Button.Text = "Remove Blue";
                    });
                    _currentImagePath = tempImagePath;
                    _isBlueShift = true;
                }
                else
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (_originalImagePath.Contains("http"))
                        {
                            EditableImage.Source = ImageSource.FromUri(new Uri(_originalImagePath));
                        }
                        else
                        {
                            EditableImage.Source = ImageSource.FromFile(_originalImagePath);
                        }
                        Filter2Button.Text = "Blue Shift";
                    });
                    _currentImagePath = _originalImagePath;
                    _isBlueShift = false;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to toggle blue shift filter: {ex.Message}", "OK");
                Debug.WriteLine($"Error in ToggleBlueShiftFilter: {ex}");
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;
            }
        }
        private void OnFilter3ButtonClicked(object sender, EventArgs e)
        {
            _ = ToggleVintageFilter();
        }

        private async Task ToggleVintageFilter()
        {
            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsRunning = true;

                if (!_isVintage)
                {
                    _originalImagePath = _currentImagePath;

                    byte[] imageBytes;
                    if (_currentImagePath.Contains("http"))
                    {
                        using var client = new HttpClient();
                        imageBytes = await client.GetByteArrayAsync(_currentImagePath);
                    }
                    else
                    {
                        imageBytes = await File.ReadAllBytesAsync(_currentImagePath);
                    }

                    using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(imageBytes);

                    // Apply vintage effect
                    image.Mutate(x => x.ProcessPixelRowsAsVector4(row =>
                    {
                        for (int x = 0; x < row.Length; x++)
                        {
                            // Get original color values
                            float r = row[x].X;
                            float g = row[x].Y;
                            float b = row[x].Z;

                            // Apply warm vintage tone
                            row[x] = new Vector4(
                                Math.Min(r * 1.2f, 1.0f),     // Enhance red slightly
                                g * 0.9f,                     // Reduce green slightly
                                b * 0.8f,                     // Reduce blue more
                                row[x].W                      // Keep alpha the same
                            );
                        }
                    }));

                    // Add slight vignette effect
                    image.Mutate(x => x
                        .Contrast(1.1f)     // Increase contrast slightly
                        .Sepia(0.2f));      // Add subtle sepia tone

                    string tempImagePath = Path.Combine(_editImagesDirectory, "vintage_image.jpg");
                    await using var fileStream = File.Create(tempImagePath);
                    await image.SaveAsJpegAsync(fileStream);

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        EditableImage.Source = ImageSource.FromFile(tempImagePath);
                        Filter3Button.Text = "Remove Vintage";
                    });
                    _currentImagePath = tempImagePath;
                    _isVintage = true;
                }
                else
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (_originalImagePath.Contains("http"))
                        {
                            EditableImage.Source = ImageSource.FromUri(new Uri(_originalImagePath));
                        }
                        else
                        {
                            EditableImage.Source = ImageSource.FromFile(_originalImagePath);
                        }
                        Filter3Button.Text = "Vintage";
                    });
                    _currentImagePath = _originalImagePath;
                    _isVintage = false;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to toggle vintage filter: {ex.Message}", "OK");
                Debug.WriteLine($"Error in ToggleVintageFilter: {ex}");
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;
            }
        }
    }
}