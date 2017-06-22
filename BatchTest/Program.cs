using Batch.Contractor;
using BatchTestBL.Test3;
using BatchTestBL.Test6;
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
            Test6();
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

            try
            {
                c.AddForeman(null, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine("An exception is thrown from Contractor but it is not crashed! (" + ex.Message + ")");
            }
            
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

            c.ConnectForeman("frmn1", "frmn2", false, true, 1000000);
            c.ConnectForeman("frmn3", "frmn2");
            c.ConnectForeman("frmn3", "frmn1", false, true, 1000000);

            // it is possible to connect frmn2 to frmn1 (or other circular pattern) that would run infinitely
            // or until stack overflow might occur as currently the test foreman is run in recursion

            // frmn2 is downstream both after frmn1 and frmn3
            // it is possible to run directly frmn2 without going through frmn1 or frmn3 first

            Person p = new Person();
            p.x = 0;
            object o = (object)p; 

            // the following statement will actually run:
            // 1. frmn3
            // 2. frmn1 (test)
            // 3. frmn2 (test)
            // 4. frmn2

            c.Run("frmn3", o, true, true);
            
            p = (Person)o;
            Console.WriteLine(p.x);

            Console.ReadLine();

            c.Run("frmn1", o, true, true);

            Console.ReadLine();


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
            // 1.4MB

            Console.WriteLine("About to init Contractor");
            Console.ReadLine();

            int x = 0;
            object o = (object)x;

            var c = new Contractor();

            // 1.4MB

            Console.WriteLine("About to load Contractor from config file");
            Console.ReadLine();

            string configString = File.ReadAllText(@"C:\projects\Batch\BatchTestBL\Test5\ctr-test5.config");

            c.ImportFromConfigString(configString); 

            // 4.3MB - 4.4MB

            GC.Collect();
            Console.WriteLine("Foremen (App Domains) Monitoring (in bytes):");



            Console.WriteLine(c.GetForemanStats("frmn1"));
            Console.WriteLine(c.GetForemanStats("frmn2"));
            Console.WriteLine("Application total memory (bytes) usage: " + GC.GetTotalMemory(false));

            Console.WriteLine("About to run");
            Console.ReadLine();

            c.Run("frmn1", o);

            Console.WriteLine(o);

            configString = c.ExportToConfigString();

            // 5.0MB - 5.2MB

            GC.Collect();
            Console.WriteLine("Foremen (App Domains) Monitoring (in bytes):");
            Console.WriteLine(c.GetForemanStats("frmn1"));
            Console.WriteLine(c.GetForemanStats("frmn2"));
            Console.WriteLine("Application total memory (bytes) usage: " + GC.GetTotalMemory(false));


            Console.WriteLine("About to dispose Contractor");
            Console.ReadLine();

            //c.RemoveForeman("frmn1");
            //c.RemoveForeman("frmn2");

            c.Dispose();
            c = null;

            // 4.3MB - 4.5MB

            Console.WriteLine("Application total memory (bytes) usage: " + GC.GetTotalMemory(false));
            Console.WriteLine("About to init Contractor again");
            Console.ReadLine();

            c = new Contractor();

            c.ImportFromConfigString(configString);

            c.Run("frmn1", o);

            Console.WriteLine(o);

            c.RemoveForeman("frmn1");
            c.RemoveForeman("frmn2");

            // 4.3MB - 4.5MB

            Console.WriteLine("Application total memory (bytes) usage: " + GC.GetTotalMemory(false));
            Console.WriteLine("About to dispose Contractor");
            Console.ReadLine();

            c.Dispose();

            // 4.3MB - 4.5MB

            Console.WriteLine("Application total memory (bytes) usage: " + GC.GetTotalMemory(true));
            Console.WriteLine("About to finish");
            Console.ReadLine();

            Console.ReadLine();
        }

        public static void Test6()
        {
            // this test demonstrates how to replace a Foreman

            // also how to use ContractSettings to set a default directory for all Foremen Dlls which can be a UNC path 
            // on the local area network!

            var c = new Contractor();

            c.Settings.ForemanFetchDLLBaseDir = "C:\\projects\\Batch\\BatchTestBL\\bin\\Debug";
            c.Settings.IsKeepLocalForemanDLL = true;
            c.Settings.IsOverwriteLocalForemanDLL = true;
            c.Settings.ForemanLocalDLLBaseDir = "C:\\projects\\Batch\\Local";

            string configString = File.ReadAllText(@"C:\projects\Batch\BatchTestBL\Test6\frmn-test6-1.config");

            c.AddForeman("frmn1", configString);
            c.AddForeman("frmn2", configString);
            c.AddForeman("frmn3", configString);

            configString = File.ReadAllText(@"C:\projects\Batch\BatchTestBL\Test6\frmn-test6-2.config");

            c.AddForeman("frmn4", configString);

            c.ConnectForeman("frmn1", "frmn2");
            c.ConnectForeman("frmn2", "frmn3");

            var o = new NumberHolder();
            o.Number = 0;

            c.Run("frmn1", o);

            Console.WriteLine(o.Number);
            Console.ReadLine();

            // first connect the new Foreman to the Foreman downstream

            c.ConnectForeman("frmn4", "frmn3");

            c.Run("frmn1", o);

            Console.WriteLine(o.Number);
            Console.ReadLine();

            // now replace the connection between the Foreman upstream to the new Foreman

            c.ConnectForeman("frmn1", "frmn4", true);

            c.Run("frmn1", o);

            Console.WriteLine(o.Number);
            Console.ReadLine();

            c.Run("frmn1", o);

            Console.WriteLine(o.Number);
            Console.ReadLine();
        }
    }
}
