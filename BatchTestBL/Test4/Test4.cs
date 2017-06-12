using BatchFoundation.Worker;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BatchTestBL.Test4
{
    public class Person : MarshalByRefObject
    {
        public int x
        {
            get;
            set;
        }

        public void addOne()
        {
            x++;
        }

        public Person()
        {
            x = 0;
        }
    }

    public class MyWorker1 : Worker
    {
        public override void Run(BlockingCollection<object> Input, BlockingCollection<object> Output, ref object Data, bool IsTest)
        {
            //Person p = (Person)data;
            //p.x++;
            //Console.WriteLine(DateTime.UtcNow + " - W1: " + p.x);

            dynamic d = Data;
            d.x++;

            Console.WriteLine(DateTime.UtcNow + " - W1");
        }
    }

    public class MyWorker2 : Worker
    {
        public override void Run(BlockingCollection<object> Input, BlockingCollection<object> Output, ref object Data, bool IsTest)
        {
            //Person p = (Person)data;
            //p.x++;
            //Console.WriteLine(DateTime.UtcNow + " - W2: " + p.x);

            dynamic d = Data;
            d.x++;
            d.addOne();
            d.addOne();


            Console.WriteLine(DateTime.UtcNow + " - W2");
        }
    }
}
