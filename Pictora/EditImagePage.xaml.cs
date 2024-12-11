using Pictora.Components;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp;
using System.Diagnostics;
using PointFt = Microsoft.Maui.Graphics.PointF;
using ColorM = Microsoft.Maui.Graphics.Color;
using SixLabors.ImageSharp.PixelFormats;
using System.Numerics;
using Pictora.services;
using Pictora.Services;


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
        private int idx;
        private ImageFilterService _filterService;
        private InpaintingService _inpaintingService;
        private DrawShapeService _drawShapeService;
        private string _currentShapeType;
        private bool _isDrawingMode = false;
        private Microsoft.Maui.Graphics.Color _currentShapeColor = Colors.Red;



        public EditImagePage(Pictora.Models.Image generated_image = null, int idx =0)
        {
            InitializeComponent();

            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string envPath = Path.Combine(baseDirectory, ".env");
            string uri = "";
            this.idx = idx;

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
            MaskCanvas.StartInteraction += OnStartDrawing;
            MaskCanvas.DragInteraction += OnDrawing;
            MaskCanvas.EndInteraction += OnEndDrawing;
            ShapeCanvas.StartInteraction += OnStartDrawingShape;
            ShapeCanvas.DragInteraction += OnDraggingShape;
            ShapeCanvas.EndInteraction += OnEndDrawingShape;
            _filterService = new ImageFilterService();
            _inpaintingService = new InpaintingService(_imageService, _editImagesDirectory);
            _drawShapeService = new DrawShapeService();

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

        public void DeleteCaption(DraggableCaption caption)
        {
            _captions.Remove(caption);
            CaptionOverlay.Children.Remove(caption);
        }

        public async Task EditCaption(DraggableCaption caption)
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
            if (_paths.Count == 0 || string.IsNullOrWhiteSpace(PromptEditor.Text))
            {
                await DisplayAlert("Error",
                    _paths.Count == 0 ? "Please draw on areas to inpaint" : "Please enter a prompt",
                    "OK");
                return;
            }

            try
            {
                LoadingIndicator.IsVisible = LoadingIndicator.IsRunning = true;

                var maskData = await _inpaintingService.CreateMaskFromPaths(_paths, MaskCanvas );
                var imageData = await _inpaintingService.GetImageData(_currentImagePath);

                var (newPath, result) = await _inpaintingService.ProcessInpainting(
                    imageData,
                    maskData,
                    PromptEditor.Text,
                    "cartoon, illustration, animation, face, male, female");

                MainThread.BeginInvokeOnMainThread(() =>
                    EditableImage.Source = ImageSource.FromFile(newPath));
                _currentImagePath = newPath;

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
                LoadingIndicator.IsVisible = LoadingIndicator.IsRunning = false;
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
                    if (_originalImagePath == null)
                        _originalImagePath = _currentImagePath;

                    var imageBytes = await _filterService.LoadImageBytes(_currentImagePath);
                    var filteredBytes = await _filterService.ApplyBlackAndWhiteFilter(imageBytes);

                    string tempImagePath = Path.Combine(_editImagesDirectory, "bw_image.jpg");
                    await File.WriteAllBytesAsync(tempImagePath, filteredBytes);

                    MainThread.BeginInvokeOnMainThread(() =>
                        EditableImage.Source = ImageSource.FromFile(tempImagePath));

                    _currentImagePath = tempImagePath;
                    _isBlackAndWhite = true;
                    UpdateFilterButtonStyles("BlackAndWhite");
                }
                else
                {
                    await ResetCurrentFilter();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to toggle black and white filter: {ex.Message}", "OK");
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
                    if (_originalImagePath == null)
                        _originalImagePath = _currentImagePath;

                    var imageBytes = await _filterService.LoadImageBytes(_currentImagePath);
                    var filteredBytes = await _filterService.ApplyBlueShiftFilter(imageBytes);

                    string tempImagePath = Path.Combine(_editImagesDirectory, "blue_shift_image.jpg");
                    await File.WriteAllBytesAsync(tempImagePath, filteredBytes);

                    MainThread.BeginInvokeOnMainThread(() =>
                        EditableImage.Source = ImageSource.FromFile(tempImagePath));

                    _currentImagePath = tempImagePath;
                    _isBlueShift = true;
                    UpdateFilterButtonStyles("BlueShift");
                }
                else
                {
                    await ResetCurrentFilter();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to toggle blue shift filter: {ex.Message}", "OK");
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
                    if (_originalImagePath == null)
                        _originalImagePath = _currentImagePath;

                    var imageBytes = await _filterService.LoadImageBytes(_currentImagePath);
                    var filteredBytes = await _filterService.ApplyVintageFilter(imageBytes);

                    string tempImagePath = Path.Combine(_editImagesDirectory, "vintage_image.jpg");
                    await File.WriteAllBytesAsync(tempImagePath, filteredBytes);

                    MainThread.BeginInvokeOnMainThread(() =>
                        EditableImage.Source = ImageSource.FromFile(tempImagePath));

                    _currentImagePath = tempImagePath;
                    _isVintage = true;
                    UpdateFilterButtonStyles("Vintage");
                }
                else
                {
                    await ResetCurrentFilter();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to toggle vintage filter: {ex.Message}", "OK");
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;
            }
        }
        private void OnBlackAndWhiteTapped(object sender, TappedEventArgs e)
        {
            if (_isBlueShift || _isVintage)
            {
                // Reset other filters first
                _ = ResetCurrentFilter();
            }
            _ = ToggleBlackAndWhiteFilter();
        }

        private void OnBlueShiftTapped(object sender, TappedEventArgs e)
        {
            if (_isBlackAndWhite || _isVintage)
            {
                // Reset other filters first
                _ = ResetCurrentFilter();
            }
            _ = ToggleBlueShiftFilter();
        }

        private void OnVintageTapped(object sender, TappedEventArgs e)
        {
            if (_isBlackAndWhite || _isBlueShift)
            {
                // Reset other filters first
                _ = ResetCurrentFilter();
            }
            _ = ToggleVintageFilter();
        }

        private async Task ResetCurrentFilter()
        {
            try
            {
                // Reset the image to original
                if (_originalImagePath != null)
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
                    });
                    _currentImagePath = _originalImagePath;
                }

                // Reset all filter states
                _isBlackAndWhite = false;
                _isBlueShift = false;
                _isVintage = false;

                // Reset visual states
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ResetCurrentFilter: {ex.Message}");
            }
        }

        private void UpdateFilterButtonStyles(string activeFilter)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                Frame previousActiveFrame = GetActiveFrame();
                Frame newActiveFrame = null;

                // Reset all labels to default state first
                BlackAndWhiteLabel.TextColor = ColorM.FromArgb("#666666");
                BlueShiftLabel.TextColor = ColorM.FromArgb("#666666");
                VintageLabel.TextColor = ColorM.FromArgb("#666666");

                // Determine new active frame
                switch (activeFilter)
                {
                    case "BlackAndWhite":
                        newActiveFrame = BlackAndWhiteFrame;
                        break;
                    case "BlueShift":
                        newActiveFrame = BlueShiftFrame;
                        break;
                    case "Vintage":
                        newActiveFrame = VintageFrame;
                        break;
                }

                if (newActiveFrame != null)
                {
                    await AnimateFilterSelection(newActiveFrame, previousActiveFrame);
                }
            });
        }
        private async Task AnimateFilterSelection(Frame newActiveFrame, Frame oldActiveFrame = null)
        {
            uint animationDuration = 300;
            Debug.WriteLine("Beginning Sliding Animation");
            Debug.WriteLine(oldActiveFrame != null);
            Debug.WriteLine(oldActiveFrame != newActiveFrame);
            if (oldActiveFrame != null )
            {
                Debug.WriteLine("Sliding");
                // Get the positions for animation
                double startPosition = GetFramePosition(oldActiveFrame);
                double endPosition = GetFramePosition(newActiveFrame);

                // Each frame is 100 units wide
                double currentX = SelectionIndicator.TranslationX;
                Debug.WriteLine("Current position: " + currentX.ToString());
                double targetX = endPosition * 100;
                Debug.WriteLine("end position: " + endPosition.ToString());

                // Create the sliding animation for the selection indicator
                var slideAnimation = new Animation(
                    callback: v => SelectionIndicator.TranslationX = v,
                    start: currentX,
                    end: targetX,
                    easing: Easing.CubicInOut
                );

                // Start the animation
                slideAnimation.Commit(
                    owner: SelectionIndicator,
                    name: "SelectionSlide",
                    length: animationDuration,
                    easing: Easing.CubicInOut,
                    finished: (v, c) => SelectionIndicator.TranslationX = targetX  // Ensure final position
                );

                // Update text colors
                if (oldActiveFrame.Content is Label oldLabel)
                {
                    oldLabel.TextColor = ColorM.FromArgb("#666666");
                }
            }
            else if (newActiveFrame != null)
            {
                // Initial positioning for first selection
                double position = GetFramePosition(newActiveFrame);
                SelectionIndicator.TranslationX = position * 100;
            }

            // Update new frame text color
            if (newActiveFrame.Content is Label newLabel)
            {
                newLabel.TextColor = Colors.White;
            }

            await Task.Delay((int)animationDuration);
        }
        private Frame GetActiveFrame()
        {
            if (_isBlackAndWhite) return BlackAndWhiteFrame;
            if (_isBlueShift) return BlueShiftFrame;
            if (_isVintage) return VintageFrame;
            return null;
        }
        private double GetFramePosition(Frame frame)
        {
            // Get the frame's position in the segmented control
            if (frame == BlackAndWhiteFrame) return 0;
            if (frame == BlueShiftFrame) return 1;
            if (frame == VintageFrame) return 2;
            return 0;
        }

        private void OnDrawShapesButtonClicked(object sender, EventArgs e)
        {
            _isDrawingMode = !_isDrawingMode;
            ShapeDrawingCanvas.IsVisible = _isDrawingMode;

            if (_isDrawingMode)
            {
                ShapeCanvas.Drawable = new Pictora.Services.ShapeDrawable(_drawShapeService.Shapes);
            }
           
        }

        private void OnStartDrawingShape(object sender, TouchEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentShapeType)) return;

            var point = e.Touches.FirstOrDefault();
            if (point.IsEmpty) return;

            _drawShapeService.StartDrawing(
                _currentShapeType,
                new Microsoft.Maui.Graphics.PointF((float)point.X, (float)point.Y),
                _currentShapeColor
            );
            UpdateShapeCanvas();
        }

        private void OnDraggingShape(object sender, TouchEventArgs e)
        {
            var point = e.Touches.FirstOrDefault();
            if (point.IsEmpty) return;

            _drawShapeService.UpdateDrawing(new Microsoft.Maui.Graphics.PointF((float)point.X, (float)point.Y));
            UpdateShapeCanvas();
        }

        private void OnEndDrawingShape(object sender, TouchEventArgs e)
        {
            _drawShapeService.EndDrawing();
            UpdateShapeCanvas();
        }

        private void UpdateShapeCanvas()
        {
            ShapeCanvas.Invalidate();
        }

        private void OnRectangleButtonClicked(object sender, EventArgs e)
        {
            _currentShapeType = "rectangle";
        }

        private void OnEllipseButtonClicked(object sender, EventArgs e)
        {
            _currentShapeType = "ellipse";
        }

        private void OnTriangleButtonClicked(object sender, EventArgs e)
        {
            _currentShapeType = "triangle";
        }

        private async void OnColorButtonClicked(object sender, EventArgs e)
        {
            var colors = new string[] { "Red", "Blue", "Green", "Yellow", "Purple", "Orange" };
            var result = await DisplayActionSheet("Select Color", "Cancel", null, colors);

            if (result != "Cancel" && result != null)
            {
                _currentShapeColor = result.ToLower() switch
                {
                    "red" => Colors.Red,
                    "blue" => Colors.Blue,
                    "green" => Colors.Green,
                    "yellow" => Colors.Yellow,
                    "purple" => Colors.Purple,
                    "orange" => Colors.Orange,
                    _ => Colors.Red
                };
            }
        }

        private void OnUndoShapeButtonClicked(object sender, EventArgs e)
        {
            var shapesCount = _drawShapeService.Shapes.Count;
            if (shapesCount > 0)
            {
                _drawShapeService.RemoveShape(shapesCount - 1);
                UpdateShapeCanvas();
            }
        }

        private void OnClearShapesButtonClicked(object sender, EventArgs e)
        {
            _drawShapeService.ClearShapes();
            UpdateShapeCanvas();
        }

        private void OnDoneDrawingButtonClicked(object sender, EventArgs e)
        {
            _isDrawingMode = false;
            ShapeDrawingCanvas.IsVisible = false;
        }
    }
}