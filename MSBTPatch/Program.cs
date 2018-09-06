using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using static MSBTPatch.MSBT;

namespace MSBTPatch
{
    class Program
    {
        static void Main(string[] args)
        {
            byte[] bytes = File.ReadAllBytes("C:/sw_nand/splatoon 2 3.2.0/romfs/Message/CommonMsg_USen.release/TalkNewsStage.msbt");
            MSBT msbt = new MSBT(bytes);

            //Stream outf = File.Open("./out.msbt", FileMode.Create);
            //msbt.Write(outf);
            //outf.Dispose();

            foreach (KeyValuePair<long, Entry> kv in msbt.entries)
            {
                Entry entry = kv.Value;
                Debug.WriteLine($"{entry.key} : {entry.value}");
                if (entry.attributes != null)
                {
                    Attributes a = entry.attributes;
                    Debug.WriteLine($"Attributes:\nUnk1: {a.unk1}\nUnk2: {a.unk2}\nType: {a.type}\nUnk3: {a.unk3}\nUnk4: {a.unk4}\nUnk5: {a.unk5}\nUnk6: {a.unk6}\nUnk7: {BitConverter.ToString(a.unk7)}");
                } else
                {
                    Debug.WriteLine("No attributes.");
                }
            }

            Console.ReadKey(); // just wait until the user does something so i can actually see the output
        }
    }
}
