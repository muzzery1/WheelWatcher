using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using WheelWatcher.Library;
using WheelWatcher.Models;
using Timer = System.Timers.Timer;

namespace WheelWatcher
{
    public partial class WheelWatch : ServiceBase
    {
        private const string EventSourceName = "WheelWatchSource";
        private const string EventLogName = "WheelWatchLog";
        private const string RootDirectoryPath = @"C:\WheelWatcher";
        private DirectoryInfo RootDirectory;
        private Browser Browser;
        private Timer Timer;
        private Dictionary<string, List<WheelResults>> Results;
        private object ResultsLock = new object();
        private object RunningLock = new object();
        private bool Running;

        public WheelWatch()
        {
            InitializeComponent();
            eventLog = new EventLog();
            if (!EventLog.SourceExists(EventSourceName))
            {
                EventLog.CreateEventSource(EventSourceName, EventLogName);
            }
            eventLog.Source = EventSourceName;
            eventLog.Log = EventLogName;

            RootDirectory = Directory.CreateDirectory(RootDirectoryPath);

            Results = new Dictionary<string, List<WheelResults>>();

            ReadLogResults();
        }

        protected override void OnStart(string[] args)
        {
            eventLog.WriteEntry("Wheel Watch Started");

            Timer = new Timer
            {
                Interval = 1
            };
            Timer.Elapsed += new ElapsedEventHandler(OnTimer);
            Timer.Start();
        }

        private void ReadLogResults()
        {
            var files = Directory.EnumerateFiles(RootDirectory.FullName, "*.csv", SearchOption.TopDirectoryOnly);

            foreach (var file in files)
            {
                string wheelTitle = file.Substring(file.LastIndexOf(@"\") + 1, file.IndexOf(".csv") - file.LastIndexOf(@"\") - 1);

                Console.WriteLine($"{DateTime.Now} | Wheel Title: {wheelTitle} | Caching current contents from {file}");
                var contents = File.ReadAllLines(file).ToList();

                contents.Reverse();

                Results.Add(wheelTitle,
                    contents.Select(c => new WheelResults(c.Substring(c.IndexOf(",") + 1), true)).ToList());
            }
        }

        public void OnTimer(object sender, ElapsedEventArgs args)
        {
            //Make sure it is only called once.
            Timer?.Stop();

            try
            {
                Watch();
            }
            catch (Exception exception)
            {
                Browser?.TakeScreenShot();

                eventLog.WriteEntry(exception.ToString(), EventLogEntryType.Error);
            }
        }

        public void Watch()
        {
            LaunchBrowser();

            CheckResults();
        }

        private void WriteResults(string wheelTitle)
        {
            var path = $@"{RootDirectory.FullName}\{wheelTitle}.csv";

            try
            {
                using (var streamWriter = File.AppendText(path))
                {
                    //Log them backwards so the most recent one is at the bottom
                    foreach (var wheelResult in Results[wheelTitle].Where(r => !r.Logged).Reverse())
                    {
                        streamWriter.WriteLine(wheelResult);
                        wheelResult.Logged = true;
                    }
                }

                Console.WriteLine($"{DateTime.Now} | Wheel Title: {wheelTitle} | Written results to path {path}");
            }
            catch (Exception exception)
            {
                var message = $"An error occured writing the results to {path}:{Environment.NewLine}{Environment.NewLine}{exception}";
                eventLog.WriteEntry(message, EventLogEntryType.Error);
                Console.WriteLine(message);
            }
        }

        private void LaunchBrowser()
        {
            if (Browser == null)
            {
                Browser = new Browser(RootDirectory, eventLog);
                Browser.Launch("https://casino.betfair.com/c/live-roulette");
            }
        }

        private void CheckResults()
        {
            SetRunningState(true);
            Browser.Driver.Manage().Timeouts().ImplicitWait = new TimeSpan(0, 0, 1);

            while (Running)
            {
                lock (RunningLock)
                {
                    var elements = Interactions.GetElementsIfLoaded(eventLog, Browser.Driver, By.XPath(ControlIds.AllWheelTiles_XPath));

                    var parellelCheck = new List<Action>();

                    foreach (var element in elements)
                        parellelCheck.Add(() => GetResults(element));

                    Parallel.Invoke(parellelCheck.ToArray());
                }

                Thread.Sleep(new TimeSpan(0, 0, 1));
            }
        }

        private void GetResults(IWebElement element)
        {
            var title = Interactions.GetElementIfLoaded(eventLog, Browser.Driver, element, By.XPath(ControlIds.WheelTitle_XPath));
            var results = GetCorrectWheelResults(element, title.Text);

            if (title == null || results == null)
            {
                Console.WriteLine($"No numbers found for wheel {title?.Text}");
                return;
            }

            if (!results.Any(r => string.IsNullOrWhiteSpace(r?.Value)) && !string.IsNullOrWhiteSpace(title.Text))
            {
                var wheelResults = results;

                lock (ResultsLock)
                    LogResults(title.Text, wheelResults);
            }
        }

        private List<WheelResults> GetCorrectWheelResults(IWebElement element, string wheelTitle)
        {
            //In some cases, during the numbers updating on screen, the same number can be retunred for 2 elements
            //This method gets the values twice, and if they do not match then the update has occured and we need to try again

            var resultsAttempt1 = Interactions.GetElementsIfLoaded(eventLog, Browser.Driver, element, By.XPath(ControlIds.WheelResults_XPath))
                .Select(r => new WheelResults(r.Text)).ToList();
            var resultsAttempt2 = Interactions.GetElementsIfLoaded(eventLog, Browser.Driver, element, By.XPath(ControlIds.WheelResults_XPath))
                .Select(r => new WheelResults(r.Text)).ToList();

            var resultsAttempt1String = string.Join(",", resultsAttempt1.Select(w => w.Value));
            var resultsAttempt2String = string.Join(",", resultsAttempt2.Select(w => w.Value));

            if (resultsAttempt1String != resultsAttempt2String)
                return GetCorrectWheelResults(element, wheelTitle);

            return resultsAttempt1;
        }

        private void LogResults(string wheelTitle, List<WheelResults> wheelResults)
        {
            if (string.IsNullOrWhiteSpace(wheelTitle) || wheelResults == null)
                return;

            if (!Results.ContainsKey(wheelTitle))
            {
                Console.WriteLine($"{DateTime.Now} | Wheel Title: {wheelTitle} | Initial Cached Results: {string.Join(", ", wheelResults.Select(r => r.Value))}");

                Results.Add(wheelTitle, wheelResults);
                WriteResults(wheelTitle);
                return;
            }

            LogNewResults(wheelTitle, wheelResults);
        }

        private void LogNewResults(string wheelTitle, List<WheelResults> wheelResults)
        {
            if (string.IsNullOrWhiteSpace(wheelTitle) || wheelResults == null)
                return;

            var resultsToAdd = new List<WheelResults>();
            var i = 0;

            while (i < wheelResults.Count)
            {
                var wheelCompare = string.Join(",", wheelResults.Skip(i).Select(w => w.Value));
                var resultsCompare = string.Join(",", Results[wheelTitle].Take(wheelResults.Count - i).Select(w => w.Value));

                //If the full list macthes, no need to continue
                if (i == 0 && wheelCompare == resultsCompare)
                    return;

                //This value is new, so log it
                if (wheelCompare != resultsCompare)
                {
                    Console.WriteLine($"{DateTime.Now} | Wheel Title: {wheelTitle} | Caching Result: {wheelResults[i].Value}");
                    resultsToAdd.Add(wheelResults[i]);
                }

                //If the lists now match, we have found all new numbers
                if (wheelCompare == resultsCompare)
                    break;

                i++;
            }

            //Add all new results
            Results[wheelTitle].InsertRange(0, resultsToAdd);
            WriteResults(wheelTitle);
        }

        private void SetRunningState(bool state)
        {
            lock (RunningLock)
                Running = state;
        }

        protected override void OnStop()
        {
            eventLog.WriteEntry("Wheel Watch Stopping");
            SetRunningState(false);
            Browser?.Stop();
            Browser = null;
            eventLog.WriteEntry("Wheel Watch Stopped");
        }
    }
}
