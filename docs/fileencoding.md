SimpleHelpers.FileEncoding
===========

[![NuGet](https://img.shields.io/nuget/v/SimpleHelpers.FileEncoding.svg?maxAge=1200&style=flat-square)](https://www.nuget.org/packages/SimpleHelpers.FileEncoding/)
[![GitHub license](https://img.shields.io/badge/license-MIT-brightgreen.svg?maxAge=3600&style=flat-square)](https://raw.githubusercontent.com/khalidsalomao/SimpleHelpers.Net/master/SimpleHelpers/LICENSE.txt)

Detect any text file charset encoding using Mozilla Charset Detector (UDE.CSharp).

FileEncoding support almost all charset encodings (utf-8, utf-7, utf-32, ISO-8859-1, ...). It checks if the file has a [BOM header](https://en.wikipedia.org/wiki/Byte_order_mark), and if not FileEncoding will load and analize the file bytes and try to decide its charset encoding.


Features
--------

* Byte order mark (BOM) detection
* Analyse file content
* Almost all charset encodings
* Large files support


Installation
------------

### NuGet Package Details

You can install using NuGet, see [SimpleHelpers.FileEncoding at NuGet.org](https://www.nuget.org/packages/SimpleHelpers.FileEncoding/)

```powershell
PM> Install-Package SimpleHelpers.FileEncoding
```

The nuget package contains **C# source code**.

The source code will be installed in your project with the following file system structure:

```
|-- <project root>
    |-- SimpleHelpers
        |-- FileEncoding.cs
```

### Download

If you prefer, you can also download the source code: [FileEncoding.cs](https://raw.githubusercontent.com/khalidsalomao/SimpleHelpers.Net/master/NugetPackaging/SimpleFileEncoding/FileEncoding.cs.pp)


### Dependencies

- [UDE.CSharp](https://www.nuget.org/packages/UDE.CSharp/)

> Compiled version of "C# port of Mozilla Universal Charset Detector"

This userful library can detect the charset encoding by analysing a byte array.


Example
-------

### DetectFileEncoding

```csharp
    var encoding = FileEncoding.DetectFileEncoding ("./my_text_file.txt");
```

### TryLoadFile

```csharp
    var content = FileEncoding.TryLoadFile ("./my_text_file.txt");
```


Project Information
-------------------

* [Contribute](../#contribute)
* [Support](../#support)
* [License](../#license)
