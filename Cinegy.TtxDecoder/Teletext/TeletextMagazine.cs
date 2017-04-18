using System;
using System.Collections.Generic;

namespace Cinegy.TtxDecoder.Teletext
{
    public class TeletextMagazine
    {
        private int _currentPageNumber = -1;

        /// <summary>
        /// A Dictionary of Teletext Magazines, which themselves may contain a collection of pages
        /// </summary>
        public Dictionary<int, TeletextPage> Pages { get; set; } = new Dictionary<int, TeletextPage>(16);

        public TeletextService ParentService { get; set; }

        public void AddPacket(TeletextPacket packet)
        {
            if (packet.Row == 29)
            {
                //this is a magazine-specific packet, which should set some over-rideable defaults for any pages
                //TODO: This

                return;
            }

            //ETS 300 706, 9.3.1
            var headerPacket = packet as TeletextHeaderPacket;

            if (headerPacket != null)
            {
                //row 0 is the 'header' row, and has some extra control non-display data at start of the data

                //check if a pre-existing page within this magazine was set, and is now terminated by this new header
                if (_currentPageNumber != -1 && Pages.ContainsKey(_currentPageNumber))
                {
                    var completedPage = Pages[_currentPageNumber];
                    if (completedPage != null)
                    {
                        ParentService.OnTeletextPageReady(completedPage);
                    }
                }

                _currentPageNumber = headerPacket.Page;

                if (_currentPageNumber == 0x8EE || _currentPageNumber == 0x8FF || !headerPacket.EraseFlag) return;
                
                if (!Pages.ContainsKey(_currentPageNumber)) return;

                Pages[_currentPageNumber].Clear();
                ParentService.OnTeletextPageCleared(_currentPageNumber, packet.Pts);
            }
            else
            {
                //any remaining row addresses are page-specific, and shall be processed within that page
                AddPacketToPages(packet);
            }
            
        }

        private void AddPacketToPages(TeletextPacket packet)
        {
            //if (PageFilter > -1)
            //    if (packet.Magazine != MagazineFilter) return; //magazine does not match filter, so skip processing

            //add this packet to the magazines and their pages associated with this service
            if (!Pages.ContainsKey(_currentPageNumber))
                Pages.Add(_currentPageNumber, new TeletextPage {ParentMagazine = this, PageNum = _currentPageNumber});

            Pages[_currentPageNumber].AddPacket(packet);
        }
    }
}

