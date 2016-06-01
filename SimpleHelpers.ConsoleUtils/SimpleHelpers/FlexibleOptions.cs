#region *   License     *
/*
    SimpleHelpers - FlexibleOptions   

    Copyright © 2015 Khalid Salomão

    Permission is hereby granted, free of charge, to any person
    obtaining a copy of this software and associated documentation
    files (the “Software”), to deal in the Software without
    restriction, including without limitation the rights to use,
    copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the
    Software is furnished to do so, subject to the following
    conditions:

    The above copyright notice and this permission notice shall be
    included in all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND,
    EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
    OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
    NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
    HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
    WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
    FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
    OTHER DEALINGS IN THE SOFTWARE. 

    License: http://www.opensource.org/licenses/mit-license.php
    Website: https://github.com/khalidsalomao/SimpleHelpers.Net
 */
#endregion

using System;
using System.Collections.Generic;

namespace SimpleHelpers
{
    public class FlexibleOptions
    {
        private bool _caseInsensitive = true;
        private Dictionary<string, string> _options;

        /// <summary>
        /// Internal dictionary with all options.
        /// </summary>
        public Dictionary<string, string> Options
        {
            get
            {
                if (_options == null)
                    _options = new Dictionary<string, string> (StringComparer.OrdinalIgnoreCase);
                return _options;
            }
            set { _options = value; }
        }

        /// <summary>
        /// Gets or sets the <see cref="string" /> with the specified key.
        /// </summary>
        public string this[string key]
        {
            get { return Get (key); }
            set { Set (key, value); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FlexibleOptions" /> class.
        /// </summary>
        public FlexibleOptions ()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FlexibleOptions" /> class.
        /// </summary>
        /// <param name="caseInsensitive">If the intenal dictionary should have case insensitive keys.</param>
        public FlexibleOptions (bool caseInsensitive)
        {
            ChangeStringComparer (caseInsensitive);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FlexibleOptions" /> class.
        /// </summary>
        /// <param name="instance">Another FlexibleObject instance to be cloned.</param>
        /// <param name="caseInsensitive">If the intenal dictionary should have case insensitive keys.</param>
        public FlexibleOptions (FlexibleOptions instance, bool caseInsensitive)
        {
            _caseInsensitive = caseInsensitive;
            _options = new Dictionary<string, string> (instance.Options, _caseInsensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        }
 
        private void ChangeStringComparer (bool caseInsensitive)
        {
            if (_caseInsensitive != caseInsensitive)
            {
                _caseInsensitive = caseInsensitive;
                if (_options != null)
                {
                    // rebuild internal data structure
                    _options = new Dictionary<string, string> (_options, _caseInsensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
                }
            }
        }

        /// <summary>
        /// Check if a key exists.
        /// </summary>
        /// <param name="key">The associated key, which is case insensitive.</param>
        /// <returns></returns>
        public bool HasOption (string key)
        {
            return Options.ContainsKey (key);
        }

        /// <summary>
        /// Add/Overwrite and option. The value will be converted or serialized to string.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">The associated key, which is case insensitive.</param>
        /// <param name="value">The data to be stored.</param>
        /// <returns>This instance</returns>
        public FlexibleOptions Set<T> (string key, T value)
        {
            if (key != null)
            {
                var desiredType = typeof (T);
                // lets check for null value
                if (value as object == null)
                {
                    Options[key] = null;
                }
                // we store data as string, so lets test if we have a string
                else if (desiredType == typeof (string))
                {
                    Options[key] = (string)(object)value;
                }
                // all primitive types are IConvertible, 
                // and if the type implements this interface lets use it (should be faster than serialization)!
                else if (typeof (IConvertible).IsAssignableFrom (desiredType))
                {
                    Options[key] = (string)Convert.ChangeType (value, typeof (string), System.Globalization.CultureInfo.InvariantCulture);
                }
                // finally we fallback to json serialization
                else
                {
                    Options[key] = Newtonsoft.Json.JsonConvert.SerializeObject (value);
                }
            }
            return this;
        }

        /// <summary>
        /// Get the option as string. If the key doen't exist, String.Empty is returned.
        /// </summary>
        /// <param name="key">The key, which is case insensitive.</param>
        /// <remarks>
        /// Since data is stored internally as string, the data will be returned directly.
        /// If the desired type is String, a conversion will only happen if the string has quotation marks, in which case json deserialization will take place.
        /// </remarks>
        /// <returns>The data as string or String.Empty.</returns>
        public string Get (string key)
        {
            return Get<string> (key, String.Empty);
        }

        /// <summary>
        /// Get the option as the desired type.<para/>
        /// If the key doen't exist or the type convertion fails, the provided defaultValue is returned.<para/>
        /// The type convertion uses the Json.Net serialization to try to convert.
        /// </summary>
        /// <typeparam name="T">The desired type to return the data.</typeparam>
        /// <param name="key">The key, which is case insensitive.</param>
        /// <param name="defaultValue">The default value to be used if the data doesn't exists or the type conversion fails.</param>
        /// <remarks>
        /// Since data is stored internally as string, the data will be returned directly.
        /// If the desired type is String, a conversion will only happen if the string has quotation marks, in which case json deserialization will take place.
        /// </remarks>
        /// <returns>The data converted to the desired type or the provided defaultValue.</returns>
        public T Get<T> (string key, T defaultValue)
        {
            return Get<T> (key, defaultValue, false);
        }

        /// <summary>
        /// Get the option as the desired type.<para/>
        /// If the key doen't exist or the type convertion fails, the provided defaultValue is returned.<para/>
        /// The type convertion uses the Json.Net serialization to try to convert.
        /// </summary>
        /// <typeparam name="T">The desired type to return the data.</typeparam>
        /// <param name="key">The key, which is case insensitive.</param>
        /// <param name="defaultValue">The default value to be used if the data doesn't exists or the type conversion fails.</param>
        /// <param name="preserveQuotes">The preserve quotes in case of string.</param>
        /// <remarks>
        /// Since data is stored internally as string, the data will be returned directly.
        /// If the desired type is String, a conversion will only happen if the string has quotation marks, in which case json deserialization will take place.
        /// </remarks>
        /// <returns>The data converted to the desired type or the provided defaultValue.</returns>
        public T Get<T> (string key, T defaultValue, bool preserveQuotes)
        {
            string v;
            if (key != null && Options.TryGetValue (key, out v))
            {
                try
                {
                    if (v == null || v.Length == 0)
                        return defaultValue;

                    bool missingQuotes = v.Length < 2 || (!(v[0] == '\"' && v[v.Length - 1] == '\"'));
                    var desiredType = typeof (T);

                    if (desiredType == typeof (string))
                    {
                        if (missingQuotes || preserveQuotes)
                            return (T)(object)v;
                        // let's deserialize to also unscape the string
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<T> (v);
                    }
                    // more comprehensive datetime parser, except formats like "\"\\/Date(1335205592410-0500)\\/\""
                    // DateTime is tested prior to IConvertible, since it also implements IConvertible
                    else if (desiredType == typeof (DateTime) || desiredType == typeof (DateTime?))
                    {
                        DateTime dt;
                        var vDt = missingQuotes ? v : v.Substring (1, v.Length - 2);
                        if (DateTime.TryParse (vDt, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out dt))
                            return (T)(object)dt;
                        if (vDt.Length == 8 && DateTime.TryParseExact (vDt, "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out dt))
                            return (T)(object)dt;
                        // if previous convertion attempts didn't work, fallback to json deserialization
                    }
                    // let's deal with enums
                    else if (desiredType.IsEnum)
                    {
                        return (T)Enum.Parse (desiredType, v, true);
                    }
                    // all primitive types are IConvertible, 
                    // and if the type implements this interface lets use it!
                    else if (typeof (IConvertible).IsAssignableFrom (desiredType))
                    {
                        if (!missingQuotes)
                            v = v.Substring (1, v.Length - 2);
                        // type convertion with InvariantCulture (faster)
                        return (T)Convert.ChangeType (v, desiredType, System.Globalization.CultureInfo.InvariantCulture);
                    }
                    // Guid doesn't implement IConvertible
                    else if (desiredType == typeof (Guid) || desiredType == typeof (Guid?))
                    {
                        Guid guid;
                        if (Guid.TryParse (v, out guid))
                            return (T)(object)guid;
                    }
                    // TimeSpan doesn't implement IConvertible
                    else if (desiredType == typeof (TimeSpan) || desiredType == typeof (TimeSpan?))
                    {
                        TimeSpan timespan;
                        if (TimeSpan.TryParse (v, out timespan))
                            return (T)(object)timespan;
                    }

                    // finally, deserialize it!                   
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<T> (v);
                }
                catch { /* ignore and return default value */ }
            }
            return defaultValue;
        }

        static char[] defaultDelimiter = new char[] { ',' };

        /// <summary>
        /// Get the option as an array of strings. If the key doen't exist, a zero length array is returned.
        /// </summary>
        /// <param name="key">The key, which is case insensitive.</param>
        /// <remarks>
        /// If the data is in between [], we will try to deserialize as json, else the string will be splited by the delimiters.
        /// </remarks>
        /// <returns>The data as string[].</returns>
        public string[] GetAsList (string key)
        {
            return GetAsList (key, defaultDelimiter);
        }

        /// <summary>
        /// Get the option as an array of strings. If the key doen't exist, a zero length array is returned.
        /// </summary>
        /// <param name="key">The key, which is case insensitive.</param>
        /// <param name="delimiters">The delimiters.</param>
        /// <remarks>
        /// If the data is in between [], we will try to deserialize as json, else the string will be splited by the delimiters.
        /// </remarks>
        /// <returns>The data as string[].</returns>
        public string[] GetAsList (string key, char[] delimiters)
        {
            var v = Get<string> (key, String.Empty);
            // test javascript array
            var v1 = v.Trim ();
            if (v1.StartsWith ("[") && v1.EndsWith ("]"))
            {
                var r = Get<string[]> (key, null);
                if (r != null)
                    return r;
            }
            return v.Split (delimiters != null && delimiters.Length > 0 ? delimiters : defaultDelimiter, StringSplitOptions.None);
        }

        /// <summary>
        /// Merge together FlexibleOptions instances, the last object in the list has priority in conflict resolution (overwrite).
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public static FlexibleOptions Merge (params FlexibleOptions[] items)
        {
            var merge = new FlexibleOptions ();
            if (items != null)
            {
                foreach (var i in items)
                {
                    if (i == null)
                        continue;
                    foreach (var o in i.Options)
                    {
                        merge.Options[o.Key] = o.Value;
                    }
                }
            }
            return merge;
        }
    }
}
