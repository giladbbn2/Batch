using Batch.Foreman;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Batch.Contractor
{
    public sealed class Contractor : IDisposable
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
        private object emptyObj = new { };

        private static Random _rand;
        private static Random rand
        {
            get
            {
                if (_rand == null)
                    Interlocked.CompareExchange(ref _rand, new Random(), null);

                return _rand;
            }
        }

        private bool IsDisposed;



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
            // prevent removal if foreman is connected to endpoint or another foreman


            // unload if ForemanLoader
            IForeman foreman;
            if (foremen.TryRemove(ForemanId, out foreman))
                ForemanLoader.Unload((ForemanLoader)foreman);

            foreman = null;
        }

        public void ConnectForeman(string ForemanIdFrom, string ForemanIdTo, bool IsForce = false, bool IsTestForeman = false, int TestForemanRequestWeight = 1000000)
        {
            // max TestForemanRequestWeight is 1000000
            // a foreman can have more than one foreman connecting to it upstream

            if (TestForemanRequestWeight < 0 || TestForemanRequestWeight > 1000000)
                throw new ArgumentException("TestForemanRequestWeight must be between 0 (inclusive) and 1000000 (inclusive)");

            if (ForemanIdFrom.Equals(ForemanIdTo))
                throw new Exception("Can't connect Foreman to itself");

            IForeman foremanFrom, foremanTo;

            if (!foremen.TryGetValue(ForemanIdFrom, out foremanFrom))
                throw new Exception("Foreman " + ForemanIdFrom + " not found");

            if (foremanFrom.IsNodesLongRunning)
                throw new Exception("Can't connect a long running foreman");

            if (!foremen.TryGetValue(ForemanIdTo, out foremanTo))
                throw new Exception("Foreman " + ForemanIdTo + " not found");

            if (foremanTo.IsNodesLongRunning)
                throw new Exception("Can't connect a long running foreman");

            if (!IsTestForeman)
            {
                if (foremanFrom.NextForeman != null)
                {
                    if (!IsForce)
                        throw new Exception("Foreman " + ForemanIdFrom + " is already assigned the next foreman");

                    if (foremanFrom.TestForeman != null && foremanFrom.TestForeman.Id.Equals(foremanTo.Id))
                        throw new Exception("Foreman " + ForemanIdTo + " is already assigned as the test foreman to " + ForemanIdFrom);
                }

                foremanFrom.NextForeman = foremanTo;
            }
            else
            {
                if (foremanFrom.TestForeman != null)
                {
                    if (!IsForce)
                        throw new Exception("Foreman " + ForemanIdFrom + " is already assigned the test foreman");

                    if (foremanFrom.NextForeman != null && foremanFrom.NextForeman.Id.Equals(foremanTo.Id))
                        throw new Exception("Foreman " + ForemanIdTo + " is already assigned as the next foreman to " + ForemanIdFrom);
                }

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

            if (foreman.IsNodesLongRunning)
            {
                // long running foreman
                foreman.Run();
                return null;
            }

            // short running foreman

            // check if lock is necessary

            return RunShortRunningForeman(foreman, ref Data, IsFollowConnections, IsContinueOnError, false);
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

        public void Dispose()
        {
            IsDisposed = true;
        }

        private object RunShortRunningForeman(IForeman Foreman, ref object Data, bool IsFollowConnections, bool IsContinueOnError, bool IsTestForeman)
        {
            // don't follow short running foreman connections

            if (!IsFollowConnections)
            {
                Foreman.Run(ref Data);
                return Data;
            }

            // follow short running foreman connections

            while (Foreman != null)
            {
                Foreman.Run(ref Data, IsTestForeman);

                // can get here after foreman threw an unhandled exception
                if (!IsContinueOnError && Foreman.IsError)
                    throw new Exception("Foreman threw an error");

                if (Foreman.TestForeman != null)
                {
                    int w = Foreman.TestForeman.TestForemanRequestWeight;
                    bool isRun = false;

                    if (w == 1000000)
                    {
                        isRun = true;
                    }
                    else if (w > 0)
                    {
                        if (rand.Next(1000001) < w)
                            isRun = true;
                    }

                    if (isRun)
                    {
                        // test foreman before next foreman
                        // blocking
                        RunShortRunningForeman(Foreman.TestForeman, ref Data, true, IsContinueOnError, true);
                    }
                }

                Foreman = Foreman.NextForeman;
            }

            return Data;
        }
    }
}
