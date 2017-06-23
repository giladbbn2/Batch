using Batch.Foreman;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Batch.Contractor
{
    public abstract class ContractorBase : IDisposable
    {
        // gets the foreman config file and allocation of a computer

        // creating a new instance of Contractor does:


        // every foreman should be run separately on its own appdomain (logical process)
        // the foreman should be loaded and unloaded manually by Contractor
        // a foreman can have only one assembly defining all workers

        public ContractorSettings Settings;

        public bool IsLoaded
        {
            get;
            private set;
        }

        private ConcurrentDictionary<string, IForeman> _foremen;             // key is foremanId
        private ConcurrentDictionary<string, IForeman> foremen
        {
            get
            {
                if (_foremen == null)
                    Interlocked.CompareExchange(ref _foremen, new ConcurrentDictionary<string, IForeman>(), null);

                return _foremen;
            }
            set
            {
                _foremen = value;
            }
        }

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

        private Regex _RegexAlphaNumeric;
        private Regex RegexAlphaNumeric
        {
            get
            {
                if (_RegexAlphaNumeric == null)
                    Interlocked.CompareExchange(ref _RegexAlphaNumeric, new Regex("^[a-zA-Z0-9]*$"), null);

                return _RegexAlphaNumeric;
            }
        }

        private bool IsDisposed;



        public ContractorBase()
        {
            IsLoaded = false;
            Settings = new ContractorSettings();
        }

        public void ImportFromConfigString(string ConfigString)
        {
            if (IsDisposed)
                throw new ObjectDisposedException("Contractor");

            var config = ParseConfigString(ConfigString);

            foreach (var ccff in config.foremen)
                AddForeman(ccff.id, null, ccff.config);

            foreach (var ccfc in config.connections)
                ConnectForeman(ccfc.from, ccfc.to, false, ccfc.IsTestForeman, ccfc.TestForemanRequestWeight);
        }

        public string ExportToConfigString()
        {
            if (IsDisposed)
                throw new ObjectDisposedException("Contractor");

            var ccf = new ContractorConfigurationFile();
            ccf.foremen = new List<CCFForeman>();
            ccf.connections = new List<CCFConnection>();

            foreach (var kvp in foremen)
            {
                var foreman = kvp.Value;

                ccf.foremen.Add(new CCFForeman() { id = kvp.Key, config = foreman.Config });

                if (foreman.NextForeman != null)
                    ccf.connections.Add(new CCFConnection() { from = foreman.Id, to = foreman.NextForeman.Id, IsTestForeman = false, TestForemanRequestWeight = 0 });

                if (foreman.TestForeman != null)
                    ccf.connections.Add(new CCFConnection() { from = foreman.Id, to = foreman.TestForeman.Id, IsTestForeman = true, TestForemanRequestWeight = foreman.TestForemanRequestWeight });
            }

            return JsonConvert.SerializeObject(ccf);
        }

        public void AddForeman(string ForemanId, string ConfigString)
        {
            AddForeman(ForemanId, ConfigString, null);
        }

        private void AddForeman(string ForemanId, string ConfigString, ForemanConfigurationFile Config)
        {
            IForeman foreman;
            if (foremen.TryGetValue(ForemanId, out foreman))
                throw new Exception("ForemanId already added");

            if (ForemanId == null)
                throw new ArgumentNullException("ForemanId");

            if (ForemanId.Length < 3 || ForemanId.Length > 15)
                throw new ArgumentException("ForemanId must be 3-15 characters in length");

            if (!RegexAlphaNumeric.IsMatch(ForemanId))
                throw new ArgumentException("ForemanId must contain only alpha-numeric characters");

            if (IsDisposed)
                throw new ObjectDisposedException("Contractor");

            if (ForemanId == null || ForemanId.Length == 0)
                throw new ArgumentException("ForemanId");

            if (Config == null)
                if (ConfigString == null || ConfigString.Length == 0)
                    throw new ArgumentException("ConfigString");
            
            if (foremen.ContainsKey(ForemanId))
                throw new Exception(ForemanId + " was already added");

            foreman = ForemanLoader.CreateInstance(ForemanId, ConfigString, Config, Settings);

            foremen.AddOrUpdate(ForemanId, foreman, (k, v) => foreman);
        }

        public void RemoveForeman(string ForemanId)
        {
            // prevent removal if foreman is connected to another foreman

            if (foremen == null)
                return;

            try
            {
                // unload if ForemanLoader
                IForeman foreman;
                if (foremen.TryRemove(ForemanId, out foreman))
                    ForemanLoader.Unload((ForemanLoader)foreman);

                foreman = null;
            }
            catch
            {

            }

            //GC.Collect();
        }

        public void ConnectForeman(string ForemanIdFrom, string ForemanIdTo, bool IsForce = false, bool IsTestForeman = false, int TestForemanRequestWeight = 1000000)
        {
            // max TestForemanRequestWeight is 1000000
            // a foreman can have more than one foreman connecting to it upstream

            if (IsDisposed)
                throw new ObjectDisposedException("Contractor");

            if (ForemanIdFrom == null || ForemanIdFrom.Length == 0)
                throw new ArgumentException("ForemanIdFrom");

            if (ForemanIdTo == null || ForemanIdTo.Length == 0)
                throw new ArgumentException("ForemanIdTo");

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
            if (IsDisposed)
                throw new ObjectDisposedException("Contractor");

        }

        public void Run(string ForemanId, object Data = null, bool IsFollowConnections = true, bool IsContinueOnError = false)
        {
            // passing the Data object without the ref keyword still passes it as reference inside the same assembly
            // but when invoking a method on another assembly the object becomes a ref to a ref thereby doesn't change
            // the actual ref on the assembly where it is created, that's why RunObjectByRef() is invoked directly from
            // the wcf assembly (BatchAgent)

            RunObjectByRef(ForemanId, ref Data, IsFollowConnections, IsContinueOnError);
        }

        public void RunObjectByRef(string ForemanId, ref object Data, bool IsFollowConnections = true, bool IsContinueOnError = false)
        {
            if (IsDisposed)
                throw new ObjectDisposedException("Contractor");

            IForeman foreman;
            if (!foremen.TryGetValue(ForemanId, out foreman))
                throw new Exception("Foreman not found");

            if (foreman.IsNodesLongRunning)
            {
                // long running foreman
                foreman.Run();
                return;
            }

            // short running foreman

            // check if lock is necessary

            RunShortRunningForeman(foreman, ref Data, IsFollowConnections, IsContinueOnError, false);
        }

        public bool SubmitData(string ForemanId, string QueueName, object Data)
        {
            if (IsDisposed)
                throw new ObjectDisposedException("Contractor");

            IForeman foreman;
            if (!foremen.TryGetValue(ForemanId, out foreman))
                throw new Exception("Foreman not found");

            return foreman.SubmitData(QueueName, Data);
        }

        public bool CompleteAdding(string ForemanId, string QueueName)
        {
            if (IsDisposed)
                throw new ObjectDisposedException("Contractor");

            IForeman foreman;
            if (!foremen.TryGetValue(ForemanId, out foreman))
                throw new Exception("Foreman not found");

            return foreman.CompleteAdding(QueueName);
        }

        public ForemanStats GetForemanStats(string ForemanId)
        {
            if (IsDisposed)
                throw new ObjectDisposedException("Contractor");

            if (ForemanId == null || ForemanId.Length == 0)
                throw new ArgumentException("ForemanId");

            IForeman foreman;
            if (!foremen.TryGetValue(ForemanId, out foreman))
                throw new Exception("Foreman " + ForemanId + " not found");

            var mon = foreman.GetAppDomainMonitoringData();

            ForemanStats stats;
            if (mon != null)
                stats = new ForemanStats(ForemanId, mon.Item1, mon.Item2, mon.Item3, mon.Item4, foreman.IsError, foreman.WorkerNodeExceptionString);
            else
                stats = new ForemanStats(ForemanId, 0L, 0L, 0L, new TimeSpan(), foreman.IsError, foreman.WorkerNodeExceptionString);

            return stats;
        }

        public void Dispose()
        {
            IsDisposed = true;

            if (foremen == null)
                return;

            foreach (var kvp in foremen)
                RemoveForeman(kvp.Value.Id);

            foremen = null;
            _rand = null;
        }

        private void RunShortRunningForeman(IForeman Foreman, ref object Data, bool IsFollowConnections, bool IsContinueOnError, bool IsTestForeman)
        {
            // don't follow short running foreman connections

            if (IsDisposed)
                throw new ObjectDisposedException("Contractor");

            if (!IsFollowConnections)
                Foreman.Run(ref Data);

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
        }

        private ContractorConfigurationFile ParseConfigString(string ConfigString)
        {
            if (IsDisposed)
                throw new ObjectDisposedException("Contractor");

            ContractorConfigurationFile Config;

            try
            {
                Config = JsonConvert.DeserializeObject<ContractorConfigurationFile>(ConfigString);
            }
            catch (Exception ex)
            {
                string err = "Can't parse config file: " + ConfigString + "(" + ex.Message + ")";
                throw new Exception(err, ex);
            }

            return Config;
        }
    }
}
