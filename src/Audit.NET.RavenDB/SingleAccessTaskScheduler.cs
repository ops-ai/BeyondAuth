using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Audit.NET.RavenDB
{
    public class SingleAccessTaskScheduler : TaskScheduler
    {
        /// <summary>
        /// Is anything processing tasks, either a ThreadPoolThread or another thread, inline?
        /// </summary>
        private bool anythingIsProcessingTasks;

        /// <summary>
        /// Is the current thread processing a task? If so, it can process others inline even if anythingIsProcessingTasks is true
        /// </summary>
        [ThreadStatic]
        private static bool currentThreadIsProcessingTasks;

        /// <summary>
        /// Tasks waiting to be executed by the ThreadPool thread
        /// </summary>
        private Queue<Task> tasks = new Queue<Task>();

        /// <summary>
        /// Lock around all members
        /// </summary>
        private object tasksLock = new object();

        public SingleAccessTaskScheduler()
        {
        }

        /// <summary>
        /// Try and run the task inline, and if that fails (another thread is running tasks), queue it for the ThreadPool thread (unless inlineOnly is true)
        /// </summary>
        /// <param name="task">Task to run</param>
        /// <param name="inlineOnly">If true, don't attempt to queue for the ThreadPool, just return false</param>
        /// <returns>The result of base.TryExecuteTask if it was executed inline, true if it was queued for the ThreadPool thread, false otherwise</returns>
        private bool TryRunTask(Task task, bool inlineOnly)
        {
            bool retVal = true;
            bool canRunInline = false;
            bool anythingWasProcessingTasks;
            bool currentThreadWasProcessingTasks;

            lock (this.tasksLock)
            {
                // If the current thread is already processing tasks, or nothing else is processing them, we can go ahead ourselves
                anythingWasProcessingTasks = this.anythingIsProcessingTasks;
                currentThreadWasProcessingTasks = currentThreadIsProcessingTasks;
                if (!this.anythingIsProcessingTasks || currentThreadIsProcessingTasks)
                {
                    this.anythingIsProcessingTasks = true;
                    currentThreadIsProcessingTasks = true;
                    canRunInline = true;
                }
            }

            if (canRunInline)
            {
                System.Diagnostics.Debug.WriteLine("Running task inline. Thread: {0}", Thread.CurrentThread.ManagedThreadId);
                try
                {
                    retVal = base.TryExecuteTask(task);
                }
                finally
                {
                    anythingIsProcessingTasks = anythingWasProcessingTasks;
                    currentThreadIsProcessingTasks = currentThreadWasProcessingTasks;
                }
                // In case someone else tried to execute a task while we were busy, notify the thread pool
                this.NotifyThreadPoolIfNecessary();
            }
            else if (!inlineOnly)
            {
                System.Diagnostics.Debug.WriteLine("Queueing task for ThreadPool execution. Thread: {0}", Thread.CurrentThread.ManagedThreadId);
                lock (this.tasksLock)
                {
                    this.tasks.Enqueue(task);
                }
                this.NotifyThreadPoolIfNecessary();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Failed to queue task for ThreadPool execution, as task is marked inline only. Thread: {0}", Thread.CurrentThread.ManagedThreadId);
                retVal = false;
            }

            return retVal;
        }

        /// <summary>
        /// If there are queued tasks for the ThreadPool thread, and nothing is processing tasks, kick off a ThreadPool thread to empty the queue
        /// </summary>
        private void NotifyThreadPoolIfNecessary()
        {
            lock (this.tasksLock)
            {
                if (this.tasks.Count == 0)
                    System.Diagnostics.Debug.WriteLine("Attempting to start ThreadPool thread, but no tasks queued. Thread: {0}", Thread.CurrentThread.ManagedThreadId);
                else if (this.anythingIsProcessingTasks)
                    System.Diagnostics.Debug.WriteLine("Attempting to start ThreadPool thread, but something else is at work. Thread: {0}", Thread.CurrentThread.ManagedThreadId);

                if (this.anythingIsProcessingTasks || this.tasks.Count == 0)
                    return;

                this.anythingIsProcessingTasks = true;
            }

            System.Diagnostics.Debug.WriteLine("Starting ThreadPool thread. Thread: {0}", Thread.CurrentThread.ManagedThreadId);
            ThreadPool.UnsafeQueueUserWorkItem(_ =>
            {
                System.Diagnostics.Debug.WriteLine("ThreadPool thread started. Thread: {0}", Thread.CurrentThread.ManagedThreadId);
                currentThreadIsProcessingTasks = true;
                while (true)
                {
                    Task task;
                    lock (this.tasksLock)
                    {
                        if (this.tasks.Count == 0)
                        {
                            this.anythingIsProcessingTasks = false;
                            currentThreadIsProcessingTasks = false;
                            break;
                        }

                        task = this.tasks.Dequeue();
                    }

                    try
                    {
                        base.TryExecuteTask(task);
                    }
                    catch (Exception e)
                    {
                        System.Diagnostics.Debug.WriteLine("Task on ThreadPool thread exception: {0}", e.Message);
                    }

                }
                System.Diagnostics.Debug.WriteLine("ThreadPool thread finished. Thread: {0}", Thread.CurrentThread.ManagedThreadId);

            }, null);
        }

        /// <summary>
        /// Execute a task inline if possible, otherwise queue for execution by a ThreadPool thread
        /// </summary>
        /// <param name="task">Task to execute</param>
        protected override void QueueTask(Task task)
        {
            this.TryRunTask(task, false);
        }

        /// <summary>
        /// Try to execute a task inline if possible, otherwise return false
        /// </summary>
        /// <remarks>In the current implementation, if taskWasPreviouslyQueued is true, this method returns false, as tasks can't be dequeued</remarks>
        /// <param name="task">Task to execute</param>
        /// <param name="taskWasPreviouslyQueued">True if the task was previously queued for execution</param>
        /// <returns>Results of base.TryExecuteTask if task could be inlined, otherwise false</returns>
        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            // We don't (yet) support removing previously-queued tasks, so if it was previously queued, tough
            if (taskWasPreviouslyQueued)
            {
                System.Diagnostics.Debug.WriteLine("Refusing to execute task inline, as it was previously queued. Thread: {0}", Thread.CurrentThread.ManagedThreadId);
                return false;
            }

            return this.TryRunTask(task, true);
        }

        public override int MaximumConcurrencyLevel
        {
            get { return 1; }
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            bool lockTaken = false;
            try
            {
                Monitor.TryEnter(this.tasksLock, ref lockTaken);
                if (lockTaken)
                    return this.tasks;
                else
                    throw new NotSupportedException();
            }
            finally
            {
                if (lockTaken)
                    Monitor.Exit(this.tasksLock);
            }
        }
    }
}
