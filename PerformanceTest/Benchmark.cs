using System;
using System.Diagnostics;
using PerformanceTest.Logging;

namespace PerformanceTest
{
    public class Benchmark: IDisposable
    {
        static ILog logger = LogProvider.For<Benchmark> ();

        Stopwatch _stopwatch;

        public string Name { get; set; }

        public TimeSpan Elapsed 
        { 
            get { return _stopwatch.Elapsed; }
        }

        public Benchmark (string opName)
        {
            Name = opName;
            _stopwatch = new Stopwatch ();            
        }

        public void Dispose ()
        {
            _stopwatch.Stop ();
            logger.Info (ToString ());
        }

        public override string ToString ()
        {
            return String.Format (String.Format ("Timing for {0}:\t {1}", Name, _stopwatch.Elapsed));
        }

        public Benchmark Start (bool skipWarmupGC = false)
        {
            if (!skipWarmupGC)
                PrepareSystemForBenchmark ();
            _stopwatch.Start ();
            return this;
        }

        public TimeSpan Stop()
        {
            _stopwatch.Stop ();
            return _stopwatch.Elapsed;
        }

        /// <summary>
        /// Prepares the system for benchmark will force Garbage Collection to run
        /// with maximum generation and wait it to finish.
        /// </summary>
        public static void PrepareSystemForBenchmark ()
        {
            GC.Collect (GC.MaxGeneration, GCCollectionMode.Forced);
            GC.WaitForPendingFinalizers ();
            GC.Collect ();
            System.Threading.Thread.Sleep (0);
        }

        public static Benchmark Start (string opName)
        {
            return new Benchmark (opName).Start();
        }

        public static Benchmark Start (string opName, params object[] arguments)
        {
            return new Benchmark (String.Format (opName, arguments)).Start ();
        }

        public static TimeSpan Time (string opName, Action action)
        {
            var mark = new Benchmark (opName).Start ();
            action ();
            mark.Stop ();
            logger.Info (mark.ToString ());
            return mark.Elapsed;
        }

        public static TimeSpan Time (string opName, Action action, int loopCount, bool warmup)
        {
            if (warmup)
                action ();
            var mark = new Benchmark (opName).Start ();
            for (var i = 0; i < loopCount; i++)
                action ();
            mark.Stop ();
            logger.Info (String.Format ("Timing for {0} run {1} times:\t {1}", opName, loopCount, mark.Elapsed));
            return mark.Elapsed;
        }
    }
}
