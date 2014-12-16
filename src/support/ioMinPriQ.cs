using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using ioSoftSmiths.ioVector;

namespace ioSoftSmiths.Collections
{
    public class ioMinPriQ<T> where T : IComparable<T>
    {

        private List<T> m_Data;
        public List<T> DataList { get { return m_Data; } } 

        public bool IsEmpty { get { return m_Data.Count == 0; } }

        public ioMinPriQ()
        {
            m_Data = new List<T>();
        }

        public void Enqueue(T _data)
        {
            m_Data.Add(_data);
            int currentIdx = m_Data.Count - 1;
            while (currentIdx > 0)
            {
                int parentIdx = (currentIdx - 1) / 2;
                if (m_Data[currentIdx].CompareTo(m_Data[parentIdx]) >= 0)
                    break;
                T tmp = m_Data[currentIdx]; 
                m_Data[currentIdx] = m_Data[parentIdx]; 
                m_Data[parentIdx] = tmp;
                currentIdx = parentIdx;
            }
        }

        public T Dequeue()
        {
            if (m_Data.Count == 0)
                throw new NullReferenceException("ioPQ: Tried to dequeue an empty priority queue.");
            int lastIdx = m_Data.Count - 1;
            T frontItem = m_Data[0];
            m_Data[0] = m_Data[lastIdx];
            m_Data.RemoveAt(lastIdx);

            --lastIdx;
            int parentIdx = 0;
            while (true)
            {
                int currentIdx = parentIdx * 2 + 1;
                if (currentIdx > lastIdx) break;
                int rc = currentIdx + 1;
                if (rc <= lastIdx && m_Data[rc].CompareTo(m_Data[currentIdx]) < 0)
                    currentIdx = rc;
                if (m_Data[parentIdx].CompareTo(m_Data[currentIdx]) <= 0) break;
                T tmp = m_Data[parentIdx]; m_Data[parentIdx] = m_Data[currentIdx]; m_Data[currentIdx] = tmp;
                parentIdx = currentIdx;
            }
            return frontItem;
        }

        public int Count()
        {
            return m_Data.Count();
        }

        public bool IsConsistent()
        {
            if (m_Data.Count == 0) return true;
            int lastIdx = m_Data.Count - 1; 
            for (int parentIdx = 0; parentIdx < m_Data.Count; ++parentIdx) // each parent index
            {
                int ltChildIdx = 2 * parentIdx + 1; 
                int rtChildIdx = 2 * parentIdx + 2; 
                if (ltChildIdx <= lastIdx && m_Data[parentIdx].CompareTo(m_Data[ltChildIdx]) > 0) return false;
                if (rtChildIdx <= lastIdx && m_Data[parentIdx].CompareTo(m_Data[rtChildIdx]) > 0) return false;
            }
            return true; // Passed all checks
        }

        public bool Contains(T _key)
        {
            return m_Data.Contains(_key);
        }

        public void Remove(T _data)
        {
            m_Data.Remove(_data);
        }
    }

    
}
