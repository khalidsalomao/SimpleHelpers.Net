using System;
using System.Linq;

namespace SimpleHelpers
{
    /// For updated code: https://gist.github.com/khalidsalomao/4968274
    /// Articles on CodeProject

    /// <summary>
    /// 
    /// </summary>
    public class MemoryCache : MemoryCache<object>
    {
        public static T GetAs<T> (string key) where T : class
        {
            return Get (key) as T;
        }

        public static T RemoveAs<T> (string key) where T : class
        {
            return Remove (key) as T;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class MemoryCache<T> where T : class
    {
        private static readonly System.Collections.Concurrent.ConcurrentDictionary <string, CachedItem> m_cacheMap = new System.Collections.Concurrent.ConcurrentDictionary<string, CachedItem> (StringComparer.Ordinal);

        private static TimeSpan m_timeout = TimeSpan.FromMinutes (5);
        private static TimeSpan m_maintenanceStep = TimeSpan.FromMinutes (5);

        /// <summary>
        /// Expiration TimeSpan of stored items
        /// </summary>
        public static TimeSpan Timeout
        {
            get { return m_timeout; }
            set { m_timeout = value; }
        }

        /// <summary>
        /// Interval duration between checks for expired cached items by the internal timer thread.
        /// </summary>
        public static TimeSpan MaintenanceStep
        {
            get { return m_maintenanceStep; }
            set
            {
                if (m_maintenanceStep != value)
                {
                    m_maintenanceStep = value;
                    StopMaintenance ();
                    StartMaintenance ();
                }
            }
        }

        #region *   Events and Event Handlers   *

        public delegate void SimpleMemoryCacheItemExpirationEventHandler (string key, T item);

        public static event SimpleMemoryCacheItemExpirationEventHandler OnExpiration;
        
        private static bool HasEventListeners ()
        {
            if (OnExpiration != null)
            {
            	return OnExpiration.GetInvocationList ().Length != 0;
            }
            return false;
        }

        #endregion

        class CachedItem
        {
            public DateTime Updated;
            public T Data;
        }

        public static T Get (string key)
        {
            CachedItem item;
            if (m_cacheMap.TryGetValue (key, out item))
                return item.Data;
            return null;
        }

        public static void Set (string key, T data)
        {
            if (key == null | key.Length == 0 | data == null)
                throw new System.ArgumentNullException ("key");
            // add or update item
            m_cacheMap[key] = new CachedItem
            {
                Updated = DateTime.UtcNow,
                Data = data
            };
            StartMaintenance ();
        }

        public static T Remove (string key)
        {
            CachedItem item;
            m_cacheMap.TryRemove (key, out item);
            return item.Data;
        }

        public static void Clear ()
        {
            m_cacheMap.Clear ();
        }

        public static T GetOrAdd (string key, Func<string, T> valueFactory)
        {
            if (valueFactory != null)
            {
                CachedItem item;
                if (!m_cacheMap.TryGetValue (key, out item))
                {
                    T data = valueFactory (key);
                    Set (key, data);                    
                    return data;
                }
                else
                {
                    return item.Data;
                }
            }
            else
            {
                throw new System.ArgumentNullException ("valueFactory");
            }
        }

        public static T GetOrSyncAdd (string key, Func<string, T> valueFactory, TimeSpan waitTimeout)
        {
            return GetOrSyncAdd (key, valueFactory, (int)waitTimeout.TotalMilliseconds);
        }

        public static T GetOrSyncAdd (string key, Func<string, T> valueFactory, int waitTimeoutMilliseconds)
        {
            if (valueFactory != null)
            {
                CachedItem item;
                if (!m_cacheMap.TryGetValue (key, out item))
                {
                    // create a lock for this key
                    using (var padlock = new NamedLock (key))
                    {
                        if (padlock.Enter (waitTimeoutMilliseconds))
                        {
                            if (!m_cacheMap.TryGetValue (key, out item))
                            {
                                T data = valueFactory (key);
                                Set (key, data);
                                return data;
                            }
                            else
                            {
                                return item.Data;
                            }
                        }
                        else
                        {
                            return default (T);
                        }
                    }                    
                }
                return item.Data;
            }
            else
            {
                throw new System.ArgumentNullException ("valueFactory");
            }
        }

        #region *   Cache Maintenance Task  *

        private static System.Threading.Timer m_maintenanceTask = null;
        private static readonly object m_lock = new object ();
        private static int m_executing = 0;

        private static void StartMaintenance ()
        {
            if (m_maintenanceTask == null)
            {
                lock (m_lock)
                {
                    if (m_maintenanceTask == null)
                    {
                        m_maintenanceTask = new System.Threading.Timer (ExecuteMaintenance, null, m_maintenanceStep, m_maintenanceStep);
                    }
                }
            }
        }

        private static void StopMaintenance ()
        {
            lock (m_lock)
            {
                if (m_maintenanceTask != null)
                    m_maintenanceTask.Dispose ();
                m_maintenanceTask = null;
            }
        }

        private static void ExecuteMaintenance (object state)
        {
            // check if a step is already executing
            if (System.Threading.Interlocked.CompareExchange (ref m_executing, 1, 0) != 0)
                return;
            // try to fire OnExpiration event
            try
            {
                // stop timed task if queue is empty
                if (m_cacheMap.Count == 0)
                {
                    StopMaintenance ();
                    // check again if the queue is empty
                    if (m_cacheMap.Count != 0)
                        StartMaintenance ();
                }
                else
                {
                    CachedItem item;
                    DateTime oldThreshold = DateTime.UtcNow - m_timeout;
                    bool hasEvents = HasEventListeners ();
                    // select elegible records
                    var expiredItems = m_cacheMap.Where (i => i.Value.Updated < oldThreshold).Select (i => i.Key);
                    // remove from cache and fire OnExpiration event
                    foreach (var key in expiredItems)
                    {
                        m_cacheMap.TryRemove (key, out item);
                        if (hasEvents)
                        {
                            OnExpiration (key, item.Data);
                        }
                    }
                }
            }
            finally
            {
                // release lock
                System.Threading.Interlocked.Exchange (ref m_executing, 0);
            }
        }
        
        #endregion
    }

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
                System.Threading.Monitor.Exit (m_locked);                
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