using System;
using System.Linq;
using System.Reflection;
using Cinegy.TsDecoder.TransportStream;
using Cinegy.TtxDecoder.Teletext;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static System.String;
using System.Collections.Generic;

namespace Cinegy.TtxDecoder.Tests.Teletext
{
    [TestClass()]
    public class TeleTextTests
    {
        private static long _lastPTS = 0;

        [TestMethod()]
        public void DecodeTeletextDataTest()
        {
            var resources = new List<string>
            {
                "Cinegy.TtxDecoder.Tests.TestStreams.Teletext-bars-1mbps-20170125-095714.ts",
                "Cinegy.TtxDecoder.Tests.TestStreams.PID_0x0163.bin"
            };
            
            foreach(var resource in resources)
            {
                LoadTestFileCheckText(resource);
            }


        }

        private void LoadTestFileCheckText(string resourceName)
        {
            var tsDecoder = new TsDecoder.TransportStream.TsDecoder();
            var ttxDecoder = new TeleTextDecoder();
            var factory = new TsPacketFactory();

            ttxDecoder.Service.TeletextPageReady += ServiceTeletextPageReady;
            ttxDecoder.Service.TeletextPageCleared += Service_TeletextPageCleared;

            var lastPts = 0L;
            
            //load some data from test file
            var assembly = Assembly.GetExecutingAssembly();
            
            const int readFragmentSize = 1316;

            //using (var stream = System.IO.File.OpenRead(resourceName))

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null) Assert.Fail("Unable to read test resource: " + resourceName);

                Console.WriteLine($"Reading test resource: {resourceName}");
                var data = new byte[readFragmentSize];

                var readCount = stream.Read(data, 0, readFragmentSize);

                if (resourceName.EndsWith(".bin"))
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

            if (ttxDecoder.Service?.Metric == null) return;

            Console.WriteLine($"Finsihed - Total TTX Packets: {ttxDecoder.Service.Metric.TtxPacketCount}");
            
            foreach (var teletextMetricPagePacketCount in ttxDecoder.Service?.Metric.PagePacketCounts)
            {
                Console.WriteLine($"Magazine: {teletextMetricPagePacketCount.Key}, Count: {teletextMetricPagePacketCount.Value}");
            }
        }

        private void Service_TeletextPageCleared(object sender, EventArgs e)
        {
            var ttxEventArgs = e as TeletextPageClearedEventArgs;
            if (ttxEventArgs == null) return;

            var timeStamp = new TimeSpan(0, 0, 0, 0, (int)(ttxEventArgs.Pts / 90));
            
            Console.WriteLine($"{timeStamp:hh\\:mm\\:ss\\.ff} [{ttxEventArgs.PageNumber}] - CLEAR");
            
        }

        private void ServiceTeletextPageReady(object sender, EventArgs e)
        {
            var ttxEventArgs = e as TeleTextPageReadyEventArgs;

            if(ttxEventArgs==null) return;

            var timeStamp = new TimeSpan(0, 0, 0, 0, (int)(ttxEventArgs.Page.Pts / 90));

            foreach (var row in ttxEventArgs.Page.Rows)
            {
                if(row.IsChanged() && !IsNullOrWhiteSpace(row.GetPlainRow()))    
                    Console.WriteLine($"{timeStamp:hh\\:mm\\:ss\\.ff} [{ttxEventArgs.Page.PageNum}] ({row.RowNum}): {row.GetPlainRow()}");
            }
        }
    }
}