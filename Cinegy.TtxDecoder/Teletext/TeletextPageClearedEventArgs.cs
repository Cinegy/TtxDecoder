using System;

namespace Cinegy.TtxDecoder.Teletext
{
    public class TeletextPageClearedEventArgs : EventArgs
    {
        public int PageNumber { get; set; }

        public long Pts { get; set; }

        public TeletextPageClearedEventArgs(int pageNumber, long pts)
        {
            PageNumber = pageNumber;
            Pts = pts;
        }
    }
}
