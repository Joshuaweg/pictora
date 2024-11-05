using Pictora.Services;
using System.Diagnostics;

namespace Pictora
{
    public partial class EditImagePage : ContentPage
    {
        private readonly ImageEditingService _imageService;
        private readonly string _editImagesDirectory;
        private string _currentImagePath;

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

                string negativePrompt = "cartoon, illustration, animation, face, male, female";

                var result = await _imageService.EditImageAsync(
                    "https://fal-cdn.batuhan-941.workers.dev/files/tiger/IExuP-WICqaIesLZAZPur.jpeg",
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


        private async void OnSaveButtonClicked(object sender, EventArgs e)
        {
            try
            {
                if (_currentImagePath == null || !File.Exists(_currentImagePath))
                {
                    await DisplayAlert("Error", "No image to save", "OK");
                    return;
                }

                // Get the Pictures folder
                string picturesFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                string fileName = $"pictora_edited_{DateTime.Now:yyyyMMddHHmmss}.jpg";
                string destinationPath = Path.Combine(picturesFolder, fileName);

                // Copy the file
                File.Copy(_currentImagePath, destinationPath, true);
                await DisplayAlert("Success", $"Image saved to Pictures folder as {fileName}", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to save image: {ex.Message}", "OK");
            }
        }
    }
}