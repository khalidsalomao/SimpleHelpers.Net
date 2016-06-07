using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace SimpleHelpers
{
    public class Arguments
    {
        public FlexibleOptions Parsed { get; set; }

        public bool ThrownOnError { get; set; }

        public int RetryCount { get; set; }

        public string[] ExternalFiles { get; set; }

        #region *   Events and Event Handlers   *

        public event EventHandler<Exception> OnError;

        public void RaiseErrorEvent (Exception ex)
        {
            var localHandler = OnError;
            if (localHandler != null)
                localHandler (this, ex);
        }
        
        #endregion

        public Arguments () {}

        /// <summary>
        /// Checks the command line params.<para/>
        /// arguments format: key=value or --key value
        /// </summary>
        /// <param name="args">The args.</param>
        public FlexibleOptions Parse (string[] args)
        {
            // 1. parse local configuration file
            // display the options listed in the configuration file                 
            FlexibleOptions localOptions = ParseAppSettings ();

            // 2. parse console arguments
            // parse arguments like: key=value
            FlexibleOptions argsOptions = ParseCommandLineArguments (args);

            // 3. merge arguments with app.config options. Priority: arguments > app.config
            Parsed = FlexibleOptions.Merge (localOptions, argsOptions);

            // 4. check for external config file
            // set config alias
            Parsed.SetAlias ("config", "S3ConfigurationPath", "webConfigurationFile");

            // load and parse web hosted configuration file (priority order: argsOptions > localOptions)
            ExternalFiles = Parsed.GetAsList ("config", new char[] { ',', ';' });
            FlexibleOptions externalLoadedOptions = ParseExternalFiles (ExternalFiles);

            // 5. merge options with the following priority:
            // 1. console arguments
            // 2. external file with json configuration object (local or web)
            // 3. local configuration file (app.config or web.config)
            Parsed = FlexibleOptions.Merge (Parsed, externalLoadedOptions, argsOptions);

            // return final merged options
            return Parsed;
        }

        public FlexibleOptions ParseAppSettings ()
        {            
            // parse local configuration file
            // display the options listed in the configuration file
            var localOptions = new FlexibleOptions ();
            try
            {
                var appSettings = System.Configuration.ConfigurationManager.AppSettings;
                foreach (var k in appSettings.AllKeys)
                {
                    localOptions.Set (k, appSettings[k]);
                }
            }
            catch (Exception appSettingsEx)
            {
                if (ThrownOnError)
                    throw;
                RaiseErrorEvent (appSettingsEx);
            }
            return localOptions;
        }

        public FlexibleOptions ParseCommandLineArguments (string[] args)
        {
            var argsOptions = new FlexibleOptions ();
            if (args != null)
            {
                string arg;
                bool openTag = false;
                string lastTag = null;
                for (int ix = 0; ix < args.Length; ix++)
                {
                    arg = args[ix];
                    // check for option with key=value sintax (restriction: the previous tag must not be an open tag)
                    // also valid for --key:value
                    bool hasStartingMarker = arg.StartsWith ("-", StringComparison.Ordinal) || arg.StartsWith ("/", StringComparison.Ordinal);
                    int p = arg.IndexOf ('=');
                    if (p > 0 && (hasStartingMarker || !openTag))
                    {
                        argsOptions.Set (arg.Substring (0, p).Trim ().TrimStart ('-', '/'), arg.Substring (p + 1).Trim ());
                        lastTag = null;
                        openTag = false;
                    }
                    // search for tag stating with special character
                    // a linux path should be valid: -path /home/file
                    else if (hasStartingMarker && !(openTag && arg[0] == '/'))
                    {
                        lastTag = arg.Trim ().TrimStart ('-', '/');
                        argsOptions.Set (lastTag, "true");
                        openTag = true;
                    }
                    // set value of last tag
                    else if (lastTag != null)
                    {
                        argsOptions.Set (lastTag, arg.Trim ());
                        openTag = false;
                    }
                }
            }
            return argsOptions;
        }

        public FlexibleOptions ParseExternalFiles (string[] filePaths)
        {
            // load and parse web hosted configuration file (priority order: argsOptions > localOptions)
            FlexibleOptions parsed = null;
            try
            {
                ExternalFiles = filePaths;
                foreach (var file in filePaths)
                {
                    var loaded = LoadExtenalConfigurationFile (file.Trim (' ', '\'', '"', '[', ']'));
                    if (parsed == null)
                        parsed = loaded;
                    else
                        parsed.AddRange (loaded);
                }
            }
            catch (Exception ex)
            {
                if (ThrownOnError)
                    throw;
                RaiseErrorEvent (ex);
            }
            return parsed;
        }

        private FlexibleOptions LoadExtenalConfigurationFile (string filePath)
        {
            if (String.IsNullOrEmpty (filePath))
                return null;
            // prepare
            filePath = filePath.Trim ();
            // load content
            string content;
            if (filePath.StartsWith ("http", StringComparison.OrdinalIgnoreCase))
            {
                content = LoadWebConfigurationFile (filePath);
            }
            else
            {
                content = LoadFileSystemConfigurationFile (filePath);
            }
            // parse content
            return ParseFileContent (content);            
        }

        private string LoadWebConfigurationFile (string filePath)
        {
            // try to download configuration, retry in case of network failure or other intermitent failures
            // NOTE: in MONO (linux) this can happen with ssl certificates and dozens of instances of the app starting at the same time...
            var count = (RetryCount > 0) ? RetryCount : 1;
            for (var i = 0; i < count; i++)
            {
                try
                {
                    using (WebClient client = new WebClient ())
                    {
                        return client.DownloadString (filePath);
                    }
                }
                catch (Exception ex)
                {
                    if (i >= 2)
                    {
                        if (ThrownOnError)
                            throw;
                        RaiseErrorEvent (ex);
                    }
                    else
                    {
                        System.Threading.Tasks.Task.Delay (100).Wait ();
                    }
                }
            }
            return null;
        }

        private string LoadFileSystemConfigurationFile (string filePath)
        {
            try
            {
                return System.IO.File.ReadAllText (filePath);
            }
            catch (Exception ex)
            {
                if (ThrownOnError)
                    throw;
                RaiseErrorEvent (ex);
                return null;
            }
        }

        private FlexibleOptions ParseFileContent (string content)
        {
            var options = new FlexibleOptions ();
            if (String.IsNullOrEmpty (content))
                return options;
            // prepare content
            content = content.Trim ();
            try
            {
                // detect xml
                if (content.StartsWith ("<"))
                {
                    var xmlDoc = System.Xml.Linq.XDocument.Parse (content);
                    var root = xmlDoc.Descendants ("config").FirstOrDefault ();
                    if (root != null && root.HasElements)
                    {
                        foreach (var i in root.Elements ())
                        {
                            options.Set (i.Name.ToString (), i.Value);
                        }
                    }
                }
                // parse as json
                else
                {
                    var json = Newtonsoft.Json.Linq.JObject.Parse (content);
                    foreach (var i in json)
                    {
                        options.Set (i.Key, i.Value.ToString (Newtonsoft.Json.Formatting.None));
                    }
                }
            }
            catch (Exception ex)
            {
                if (ThrownOnError)
                    throw;
                RaiseErrorEvent (ex);
                return null;
            }
            return options;
        }
    }
}
