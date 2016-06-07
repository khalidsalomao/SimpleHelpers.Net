using SimpleHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Extensions;

namespace SimpleHelpersTests
{
    public class MemoryCacheTest
    {
        static int counter = 0;
        static int factoryCall = 0;
        const int loop = 1000;

        [Fact]
        public void ComplexTest ()
        {
            string v;
            MemoryCache<string>.Clear ();

            MemoryCache<string>.Set ("k1", "test");
            Assert.True ("test" == MemoryCache<string>.Get ("k1"), "item not found after insertion");

            MemoryCache<string>.Set ("k1", "test2");
            Assert.True ("test2" == MemoryCache<string>.Get ("k1"), "item not found after update");

            MemoryCache<string>.Clear ();
            Assert.True (null == MemoryCache<string>.Get ("k1"), "item found after clear");
            
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

            Assert.True (counter == loop, String.Format ("items not expired! Counter: {0}, Expected: {1}", counter, loop));

            // Factory call tests
            for (var i = 0; i < 3; i++)
                Assert.True ("k11" == MemoryCache<string>.GetOrAdd ("k1", ItemFactory), "item factory called when it should not be");

            // clear and check for item factory call
            MemoryCache<string>.Clear ();
            Assert.True ("k12" == MemoryCache<string>.GetOrAdd ("k1", ItemFactory), "item factory called when it should not be");
        }

        [Fact]
        public void GetOrSyncAdd_ComplexParallelTest ()
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
            System.Threading.Tasks.Parallel.ForEach (Enumerable.Range (0, loop), (int i) =>
            {
                MemoryCache<string>.GetOrSyncAdd ("k1", ItemFactory, TimeSpan.FromMinutes (1));
            });
            
            Assert.True (1 == factoryCall, "Factory was called multiple times. Call count: " + factoryCall);
            v = MemoryCache<string>.GetOrAdd ("k1", ItemFactory);
            Assert.True ("k11" == v, "item factory called when it should not be: k11");
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
