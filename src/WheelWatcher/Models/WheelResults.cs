using System;

namespace WheelWatcher.Models
{
    internal class WheelResults
    {
        internal string Value;
        internal DateTime Time;

        internal WheelResults(string value)
        {
            Value = value;
            Time = DateTime.Now;
        }
    }
}
