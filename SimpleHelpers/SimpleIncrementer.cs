using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace SimpleHelpers
{
    public class SimpleIncrementer
    {
        // Query statistics
        private readonly ConcurrentDictionary<string, IncrementerElement> m_statsMap = new ConcurrentDictionary<string, IncrementerElement> (StringComparer.Ordinal);

        /// <summary>
        /// Gets the count.
        /// </summary>
        /// <value>The count.</value>
        public int Count
        {
            get { return m_statsMap.Count; }
        }

        /// <summary>
        /// Gets the statistics.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IncrementerElement> Values
        {
            get { return m_statsMap.Values; }
        }

        /// <summary>
        /// Gets or sets the <see cref="string" /> with the specified key.
        /// </summary>
        /// <value></value>
        public IncrementerElement this[string Key]
        {
            get { return Get (Key); }
        }

        /// <summary>
        /// Clears this instance.
        /// </summary>
        public void Clear ()
        {
            m_statsMap.Clear ();
        }

        /// <summary>
        /// Tries to remove an incrementer value.
        /// </summary>
        /// <param name="key">Name of the value.</param>
        /// <returns>The current value of the removed key</returns>
        public Int64 TryRemove (string key)
        {
            IncrementerElement obj;
            if (m_statsMap.TryRemove (key, out obj))
                return obj.Count;
            return 0;
        }

        /// <summary>
        /// Increments the specified key value.
        /// </summary>
        /// <param name="key">Name of the statistic.</param>
        /// <param name="upperBound">The upper bound.</param>
        /// /// <returns>The current value of the removed key</returns>
        public Int64 Increment (string key, Int64 upperBound = Int64.MaxValue)
        {
            var i = Get (key);
            var v = i.Increment ();
            if (v > upperBound)
                return i.SetValue (upperBound);
            return v;
        }

        /// <summary>
        /// Decrements the specified key value.
        /// </summary>
        /// <param name="key">Name of the statistic.</param>
        /// <param name="lowerBound">The lower bound.</param>
        public Int64 Decrement (string key, Int64 lowerBound = Int64.MinValue)
        {
            var i = Get (key);
            var v = i.Decrement ();
            if (v < lowerBound)
                return i.SetValue (lowerBound);
            return v;
        }

        /// <summary>
        /// Adds a value to the specified key value.
        /// </summary>
        /// <param name="key">Name of the statistic.</param>
        /// <param name="value">The value.</param>
        /// <param name="upperBound">The upper bound.</param>
        public Int64 Add (string key, Int64 value, Int64 upperBound = Int64.MaxValue)
        {
            var i = Get (key);
            var v = i.Add (value);
            if (v > upperBound)
                return i.SetValue (upperBound);
            return v;
        }

        /// <summary>
        /// Subtracts the specified key value.
        /// </summary>
        /// <param name="key">Name of the statistic.</param>
        /// <param name="value">The value.</param>
        /// <param name="lowerBound">The lower bound.</param>
        public Int64 Subtract (string key, Int64 value, Int64 lowerBound = Int64.MinValue)
        {
            var i = Get (key);
            var v = i.Subtract (value);
            if (v < lowerBound)
                return i.SetValue (lowerBound);
            return v;
        }

        /// <summary>
        /// Gets the element.
        /// </summary>
        /// <param name="key">Name of the statistic.</param>
        /// <returns></returns>
        public IncrementerElement Get (string key)
        {
            IncrementerElement obj = null;
            // try to get the desired statistic
            if (!m_statsMap.TryGetValue (key, out obj))
            {
                // create if not exist and try to add atomically
                m_statsMap.TryAdd (key, new IncrementerElement (key));
                // try get the current object
                obj = m_statsMap[key];
            }
            return obj;
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="key">Name of the statistic.</param>
        /// <returns></returns>
        public Int64 GetValue (string key)
        {
            return Get (key).Count;
        }
    }

    /// <summary>
    /// Item of ConcurrentStatistic object
    /// </summary>
    public class IncrementerElement
    {
        private readonly string m_name;
        private Int64 m_count;

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get { return m_name; }
        }

        /// <summary>
        /// Gets the current count.
        /// </summary>
        /// <value>The count.</value>
        public Int64 Count
        {
            get { return m_count; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IncrementerElement" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        public IncrementerElement (string name)
        {
            m_name = name;
            m_count = 0;
        }

        /// <summary>
        /// Resets the count to '0'.
        /// </summary>
        public void Clear ()
        {
            Interlocked.Exchange (ref m_count, 0);
        }

        /// <summary>
        /// Sets the value as an atomic operation.
        /// </summary>
        /// <param name="value">The value.</param>
        public Int64 SetValue (Int64 value)
        {
            Interlocked.Exchange (ref m_count, value);
            return value;
        }

        /// <summary>
        /// Increments the value as an atomic operation.
        /// </summary>
        /// <returns></returns>
        public Int64 Increment ()
        {
            return Interlocked.Increment (ref m_count);
        }

        /// <summary>
        /// Decrements the value as an atomic operation.
        /// </summary>
        /// <returns></returns>
        public Int64 Decrement ()
        {
            return Interlocked.Decrement (ref m_count);
        }

        /// <summary>
        /// Adds the specified value as an atomic operation.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>the result value</returns>
        public Int64 Add (Int64 value)
        {
            return Interlocked.Add (ref m_count, value);
        }

        /// <summary>
        /// Subtracts the specified value as an atomic operation.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>the result value</returns>
        public Int64 Subtract (Int64 value)
        {
            return Interlocked.Add (ref m_count, value * -1);
        }
    }
}
