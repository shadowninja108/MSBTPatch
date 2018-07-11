using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MSBTPatch.MSBT;

namespace MSBTPatch
{
    class Program
    {
        static void Main(string[] args)
        {
            byte[] bytes = File.ReadAllBytes("C:/sw_nand/splatoon_2_3.1.0/dec/Message/CommonMsg_USen/TalkNews.msbt");
            MSBT msbt = new MSBT(bytes);

            Stream outf = File.Open("./out.msbt", FileMode.Create);
            msbt.Write(outf);
            outf.Dispose();

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

            Console.ReadKey();
        }
    }
}
