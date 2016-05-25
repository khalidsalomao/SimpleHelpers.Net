SimpleHelpers.MemoryCache
===========

[![NuGet](https://img.shields.io/nuget/v/SimpleHelpers.MemoryCache.svg?maxAge=1200&style=flat-square)](https://www.nuget.org/packages/SimpleHelpers.MemoryCache/)
[![GitHub license](https://img.shields.io/badge/license-MIT-brightgreen.svg?maxAge=3600&style=flat-square)](https://raw.githubusercontent.com/khalidsalomao/SimpleHelpers.Net/master/SimpleHelpers/LICENSE.txt)

Simple, lightweight and fast in-memory caching.

MemoryCache implements a fast in-memory, e.g. in-process, caching for data that are expensive to create and are thread-safe.  manner.

All items are stored in a concurrent data structure (`System.Collections.Concurrent.ConcurrentDictionary`) to allow fast and safe multi-threaded usage through the MemoryCache static methods, with a lightweight background timer (`System.Threading.Timer`) to remove expired items.


Features
--------

* Simple to use
* Fast
* Lightweight
* Flexible


Installation
------------

### NuGet Package Details

You can install using NuGet, see [SimpleHelpers.MemoryCache at NuGet.org](https://www.nuget.org/packages/SimpleHelpers.MemoryCache/)

```powershell
PM> Install-Package SimpleHelpers.MemoryCache
```

The nuget package contains **C# source code** and depends on `System.Collections.Concurrent` introduced in .Net 4.0.

The source code will be installed in your project with the following file system structure:

```
|-- <project root>
    |-- SimpleHelpers
        |-- MemoryCache.cs
```

### Download

If you prefer, you can also download the source code: [MemoryCache.cs](https://github.com/khalidsalomao/SimpleHelpers.Net/blob/master/SimpleHelpers/MemoryCache.cs)


## Implementation

MemoryCache is designed as a static class, so all everything you need is either static properties or methods.

### Generic Class

MemoryCache is a generic class and so two different types cannot access each other cache.
Since the JIT compiler will generate different code at run time, for example `MemoryCache<string>` is a different class from `MemoryCache<StringBuilder>` or `MemoryCache<byte[]>`.
This is important to remember whenever working with inheritance, since inherited types won't be able to see its parent MemoryCache.

```csharp
    MemoryCache<parent> != MemoryCache<child>
```

### Thread-safe

MemoryCache uses `System.Collections.Concurrent.ConcurrentDictionary` and every property and method are thread-safe.


!!! note

    Since we are dealing with **caching**, it is recommended that stored objects be thread-safe, since the same instance of an object can and will be returned by multiple calls of `Get` methods.

    You still can use non-thread-safe objects, see [Remove Example](#remove) or [FAQ](#faq).


### Lightweight

MemoryCache uses a `System.Threading.Timer` to periodically check for expired items and remove them.
The timer step can be configured, see [Configuration](#configuration).

The timer has a lazy start. Only after the first `Set` call to store an item, the timer will start.
If the cache internal dictionary is empty for a while - after 3 runs - the timer is released and only to start again on next `Set` call.


Configuration
-------------

Simple configuration of the MemoryCache settings.

It should be done for each type, as explained in [Implentation](#generic_class).

```csharp

// setup (called once in application initialization)

// set a duration of an item in the cache to 1 second
MemoryCache<string>.Expiration = TimeSpan.FromSeconds (1);
// set the internal maintenance timed task to run each 500 ms removing expired items
MemoryCache<string>.MaintenanceStep = TimeSpan.FromMilliseconds (500);
// Our event, if we want to treat the removed expired items
MemoryCache<string>.OnExpiration += (string key, string item) =>
{
    // do something
};

```


API
---

### Set

Use `Set` method to store data with a key.

```csharp
// store an item
MemoryCache<string>.Set ("k1", "test");
```

### Get

Use `Get` method to retrieve data associated with a key.

```csharp
// try to get from cache
string value = MemoryCache<string>.Get ("k1");
// check if we got it
if (value != null)
{
    // ok we got it!
}
```

### Remove

Use `Remove` method to atomically remove and return the value associated with the specified key.

```csharp
// try to get from cache
string value = MemoryCache<string>.Remove ("k1");
// check if we got it
if (value != null)
{
    // ok we got it!
}
```

### GetOrAdd

Use `GetOrAdd` to try to retrieve data from cache and if the data is not present in cache use a factory method to create the new item.

```csharp
// prepare our factory
var factory = (string key) =>
{
    return key + DateTime.Now.ToString ();
};

// try to get the associated value if the cache does not have it
// then use the factory to create a new one, store and return it.
string value = MemoryCache<string>.GetOrAdd ("key2", factory);
```

### GetOrSyncAdd

Using a more complex concurrent scenario where, if the item is not found, we want to call the factory **only once**.
`GetOrSyncAdd` guarantees that the factory method will be called only once in case of concurrent invocations.
So all concurrent calls will wait until factory has created and stored the new item, and will return it.

```csharp
// prepare our factory
var factory = (string key) =>
{
    return key + DateTime.Now.ToString ();
};

// try to get the associated value if the cache does not have it
// then use the factory to create a new one, store and return it.
// It the factory takes more than 250 milliseconds to create then new instance,
// it will exit returning the default value for the class
string value = MemoryCache<string>.GetOrSyncAdd ("key2", factory, TimeSpan.FromMilliseconds (250));

// check if we got it
if (value != null)
{
    // ok we got it!
}
```

FAQ
---

### How to deal with *non-thread-safe* objects
If you wish to use non-thread safe object instances, you can! Instead of using the `Get` methods, you **must** use the `Remove` method to atomically (safelly) get and remove the object instance from the cache.

Project Information
-------------------

* [Contribute](../#contribute)
* [Support](../#support)
* [License](../#license)
