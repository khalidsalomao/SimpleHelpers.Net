/********************************************************************
 *	author:		Khalid Salomão
 *  e-mail:     khalidsalomao@gmail.com
*********************************************************************/
using System;

namespace AudioParserLib.SimpleHelpers
{
    /* Specialized PQ for Pathfiding */
    public class PriorityQueue<T>
    {
        #region **  internal variables  **
        
        private T[] m_nodes = null;
        int m_count = 0;
        int m_size = 0;
        static readonly T m_defaultValue = default (T);

        #endregion

        public Func<T, T, int> ComparisonMethod = null;

        static int DefaultSimpleCompare (T p1, T p2)
        {            
            return String.CompareOrdinal (p1.ToString (), p2.ToString ());
        }

        static int DefaultCompareTo (T p1, T p2)
        {
            return ((IComparable)p1).CompareTo (p2);
        }

        /* Methods */
        #region Constructors

        public PriorityQueue () :
        this (6)
        {
        }

        public PriorityQueue (int Size)
        {
            m_size = Size;
            if (m_size < 4)
                m_size = 4;
            m_nodes = new T[m_size];
            // set default comparison method
            if (typeof (IComparable).IsAssignableFrom (typeof (T)))
                ComparisonMethod = DefaultCompareTo;
            else
                ComparisonMethod = DefaultSimpleCompare;
        }

        #endregion

        public int Count
        {
            get { return m_count; }
        }

        public int Capacity
        {
            get { return m_size; }
            set { resizeHeap (value); }
        }

        // Method Insert will add an element to heap; O(log n)
        public void Add (T data)
        {
            if (m_count >= m_size)
                resizeHeap (m_size * 2);
            //insert at the end
            m_nodes[m_count] = data;
            // Rearrange heap and increment counter
            shiftUp (m_count++);
        }

        void resizeHeap (int Size)
        {
            if (Size > m_size)
            {
                Array.Resize (ref m_nodes, Size);
                m_size = Size;
            }
        }

        // Method DeleteMin will remove the element with minimum value from heap and return it; O(log n)
        public T DeleteMin ()
        {
            if (m_count > 0)
            {
                //get element and remove it from vector
                var result = m_nodes[0];
                m_nodes[0] = m_defaultValue;
                --m_count;
                // check heap 
                if (m_count > 0)
                {
                    //put last element at removed node position
                    m_nodes[0] = m_nodes[m_count];
                    //remove node
                    m_nodes[m_count] = m_defaultValue;
                    //maintain heap integrity
                    shiftDown (0);
                }
                return result;
            }
            return m_defaultValue;
        }

        // Method Min will return the element with minimum value from heap; O(1)
        public T Min ()
        {
            if (m_count == 0)
                return m_defaultValue;
            return m_nodes[0];
        }

        public T GetSecond ()
        {
            if (m_count == 0)
                return m_defaultValue;
            var result = m_nodes[1];
            /* choose min data in heap of the following next two items */
            if (m_count > 1)
            {
                if (ComparisonMethod (m_nodes[2], result) < 0)
                    return m_nodes[2];
            }
            return result;
        }

        public T GetAt (int index)
        {
            if (index < m_count)
            {
                return m_nodes[index];
            }
            return m_defaultValue;
        }

        // Method Delete will remove the element at index from heap and return it; O(log n)
        public T Delete (int index)
        {
            if (index < m_count)
            {
                //get element and remove it from vector
                var result = m_nodes[index];
                --m_count;
                if (index < m_count)
                {
                    //put last element at removed node position
                    m_nodes[index] = m_nodes[m_count];
                }
                //remove node
                m_nodes[m_count] = m_defaultValue;

                //maintain heap integrity
                if (m_count > 0)
                {
                    if (ComparisonMethod (m_nodes[index], result) < 0)
                        shiftUp (index);
                    else
                        shiftDown (index);
                }
                return result;
            }
            return m_defaultValue;
        }

        // Method Clear will remove all elements from heap	
        public void Clear ()
        {
            for (int i = 0; i < m_count; ++i)
            {
                m_nodes[i] = m_defaultValue;
            }
            m_count = 0;
        }

        public void UpdateValue (int index, T newValue)
        {
            if (index < m_count)
            {
                // update values
                var node = m_nodes[index];
                var OldValue = node;
                node = newValue;

                // maintain heap integrity
                if (ComparisonMethod (newValue, OldValue) < 0)
                    shiftUp (index);
                else
                    shiftDown (index);
            }
        }

        public void UpdateMinValue (T newValue)
        {
            if (m_count > 0)
            {
                // update values
                m_nodes[0] = newValue;
                // maintain heap integrity
                shiftDown (0);
            }
            else
            {
                Add (newValue);
            }
        }

        // maintain heap integrity upwards
        private void shiftUp (int index)
        {
            if (index > 0)
            {
                int Jx = (index - 1) >> 1;
                if (ComparisonMethod (m_nodes[index], m_nodes[Jx]) < 0)
                {
                    /* swap nodes */
                    var tempNode    = m_nodes[index];
                    m_nodes[index]  = m_nodes[Jx];
                    m_nodes[Jx]     = tempNode;
                    /* proceed verifying heap structure */
                    shiftUp (Jx);
                }
            }
        }

        // maintain heap integrity downwards
        private void shiftDown (int index)
        {
            // get children indexes
            int Jx = (index << 1) + 1;
            // try to shift
            if (Jx < m_count)
            {
                // choose left or right
                if (((Jx + 1) < m_count) && (ComparisonMethod (m_nodes[Jx], m_nodes[Jx + 1]) > 0))
                    ++Jx;
                // compare nodes
                if (ComparisonMethod (m_nodes[index], m_nodes[Jx]) > 0)
                {
                    /* swap nodes */
                    var tempNode    = m_nodes[index];
                    m_nodes[index]  = m_nodes[Jx];
                    m_nodes[Jx]     = tempNode;
                    /* proceed verifying heap structure */
                    shiftDown (Jx);
                }
            }
        }
    }
}
