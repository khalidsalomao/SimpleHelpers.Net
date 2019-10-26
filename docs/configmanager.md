SimpleHelpers.ConfigManager
===========

[![NuGet](https://img.shields.io/nuget/v/SimpleHelpers.ConfigManager.svg?maxAge=1200&style=flat-square)](https://www.nuget.org/packages/SimpleHelpers.ConfigManager/)
[![GitHub license](https://img.shields.io/badge/license-MIT-brightgreen.svg?maxAge=3600&style=flat-square)](https://cdn.jsdelivr.net/gh/khalidsalomao/SimpleHelpers.Net/SimpleHelpers/LICENSE.txt)

Simple configuration manager to get and set the values in the AppSettings section of the default configuration file (C# - Source file).

Note: this nuget package contains csharp source code and depends on Generics introduced in .Net 2.0.


Installation
------------

### NuGet Package Details

You can install using NuGet, see [SimpleHelpers.ConfigManager at NuGet.org](https://www.nuget.org/packages/SimpleHelpers.ConfigManager/)

```powershell
PM> Install-Package SimpleHelpers.ConfigManager
```

The nuget package contains **C# source code**.

The source code will be installed in your project with the following file system structure:

```
|-- <project root>
    |-- SimpleHelpers
        |-- ConfigManager.cs
```

### Download

If you prefer, you can also download the source code: [ConfigManager.cs](https://cdn.jsdelivr.net/gh/khalidsalomao/SimpleHelpers.Net/SimpleHelpers/ConfigManager.cs)


Configuration
-------------

```csharp
// setup (called once in application initialization)

// set to add any new keys added during the application execution
ConfigManager.AddNonExistingKeys = true;
```

Example
-------

```csharp
string address = ConfigManager.Get ("MongoDBaddress", "localhost");
int port = ConfigManager.Get ("MongoDBport", 21766);
```


Project Information
-------------------

* [Contribute](../#contribute)
* [Support](../#support)
* [License](../#license)
