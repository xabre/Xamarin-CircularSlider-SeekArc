using System;

namespace SeekArc.Droid
{
    public class SeekArcProgressChangedEventArgs : EventArgs
    {
        public SeekArc SeekArc { get; set; }
        public int Progress { get; set; }
        public bool FromUser { get; set; }

        public SeekArcProgressChangedEventArgs(SeekArc seekArc, int progress, bool fromUser)
        {
            SeekArc = seekArc;
            Progress = progress;
            FromUser = fromUser;
        }
    }
}