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
        
        private ConcurrentDictionary<string, IForeman> foremen;             // key is foremanId



        public Contractor()
        {
            foremen = new ConcurrentDictionary<string, IForeman>();
            IsLoaded = false;
        }

        public void ImportConfigFile(string PathToConfigFile)
        {
            // Contractor's config file
        }

        public ContractorConfigurationFile ExportConfigFile()
        {
            var ccf = new ContractorConfigurationFile();

            // ...

            return ccf;
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

        public void ConnectForeman(string ForemanIdFrom, string ForemanIdTo, bool IsTestForeman = false, int TestForemanRequestWeight = 1000000)
        {
            // max TestForemanRequestWeight is 1000000

            if (ForemanIdFrom.Equals(ForemanIdTo))
                throw new Exception("Can't connect Foreman to itself");

            IForeman foremanFrom, foremanTo;

            if (!foremen.TryGetValue(ForemanIdFrom, out foremanFrom))
                throw new Exception("Foreman " + ForemanIdFrom + " not found");

            if (foremanFrom.IsNodesLongRunning)
                throw new Exception("Can't connect long running foremen");

            if (!foremen.TryGetValue(ForemanIdTo, out foremanTo))
                throw new Exception("Foreman " + ForemanIdTo + " not found");

            if (foremanFrom.IsNodesLongRunning)
                throw new Exception("Can't connect long running foremen");

            if (!IsTestForeman)
            {
                if (foremanFrom.NextForeman != null)
                    throw new Exception("Foreman " + ForemanIdFrom + " was already assigned the next foreman");

                foremanFrom.NextForeman = foremanTo;
            }
            else
            {
                if (foremanFrom.TestForeman != null)
                    throw new Exception("Foreman " + ForemanIdFrom + " was already assigned the test foreman");

                foremanFrom.TestForeman = foremanTo;
                foremanTo.TestForemanRequestWeight = TestForemanRequestWeight;
            }
        }

        public void DisconnectForeman(string ForemanIdFrom, string ForemanIdTo)
        {
            
        }

        public object Run(string ForemanId, object Data = null, bool IsFollowConnections = true, bool IsContinueOnError = false)
        {
            IForeman foreman;
            if (!foremen.TryGetValue(ForemanId, out foreman))
                throw new Exception("Foreman not found");

            try
            {
                if (foreman.IsNodesLongRunning)
                {
                    foreman.Run();
                    return null;
                }

                // short running foreman

                if (!IsFollowConnections)
                {
                    foreman.Data = Data;
                    foreman.Run();
                    return foreman.Data;
                }

                // follow short running foreman connections

                while (foreman != null)
                {

                    // test foreman!
                    
                    //if (foreman.TestForeman != null)



                    foreman.Data = Data;
                    foreman.Run();

                    // can get here after foreman threw an unhandled exception (foreman is still running)
                    if (!IsContinueOnError && foreman.IsError)
                    {
                        throw new Exception("Foreman threw an error");
                    }

                    Data = foreman.Data;

                    foreman = foreman.NextForeman;

                    // branch foreman async?
                }

                return Data;
            }
            catch (Exception ex)
            {

                throw new Exception("Error running foreman", ex);
            }
        }

        public bool SubmitData(string ForemanId, string QueueName, object Data)
        {
            IForeman foreman;
            if (!foremen.TryGetValue(ForemanId, out foreman))
                throw new Exception("Foreman not found");

            return foreman.SubmitData(QueueName, Data);
        }

        public bool CompleteAdding(string ForemanId, string QueueName)
        {
            IForeman foreman;
            if (!foremen.TryGetValue(ForemanId, out foreman))
                throw new Exception("Foreman not found");

            return foreman.CompleteAdding(QueueName);
        }

        public void AddEndpoint(string Endpoint) { }

        public void RemoveEndpoint(string Endpoint) { }

        public void ConnectEndpointToForeman(string Endpoint, string ForemanId, string QueueName = null)
        {
            if (QueueName == null)
            {
                // connect endpoint to a short running foreman
            }
            else
            {
                // connect endpoint to a queue inside a long running foreman
            }
        }

        public void DisconnectEndpoint(string Endpoint)
        {

        }
    }
}
