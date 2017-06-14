using BatchFoundation.Worker;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BatchTestBL.Test4
{
    // continuation of Test3

    public class MyWorker1 : Worker
    {
        public override void Run(BlockingCollection<object> Input, BlockingCollection<object> Output, ref object Data, bool IsTest)
        {
            dynamic d = Data;
            d.x++;
            d.Name += "-5-";

            if (IsTest)
                Console.WriteLine(DateTime.UtcNow + " - W4 (TEST)");
            else
                Console.WriteLine(DateTime.UtcNow + " - W4");
        }
    }

    public class MyWorker2 : Worker
    {
        public override void Run(BlockingCollection<object> Input, BlockingCollection<object> Output, ref object Data, bool IsTest)
        {
            dynamic d = Data;
            d.x++;
            d.Name += "-6-";

            if (IsTest)
                Console.WriteLine(DateTime.UtcNow + " - W5 (TEST)");
            else
                Console.WriteLine(DateTime.UtcNow + " - W5");
        }
    }
}
