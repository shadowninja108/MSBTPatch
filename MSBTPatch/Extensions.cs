using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSBTPatch
{
    public static class Extensions
    {

        public static byte[] Read(this Stream obj, int count)
        {
            byte[] buffer = new byte[count];
            int read = obj.Read(buffer, 0, count);
            return buffer.Take(read).ToArray();
        }

        public static string ReadString(this Stream obj, int count)
        {
            return Encoding.ASCII.GetString(Read(obj, count));
        }

        public static string ReadASCII(this Stream obj)
        {
            IList<char> buffer = new List<char>();
            char b = '\n';
            while(b != '\0')
            {
                b = (char) obj.ReadByte();
                buffer.Add(b);
            }
            return new string(buffer.ToArray());
        }

        public static UInt16 ReadUInt16(this Stream obj)
        {
            return BitConverter.ToUInt16(obj.Read(2), 0);
        }

        public static UInt32 ReadUInt32(this Stream obj)
        {
            return BitConverter.ToUInt32(obj.Read(4), 0);
        }

        public static bool IsOnly<T>(this IList<T> obj, T o)
        {
            return obj.Except(new List<T>() { o }).Any();
        }
    }
}
