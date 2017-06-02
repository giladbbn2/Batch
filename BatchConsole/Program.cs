using Batch.Foreman;
using BatchFoundation.Worker;
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
            var wl = WorkerLoader.CreateInstance("Test", "C:\\projects\\Batch\\BatchTest\\bin\\Debug\\BatchTest.dll");
            int x = 12;
            object o = (object)x;
            wl.Run("BatchTest.Test2.MyWorker2", null, null, ref o);

            Console.WriteLine("The number: " + o);
            Console.ReadLine();
            return;
            */





            /*
            AppDomain ad = AppDomain.CreateDomain("Test");

            // Loader lives in another AppDomain
            Loader loader = (Loader)ad.CreateInstanceAndUnwrap(
                typeof(Loader).Assembly.FullName,
                typeof(Loader).FullName);

            loader.LoadAssembly("C:\\projects\\Batch\\BatchTest\\bin\\Debug\\BatchTest.dll");
            var t = loader.getWorkerType("BatchTest.Test2.MyWorker1");


            Console.ReadLine();



            */


            /*
            WorkerLoader loader = (WorkerLoader)ad.CreateInstanceAndUnwrap(
                typeof(WorkerLoader).Assembly.FullName,
                typeof(WorkerLoader).FullName
            );

            loader.LoadAssembly("C:\\projects\\Batch\\BatchTest\\bin\\Debug\\BatchTest.dll");

            // Create application domain setup information.
            AppDomainSetup domaininfo = new AppDomainSetup();
            domaininfo.ApplicationBase = "C:\\projects\\Batch\\BatchTest\\bin\\Debug\\BatchTest.dll";

            // Create the application domain.
            AppDomain domain = AppDomain.CreateDomain("MyDomain", null, domaininfo);
            foreach (Assembly asm in domain.GetAssemblies()) //AppDomain.CurrentDomain.GetAssemblies())
                foreach (Type t in asm.GetTypes())
                    //Console.WriteLine(t.GetTypeInfo().FullName);
                    if (t.GetTypeInfo().FullName.Contains("BatchTest.Test2.MyWorker1"))
                        Console.WriteLine("1");


                //Console.WriteLine(asm.GetName());
            */

            //var wl = WorkerLoader.CreateInstance("TestAppDomain", "C:\\projects\\Batch\\BatchTest\\bin\\Debug\\BatchTest.dll");
            //Console.WriteLine(wl.PathToAssembly);

            //var t = wl.GetWorkerType("BatchTest.Test2.MyWorker1");




            //Console.ReadLine();

            //return;
            /*
            // Write application domain information to the console.
            Console.WriteLine("Host domain: " + AppDomain.CurrentDomain.FriendlyName);
            Console.WriteLine("child domain: " + domain.FriendlyName);
            Console.WriteLine("Application base is: " + domain.SetupInformation.ApplicationBase);

            // Unload the application domain.
            AppDomain.Unload(domain);
            */


            var frmn = new Foreman(@"C:\projects\Batch\BatchTest\Test2\frmn-test2.config");
            frmn.Load();
            frmn.Run();

            Console.ReadLine();
        }
    }
}
