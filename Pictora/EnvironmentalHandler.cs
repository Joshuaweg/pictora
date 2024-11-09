using System.IO;

namespace Pictora.Services
{
    public static class EnvironmentHandler
    {
        private static string EnvFilePath => Path.Combine(FileSystem.AppDataDirectory, "files", ".env");

        public static void InitializeEnvironment()
        {
            try
            {
                // Create the files directory if it doesn't exist
                string filesDir = Path.Combine(FileSystem.AppDataDirectory, "files");
                if (!Directory.Exists(filesDir))
                {
                    Directory.CreateDirectory(filesDir);
                }

                // Create .env file if it doesn't exist
                if (!File.Exists(EnvFilePath))
                {
                    string defaultEnvContent = @"FAL_API_KEY=63323d45-b888-473c-9b6a-30d0b2e2d602:a1c3af767eb96b9c9b6bc30c5928de9d";
                    File.WriteAllText(EnvFilePath, defaultEnvContent);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing environment: {ex.Message}");
                throw;
            }
        }

        public static Dictionary<string, string> LoadEnvironmentVariables()
        {
            var envVars = new Dictionary<string, string>();

            try
            {
                if (File.Exists(EnvFilePath))
                {
                    string[] lines = File.ReadAllLines(EnvFilePath);
                    foreach (string line in lines)
                    {
                        // Skip comments and empty lines
                        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                            continue;

                        // Parse KEY=VALUE format
                        int separatorIndex = line.IndexOf('=');
                        if (separatorIndex > 0)
                        {
                            string key = line.Substring(0, separatorIndex).Trim();
                            string value = line.Substring(separatorIndex + 1).Trim();
                            envVars[key] = value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading environment variables: {ex.Message}");
                throw;
            }

            return envVars;
        }
    }
}