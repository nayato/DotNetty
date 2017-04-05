// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Transport.Libuv.Native
{
    using System;
    using System.Diagnostics.Contracts;

    sealed class ShutdownRequest : NativeRequest
    {
        static readonly uv_watcher_cb WatcherCallback = OnCompletedCallback;
        readonly NativeHandle stream;

        ShutdownRequest(NativeHandle stream) : base(uv_req_type.UV_SHUTDOWN, 0)
        {
            Contract.Requires(stream != null);

            this.stream = stream;

            int result = NativeMethods.uv_shutdown(this.Handle, this.stream.Handle, WatcherCallback);
            if (result < 0)
            {
                throw NativeMethods.CreateError((uv_err_code)result);
            }
        }

        internal static void Start(NativeHandle stream)
        {
            if (stream == null || !stream.IsValid)
            {
                return;
            }

            ShutdownRequest request = null;
            try
            {
                request = new ShutdownRequest(stream);
            }
            catch (Exception exception)
            {
                Logger.Warn("Failed to create shutdown request for {}.", stream.HandleType,  exception);
                request?.Dispose();
            }
        }

        void OnCompletedCallback(int status)
        {
            if (status < 0)
            {
                OperationException error = NativeMethods.CreateError((uv_err_code)status);
                Logger.Warn("Failed to shutdown stream {}.", this.stream.HandleType, error);
            }

            this.stream.Dispose();
            this.Dispose();
        }

        static void OnCompletedCallback(IntPtr handle, int status)
        {
            var request = GetTarget<ShutdownRequest>(handle);
            request?.OnCompletedCallback(status);
        }
    }
}
