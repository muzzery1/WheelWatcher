using WheelWatcher;

namespace WheelWatcherRunner
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var watcher = new WheelWatch();

                watcher.Watch();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }
    }
}