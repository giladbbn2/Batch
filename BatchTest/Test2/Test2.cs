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
            Console.WriteLine("w1+");
            //data = (new Random()).Next(1000);
            Console.WriteLine(data);
            Console.WriteLine("w1-");
        }
    }

    public class MyWorker2 : Worker
    {
        public override void Run(BlockingCollection<object> Input, BlockingCollection<object> Output, ref object data)
        {
            Console.WriteLine("w2+");
            data = (object)((int)data + 1);
            Console.WriteLine(data);
            Console.WriteLine("w2-");
        }
    }

    public class MyWorker3 : Worker
    {
        public override void Run(BlockingCollection<object> Input, BlockingCollection<object> Output, ref object data)
        {
            Console.WriteLine("w3+");
            data = (object)((int)data + 1); Console.WriteLine(data);
            Console.WriteLine("w3-");
        }
    }

    public class MyWorker4 : Worker
    {
        public override void Run(BlockingCollection<object> Input, BlockingCollection<object> Output, ref object data)
        {
            Console.WriteLine("w4+");
            data = (object)((int)data + 1); Console.WriteLine(data);
            Console.WriteLine("w4-");
        }
    }

    public class MyWorker5 : Worker
    {
        public override void Run(BlockingCollection<object> Input, BlockingCollection<object> Output, ref object data)
        {
            Console.WriteLine("w5+");
            data = (object)((int)data + 1); Console.WriteLine(data);
            Console.WriteLine("w5-");
        }
    }

    public class MyWorker6 : Worker
    {
        public override void Run(BlockingCollection<object> Input, BlockingCollection<object> Output, ref object data)
        {
            Console.WriteLine("w6+");
            Console.WriteLine(data);
            Thread.Sleep(3000);
            Console.WriteLine("w6-");
        }
    }
}
