/* Copyright 2017 Cinegy GmbH.

  Licensed under the Apache License, Version 2.0 (the "License");
  you may not use this file except in compliance with the License.
  You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

  Unless required by applicable law or agreed to in writing, software
  distributed under the License is distributed on an "AS IS" BASIS,
  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
  See the License for the specific language governing permissions and
  limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Cinegy.TsDecoder.TransportStream;

namespace Cinegy.TtxDecoder.Teletext
{
    public class TeleTextDecoder
    {
        private TeletextDescriptor _currentTeletextDescriptor;
        private Pes _currentTeletextPes;
        private Dictionary<ushort, TeleTextPage> _teletextSubtitlePages;

        /// <summary>
        /// The TS Packet ID that has been selected as the elementary stream containing teletext data
        /// </summary>
        public short TeletextPid { get; private set; } = -1;

        /// <summary>
        /// A Dictionary of decoded Teletext pages, where each page contains a number of strings
        /// </summary>
        public Dictionary<ushort, string[]> TeletextDecodedSubtitlePage { get; } = new Dictionary<ushort, string[]>();

        /// <summary>
        /// The Program Number of the service that is used as source for teletext data - can be set by constructor only, otherwise default program will be used.
        /// </summary>
        public ushort ProgramNumber { get; private set; }

        public TeleTextDecoder(ushort programNumber)
        {
            ProgramNumber = programNumber;
        }

        public TeleTextDecoder()
        {
        }

        public bool FindTeletextService(TsDecoder.TransportStream.TsDecoder tsDecoder, out EsInfo esStreamInfo, out TeletextDescriptor teletextDescriptor)
        {
            if (tsDecoder == null) throw new InvalidOperationException("Null reference to TS Decoder");

            esStreamInfo = null;
            teletextDescriptor = null;

            lock (tsDecoder)
            {
                if (ProgramNumber == 0)
                {
                    var pmt = tsDecoder.GetSelectedPmt(ProgramNumber);
                    if (pmt != null)
                    {
                        ProgramNumber = pmt.ProgramNumber;
                    }
                }

                if (ProgramNumber == 0) return false;

                esStreamInfo = tsDecoder.GetEsStreamForProgramNumberByTag(ProgramNumber, 0x6, 0x56);

                teletextDescriptor = tsDecoder.GetDescriptorForProgramNumberByTag<TeletextDescriptor>(ProgramNumber, 6, 0x56);

                if (teletextDescriptor == null) return false;

                return true;

            }
        }

        private void Setup(TsDecoder.TransportStream.TsDecoder tsDecoder)
        {
            EsInfo esStreamInfo;
            TeletextDescriptor ttxDesc;

            if( FindTeletextService(tsDecoder, out esStreamInfo,out ttxDesc))
            {
                Setup(ttxDesc, esStreamInfo.ElementaryPid);
            }

        }

        public void Setup(TeletextDescriptor teletextDescriptor, short teletextPid)
        {
            TeletextPid = teletextPid;
            _currentTeletextDescriptor = teletextDescriptor;

            foreach (var lang in teletextDescriptor.Languages)
            {
                //  if (lang.TeletextType != 0x02 && lang.TeletextType != 0x05)
                //    continue;

                if (_teletextSubtitlePages == null)
                {
                    _teletextSubtitlePages = new Dictionary<ushort, TeleTextPage>();
                }

                var m = lang.TeletextMagazineNumber;
                if (lang.TeletextMagazineNumber == 0)
                {
                    m = 8;
                }
                var page = (ushort)((m << 8) + lang.TeletextPageNumber);

                if (_teletextSubtitlePages.ContainsKey(page)) continue;

                _teletextSubtitlePages.Add(page, new TeleTextPage(page, TeletextPid));
                _teletextSubtitlePages[page].TeletextPageRecieved += TeleTextDecoder_TeletextPageRecieved;

            }
        }

        public void Setup(int magazineNum, int pageNum, short teletextPid)
        {
            TeletextPid = teletextPid;
            
            if (_teletextSubtitlePages == null)
            {
                _teletextSubtitlePages = new Dictionary<ushort, TeleTextPage>();
            }

            var m = magazineNum;
            if (magazineNum == 0)
            {
                m = 8;
            }
            var page = (ushort)((m << 8) + pageNum);

            if (_teletextSubtitlePages.ContainsKey(page)) return;

            _teletextSubtitlePages.Add(page, new TeleTextPage(page, TeletextPid));
            _teletextSubtitlePages[page].TeletextPageRecieved += TeleTextDecoder_TeletextPageRecieved;
            
        }


        public void AddPacket(TsDecoder.TransportStream.TsDecoder tsDecoder, TsPacket tsPacket)
        {
            if (TeletextPid == -1)
            {
                Setup(tsDecoder);
            }

            if (tsPacket.Pid != TeletextPid) return;

            if (tsPacket.PayloadUnitStartIndicator)
            {
                if (_currentTeletextPes == null)
                {
                    _currentTeletextPes = new Pes(tsPacket);
                }

                if (_currentTeletextPes.HasAllBytes())
                {
                    _currentTeletextPes.Decode();
                    foreach (var key in _teletextSubtitlePages.Keys)
                    {
                        _teletextSubtitlePages[key].DecodeTeletextData(_currentTeletextPes, tsPacket.PesHeader.Pts);
                    }

                    _currentTeletextPes = null;
                }
            }
            else
            {
                _currentTeletextPes?.Add(tsPacket);
            }
        }

        private void TeleTextDecoder_TeletextPageRecieved(object sender, EventArgs e)
        {
            var teletextArgs = (TeleTextSubtitleEventArgs)e;

            lock (TeletextDecodedSubtitlePage)
            {
                if (!TeletextDecodedSubtitlePage.ContainsKey(teletextArgs.PageNumber))
                {
                    TeletextDecodedSubtitlePage.Add(teletextArgs.PageNumber, new string[0]);
                }

                TeletextDecodedSubtitlePage[teletextArgs.PageNumber] = teletextArgs.Page;
            }

            TeletextPageAdded?.BeginInvoke(this, teletextArgs, EndAsyncEvent, null);
        }

        public event EventHandler TeletextPageAdded;

        private static void EndAsyncEvent(IAsyncResult iar)
        {
            //var ar = (System.Runtime.Remoting.Messaging.AsyncResult)iar;
            //var invokedMethod = (EventHandler)ar.AsyncDelegate;

            //try
            //{
            //    invokedMethod.EndInvoke(iar);
            //}
            //catch
            //{
            //    //nothing to do
            //}
        }
    }
}
