using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimpleHelpers.SQLite;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;

namespace Tests
{
    [TestClass]
    public class SQLiteStorageTest
    {
        static string filename = @"C:\temp\testfile.sqlite";
        static SQLiteStorage<string> logDb;

        [ClassInitialize ()]
        public static void ClassInit (TestContext context)
        {            
        }

        [TestInitialize ()]
        public void Initialize ()
        {
            System.Diagnostics.Debug.WriteLine ("SQLiteStorageTest.Initialize");
            logDb = new SQLiteStorage<string> (filename, "Log", SQLiteStorageOptions.KeepItemsHistory ());
        }

        [TestMethod]
        public void SQLiteStorageTest_SimpleTest ()
        {
            var db = new SQLiteStorage<Item1> (filename);
            db.Clear ();

            db.Set ("2", new Item1 { name = "luis", counter = 2, address = "raphael" });
            db.Set ("1", new Item1 { name = "xpto", counter = 1, address = "xpto"});
            db.Set ("3", new Item1 { name = "xpto", counter = 1, address = "xpto" });
            db.Set ("12", new Item1 { name = "name1", counter = 12, address = "address" });

            var obj = db.Find (new { counter = 1, name = "xpto" }).FirstOrDefault ();

           // db.Get ("1");

            Assert.IsNotNull (obj, "item not found!");
            Assert.IsTrue (obj.name == "xpto", "wrong item!");
            Assert.IsTrue (obj.counter == 1, "wrong item!");

            var obj2 = db.Get ("1").Where (i => i.counter == 1 && i.name == "xpto").First ();

            Assert.IsNotNull (obj2, "item not found!");
            Assert.IsTrue (obj2.name == "xpto", "wrong item!");
            Assert.IsTrue (obj2.counter == 1, "wrong item!");            

            Assert.IsTrue (db.Get ("1").Count () == 1, "wrong item count!");
            Assert.IsTrue (db.Get (new string[] { "1", "2" }).Count () == 2, "wrong item count!");                        
            
            var details = db.GetDetails ("1").First ();
            Assert.IsTrue (details.Date.Hour == DateTime.UtcNow.Hour, "wrong date format!");
        }

        public class Item1
        {
            public string name { get; set; }
            public int counter { get; set; }
            public string address { get; set; }
            public string field1 { get; set; }
            public string field2 { get; set; }
            public int[] array1 { get; set; }
            public string[] array2 { get; set; }
        }

        [TestMethod]
        public void SQLiteStoragePerformance_SimpleTest ()
        {
            int loopCounter = 10000;
            var db = new SQLiteStorage<FFUser> (filename);
            db.Clear ();
            uniqueCounter = 0;

            var list = GetTestUserDefinition (loopCounter, "xpto", true).ToList ();

            int len = loopCounter / 50;
            int start = list.Count - (len + 1);
            if (start < 0)
                start = 0;
            len = start + len;
            if (len > list.Count)
                len = list.Count - start;

            Time ("Set Test (" + loopCounter + ")", () =>
            {
                db.Set (list.Select (u => new KeyValuePair<string, FFUser> (u.Login, u)));
            });

            Time ("Get Test All (" + loopCounter + ")", () =>
            {
                db.Get ().ToList ();
            });

            Time ("Get Test All Descending (" + loopCounter + ")", () =>
            {
                db.Get (true).ToList ();
            });

            Time ("Parallel Get Test ForEach Key (" + loopCounter + ")", () =>
            {
                System.Threading.Tasks.Parallel.ForEach (list, u => db.Get (u.Login).First ());
            });

            Time ("Get Test ForEach Key (" + loopCounter + ")", () =>
            {
                foreach (var u in list)
                    db.Get (u.Login).First ();
            });

            Time ("Find Test ForEach Key (" + (len - start) + ")", () =>
            {   
                for (int i = start; i < len; i++)
                    db.Find (new { Login = list[i].Login }).Count ();
            });

            Time ("In-memory Linq Test ForEach Key (" + (len - start) + " x " + loopCounter + ")", () =>
            {
                for (int i = start; i < len; i++)
                    db.Get().Where (u => u.Login == list[i].Login).Count ();
            });

            db.Clear ();

            Time ("Set single insert Test (" + (loopCounter / 10) + ")", () =>
            {
                foreach (var u in list.Take (loopCounter / 10))
                    db.Set (u.Login, u);
            });

            db.Clear ();

            Time ("Parallel Set And Get Test for some keys (" + (loopCounter / 8) + ")", () =>
            {
                Assert.IsTrue (System.Threading.Tasks.Parallel.ForEach (list.Skip (loopCounter / 10).Take (loopCounter / 8),
                    u =>
                    {
                        db.Set (u.Login, u);
                        Assert.IsTrue (db.Get (u.Login).Select (i =>
                        {
                            Assert.IsTrue (i.Login == u.Login, "Error Parallel Set And Get Test for some keys (wrong item)");
                            return i;
                        }).Count () == 1, "parallel fail");

                    }).IsCompleted, "error in Parallel Set And Get Test for some keys ");
            });

            Time ("Vaccum", () =>
            {
                db.Vaccum ();
            });
        }

        public static TimeSpan Time (string opName, Action action)
        {
            Stopwatch stopwatch = Stopwatch.StartNew ();
            action ();
            stopwatch.Stop ();
            System.Diagnostics.Debug.WriteLine ("Operation {0} time: {1}", opName, stopwatch.Elapsed);
            logDb.Set (opName, stopwatch.Elapsed.ToString ());
            return stopwatch.Elapsed;
        }

        public static FFUser GetTestUserDefinition (string login, string group, bool admin)
        {
            return new FFUser
            {
                Login = login,
                Name = login,
                Desc = "TestUser",
                Password = "TestGroup",
                IsEnabled = true,
                IsSystemAdmin = admin,
                Group = group
            };
        }

        public static IEnumerable<FFUser> GetTestUserDefinition (int count, string group, bool admin)
        {
            var prefix = "test_" + (uniqueCounter++).ToString () + "_";
            for (var i = 0; i < count; i++)
            {
                yield return GetTestUserDefinition (prefix + i, group, admin);
            }
        }

        static int uniqueCounter = 0;

        public class FFUser
        {
            private Dictionary<string, string> m_parameters = new Dictionary<string, string> (StringComparer.Ordinal);

            public object Id { get; set; }
            public string Login { get; set; }
            public string Name { get; set; }
            public string SessionId { get; set; }
            public string Group { get; set; }
            public string Desc { get; set; }
            public string Tel1 { get; set; }
            public string Cel1 { get; set; }
            public string Email { get; set; }
            public bool IsEnabled { get; set; }
            public string Password { get; set; }
            public string Question { get; set; }
            public string QuestionAnswer { get; set; }
            public bool IsSystemAdmin { get; set; }
            public Dictionary<string, string> Parameters
            {
                get { return m_parameters; }
                set { m_parameters = value; }
            }            
        }
    }
}
