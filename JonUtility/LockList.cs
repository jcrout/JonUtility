using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JonUtility
{
    public class LockList<T> : IEnumerable<T>
    {
        private IList<T> internalList = new List<T>();

        public void Add(T item)
        {
            lock (internalList)
            {
                internalList.Add(item);
            }
        }

        public void Remove(T item)
        {
            lock (internalList)
            {
                internalList.Remove(item);
            }
        }

        public T this[int index]
        {
            get
            {
                lock (this.internalList)
                {
                    return this.internalList[index];
                }
            }

            set
            {
                lock (this.internalList)
                {
                    this.internalList[index] = value;
                }
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
