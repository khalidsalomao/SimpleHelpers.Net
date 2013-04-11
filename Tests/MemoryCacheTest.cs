using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimpleHelpers;

namespace Tests
{
    [TestClass]
    public class MemoryCacheTest
    {
        static int counter = 0;
        static int factoryCall = 0;
        static int loop = 1000;
        static int[] list;

        [ClassInitialize ()]
        public static void ClassInit (TestContext context)
        {
            System.Diagnostics.Debug.WriteLine ("ClassInit");
            // Parallel tests            
            list = new int[loop];
            for (int i = 0; i < loop; i++)
                list[i] = i;
            // warm up threads
            System.Threading.Tasks.Parallel.ForEach (list, (int i) =>
            {
                System.Threading.Thread.Sleep (100);
            });
        }

        [TestInitialize ()]
        public void Initialize ()
        {
            System.Diagnostics.Debug.WriteLine ("TestMethodInit");
        }

        [TestMethod]
        public void MemoryCache_SimpleTest ()
        {
            string v;
            MemoryCache<string>.Clear ();

            MemoryCache<string>.Set ("k1", "test");
            Assert.AreEqual (MemoryCache<string>.Get ("k1"), "test", "item not found after insertion");

            MemoryCache<string>.Set ("k1", "test2");
            Assert.AreEqual (MemoryCache<string>.Get ("k1"), "test2", "item not found after update");

            MemoryCache<string>.Clear ();
            Assert.AreEqual (MemoryCache<string>.Get ("k1"), null, "item found after clear");
            
            // Expiration test
            // clear counters
            counter = 0;

            // setup
            MemoryCache<string>.Clear ();
            MemoryCache<string>.Expiration = TimeSpan.FromMilliseconds (1);
            MemoryCache<string>.OnExpiration -= onExpirationCounter;
            MemoryCache<string>.OnExpiration += onExpirationCounter;
            MemoryCache<string>.MaintenanceStep = TimeSpan.FromMilliseconds (100);

            for (int i = 0; i < loop; i++)
            {
                MemoryCache<string>.Set (i.ToString (), i.ToString ());
            }
 
            System.Threading.Thread.Sleep (220);

            Assert.IsTrue (counter == loop, "items not expired! Counter: {0}, Expected: {1}", counter, loop);

            // Factory call tests

            Assert.AreEqual (MemoryCache<string>.GetOrAdd ("k1", ItemFactory), "k11", "item factory not called");
            v = MemoryCache<string>.GetOrAdd ("k1", ItemFactory);
            Assert.AreEqual (v, "k11", "item factory called when it should not be: k11");
            Assert.AreNotEqual (v, "k12", "item factory called when it should not be : k12");
        }

        [TestMethod]
        public void MemoryCache_ParallelFatorySyncTest ()
        {
            string v;
            // Parallel tests

            // setup
            MemoryCache<string>.Clear ();
            // increase expiration timeout to avoid interferences            
            MemoryCache<string>.Expiration = TimeSpan.FromMinutes (1);
            
            // reset factoryCall counter
            System.Threading.Interlocked.Exchange (ref factoryCall,  0);
            // tesk sync factory call with long wait
            System.Threading.Tasks.Parallel.ForEach (list, (int i) =>
            {
                MemoryCache<string>.GetOrSyncAdd ("k1", ItemFactory, TimeSpan.FromMinutes (1));
            });
            
            Assert.IsTrue (factoryCall == 1, "Factory was called multiple times. Call count: {0}", factoryCall);
            v = MemoryCache<string>.GetOrAdd ("k1", ItemFactory);
            Assert.AreEqual (v, "k11", "item factory called when it should not be: k11");
        }
  
        static void onExpirationCounter (string key, string item)
        {
            counter++;
        }

        static string ItemFactory (string key)
        {
            return key + System.Threading.Interlocked.Increment (ref factoryCall);
        }

    }
}
