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

        public Dictionary<String, String> entries;

        public MSBT(byte[] bytes)
        {
            MemoryStream stream = new MemoryStream(bytes);
            BinaryDataReader reader = new BinaryDataReader(stream) { ByteConverter = ByteConverter.BigEndian };

            if (Encoding.ASCII.GetString(reader.ReadBytes(8)) != "MsgStdBn")
                throw new InvalidDataException("MsgStdBn magic is missing!");

            ushort bom = reader.ReadUInt16();

            if (bom != 0xFEFF && bom != 0xFFFE)
                throw new InvalidDataException("BOM is not 0xFEFF or 0xFFFE!");

            reader.ByteOrder = (ByteOrder) bom;

            if (reader.ReadUInt16() != 0x0)
                throw new InvalidDataException("Invalid data!");

            if (reader.ReadUInt16() != 0x0103)
                throw new InvalidDataException("Invalid data!");

            int sections = reader.ReadUInt16();

            if (reader.ReadUInt16() != 0x0)
                throw new InvalidDataException("Invalid data!");

            long filesize = reader.ReadUInt32(); 

            if (reader.ReadBytes(10).IsOnly<byte>(0))
                throw new InvalidDataException("Invalid data!");

            while (true)
            {
                try
                {
                    string magic = reader.ReadString(4, Encoding.ASCII);
                    switch (magic)
                    {
                        case "LBL1":
                            foreach(KeyValuePair<UInt32, UInt32> entry in ParseLBL1(reader))
                            {
                                Console.WriteLine($"{entry.Key} : {entry.Value}");
                            }
                            break;
                        default:
                            Console.WriteLine($"Unknown section {magic}!");
                            break;
                    }
            } catch
                {
                    break; // probably end of file, just exit
                }
            }

            stream.Dispose();

        }
        
        public static Dictionary<UInt32, UInt32> ParseLBL1(BinaryDataReader stream)
        {

            long size = stream.ReadUInt32();
            if (stream.ReadBytes(8).IsOnly<byte>(0))
                throw new InvalidDataException("Offset 0x08 of LBL1 section is not empty!");
            long entriesLength = stream.ReadUInt32();

            Dictionary<UInt32, UInt32> entries = new Dictionary<UInt32, UInt32>();

            for(int i = 0; i < entriesLength; i++)
                entries.Add(stream.ReadUInt32(), stream.ReadUInt32());

            return entries;
        }
    }
}
