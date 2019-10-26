SimpleHelpers.TimedQueue
===========

[![NuGet](https://img.shields.io/nuget/v/SimpleHelpers.TimedQueue.svg?maxAge=1200&style=flat-square)](https://www.nuget.org/packages/SimpleHelpers.TimedQueue/)
[![GitHub license](https://img.shields.io/badge/license-MIT-brightgreen.svg?maxAge=3600&style=flat-square)](https://cdn.jsdelivr.net/gh/khalidsalomao/SimpleHelpers.Net/SimpleHelpers/LICENSE.txt)

Fast lightweight in-memory queue that stores data in a concurrent queue and periodically process the queued items.

Useful for:

* processing items in batches;
* grouping data for later processing;
* async processing (consumer/producer);
* etc.

Note: this nuget package contains C# source code and depends on System.Collections.Concurrent introduced in .Net 4.0.


Installation
------------

### NuGet Package Details

You can install using NuGet, see [SimpleHelpers.TimedQueue at NuGet.org](https://www.nuget.org/packages/SimpleHelpers.TimedQueue/)

```powershell
PM> Install-Package SimpleHelpers.TimedQueue
```

The nuget package contains **C# source code**.

The source code will be installed in your project with the following file system structure:

```
|-- <project root>
    |-- SimpleHelpers
        |-- TimedQueue.cs
```

### Download

If you prefer, you can also download the source code: [TimedQueue.cs](https://cdn.jsdelivr.net/gh/khalidsalomao/SimpleHelpers.Net/SimpleHelpers/TimedQueue.cs)


Configuration
-------------

Simple configuration of the TimedQueue settings.


```csharp
// create our timedQueue instance
SimpleHelpers.TimedQueue<LogEvent> my_queue = new SimpleHelpers.TimedQueue<LogEvent> ();

// set the queue timed task to run each 500 ms executing the registered action
my_queue.TimerStep = TimeSpan.FromMilliseconds (500);

// Our event, if we want to treat the removed expired items
my_queue.OnExecution = (IEnumerable<Our_Object> items) =>
{
	// the items IEnumerable must be consumed to clear the queued items!
	foreach (var evt in items)
		// do something
};
```

Another example passing the parameters to the constructor.

```csharp
// create our timedQueue instance
SimpleHelpers.TimedQueue<Our_Object> my_queue = new SimpleHelpers.TimedQueue<Our_Object> (TimeSpan.FromMilliseconds (2500), ConsumerMethod);
```


!!! note

	Remember that since TimedQueue is executed asynchronously - in a timer thread - you should hold the reference to TimedQueue instance to avoid the instance destruction before the `OnExecution` method is invoked. The .Net garbage collector will collect all instances without that it thinks are not being used, and that may includes local method variables that are optimized by the compiler depending on how you coded it...


Examples
--------

### Put

Enqueue items to be processed by the `OnExecution` action.

```csharp
	my_queue.Put (evt);
```

More complex example:

```csharp

// our method that does an network call to store out object
// and keeps retrying in case of failure
public static void SaveEvent (Our_Object evt)
{
	// try to save
	try
	{
		SaveOverTheInternet (evt);
	}
	catch (System.IO.IOException ex)
	{
		// log.Error (ex);
		SimpleHelpers.TimedQueue<Our_Object>.Put (evt);
	}
}

```


Project Information
-------------------

* [Contribute](../#contribute)
* [Support](../#support)
* [License](../#license)
