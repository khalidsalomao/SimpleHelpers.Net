SimpleHelpers.SQLiteStorage
===========

[![NuGet](https://img.shields.io/nuget/v/SimpleHelpers.SQLiteStorage.svg?maxAge=1200&style=flat-square)](https://www.nuget.org/packages/SimpleHelpers.SQLiteStorage/)
[![GitHub license](https://img.shields.io/badge/license-MIT-brightgreen.svg?maxAge=3600&style=flat-square)](https://raw.githubusercontent.com/khalidsalomao/SimpleHelpers.Net/master/SimpleHelpers/LICENSE.txt)

Simple key value storage using sqlite.

All member methods are thread-safe, so a instance can be safely be accessed by multiple threads.

All stored items are serialized to json by json.net.

Note: this nuget package contains csharp source code and depends on .Net 4.0.


Installation
------------

### NuGet Package Details

You can install using NuGet, see [SimpleHelpers.SQLiteStorage at NuGet.org](https://www.nuget.org/packages/SimpleHelpers.SQLiteStorage/)

```powershell
PM> Install-Package SimpleHelpers.SQLiteStorage
```

The nuget package contains **C# source code**.

The source code will be installed in your project with the following file system structure:

```
|-- <project root>
    |-- SimpleHelpers
        |-- SQLiteStorage.cs
```

### Download

If you prefer, you can also download the source code: [SQLiteStorage.cs](https://raw.githubusercontent.com/khalidsalomao/SimpleHelpers.Net/master/SimpleHelpers/SQLiteStorage.cs)


Configuration
-------------

```csharp
// setup:
SQLiteStorage<My_Class> db = new SQLiteStorage<My_Class> ("path_to_my_file.sqlite",
							  SQLiteStorageOptions.UniqueKeys ());
```

Example
-------

```csharp
// create a new instance
SQLiteStorage<My_Class> db = new SQLiteStorage<My_Class> ("path_to_my_file.sqlite",
							  SQLiteStorageOptions.UniqueKeys ());

// save an item with a key associated
db.Set ("my_key_for_this_item", new My_Class ());

// get it back
var my_obj = db.Get ("my_key_for_this_item").FirstOrDefault ();

// to save any changes, just call set again
db.Set ("my_key_for_this_item", my_obj);

// get all stored items
foreach (var item in db.Get ())
{
	// ...
}
```


Project Information
-------------------

* [Contribute](../#contribute)
* [Support](../#support)
* [License](../#license)
