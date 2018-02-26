using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cinegy.TsDecoder.TransportStream;

namespace Cinegy.TtxDecoder.Teletext
{
    public class TeletextService
    {
        //TODO: Actually support differences between parallel and serial...
        public const byte TransmissionModeParallel = 0;
        public const byte TransmissionModeSerial = 1;
        
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
        /// The Program Number ID to which the selected teletext PID belongs, if any
        /// </summary>
        public ushort ProgramNumber { get; set; } = 0;

        /// <summary>
        /// The associated TeletextDescriptor for the service, if any
        /// </summary>
        public TeletextDescriptor AssociatedDescriptor { get; set; }

        /// <summary>
        /// Optional value to restrict decoded pages to the specified magazine (reduces callback events)
        /// </summary>
        public int MagazineFilter { get; set; } = -1;
        
        //TODO: Implement
        /// <summary>
        /// Optional value to restrice decodec packets to the specified page number (tens and units element only - reduces callback events)
        /// </summary>
        public int PageFilter { get; set; } = -1;

        //TODO: Implement
        /// <summary>
        /// If true, only pages marked with the 'subtitle' control field will be returned via events
        /// </summary>
        public bool SubtitleFilter { get; set; }

        /// <summary>
        /// A Dictionary of Teletext Magazines, which themselves may contain a collection of pages
        /// </summary>
        public Dictionary<int, TeletextMagazine> Magazines { get; set; } = new Dictionary<int, TeletextMagazine>(9);
        
        public void AddData(Pes pes, PesHdr tsPacketPesHeader)
        {
            //update / store any reference PTS for displaying easy relative values
            if (ReferencePts == 0) ReferencePts = tsPacketPesHeader.Pts;
            if (ReferencePts > 0 && tsPacketPesHeader.Pts < ReferencePts) ReferencePts = tsPacketPesHeader.Pts;

            var ttxPackets = TeletextPacketFactory.GetTtxPacketsFromData(pes, tsPacketPesHeader);

            if (ttxPackets == null) return;

            OnTeletextPacketsReady(ttxPackets);

            foreach (var ttxPacket in ttxPackets)
            {
                AddPacketToService(ttxPacket);
            }

        }
        

        private void AddPacketToService(TeletextPacket packet)
        {
            if (packet.Row == 30)
            {
                //this is a service-wide enhancement packet 
                //TODO: This
            }

            //if packet was not service-wide, add to magazines:
            AddPacketToMagazines(packet);
        }

        private void AddPacketToMagazines(TeletextPacket packet)
        {
            if (MagazineFilter > -1)
                if (packet.Magazine != MagazineFilter) return; //magazine does not match filter, so skip processing

            //add this packet to the magazines and their pages associated with this service
            if (!Magazines.ContainsKey(packet.Magazine)) Magazines.Add(packet.Magazine, new TeletextMagazine(packet.Magazine) { ParentService = this });

            Magazines[packet.Magazine].AddPacket(packet);
        }

        public event EventHandler TeletextPageReady;

        public event EventHandler TeletextPageCleared;

        public event EventHandler TeletextPacketsReady;

        internal virtual void OnTeletextPageReady(TeletextPage page)
        {
            TeletextPageReady?.Invoke(this, new TeletextPageReadyEventArgs(page));
        }

        internal virtual void OnTeletextPageCleared(int pageNumber, long pts)
        {
            TeletextPageCleared?.Invoke(this, new TeletextPageClearedEventArgs(pageNumber, pts));
        }

        internal virtual void OnTeletextPacketsReady(List<TeletextPacket> packets)
        {
            TeletextPacketsReady?.Invoke(this, new TeletextPacketsReadyEventArgs(packets));
        }


    }
}
