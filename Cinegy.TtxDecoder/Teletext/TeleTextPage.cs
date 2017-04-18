using System;

namespace Cinegy.TtxDecoder.Teletext
{
    public class TeletextPage
    {
        public TeletextMagazine ParentMagazine { get; set; }

        public int PageNum { get; set; }

        public long Pts { get; set; }

        public TeletextRow[] Rows { get; set; } = new TeletextRow[25];

        public TeletextPage()
        {
            Clear();
        }

        public void Clear()
        {
            for (var y = 0; y < Rows.Length; y++)
            {
                Rows[y] = new TeletextRow() { RowNum = y};
            }
        }
        
        public void AddPacket(TeletextPacket packet)
        {
            Pts = packet.Pts;

            var relPts = packet.Pts - ParentMagazine.ParentService.ReferencePts;

           // var timeStamp = new TimeSpan(0, 0, 0, 0, (int)(relPts / 90));

            if (packet.Row >= 24) return;
            for (var x = 0; x < 40; x++)
            {
                var c = (char)Utils.ParityChar(packet.Data[6 + x]);
                if (c == '\0') c = ' ';
                    
                Rows[packet.Row].SetChar(x,c);
            }

            //Console.WriteLine($"{timeStamp:hh\\:mm\\:ss\\.ff} ({packet.Row}): {Rows[packet.Row].GetPlainRow()}");
        }

    }
}
