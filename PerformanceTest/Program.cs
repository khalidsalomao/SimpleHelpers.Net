using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PerformanceTest.Logging;
using SimpleHelpers;

namespace PerformanceTest
{
    class Program
    {
        static int verbosity;
        static List<string> parsedOptions;
        static ILog logger = LogProvider.For<Program> ();
        static int loopCount = 1000;
        static string testName;

        static void Main (string[] args)
        {
            try
            {
                // Program initialization
                ConsoleUtils.Initialize (args, false);

                logger.Info ("test");
                // Initialize possible options
                // http://www.ndesk.org/Options for more info
                ParseCmdOptions (args);
                // run op
                ExecuteTest (testName, loopCount);
            }
            catch (Exception ex)
            {
                logger.FatalException ("Unexpected error", ex);
            }
            // wait before exit
            Console.WriteLine ();
            Console.WriteLine ("Press any key to end the application...");
            Console.ReadKey ();
            // exit
            ApplicationExit (0);
        }

        /// <summary>
        /// Gracefully exit the application, doing the necessary cleanup.
        /// </summary>
        /// <param name="exitCode">Exit code to be given to the operating system.</param>
        public static void ApplicationExit (int exitCode = 0)
        {
            // Display logged messages
            System.Threading.Thread.Sleep (100);
            System.Environment.Exit (exitCode);
        }

        private static void ParseCmdOptions (string[] args)
        {
            // Initialize possible options
            // http://www.ndesk.org/Options for more info
            optionsParser.Add ("t|test=", "The name|id of the {test} to be executed.", v => testName = v);
            optionsParser.Add ("r|repeat=", "The number of {TIMES} to repeat the test tin a single loop to get the total timming.\nThis must be an integer.\nDefault to 1000.", v => loopCount = Int32.Parse (v));
            optionsParser.Add ("h|?|help", "Help with options descriptions", v => ShowHelp ());
            optionsParser.Add ("q|quiet", "Application will only display errors messages", o => ConfigureLog ("ERROR"));
            optionsParser.Add ("v|verbose", "Application will only display errors messages", o => ConfigureLog ("TRACE"));
            // Parse options
            try
            {
                parsedOptions = optionsParser.Parse (args);
                foreach (var i in parsedOptions)
                    Console.WriteLine (i);
            }
            catch (Mono.Options.OptionException e)
            {
                logger.ErrorException ("Error parsing argument options", e);
                Console.WriteLine ("Invalid options: ");
                Console.WriteLine (e.Message);
                Console.WriteLine ();
                Console.WriteLine ("Try '--help' for more information.");
                ApplicationExit (-1);
            }
        }

        private static void ConfigureLog (string level)
        {
            //// create properties
            //var properties = new System.Collections.Specialized.NameValueCollection ()
            //{
            //    { "level", level.ToUpperInvariant () },
            //   { "showDateTime", "true" },
            //    { "showLogName", "true" },
            //    { "dateTimeFormat", "yyyy/MM/dd HH:mm:ss:fff" }
            //};
            //// set Adapter
            //Common.Logging.LogManager.Adapter = new Common.Logging.Simple.ConsoleOutLoggerFactoryAdapter (properties);
            //logger = Common.Logging.LogManager.GetCurrentClassLogger ();
        }

        static void ShowHelp ()
        {
            Console.WriteLine ();
            Console.WriteLine ("Options:");
            if (optionsParser != null)
            {
                optionsParser.WriteOptionDescriptions (Console.Out);
            }
            Console.WriteLine ();
            ApplicationExit ();
        }

        private static void ExecuteTest (string testName, int loopCount)
        {
            if (String.IsNullOrWhiteSpace (testName))
                throw new ArgumentException ("No test selected.", testName);
            if (loopCount <= 0)
                throw new ArgumentException ("Invalid number of repeats.", loopCount.ToString ());

            switch (testName.Trim ().ToLowerInvariant ())
            {
                case "json":
                    JsonSpeedTest.Test (loopCount);
                    break;
                case "sqlitestorage":
                    SQLiteStorageTest.Test (loopCount);
                    break;
                default:
                    throw new ArgumentException ("No test selected.", testName);
            }
        }
        
    }
}
