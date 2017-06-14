using BatchFoundation.Worker;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BatchTestBL.Test2
{
    public class MyWorker1 : Worker
    {
        public override void Run(BlockingCollection<object> Input, BlockingCollection<object> Output, ref object Data, bool IsTest)
        {
            Data = (object)((int)Data + 5);
            Console.WriteLine(DateTime.UtcNow + " - W1: " + Data);
        }
    }

    public class MyWorker2 : Worker
    {
        public override void Run(BlockingCollection<object> Input, BlockingCollection<object> Output, ref object Data, bool IsTest)
        {
            Data = (object)((int)Data + 1);
            Console.WriteLine(DateTime.UtcNow + " - W2: " + Data);
        }
    }

    public class MyWorker3 : Worker
    {
        public override void Run(BlockingCollection<object> Input, BlockingCollection<object> Output, ref object Data, bool IsTest)
        {
            Data = (object)((int)Data + 1);
            Console.WriteLine(DateTime.UtcNow + " - W3: " + Data);
        }
    }

    public class MyWorker4 : Worker
    {
        public override void Run(BlockingCollection<object> Input, BlockingCollection<object> Output, ref object Data, bool IsTest)
        {
            Data = (object)((int)Data + 1);
            Console.WriteLine(DateTime.UtcNow + " - W4: " + Data);
        }
    }

    public class MyWorker5 : Worker
    {
        public override void Run(BlockingCollection<object> Input, BlockingCollection<object> Output, ref object Data, bool IsTest)
        {
            Data = (object)((int)Data + 1);
            Console.WriteLine(DateTime.UtcNow + " - W5: " + Data);
        }
    }

    public class MyWorker6 : Worker
    {
        public override void Run(BlockingCollection<object> Input, BlockingCollection<object> Output, ref object Data, bool IsTest)
        {
            //Thread.Sleep(3000);
            Data = (object)((int)Data + 1);
            Console.WriteLine(DateTime.UtcNow + " - W6: " + Data);
        }
    }
}
