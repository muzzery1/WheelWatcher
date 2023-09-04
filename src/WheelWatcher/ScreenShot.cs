using System;

namespace WheelWatcher
{
    internal class ScreenShot
    {
        public string Path { get; }

        public DateTime TimeStamp { get; }

        /// <summary>
        /// Create a new instance of a <see cref="ScreenShot"/>
        /// </summary>
        /// <param name="path">The path to teh screen shot</param>
        public ScreenShot(string path)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
            TimeStamp = DateTime.UtcNow;
        }
    }
}
