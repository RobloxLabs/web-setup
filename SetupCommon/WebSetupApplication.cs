using System.IO;
using System.Text.Json;

namespace SetupCommon
{
    public class WebSetupApplication<ApplicationSettings>
    {
        /// <summary>
        /// The application-wide config.
        /// </summary>
        protected ApplicationSettings Config { get; set; }

        /// <summary>
        /// Gets the application configuration from the designated file in the working directory.
        /// </summary>
        protected void FetchConfig()
        {
            const string configFileName = "appsettings.json";

            if (!File.Exists(configFileName))
                throw new FileNotFoundException($"Application settings file not found for {GetType().Name}.");

            using (FileStream configFileStream = File.OpenRead(configFileName))
            {
                Config = JsonSerializer.Deserialize<ApplicationSettings>(configFileStream);
            }
        }
        
        public WebSetupApplication()
        {
            // Get application settings
            FetchConfig();
        }
    }
}
