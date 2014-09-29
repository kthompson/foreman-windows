using System;

namespace Foreman
{
    class ProcfileEventArgs : EventArgs
    {
        public ProcfileEventArgs(string name, string text, DateTime time)
        {
            this.Name = name;
            this.Text = text;
            this.Time = time;
        }

        public string Name { get; private set; }
        public string Text { get; private set; }
        public DateTime Time { get; private set; }
    }
}