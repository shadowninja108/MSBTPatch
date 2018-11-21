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
            string[] rootdirs = new string[] { "C:/sw_nand/splatoon 2 4.1.0/romfs/Message/LayoutMsg_USen.release/", "C:/sw_nand/splatoon 2 4.2.0 dude/romfs/Message/LayoutMsg_USen.release/" };
            string[] friendly = new string[] { "4.1.0", "4.2.0" };

            DirectoryInfo[] roots = new DirectoryInfo[rootdirs.Length];
            for (int i = 0; i < rootdirs.Length; i++)
                roots[i] = new DirectoryInfo(rootdirs[i]);

            Dictionary<DirectoryInfo, List<FileInfo>> msbts = new Dictionary<DirectoryInfo, List<FileInfo>>();
            foreach(DirectoryInfo root in roots)
            {
                if (!msbts.ContainsKey(root))
                    msbts[root] = new List<FileInfo>();

                foreach (FileInfo msbt in root.EnumerateFiles())
                    msbts[root].Add(msbt);
            }

            Dictionary<string, Dictionary<DirectoryInfo, Dictionary<string, string>>> mapped = new Dictionary<string, Dictionary<DirectoryInfo, Dictionary<string, string>>>();

            foreach (KeyValuePair<DirectoryInfo, List<FileInfo>> kv in msbts)
            {
                DirectoryInfo root = kv.Key;
                List<FileInfo> msbtFiles = kv.Value;

                foreach (FileInfo msbtFile in msbtFiles)
                {
                    string msbtFileName = msbtFile.Name;
                    if (!mapped.ContainsKey(msbtFileName))
                        mapped[msbtFileName] = new Dictionary<DirectoryInfo, Dictionary<string, string>>();
                    if (!mapped[msbtFileName].ContainsKey(root))
                        mapped[msbtFileName][root] = new Dictionary<string, string>();

                    MSBT msbt = new MSBT(File.ReadAllBytes(msbtFile.FullName));
                    Dictionary<string, string> entries = new Dictionary<string, string>();
                    foreach (KeyValuePair<long, Entry> kv2 in msbt.entries)
                    {
                        Entry entry = kv2.Value;

                        entries[entry.key] = entry.value.ToString();
                        
                    }

                    mapped[msbtFile.Name][root] = entries;
                }
            }

            string log = "";

            foreach(KeyValuePair<string, Dictionary<DirectoryInfo, Dictionary<string, string>>> kv in mapped)
            {
                string msbtFileName = kv.Key;
                Dictionary<DirectoryInfo, Dictionary<string, string>> rootToEntryMap = kv.Value;

                List<Dictionary<string, string>>  entryLists = new List<Dictionary<string, string>>(rootToEntryMap.Values);

                for (int i = 1; i < entryLists.Count; i++)
                {
                    Dictionary<string, string> entries = entryLists[i];
                    Dictionary<string, string> previousEntries = entryLists[i - 1];
                    foreach (KeyValuePair<string, string> kv2 in entries)
                    {
                        if (!previousEntries.ContainsKey(kv2.Key))
                        {
                            log += $"{friendly[i]} added key \"{kv2.Key}\" to file {msbtFileName}";
                            log += $"\t{kv2.Value}{Environment.NewLine}";
                        }
                        if (!previousEntries.ContainsValue(kv2.Value) && previousEntries.ContainsKey(kv2.Key)) // make sure to ignore new entiries entirely
                        {
                            log += $"{friendly[i]} changed key \"{kv2.Key}\" in file {msbtFileName}";
                            log += $"\t{friendly[i - 1]}: {previousEntries[kv2.Key]}";
                            log += $"\t{friendly[i]}: {kv2.Value}{Environment.NewLine}";
                        }
                    }
                }
            }

            File.WriteAllText("./log.txt", log);
            Console.WriteLine(log);

            ;


           // MSBT msbt = new MSBT(bytes);

            /*for (int i = 0; i < msbt.entries.Count; i++)
            {
                msbt.entries[i].attributes.unk3 = 1;
                msbt.entries[i].attributes.unk4 = 265;
            }*/
            /*
            Stream outf = File.Open("./out.msbt", FileMode.Create);
            msbt.Write(outf);
            outf.Dispose();

            /*foreach (KeyValuePair<long, Entry> kv in msbt.entries)
            {
                Entry entry = kv.Value;
                Debug.WriteLine($"{entry.key} : {entry.value}");
                if (entry.attributes != null)
                {
                    Attributes a = entry.attributes;
                    Debug.WriteLine($"Attributes:\nUnk1: {a.unk1}\nUnk2: {a.unk2}\nType: {a.type}\nUnk3: {a.unk3}\nUnk4: {a.unk4}\nUnk5: {a.unk5}\nUnk6: {a.unk6}\nUnk7: {BitConverter.ToString(a.unk7)}");
                }
                else
                {
                    Debug.WriteLine("No attributes.");
                }
            }*/

            Console.ReadKey(); // just wait until the user does something so i can actually see the output
        }
    }
}
