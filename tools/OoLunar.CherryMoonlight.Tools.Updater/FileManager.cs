using System;
using System.IO;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using Serilog.Core;

namespace OoLunar.CherryMoonlight.Tools.Updater
{
    public sealed class FileManager
    {
        private static readonly string _cachePath = Path.Join(Path.GetTempPath(), "CherryMoonlight", "ModCache");
        private static readonly string _importPath = Path.Join(_cachePath, "import");

        public static async ValueTask PackModpackAsync(Logger logger)
        {
            // Create the cache directory
            if (Directory.Exists(_cachePath))
            {
                Directory.Delete(_cachePath, true);
            }

            Directory.CreateDirectory(_cachePath);

            // Try to download all the mods as is
            (string output, int exitCode) = await Program.ExecuteProgramAsync("packwiz", $"modrinth export --cache {_cachePath}", logger);
            if (exitCode == 0)
            {
                logger.Information("Successfully exported the modpack");
                return;
            }

            // Download all the mods manually
            logger.Information("Modpack requires mods to be manually downloaded, one moment here...");

            FirefoxProfile profile = new();
            profile.SetPreference("browser.download.folderList", 2);
            profile.SetPreference("browser.download.dir", _importPath);
            profile.SetPreference("browser.helperApps.neverAsk.saveToDisk", "text/csv");

            FirefoxOptions options = new()
            {
                Profile = profile,
                EnableDownloads = true,
                LogLevel = FirefoxDriverLogLevel.Fatal
            };

            options.AddArgument("--headless");
            options.SetLoggingPreference(LogType.Browser, LogLevel.Off);
            options.SetLoggingPreference(LogType.Client, LogLevel.Off);
            options.SetLoggingPreference(LogType.Driver, LogLevel.Off);
            options.SetLoggingPreference(LogType.Profiler, LogLevel.Off);
            options.SetLoggingPreference(LogType.Server, LogLevel.Off);
            options.SetLoggingPreference(LogType.Performance, LogLevel.Off);

            FirefoxDriverService service = FirefoxDriverService.CreateDefaultService();
            service.HideCommandPromptWindow = true;
            service.LogLevel = FirefoxDriverLogLevel.Fatal;
            service.SuppressInitialDiagnosticInformation = true;

            // Create the browser
            FirefoxDriver firefox = new(service, options);
            foreach (string word in output.Split([' ', '\n']))
            {
                // Parse each word until we find a download Url
                if (!word.StartsWith("https://", StringComparison.Ordinal) || !word.Contains("curseforge") || !Uri.IsWellFormedUriString(word, UriKind.Absolute))
                {
                    continue;
                }

                // Replace the /files with /download
                string url = word.Replace("/files", "/download");
                firefox.Navigate().GoToUrl(url);

                // Wait for the file to be fully downloaded
                await Task.Delay(TimeSpan.FromSeconds(10));

                // Let the user know
                logger.Information("Downloaded {Url}", url);
            }

            // Close the browser
            firefox.Dispose();
            service.Dispose();

            // Try again
            (output, exitCode) = await Program.ExecuteProgramAsync("packwiz", $"modrinth export -y --cache {_cachePath}", logger);
            if (exitCode != 0)
            {
                logger.Fatal("Failed to export the modpack: {Output}", output);
            }
            else
            {
                logger.Information("Successfully exported the modpack");
            }
        }
    }
}