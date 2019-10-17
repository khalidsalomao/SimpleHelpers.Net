﻿#region *   License     *
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

namespace $rootnamespace$.SimpleHelpers
{

    /// <summary>
    /// TimedQueueManager is a helper class that stores instances of TimedQueue by a key.
    /// </summary>
    /// <typeparam name="T">The type used on the TimedQueue.</typeparam>
    public class TimedQueueManager<T> where T : class
    {
        private static System.Collections.Concurrent.ConcurrentDictionary<string, TimedQueue<T>> m_map = new System.Collections.Concurrent.ConcurrentDictionary<string, TimedQueue<T>> (StringComparer.Ordinal);

        /// <summary>
        /// Gets all registered queue names.
        /// </summary>
        public static IEnumerable<string> Queues
        {
            get { return m_map.Keys; }
        }

        /// <summary>
        /// Configures a stored TimedQueue by its key.<para/>
        /// Note: if the queue does not exists, it will be created.
        /// </summary>
        /// <param name="key">The TimedQueue associated key.</param>
        /// <param name="timerStep">The interval between OnExecution calls by the internal timer thread.</param>
        /// <param name="action">The OnExecution action fired for every timer step.</param>
        /// <returns></returns>
        public static TimedQueue<T> Configure (string key, TimeSpan timerStep, Action<IEnumerable<T>> action)
        {
            TimedQueue<T> q = Get(key);
            q.TimerStep = timerStep;
            q.OnExecution = action;
            return q;
        }

        /// <summary>
        /// Gets a stored TimedQueue by its key.<para/>
        /// Note: if the queue does not exists, it will be created.
        /// </summary>
        /// <param name="key">The TimedQueue associated key.</param>
        public static TimedQueue<T> Get (string key)
        {
            TimedQueue<T> q;
            if (!m_map.TryGetValue (key, out q))
            {
                q = new TimedQueue<T> ();
                m_map[key] = q;
            }
            return q;
        }

        /// <summary>
        /// Removes the specified TimedQueue by key.<para/>
        /// Note: the TimedQueue will safely be removed and disposed. 
        /// If the queue was already removed, this will be a NOP.
        /// </summary>
        /// <param name="key">The TimedQueue associated key.</param>
        public static void Remove (string key)
        {
            TimedQueue<T> q;
            if (!m_map.TryRemove (key, out q))
            {
                q.Dispose ();
            }
        }

        /// <summary>
        /// Removes and dispose of all TimedQueues.
        /// Note: the TimedQueue will safely be removed and disposed. 
        /// </summary>
        public static void Clear ()
        {
            foreach (var q in m_map.ToList ())
                Remove (q.Key);
        }
    }

    /// <summary>
    /// Simple lightweight queue that stores data in a concurrent queue and periodically process the queued items.
    /// Userful for:
    /// * processing items in batches;
    /// * grouping data for later processing;
    /// * async processing (consumer/producer);
    /// * etc.
    /// Note: this nuget package contains C# source code and depends on System.Collections.Concurrent introduced in .Net 4.0.
    /// </summary>    
    public class TimedQueue<T> : IDisposable where T : class
    {
        private TimeSpan m_timerStep = TimeSpan.FromMilliseconds (1000);

        private System.Collections.Concurrent.ConcurrentQueue<T> m_queue = new System.Collections.Concurrent.ConcurrentQueue<T> ();

        /// <summary>
        /// Interval duration between OnExecution calls by the internal timer thread.
        /// Default value is 1000 milliseconds.
        /// </summary>
        public TimeSpan TimerStep
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

        /// <summary>
        /// Gets the number of queued items.
        /// </summary>
        public int Count
        {
            get { return m_queue.Count; }
        }

        /// <summary>
        /// Event fired for every timer step.
        /// Note: the IEnumerable must be consumed to clear the queued items.
        /// </summary>
        public Action<IEnumerable<T>> OnExecution { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimedQueue" /> class.
        /// </summary>
        public TimedQueue ()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimedQueue" /> class.
        /// </summary>
        /// <param name="timerStep">The interval between OnExecution calls by the internal timer thread.</param>
        /// <param name="action">The OnExecution action fired for every timer step.</param>
        public TimedQueue (TimeSpan timerStep, Action<IEnumerable<T>> action)
        {
            TimerStep = timerStep;
            OnExecution = action;
        }

        /// <summary>
        /// Puts the specified data in the timed queue for processing.
        /// </summary>
        public void Put (T data)
        {
            if (data == null)
                return;
            m_queue.Enqueue (data);
            StartMaintenance ();
        }

        /// <summary>
        /// Clears this instance.
        /// </summary>
        public void Clear ()
        {
            System.Threading.Interlocked.Exchange (ref m_queue, new System.Collections.Concurrent.ConcurrentQueue<T> ());
        }

        /// <summary>
        /// Flushes the current queue by firing the event OnExecute.
        /// </summary>
        public void Flush ()
        {
            StopMaintenance ();
            ExecuteMaintenance (null);            
        }

        /// <summary>
        /// Flushes the current enqueued events.
        /// </summary>
        public void Dispose () 
        {
            Flush ();
            StopMaintenance ();
        }

        #region *   Scheduled Task  *

        private System.Threading.Timer m_scheduledTask = null;
        private readonly object m_lock = new object ();
        private int m_executing = 0;
        private int m_idleCounter = 0;

        private void StartMaintenance ()
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

        private void StopMaintenance ()
        {
            lock (m_lock)
            {
                if (m_scheduledTask != null)
                    m_scheduledTask.Dispose ();
                m_scheduledTask = null;
            }
        }

        private void ExecuteMaintenance (object state)
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
                    // fire event
                    if (OnExecution != null)
                    {
                        // clear idle queue marker
                        m_idleCounter = 0;

                        // execute event handler
                        OnExecution (TakeQueuedItems ());    
                    }                    
                    else
                    {
                        // simply stop the queue if there is no event listening
                        StopMaintenance ();
                    }
                }
            }
            finally
            {
                System.Threading.Interlocked.Exchange (ref m_executing, 0);
            }
        }

        private IEnumerable<T> TakeQueuedItems ()
        {
            T obj;
            while (m_queue.TryDequeue (out obj))
                yield return obj;
        }

        #endregion
    }
}