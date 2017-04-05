// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http.Tests
{
    using System;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using DotNetty.Buffers;
    using DotNetty.Common.Concurrency;
    using DotNetty.Common.Utilities;
    using DotNetty.Transport.Bootstrapping;
    using DotNetty.Transport.Channels;
    using DotNetty.Transport.Channels.Embedded;
    using DotNetty.Transport.Channels.Sockets;
    using Xunit;

    using HttpVersion = DotNetty.Codecs.Http.HttpVersion;

    public sealed class HttpClientCodecTest
    {
        const string Response = "HTTP/1.0 200 OK\r\n" 
            + "Date: Fri, 31 Dec 1999 23:59:59 GMT\r\n" 
            + "Content-Type: text/html\r\n" + "Content-Length: 28\r\n" + "\r\n"
            + "<html><body></body></html>\r\n";

        const string IncompleteChunkedResponse = "HTTP/1.1 200 OK\r\n" 
            + "Content-Type: text/plain\r\n" 
            + "Transfer-Encoding: chunked\r\n" + "\r\n" 
            + "5\r\n" + "first\r\n" + "6\r\n" + "second\r\n" + "0\r\n";

        const string ChunkedResponse = IncompleteChunkedResponse + "\r\n";

        [Fact]
        public void FailsNotOnRequestResponse()
        {
            var codec = new HttpClientCodec(4096, 8192, 8192, true);
            var ch = new EmbeddedChannel(codec);
            ch.WriteOutbound(new DefaultFullHttpRequest(HttpVersion.Http11, HttpMethod.Get, "http://localhost/"));
            ch.WriteInbound(Unpooled.CopiedBuffer(Encoding.UTF8.GetBytes(Response)));
            ch.Finish();

            for (;;)
            {
                var msg = ch.ReadOutbound<object>();
                if (msg == null)
                {
                    break;
                }
                ReferenceCountUtil.Release(msg);
            }
            for (;;)
            {
                var msg = ch.ReadInbound<object>();
                if (msg == null)
                {
                    break;
                }
                ReferenceCountUtil.Release(msg);
            }
        }

        [Fact]
        public void FailsNotOnRequestResponseChunked()
        {
            var codec = new HttpClientCodec(4096, 8192, 8192, true);
            var ch = new EmbeddedChannel(codec);

            ch.WriteOutbound(new DefaultFullHttpRequest(HttpVersion.Http11, HttpMethod.Get, "http://localhost/"));
            ch.WriteInbound(Unpooled.CopiedBuffer(Encoding.UTF8.GetBytes(ChunkedResponse)));
            ch.Finish();

            for (;;)
            {
                var msg = ch.ReadOutbound<object>();
                if (msg == null)
                {
                    break;
                }
                ReferenceCountUtil.Release(msg);
            }
            for (;;)
            {
                var msg = ch.ReadInbound<object>();
                if (msg == null)
                {
                    break;
                }
                ReferenceCountUtil.Release(msg);
            }
        }

        [Fact]
        public void FailsOnMissingResponse()
        {
            var codec = new HttpClientCodec(4096, 8192, 8192, true);
            var ch = new EmbeddedChannel(codec);

            Assert.True(ch.WriteOutbound(new DefaultFullHttpRequest(HttpVersion.Http11, HttpMethod.Get, "http://localhost/")));
            var buffer = ch.ReadOutbound<IByteBuffer>();
            Assert.NotNull(buffer);
            buffer.Release();

            try
            {
                ch.Finish();
                Assert.True(false, "Should not get here, expecting exception thrown");
            }
            catch (CodecException e)
            {
                Assert.IsType<PrematureChannelClosureException>(e);
            }
        }

        [Fact]
        public void FailsOnIncompleteChunkedResponse()
        {
            var codec = new HttpClientCodec(4096, 8192, 8192, true);
            var ch = new EmbeddedChannel(codec);

            ch.WriteOutbound(new DefaultFullHttpRequest(HttpVersion.Http11, HttpMethod.Get, "http://localhost/"));
            var buffer = ch.ReadOutbound<IByteBuffer>();
            Assert.NotNull(buffer);
            buffer.Release();
            Assert.Null(ch.ReadInbound<IByteBuffer>());
            ch.WriteInbound(Unpooled.CopiedBuffer(Encoding.UTF8.GetBytes(IncompleteChunkedResponse)));
            var response = ch.ReadInbound<IHttpResponse>();
            Assert.NotNull(response);
            var content = ch.ReadInbound<IHttpContent>();
            Assert.NotNull(content); // Chunk 'first'
            content.Release();

            content = ch.ReadInbound<IHttpContent>();
            Assert.NotNull(content); // Chunk 'second'
            content.Release();

            content = ch.ReadInbound<IHttpContent>();
            Assert.Null(content);

            try
            {
                ch.Finish();
                Assert.True(false, "Should not get here, expecting exception thrown");
            }
            catch (CodecException e)
            {
                Assert.IsType<PrematureChannelClosureException>(e);
            }
        }

        [Fact]
        public void ServerCloseSocketInputProvidesData()
        {
            var clientGroup = new MultithreadEventLoopGroup(1);
            var serverGroup = new MultithreadEventLoopGroup(1);
            try
            {
                var serverCompletion = new TaskCompletionSource();

                var serverHandler = new ServerHandler();
                ServerBootstrap sb = new ServerBootstrap()
                    .Group(serverGroup)
                    .Channel<TcpServerSocketChannel>()
                    .ChildHandler(new ActionChannelInitializer<ISocketChannel>(ch =>
                    {
                        // Don't use the HttpServerCodec, because we don't want to have content-length or anything added.
                        ch.Pipeline.AddLast(new HttpRequestDecoder()); //4096, 8192, 8192, true
                        ch.Pipeline.AddLast(new HttpObjectAggregator(4096));
                        ch.Pipeline.AddLast(serverHandler);
                        serverCompletion.TryComplete();
                    }));

                var clientHandler = new ClientHandler();
                Bootstrap cb = new Bootstrap()
                    .Group(clientGroup)
                    .Channel<TcpSocketChannel>()
                    .Handler(new ActionChannelInitializer<ISocketChannel>(ch =>
                    {
                        ch.Pipeline.AddLast(new HttpClientCodec(4096, 8192, 8192, true));
                        ch.Pipeline.AddLast(new HttpObjectAggregator(4096));
                        ch.Pipeline.AddLast(clientHandler);
                    }));
                
                Task<IChannel> task = sb.BindAsync(IPAddress.Loopback, IPEndPoint.MinPort);
                task.Wait(TimeSpan.FromSeconds(5));
                Assert.True(task.Status == TaskStatus.RanToCompletion);
                IChannel serverChannel = task.Result;
                int port = ((IPEndPoint)serverChannel.LocalAddress).Port;

                task = cb.ConnectAsync(IPAddress.Loopback, port);
                task.Wait(TimeSpan.FromSeconds(5));
                Assert.True(task.Status == TaskStatus.RanToCompletion);
                IChannel clientChannel = task.Result;

                serverCompletion.Task.Wait(TimeSpan.FromSeconds(5));
                clientChannel.WriteAndFlushAsync(new DefaultHttpRequest(HttpVersion.Http11, HttpMethod.Get, "/")).Wait(TimeSpan.FromSeconds(1));
                Assert.True(serverHandler.WaitForCompletion());
                Assert.True(clientHandler.WaitForCompletion());
            }
            finally
            {
                Task.WaitAll(
                    clientGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1)),
                    serverGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1)));
            }
        }

        class ClientHandler : SimpleChannelInboundHandler<IFullHttpResponse>
        {
            readonly TaskCompletionSource completion = new TaskCompletionSource();

            public bool WaitForCompletion()
            {
                this.completion.Task.Wait(TimeSpan.FromSeconds(5));
                return this.completion.Task.Status == TaskStatus.RanToCompletion;
            }

            protected override void ChannelRead0(IChannelHandlerContext ctx, IFullHttpResponse msg) =>
                this.completion.TryComplete();

            public override void ExceptionCaught(IChannelHandlerContext context, Exception exception) => 
                this.completion.TrySetException(exception);
        }

        class ServerHandler : SimpleChannelInboundHandler<IFullHttpRequest>
        {
            readonly TaskCompletionSource completion = new TaskCompletionSource();

            public bool WaitForCompletion()
            {
                this.completion.Task.Wait(TimeSpan.FromSeconds(5));
                return this.completion.Task.Status == TaskStatus.RanToCompletion;
            }

            protected override void ChannelRead0(IChannelHandlerContext ctx, IFullHttpRequest msg)
            {
                // This is just a simple demo...don't block in IO
                Assert.IsAssignableFrom<ISocketChannel>(ctx.Channel);

                var sChannel = (ISocketChannel)ctx.Channel;
                /**
                 * The point of this test is to not add any content-length or content-encoding headers
                 * and the client should still handle this.
                 * See <a href="https://tools.ietf.org/html/rfc7230#section-3.3.3">RFC 7230, 3.3.3</a>.
                 */

                sChannel.WriteAndFlushAsync(Unpooled.WrappedBuffer(Encoding.UTF8.GetBytes("HTTP/1.0 200 OK\r\n" + "Date: Fri, 31 Dec 1999 23:59:59 GMT\r\n" + "Content-Type: text/html\r\n\r\n")));
                sChannel.WriteAndFlushAsync(Unpooled.WrappedBuffer(Encoding.UTF8.GetBytes("<html><body>hello half closed!</body></html>\r\n")));
                sChannel.CloseAsync();

                sChannel.CloseCompletion.LinkOutcome(this.completion);
            }

            public override void ExceptionCaught(IChannelHandlerContext context, Exception exception) => this.completion.TrySetException(exception);
        }
    }
}
