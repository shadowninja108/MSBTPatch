using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSBTPatch
{
    public class MemoryStream : System.IO.MemoryStream
    { 
        private Endianness? endianness;

        public MemoryStream(byte[] buffer) : base(buffer)
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int c = base.Read(buffer, offset, count);
            if (endianness != null && GetSystemEndianess() != endianness)
                Array.Reverse(buffer);
            return c;
        }

        public Endianness GetEndianness()
        {
            return endianness ?? GetSystemEndianess();
        }

        public void SetEndianness(Endianness? endianness)
        {
            this.endianness = endianness;
        }

        public static Endianness GetSystemEndianess()
        {
            return BitConverter.IsLittleEndian ? Endianness.Little : Endianness.Big;
        }

    }

    public enum Endianness
    {
        Little, Big
    }
}
