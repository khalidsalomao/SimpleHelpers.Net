using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.Text;

namespace PerformanceTest
{
    public class JsonSpeedTest
    {
        internal static void Test (int loopCount)
        {
            // warm up
            Common.Logging.LogManager.GetCurrentClassLogger ().Info ("Warm up");
            TestInternal (1);

            Common.Logging.LogManager.GetCurrentClassLogger ().Info ("Real test");
            TestInternal (loopCount);
        }

        internal static void TestInternal (int loopCount)
        {
            Common.Logging.LogManager.GetCurrentClassLogger ().Info ("Initializing 10.000 items");
            var list = MockObjectGen.GetTestUserDefinition (10000, "group name test", true).ToList ();
            var txt = new string[list.Count];

            using (Benchmark.Start ("JsonSpeedTest.Newtonsoft.Serialize"))
            {
                for (var i = 0; i < loopCount; i++)
                {
                    for (int i1 = 0; i1 < list.Count; i1++)
                        txt[i1] = Newtonsoft.Json.JsonConvert.SerializeObject (list[i1]);
                }
            }

            using (Benchmark.Start ("JsonSpeedTest.Newtonsoft.Deserialize"))
            {
                for (var i = 0; i < loopCount; i++)
                {
                    for (int i1 = 0; i1 < txt.Length; i1++)
                        Newtonsoft.Json.JsonConvert.DeserializeObject<PerformanceTest.MockObjectGen.MockUser> (txt[i1]);
                }
            }

            using (Benchmark.Start ("JsonSpeedTest.fastJSON.Serialize"))
            {
                for (var i = 0; i < loopCount; i++)
                {
                    for (int i1 = 0; i1 < list.Count; i1++)
                        txt[i1] = fastJSON.JSON.Instance.ToJSON (list[i1]);
                }
            }

            using (Benchmark.Start ("JsonSpeedTest.fastJSON.Deserialize"))
            {
                for (var i = 0; i < loopCount; i++)
                {
                    for (int i1 = 0; i1 < txt.Length; i1++)
                        fastJSON.JSON.Instance.ToObject <PerformanceTest.MockObjectGen.MockUser> (txt[i1]);
                }
            }

            using (Benchmark.Start ("JsonSpeedTest.ServiceStack.Serialize"))
            {
                for (var i = 0; i < loopCount; i++)
                {
                    for (int i1 = 0; i1 < list.Count; i1++)
                        txt[i1] = ServiceStack.Text.JsonSerializer.SerializeToString (list[i1]);
                }
            }

            using (Benchmark.Start ("JsonSpeedTest.ServiceStack.Deserialize"))
            {
                for (var i = 0; i < loopCount; i++)
                {
                    for (int i1 = 0; i1 < txt.Length; i1++)
                        ServiceStack.Text.JsonSerializer.DeserializeFromString<PerformanceTest.MockObjectGen.MockUser> (txt[i1]);
                }
            }

            using (Benchmark.Start ("JsonSpeedTest.ServiceStack.Serialize [JSV]"))
            {
                for (var i = 0; i < loopCount; i++)
                {
                    for (int i1 = 0; i1 < list.Count; i1++)
                        txt[i1] = ServiceStack.Text.TypeSerializer.SerializeToString (list[i1]);
                }
            }

            using (Benchmark.Start ("JsonSpeedTest.ServiceStack.Deserialize [JSV]"))
            {
                for (var i = 0; i < loopCount; i++)
                {
                    for (int i1 = 0; i1 < txt.Length; i1++)
                        ServiceStack.Text.TypeSerializer.DeserializeFromString<PerformanceTest.MockObjectGen.MockUser> (txt[i1]);
                }
            }
        }
    }
}
