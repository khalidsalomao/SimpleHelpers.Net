#region *   License     *
/*
    SimpleHelpers - NamedLock   

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

namespace $rootnamespace$.SimpleHelpers
{
    /// <summary>
    /// Synchronization helper: a static lock collection associated with a key.
    /// NamedLock manages the lifetime of critical sections that can be accessed by a key (name) throughout the application. 
    /// It also have some helper methods to allow a maximum wait time (timeout) to aquire the lock and safelly release it.    
    /// Note: this nuget package contains C# source code and depends on System.Collections.Concurrent introduced in .Net 4.0.
    /// </summary>
    /// <example>
    /// // create a lock for this key
    /// using (var padlock = new NamedLock (key))
    /// {
    ///     if (padlock.Enter (TimeSpan.FromMilliseconds (100)))
    ///     {
    ///         // do something
    ///     }
    ///     else
    ///     {
    ///         // do some other thing
    ///     }
    /// }
    /// </example>
    public class NamedLock : IDisposable
    {
        #region *   Internal static methods   *

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
            private int m_counter = 0;

            public int Increment ()
            {
                return System.Threading.Interlocked.Increment (ref m_counter);
            }

            public int Decrement ()
            {
                return System.Threading.Interlocked.Decrement (ref m_counter);
            }
        }

        #endregion

        #region *   Internal variables & properties *

        private string m_key;

        private object m_padlock;

        private bool m_locked = false;
        
        /// <summary>
        /// Check if a lock was aquired.
        /// </summary>
        public bool IsLocked
        {
            get { return m_locked; }
        }

        /// <summary>
        /// Gets the lock key name.
        /// </summary>
        public string Key
        {
            get { return m_key; }
        }

        /// <summary>
        /// Gets the internal lock object.
        /// </summary>
        public object Lock
        {
            get { return m_padlock; }
        }

        #endregion

        #region *   Constructor & finalizer *

        /// <summary>
        /// Initializes a new instance of the <see cref="NamedLock" /> class.
        /// </summary>
        /// <param name="key">The named lock key.</param>
        public NamedLock (string key)
        {
            m_key = key;
            m_padlock = GetOrAdd (m_key);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing,
        /// or resetting unmanaged resources.
        /// Releases aquired lock and related resources.
        /// </summary>
        public void Dispose ()
        {
            Exit ();
            ReleaseOrRemove (m_key);
        }

        #endregion

        #region *   Internal variables & properties *

        /// <summary>
        /// Tries to aquire a lock.
        /// </summary>
        public bool Enter ()
        {
            if (!m_locked)
            {
                System.Threading.Monitor.Enter (m_padlock, ref m_locked);
            }
            return m_locked;
        }

        /// <summary>
        /// Tries to aquire a lock respecting the specified timeout.
        /// </summary>
        /// <param name="waitTimeoutMilliseconds">The wait timeout milliseconds.</param>
        /// <returns>If the lock was aquired in the specified timeout</returns>
        public bool Enter (int waitTimeoutMilliseconds)
        {
            if (!m_locked)
            {
                System.Threading.Monitor.TryEnter (m_padlock, waitTimeoutMilliseconds, ref m_locked);
            }
            return m_locked;
        }

        /// <summary>
        /// Tries to aquire a lock respecting the specified timeout.
        /// </summary>
        /// <param name="waitTimeout">The wait timeout.</param>
        /// <returns>If the lock was aquired in the specified timeout</returns>
        public bool Enter (TimeSpan waitTimeout)
        {
            return Enter ((int)waitTimeout.TotalMilliseconds);
        }

        /// <summary>
        /// Releases the lock if it was already aquired.
        /// Called also at "Dispose".
        /// </summary>
        public bool Exit ()
        {
            if (m_locked)
            {
                m_locked = false;
                System.Threading.Monitor.Exit (m_padlock);                
            }
            return false;
        }

        #endregion

        #region *   Factory methods     *

        /// <summary>
        /// Creates a new instance and tries to aquire a lock.
        /// </summary>
        /// <param name="key">The named lock key.</param>
        public static NamedLock CreateAndEnter (string key)
        {
            NamedLock item;
            item = new NamedLock (key);
            item.Enter ();
            return item;
        }

        /// <summary>
        /// Creates a new instance and tries to aquire a lock.
        /// </summary>
        /// <param name="key">The named lock key.</param>
        /// <param name="waitTimeoutMilliseconds">The wait timeout milliseconds.</param>
        public static NamedLock CreateAndEnter (string key, int waitTimeoutMilliseconds)
        {
            NamedLock item;
            item = new NamedLock (key);
            item.Enter (waitTimeoutMilliseconds);
            return item;
        }

        /// <summary>
        /// Creates a new instance and tries to aquire a lock.
        /// </summary>
        /// <param name="key">The named lock key.</param>
        /// <param name="waitTimeout">The wait timeout.</param>
        public static NamedLock CreateAndEnter (string key, TimeSpan waitTimeout)
        {
            return CreateAndEnter (key, (int)waitTimeout.TotalMilliseconds);
        }

        #endregion
    }
}