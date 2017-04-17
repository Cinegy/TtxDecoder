using System;
using System.Collections.Generic;
using Cinegy.TtxDecoder.Teletext;

namespace Cinegy.TtxDecoder.Metrics
{
    public class TeletextMetric : Metric
    {
        private int _periodTtxPacketCount;

        internal override void ResetPeriodTimerCallback(object o)
        {
            lock (this)
            {
                PeriodTtxPacketCount = _periodTtxPacketCount;
                _periodTtxPacketCount = 0;
                
                base.ResetPeriodTimerCallback(o);
            }
        }

        public long TtxPacketCount { get; set; }

        public int PeriodTtxPacketCount { get; private set; }

        public Dictionary<int,long> PagePacketCounts {
            get;
            set;
        } = new Dictionary<int, long>(256);

        public void AddPacket(TeletextPacket newPacket)
        {
            try
            {
                TtxPacketCount++;
                _periodTtxPacketCount++;
 
                if(!PagePacketCounts.ContainsKey(newPacket.Magazine)) PagePacketCounts.Add(newPacket.Magazine,0);

                PagePacketCounts[newPacket.Magazine]++;
            }
            catch (Exception ex)
            {
               // Debug.WriteLine("Exception generated within AddPacket method: " + ex.Message);
            }
        }
    }
}
