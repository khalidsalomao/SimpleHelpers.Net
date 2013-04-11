using System;
using System.Linq;

namespace SimpleHelpers
{
    /// For updated code: https://gist.github.com/khalidsalomao/4968274
    /// Articles on CodeProject

    public class ObjectPool<T> where T : class
    {
        static System.Collections.Concurrent.ConcurrentStack<T> m_bag = new System.Collections.Concurrent.ConcurrentStack<T> ();
        
        public static int MaxCapacity = 10;

        public static Func<T> InstanceFactory;

        /// <summary>
        /// Gets the specified instance factory.
        /// </summary>
        /// <param name="instanceFactory">The instance factory.</param>
        /// <returns></returns>
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
        /// Gets this instance.
        /// </summary>
        /// <returns></returns>
        public static T Get ()
        {
            T item;
            if (!m_bag.TryPop (out item))
            {
                if (InstanceFactory == null)
                    return null;
                return InstanceFactory ();
            }
            return item;
        }

        /// <summary>
        /// Puts the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        public static void Put (T item)
        {
            // add to pool if it is not full
            if (m_bag.Count < MaxCapacity)
            {               
                m_bag.Push (item);
            }
        }

        /// <summary>
        /// Clears this instance.
        /// </summary>
        public static void Clear ()
        {
            m_bag.Clear ();
        }

        /// <summary>
        /// Gets the count.
        /// </summary>
        /// <value>The count.</value>
        public static int Count
        {
            get { return m_bag.Count; }
        }
    }
}