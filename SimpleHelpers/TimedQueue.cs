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

        public int Count
        {
            get { return m_queue.Count; }
        }

        #region *   Events and Event Handlers   *

        public delegate void SimpleTimedQueueEventHandler (IEnumerable<T> items);

        /// <summary>
        /// Event fired for every timer step.
        /// Note: the IEnumerable must be consumed to clear the queued items.
        /// </summary>
        public SimpleTimedQueueEventHandler OnExecution { get; set; }

        #endregion

        /// <summary>
        /// Puts the specified data in the timed queue.
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
            ExecuteMaintenance (null);
            StopMaintenance ();
        }

        /// <summary>
        /// Flushes the current enqueued events.
        /// </summary>
        public void Dispose () 
        {
            Flush ();
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
                        // simply clear the queue if there is no event listenning
                        Clear ();
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