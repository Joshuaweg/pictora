using Pictora.Services;

namespace Pictora
{
    public partial class App : Application
    {
        public static Dictionary<string, string> EnvironmentVariables { get; private set; }

        public App()
        {
            InitializeComponent();

            InitializeAppAsync();
        }

        private async void InitializeAppAsync()
        {
            try
            {
                // Initialize and load environment variables
                await AndroidEnvironmentHandler.InitializeEnvironmentAsync();
                EnvironmentVariables = await AndroidEnvironmentHandler.LoadEnvironmentVariablesAsync();

                MainPage = new AppShell();
            }
            catch (Exception ex)
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
            }
        }
    }
}