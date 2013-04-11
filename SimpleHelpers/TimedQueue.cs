using System;
using System.Collections.Generic;
using System.Linq;

namespace SimpleHelpers
{
    /// For updated code: https://gist.github.com/khalidsalomao/4977955
    /// Articles on CodeProject

    /// <summary>
    /// TimedQueue stores all data in a concurrent queue and periodically process the queued items.
    /// </summary>    
    public class TimedQueue : TimedQueue<object>
    {
    }

    /// <summary>
    /// TimedQueue stores all data in a concurrent queue and periodically process the queued items.
    /// </summary>
    public class TimedQueue<T> where T : class
    {
        private static TimeSpan m_timerStep = TimeSpan.FromMilliseconds (1000);

        private static System.Collections.Concurrent.ConcurrentQueue<T> m_queue = new System.Collections.Concurrent.ConcurrentQueue<T> ();

        /// <summary>
        /// Interval duration between OnExecution calls by the internal timer thread.
        /// </summary>
        public static TimeSpan TimerStep
        {
            get { return m_timerStep; }
            set
            {
                if (!m_timerStep.Equals (value))
                {
                    m_timerStep = value;
                    StopMaintenance ();
                    StartMaintenance ();
                }
            }
        }

        #region *   Events and Event Handlers   *

        public delegate void SimpleTimedQueueEventHandler (IEnumerable<T> items);

        /// <summary>
        /// Event fired for every timer step.
        /// Note: the IEnumerable must be consumed to clear the queued items.
        /// </summary>
        public static event SimpleTimedQueueEventHandler OnExecution;

        private static int EventListenersCount ()
        {
            if (OnExecution != null)
            {
                return OnExecution.GetInvocationList ().Length;
            }
            return 0;
        }

        #endregion

        /// <summary>
        /// Puts the specified data in the timed queue.
        /// </summary>
        public static void Put (T data)
        {
            if (data == null)
                return;
            m_queue.Enqueue (data);
            StartMaintenance ();
        }

        /// <summary>
        /// Clears this instance.
        /// </summary>
        public static void Clear ()
        {
            System.Threading.Interlocked.Exchange (ref m_queue, new System.Collections.Concurrent.ConcurrentQueue<T> ());
        }

        /// <summary>
        /// Flushes the current queue by firing the event OnExecute.
        /// </summary>
        public static void Flush ()
        {
            ExecuteMaintenance (null);
        }

        #region *   Scheduled Task  *

        private static System.Threading.Timer m_scheduledTask = null;
        private static readonly object m_lock = new object ();
        private static int m_executing = 0;
        private static int m_idleCounter = 0;

        private static void StartMaintenance ()
        {
            if (m_scheduledTask == null)
            {
                lock (m_lock)
                {
                    if (m_scheduledTask == null)
                    {
                        m_scheduledTask = new System.Threading.Timer (ExecuteMaintenance, null, m_timerStep, m_timerStep);
                    }
                }
            }
        }

        private static void StopMaintenance ()
        {
            lock (m_lock)
            {
                if (m_scheduledTask != null)
                    m_scheduledTask.Dispose ();
                m_scheduledTask = null;
                m_takeCache = null;
            }
        }

        private static void ExecuteMaintenance (object state)
        {
            // check if a step is executing
            if (System.Threading.Interlocked.CompareExchange (ref m_executing, 1, 0) != 0)
                return;
            // try to fire OnExecute event
            try
            {
                // check for idle queue
                if (m_queue.Count == 0)
                {
                    // after 3 loops with empty queue, stop timer
                    if (m_idleCounter++ > 2)                    
                        StopMaintenance ();
                }
                else
                {
                    // clear idle queue marker
                    m_idleCounter = 0;
                    // check for the listenners
                    int count = EventListenersCount ();
                    // fire event
                    if (count == 1)
                    {
                        OnExecution (TakeQueuedItems ());
                    }
                    else if (count > 0)
                    {
                        // if we have more than one listenner, then we need to maintain the consistent view of the queue
                        OnExecution (TakeQueuedItemsAsList ());
                    }
                    else
                    {
                        // simply clear the queue if there is no event listenning
                        Clear ();
                    }
                }
            }
            finally
            {
                System.Threading.Interlocked.Exchange (ref m_executing, 0);
            }
        }

        private static IEnumerable<T> TakeQueuedItems ()
        {
            T obj;
            while (m_queue.TryDequeue (out obj))
                yield return obj;
        }

        static List<T> m_takeCache = null;

        private static IEnumerable<T> TakeQueuedItemsAsList ()
        {
            // check 
            if (m_takeCache != null)
                m_takeCache = new List<T> ();
            else
                m_takeCache.Clear ();
            T obj;
            while (m_queue.TryDequeue (out obj))
                m_takeCache.Add (obj);
            return m_takeCache;
        }
        #endregion
    }
}