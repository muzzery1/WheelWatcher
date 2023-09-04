using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using System.Timers;
using WheelWatcher.Library;
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

            RootDirectory = Directory.CreateDirectory(RootDirectoryPath);
        }

        public void OnTimer(object sender, ElapsedEventArgs args)
        {
            //Make sure it is only called once.
            Timer?.Stop();

            try
            {
                if (Browser == null)
                {
                    Browser = new Browser(RootDirectory, eventLog);
                    Browser.Launch("https://casino.betfair.com/c/live-roulette");
                }

                CheckResults();
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

        private void CheckResults()
        {
            while (true)
            {
                var numbers = new List<string>();

                //TODO See why timeout occurs
                var elements = Interactions.GetElementsIfLoaded(eventLog, Browser.Driver, By.XPath(ControlIds.AllWheelTiles_XPath));

                foreach (var element in elements)
                {
                    var title = element.FindElement(By.XPath(ControlIds.WheelTitle_XPath));
                    var results = element.FindElements(By.XPath(ControlIds.WheelResults_XPath));

                    if (title == null || results == null)
                        continue;

                    if (!results.Any(r => string.IsNullOrWhiteSpace(r?.Text)) && !string.IsNullOrWhiteSpace(title.Text))
                        numbers.Add($"Title: {title.Text}, Results: {string.Join(", ", results.Select(r => r.Text))}");
                }

                //TODO Write to CSV file
                eventLog.WriteEntry(string.Join(Environment.NewLine, numbers), EventLogEntryType.Information, eventId++);

                Thread.Sleep(1000);
            }
        }

        protected override void OnStop()
        {
            Browser?.Stop();
            eventLog.WriteEntry("Wheel Watch Stopped");
        }
    }
}
