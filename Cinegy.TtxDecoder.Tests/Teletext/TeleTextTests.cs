using System;
using Cinegy.TsDecoder.TransportStream;
using Cinegy.TtxDecoder.Teletext;
using System.IO;
using NUnit.Framework;

namespace Cinegy.TtxDecoder.Tests.Teletext
{
    [TestFixture]
    public class TeletextTests
    {
        [TestCase(@"TestStreams\Teletext-bars-1mbps-20170125-095714.ts")]
        [TestCase(@"TestStreams\PID_0x0163.bin")]
        public void DecodeTeletextDataTest(string fileName)
        {
            var tsDecoder = new TsDecoder.TransportStream.TsDecoder();
            var ttxDecoder = new TeletextDecoder();
            var factory = new TsPacketFactory();

            ttxDecoder.Service.TeletextPageReady += ServiceTeletextPageReady;
            ttxDecoder.Service.TeletextPageCleared += Service_TeletextPageCleared;

            var lastPts = 0L;
            
            const int readFragmentSize = 1316;

            using (var stream = File.OpenRead(Path.Combine(TestContext.CurrentContext.TestDirectory, fileName)))
            {
                Console.WriteLine($"Reading test file: {fileName}");
                var data = new byte[readFragmentSize];

                var readCount = stream.Read(data, 0, readFragmentSize);

                if (fileName.EndsWith(".bin"))
                {
                    //resource provided is a stripped BIN without TS tables - override setup with explicit values
                    ttxDecoder.Setup(8, 1, 355);
                }

                while (readCount > 0)
                {
                    var tsPackets = factory.GetTsPacketsFromData(data);

                    if (tsPackets == null) break;

                    foreach (var tsPacket in tsPackets)
                    {
                        if (tsPacket.Pid == ttxDecoder.Service?.TeletextPid && tsPacket.PesHeader.Pts != 0)
                        {
                            if (tsPacket.PesHeader.Pts < lastPts)
                            {
                                Console.WriteLine($"Backward PTS - New PTS: {tsPacket.PesHeader.Pts}, Last PTS: {lastPts} ");
                            }

                            lastPts = tsPacket.PesHeader.Pts;
                        }

                        tsDecoder.AddPacket(tsPacket);
                        ttxDecoder.AddPacket(tsPacket, tsDecoder);
                    }

                    if (stream.Position < stream.Length)
                    {
                        readCount = stream.Read(data, 0, readFragmentSize);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            //if (ttxDecoder.Service?.Metric == null) return;

            //Console.WriteLine($"Finished - Total TTX Packets: {ttxDecoder.Service.Metric.TtxPacketCount}");
            
            //foreach (var teletextMetricPagePacketCount in ttxDecoder.Service?.Metric.PagePacketCounts)
            //{
              //  Console.WriteLine($"Magazine: {teletextMetricPagePacketCount.Key}, Count: {teletextMetricPagePacketCount.Value}");
           // }
        }

        private static void Service_TeletextPageCleared(object sender, EventArgs e)
        {
            if (!(e is TeletextPageClearedEventArgs ttxEventArgs)) return;

            var timeStamp = new TimeSpan(0, 0, 0, 0, (int)(ttxEventArgs.Pts / 90));
            
            Console.WriteLine($"{timeStamp:hh\\:mm\\:ss\\.ff} [{ttxEventArgs.PageNumber}] - CLEAR");
            
        }

        private static void ServiceTeletextPageReady(object sender, EventArgs e)
        {
            if(!(e is TeletextPageReadyEventArgs ttxEventArgs)) return;

            var timeStamp = new TimeSpan(0, 0, 0, 0, (int)(ttxEventArgs.Page.Pts / 90));

            foreach (var row in ttxEventArgs.Page.Rows)
            {
                if(row.IsChanged() && !string.IsNullOrWhiteSpace(row.GetPlainRow()))    
                    Console.WriteLine($"{timeStamp:hh\\:mm\\:ss\\.ff} [{ttxEventArgs.Page.PageNum}] ({row.RowNum}): {row.GetPlainRow()}");
            }
        }
    }
}