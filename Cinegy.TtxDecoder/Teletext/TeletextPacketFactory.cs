using System;
using System.Collections.Generic;
using Cinegy.TsDecoder.TransportStream;

namespace Cinegy.TtxDecoder.Teletext
{
    public static class TeletextPacketFactory
    {
        public const byte SizeOfTeletextPayload = 44;

        public static List<TeletextPacket> GetTtxPacketsFromData(Pes pes, PesHdr tsPacketPesHeader)
        {
            var returnedPackets = new List<TeletextPacket>();
            
            if (pes.PacketStartCodePrefix != Pes.DefaultPacketStartCodePrefix || pes.StreamId != (byte)PesStreamTypes.PrivateStream1 ||
                pes.PesPacketLength <= 0) return null;

            ushort startOfTeletextData = 7;

            if (pes.OptionalPesHeader.MarkerBits == 2)
            {
                startOfTeletextData += (ushort)(3 + pes.OptionalPesHeader.PesHeaderLength);
            }
            
            while (startOfTeletextData <= pes.PesPacketLength)
            {
                var ttxPacket = new TeletextPacket(pes.Data[startOfTeletextData], pes.Data[startOfTeletextData + 1], tsPacketPesHeader.Pts);

                if (ttxPacket.DataUnitLength == SizeOfTeletextPayload)
                {
                    var data = new byte[ttxPacket.DataUnitLength + 2];
                    Buffer.BlockCopy(pes.Data, startOfTeletextData, data, 0, ttxPacket.DataUnitLength + 2);
                    ttxPacket.Data = data;
                }

                returnedPackets.Add(ttxPacket.Row == 0 ? new TeletextHeaderPacket(ttxPacket) : ttxPacket);
                
                startOfTeletextData += (ushort)(ttxPacket.DataUnitLength + 2);
            }
            
            return returnedPackets;
        }
    }
}
