using BatchFoundation.Worker;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BatchTest.Test2
{
    public class test
    {
        public int run(int x)
        {
            return x + 5;
        }
    }

    public class MyWorker1 : Worker
    {
        public override void Run(BlockingCollection<object> Input, BlockingCollection<object> Output, ref object data)
        {
            Console.WriteLine(DateTime.UtcNow + " - W1: " + data);
        }
    }

    public class MyWorker2 : Worker
    {
        public override void Run(BlockingCollection<object> Input, BlockingCollection<object> Output, ref object data)
        {
            data = (object)((int)data + 1);
            Console.WriteLine(DateTime.UtcNow + " - W2: " + data);
        }
    }

    public class MyWorker3 : Worker
    {
        public override void Run(BlockingCollection<object> Input, BlockingCollection<object> Output, ref object data)
        {
            data = (object)((int)data + 1);
            Console.WriteLine(DateTime.UtcNow + " - W3: " + data);
        }
    }

    public class MyWorker4 : Worker
    {
        public override void Run(BlockingCollection<object> Input, BlockingCollection<object> Output, ref object data)
        {
            data = (object)((int)data + 1);
            Console.WriteLine(DateTime.UtcNow + " - W4: " + data);
        }
    }

    public class MyWorker5 : Worker
    {
        public override void Run(BlockingCollection<object> Input, BlockingCollection<object> Output, ref object data)
        {
            data = (object)((int)data + 1);
            Console.WriteLine(DateTime.UtcNow + " - W5: " + data);
        }
    }

    public class MyWorker6 : Worker
    {
        public override void Run(BlockingCollection<object> Input, BlockingCollection<object> Output, ref object data)
        {
            Thread.Sleep(3000);
            data = (object)((int)data + 1);
            Console.WriteLine(DateTime.UtcNow + " - W6: " + data);
        }
    }
}
