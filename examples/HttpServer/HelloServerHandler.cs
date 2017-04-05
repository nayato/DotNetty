// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace HttpServer
{
    using System.Text;
    using DotNetty.Buffers;
    using DotNetty.Codecs.Http;
    using DotNetty.Common.Utilities;
    using DotNetty.Transport.Channels;
    using System;
    using DotNetty.Common;

    sealed class HelloServerHandler : SimpleChannelInboundHandler<IHttpRequest>
    {
        static readonly ThreadLocalCache Cache = new ThreadLocalCache();

        sealed class ThreadLocalCache : FastThreadLocal<AsciiString>
        {
            protected override AsciiString GetInitialValue()
            {
                DateTime dateTime = DateTime.UtcNow;
                return new AsciiString($"{dateTime.DayOfWeek}, {dateTime:dd MMM yyyy HH:mm:ss z}");
            }
        }

        static readonly byte[] StaticPlaintext = Encoding.UTF8.GetBytes("Hello, World!");
        static readonly int StaticPlaintextLen = StaticPlaintext.Length;
        static readonly IByteBuffer PlaintextContentBuffer = Unpooled.WrappedBuffer(StaticPlaintext).Unreleasable();
        static readonly ICharSequence PlaintextClheaderValue = new AsciiString($"{StaticPlaintextLen}");
        static readonly ICharSequence JsonClheaderValue = new AsciiString($"{JsonLen()}");

        static readonly ICharSequence TypePlain = new AsciiString("text/plain");
        static readonly ICharSequence TypeJson = new AsciiString("application/json");
        static readonly ICharSequence ServerName = new AsciiString("Netty");
        static readonly ICharSequence ContentTypeEntity = HttpHeaderNames.ContentType;
        static readonly ICharSequence DateEntity = HttpHeaderNames.Date;
        static readonly ICharSequence ContentLengthEntity = HttpHeaderNames.ContentLength;
        static readonly ICharSequence ServerEntity = HttpHeaderNames.Server;

        volatile ICharSequence date = Cache.Value;

        static int JsonLen() => Encoding.UTF8.GetBytes(NewMessage().ToJsonFormat()).Length;

        static MessageBody NewMessage() => new MessageBody("Hello, World!");

        protected override void ChannelRead0(IChannelHandlerContext ctx, IHttpRequest request)
        {
            string uri = request.Uri;
            switch (uri)
            {
                case "/plaintext":
                    this.WriteResponse(ctx, PlaintextContentBuffer.Duplicate(), TypePlain, PlaintextClheaderValue);
                    break;
                case "/json":
                    byte[] json = Encoding.UTF8.GetBytes(NewMessage().ToJsonFormat());
                    this.WriteResponse(ctx, Unpooled.WrappedBuffer(json), TypeJson, JsonClheaderValue);
                    break;
                default:
                    var response = new DefaultFullHttpResponse(HttpVersion.Http11, HttpResponseStatus.NotFound, Unpooled.Empty, false);
                    ctx.WriteAndFlushAsync(response);
                    ctx.CloseAsync();
                    break;
            }
        }

        void WriteResponse(IChannelHandlerContext ctx, IByteBuffer buf, ICharSequence contentType, ICharSequence contentLength)
        {
            // Build the response object.
            var response = new DefaultFullHttpResponse(HttpVersion.Http11, HttpResponseStatus.OK, buf, false);
            HttpHeaders headers = response.Headers;
            headers.Set(ContentTypeEntity, contentType);
            headers.Set(ServerEntity, ServerName);
            headers.Set(DateEntity, this.date);
            headers.Set(ContentLengthEntity, contentLength);

            // Close the non-keep-alive connection after the write operation is done.
            ctx.WriteAsync(response);
        }

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception) => context.CloseAsync();

        public override void ChannelReadComplete(IChannelHandlerContext context) => context.Flush();
    }
}
