using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BatchFoundation.Worker
{
    public class ForemanLoader : MarshalByRefObject, IDisposable
    {
        public AppDomain AppDomain
        {
            get;
            private set;
        }
        public string AppDomainName
        {
            get;
            private set;
        }
        public bool isLoaded
        {
            get;
            private set;
        }
        public string PathToAssembly
        {
            get;
            private set;
        }
        
        private Assembly asm;
        private bool Disposed;

        private static object wlsync = new object();
        private static bool isInit = false;
        public static ConcurrentDictionary<string, ForemanLoader> AppDomainToWorkerLoader;
        public static ConcurrentDictionary<string, ForemanLoader> AssemblyPathToWorkerLoader;



        public ForemanLoader()
        {
            isLoaded = false;
        }

        public override object InitializeLifetimeService()
        {
            return null;
        }

        public void Load(AppDomain AppDomain, string AppDomainName, string PathToAssembly)
        {
            if (Disposed)
                return;

            if (isLoaded || asm != null)
                throw new Exception("Load() was already executed");

            this.AppDomain = AppDomain;
            this.PathToAssembly = PathToAssembly;
            this.AppDomainName = AppDomainName;

            asm = Assembly.Load(AssemblyName.GetAssemblyName(this.PathToAssembly));

            isLoaded = true;
        }

        public void Run(string WorkerClassName, BlockingCollection<object> Input, BlockingCollection<object> Output, ref object data)
        {
            if (Disposed)
                return;

            if (!isLoaded)
                throw new Exception("Load() must be executed before Run()");

            var t = asm.GetTypes().First(x => x.FullName.Equals(WorkerClassName));
            BatchFoundation.Worker.Worker w = (BatchFoundation.Worker.Worker)Activator.CreateInstance(t);
            w.Run(Input, Output, ref data);
        }

        public void Dispose()
        {
            if (Disposed)
                return;

            Disposed = true;
            AppDomain.Unload(AppDomain);
            AppDomain = null;
            asm = null;
        }

        public static void Init()
        {
            if (!isInit)
                lock (wlsync)
                {
                    if (!isInit)
                    {
                        AppDomainToWorkerLoader = new ConcurrentDictionary<string, ForemanLoader>();
                        AssemblyPathToWorkerLoader = new ConcurrentDictionary<string, ForemanLoader>();
                        isInit = true;
                    }
                }
        }

        public static ForemanLoader RegisterInstance(string AppDomainName, string PathToAssembly)
        {
            if (!isInit)
                Init();

            if (AppDomainName == null)
                throw new ArgumentNullException("AppDomainName");

            if (PathToAssembly == null)
                throw new ArgumentNullException("PathToAssembly");

            // check that there isn't an app domain already for this name and path
            ForemanLoader wl, wlAppDomain, wlPath;

            bool isAppDomain = AppDomainToWorkerLoader.TryGetValue(AppDomainName, out wlAppDomain);
            bool isPathToAssembly = AssemblyPathToWorkerLoader.TryGetValue(PathToAssembly, out wlPath);

            if (isAppDomain && isPathToAssembly)
            {
                // both found
                wl = wlAppDomain;
            }
            else if (isAppDomain && !isPathToAssembly)
            {
                // another path was found with the same app domain name
                throw new Exception("Cannot declare two assemblies with the same name but different paths");
            }
            else if (!isAppDomain && isPathToAssembly)
            {
                // another app domain was found with the same path
                throw new Exception("Cannot declare two assemblies with the same path but different names");
                //AppDomainToWorkerLoader.AddOrUpdate(AppDomainName, wlPath, (k, v) => wlPath);
                //wl = wlPath;
            }
            else
            {
                // both not found so create new and don't forget to Load()
                AppDomain ad = AppDomain.CreateDomain(AppDomainName);

                wl = (ForemanLoader)ad.CreateInstanceAndUnwrap(typeof(ForemanLoader).Assembly.FullName, typeof(ForemanLoader).FullName);
                wl.Load(ad, AppDomainName, PathToAssembly);

                AppDomainToWorkerLoader.AddOrUpdate(AppDomainName, wl, (k, v) => wl);
                AssemblyPathToWorkerLoader.AddOrUpdate(PathToAssembly, wl, (k, v) => wl);

                /*
                int x = 1;
                object o = (object)x;
                wl.Run("BatchTest.Test2.MyWorker2", null, null, ref o);
                Console.WriteLine(o);
                Console.ReadLine();
                */
            }
            
            return wl;
        }

        public static bool TryGetInstanceByAppDomainName(string AppDomainName, out ForemanLoader wl)
        {
            return AppDomainToWorkerLoader.TryGetValue(AppDomainName, out wl);
        }

        public static bool TryGetInstanceByPathToAssembly(string PathToAssembly, out ForemanLoader wl)
        {
            return AssemblyPathToWorkerLoader.TryGetValue(PathToAssembly, out wl);
        }

        public static void UnloadByAppDomainName(string AppDomainName)
        {
            // first check that this assembly is never used in current configs!

            ForemanLoader wlAppDomain, wlPath;

            AppDomainToWorkerLoader.TryRemove(AppDomainName, out wlAppDomain);

            if (wlAppDomain != null)
            {
                AssemblyPathToWorkerLoader.TryRemove(wlAppDomain.PathToAssembly, out wlPath);
                wlAppDomain.Dispose();
                wlPath.Dispose();
            }
        }

        public static void UnloadByPathToAssembly(string PathToAssembly)
        {
            // first check that this assembly is never used in current configs!

            ForemanLoader wlAppDomain, wlPath;

            AssemblyPathToWorkerLoader.TryRemove(PathToAssembly, out wlPath);

            if (wlPath != null)
            {
                AppDomainToWorkerLoader.TryRemove(wlPath.AppDomainName, out wlAppDomain);
                wlAppDomain.Dispose();
                wlPath.Dispose();
            }
        }
    }
}
