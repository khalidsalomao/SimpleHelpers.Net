using System;
using SimpleHelpers.SQLite;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;

namespace PerformanceTest
{
    public class SQLiteStorageTest
    {
        static string filename = System.IO.Path.Combine (AppDomain.CurrentDomain.BaseDirectory, "storage.sqlite");
        static SQLiteStorage<string> logDb;

        public SQLiteStorageTest ()
        {
            Common.Logging.LogManager.GetCurrentClassLogger ().Info ("Initialize");
            logDb = new SQLiteStorage<string> (filename, "Log", SQLiteStorageOptions.KeepItemsHistory ());
        }

        public static void Test (int loopCount)
        {
            // warm up
            Common.Logging.LogManager.GetCurrentClassLogger ().Info ("Warm up");
            TestInternal (1);

            Common.Logging.LogManager.GetCurrentClassLogger ().Info ("Real test");
            TestInternal (loopCount);
        }

        static void TestInternal (int loopCount)
        {
            var db = new SQLiteStorage<MockObjectGen.MockUser> (filename);
            db.Clear ();
            db.Vaccum ();
            ServiceStack.Text.JsConfig.DateHandler = ServiceStack.Text.JsonDateHandler.ISO8601;
            
            Common.Logging.LogManager.GetCurrentClassLogger ().Info (String.Format ("Initializing {0} items", loopCount));
            var list = MockObjectGen.GetTestUserDefinition (loopCount, "group name test", true).ToList ();

            int len = loopCount / 50;
            int start = list.Count - (len + 1);
            if (start < 0)
                start = 0;
            len = start + len;
            if (len > list.Count)
                len = list.Count - start;

            using (Benchmark.Start ("Insertion test"))
            {
                foreach (var i1 in list)
                    db.Set (i1.Login, i1);
            }

            using (Benchmark.Start ("Get All with linq filter {0} x {1}", (len - start), loopCount))
            {
                for (int i = start; i < len; i++)
                    db.Get ().Where (u => u.Login == list[i].Login).Count ();
            }

            db.Clear ();
            db.Vaccum ();
        }
    }
}
