using Batch.Contractor;
using BatchTestBL.Test3;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
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

            string configString = File.ReadAllText(@"C:\projects\Batch\BatchTestBL\Test1\frmn-test1.config");
            c.AddForeman("frmn1", configString);

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
            //Console.ReadLine();
            //c.RemoveForeman("frmn1");

            // trigger unload
            //Console.ReadLine();
            //c.RemoveForeman("frmn1");

            // trigger contarctor dispose
            Console.ReadLine();

            c.Dispose();

            Console.ReadLine();
        }

        public static void Test2()
        {
            var c = new Contractor();

            string configString = File.ReadAllText(@"C:\projects\Batch\BatchTestBL\Test2\frmn-test2.config");
            c.AddForeman("frmn1", configString);
            c.AddForeman("frmn2", configString);

            c.ConnectForeman("frmn1", "frmn2", false);

            int x = 0;
            object o = (object)x;

            c.Run("frmn1", o);

            Console.WriteLine(o);

            c.RemoveForeman("frmn1");
            c.RemoveForeman("frmn2");

            Console.ReadLine();
        }

        public static void Test3()
        {
            var c = new Contractor();

            string configString = File.ReadAllText(@"C:\projects\Batch\BatchTestBL\Test3\frmn-test3.config");

            c.AddForeman("frmn1", configString);
            c.AddForeman("frmn2", configString);

            configString = File.ReadAllText(@"C:\projects\Batch\BatchTestBL\Test4\frmn-test4.config");

            c.AddForeman("frmn3", configString);

            c.ConnectForeman("frmn1", "frmn2", false); //, true, 100000);
            c.ConnectForeman("frmn3", "frmn2");

            // frmn2 is downstream both after frmn1 and frmn3
            // it is possible to run directly "frmn2" without going through frmn1 or frmn3 first

            Person p = new Person();
            p.x = 0;
            object o = (object)p; 

            c.Run("frmn1", o, true, true);
            
            p = (Person)o;
            Console.WriteLine(p.x);

            c.Run("frmn3", o, true, true);

            //Console.ReadLine();
            //return;

            p = (Person)o;
            Console.WriteLine(p.x);
            Console.WriteLine(p.Name);

            // isContinueOnError is by default set to false
            // so following statements throw an error

            c.Run("frmn1", o);
            c.Run("frmn1", o);
            c.Run("frmn1", o);

            p = (Person)o;
            Console.WriteLine(p.x);

            c.RemoveForeman("frmn1");
            c.RemoveForeman("frmn2");

            Console.ReadLine();
        }

        public static void Test5()
        {
            int x = 0;
            object o = (object)x;

            Console.WriteLine("About to init Contractor");
            Console.ReadLine();

            var c = new Contractor();

            Console.WriteLine("About to load Contractor from config file");
            Console.ReadLine();

            string configString = File.ReadAllText(@"C:\projects\Batch\BatchTestBL\Test5\ctr-test5.config");

            c.ImportFromConfigString(configString);

            Console.WriteLine("About to run");
            Console.ReadLine();

            c.Run("frmn1", o);

            Console.WriteLine(o);

            configString = c.ExportToConfigString();

            Console.WriteLine("About to dispose Contractor");
            Console.ReadLine();

            c.Dispose();
            c = null;


            


            Console.WriteLine("About to init Contractor again");
            Console.ReadLine();

            c = new Contractor();

            c.ImportFromConfigString(configString);

            c.Run("frmn1", o);

            Console.WriteLine(o);

            c.RemoveForeman("frmn1");
            c.RemoveForeman("frmn2");

            Console.WriteLine("About to dispose Contractor");
            Console.ReadLine();

            c.Dispose();

            Console.WriteLine("About to finish");
            Console.ReadLine();

            Console.ReadLine();
        }
    }
}
