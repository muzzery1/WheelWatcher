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
        private int eventId = 1;
        private Browser Browser;
        private Timer Timer;
        private Dictionary<string, List<WheelResults>> Results;
        private object ResultsLock = new object();

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
            finally
            {
                //Start the timer to ensure this gets called again
                Timer?.Start();
            }
        }

        public void Watch()
        {
            LaunchBrowser();

            CheckResults();
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
            Browser.Driver.Manage().Timeouts().ImplicitWait = new TimeSpan(0, 0, 1);

            while (true)
            {
                var elements = Interactions.GetElementsIfLoaded(eventLog, Browser.Driver, By.XPath(ControlIds.AllWheelTiles_XPath));

                var parellelCheck = new List<Action>();

                foreach (var element in elements)
                    parellelCheck.Add(() => GetResults(element));

                Parallel.Invoke(parellelCheck.ToArray());

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
                Console.WriteLine($"{DateTime.Now} | Title: {wheelTitle} | Initial Results: {string.Join(", ", wheelResults.Select(r => r.Value))}");

                Results.Add(wheelTitle, wheelResults);
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
                    Console.WriteLine($"{DateTime.Now} | Title: {wheelTitle} | Adding Result: {wheelResults[i].Value}");
                    resultsToAdd.Add(wheelResults[i]);
                }

                //If the lists now match, we have found all new numbers
                if (wheelCompare == resultsCompare)
                    break;

                i++;
            }

            //Add all new results
            Results[wheelTitle].InsertRange(0, resultsToAdd);
        }

        protected override void OnStop()
        {
            Browser?.Stop();
            eventLog.WriteEntry("Wheel Watch Stopped");
        }
    }
}
