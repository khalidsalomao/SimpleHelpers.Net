#region *   License     *
/*
    SimpleHelpers - ObjectPool   

    Copyright © 2013 Khalid Salomão

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

namespace SimpleHelpers
{
    /// <summary>
    /// A simple lightweight object pool for fast and simple object reuse.
    /// Fast lightweight thread-safe object pool for objects that are expensive to create or could efficiently be reused.
    /// Note: this nuget package contains c# source code and depends on System.Collections.Concurrent introduced in .Net 4.0.
    /// </summary>
    public class ObjectPool<T> where T : class
    {
        static System.Collections.Concurrent.ConcurrentStack<T> m_bag = new System.Collections.Concurrent.ConcurrentStack<T> ();
        
        /// <summary>
        /// Maximum capactity of the pool, used in Put to discards objects if the capacity was reached.
        /// </summary>
        public static int MaxCapacity = 10;

        /// <summary>
        /// Default instance factory method, used to create new instances if the pool is empty.
        /// </summary>
        public static Func<T> DefaultInstanceFactory = null;

        /// <summary>
        /// Removes a stored object from the pool and return it.
        /// If the pool is empty, instanceFactory will be called to generate a new object.
        /// </summary>
        /// <param name="instanceFactory">The instance factory method used to create a new instance if pool is empty.</param>
        public static T Get (Func<T> instanceFactory)
        {
            T item;
            if (!m_bag.TryPop (out item))
            {
                return instanceFactory ();
            }
            return item;
        }

        /// <summary>
        /// Removes a stored object from the pool and return it.
        /// If the pool is empty and a 'DefaultInstanceFactory' was provided, 
        /// then 'DefaultInstanceFactory' will be called to generate a new object,
        /// otherwise null is returned.
        /// </summary>
        public static T Get ()
        {
            T item;
            if (!m_bag.TryPop (out item))
            {
                if (DefaultInstanceFactory == null)
                    return null;
                return DefaultInstanceFactory ();
            }
            return item;
        }

        /// <summary>
        /// Puts the specified item in the pool.
        /// Is the 'MaxCapacity' has been reached the item is ignored.
        /// </summary>
        public static void Put (T item)
        {
            // add to pool if it is not full
            if (m_bag.Count < MaxCapacity)
            {               
                m_bag.Push (item);
            }
        }

        /// <summary>
        /// Clears this instance by removing all stored items.
        /// </summary>
        public static void Clear ()
        {
            m_bag.Clear ();
        }

        /// <summary>
        /// Gets the number of objects in the pool.
        /// </summary>
        /// <value>The count.</value>
        public static int Count
        {
            get { return m_bag.Count; }
        }
    }
}