using System;
using System.Linq;
using System.Reflection;
using Cinegy.TsDecoder.TransportStream;
using Cinegy.TtxDecoder.Teletext;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static System.String;

namespace Cinegy.TtxDecoder.Tests.Teletext
{
    [TestClass()]
    public class TeleTextTests
    {
        [TestMethod()]
        public void DecodeTeletextDataTest()
        {
            const string resourceName = "Cinegy.TtxDecoder.Tests.TestStreams.Teletext-bars-1mbps-20170125-095714.ts";
            LoadTestFileCheckText(resourceName);
        }

        private void LoadTestFileCheckText(string resourceName)
        {
            var tsDecoder = new TsDecoder.TransportStream.TsDecoder();
            var ttxDecoder = new TeleTextDecoder();
            var factory = new TsPacketFactory();

            ttxDecoder.TeletextPageAdded += TtxDecoder_TeletextPageAdded;
            //load some data from test file
            var assembly = Assembly.GetExecutingAssembly();
            
            const int readFragmentSize = 1316;

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null) Assert.Fail("Unable to read test resource: " + resourceName);
                
                var data = new byte[readFragmentSize];

                var readCount = stream.Read(data, 0, readFragmentSize);

                while (readCount > 0)
                {
                    var tsPackets = factory.GetTsPacketsFromData(data);

                    if (tsPackets == null) break;
                    
                    foreach (var tsPacket in tsPackets)
                    {
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
        }

        private static void TtxDecoder_TeletextPageAdded(object sender, EventArgs e)
        {
            var decoder = (TeleTextDecoder) sender;
            var teletextArgs = (TeleTextSubtitleEventArgs)e;

            Console.WriteLine($"Page Rcvd - Page Num:{teletextArgs.PageNumber:X}, Service ID: {decoder.ProgramNumber}, PID: {teletextArgs.Pid:X}");

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