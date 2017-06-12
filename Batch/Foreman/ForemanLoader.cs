using Batch.Foreman;
using Batch.Worker;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Batch.Foreman
{
    internal class ForemanLoader : MarshalByRefObject, IForeman, IDisposable
    {
        public string Id
        {
            get;
            set;
        }
        public string PathToConfigFile;
        public AppDomain AppDomain;
        public bool IsLoaded
        {
            get;
            private set;
        }
        public bool IsNodesLongRunning
        {
            get;
            private set;
        }
        public bool IsError
        {
            get
            {
                if (foreman == null)
                    return false;

                return foreman.IsError;
            }
        }
        public object Data
        {
            get;
            set;
        }

        public IForeman NextForeman
        {
            get;
            set;
        }
        public IForeman TestForeman
        {
            get;
            set;
        }
        public int TestForemanRequestWeight
        {
            get;
            set;
        }

        public IEnumerable<WorkerNodeState> WorkerNodeStates
        {
            get
            {
                if (foreman == null)
                    return null;

                return foreman.WorkerNodeStates;
            }
        }

        private ForemanBase foreman;
        private bool IsDisposed;



        public ForemanLoader()
        {
            IsLoaded = false;
        }

        public override object InitializeLifetimeService()
        {
            return null;
        }

        public void Load()
        {
            if (IsDisposed)
                return;

            foreman = new Foreman(PathToConfigFile);
            foreman.Id = Id;
            foreman.Load();
            IsNodesLongRunning = foreman.IsNodesLongRunning;

            IsLoaded = true;
        }

        public void Run(bool IsTestForeman = false)
        {
            if (IsDisposed)
                return;

            if (!IsLoaded)
                throw new Exception("ForemanLoader not loaded yet");

            foreman.Data = Data;
            foreman.NextForeman = NextForeman;
            foreman.TestForeman = TestForeman;
            foreman.TestForemanRequestWeight = TestForemanRequestWeight;

            foreman.Run(IsTestForeman);

            Data = foreman.Data;
        }

        public void Pause()
        {
            if (IsDisposed)
                return;

            foreman.Pause();
        }

        public void Resume()
        {
            if (IsDisposed)
                return;

            foreman.Resume();
        }

        public bool SubmitData(string QueueName, object data)
        {
            if (IsDisposed)
                return false;

            if (!IsNodesLongRunning)
                throw new Exception("SubmitData() is used only in long running foremen");

            if (QueueName == null)
                throw new ArgumentNullException("QueueName");

            if (data == null)
                throw new ArgumentNullException("data");

            return foreman.SubmitData(QueueName, data);
        }

        public bool CompleteAdding(string QueueName)
        {
            if (IsDisposed)
                return false;

            if (!IsNodesLongRunning)
                throw new Exception("CompleteAdding() is used only in long running foremen");

            if (QueueName == null)
                throw new ArgumentNullException("QueueName");

            return foreman.CompleteAdding(QueueName);
        }

        public void Dispose()
        {
            // AppDomain.Unload() must be executed on parent AppDomain

            IsDisposed = true;

            if (foreman != null)
                foreman.Dispose();

            foreman = null;
        }

        /*
        public void Load(AppDomain AppDomain, string AppDomainName, string PathToAssembly)
        {
            if (Disposed)
                return;

            if (IsLoaded || asm != null)
                throw new Exception("Load() was already executed");

            this.AppDomain = AppDomain;
            this.PathToAssembly = PathToAssembly;
            this.AppDomainName = AppDomainName;

            asm = Assembly.Load(AssemblyName.GetAssemblyName(this.PathToAssembly));

            IsLoaded = true;
        }

        public void Run()
        {
            if (Disposed)
                return;

            if (!IsLoaded)
                throw new Exception("Load() must be executed before Run()");

            
            //var t = asm.GetTypes().First(x => x.FullName.Equals(WorkerClassName));
            //BatchFoundation.Worker.Worker w = (BatchFoundation.Worker.Worker)Activator.CreateInstance(t);
            //w.Run(Input, Output, ref data);
        }
            
        public static void Init()
        {
            if (!isInit)
                lock (wlsync)
                {
                    if (!isInit)
                    {
                        ForemanIdToLoader = new ConcurrentDictionary<string, ForemanLoader>();
                        isInit = true;
                    }
                }
        }

            public static ForemanLoader RegisterInstance(string PathToConfigFile)
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

                
                //int x = 1;
                //object o = (object)x;
                //wl.Run("BatchTest.Test2.MyWorker2", null, null, ref o);
                //Console.WriteLine(o);
                //Console.ReadLine();
                
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
        */

        public static ForemanLoader CreateInstance(string Id, string PathToConfigFile)
        {
            AppDomain ad = AppDomain.CreateDomain(Guid.NewGuid().ToString());

            var fl = (ForemanLoader)ad.CreateInstanceAndUnwrap(typeof(ForemanLoader).Assembly.FullName, typeof(ForemanLoader).FullName);

            fl.Id = Id;
            fl.AppDomain = ad;
            fl.PathToConfigFile = PathToConfigFile;
            fl.Load();

            return fl;
        }

        public static void Unload(ForemanLoader Loader)
        {
            if (Loader == null)
                return;

            Loader.Dispose();

            try
            {
                AppDomain.Unload(Loader.AppDomain);
            }
            catch (Exception ex)
            {
                // log errors
                // if code got here then AppDomain was not unloaded
                // apparently because a finally block or unmanaged code which didn't finish running
            }
            
        }
    }
}
