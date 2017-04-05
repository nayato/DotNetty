// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Transport.Libuv
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
    using DotNetty.Transport.Channels;
    using DotNetty.Transport.Libuv.Native;

    public sealed class DispatcherEventLoop : LoopExecutor, IEventLoop
    {
        static readonly TimeSpan DefaultBreakoutInterval = TimeSpan.FromMilliseconds(100);

        PipeListener pipeListener;
        IServerNativeUnsafe nativeUnsafe;

        public DispatcherEventLoop(IEventLoopGroup parent = null, string threadName = null)
            : this(parent, threadName, DefaultBreakoutInterval)
        {
        }

        public DispatcherEventLoop(IEventLoopGroup parent, string threadName, TimeSpan breakoutInterval)
            : base(parent, threadName, breakoutInterval)
        {
            string name = $"{nameof(DispatcherEventLoop)}{this.LoopThreadId}";
            this.PipeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) 
                ? $@"\\.\pipe\{name}" : $"/tmp/{name}";
        }

        internal string PipeName { get; }

        internal void Register(IServerNativeUnsafe serverChannel)
        {
            Contract.Requires(serverChannel != null);

            this.nativeUnsafe = serverChannel;
        }

        protected override void Initialize()
        {
            Loop loop = ((ILoopExecutor)this).UnsafeLoop;
            this.pipeListener = new PipeListener(loop, false);
            this.pipeListener.Listen(this.PipeName);

            if (Logger.InfoEnabled)
            {
                Logger.Info($"{nameof(DispatcherEventLoop)} ({this.LoopThreadId}) listening on pipe {this.PipeName}.");
            }
        }

        protected override void Shutdown() => this.pipeListener.Shutdonw();

        internal void Dispatch(NativeHandle handle)
        {
            try
            {
                this.pipeListener.DispatchHandle(handle);
            }
            catch
            {
                handle.CloseHandle();
                throw;
            }
        }

        internal void Accept(NativeHandle handle)
        {
            this.nativeUnsafe.Accept(handle);
        }

        public Task RegisterAsync(IChannel channel) => channel.Unsafe.RegisterAsync(this);

        public new IEventLoopGroup Parent => (IEventLoopGroup)base.Parent;
    }
}
