// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Transport.Libuv
{
    using System;
    using System.Threading.Tasks;
    using DotNetty.Transport.Channels;

    public sealed class EventLoop : LoopExecutor, IEventLoop
    {
        static readonly TimeSpan DefaultBreakoutInterval = TimeSpan.FromMilliseconds(100);

        public EventLoop(IEventLoopGroup parent = null, string threadName = null)
            : this(parent, threadName, DefaultBreakoutInterval)
        {
        }

        public EventLoop(IEventLoopGroup parent, string threadName, TimeSpan breakoutInterval)
            : base(parent, threadName, breakoutInterval)
        {
        }

        public Task RegisterAsync(IChannel channel) => channel.Unsafe.RegisterAsync(this);

        public new IEventLoopGroup Parent => (IEventLoopGroup)base.Parent;
    }
}
