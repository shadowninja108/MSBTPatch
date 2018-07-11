using Syroot.BinaryData;
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
        public static bool IsOnly<T>(this IList<T> obj, T o)
        {
            return obj.Except(new List<T>() { o }).Any();
        }

        public static void WriteUInt16(this BinaryDataWriter obj, UInt16 i)
        {
            obj.WriteObject(i);
        }

        public static void WriteString(this BinaryDataWriter obj, string str, Encoding encoding = null, int max = int.MaxValue)
        {
            obj.Write((encoding ?? obj.Encoding).GetBytes(str).Take(max).ToArray());
        }

        public static void WriteUInt32(this BinaryDataWriter obj, UInt32 i)
        {
            obj.WriteObject(i);
        }

        public static void WriteMultiple(this BinaryDataWriter obj, Object w, long count)
        {
            for (int i = 0; i < count; i++)
                obj.WriteObject(w);
        }

        public static void WritePadding(this BinaryDataWriter obj, byte padding)
        {
            byte count = (byte) (0x10 - obj.Position % 0x10);
            if (count > 0xF) count = 0;
            obj.WriteMultiple(padding, count);
        }

        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue)
        {
            TValue value;
            return dictionary.TryGetValue(key, out value) ? value : defaultValue;
        }

        public static TV GetValue<TK, TV>(this IDictionary<TK, TV> dict, TK key, TV defaultValue = default(TV))
        {
            TV value;
            return dict.TryGetValue(key, out value) ? value : defaultValue;
        }
    }
}
