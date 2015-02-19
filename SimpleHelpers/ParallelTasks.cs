#region *   License     *
/*
    SimpleHelpers - ParallelTasks   

    Copyright © 2015 Khalid Salomão

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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleHelpers
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class ParallelTasks<T> : IDisposable
    {
        private List<Thread> m_threads;
        private BlockingCollection<T> m_tasks;
        private int _maxNumberOfThreads = 0;
        private Action<T> _action;

        /// <summary>
        /// Processes the specified items.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="numberOfThreads">The number of threads.</param>
        /// <param name="action">The action.</param>
        public static void Process (IEnumerable<T> items, int numberOfThreads, Action<T> action)
        {
            Process (items, numberOfThreads, 0, 0, action);
        }

        /// <summary>
        /// Processes the specified items.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="initialNumberOfThreads">The initial number of threads.</param>
        /// <param name="maxNumberOfThreads">The max number of threads.</param>
        /// <param name="action">The action.</param>
        public static void Process (IEnumerable<T> items, int initialNumberOfThreads, int maxNumberOfThreads, Action<T> action)
        {
            Process (items, initialNumberOfThreads, maxNumberOfThreads, 0, action);
        }

        /// <summary>
        /// Processes the specified items.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="initialNumberOfThreads">The initial number of threads.</param>
        /// <param name="maxNumberOfThreads">The max number of threads.</param>
        /// <param name="queueBoundedCapacity">
        /// The queue bounded capacity.<para/>
        /// A negative value will make the queue without upper bound.<para/>
        /// The default value is twice the number of threads.<para/>
        /// </param>
        /// <param name="action">The action.</param>
        public static void Process (IEnumerable<T> items, int initialNumberOfThreads, int maxNumberOfThreads, int queueBoundedCapacity, Action<T> action)
        {
            using (var mgr = new ParallelTasks<T> (initialNumberOfThreads, maxNumberOfThreads, queueBoundedCapacity, action))
            {
                foreach (var i in items)
                    mgr.AddTask (i);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParallelTasks" /> class.
        /// </summary>
        /// <param name="numberOfThreads">The number of threads.</param>
        /// <param name="action">The action.</param>
        public ParallelTasks (int numberOfThreads, Action<T> action)
        {
            if (numberOfThreads < 1)
                throw new ArgumentOutOfRangeException ("concurrencyLevel");

            // create task queue
            Initialize (numberOfThreads, numberOfThreads * 2, action);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParallelTasks" /> class.
        /// </summary>
        /// <param name="initialNumberOfThreads">The initial number of threads.</param>
        /// <param name="maxNumberOfThreads">The max number of threads.</param>
        /// <param name="action">The action.</param>
        public ParallelTasks (int initialNumberOfThreads, int maxNumberOfThreads, Action<T> action)
        {
            if (initialNumberOfThreads < 1)
                throw new ArgumentOutOfRangeException ("concurrencyLevel");
            if (maxNumberOfThreads > initialNumberOfThreads)
                _maxNumberOfThreads = maxNumberOfThreads;

            // create task queue
            Initialize (initialNumberOfThreads, initialNumberOfThreads * 2, action);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParallelTasks" /> class.
        /// </summary>
        /// <param name="initialNumberOfThreads">The initial number of threads.</param>
        /// <param name="maxNumberOfThreads">The max number of threads.</param>
        /// <param name="queueBoundedCapacity">
        /// The queue bounded capacity.<para/>
        /// A negative value will make the queue without upper bound.<para/>
        /// The default value is twice the number of threads.<para/>
        /// </param>
        /// <param name="action">The action.</param>
        public ParallelTasks (int initialNumberOfThreads, int maxNumberOfThreads, int queueBoundedCapacity, Action<T> action)
        {
            if (initialNumberOfThreads < 1)
                throw new ArgumentOutOfRangeException ("concurrencyLevel");
            if (maxNumberOfThreads > initialNumberOfThreads)
                _maxNumberOfThreads = maxNumberOfThreads;

            // create task queue
            Initialize (initialNumberOfThreads, queueBoundedCapacity, action);
        }
 
        private void Initialize (int numberOfThreads, int queueBoundedCapacity, Action<T> action)
        {
            if (queueBoundedCapacity == 0)
                queueBoundedCapacity = numberOfThreads * 2;
            _action = action;
            // create task queue
            m_tasks = (queueBoundedCapacity > 0) ? new BlockingCollection<T> (queueBoundedCapacity) : new BlockingCollection<T>();

            // create all threads
            m_threads = new List<Thread> (numberOfThreads);
            CreateThreads (numberOfThreads, action);
        }
 
        private void CreateThreads (int numberOfThreads, Action<T> action)
        {
            for (var i = 0; i < numberOfThreads; i++)
            {
                var thread = new Thread (() =>
                {
                    foreach (var t in m_tasks.GetConsumingEnumerable ())
                    {
                        action (t);
                    }
                });
                thread.IsBackground = true;
                lock (m_threads) 
                    m_threads.Add (thread);

                // start all threads
                thread.Start ();
            }
        }

        /// <summary>
        /// Adds the task.<para/>
        /// This method is thread safe.
        /// </summary>
        /// <param name="task">The task.</param>
        /// <remarks>
        /// This method is thread safe.<para/>
        /// Any call to Add may block until space is available to store the provided item in the processing queue.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">If this instance is Disposed, any subsequent call may raise this exception.</exception>
        /// <exception cref="InvalidOperationException">If this instance is Disposed, any subsequent call may raise this exception.</exception>
        public void AddTask (T task)
        {
            m_tasks.Add (task);
            if (_maxNumberOfThreads > 0 && m_threads.Count < _maxNumberOfThreads && m_tasks.Count > 1)
                CreateThreads (1, _action);
        }

        /// <summary>
        /// Gets the queue count.
        /// </summary>
        /// <value>The queue count.</value>
        public int QueueCount
        {
            get { return m_tasks != null ? m_tasks.Count : 0; }
        }

        /// <summary>
        /// Gets the thread count.
        /// </summary>
        /// <value>The thread count.</value>
        public int ThreadCount
        {
            get { return m_threads != null ? m_threads.Count : 0; }
        }

        /// <summary>
        /// Remove all waiting items from processing queue.
        /// </summary>
        public void Clear ()
        {
            if (m_tasks != null)
            {
                while (m_tasks.Count > 0)
                {
                    T item;
                    m_tasks.TryTake (out item);
                }
            }
        }

        /// <summary>
        /// Closes this instance by waiting all threads to complete processing all waiting items.
        /// </summary>
        private void Close ()
        {
            if (m_tasks != null)
            {
                m_tasks.CompleteAdding ();

                foreach (var thread in m_threads)
                    thread.Join ();
                
                m_tasks.Dispose ();
                m_tasks = null;
                m_threads = null;
            }
        }

        /// <summary>
        /// Closes this instance by waiting all threads to complete processing all waiting items.
        /// </summary>
        public void Dispose ()
        {
            Close ();
            // no need to call dispose again by GC
            GC.SuppressFinalize (this);
        }

        ~ParallelTasks ()
        {
            Close ();
        }
    }
}
