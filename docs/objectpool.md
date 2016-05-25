SimpleHelpers.ObjectPool
===========

[![NuGet](https://img.shields.io/nuget/v/SimpleHelpers.ObjectPool.svg?maxAge=1200&style=flat-square)](https://www.nuget.org/packages/SimpleHelpers.ObjectPool/)
[![GitHub license](https://img.shields.io/badge/license-MIT-brightgreen.svg?maxAge=3600&style=flat-square)](https://raw.githubusercontent.com/khalidsalomao/SimpleHelpers.Net/master/SimpleHelpers/LICENSE.txt)

A fast lightweight object pool for fast and simple object reuse.

Simple to use, fast, lightweight and thread-safe object pool for objects that are expensive to create or could efficiently be reused.

Note: this nuget package contains c# source code and depends on System.Collections.Concurrent introduced in .Net 4.0.


Features
--------

* Simple to use
* Fast
* Lightweight
* Thread-safe


Installation
------------

### NuGet Package Details

You can install using NuGet, see [SimpleHelpers.ObjectPool at NuGet.org](https://www.nuget.org/packages/SimpleHelpers.ObjectPool/)

```powershell
PM> Install-Package SimpleHelpers.ObjectPool
```

The nuget package contains **C# source code**.

The source code will be installed in your project with the following file system structure:

```
|-- <project root>
    |-- SimpleHelpers
        |-- ObjectPool.cs
```

### Download

If you prefer, you can also download the source code: [ObjectPool.cs](https://raw.githubusercontent.com/khalidsalomao/SimpleHelpers.Net/master/SimpleHelpers/ObjectPool.cs)


Examples
--------

```csharp
// Get or create a new random generator
Random rnd = ObjectPool<Random>.Get (CreateRandomGenerator);
// start generating random numbers
try
{
	// ...

	var num = rnd.Next (max);

	// ...
}
finally
{
	// Release the random generator by putting it back in the object pool
	ObjectPool<Random>.Put (rnd);
}

// our factory
private static Random CreateRandomGenerator ()
{
	return new Random ((int)DateTime.UtcNow.Ticks);
}
```


Project Information
-------------------

* [Contribute](../#contribute)
* [Support](../#support)
* [License](../#license)
