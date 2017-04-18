using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
