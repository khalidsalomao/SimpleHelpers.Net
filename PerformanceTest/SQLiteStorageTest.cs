using System;
using SimpleHelpers.SQLite;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using PerformanceTest.Logging;

namespace PerformanceTest
{
    public class SQLiteStorageTest
    {
        static string filename = System.IO.Path.Combine (AppDomain.CurrentDomain.BaseDirectory, "storage.sqlite");
        static SQLiteStorage<string> logDb;
        static ILog logger = LogProvider.For<SQLiteStorageTest> ();

        public SQLiteStorageTest ()
        {
            logger.Info ("Initialize");
            logDb = new SQLiteStorage<string> (filename, "Log", SQLiteStorageOptions.KeepItemsHistory ());
        }

        public static void Test (int loopCount)
        {
            // warm up
            logger.Info ("Warm up");
            TestInternal (1);

            logger.Info ("Real test");
            TestInternal (loopCount);
        }

        static void TestInternal (int loopCount)
        {
            var db = new SQLiteStorage<MockObjectGen.MockUser> (filename);
            db.Clear ();
            db.Shrink ();
            ServiceStack.Text.JsConfig.DateHandler = ServiceStack.Text.JsonDateHandler.ISO8601;

            logger.Info (String.Format ("Initializing {0} items", loopCount));
            var list = MockObjectGen.GetTestUserDefinition (loopCount, "group name test", true).ToList ();

            int len = loopCount / 50;
            int start = list.Count - (len + 1);
            if (start < 0)
                start = 0;
            len = start + len;
            if (len > list.Count)
                len = list.Count - start;

            using (Benchmark.Start ("Insertion test {0}", list.Count))
            {
                foreach (var i1 in list)
                    db.Set (i1.Login, i1);
            }

            using (Benchmark.Start ("Get All with index {0}", list.Count))
            {
                foreach (var i in list)
                    db.Get (i.Login).Count ();
            }

            using (Benchmark.Start ("Get All with linq filter {0} x {1}", (len - start), loopCount))
            {
                for (int i = start; i < len; i++)
                    db.Get ().Where (u => u.Login == list[i].Login).Count ();
            }

            using (Benchmark.Start ("Parallel Set And Get Test for some keys ({0})", loopCount / 10))
            {
                var forRes = System.Threading.Tasks.Parallel.ForEach (list.Skip (loopCount / 10).Take (loopCount / 10),
                    u =>
                    {
                        db.Set (u.Login, u);
                        db.Get (u.Login).First ();
                        db.Get (u.Login).Count ();
                    });
                if (!forRes.IsCompleted)
                    throw new Exception ("Parallel execution error!");
            };

            db.Clear ();
            db.Shrink ();
        }
    }
}
