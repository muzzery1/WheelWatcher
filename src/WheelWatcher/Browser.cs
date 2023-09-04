using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.Extensions;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace WheelWatcher
{
    /// <summary>
    /// The browser type
    /// </summary>
    public enum BrowserType
    {
        Chrome,
        Edge,
        Firefox,
        IE
    }

    internal class Browser
    {
        public IWebDriver Driver;
        public DirectoryInfo RootDirectory;
        public EventLog EventLog;

        public Browser(DirectoryInfo rootDirectory, EventLog eventLog)
        {
            RootDirectory = rootDirectory ?? throw new ArgumentNullException(nameof(rootDirectory));
            EventLog = eventLog ?? throw new ArgumentNullException(nameof(eventLog));
        }

        /// <summary>
        /// Launches the browser in a maximized window for a URL. Returns the driver created
        /// </summary>
        /// <param name="url">The URL you want to log into</param>
        /// <param name="browserType">The browser type to launch</param>
        /// <param name="driverTimeout">The time out for the driver being used</param>
        /// <param name="retries">The number of times to retry launching the browser</param>
        /// <param name="headlessBrowser">Whether to use a headless browser for those that support it</param>
        public void Launch(string url, BrowserType browserType = BrowserType.Chrome, TimeSpan? driverTimeout = null, int retries = 5, bool headlessBrowser = true)
        {
            if (!driverTimeout.HasValue)
                driverTimeout = new TimeSpan(0, 1, 0);

            var retryCounter = 1;

            //Keep re-trying until the retry count is reached
            while (retryCounter <= retries)
            {
                try
                {
                    //Launch Browser for the URL
                    LaunchBrowserType(browserType, headlessBrowser, driverTimeout.GetValueOrDefault());

                    break;
                }
                catch (Exception exception)
                {
                    EventLog.WriteEntry($"An error occured lanuching the browser:{Environment.NewLine}{Environment.NewLine}{exception}", EventLogEntryType.Error);

                    retryCounter++;
                    if (retryCounter == retries)
                        throw;

                    Thread.Sleep(new TimeSpan(0, 0, 1));
                }
            }

            //Set the timeout
            Driver.Manage().Timeouts().ImplicitWait = driverTimeout.GetValueOrDefault();

            //Maximize the window
            Driver.Manage().Window.Maximize();

            //Navigate to the URL
            Driver.Navigate().GoToUrl(url);
        }

        /// <summary>
        /// Launches the browser type specified
        /// </summary>
        /// <param name="browserType">The browser type to launch</param>
        /// <param name="headlessBrowser">Whether to use a headless browser for those that support it</param>
        private void LaunchBrowserType(BrowserType browserType, bool headlessBrowser, TimeSpan driverTimeout)
        {
            //Launch Browser for the URL
            switch (browserType)
            {
                case BrowserType.Chrome:
                    var service = ChromeDriverService.CreateDefaultService();

                    var options = new ChromeOptions();

                    //Set options to get best performance
                    //See https://docs.browserless.io/docs/dotnet.html
                    options.AddArgument("--disable-background-timer-throttling");
                    options.AddArgument("--disable-backgrounding-occluded-windows");
                    options.AddArgument("--disable-breakpad");
                    options.AddArgument("--disable-component-extensions-with-background-pages");
                    options.AddArgument("--disable-dev-shm-usage");
                    options.AddArgument("--disable-extensions");
                    options.AddArgument("--disable-features=TranslateUI,BlinkGenPropertyTrees");
                    options.AddArgument("--disable-ipc-flooding-protection");
                    options.AddArgument("--disable-renderer-backgrounding");
                    options.AddArgument("--enable-features=NetworkService,NetworkServiceInProcess");
                    options.AddArgument("--force-color-profile=srgb");
                    options.AddArgument("--hide-scrollbars");
                    options.AddArgument("--metrics-recording-only");
                    options.AddArgument("--mute-audio");
                    options.AddArgument("--no-sandbox");

                    //Set the logging preferences so we can extract browser logs
                    options.SetLoggingPreference(LogType.Browser, LogLevel.All);

                    //Add flag for headless if requested
                    if (headlessBrowser)
                    {
                        options.AddArgument("--headless");
                        options.AddArgument("--window-size=1980,960");
                    }

                    Driver = new ChromeDriver(service, options, driverTimeout);
                    return;
                default:
                    throw new Exception(
                        $"The browser type of {browserType} is not supported yet");
            }
        }

        /// <summary>
        /// Captures a screen shot of the driver if it exists, and saves it to the specified directory in JPEG format
        /// </summary>
        public void TakeScreenShot()
        {
            if (Driver == null)
                return;

            try
            {
                var fullFileName = $"{DateTime.Now:yyyyMMddHHmmssfff}.jpeg";

                var screenShot = Driver.TakeScreenshot();
                var filePath = Path.Combine(RootDirectory.FullName, fullFileName);

                screenShot.SaveAsFile(filePath, ScreenshotImageFormat.Jpeg);

                EventLog.WriteEntry($"Screenshot saved under {filePath}", EventLogEntryType.Error);
            }
            catch (Exception exception)
            {
                EventLog.WriteEntry($"Unable to take screenshot{Environment.NewLine}{Environment.NewLine}{exception}", EventLogEntryType.Error);
            }
        }

        /// <summary>
        /// Stops the browser
        /// </summary>
        public void Stop()
        {
            Driver?.Quit();
            Driver?.Dispose();
        }
    }
}