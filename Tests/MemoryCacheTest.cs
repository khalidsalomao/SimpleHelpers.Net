using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class MemoryCacheTest
    {
        [TestMethod]
        public void TestMethod1 ()
        {
            MemoryCache<string>.Set ()
        }
    }
}
