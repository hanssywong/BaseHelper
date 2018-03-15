using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BaseHelper
{
    /// <summary>
    /// pool of buffers
    /// Checkout blocking if resource is empty
    /// </summary>
    /// <typeparam name="T">class</typeparam>
    public class objPoolV2<T>
    {
        private ConcurrentBag<T> m_FreeBuffers { get; set; }
        private Func<T> newConstructor { get; set; }
        private ManualResetEventSlim evt { get; } = new ManualResetEventSlim();
        private CancellationTokenSource cts { get; } = new CancellationTokenSource();

        public objPoolV2(Func<T> newOjb, int _poolSize = 1000)
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
            if (evt.SpinCount > 0) evt.Set();
        }
        /// <summary>
        ///  check out a buffer, Thread safe in multi-thread environment
        ///  this method will not increase total buffer size when buffer runs out instead it will return false
        /// </summary>
        /// <returns></returns>
        public T Checkout()
        {
            T item;
            while(!m_FreeBuffers.TryTake(out item))
            {
                evt.Wait(cts.Token);
            }
            return item;
        }
        /// <summary>
        /// Thread safe, check in a buffer
        /// </summary>
        /// <param name="buffer"></param>
        public void Checkin(T buffer)
        {
            m_FreeBuffers.Add(buffer);
            if (evt.SpinCount > 0) evt.Set();
        }
        public void Shutdown()
        {
            cts.Cancel();
        }
    }
}
