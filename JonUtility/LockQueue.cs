using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JonUtility
{
    public class LockStack<T> : IEnumerable<T>
    {
        private Stack<T> internalList = new Stack<T>();

        public void Push(T item)
        {
            lock (internalList)
            {
                internalList.Push(item);
            }
        }

        public T Pop()
        {
            lock (internalList)
            {
                return internalList.Pop();
            }
        }

        public bool Contains(T item)
        {
            lock (internalList)
            {
                return internalList.Any(t => Object.Equals(t, item));
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return Clone().GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return Clone().GetEnumerator();
        }

        private List<T> Clone()
        {
            var newList = new List<T>();

            lock (this.internalList)
            {
                newList.AddRange(this.internalList);
            }

            return newList;
        }
    }

    public class LockQueue<T> : IEnumerable<T>
    {
        private Queue<T> internalList = new Queue<T>();

        public void Enqueue(T item)
        {
            lock (internalList)
            {
                internalList.Enqueue(item);
            }
        }

        public void Dequeue()
        {
            lock (internalList)
            {               
                internalList.Dequeue();
            }
        }
        
        public IEnumerator<T> GetEnumerator()
        {
            return Clone().GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return Clone().GetEnumerator();
        }

        private List<T> Clone()
        {
            var newList = new List<T>();

            lock (this.internalList)
            {
                newList.AddRange(this.internalList);
            }

            return newList;
        }
    }
}