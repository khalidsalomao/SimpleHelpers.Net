using System;
using System.Collections.Generic;
using System.Linq;

namespace SimpleHelpers
{
    /// For updated code: https://gist.github.com/khalidsalomao/5065646
    /// Articles on CodeProject

    public class ConfigManager
    {
        private static System.Configuration.Configuration m_instance = null;
        private static object m_lock = new object ();

        protected static Func<System.Configuration.Configuration> LoadConfiguration;

        private static System.Configuration.Configuration GetConfig ()
        {
            if (m_instance == null)
            {
                lock (m_lock)
                {
                    if (m_instance == null)
                    {
                        if (LoadConfiguration == null)
                        {
                            // get process name
                            var name = System.Diagnostics.Process.GetCurrentProcess ().ProcessName;
                            // remove extension
                            int num = name.LastIndexOf ('.');
                            if (num > 0)
                            {
                                name = name.Substring (0, num);
                            }
                            // check name
                            if (name.Equals ("w3wp", StringComparison.OrdinalIgnoreCase) || name.Equals ("aspnet_wp", StringComparison.OrdinalIgnoreCase))
                            {   //is web app
                                LoadConfiguration = () => System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration ("~");
                            }
                            else
                            {   //is windows app
                                LoadConfiguration = () => System.Configuration.ConfigurationManager.OpenExeConfiguration (System.Configuration.ConfigurationUserLevel.None);
                            }
                        }
                        m_instance = LoadConfiguration ();
                    }
                }
            }
            return m_instance;
        }

        public static bool AddNonExistingKeys { get; set; }

        public static T Get<T> (string key, T defaultValue = default(T))
        {
            var cfg = GetConfig ().AppSettings.Settings;
            var item = cfg[key];
            if (item != null)
            {
                return Converter (item.Value, defaultValue);
            }
            else if (AddNonExistingKeys)
            {
                Set (key, defaultValue);
            }
            return defaultValue;
        }

        public static void Set<T> (string key, T value)
        {
            var mgr = GetConfig ();
            var cfg = mgr.AppSettings.Settings;
            var item = cfg[key];
            if (item == null)
            {
                cfg.Add (key, value.ToString ());
            }
            else
            {
                item.Value = value.ToString ();
            }
            mgr.Save (System.Configuration.ConfigurationSaveMode.Modified);
        }

        public static void Set (IEnumerable<KeyValuePair<string, string>> values)
        {
            var mgr = GetConfig ();
            var cfg = mgr.AppSettings.Settings;
            foreach (var i in values)
            {
                var item = cfg[i.Key];
                if (item == null)
                {
                    cfg.Add (i.Key, i.Value);
                }
                else
                {
                    item.Value = i.Value;
                }
            }
            mgr.Save (System.Configuration.ConfigurationSaveMode.Modified);
        }

        public static T Converter<T> (object input, T defaultValue = default(T))
        {
            if (input != null)
            {
                try
                {
                    return (T)Convert.ChangeType (input, typeof (T));
                }
                catch
                {
                    // return default value in case of a failed convertion
                }
            }
            return defaultValue;
        }
    }
}