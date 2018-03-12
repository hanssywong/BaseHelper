using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BaseHelper
{
    public class objPool<T>
    {
        // pool of buffers
        private ConcurrentBag<T> m_FreeBuffers { get; set; }
        private Func<T> newConstructor { get; set; }

        public objPool(Func<T> newOjb, int _poolSize = 1000)
        {
            newConstructor = newOjb;
            m_FreeBuffers = new ConcurrentBag<T>();
            for (int i = 0; i < _poolSize; i++)
            {
                m_FreeBuffers.Add(newConstructor());
            }
        }
        /// <summary>
        /// Increase Object pool size by number of count
        /// Thread safe
        /// </summary>
        /// <param name="count"></param>
        public void IncObjPoolSize(int count)
        {
            for (int i = 0; i < count; i++)
            {
                m_FreeBuffers.Add(newConstructor());
            }
        }
        /// <summary>
        ///  check out a buffer, Thread safe
        ///  if resource isn't enough, it will create a new resource
        /// </summary>
        /// <returns></returns>
        public T Checkout()
        {
            T buffer;

            if (!m_FreeBuffers.TryTake(out buffer))
            {
                buffer = newConstructor();
            }
            // instead of creating new buffer, 
            // blocking waiting or refusing request may be better
            return buffer;
        }
        /// <summary>
        ///  check out a buffer, Thread safe in multi-thread environment
        ///  this method will not increase total buffer size when buffer runs out instead it will return false
        /// </summary>
        /// <returns></returns>
        public bool CheckoutLimited(out T item)
        {
            return m_FreeBuffers.TryTake(out item);
        }
        /// <summary>
        /// Thread safe, check in a buffer
        /// </summary>
        /// <param name="buffer"></param>
        public void Checkin(T buffer)
        {
            m_FreeBuffers.Add(buffer);
        }
    }
}
