SimpleHelpers.ObjectDiffPatch
===========

[![NuGet](https://img.shields.io/nuget/v/SimpleHelpers.ObjectDiffPatch.svg?maxAge=1200&style=flat-square)](https://www.nuget.org/packages/SimpleHelpers.ObjectDiffPatch/)
[![GitHub license](https://img.shields.io/badge/license-MIT-brightgreen.svg?maxAge=3600&style=flat-square)](https://raw.githubusercontent.com/khalidsalomao/SimpleHelpers.Net/master/SimpleHelpers/LICENSE.txt)

Simple Object Comparer that generates a Diff between objects and is able to Patch one object to transforms into the other.

ObjectDiffPatch uses *Newtonsoft.Json* internally to create a `JObject` that we use to run a simple and reliable deep/recursive object comparison.


Features
--------

* Deep/recursive comparison
* Reliable
* Diff
* Patch

Installation
------------

### NuGet Package Details

You can install using NuGet, see [SimpleHelpers.ObjectDiffPatch at NuGet.org](https://www.nuget.org/packages/SimpleHelpers.ObjectDiffPatch/)

```powershell
PM> Install-Package SimpleHelpers.ObjectDiffPatch
```

The nuget package contains **C# source code**.

The source code will be installed in your project with the following file system structure:

```
|-- <project root>
    |-- SimpleHelpers
        |-- ObjectDiffPatch.cs
```

### Download

If you prefer, you can also download the source code: [ObjectDiffPatch.cs](https://raw.githubusercontent.com/khalidsalomao/SimpleHelpers.Net/master/SimpleHelpers/ObjectDiffPatch.cs)


API
--------

### GenerateDiff

Compares two objects and generates the differences between them returning an object listing all changes.

Detected changes are expressed as two `JObject`, old and new values.


```csharp
var diff = ObjectDiffPatch.GenerateDiff (originalObj, updateObj);

// original properties values
Console.WriteLine (diff.OldValues.ToString());

// updated properties values
Console.WriteLine (diff.OldValues.ToString());
```


Project Information
-------------------

* [Contribute](../#contribute)
* [Support](../#support)
* [License](../#license)
