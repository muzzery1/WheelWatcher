using System.Timers;
using WheelWatcher;

namespace WheelWatcherRunner
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var watcher = new WheelWatch();

            watcher.OnTimer(new object(), new EventArgs() as ElapsedEventArgs);
        }
    }
}