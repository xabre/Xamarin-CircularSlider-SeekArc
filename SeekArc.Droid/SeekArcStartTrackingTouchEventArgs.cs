using System;

namespace SeekArc.Droid
{
    public class SeekArcTrackingTouchEventArgs :EventArgs
    {
        public SeekArc SeekArc { get; set; }

        public SeekArcTrackingTouchEventArgs(SeekArc seekArc)
        {
            SeekArc = seekArc;
        }
    }
}