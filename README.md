SimpleHelpers.Net
=================

Collection of simple pieces of utility code

Examples
--------
<h3>MemoryCache</h3>

Simple configuration of the MemoryCache settings.

```csharp

// setup
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

Simple usage.

```csharp

// store and item
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

Simple usage where we try to aquire the lock for 100 ms. 
So if somewhere else in our application this same lock was already aquired, we will wait until we aquire the lock or 100 ms.

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

Another usage example.

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
