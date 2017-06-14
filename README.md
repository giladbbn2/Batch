# Batch

__A framework written in C# for big data processing__

## General

Batch is a _Layer Oriented Programming_ system that enables the division of the business logic into externally
controlled modules which can be loaded and unloaded in runtime. The logical units are external dlls that are each 
loaded into a separate logical process and can be chained or either run in a long running blocking thread.

In Batch terms each dll logical unit is called a __Foreman__ and it may run in two ways:
1. Short Running Foreman - This logical unit is intended to be executed over and over again.
In this mode multiple Foremen may be chained, so the output of the a Foreman becomes the input for the Foreman 
connected downstream. Besides the regular downstream connected Foreman there's an option to connect another one
for testing while controlling the percent of actual traffic reacing the test Foreman. E.g.: frmn2 is connected 
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

## Philosophy ##

The wet dream of every development manager is finding a way to divide the work between the developers just like
in a factory assembly line without letting all hell break loose. Most of the time each developer is assigned a 
project (or responsibility for a large portion of it)...


## Anatomy of a Foreman ##

## What's Next ##

Distributed Batch




