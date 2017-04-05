// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Transport.Libuv
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Threading.Tasks;
    using DotNetty.Transport.Channels;
    using DotNetty.Transport.Libuv.Native;

    sealed class WorkerLoopExecutor : LoopExecutor, IEventLoop
    {
        static readonly TimeSpan DefaultBreakoutInterval = TimeSpan.FromMilliseconds(100);

        public WorkerLoopExecutor(MultiLoopGroup parent) 
            : base(parent, null, DefaultBreakoutInterval)
        {
            Contract.Requires(parent != null);

            string pipeName = parent.PipeName;
            if (string.IsNullOrEmpty(pipeName))
            {
                throw new ArgumentException("Pipe nane is required for work loops");
            }

            this.PipeName = pipeName;
        }

        internal string PipeName { get; }

        internal Pipe PipeHandle { get; set; }

        protected override void Initialize()
        {
            Loop loop = ((ILoopExecutor)this).UnsafeLoop;
            this.PipeHandle = new Pipe(loop, true);
            PipeConnect request = null;

            try
            {
                request = new PipeConnect(this);
            }
            catch (Exception exception)
            {
                Logger.Warn($"{nameof(WorkerLoopExecutor)} failed to create connect request to dispatcher", exception);
                request?.Dispose();
            }
        }

        protected override void Shutdown()
        {
            ShutdownRequest.Start(this.PipeHandle);
        }

        void OnConnected(ConnectRequest request)
        {
            try
            {
                if (request.Error != null)
                {
                    Logger.Warn($"{nameof(WorkerLoopExecutor)} failed to connect to dispatcher", request.Error);
                }
                else
                {
                    if (Logger.InfoEnabled)
                    {
                        Logger.Info($"{nameof(WorkerLoopExecutor)} ({this.LoopThreadId}) dispatcher pipe {this.PipeName} connected.");
                    }

                    this.PipeHandle.ReadStart(this.OnRead);
                }
            }
            finally
            {
                request.Dispose();
            }
        }

        void OnRead(Pipe pipe, int status)
        {
            if (status < 0)
            {
                pipe.CloseHandle();
                if (status != (int)uv_err_code.UV_EOF)
                {
                    OperationException error = NativeMethods.CreateError((uv_err_code)status);
                    Logger.Warn("IPC Pipe read error", error);
                }
            }
            else
            {
                Tcp handle = pipe.GetPendingHandle();
                ((MultiLoopGroup)this.Parent).Accept(handle);
            }
        }

        public Task RegisterAsync(IChannel channel) => channel.Unsafe.RegisterAsync(this);

        public new IEventLoopGroup Parent => (IEventLoopGroup)base.Parent;

        sealed class PipeConnect : ConnectRequest
        {
            readonly WorkerLoopExecutor workerLoop;

            public PipeConnect(WorkerLoopExecutor workerLoop)
            {
                Contract.Requires(workerLoop != null);

                this.workerLoop = workerLoop;

                NativeMethods.uv_pipe_connect(
                    this.Handle,
                    workerLoop.PipeHandle.Handle,
                    workerLoop.PipeName,
                    WatcherCallback);
            }

            protected override void OnWatcherCallback()
            {
                this.workerLoop.OnConnected(this);
            }
        }
    }
}
