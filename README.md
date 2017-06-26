# Batch

__A framework written in C# for distributed big data processing__

## Overview

Batch is a _Layer Oriented Programming_ system that enables the division of the business logic into externally
controlled modules which can be loaded and unloaded at runtime. The logical units are external dlls that are each 
loaded into a separate logical process and can be chained or either run in a long running blocking thread.

In Batch terms each dll logical unit is called a __Foreman__ and it may run in two ways:
1. Short Running Foreman - This logical unit is intended to be executed over and over again.
In this mode multiple Foremen may be chained, so the output of the a Foreman becomes the input for the Foreman 
connected downstream. Besides the regular downstream connected Foreman there's an option to connect another one
for testing while controlling the percent of actual traffic reaching the test Foreman. E.g.: frmn2 is connected 
downstream to frmn1. frmn3 can also be connected to frmn1 in a restricted testing mode with a routing weight 
equal to 30% (meaning that 30% of the time output from frmn1 will be delivered first to frmn3 and then also to
frmn2 like it supposed to).
2. Long Running Foreman - This logical unit is intended to be executed only once. The logic inside it is supposed
to be blocking until an external signal arrives. The signal may introduce fresh data to be processed in the long 
running thread or may signal the thread to end gracefully. Either way, it is up to the developer to decide how
the logic should block (i.e. use AutoResetEvents, Wait and Pulse, BlockingCollections). There's an out of the box
option to use producer/consumer queues (which are implemented internally as BlockingCollections) to simulate a
pipeline.

Besides allowing the developer the flexibility of adding/removing complete logical units within the stack while 
remaining in runtime the system can also be used extensively by devops/testing teams/integrators for:
* Testing new versions of logical modules inside the production environment
* Switching between old versions and new versions of logical modules without using elaborate switching
mechanisms to route incoming server traffic to a different server side software
* Simplifying Continuous Integration methods
* Enables chaining different analytical (logical) building blocks together by a non-developer to be used for
routine analytics use
* Enables distributing different logical building blocks between several computers in a network via an internally
implemented messaging queue system (Distributed Batch is coming soon!)

## Philosophy

Batch envisions the way an enterprise server side system works as an assembly line. Imagine a conveyor belt
running through several rooms in a factory. Each room occupies a different assembly worker that is responsible
for doing something very specific. Every time a box reaches a certain room the room's worker modifies it according 
to his instructions and puts the modified box on the rotating belt again to be conveyed to the next room in line.
You may even have some branching points along its way to route different boxes to different rooms and another
branching point connecting the output of different rooms into a single pathway leading to more rooms downstream.
The major advantage here is that there's (supposed to be) complete modularity between every room. A worker in some
room doesn't have to know where the box is coming from or where it is going to. Moreover, the factory manager
may decide to stop routing boxes to a certain room and bypass it to another room in the factory which was set up
earlier with more advanced machinery or better trained workers.

In Batch terms every room in the factory is called a __Foreman__, every worker inside the room is also called a 
__Worker__ and the factory manager is called a __Contractor__. Implied here is the fact that a Foreman, which is
a standalone dll, may hold several Workers that reside within the same assembly. A short running Foreman may be 
responsible for some OLTP business logic operations and a long running Foreman may be responsible for some OLAP 
operations, but this division is not completely accurate as a pipeline consisting of some long running threads
connected via a complex pathway of consumer/producer queues may be used more efficiently (in some cases) for 
chaining logic modules for some OLTP operations. In a different scenario, a company may build in advance some
logic modules (Foremen) for heavy calculations/big data retrieval for general use and let a non-developer
the ability to connect between them on demand, e.g.: a company analyst may pass the fiscal year as 4 digits to
frmn1 which then outputs a matrix holding financial calculations for each month of that year. frmn1 is a 
short running Foreman and connected to another short running Foreman, frmn2, which is responsible to compare
its input monthly financial calculations to history data stored on some db and outputs the resulting trends and
conclusions for that year. The analyst may choose to connect another Foreman that knows how to do aggregations
and/or filtering according to some criteria. These analytics Foremen are ready to be reused by a different 
analyst that wants to slice the data differently and choose a different combination of preexisting Foremen to 
do so.

## File Tree

* BatchFoundation directory - contains definition to the Worker class. This lib must be referenced in order to 
extend the Worker class operating inside a Foreman and to override its Run() method. If your solution doesn't
require using the BatchAgent (controlling Batch on a remote computer) then this lib would be the only one 
referenced in your Foreman DLL lib.
* Batch directory - contains all code related to the Batch server, including the Contractor and Foreman definitions.
This class lib may be referenced in your project if you plan to use the Contractor directly and let your project
the capability of loading/unloading external DLL's as Foremen.
* BatchAgent directory - contains a WCF application for controlling a remote Batch server. Includes definition
for RemoteContractor.
* BachTest directory - a console app with various testing units. Some units test long running Foremen or short
running Foremen and another one tests communication with BatchAgent by using JSON inside Run()'s Data parameter.
* BatchTestBL directory - contains some business logic to be loaded as Foremen by BatchTest. Inner test directories
are numbered according to BatchTest unit tests.

## A Simple Example

__frmn1.dll (Class lib):__

~~~~

namespace Foreman1
{
    public class MyWorker : Worker
    {
        public override void Run(BlockingCollection<object> Input, BlockingCollection<object> Output, ref object Data, bool IsTest)
        {
            Data = (object)((int)Data + 5);
            Console.WriteLine(DateTime.UtcNow + " - W1: " + Data);
			
			/*
			
			Data will be passed to downstream Foremen
			
			*/
        }
    }	
}

~~~~

__Console App:__

~~~~

namespace ConsoleApp1
{
	class Program
	{
		static void Main(string[] args)
		{
			var c = new Contractor();

			string configString = File.ReadAllText("C:\\\\batchtest\\\\frmn1.config");

			// create two Foremen with the same BL

			c.AddForeman("frmn1", configString);

			c.AddForeman("frmn2", configString);

			/*

			frmn1.config:

			{
				"foremanVer": "0.1",
				"assemblyPath": "C:\\\\batchtest\\\\frmn1.dll",
				"nodes": [{
					"name": "n1",
					"exeOrderId": 1,
					"className": "Foreman1.MyWorker"    
				}],
				"queues": [],
				"connections": []
			}

			there's only one Worker (Foreman1.MyWorker). This is a simple short running Foreman.

			*/

			// the output of frmn1 shall be the input for frmn2

			c.ConnectForeman("frmn1", "frmn2");

			int x = 5;

			object o = (object)x;

			// a simple object in this case is not serializable so we won't get anything back (one-way)

			// this will automatically run frmn2 immediately after frmn1 ends.

			c.Run("frmn1", o);		
		}
	}
}
~~~~

## License

Batch is licensed under MIT license. For full license see the LICENSE file.




