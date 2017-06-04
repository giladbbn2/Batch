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

        public string Id;

        public bool IsLoaded
        {
            get;
            private set;
        }
        public bool IsStarted
        {
            get;
            private set;
        }
        
        private ConcurrentDictionary<string, IForeman> foremen;     // key is foremanId



        public Contractor()
        {
            foremen = new ConcurrentDictionary<string, IForeman>();
            IsLoaded = false;
            IsStarted = false;
        }

        public void LoadConfigFile(string PathToConfigFile)
        {
            // Contractor's config file
        }

        //public void ResetTopology

        public void Start()
        {
            // run all long running foremen first and then 
            if (IsStarted)
                throw new Exception("Contractor already started");

            // verify there are not errors before running all foremen

            IsStarted = true;


        }

        public void AddForeman(string PathToConfigFile)
        {
            // check that foremanId doesn't exist
        }

        public void RemoveForeman(string ForemanId)
        {
            // unload if ForemanLoader
        }

        public void AddConnection(string FormanIdFrom, string FormanIdTo, int Weight = 250)
        {
            // max weight is 250
        }
    }
}
