using System;
using System.Collections.Generic;
using System.Diagnostics;
using Cinegy.TtxDecoder.Teletext;

namespace Cinegy.TtxDecoder.Metrics
{
    public class TeletextMetric : Telemetry.Metrics.Metric
    {
        private int _periodTtxPacketCount;
        private int _periodTtxPageReadyCount;
        private int _periodTtxPageClearCount;

        public TeletextMetric(TeletextService service)
        {
            service.TeletextPageCleared += Service_TeletextPageCleared;
            service.TeletextPageReady += Service_TeletextPageReady;
        }

        private void Service_TeletextPageReady(object sender, EventArgs e)
        {
            _periodTtxPageReadyCount++;
            TtxPageReadyCount++;
        }

        private void Service_TeletextPageCleared(object sender, EventArgs e)
        {
            _periodTtxPageClearCount++;
            TtxPageClearCount++;
        }

        protected override void ResetPeriodTimerCallback(object o)
        {
            lock (this)
            {
                PeriodTtxPacketCount = _periodTtxPacketCount;
                _periodTtxPacketCount = 0;

                PeriodTtxPageClearCount = _periodTtxPageClearCount;
                _periodTtxPageClearCount = 0;

                PeriodTtxPageReadyCount = _periodTtxPageReadyCount;
                _periodTtxPageReadyCount = 0;
                
                base.ResetPeriodTimerCallback(o);
            }
        }

        public long TtxPacketCount { get; set; }

        public int PeriodTtxPacketCount { get; private set; }

        public long TtxPageReadyCount { get; private set; }

        public long PeriodTtxPageReadyCount { get; private set; }

        public long TtxPageClearCount { get; private set; }

        public long PeriodTtxPageClearCount { get; private set; }

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
                Debug.WriteLine("Exception generated within AddPacket method: " + ex.Message);
            }
        }

     
    }
}
