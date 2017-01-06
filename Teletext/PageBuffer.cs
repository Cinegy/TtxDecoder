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

namespace TtxDecoder.Teletext
{
    internal class PageBuffer
    {
        private readonly char[][] _buffer = {
            new char[40], new char[40], new char[40], new char[40], new char[40], new char[40], new char[40], new char[40], new char[40], new char[40], new char[40], new char[40], new char[40], new char[40], new char[40], new char[40], new char[40], new char[40], new char[40], new char[40], new char[40], new char[40], new char[40], new char[40], new char[40]
        };

        public void SetChar(int x, int y, char c)
        {
            _buffer[y][x] = c;
            _changed = true;
        }

        public char GetChar(int x, int y)
        {
            return _buffer[y][x];
        }

        private bool _changed;

        public bool IsChanged()
        {
            return _changed;
        }

        public void Clear()
        {
            foreach (var t in _buffer)
            {
                for (var x = 0; x < t.Length; x++)
                {
                    t[x] = '\0';
                }
            }
            _changed = false;
        }
    }
}
