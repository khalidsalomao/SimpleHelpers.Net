SimpleHelpers.Net
=================

[![GitHub license](https://img.shields.io/badge/license-MIT-brightgreen.svg?maxAge=3600&style=flat-square)](https://raw.githubusercontent.com/khalidsalomao/SimpleHelpers.Net/master/SimpleHelpers/LICENSE.txt)


*Micro-libraries* (pieces of utility code) for .Net that are safe and simple to use.

In every project there are lot's of reusable patterns that we find our selves rewriting or just doing some copy & paste, and thus the idea behind SimpleHelpers micro-libraries is to create a small collection of such code but keeping it reliable and easy to use.


Most of SimpleHelpers.Net libraries are distributed by Nuget.org as source code files (c#), since this enable us to use these utility code as micro-libraries without creating and having to deploy a huge number of assemblies (dlls). The idea is to keep things simple!

Distributing source code also make things easier in case of doubt or curiosity. You can just take a look into the full source code!


All micro-libraries are well-tested for both performance and reliability. So feel free to use them!


Micro-libraries
---------------

### FileEncoding

- Detect any text file charset encoding using Mozilla Charset Detector.
- [See documentation](docs/fileencoding.md)

### MemoryCache

- Simple, lightweight and fast in-memory caching.
- [See documentation](docs/memorycache.md)

### NamedLock

- Synchronization helper: a lock associated with a key and with timeout.
- [See documentation](docs/namedlock.md)

### SQLiteStorage

- Simple key & value storage using sqlite with json serialization.
- [See documentation](docs/sqlitestorage.md)

### ObjectDiffPatch

### ConsoleUtils

### ConfigManager

- Simple configuration manager to get and set the values in the AppSettings section of the default configuration file.
- [See documentation](docs/configmanager.md)

### ObjectPool

- A fast lightweight object pool for fast and simple object reuse.
- [See documentation](docs/objectpool.md)

### TimedQueue

- Fast lightweight in-memory queue that stores data in a concurrent queue and periodically process the queued items.
- [See documentation](docs/timedqueue.md)

### ParallelTasks

### FlexibleObject

### ScriptEvaluator

### ModuleContainer

### RabbitWorkQueue

- Use RabbitMQ as a distributed work queue!
- [See documentation](https://github.com/khalidsalomao/SimpleHelpers.Net.RabbitMQ)


How to build docs site
----------------------

- Built with [MkDocs](http://www.mkdocs.org/).
- Theme: [Material](http://squidfunk.github.io/mkdocs-material/getting-started/)

**Build steps**

1. Make sure MkDocs is installed, using python package manager - pip:

    ```
    python --version
    pip install mkdocs pymdown-extensions pygments mkdocs-material --upgrade
    ```

2. Test the docs site:
    [build_docs.test.bat](build_docs.test.bat)

3. Build and deploy on gh-pages:
    [build_docs.deploy.bat](build_docs.deploy.bat)


Contribute
----------

- Issue Tracker: https://github.com/khalidsalomao/SimpleHelpers.Net/issues
- Source Code: https://github.com/khalidsalomao/SimpleHelpers.Net

Support
-------

If you are having issues, please let us know [here](https://github.com/khalidsalomao/SimpleHelpers.Net/issues).

License
-------

The project is licensed under the MIT license.
