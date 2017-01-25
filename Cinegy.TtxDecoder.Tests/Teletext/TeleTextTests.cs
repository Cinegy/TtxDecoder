using System;
using System.Linq;
using System.Reflection;
using Cinegy.TsDecoder.TransportStream;
using Cinegy.TtxDecoder.Teletext;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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

            //var things = assembly.GetManifestResourceNames();

            const int readFragmentSize = 1316;

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null) Assert.Fail("Unable to read test resource: " + resourceName);

                var packetCounter = 0;

                var data = new byte[readFragmentSize];

                var readCount = stream.Read(data, 0, readFragmentSize);

                while (readCount > 0)
                {
                    var tsPackets = factory.GetTsPacketsFromData(data);

                    if (tsPackets == null) break;

                    packetCounter += tsPackets.Length;

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

                    //PrintTeletext(ttxDecoder);
                }
            }
        }

        private void TtxDecoder_TeletextPageAdded(object sender, EventArgs e)
        {
            var teletextArgs = (TeleTextSubtitleEventArgs)e;
            Console.WriteLine($"Page Rcvd: {teletextArgs.PageNumber},{teletextArgs.Pid}");

            foreach (var line in teletextArgs.Page)
            {
                if (String.IsNullOrEmpty(line) || String.IsNullOrEmpty(line.Trim())) continue;
                Console.WriteLine($"{new string(line.Where(c => !char.IsControl(c)).ToArray())}");
            }
        }

        private static void PrintTeletext(TeleTextDecoder ttxDecoder)
        {
            //    Console.WriteLine(
            //        $"\nTeleText Subtitles - decoding from Service ID {ttxDecoder.ProgramNumber}\n----------------");

                foreach (var page in ttxDecoder.TeletextDecodedSubtitlePage.Keys)
                {
                    //Console.WriteLine($"Live Decoding Page {page:X}\n");
                    
                    foreach (var line in ttxDecoder.TeletextDecodedSubtitlePage[page])
                    {
                        if (String.IsNullOrEmpty(line) || String.IsNullOrEmpty(line.Trim())) continue;
                        Console.WriteLine($"{new string(line.Where(c => !char.IsControl(c)).ToArray())}");
                    }
                }
            
        }
    }
}