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
            GC.Collect ();
            GC.WaitForPendingFinalizers ();
            stopwatch.Start ();
        }

        public void Dispose ()
        {
            stopwatch.Stop ();
            Common.Logging.LogManager.GetCurrentClassLogger ().Info (String.Format ("Timing for {0}:\t {1}", m_name, stopwatch.Elapsed));
        }

        public static Benchmark Start (string name)
        {
            return new Benchmark (name);
        }

        public static Benchmark Start (string name, params object[] arguments)
        {
            return new Benchmark (String.Format (name, arguments));
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
            Common.Logging.LogManager.GetCurrentClassLogger ().Info (String.Format ("Timing for {0}:\t {1}", opName, stopwatch.Elapsed));
            return stopwatch.Elapsed;
        }
    }
}
