SimpleHelpers.FileEncoding
===========

[![NuGet](https://img.shields.io/nuget/v/SimpleHelpers.FileEncoding.svg?maxAge=1200&style=flat-square)](https://www.nuget.org/packages/SimpleHelpers.FileEncoding/)
[![GitHub license](https://img.shields.io/badge/license-MIT-brightgreen.svg?maxAge=3600&style=flat-square)](https://cdn.jsdelivr.net/gh/khalidsalomao/SimpleHelpers.Net/SimpleHelpers/LICENSE.txt)

Detect any text file charset encoding using Mozilla Charset Detector (UDE.CSharp).

FileEncoding support almost all charset encodings (utf-8, utf-7, utf-32, ISO-8859-1, ...). It checks if the file has a [BOM header](https://en.wikipedia.org/wiki/Byte_order_mark), and if not FileEncoding will load and analize the file bytes and try to decide its charset encoding.


Features
--------

* Byte order mark (BOM) detection
* Analyse file content
* Comprehensive charset encodings detection
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

If you prefer, you can also download the source code: [FileEncoding.cs](https://github.com/khalidsalomao/SimpleHelpers.Net/blob/master/SimpleHelpers.ConsoleUtils/SimpleHelpers/FileEncoding.cs)


### Dependencies

- [UDE.CSharp](https://www.nuget.org/packages/UDE.CSharp/)

> Compiled version of "C# port of Mozilla Universal Charset Detector"

This userful library can detect the charset encoding by analysing a byte array.


API
-------

### DetectFileEncoding

Tries to detect the file encoding by checking byte order mark (BOM) existence and then loading a part of the file and tries to detect the charset using [UDE.CSharp](https://github.com/errepi/ude#readme)

```csharp
    var encoding = FileEncoding.DetectFileEncoding ("./my_text_file.txt");
```

### TryLoadFile

Tries to load file content with the correct encoding.
This is a shortcut that uses `System.IO.File.ReadAllText` to load the file content, but first it detects the correct encoding.

If the file doesn't exist or it couldn't be loaded, the provided `defaultValue` (second parameter) will be returned.

```csharp
    var content = FileEncoding.TryLoadFile ("./my_text_file.txt", "");
```


### Detect

Detects the encoding of textual data of the specified input data

```
var det = new FileEncoding ();
using (var stream = new System.IO.FileStream (inputFilename, System.IO.FileMode.Open))
{
    det.Detect (inputStream);
}

// Finalize detection phase and gets detected encoding name
var encoding = det.Complete ();

// check results
Console.WriteLine ("IsText = {0}", det.IsText);
Console.WriteLine ("HasByteOrderMark = {0}", det.HasByteOrderMark);
Console.WriteLine ("EncodingName = {0}", det.EncodingName);
```

Project Information
-------------------

* [Contribute](../#contribute)
* [Support](../#support)
* [License](../#license)
