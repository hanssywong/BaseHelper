using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BaseHelper
{
    public sealed class SpinQueue<T> : IDisposable
    {
        ConcurrentQueue<T> QueueA { get; } = new ConcurrentQueue<T>();
        ConcurrentQueue<T> QueueB { get; } = new ConcurrentQueue<T>();
        bool bWritingQueueA { get { return bWritingQueueFlag; } set { bWritingQueueFlag = value; } }
        volatile bool bWritingQueueFlag = true;
        ManualResetEventSlim WaitForMsgEvent { get; } = new ManualResetEventSlim();
        public bool IsQueueEmpty { get { return (QueueA.Count == 0) && (QueueB.Count == 0); } }
        public bool IsRunning { get { return bIsRunningFlag; } private set { bIsRunningFlag = value; } }
        volatile bool bIsRunningFlag = true;
        CancellationTokenSource cts { get; } = new CancellationTokenSource();
        /// <summary>
        /// This function will wait all queue being empty before shutdown
        /// </summary>
        public void ShutdownGracefully()
        {
            IsRunning = false;
            while (!IsQueueEmpty)
            {
                Thread.Sleep(100);
            }
            cts.Cancel();
        }
        /// <summary>
        /// This function will shutdown immediately no matter the queue being empty or not
        /// </summary>
        public void ShutdownImmediately()
        {
            IsRunning = false;
            cts.Cancel();
        }
        public bool Enqueue(T obj)
        {
            if (!IsRunning) return false;
            if (bWritingQueueA)
            {
                QueueA.Enqueue(obj);
            }
            else
            {
                QueueB.Enqueue(obj);
            }

            if (WaitForMsgEvent.SpinCount > 0)
                WaitForMsgEvent.Set();
            return true;
        }

        /// <summary>
        /// the out T can be null and return will be false
        /// if the queue is empty, then this function will be blocked until new message arrive
        /// </summary>
        /// <param name="logmsg"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public bool TryDequeue(out T logmsg)
        {
            if (IsQueueEmpty)
            {
                WaitForMsgEvent.Wait(cts.Token);
                WaitForMsgEvent.Reset();
            }

            bool ret = false;
            if (!bWritingQueueA)
            {
                ret = QueueA.TryDequeue(out logmsg);
            }
            else
            {
                ret = QueueB.TryDequeue(out logmsg);
            }

            if (!ret)
            {
                if (bWritingQueueA)
                    bWritingQueueA = false;
                else
                    bWritingQueueA = true;
            }

            return ret;
        }

        public void Dispose()
        {
            WaitForMsgEvent.Dispose();
            cts.Dispose();
        }
    }
}
