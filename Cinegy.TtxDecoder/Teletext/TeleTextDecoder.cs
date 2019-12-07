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
using System.Linq;
using Cinegy.TsDecoder.TransportStream;

namespace Cinegy.TtxDecoder.Teletext
{
    public class TeletextDecoder
    {
        private Pes _currentTeletextPes;
        public long LastPts { get; private set; }
        
        public TeletextService Service { get; set; } = new TeletextService();

        public TeletextDescriptor CurrentTeletextDescriptor { get; private set; }

        /// <summary>
        /// The Program Number of the service that is used as source for teletext data - can be set by constructor only, otherwise default program will be used.
        /// </summary>
        public ushort ProgramNumber { get; private set; }

        public TeletextDecoder(ushort programNumber)
        {
            ProgramNumber = programNumber;
        }

        public TeletextDecoder()
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

                Service.ProgramNumber = ProgramNumber;

                esStreamInfo = tsDecoder.GetEsStreamForProgramNumberByTag(ProgramNumber, 0x6, 0x56);

                teletextDescriptor = tsDecoder.GetDescriptorForProgramNumberByTag<TeletextDescriptor>(ProgramNumber, 0x6, 0x56);
                
                return teletextDescriptor != null;
            }
        }

        private void Setup(TsDecoder.TransportStream.TsDecoder tsDecoder)
        {
            if (FindTeletextService(tsDecoder, out var esStreamInfo, out var ttxDesc))
            {
                Setup(ttxDesc, esStreamInfo.ElementaryPid);
            }
        }

        public void Setup(TeletextDescriptor teletextDescriptor, short teletextPid)
        {
            CurrentTeletextDescriptor = teletextDescriptor;

            var defaultLang = teletextDescriptor.Languages.FirstOrDefault();

            if(defaultLang==null) return;

            Service.AssociatedDescriptor = teletextDescriptor;

            Setup(defaultLang.TeletextMagazineNumber, defaultLang.TeletextPageNumber, teletextPid);
        }

        public void Setup(int magazineNum, int pageNum, short teletextPid)
        {
            if (magazineNum == 0)
            {
                magazineNum = 8;
            }

            Service.TeletextPid = teletextPid;
            Service.MagazineFilter = magazineNum;
            Service.PageFilter = pageNum;

        }

        public void AddPacket(TsPacket tsPacket, TsDecoder.TransportStream.TsDecoder tsDecoder = null)
        {
            if (Service == null || Service.TeletextPid == -1)
            {
                Setup(tsDecoder);
            }

            if (tsPacket.Pid != Service?.TeletextPid) return;
            
            if (tsPacket.PayloadUnitStartIndicator)
            {
                if (tsPacket.PesHeader.Pts > -1)
                    LastPts = tsPacket.PesHeader.Pts;

                if (_currentTeletextPes == null)
                {
                    _currentTeletextPes = new Pes(tsPacket);
                }
            }
            else
            {
                _currentTeletextPes?.Add(tsPacket);
            }

            if (_currentTeletextPes?.HasAllBytes() != true) return;

            _currentTeletextPes.Decode();

            Service.AddData(_currentTeletextPes, tsPacket.PesHeader);

            _currentTeletextPes = null;
        }
    }
}
