using System;

namespace WheelWatcher.Models
{
    internal class WheelResults
    {
        internal string Value;
        internal DateTime Time;
        internal bool Logged;

        internal WheelResults(string value, bool logged = false)
        {
            Value = value;
            Time = DateTime.Now;
            Logged = logged;
        }

        public override string ToString() => $"{Time},{Value}";
    }
}
