using System;
using System.Collections.Generic;

namespace SimpleHelpers.SQLite
{
    // http://stackoverflow.com/questions/233711/add-property-to-anonymous-type-after-creation
    // http://stackoverflow.com/questions/7595416/convert-dictionarystring-object-to-anonymous-object
    public static class ObjectExtensions
    {
        public static IDictionary<string, object> AddProperty (this object obj, string name, object value)
        {
            var dictionary = obj.ParseToDictionary ();
            dictionary.Add (name, value);
            return dictionary;
        }

        public static Dictionary<string, object> ParseToDictionary (this object obj)
        {
            System.ComponentModel.PropertyDescriptorCollection properties = System.ComponentModel.TypeDescriptor.GetProperties (obj);
            Dictionary<string, object> result = new Dictionary<string, object> (properties.Count + 1, StringComparer.Ordinal);
            foreach (System.ComponentModel.PropertyDescriptor property in properties)
            {
                result.Add (property.Name, property.GetValue (obj));
            }
            return result;
        }

        public static List<KeyValuePair<string, object>> ParseToList (this object obj)
        {
            System.ComponentModel.PropertyDescriptorCollection properties = System.ComponentModel.TypeDescriptor.GetProperties (obj);
            List<KeyValuePair<string, object>> result = new List<KeyValuePair<string, object>> (properties.Count);
            foreach (System.ComponentModel.PropertyDescriptor property in properties)
            {
                result.Add (new KeyValuePair<string, object> (property.Name, property.GetValue (obj)));
            }
            return result;
        }

        public static object ToAnonymousType (this IEnumerable<KeyValuePair<string, object>> dict)
        {
            var eo = new System.Dynamic.ExpandoObject ();
            var eoColl = (ICollection<KeyValuePair<string, object>>)eo;
            foreach (KeyValuePair<string, object> kvp in dict)
            {
                eoColl.Add (kvp);
            }
            return (dynamic)eo;
        }
    }
}