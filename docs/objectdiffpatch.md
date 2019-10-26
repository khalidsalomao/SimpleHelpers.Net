SimpleHelpers.ObjectDiffPatch
===========

[![NuGet](https://img.shields.io/nuget/v/SimpleHelpers.ObjectDiffPatch.svg?maxAge=1200&style=flat-square)](https://www.nuget.org/packages/SimpleHelpers.ObjectDiffPatch/)
[![GitHub license](https://img.shields.io/badge/license-MIT-brightgreen.svg?maxAge=3600&style=flat-square)](https://cdn.jsdelivr.net/gh/khalidsalomao/SimpleHelpers.Net/SimpleHelpers/LICENSE.txt)

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

If you prefer, you can also download the source code: [ObjectDiffPatch.cs](https://cdn.jsdelivr.net/gh/khalidsalomao/SimpleHelpers.Net/SimpleHelpers/ObjectDiffPatch.cs)


### Dependencies

- [Newtonsoft.Json](https://www.nuget.org/packages/Newtonsoft.Json/)

Tested with versions 6 and later.


API
--------

### GenerateDiff

Compares two objects and generates the differences between them returning an object listing all changes.

Detected changes are expressed as two `Newtonsoft.Json.Linq.JObject`, old and new values.


```csharp
var diff = ObjectDiffPatch.GenerateDiff (originalObj, updatedObj);

// original properties values
Console.WriteLine (diff.OldValues.ToString());

// updated properties values
Console.WriteLine (diff.NewValues.ToString());
```


### Snapshot

Creates an object snapshots as a `Newtonsoft.Json.Linq.JObject`.

```csharp
var snapshot = ObjectDiffPatch.Snapshot (obj);

// do something....
do_something (obj);

// log changes
var diff = ObjectDiffPatch.GenerateDiff (snapshot, obj);
if (!diff.AreEqual)
{
    Console.WriteLine (diff.NewValues.ToString());
}
```


### PatchObject

Modifies an object according to a diff.

```csharp
var diff = ObjectDiffPatch.GenerateDiff (originalObj, updatedObj);

// recreate originalObj from updatedObj
var patched = ObjectDiffPatch.PatchObject (updatedObj, diff.OldValues);
```


### DefaultSerializerSettings

Gets or sets the default newtonsoft json serializer settings.

```csharp
// enable circular reference handling
ObjectDiffPatch.DefaultSerializerSettings.PreserveReferencesHandling =
    Newtonsoft.Json.PreserveReferencesHandling.All;
```

----

FAQ
---

### How to enable circular references?

The default serializer settings are exposed in `ObjectDiffPatch.DefaultSerializerSettings`.
You can change the default settings to enable circular references.

The resulting json will have additional fields `$id/$ref` to mark the object references.
For more details of the inner workings see http://www.newtonsoft.com/json/help/html/PreserveReferencesHandlingObject.htm

```csharp
// enable circular reference handling
ObjectDiffPatch.DefaultSerializerSettings.PreserveReferencesHandling =
    Newtonsoft.Json.PreserveReferencesHandling.All;
```


----

Project Information
-------------------

* [Contribute](../#contribute)
* [Support](../#support)
* [License](../#license)
