using System;
using System.Linq;

namespace SimpleHelpers
{
    /// For updated code: https://gist.github.com/khalidsalomao/4968274
    /// Articles on CodeProject

    public class SimpleObjectPool<T> where T : class
    {
        static System.Collections.Concurrent.ConcurrentStack<T> m_bag = new System.Collections.Concurrent.ConcurrentStack<T> ();
        public static int MaxCapacity = 10;

        public static Func<T> InstanceFactory;

        public static T Get (Func<T> instanceFactory)
        {
            T item;
            if (!m_bag.TryPop (out item))
            {
                return instanceFactory ();
            }
            return item;
        }

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

        public static void Put (T item)
        {
            // add to pool if it is not full
            if (m_bag.Count < MaxCapacity)
            {               
                m_bag.Push (item);
            }
        }

        public static void Clear ()
        {
            m_bag.Clear ();
        }

        public static int Count
        {
            get { return m_bag.Count; }
        }
    }
}