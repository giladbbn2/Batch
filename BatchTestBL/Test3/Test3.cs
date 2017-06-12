using BatchFoundation.Worker;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BatchTestBL.Test3
{
    // will work either with SerializableAttribute or with by extending MarshalByRefObject
    // class Person is init'd from the outside, and passed to the workers

    public class Person : MarshalByRefObject
    {
        public int x
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }


        public Person()
        {
            x = 0;
        }

        public void addOne()
        {
            x++;
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
            d.Name += "-1-";

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
            
            // these statements still have an effect on data
            d.addOne();
            d.Name += "-2-";

            // this will trigger an error
            // everything before the following statement is committed to data
            d.addOne2();

            // NEVER GETS HERE

            d.Name += "-3-";

            Console.WriteLine(DateTime.UtcNow + " - W2");
        }
    }

    public class MyWorker3 : Worker
    {
        public override void Run(BlockingCollection<object> Input, BlockingCollection<object> Output, ref object Data, bool IsTest)
        {

            dynamic d = Data;

            d.Name += "-4-";

            d.x++;
            d.addOne();
            d.addOne();
            d.addOne();
            d.addOne();
            d.addOne();
            d.addOne();
            d.addOne();
            d.addOne();
            d.addOne();


            Console.WriteLine(DateTime.UtcNow + " - W3");
        }
    }
}
