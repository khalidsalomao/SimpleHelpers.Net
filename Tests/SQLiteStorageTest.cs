using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimpleHelpers.SQLite;
using System.Linq;

namespace Tests
{
    [TestClass]
    public class SQLiteStorageTest
    {
        [ClassInitialize ()]
        public static void ClassInit (TestContext context)
        {            
        }

        [TestInitialize ()]
        public void Initialize ()
        {
            System.Diagnostics.Debug.WriteLine ("SQLiteStorageTest.Initialize");
        }

        [TestMethod]
        public void SQLiteStorageTest_SimpleTest ()
        {
            string filename = @"C:\temp\testfile.sqlite";
            var db = new SQLiteStorage<Item1> (filename);

            db.Set ("2", new Item1 { name = "luis", counter = 2, address = "raphael" });
            db.Set ("1", new Item1 { name = "xpto", counter = 1, address = "xpto"});

            var obj = db.Find ("1", new { name = "xpto" }).FirstOrDefault ();

           // db.Get ("1");

            Assert.IsNotNull (obj, "item not found!");
            Assert.IsTrue (obj.name == "xpto", "wrong item!");
            Assert.IsTrue (obj.counter == 1, "wrong item!");
        }

        public class Item1
        {
            public string name { get; set; }
            public int counter { get; set; }
            public string address { get; set; }
        }
    }
}
