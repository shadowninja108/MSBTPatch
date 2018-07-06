using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSBTPatch
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            byte[] bytes = File.ReadAllBytes("C:/sw_nand/splatoon_2_3.1.0/dec/Message/CommonMsg_USen/AmPm.msbt");
            new MSBT(bytes);
            Console.ReadKey();
        }
    }
}
