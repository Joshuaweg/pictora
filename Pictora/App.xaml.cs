using Pictora.Services;

namespace Pictora
{
    public partial class App : Application
    {
        public static Dictionary<string, string> EnvironmentVariables { get; private set; }

        public App()
        {
            InitializeComponent();

            // Set a loading page immediately
            MainPage = new ContentPage
            {
                Content = new VerticalStackLayout
                {
                    Spacing = 10,
                    Padding = new Thickness(20),
                    Children =
                    {
                        new ActivityIndicator
                        {
                            IsRunning = true,
                            HorizontalOptions = LayoutOptions.Center,
                            VerticalOptions = LayoutOptions.Center
                        },
                        new Label
                        {
                            Text = "Loading...",
                            HorizontalOptions = LayoutOptions.Center
                        }
                    }
                }
            };

            // Start initialization process
            InitializeAppAsync();
        }

        private async void InitializeAppAsync()
        {
            try
            {
                // Initialize and load environment variables
                // await AndroidEnvironmentHandler.InitializeEnvironmentAsync();
                // EnvironmentVariables = await AndroidEnvironmentHandler.LoadEnvironmentVariablesAsync();

                // Switch to the main app shell on the main thread
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    MainPage = new AppShell();
                });
            }
            catch (Exception ex)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    MainPage = new ContentPage
                    {
                        Content = new VerticalStackLayout
                        {
                            Spacing = 10,
                            Padding = new Thickness(20),
                            Children =
                            {
                                new Label
                                {
                                    Text = "Error Initializing App",
                                    FontSize = 20,
                                    HorizontalOptions = LayoutOptions.Center
                                },
                                new Label
                                {
                                    Text = ex.Message,
                                    HorizontalOptions = LayoutOptions.Center,
                                    TextColor = Colors.Red
                                },
                                new Label
                                {
                                    Text = $"App Data Directory: {FileSystem.AppDataDirectory}",
                                    FontSize = 14,
                                    HorizontalOptions = LayoutOptions.Center
                                }
                            }
                        }
                    };
                });
            }
        }
    }
}