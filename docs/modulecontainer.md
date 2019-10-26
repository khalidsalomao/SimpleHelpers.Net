SimpleHelpers.ModuleContainer
===========

[![NuGet](https://img.shields.io/nuget/v/SimpleHelpers.ModuleContainer.svg?maxAge=1200&style=flat-square)](https://www.nuget.org/packages/SimpleHelpers.ModuleContainer/)
[![GitHub license](https://img.shields.io/badge/license-MIT-brightgreen.svg?maxAge=3600&style=flat-square)](https://cdn.jsdelivr.net/gh/khalidsalomao/SimpleHelpers.Net/SimpleHelpers/LICENSE.txt)

Easy, fast and non-invasive dependency injection and plugin system.

ModuleContainer is for those that don't need or want a fully featured DI or IOC framework, but wants to:

* create an object
* create an object that implements an interface
* create an object from its name (string) at runtime
* be able to load an external assembly (like a plugin) from the file system


Features
--------

* Small footprint
* Fast initialization
* No-dependencies
* Load assemblies at runtime (plugins)
* Fast instance creation
* Easy (almost no learning curve)


Installation
------------

### NuGet Package Details

You can install using NuGet, see [SimpleHelpers.ModuleContainer at NuGet.org](https://www.nuget.org/packages/SimpleHelpers.ModuleContainer/)

```powershell
PM> Install-Package SimpleHelpers.ModuleContainer
```

The nuget package contains **C# source code**.

The source code will be installed in your project with the following file system structure:

```
|-- <project root>
    |-- SimpleHelpers
        |-- ModuleContainer.cs
```

### Download

If you prefer, you can also download the source code: [ModuleContainer.cs](https://cdn.jsdelivr.net/gh/khalidsalomao/SimpleHelpers.Net/SimpleHelpers.ModuleContainer/ModuleContainer.cs)


API
--------

### GetInstance

Create an object instance by its type name. The full type name (namespace + class) is preferred but not required.

```csharp
object instance = ModuleContainer.Instance.GetInstance ("SimpleHelpers.SampleClass");

// here you have your instance...
((SampleClass)instance).SampleMethod ();
```

If you have the type at compile time, you can also pass `Type` as parameter.

```csharp
object instance = ModuleContainer.Instance.GetInstance (typeof(SampleClass));

// here you have your instance...
((SampleClass)instance).SampleMethod ();
```


### GetInstanceOf

Search for a class that implements the provided type (that can be a base class or an interface) and create an instance.

Note that if there is multiple types that implements the provided type, the first one will be returned...

```csharp
ISampleClass instance = ModuleContainer.Instance.GetInstanceOf<ISampleClass> ();

// here you have your instance...
instance.SampleMethod ();
```


### GetInstancesOf

Enumerates created instances of each found type that implements the provided type (that can be a base class or an interface).

```csharp
var list = ModuleContainer.Instance.GetInstancesOf<ISampleClass> ();

// here you have your instance...
foreach (var instance in list)
    instance.SampleMethod ();
```


### GetTypesOf

Enumerates all found types that implements the provided type (that can be a base class or an interface).

```csharp
var list = ModuleContainer.Instance.GetTypesOf (typeof(ISampleClass));

// here you have your instance...
foreach (Type type in list)
    // ...
```


### GetConstructor

Get a type constructor. The delegate factory will be generated only once by lambda expressions and cached for future requests.

Note that only the parameterless constructor will be used.

```csharp
var ctor = ModuleContainer.Instance.GetConstructor ("SimpleHelpers.SampleClass");

// here you we can use it....
for (var i; i < 1000; i++)
    ctor ();
```


### LoadModules

Will load all valid .net assemblies found in the modules folder and subfolders and scan for the types derived from list of interfaces.

The list of interfaces is optional, since a later call of `GetInstanceOf` will search for all derived types to build the internal cache for this type.

**Important observations:**

* a loaded assembly cannot be unloaded
* all assemblies are loaded in the main appdomain
* the derived types of a given type are searched only once, unless when `LoadModules` are used with a list of interfaces.

```csharp
ModuleContainer.Instance.LoadModules ("my/path/plugins");
```


### RegisterInterface

Registers a interface by searching all derived types.

This method will rebuild the internal cache for a given type.

```csharp
ModuleContainer.Instance.RegisterInterface (typeof(SampleClass));
```


Restrictions
------------

Only types with a parameterless constructor can be created by `GetInstance`, `GetInstanceOf` or `GetInstancesOf`.
Multiple constructors are allowed, but the parameterless constructor will be used!


FAQ
---

### What about the performance of instance creation

The instance creation is optimized with usage of `LambdaExpression` and caching. So it is fast!

If you are creating lots of instances in a loop, for example, you can get an *extra performance* by using `GetConstructor` method ([see GetConstructor](#GetConstructor))


### How do I unload a module

ModuleContainer does not implement appdomain management funcionality and all modules are loaded in the current appdomain.
If you wish to be able load and unload assemblies, than you must implement your own appdomain management logic...


Project Information
-------------------

* [Contribute](../#contribute)
* [Support](../#support)
* [License](../#license)
