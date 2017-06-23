using BatchFoundation.Worker;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BatchTestBL.Test7
{
    public class Test7Object
    {
        public int a;
    }

    public class MyWorker1 : Worker
    {
        public override void Run(BlockingCollection<object> Input, BlockingCollection<object> Output, ref object Data, bool IsTest)
        {
            // Using Batch directly in your project doesn't require the BL to hold a reference to Batch because the project itself would have the ref
            // But in order to load Foreman via BatchAgent (RemoteContractor) the Foreman DLL (BL) MUST contain a reference also to Batch and not only to BatchFoundation

            // When working via BatchAgent Console.WriteLine doesn't work!

            // Console.WriteLine(Data);

            var o = JsonConvert.DeserializeObject<Test7Object>(Data.ToString());

            o.a += 50;

            Data = JsonConvert.SerializeObject(o);
        }
    }
}
