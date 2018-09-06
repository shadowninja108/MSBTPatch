using Syroot.BinaryData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSBTPatch
{
    public class MSBT
    {
        public Dictionary<long, Entry> entries;

        public ByteOrder endianness;

        public MSBT(byte[] bytes)
        {
            MemoryStream stream = new MemoryStream(bytes);
            BinaryDataReader reader = new BinaryDataReader(stream, Encoding.ASCII) { ByteConverter = ByteConverter.BigEndian };

            if (reader.ReadString(8) != "MsgStdBn")
                throw new InvalidDataException("MsgStdBn magic is missing!");

            ushort bom = reader.ReadUInt16();

            if (bom != 0xFEFF && bom != 0xFFFE)
                throw new InvalidDataException("BOM is not 0xFEFF or 0xFFFE!");

            endianness = (ByteOrder)bom;
            reader.ByteOrder = endianness;

            if (reader.ReadUInt16() != 0x0)
                throw new InvalidDataException("Invalid data!");

            reader.ReadBytes(2); // skip unknown

            int sections = reader.ReadUInt16();

            if (reader.ReadUInt16() != 0x0)
                throw new InvalidDataException("Invalid data!");

            long filesize = reader.ReadUInt32(); 

            if (reader.ReadBytes(10).IsOnly<byte>(0))
                throw new InvalidDataException("Invalid data!");

            Dictionary<long, string> LBL1 = null;
            Dictionary<int, Attributes> ATR1 = null;
            Dictionary<int, string> TXT2 = null;

            while (true)
            {
                try
                {
                    string magic = reader.ReadString(4);
                    switch (magic)
                    {
                        case "LBL1":
                            LBL1 = ParseLBL1(reader);
                            break;
                        case "ATR1":
                            ATR1 = ParseATR1(reader);
                            break;
                        case "TXT2":
                            TXT2 = ParseTXT2(reader);
                            break;
                        default:
                            if (!Encoding.ASCII.GetBytes(magic).IsOnly<byte>(0xAB))
                                Console.WriteLine($"Unknown section {magic}!");
                            reader.Seek(-3); // move back so that the read string moves 1 byte at a time (and not 4)
                            break;
                    }
            } catch (EndOfStreamException)
                {
                    break; // end of file, just exit
                }
            }

            entries = new Dictionary<long, Entry>();

            foreach (KeyValuePair<long, string> kv in LBL1)
            {
                int key = (int)kv.Key;
                if (!TXT2.ContainsKey(key))
                    throw new InvalidDataException($"TXT2 does not have the associated index {kv.Key} from LBL1!");
                Entry entry = new Entry();
                entry.key = kv.Value;
                entry.value = TXT2[key];
                entry.attributes = ATR1.GetValue(key);
                entries.Add(kv.Key, entry);
            }

            /*Console.WriteLine("LBL1:");
            foreach (KeyValuePair<long, string> kv in LBL1)
            {
                Console.WriteLine($"Index: {kv.Key}\nEntry: {kv.Value}\n");
            }

            Console.WriteLine("ATR1:");
            foreach (KeyValuePair<int, Attributes> kv in ATR1)
            {
                Attributes a = kv.Value;
                Console.WriteLine($"Index: {kv.Key}\nUnk1: {a.unk1}\nUnk2: {a.unk2}\nType: {a.type}\nUnk3: {a.unk3}\nUnk4: {a.unk4}\nUnk5: {a.unk5}\nUnk6: {a.unk6}\n");
            }

            Console.WriteLine("TXT2:");
            foreach (KeyValuePair<int, string> kv in TXT2)
            {
                Console.WriteLine($"Index: {kv.Key}\nEntry: {kv.Value}\n");
            }*/

            stream.Dispose();

        }
        
        public void Write(Stream stream)
        {
            BinaryDataWriter writer = new BinaryDataWriter(stream, Encoding.ASCII) { ByteOrder = ByteOrder.BigEndian};

            // order by id so that code works
            entries = entries.OrderBy(x => x.Key).ToDictionary(pair => pair.Key, pair => pair.Value);

            // MSBT header
            writer.WriteString("MsgStdBn"); // magic
            writer.WriteObject(endianness); // bom
            writer.WriteUInt16(0); // padding
            writer.WriteUInt16(0x0103); // magic shit
            writer.ByteOrder = endianness; // don't ask why i set it here
            writer.WriteUInt16(3); // only support 3 sections rn
            writer.WriteUInt16(0); // padding
            long filesize = writer.Position; // saved for later
            writer.WriteUInt32(0); // filesize (filled in later)
            writer.WriteMultiple((byte)0, 10); // moar padding

            //LBL1 header
            writer.WriteString("LBL1");
            long lbl1size = writer.Position; // to be overwritten
            writer.WriteUInt32(0); // size of LBL relative to after padding
            writer.WriteMultiple((byte)0, 8); // padding
            writer.WriteUInt32(1); // number of entries (we just lump every string into one entry)

            uint relativeOffset;
            long stringBytes;

            relativeOffset = sizeof(UInt32) * 3; // number of entries + string count + offset
            stringBytes = 0;

            writer.WriteUInt32((uint)entries.Count);
            writer.WriteUInt32(relativeOffset); // 

            foreach (KeyValuePair<long, Entry> kv in entries)
            {
                long id = kv.Key;
                Entry entry = kv.Value;
                byte[] key = Encoding.ASCII.GetBytes(entry.key);

                long offset = relativeOffset + stringBytes;
                writer.Write((byte) key.Length);
                writer.Write(key);
                writer.WriteUInt32((UInt32) id);

                stringBytes++;
                stringBytes += key.Length;
            }

            SeekTask tmp;
            tmp = writer.TemporarySeek(lbl1size, SeekOrigin.Begin);
            writer.WriteUInt32((UInt32) stringBytes + 0x1C);
            tmp.Dispose();

            writer.WritePadding(0xAB);

            writer.WriteString("ATR1");
            long atr1size = writer.Length;
            writer.WriteUInt32(0); // section size
            writer.WriteMultiple((UInt32)0, 2); // the magic of unknown shit
            writer.WriteUInt32((UInt32) entries.Count);
            writer.WriteUInt32(0x18); // appears to be entry size. hopefully older games don't fuck up with more than they expect lol

            foreach (KeyValuePair<long, Entry> kv in entries)
            {
                Attributes set = kv.Value.attributes ?? new Attributes();
                writer.WriteObject(set.unk1);
                writer.WriteObject(set.unk2);
                writer.WriteObject(set.unk3);
                writer.WriteObject(set.type);
                writer.WriteUInt16(set.unk4);
                writer.WriteObject(set.unk5);
                writer.WriteObject(set.unk6);
                writer.WriteObject(set.unk7);
            }

            tmp = writer.TemporarySeek(atr1size, SeekOrigin.Begin);
            writer.WriteUInt32((UInt32)(0x18 * entries.Count + 2));
            tmp.Dispose();

            writer.WritePadding(0xAB);

            writer.WriteString("TXT2");
            long txt2size = writer.Length;
            writer.WriteUInt32(0); // section size
            writer.WriteMultiple((UInt32)0, 2); // unknowns
            writer.WriteUInt32((UInt32)entries.Count);
            int entriesLength = 4 * entries.Count;

            relativeOffset = sizeof(UInt32); // number of entries
            stringBytes = 0;

            foreach (KeyValuePair<long, Entry> kv in entries)
            {
                Entry entry = kv.Value;
                byte[] key = Encoding.Unicode.GetBytes(entry.value + '\0');


                UInt32 offset = (UInt32)(relativeOffset + entriesLength + stringBytes);
                writer.WriteUInt32(offset);

                stringBytes += key.Length;
            }

            foreach (KeyValuePair<long, Entry> kv in entries)
            {
                Entry entry = kv.Value;
                byte[] key = Encoding.Unicode.GetBytes(entry.value + '\0');

                writer.WriteObject(key);
            }

            tmp = writer.TemporarySeek(txt2size, SeekOrigin.Begin);
            writer.WriteUInt32((UInt32)(stringBytes + relativeOffset));
            tmp.Dispose();

            tmp = writer.TemporarySeek(filesize, SeekOrigin.Begin);
            writer.WriteUInt32((UInt32)(writer.Length));
            tmp.Dispose();
        }

        public static Dictionary<long, string> ParseLBL1(BinaryDataReader reader)
        {
            long size = reader.ReadUInt32();

            if (reader.ReadBytes(8).IsOnly<byte>(0)) // padding
                throw new InvalidDataException("Offset 0x08 of LBL1 section is not empty!");

            long position = reader.Position; // offsets for entry text is relative from here, don't ask why

            List<Tuple<UInt32, UInt32>> entries = new List<Tuple<UInt32, UInt32>>();
            long entriesLength = reader.ReadUInt32();

            for (int i = 0; i < entriesLength; i++)
                entries.Add(new Tuple<UInt32,UInt32>(reader.ReadUInt32(), reader.ReadUInt32()));

            Dictionary<long, string> data = new Dictionary<long, string>();

            foreach(Tuple<UInt32, UInt32> entry in entries)
            {
                reader.Seek(position + entry.Item2, SeekOrigin.Begin);
                for(int i = 0; i < entry.Item1; i++)
                {
                    byte length = reader.ReadByte();
                    string text = reader.ReadString(length);
                    long index = reader.ReadUInt32();
                    data.Add(index, text);
                }
            }

            reader.Seek(position + size, SeekOrigin.Begin); // there is no "guarentee" that the last entry will be at the end of the section, so i just do this to make sure

            return data;
        }

        public static Dictionary<int, Attributes> ParseATR1(BinaryDataReader reader)
        {
            long size = reader.ReadUInt32();

            if (reader.ReadBytes(8).IsOnly<byte>(0)) // padding
                throw new InvalidDataException("Offset 0x08 of ATR1 section is not empty!");

            long entriesLength = reader.ReadUInt32();

            long entryLength = reader.ReadUInt32();

            Dictionary<int, Attributes> entries = new Dictionary<int, Attributes>();

            for (int i = 0; i < entriesLength; i++)
            {
                Attributes set = new Attributes
                {
                    unk1 = reader.ReadByte(),
                    unk2 = reader.ReadByte(),
                    type = reader.ReadByte(),
                    unk3 = reader.ReadByte(),
                    unk4 = reader.ReadUInt16(),
                    unk5 = reader.ReadByte(),
                    unk6 = reader.ReadByte(),
                    unk7 = reader.ReadBytes((int)(entryLength - 8))
                };

                if (set.unk2 > 2)
                {
                    Console.WriteLine($"Entry {i} in ATR1 is invalid! Skipping...");
                    continue;
                }

               //if (set.unk4 > 8)
               // {
                //    Console.WriteLine($"Entry {i} in ATR1 is invalid! Skipping...");
                //    continue;
               // }

                entries.Add(i, set);
            }

            return entries;
        }

        public static Dictionary<int, string> ParseTXT2(BinaryDataReader reader)
        {
            long size = reader.ReadUInt32();

            if (reader.ReadBytes(8).IsOnly<byte>(0)) // padding
                throw new InvalidDataException("Offset 0x08 of ATR1 section is not empty!");

            long position = reader.Position;

            long entriesLength = reader.ReadUInt32();

            Dictionary<int, string> entries = new Dictionary<int, string>();

            int emptyStrings = 0; // because satan designed this format himself

            for (int i = 0; i < entriesLength; i++)
            {
                long offset = reader.ReadUInt32();
                long nextEntry = reader.Position;

                reader.Seek(position + offset, SeekOrigin.Begin);

                string str = reader.ReadString(StringDataFormat.ZeroTerminated, Encoding.Unicode);

                if (String.IsNullOrEmpty(str))
                    emptyStrings++;
                else
                    entries.Add(i - emptyStrings, str);

                reader.Seek(nextEntry, SeekOrigin.Begin);
            }

            return entries;
        }

        public struct Entry
        {
            public string key, value;
            public Attributes attributes;
        }

        public class Attributes
        {
            public Attributes()
            {
                unk1 = 0;
                unk2 = 0;
                unk3 = 0;
                type = 0;
                unk4 = 0;
                unk5 = 0;
                unk6 = 0;
                unk7 = new byte[24-8];
            }

            // no one fucking knows lol
            public byte unk1, unk2, type, unk3;
            public ushort unk4;
            public byte unk5, unk6;
            public byte[] unk7;
        }
    }
}
