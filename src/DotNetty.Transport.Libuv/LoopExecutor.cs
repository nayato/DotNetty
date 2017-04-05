// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Transport.Libuv
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Threading.Tasks;
    using DotNetty.Common.Concurrency;
    using DotNetty.Transport.Channels;
    using DotNetty.Common.Internal.Logging;
    using DotNetty.Common.Internal;
    using System.Threading;
    using DotNetty.Common;
    using DotNetty.Transport.Libuv.Native;

    public class LoopExecutor : AbstractScheduledEventExecutor, ILoopExecutor
    {
        static readonly int MaxPendingExecutorTasks = Math.Max(16, SystemPropertyUtil.GetInt("io.netty.eventexecutor.maxPendingTasks", 4096));
        static readonly string DefaultWorkerThreadName = $"Libuv {nameof(LoopExecutor)} {0}";

        protected static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<LoopExecutor>();

        const int NotStartedState = 1;
        const int StartedState = 2;
        const int ShuttingDownState = 3;
        const int ShutdownState = 4;
        const int TerminatedState = 5;

        readonly IQueue<IRunnable> taskQueue;
        readonly XThread thread;
        readonly TaskScheduler scheduler;
        readonly TaskCompletionSource terminationCompletionSource;
        readonly PreciseTimeSpan preciseBreakoutInterval;
        readonly Loop loop;
        readonly Async asyncHandle;

        volatile int executionState = NotStartedState;

        public LoopExecutor(TimeSpan breakoutInterval) 
            : this(null, nameof(LoopExecutor), breakoutInterval)
        {
        }

        public LoopExecutor(string threadName, TimeSpan breakoutInterval)
            : this(null, threadName, breakoutInterval)
        {
        }

        public LoopExecutor(IEventLoopGroup parent, string threadName, TimeSpan breakoutInterval) : base(parent)
        {
            this.preciseBreakoutInterval = PreciseTimeSpan.FromTimeSpan(breakoutInterval);

            this.terminationCompletionSource = new TaskCompletionSource();
            this.taskQueue = PlatformDependent.NewFixedMpscQueue<IRunnable>(MaxPendingExecutorTasks);
            this.scheduler = new ExecutorTaskScheduler(this);

            this.loop = new Loop();
            this.asyncHandle = new Async(this.loop, OnCallback, this);
            string name = string.Format(DefaultWorkerThreadName, this.loop.Handle);
            if (!string.IsNullOrEmpty(threadName))
            {
                name = $"{name} ({threadName})";
            }
            this.thread = new XThread(RunLoop)
            {
                Name = name
            };

            this.thread.Start(this);
        }

        Loop ILoopExecutor.UnsafeLoop => this.loop;

        internal int LoopThreadId => this.thread.Id;

        static void RunLoop(object state)
        {
            var loop = (LoopExecutor)state;
            loop.SetCurrentExecutor(loop);

            Task.Factory.StartNew(
                () =>
                {
                    try
                    {
                        loop.asyncHandle.RemoveReference();
                        loop.Initialize();
                        loop.executionState = StartedState;
                        loop.loop.Run(uv_run_mode.UV_RUN_DEFAULT);
                        loop.terminationCompletionSource.TryComplete();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("{}: execution loop failed", loop.thread.Name, ex);
                        loop.terminationCompletionSource.TrySetException(ex);
                    }

                    loop.executionState = TerminatedState;
                },
                CancellationToken.None,
                TaskCreationOptions.None,
                loop.scheduler);
        }

        static void OnCallback(object state)
        {
            var eventLoop = (LoopExecutor)state;
            eventLoop.RunAllTasks(eventLoop.preciseBreakoutInterval);
        }

        protected virtual void Initialize()
        {
        }

        void ShutdownLoop()
        {
            try
            {
                this.Shutdown();
                this.asyncHandle.CloseHandle();
                this.loop.Close();
            }
            catch (Exception exception)
            {
                Logger.Error("{}: stop loop failed", exception);
            }
        }

        protected virtual void Shutdown()
        {
        }

        void RunAllTasks(PreciseTimeSpan timeout)
        {
            this.FetchFromScheduledTaskQueue();
            IRunnable task = this.PollTask();
            if (task == null)
            {
                return;
            }

            long start = this.loop.Now;
            long deadline = timeout.Ticks;
            long runTasks = 0;
            while (true)
            {
                try
                {
                    task.Run();
                }
                catch (Exception ex)
                {
                    Logger.Warn("A task raised an exception.", ex);
                }

                runTasks++;

                if (this.IsShutdown)
                {
                    this.ShutdownLoop();
                    break;
                }

                if ((runTasks & 0x3F) == 0)
                {
                    if ((this.loop.Now - start) >= deadline)
                    {
                        break;
                    }
                }

                task = this.PollTask();
                if (task == null)
                {
                    break;
                }
            }
        }

        void FetchFromScheduledTaskQueue()
        {
            PreciseTimeSpan nanoTime = PreciseTimeSpan.FromStart;
            IScheduledRunnable scheduledTask = this.PollScheduledTask(nanoTime);
            while (scheduledTask != null)
            {
                if (!this.taskQueue.TryEnqueue(scheduledTask))
                {
                    // No space left in the task queue add it back to the scheduledTaskQueue so we pick it up again.
                    this.ScheduledTaskQueue.Enqueue(scheduledTask);
                    break;
                }
                scheduledTask = this.PollScheduledTask(nanoTime);
            }
        }

        IRunnable PollTask()
        {
            Contract.Assert(this.InEventLoop);

            if (!this.taskQueue.TryDequeue(out IRunnable task))
            {
                if (!this.taskQueue.TryDequeue(out task) && !this.IsShuttingDown) // revisit queue as producer might have put a task in meanwhile
                {
                    IScheduledRunnable nextScheduledTask = this.ScheduledTaskQueue.Peek();
                    if (nextScheduledTask != null)
                    {
                        PreciseTimeSpan wakeupTimeout = nextScheduledTask.Deadline - PreciseTimeSpan.FromStart;
                        if (wakeupTimeout.Ticks > 0)
                        {
                            this.taskQueue.TryDequeue(out task);
                        }
                    }
                    else
                    {
                        this.taskQueue.TryDequeue(out task);
                    }
                }
            }

            return task;
        }

        public override bool IsShuttingDown => this.executionState >= ShuttingDownState;

        public override Task TerminationCompletion => this.terminationCompletionSource.Task;

        public override bool IsShutdown => this.executionState >= ShutdownState;

        public override bool IsTerminated => this.executionState == TerminatedState;

        public override bool IsInEventLoop(XThread t) => this.thread == t;

        public override void Execute(IRunnable task)
        {
            this.taskQueue.TryEnqueue(task);
            this.asyncHandle.Send();
        }

        public override Task ShutdownGracefullyAsync(TimeSpan quietPeriod, TimeSpan timeout)
        {
            Contract.Requires(quietPeriod >= TimeSpan.Zero);
            Contract.Requires(timeout >= quietPeriod);

            if (this.IsShuttingDown)
            {
                return this.TerminationCompletion;
            }

            bool inEventLoop = this.InEventLoop;
            bool wakeup;
            while (true)
            {
                if (this.IsShuttingDown)
                {
                    return this.TerminationCompletion;
                }
                int newState;
                wakeup = true;
                int oldState = this.executionState;
                if (inEventLoop)
                {
                    newState = ShuttingDownState;
                }
                else
                {
                    switch (oldState)
                    {
                        case NotStartedState:
                        case StartedState:
                            newState = ShuttingDownState;
                            break;
                        default:
                            newState = oldState;
                            wakeup = false;
                            break;
                    }
                }
#pragma warning disable 420
                if (Interlocked.CompareExchange(ref this.executionState, newState, oldState) == oldState)
#pragma warning restore 420
                {
                    break;
                }
            }

            if (wakeup)
            {
                this.asyncHandle.Send();
            }

            return this.TerminationCompletion;
        }
    }
}
