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

namespace Cinegy.TtxDecoder.Teletext
{
    public class TeleTextSubtitleEventArgs : EventArgs
    {
        public string[] Page { get; set; }
        public ushort PageNumber { get; set; }
        public short Pid { get; set; }

        public TeleTextSubtitleEventArgs(IList<string> page, ushort pageNumber, short pid)
        {
            Page = new string[page.Count];

            for (var i = 0; i < page.Count; i++)
            {
                Page[i] = page[i];
            }

            PageNumber = pageNumber;
            Pid = pid;
        }
    }
}
