using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cinegy.TtxDecoder.Teletext
{
    public class TeletextHeaderPacket : TeletextPacket
    {
        public int Page { get; private set; }

        public bool EraseFlag { get; private set; }

        public int Subcode { get; private set; }

        public TeletextHeaderPacket(TeletextPacket basePacket) : base(basePacket)
        {
            if (basePacket.Row != 0)
                throw new InvalidDataException("Non-header packet passed to constructor of header packet class");
            
            Page = (ushort)(Magazine << 8) | (Utils.UnHam84(Data[7]) << 4) + Utils.UnHam84(Data[6]);
            EraseFlag = ((Utils.UnHam84(Data[9]) & 0x08) == 8);

            Subcode = (Utils.UnHam84(Data[11]) << 24) + (Utils.UnHam84(Data[10]) << 16) + (Utils.UnHam84(Data[9]) << 8) + Utils.UnHam84(Data[8]);
        }
    }
}
