// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Transport.Libuv
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using DotNetty.Common.Concurrency;
    using DotNetty.Transport.Channels;
    using DotNetty.Transport.Libuv.Native;

    public sealed class MultiLoopGroup : IEventLoopGroup
    {
        static readonly int DefaultEventLoopThreadCount = Environment.ProcessorCount * 2;
        readonly IEventLoop[] eventLoops;
        readonly DispatcherEventLoop dispatcherLoop;
        int requestId;

        public MultiLoopGroup(DispatcherEventLoop dispatcherLoop = null) 
            : this(dispatcherLoop, DefaultEventLoopThreadCount)
        {
        }

        public MultiLoopGroup(DispatcherEventLoop dispatcherLoop, int eventLoopCount)
        {
            this.dispatcherLoop = dispatcherLoop;

            this.eventLoops = new IEventLoop[eventLoopCount];
            var terminationTasks = new Task[eventLoopCount];
            for (int i = 0; i < eventLoopCount; i++)
            {
                IEventLoop eventLoop;
                bool success = false;
                try
                {
                    if (dispatcherLoop == null)
                    {
                        eventLoop = new EventLoop(this);
                    }
                    else
                    {
                        eventLoop = new WorkerLoopExecutor(this);
                    }
                    success = true;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("failed to create a child event loop.", ex);
                }
                finally
                {
                    if (!success)
                    {
                        Task.WhenAll(this.eventLoops
                                .Take(i)
                                .Select(loop => loop.ShutdownGracefullyAsync()))
                            .Wait();
                    }
                }

                this.eventLoops[i] = eventLoop;
                terminationTasks[i] = eventLoop.TerminationCompletion;
            }

            this.TerminationCompletion = Task.WhenAll(terminationTasks);
        }

        internal string PipeName => this.dispatcherLoop?.PipeName;

        internal void Accept(NativeHandle handle)
        {
            Debug.Assert(this.dispatcherLoop != null);
            this.dispatcherLoop.Accept(handle);
        }

        public Task TerminationCompletion { get; }

        public IEventLoop GetNext()
        {
            int id = Interlocked.Increment(ref this.requestId);
            return this.eventLoops[Math.Abs(id % this.eventLoops.Length)];
        }

        IEventExecutor IEventExecutorGroup.GetNext() => this.GetNext();

        public Task RegisterAsync(IChannel channel)
        {
            var nativeChannel = channel as NativeChannel;
            if (nativeChannel == null)
            {
                throw new ArgumentException($"{nameof(channel)} must be of {typeof(NativeChannel)}");
            }

            IntPtr loopHandle = nativeChannel.GetLoopHandle();
            foreach (IEventLoop loop in this.eventLoops)
            {
                if (((ILoopExecutor)loop).UnsafeLoop.Handle == loopHandle)
                {
                    return loop.RegisterAsync(nativeChannel);
                }
            }

            throw new InvalidOperationException($"Loop {loopHandle} does not exist");
        }

        public Task ShutdownGracefullyAsync()
        {
            foreach (IEventLoop eventLoop in this.eventLoops)
            {
                eventLoop.ShutdownGracefullyAsync();
            }
            return this.TerminationCompletion;
        }

        public Task ShutdownGracefullyAsync(TimeSpan quietPeriod, TimeSpan timeout)
        {
            foreach (IEventLoop eventLoop in this.eventLoops)
            {
                eventLoop.ShutdownGracefullyAsync(quietPeriod, timeout);
            }

            return this.TerminationCompletion;
        }
    }
}
