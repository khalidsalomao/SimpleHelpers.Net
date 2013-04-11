using System;

namespace SimpleHelpers
{
    public class NamedLock : IDisposable
    {
        private static readonly System.Collections.Concurrent.ConcurrentDictionary <string, CountedLock> m_waitLock = new System.Collections.Concurrent.ConcurrentDictionary<string, CountedLock> (StringComparer.Ordinal);

        private static object GetOrAdd (string key)
        {
            CountedLock padlock = m_waitLock.GetOrAdd (key, LockFactory);
            padlock.Increment ();
            return padlock;
        }

        private static void ReleaseOrRemove (string key)
        {
            CountedLock padlock;
            if (m_waitLock.TryGetValue (key, out padlock))
            {
                if (padlock.Decrement () <= 0)
                    m_waitLock.TryRemove (key, out padlock);
            }
        }

        private static CountedLock LockFactory (string key)
        {
            return new CountedLock ();
        }

        class CountedLock
        {
            int m_counter = 0;

            public int Increment ()
            {
                return System.Threading.Interlocked.Increment (ref m_counter);
            }

            public int Decrement ()
            {
                return System.Threading.Interlocked.Decrement (ref m_counter);
            }
        }

        string m_key;
        object m_padlock;
        volatile bool m_locked = false;

        public bool IsLocked
        {
            get { return m_locked; }
        }

        public string Key
        {
            get { return m_key; }
        }

        public object Lock
        {
            get { return m_padlock; }
        }

        public NamedLock (string key)
        {
            m_key = key;
            m_padlock = GetOrAdd (m_key);
        }

        public void Dispose ()
        {
            Exit ();
            ReleaseOrRemove (m_key);
        }

        public bool Enter ()
        {
            if (!m_locked)
            {
                System.Threading.Monitor.Enter (m_padlock, ref m_locked);
            }
            return m_locked;
        }

        public bool Enter (int waitTimeoutMilliseconds)
        {
            if (!m_locked)
            {
                System.Threading.Monitor.TryEnter (m_padlock, waitTimeoutMilliseconds, ref m_locked);
            }
            return m_locked;
        }

        public bool Enter (TimeSpan waitTimeout)
        {
            return Enter ((int)waitTimeout.TotalMilliseconds);
        }

        public bool Exit ()
        {
            if (m_locked)
            {
                m_locked = false;
                System.Threading.Monitor.Exit (m_padlock);                
            }
            return false;
        }

        public static NamedLock CreateAndEnter (string key)
        {
            NamedLock item;
            item = new NamedLock (key);
            item.Enter ();
            return item;
        }

        public static NamedLock CreateAndEnter (string key, int waitTimeoutMilliseconds)
        {
            NamedLock item;
            item = new NamedLock (key);
            item.Enter (waitTimeoutMilliseconds);
            return item;
        }

        public static NamedLock CreateAndEnter (string key, TimeSpan waitTimeout)
        {
            return CreateAndEnter (key, (int)waitTimeout.TotalMilliseconds);
        }
    }
}