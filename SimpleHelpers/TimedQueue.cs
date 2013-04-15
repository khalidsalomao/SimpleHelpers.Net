#region *   License     *
/*
    SimpleHelpers - TimedQueue   

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
using System.Collections.Generic;
using System.Linq;

namespace SimpleHelpers
{
    /// <summary>
    /// Simple lightweight queue that stores data in a concurrent queue and periodically process the queued items.
    /// Userful for:
    /// * processing items in batches;
    /// * grouping data for later processing;
    /// * async processing (consumer/producer);
    /// * etc.
    /// Note: this nuget package contains c# source code and depends on System.Collections.Concurrent introduced in .Net 4.0.
    /// </summary>    
    public class TimedQueue<T> where T : class
    {
        private static TimeSpan m_timerStep = TimeSpan.FromMilliseconds (1000);

        private static System.Collections.Concurrent.ConcurrentQueue<T> m_queue = new System.Collections.Concurrent.ConcurrentQueue<T> ();

        /// <summary>
        /// Interval duration between OnExecution calls by the internal timer thread.
        /// Default value is 1000 milliseconds.
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