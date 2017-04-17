using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cinegy.TtxDecoder.Teletext
{
    public class TeletextMagazine
    {
        private int _currentPageNumber = -1;

        public TeletextService ParentService { get; set; }
        
        public void AddPacket(TeletextPacket packet)
        { 
            var relPts = packet.Pts - ParentService.ReferencePts;

            var timeStamp = new TimeSpan(0, 0, 0, 0, (int)(relPts / 90));

            //ETS 300 706, 9.3.1
            if (packet.Row == 0)
            {
                //row 0 is the 'header' row, and has some extra control non-display data at start of the data
                _currentPageNumber = (ushort)((packet.Magazine << 8) | (Utils.UnHam84(packet.Data[7]) << 4) + Utils.UnHam84(packet.Data[6]));

                var eraseFlag = ((Utils.UnHam84(packet.Data[9]) & 0x08) == 8);

                if (_currentPageNumber != 0x8EE && _currentPageNumber != 0x8FF && eraseFlag)
                {
                    Console.WriteLine($"{timeStamp:g} TTX Header Mag: {packet.Magazine}, Page: {_currentPageNumber}, Clear: {eraseFlag}");
                    
                }


                return;
            }

            if (packet.Row < 24)
            {
                Console.WriteLine($"{timeStamp:g} TTX Row: {packet.Row}, Mag: {packet.Magazine}");

                var row = "";
                for (var x = 0; x < 40; x++)
                {
                    var c = (char) Utils.ParityChar(packet.Data[6 + x]);
                    if (c == '\0')
                    {
                        row += " ";
                    }
                    else
                    {
                        row += c;
                    }
                }
                Console.WriteLine(row);
            }
        }
    }
}
