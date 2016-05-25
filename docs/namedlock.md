SimpleHelpers.NamedLock
===========

[![NuGet](https://img.shields.io/nuget/v/SimpleHelpers.NamedLock.svg?maxAge=1200&style=flat-square)](https://www.nuget.org/packages/SimpleHelpers.NamedLock/)
[![GitHub license](https://img.shields.io/badge/license-MIT-brightgreen.svg?maxAge=3600&style=flat-square)](https://raw.githubusercontent.com/khalidsalomao/SimpleHelpers.Net/master/SimpleHelpers/LICENSE.txt)

Synchronization helper: a static lock collection associated with a key.

NamedLock manages the lifetime of critical sections that can be accessed by a key (name) throughout the application. It also have some helper methods to allow a maximum wait time (timeout) to acquire the lock and safely release it.


Installation
------------

### NuGet Package Details

You can install using NuGet, see [SimpleHelpers.NamedLock at NuGet.org](https://www.nuget.org/packages/SimpleHelpers.NamedLock/)

```powershell
PM> Install-Package SimpleHelpers.NamedLock
```

The nuget package contains **C# source code**.

The source code will be installed in your project with the following file system structure:

```
|-- <project root>
    |-- SimpleHelpers
        |-- NamedLock.cs
```

### Download

If you prefer, you can also download the source code: [NamedLock.cs](https://raw.githubusercontent.com/khalidsalomao/SimpleHelpers.Net/master/SimpleHelpers/NamedLock.cs)


Examples
--------

Simple usage where we try to acquire the lock for 100 ms.
So if somewhere else in our application this same lock was already acquired, we will wait until we acquire the lock or 100 ms has passed.

```csharp
string key = "our lock name";

using (var padlock = new NamedLock (key))
{
    if (padlock.Enter (TimeSpan.FromMilliseconds (100)))
    {
        // do something as we now own the lock
    }
    else
    {
        // do some other thing since we could not aquire the lock
    }
}
```

Another usage example with a static helper method.

```csharp
string key = "our lock name";

using (var padlock = NamedLock.CreateAndEnter (key, TimeSpan.FromMilliseconds (100)))
{
    if (padlock.IsLocked)
    {
        // do something
    }
}
```


Project Information
-------------------

* [Contribute](../#contribute)
* [Support](../#support)
* [License](../#license)
