using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace PerformanceTest
{
    public class Benchmark: IDisposable
    {
        string m_name;
        Stopwatch stopwatch;

        public Benchmark (string opName)
        {
            m_name = opName;
            stopwatch = new Stopwatch ();            
        }

        public void Dispose ()
        {
            stopwatch.Stop ();
            Common.Logging.LogManager.GetCurrentClassLogger ().Info (ToString ());
        }

        public override string ToString ()
        {
            return string.Format (String.Format ("Timing for {0}:\t {1}", m_name, stopwatch.Elapsed));
        }

        public Benchmark Start (bool runGCbeforeStart = false)
        {
            if (runGCbeforeStart)
                PrepareSystemForBenchmark ();
            stopwatch.Start ();
            return this;
        }

        public TimeSpan Stop()
        {
            stopwatch.Stop ();
            return stopwatch.Elapsed;
        }

        /// <summary>
        /// Prepares the system for benchmark will force Garbage Collection to run
        /// with maximum generation and wait it to finish.
        /// </summary>
        public static void PrepareSystemForBenchmark ()
        {
            GC.Collect (GC.MaxGeneration, GCCollectionMode.Forced);
            GC.WaitForPendingFinalizers ();
            System.Threading.Thread.Sleep (0);
        }

        public static Benchmark Start (string name)
        {
            return new Benchmark (name).Start();
        }

        public static Benchmark Start (string name, params object[] arguments)
        {
            return new Benchmark (String.Format (name, arguments)).Start ();
        }

        public static TimeSpan Time (string opName, Action action)
        {
            Stopwatch stopwatch = Stopwatch.StartNew ();
            action ();
            stopwatch.Stop ();
            Common.Logging.LogManager.GetCurrentClassLogger ().Info (String.Format ("Timing for {0}:\t {1}", opName, stopwatch.Elapsed));
            return stopwatch.Elapsed;
        }

        public static TimeSpan Time (string opName, int loopCount, Action action)
        {
            Stopwatch stopwatch = Stopwatch.StartNew ();
            for (var i = 0; i < loopCount; i++)
                action ();
            stopwatch.Stop ();
            Common.Logging.LogManager.GetCurrentClassLogger ().Info (String.Format ("Timing for {0} run {1} times:\t {1}", opName, loopCount, stopwatch.Elapsed));
            return stopwatch.Elapsed;
        }
    }
}
