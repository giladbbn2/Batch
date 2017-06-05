using Batch.Foreman;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Batch.Contractor
{
    public class Contractor
    {
        // gets the foreman config file and allocation of a computer

        // creating a new instance of Contractor does:


        // every foreman should be run separately on its own appdomain (logical process)
        // the foreman should be loaded and unloaded manually by Contractor
        // a foreman can have only one assembly defining all workers

        public bool IsLoaded
        {
            get;
            private set;
        }
        public bool IsLongRunningForemenRun
        {
            get;
            private set;
        }
        
        private ConcurrentDictionary<string, IForeman> foremen;             // key is foremanId



        public Contractor()
        {
            foremen = new ConcurrentDictionary<string, IForeman>();
            IsLoaded = false;
            IsLongRunningForemenRun = false;
        }

        public void LoadConfigFile(string PathToConfigFile)
        {
            // Contractor's config file
        }

        public void Reset()
        {
            // try to stop all long running foremen and
            // try to remove all foremen connections and all foremen
        }

        public void RunLongRunningForemen()
        {
            if (IsLongRunningForemenRun)
                throw new Exception("Long running foreman already run");

            // verify there are no errors before running all long running foremen

            foreach (var foreman in foremen.Values)
                if (foreman.IsNodesLongRunning)
                    foreman.Run();

            IsLongRunningForemenRun = true;
        }

        public void StopLongRunningForemen()
        {

        }

        public void AddForeman(string ForemanId, string PathToConfigFile)
        {
            // check that foremanId doesn't exist

            if (foremen.ContainsKey(ForemanId))
                throw new Exception(ForemanId + " was already added");

            var foreman = ForemanLoader.CreateInstance(ForemanId, PathToConfigFile);

            foremen.AddOrUpdate(ForemanId, foreman, (k, v) => foreman);
        }

        public void RemoveForeman(string ForemanId)
        {
            // unload if ForemanLoader
            IForeman foreman;
            if (foremen.TryRemove(ForemanId, out foreman))
                ForemanLoader.Unload((ForemanLoader)foreman);

            foreman = null;
        }

        public void AddForemanConnection(string ForemanIdFrom, string ForemanIdTo, int BranchRequestWeight = 1000, bool IsBranchForeman = false)
        {
            // max BackupRequestWeight is 1000

            if (ForemanIdFrom.Equals(ForemanIdTo))
                throw new Exception("Can't connect Foreman to itself");

            IForeman foremanFrom, foremanTo;

            if (!foremen.TryGetValue(ForemanIdFrom, out foremanFrom))
                throw new Exception("Foreman " + ForemanIdFrom + " not found");

            if (!foremen.TryGetValue(ForemanIdTo, out foremanTo))
                throw new Exception("Foreman " + ForemanIdTo + " not found");

            if (!IsBranchForeman)
            {
                if (foremanFrom.NextForeman != null)
                    throw new Exception("Foreman " + ForemanIdFrom + " was already assigned the next foreman");

                foremanFrom.NextForeman = foremanTo;
            }
            else
            {
                if (foremanFrom.BranchForeman != null)
                    throw new Exception("Foreman " + ForemanIdFrom + " was already assigned the backup foreman");

                foremanFrom.BranchForeman = foremanTo;
                foremanTo.BranchRequestWeight = BranchRequestWeight;
            }
        }

        public void RemoveForemanConnection(string ForemanIdFrom, string ForemanIdTo)
        {

        }

        public object RunSingleForeman(string ForemanId, object Data)
        {
            IForeman foreman;
            if (foremen.TryGetValue(ForemanId, out foreman))
            {
                foreman.Data = Data;
                foreman.Run();
                return foreman.Data;
            }

            return null;
        }

        public object RunSequence(string ForemanId, object Data)
        {
            IForeman foreman;
            if (foremen.TryGetValue(ForemanId, out foreman))
            {

                while (foreman != null)
                {
                    foreman.Data = Data;
                    foreman.Run();
                    Data = foreman.Data;

                    foreman = foreman.NextForeman;
                    // backup forman
                }

                return Data;
                
            }

            return null;
        }

        public void AddEndpointConnection(string Endpoint, string ForemanIdTo)
        {

        }

        public void RemoveEndpointConnection(string Endpoint)
        {

        }
    }
}
