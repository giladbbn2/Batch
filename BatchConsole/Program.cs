using Batch.Foreman;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BatchConsole
{

    class WorkerClassLoader : MarshalByRefObject, IDisposable
    {
        public AppDomain AppDomain;
        public string PathToAssembly;
        public string AppDomainName;

        private Assembly asm;
        private Dictionary<string, Type> WorkerType;    // key is ClassName
        private bool IsDisposed;
        


        public WorkerClassLoader()
        {

        }

        public override object InitializeLifetimeService()
        {
            return null;
        }

        public Type GetWorkerType(string ClassName)
        {
            if (IsDisposed)
                return null;

            if (asm == null)
                asm = Assembly.Load(AssemblyName.GetAssemblyName(PathToAssembly));

            if (WorkerType == null)
                WorkerType = new Dictionary<string, Type>();

            Type t;
            if (WorkerType.TryGetValue(ClassName, out t))
                return t;

            t = asm.GetType(ClassName);
            WorkerType[ClassName] = t;
            return t;
        }

        public static WorkerClassLoader CreateInstance(string AppDomainName, string PathToAssembly)
        {
            AppDomain ad = AppDomain.CreateDomain(AppDomainName);

            var wl = (WorkerClassLoader)ad.CreateInstanceAndUnwrap(typeof(WorkerClassLoader).Assembly.FullName, typeof(WorkerClassLoader).FullName);
            wl.PathToAssembly = PathToAssembly;
            wl.AppDomainName = AppDomainName;
            wl.AppDomain = ad;
            
            return wl;
        }

        public void Dispose()
        {
            IsDisposed = true;
            AppDomain.Unload(AppDomain);
            AppDomain = null;
            WorkerType = null;
            asm = null;
        }
    }

    class Loader : MarshalByRefObject
    {
        private Assembly _assembly;

        public override object InitializeLifetimeService()
        {
            return null;
        }

        public void LoadAssembly(string path)
        {
            _assembly = Assembly.Load(AssemblyName.GetAssemblyName(path));
        }

        public object ExecuteStaticMethod(string typeName, string methodName, params object[] parameters)
        {
            Type type = _assembly.GetType(typeName);
            // TODO: this won't work if there are overloads available
            MethodInfo method = type.GetMethod(
                methodName,
                BindingFlags.Static | BindingFlags.Public);
            return method.Invoke(null, parameters);
        }

        public Type getWorkerType(string typeName)
        {
            return _assembly.GetType(typeName);
        }
    }



    class Program
    {
        static void Main(string[] args)
        {
            AppDomain ad = AppDomain.CreateDomain("Test");

            // Loader lives in another AppDomain
            Loader loader = (Loader)ad.CreateInstanceAndUnwrap(
                typeof(Loader).Assembly.FullName,
                typeof(Loader).FullName);

            loader.LoadAssembly("C:\\projects\\Batch\\BatchTest\\bin\\Debug\\BatchTest.dll");
            var t = loader.getWorkerType("BatchTest.Test2.MyWorker1");


            Console.ReadLine();






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

            var wl = WorkerClassLoader.CreateInstance("TestAppDomain", "C:\\projects\\Batch\\BatchTest\\bin\\Debug\\BatchTest.dll");
            Console.WriteLine(wl.PathToAssembly);

            //var t = wl.GetWorkerType("BatchTest.Test2.MyWorker1");




            Console.ReadLine();

            return;
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
