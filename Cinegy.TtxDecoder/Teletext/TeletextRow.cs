using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cinegy.TtxDecoder.Teletext
{
    public class TeletextRow
    {
        private readonly char[] _buffer = new char[40];

        public TeletextRow()
        {
            Clear();
        }
        
        public int RowNum { get; set; }

        public void SetChar(int x, char c)
        {
            _buffer[x] = c;
            _changed = true;
        }

        public char GetChar(int x)
        {
            return _buffer[x];
        }

        public string GetPlainRow()
        {
            return new string(_buffer.Where(c => !char.IsControl(c)).ToArray());
        }

        private bool _changed;

        public bool IsChanged()
        {
            return _changed;
        }

        public void Clear()
        {
                for (var x = 0; x < _buffer.Length; x++)
                {
                    _buffer[x] = '\0';
                }
         
            _changed = false;
        }
    }
}
