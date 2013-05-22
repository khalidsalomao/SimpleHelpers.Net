SimpleHelpers.Net
=================

Collection of simple pieces of utility code

Description and Examples
--------
<h3>MemoryCache</h3>

Simple lightweight object in-memory cache, with a background timer to remove expired objects.

Fast in-memory cache for data that are expensive to create and can be used in a thread-safe manner.

All stored items are kept in concurrent data structures (ConcurrentDictionary) to allow multi-thread usage of the MemoryCache static methods. Note that the stored objects must be **thread-safe**, since the same instace of an object can and will be returned by multiple calls of *Get* methods. If you wish to use non-thread safe object instances you must use the *Remove* method to atomically (safelly) get and remove the object instance from the cache.

Note: this nuget package contains csharp source code and depends on System.Collections.Concurrent introduced in .Net 4.0.

**Configuration**

Simple configuration of the MemoryCache settings. 
It should be done for each type, since since the JIT compiler will generate differente code at run time, MemoryCache<string> is considered a diferent class, for example, from MemoryCache<StringBuilder> or MemoryCache<byte[]>.

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

**Example**

```csharp

// store an item
MemoryCache<string>.Set ("k1", "test");

// get it
string value = MemoryCache<string>.Get ("k1");
// check if we got it
if (value != null)
{
	// ok we got it!
}

```

Using a factory to create a new item if the in memory cache does not have it.

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

Using a more complex concurrent scenario where, if the item is not found, we only want to call the factory only once.
So all concurrent calls will wait until factory (that is guaranteed to be called only once) has created and stored the new item, and will return it.

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

<h3>NamedLock</h3>

Synchronization helper: a static lock collection associated with a key.

NamedLock manages the lifetime of critical sections that can be accessed by a key (name) throughout the application. It also have some helper methods to allow a maximum wait time (timeout) to aquire the lock and safelly release it.
	
Note: this nuget package contains c# source code and depends on System.Collections.Concurrent introduced in .Net 4.0.

**Example**

Simple usage where we try to aquire the lock for 100 ms. 
So if somewhere else in our application this same lock was already aquired, we will wait until we aquire the lock or 100 ms has passed.

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

<h3>ObjectPool</h3>

A simple lightweight object pool for fast and simple object reuse.

Fast lightweight thread-safe object pool for objects that are expensive to create or could efficiently be reused.

Note: this nuget package contains c# source code and depends on System.Collections.Concurrent introduced in .Net 4.0.

**Example**

```csharp

// Get or create a new random generator
Random rnd = ObjectPool<Random>.Get (CreateRandomGenerator);
// start generating random numbers
try
{	
	// ...
	
	var num = rnd.Next (max);
	
	// ...
}
finally
{
	// Release the random generator by putting it back in the object pool
	ObjectPool<Random>.Put (rnd);
}

// our factory
private static Random CreateRandomGenerator ()
{
	return new Random ((int)DateTime.UtcNow.Ticks);
}

```

<h3>TimedQueue</h3>
Simple lightweight queue that stores data in a concurrent queue and periodically process the queued items.

Userful for:
* processing items in batches;
* grouping data for later processing;
* async processing (consumer/producer);
* etc.

Note: this nuget package contains c# source code and depends on System.Collections.Concurrent introduced in .Net 4.0.

**Configuration**

Simple configuration of the TimedQueue settings. 
It should be done for each type, since since the JIT compiler will generate differente code at run time, TimedQueue<string> is considered a diferent class, for example, from TimedQueue<StringBuilder> or TimedQueue<byte[]>.

```csharp

// setup (called once in application initialization)

// set the queue timed task to run each 500 ms executing the registered action
SimpleHelpers.TimedQueue<Our_Object>.TimerStep = TimeSpan.FromMilliseconds (500);
// Our event, if we want to treat the removed expired items
MemoryCache<string>.OnExecution += (IEnumerable<Our_Object> items) => 
{ 
	foreach (var evt in items)
		// do something
};

```

**Example**

```csharp

// our method that does an network call to store out object 
// and keeps retrying in case of failure
public static void SaveEvent (Our_Object evt)
{
	// try to save
	try
	{
		SaveOverTheInternet (evt);
	}
	catch (System.IO.IOException ex)
	{
		// log.Error (ex);
		SimpleHelpers.TimedQueue<Our_Object>.Put (evt);
	}
}

```

<h3>ConfigManager</h3>

Simple configuration manager to get and set the values in the AppSettings section of the default configuration file.

Note: this nuget package contains csharp source code and depends on Generics introduced in .Net 2.0.

**Configuration**

```csharp

// setup (called once in application initialization)

// set to add any new keys added during the application execution
SimpleConfiguration.AddNonExistingKeys = true;

```

** Example **

```csharp

string address = SimpleConfiguration.Get ("MongoDBaddress", "localhost");
int port = SimpleConfiguration.Get ("MongoDBport", 21766);

```

<h3>SQLiteStorage</h3>

Simple key value storage using sqlite.

All member methods are thread-safe, so a instance can be safelly be accessed by multiple threads.

All stored items are serialized to json by json.net.

Note: this nuget package contains csharp source code and depends on .Net 4.0.

**Configuration**

```csharp

// setup:
SQLiteStorage<My_Class> db = new SQLiteStorage<My_Class> ("path_to_my_file.sqlite", SQLiteStorageOptions.UniqueKeys ());

```

** Example **

```csharp

// create a new instance
SQLiteStorage<My_Class> db = new SQLiteStorage<My_Class> ("path_to_my_file.sqlite", SQLiteStorageOptions.UniqueKeys ());

// save an item with a key associated
db.Set ("my_key_for_this_item", new My_Class ());
// get it back
My_Class my_obj = db.Get ("my_key_for_this_item").FirstOrDefault ();

// to save any changes, just call set again
db.Set ("my_key_for_this_item", my_obj);

// get all stored items
foreach (var item in db.Get ())
{
	// ...
}
```
