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
using Cinegy.TsDecoder.TransportStream;
using System.Diagnostics;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cinegy.TtxDecoder.Teletext
{
    public class TeleTextSubtitlePage
    {
        public const byte TransmissionModeParallel = 0;
        public const byte TransmissionModeSerial = 1;
        public const byte DataUnitEbuTeletextNonsubtitle = 0x02;
        public const byte DataUnitEbuTeletextSubtitle = 0x03;
        public const byte DataUnitEbuTeletextInverted = 0x0c;
        public const byte DataUnitVps = 0xc3;
        public const byte DataUnitClosedCaptions = 0xc5;
        public const byte SizeOfTeletextPayload = 44;

        private byte _transmissionMode = TransmissionModeSerial;
        private bool _receivingData;
        private readonly PageBuffer _pageBuffer = new PageBuffer();

        private long _referencePts;

        
        private Utils.Charset _primaryCharset = new Utils.Charset
        {
            Current = 0x00,
            G0M29 = Utils.Undef,
            G0X28 = Utils.Undef
        };

        // private readonly Utils _utils = new Utils();

        public short Pid { get; set; }

        public ushort SubtitlePageNumber { get; set; }// = 0x199;

        public TeleTextSubtitlePage(ushort page, short pid)
        {
            SubtitlePageNumber = page;
            Pid = pid;
        }

        public bool DecodeTeletextData(Pes pes, long pts)
        {
            if (pes.PacketStartCodePrefix != Pes.DefaultPacketStartCodePrefix || pes.StreamId != Pes.PrivateStream1 ||
                pes.PesPacketLength <= 0) return false;

            ushort startOfSubtitleData = 7;
            if (pes.OptionalPesHeader.MarkerBits == 2)
            {
                startOfSubtitleData += (ushort)(3 + pes.OptionalPesHeader.PesHeaderLength);
            }
            while (startOfSubtitleData <= pes.PesPacketLength)
            {
                var dataUnitId = pes.Data[startOfSubtitleData];
                var dataUnitLength = pes.Data[startOfSubtitleData + 1];

                if ((dataUnitId == DataUnitEbuTeletextNonsubtitle || dataUnitId == DataUnitEbuTeletextSubtitle) && dataUnitLength == SizeOfTeletextPayload)
                {
                    var data = new byte[dataUnitLength + 2];
                    Buffer.BlockCopy(pes.Data, startOfSubtitleData, data, 0, dataUnitLength + 2);
                    
                    //ETS 300 706 7.1
                    Utils.ReverseArray(ref data, 2, dataUnitLength);

                    if (_referencePts == 0) _referencePts = pts;
                    if ((_referencePts > 0) && (pts < _referencePts)) _referencePts = pts;

                    DecodeTeletextPacket(data, pts);
                }

                startOfSubtitleData += (ushort)(dataUnitLength + 2);
            }
            return false;
        }

        private void DecodeTeletextPacket(IList<byte> data, long pts)
        {
            //ETS 300 706, 9.3.1
            var address = (byte)((Utils.UnHam84(data[5]) << 4) + Utils.UnHam84(data[4]));
            var m = (byte)(address & 0x7);
            if (m == 0)
                m = 8;

            var y = (byte)((address >> 3) & 0x1f);

            //if(y>23)
            var relPts = pts - _referencePts;

            var timeStamp = new TimeSpan(0,0,0,0,(int)(relPts / 90));
             
            if (y == 0)
            {
                var pageNumber = (ushort)((m << 8) | (Utils.UnHam84(data[7]) << 4) + Utils.UnHam84(data[6]));

                var _eraseFlag = ((Utils.UnHam84(data[9]) & 0x08) == 8);

                if (pageNumber != 0x8EE && pageNumber != 0x8FF)
                    
                    Console.WriteLine($"{timeStamp:g} TTX Header Mag: {m}, Page: {pageNumber}, Clear: {_eraseFlag}");
            }
            else
            {
                Console.WriteLine($"{timeStamp:g} TTX Row: {y}, Mag: {m}");
               
                    var row = "";
                    for (var x = 0; x < 40; x++)
                    {
                        var c = (char)Utils.ParityChar(data[6 + x]);
                        if (c == '\0')
                        {
                            row += " ";
                        }
                        else
                        {
                            row += c;
                        }
                    }
                Console.WriteLine(row);
            }

            return;
            //ETS 300 706, 9.4
            byte designationCode = 0;
            if (y > 25 && y < 32)
            {
                designationCode = Utils.UnHam84(data[6]);
            }

            //ETS 300 706, 9.3.1 - Page Header
            if (y == 0)
            {
                //ETS 300 706, 9.3.1.1
                var pageNumber = (ushort)((m << 8) | (Utils.UnHam84(data[7]) << 4) + Utils.UnHam84(data[6]));

                //ETS 300 706 Table 2,C11
                _transmissionMode = (byte)(Utils.UnHam84(data[13]) & 0x01);

                //ETS 300 706 Table 2,C4
                var _eraseFlag = ((Utils.UnHam84(data[9]) & 0x08) == 8);

                //ETS 300 706 Table 2, C12, C13, C14
                var charset = (byte)(((Utils.UnHam84(data[13]) & 0x08) + (Utils.UnHam84(data[13]) & 0x04) + (Utils.UnHam84(data[13]) & 0x02)) >> 1);


                //ETS 300 706 Table 2, C11
                if ((_receivingData) && (
                                        ((_transmissionMode == TransmissionModeSerial) && (Utils.Page(pageNumber) != Utils.Page(SubtitlePageNumber))) ||
                                        ((_transmissionMode == TransmissionModeParallel) && (Utils.Page(pageNumber) != Utils.Page(SubtitlePageNumber)) && (m == Utils.Magazine(SubtitlePageNumber)))))
                {
                    _receivingData = false;
                    return;
                }


                if (pageNumber != SubtitlePageNumber) //wrong page
                {
                    //Console.WriteLine($"Teletext packet with wrong page set (expected {SubtitlePageNumber:X}, got {pageNumber:X})");
                    return;
                }

                if (_eraseFlag)
                {
                    _pageBuffer.Clear();
                    
                    Console.WriteLine("Clear page");
                }

                //Console.WriteLine($"TTX Packet: Y value: {y}, M value: {m}, Mode: {_transmissionMode}, Clear {_eraseFlag}");

               // if (_pageBuffer.IsChanged())
                {
                    ProcessBuffer(pts);
                }

                _primaryCharset.G0X28 = Utils.Undef;

                var c = (_primaryCharset.G0M29 != Utils.Undef) ? _primaryCharset.G0M29 : charset;
                Utils.remap_g0_charset(c, _primaryCharset);
                
                _receivingData = true;
            }

            //ETS 300 706, 9.3.2 - row data
            if ((m == Utils.Magazine(SubtitlePageNumber)) && (y >= 1) && (y <= 23) && _receivingData)
            {
                Console.WriteLine($"TTX Packet: Y value: {y}, M value: {m}");

                for (var x = 0; x < 40; x++)
                {
//if (_pageBuffer.GetChar(x, y) == '\0')
                    {
                        _pageBuffer.SetChar(x, y, (char)Utils.ParityChar(data[6 + x]));
                    }
                }
            }
            else if ((m == Utils.Magazine(SubtitlePageNumber)) && (y == 26) && (_receivingData))
            {
                // ETS 300 706, chapter 12.3.2: X/26 definition
                byte x26Row = 0;

                var triplets = new uint[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                for (byte i = 1, j = 0; i < 40; i += 3, j++) triplets[j] = Utils.UnHam2418((uint)((data[6 + i + 2] << 16) + (data[6 + i + 1] << 8) + data[6 + i]));

                for (byte j = 0; j < 13; j++)
                {
                    if (triplets[j] == 0xffffffff)
                    {
                        continue;
                    }

                    var d = (byte)((triplets[j] & 0x3f800) >> 11);
                    var mode = (byte)((triplets[j] & 0x7c0) >> 6);
                    var a = (byte)(triplets[j] & 0x3f);
                    var rowAddressGroup = ((a >= 40) && (a <= 63));

                    // ETS 300 706, chapter 12.3.1, table 27: set active position
                    byte x26Col;
                    if ((mode == 0x04) && (rowAddressGroup))
                    {
                        x26Row = (byte)(a - 40);
                        if (x26Row == 0) x26Row = 24;
                    }

                    // ETS 300 706, chapter 12.3.1, table 27: termination marker
                    if ((mode >= 0x11) && (mode <= 0x1f) && (rowAddressGroup)) break;

                    // ETS 300 706, chapter 12.3.1, table 27: character from G2 set
                    if ((mode == 0x0f) && (!rowAddressGroup))
                    {
                        x26Col = a;
                        if (d > 31) _pageBuffer.SetChar(x26Col, x26Row, (char)Utils.G2[0][d - 0x20]);
                    }

                    // ETS 300 706, chapter 12.3.1, table 27: G0 character with diacritical mark
                    if ((mode >= 0x11) && (mode <= 0x1f) && (!rowAddressGroup))
                    {
                        x26Col = a;

                        // A - Z
                        if ((d >= 65) && (d <= 90)) _pageBuffer.SetChar(x26Col, x26Row, (char)Utils.G2Accents[mode - 0x11][d - 65]);
                        // a - z
                        else if ((d >= 97) && (d <= 122)) _pageBuffer.SetChar(x26Col, x26Row, (char)Utils.G2Accents[mode - 0x11][d - 71]);
                        // other
                        else _pageBuffer.SetChar(x26Col, x26Row, (char)Utils.ParityChar(d));
                    }
                }
            }
            else if ((m == Utils.Magazine(SubtitlePageNumber)) && (y == 28) && (_receivingData))
            {
                // TODO:
                //   ETS 300 706, chapter 9.4.7: Packet X/28/4
                //   Where packets 28/0 and 28/4 are both transmitted as part of a page, packet 28/0 takes precedence over 28/4 for all but the colour map entry coding.

                if ((designationCode != 0) && (designationCode != 4)) return;

                // ETS 300 706, chapter 9.4.2: Packet X/28/0 Format 1
                // ETS 300 706, chapter 9.4.7: Packet X/28/4
                var triplet0 = Utils.UnHam2418((uint)((data[6 + 3] << 16) + (data[6 + 2] << 8) + data[6 + 1]));

                if (triplet0 == 0xffffffff)
                {
                    // invalid data (HAM24/18 uncorrectable error detected), skip group                        
                }
                else
                {
                    // ETS 300 706, chapter 9.4.2: Packet X/28/0 Format 1 only
                    if ((triplet0 & 0x0f) == 0x00)
                    {
                        _primaryCharset.G0X28 = (byte)((triplet0 & 0x3f80) >> 7);
                        Utils.remap_g0_charset(_primaryCharset.G0X28, _primaryCharset);
                    }
                }
            }
            else if ((m == Utils.Magazine(SubtitlePageNumber)) && (y == 29))
            {
                // TODO:
                //   ETS 300 706, chapter 9.5.1 Packet M/29/0
                //   Where M/29/0 and M/29/4 are transmitted for the same magazine, M/29/0 takes precedence over M/29/4.
                if ((designationCode == 0) || (designationCode == 4))
                {
                    // ETS 300 706, chapter 9.5.1: Packet M/29/0
                    // ETS 300 706, chapter 9.5.3: Packet M/29/4
                    var triplet0 = Utils.UnHam2418((uint)((data[6 + 3] << 16) + (data[6 + 2] << 8) + data[6 + 1]));

                    if (triplet0 == 0xffffffff)
                    {
                        // invalid data (HAM24/18 uncorrectable error detected), skip group                       
                    }
                    else
                    {
                        // ETS 300 706, table 11: Coding of Packet M/29/0
                        // ETS 300 706, table 13: Coding of Packet M/29/4
                        if ((triplet0 & 0xff) != 0x00) return;
                        
                        _primaryCharset.G0M29 = (byte)((triplet0 & 0x3f80) >> 7);
                        // X/28 takes precedence over M/29
                        if (_primaryCharset.G0X28 == Utils.Undef)
                        {
                            Utils.remap_g0_charset(_primaryCharset.G0M29, _primaryCharset);
                        }
                    }
                }
            }

        }

        public void ProcessBuffer(long pts)
        {
            var page = new string[25];
            for (var y = 0; y < 25; y++)
            {
                page[y] = "";
                for (var x = 0; x < 40; x++)
                {
                    var c = _pageBuffer.GetChar(x, y);
                    if (c == '\0')
                    {
                        page[y] += " ";
                    }
                    else
                    {
                        page[y] += c;
                    }
                }
            }

            OnTeletextPageRecieved(page, SubtitlePageNumber, Pid, pts);
            //System.Threading.Thread.Sleep(1000);
        }

        public event EventHandler TeletextPageRecieved;

        protected virtual void OnTeletextPageRecieved(string[] page, ushort pageNumber, short pid, long pts)
        {
            TeletextPageRecieved?.BeginInvoke(this, new TeleTextSubtitleEventArgs(page, pageNumber, pid, pts), EndAsyncEvent, null);
        }


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
