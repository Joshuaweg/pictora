using System.Diagnostics;
using System.IO;
using Microsoft.Maui.Storage;

namespace Pictora.Services
{
    public static class AndroidEnvironmentHandler
    {
        // Get the absolute path for Android internal storage
        private static string GetAndroidInternalPath()
        {
            if (DeviceInfo.Platform != DevicePlatform.Android)
                throw new PlatformNotSupportedException("This handler is for Android only");

            return FileSystem.AppDataDirectory;
        }

        public static async Task InitializeEnvironmentAsync()
        {
            try
            {
                string internalPath = GetAndroidInternalPath();
                string filesDir = Path.Combine(internalPath, "files");
                string envPath = Path.Combine(filesDir, ".env");

                // Debug information
                Debug.WriteLine($"Internal Path: {internalPath}");
                Debug.WriteLine($"Files Directory: {filesDir}");
                Debug.WriteLine($"Env File Path: {envPath}");

                // Create directories if they don't exist
                if (!Directory.Exists(filesDir))
                {
                    Debug.WriteLine("Creating files directory...");
                    Directory.CreateDirectory(filesDir);
                }

                // Create .env file if it doesn't exist
                if (!File.Exists(envPath))
                {
                    Debug.WriteLine("Creating .env file...");
                    string defaultEnvContent = @"# Pictora Environment Configuration
API_URL=https://api.example.com
API_KEY=your_default_key_here
DEBUG=true
ENVIRONMENT=development
";
                    await File.WriteAllTextAsync(envPath, defaultEnvContent);
                    Debug.WriteLine(".env file created successfully");
                }

                // Verify file creation
                if (File.Exists(envPath))
                {
                    Debug.WriteLine("Environment file exists at: " + envPath);
                    var contents = await File.ReadAllTextAsync(envPath);
                    Debug.WriteLine("Environment file contents:");
                    Debug.WriteLine(contents);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in InitializeEnvironmentAsync: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public static async Task<Dictionary<string, string>> LoadEnvironmentVariablesAsync()
        {
            var envVars = new Dictionary<string, string>();

            try
            {
                string envPath = Path.Combine(GetAndroidInternalPath(), "files", ".env");

                if (File.Exists(envPath))
                {
                    string[] lines = await File.ReadAllLinesAsync(envPath);
                    foreach (string line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                            continue;

                        int separatorIndex = line.IndexOf('=');
                        if (separatorIndex > 0)
                        {
                            string key = line.Substring(0, separatorIndex).Trim();
                            string value = line.Substring(separatorIndex + 1).Trim();
                            envVars[key] = value;
                        }
                    }
                }
                else
                {
                    Debug.WriteLine($"Environment file not found at: {envPath}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading environment variables: {ex.Message}");
                throw;
            }

            return envVars;
        }
    }
}