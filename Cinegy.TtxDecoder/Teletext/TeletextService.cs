using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cinegy.TsDecoder.TransportStream;
using Cinegy.TtxDecoder.Metrics;

namespace Cinegy.TtxDecoder.Teletext
{
    public class TeletextService
    {
        public const byte TransmissionModeParallel = 0;
        public const byte TransmissionModeSerial = 1;
        public const byte SizeOfTeletextPayload = 44;


        //private Dictionary<ushort, TeleTextSubtitlePage> _teletextSubtitlePages;

        /// <summary>
        /// Byte indicating the TransmissionMode of the teletext pages (serial or parrallel)
        /// </summary>
        public byte TransmissionMode { get; set; }

        /// <summary>
        /// Reference PTS, used to calulate and display relative time offsets for data within stream
        /// </summary>
        public long ReferencePts { get; set; }

        /// <summary>
        /// The TS Packet ID that has been selected as the elementary stream containing teletext data
        /// </summary>
        public short TeletextPid { get; set; } = -1;

        /// <summary>
        /// Optional value to restrict decoded pages to the specified magazine (reduces callback events)
        /// </summary>
        public int MagazineFilter { get; set; } = -1;


        /// <summary>
        /// Optional value to restrice decodec packets to the specified page number (reduces callback events)
        /// </summary>
        public int PageFilter { get; set; } = -1;

        /// <summary>
        /// Analysis object, used for collecting cumulative and periodic data about teletext service
        /// </summary>
        public TeletextMetric Metric { get; set; }
        
        /// <summary>
        /// A Dictionary of Teletext Magazines, which themselves may contain a collection of pages
        /// </summary>
        public Dictionary<int,TeletextMagazine> Magazines { get; set; } = new Dictionary<int,TeletextMagazine>(9);

        /// <summary>
        /// A Dictionary of decoded Teletext pages, where each page contains a number of strings
        /// </summary>
        //public Dictionary<ushort, string[]> TeletextDecodedSubtitlePage { get; } = new Dictionary<ushort, string[]>();

        public TeletextService()
        {
           // _teletextSubtitlePages = new Dictionary<ushort, TeleTextSubtitlePage>();
            Metric = new TeletextMetric();
        }
        
        public bool AddData(Pes pes, PesHdr tsPacketPesHeader)
        {
                if (pes.PacketStartCodePrefix != Pes.DefaultPacketStartCodePrefix || pes.StreamId != Pes.PrivateStream1 ||
                    pes.PesPacketLength <= 0) return false;

                ushort startOfTeletextData = 7;

                if (pes.OptionalPesHeader.MarkerBits == 2)
                {
                    startOfTeletextData += (ushort)(3 + pes.OptionalPesHeader.PesHeaderLength);
                } 
                
                //update / store any reference PTS for displaying easy relative values
                if (ReferencePts == 0) ReferencePts = tsPacketPesHeader.Pts;
                if ((ReferencePts > 0) && (tsPacketPesHeader.Pts < ReferencePts)) ReferencePts = tsPacketPesHeader.Pts;


                while (startOfTeletextData <= pes.PesPacketLength)
                {
                    var ttxPacket = new TeletextPacket(pes.Data[startOfTeletextData], pes.Data[startOfTeletextData + 1], tsPacketPesHeader.Pts);

                    if (ttxPacket.DataUnitLength == SizeOfTeletextPayload)
                    {
                        var data = new byte[ttxPacket.DataUnitLength + 2];
                        Buffer.BlockCopy(pes.Data, startOfTeletextData, data, 0, ttxPacket.DataUnitLength + 2);

                        ttxPacket.Data = data;

                        Metric.AddPacket(ttxPacket);

                        AddPacketToService(ttxPacket);
                    }

                    startOfTeletextData += (ushort)(ttxPacket.DataUnitLength + 2);
                }
                return false;            
        }

        private void AddPacketToService(TeletextPacket packet)
        { 
            //TODO: check for any service-wide packets here
            
            
            //if packet was not service-wide, add to magazines:
            AddPacketToMagazines(packet);  
        }

        private void AddPacketToMagazines(TeletextPacket packet)
        {
            if(MagazineFilter>-1)
                if (packet.Magazine != MagazineFilter) return; //magazine does not match filter, so skip processing

            //add this packet to the magazines and their pages associated with this service
            if (!Magazines.ContainsKey(packet.Magazine)) Magazines.Add(packet.Magazine, new TeletextMagazine {ParentService =  this});
            
            Magazines[packet.Magazine].AddPacket(packet);
        }
    }
}
