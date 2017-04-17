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
            List<string> resources = new List<string>();

            //resources.Add("Cinegy.TtxDecoder.Tests.TestStreams.Teletext-bars-1mbps-20170125-095714.ts");
            resources.Add("Cinegy.TtxDecoder.Tests.TestStreams.PID_0x0163.bin");

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

            var lastPts = 0L;

          //  ttxDecoder.TeletextPageAdded += TtxDecoder_TeletextPageAdded;
            //load some data from test file
            var assembly = Assembly.GetExecutingAssembly();
            
            const int readFragmentSize = 1316;

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
                        if (tsPacket.Pid == ttxDecoder.TeletextService?.TeletextPid && tsPacket.PesHeader.Pts != 0)
                        {
                            if (tsPacket.PesHeader.Pts < lastPts)
                            {
                                Console.WriteLine($"Backward PTS - New PTS: {tsPacket.PesHeader.Pts}, Last PTS: {lastPts} ");
                            }

                            lastPts = tsPacket.PesHeader.Pts;
                        }

                        tsDecoder.AddPacket(tsPacket);
                        ttxDecoder.AddPacket(tsDecoder, tsPacket);
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

            Console.WriteLine($"Finsihed - Total TTX Packets: {ttxDecoder.TeletextService.Metric.TtxPacketCount}");

            foreach (var teletextMetricPagePacketCount in ttxDecoder.TeletextService.Metric.PagePacketCounts)
            {
                Console.WriteLine($"Magazine: {teletextMetricPagePacketCount.Key}, Count: {teletextMetricPagePacketCount.Value}");
            }
        }

        private static void TtxDecoder_TeletextPageAdded(object sender, EventArgs e)
        {
            var decoder = (TeleTextDecoder) sender;
            var teletextArgs = (TeleTextSubtitleEventArgs)e;

            if (teletextArgs.Pts < _lastPTS)
            {
                Console.WriteLine($"Page Backward PTS - New PTS: {teletextArgs.Pts}, Last PTS: {_lastPTS}, Delta: {teletextArgs.Pts - _lastPTS} ");
            }

            _lastPTS = teletextArgs.Pts;

            Console.WriteLine($"Page Rcvd - Page Num:{teletextArgs.PageNumber:X}, Service ID: {decoder.ProgramNumber}, PID: {teletextArgs.Pid:X}, Lines: {teletextArgs.Page.Count()}, PTS: {teletextArgs.Pts}");

            var lineCtr = 0;
            foreach (var line in teletextArgs.Page)
            {
                lineCtr++;
                if (IsNullOrEmpty(line) || IsNullOrEmpty(line.Trim())) continue;
                Console.WriteLine($"Line {lineCtr}: {new string(line.Where(c => !char.IsControl(c)).ToArray())}");
            }
        }
    }
}