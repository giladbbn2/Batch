using Batch.Contractor;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BatchConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            /*
            var fl = ForemanLoader.CreateInstance(@"C:\projects\Batch\BatchTest\Test2\frmn-test2.config");
            //var fl = ForemanLoader.CreateInstance(@"C:\projects\Batch\BatchTest\Test1\frmn-test1.config");
            fl.Run();

            //var frmn = new Foreman(@"C:\projects\Batch\BatchTest\Test1\frmn-test1.config");
            //frmn.Load();
            //frmn.Run();
            //fl.Unload();

            Console.ReadLine();

            ForemanLoader.Unload(fl);

            Console.ReadLine();
            */


            var c = new Contractor();
            c.AddForeman("frmn1", @"C:\projects\Batch\BatchTest\Test2\frmn-test2.config");
            c.AddForeman("frmn2", @"C:\projects\Batch\BatchTest\Test2\frmn-test2.config");


            int x = 15;
            object o = (object)x;

            //o = c.RunForeman("frmn1", o);
            //Console.WriteLine("1. foreman returned: " + o);
            //o = c.RunForeman("frmn1", o);
            //Console.WriteLine("2. foreman returned: " + o);

            c.AddForemanConnection("frmn1", "frmn2");

            o = c.RunSequence("frmn1", o);

            Console.WriteLine(o);



            c.RemoveForeman("frmn1");
            c.RemoveForeman("frmn2");

            Console.ReadLine();

        }
    }
}
