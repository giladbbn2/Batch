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
            Test1();
        }

        public static void Test1()
        {
            var c = new Contractor();
            c.AddForeman("frmn1", @"C:\projects\Batch\BatchTestBL\Test1\frmn-test1.config");

            c.Run("frmn1");

            // trigger SubmitData
            Console.ReadLine();
            int x = 15;
            object o = (object)x;

            if (c.SubmitData("frmn1", "q1", o))
                Console.WriteLine("Data submitted!");
            else
                Console.WriteLine("Data wasn't submitted!");

            // trigger SubmitData
            Console.ReadLine();
            int x2 = 24;
            object o2 = (object)x2;

            if (c.SubmitData("frmn1", "q1", o2))
                Console.WriteLine("Data submitted!");
            else
                Console.WriteLine("Data wasn't submitted!");

            // trigger CompleteAdding
            Console.ReadLine();
            if (c.CompleteAdding("frmn1", "q1"))
                Console.WriteLine("complete adding success!");
            else
                Console.WriteLine("complete adding fail!");

            // trigger SubmitData
            Console.ReadLine();
            int x3 = 24;
            object o3 = (object)x3;

            if (c.SubmitData("frmn1", "q1", o3))
                Console.WriteLine("Data submitted!");
            else
                Console.WriteLine("Data wasn't submitted!");

            // trigger CompleteAdding
            Console.ReadLine();
            if (c.CompleteAdding("frmn1", "q1"))
                Console.WriteLine("complete adding success!");
            else
                Console.WriteLine("complete adding fail!");

            // trigger unload
            Console.ReadLine();
            c.RemoveForeman("frmn1");

            // trigger unload
            Console.ReadLine();
            c.RemoveForeman("frmn1");

            Console.ReadLine();
        }

        public static void Test2()
        {
            var c = new Contractor();
            c.AddForeman("frmn1", @"C:\projects\Batch\BatchTestBL\Test2\frmn-test2.config");
            c.AddForeman("frmn2", @"C:\projects\Batch\BatchTestBL\Test2\frmn-test2.config");
            c.ConnectForeman("frmn1", "frmn2");

            int x = 15;
            object o = (object)x;

            o = c.Run("frmn1", o);

            Console.WriteLine(o);

            c.RemoveForeman("frmn1");
            c.RemoveForeman("frmn2");

            Console.ReadLine();
        }
    }
}
