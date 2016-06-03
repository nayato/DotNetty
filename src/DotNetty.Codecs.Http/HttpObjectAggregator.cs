// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http
{
    using System.Diagnostics.Contracts;
    using System.Text;
    using DotNetty.Buffers;
    using DotNetty.Common.Internal.Logging;
    using DotNetty.Transport.Channels;

    /**
 * A {@link ChannelHandler} that aggregates an {@link HttpMessage}
 * and its following {@link HttpContent}s into a single {@link FullHttpRequest}
 * or {@link FullHttpResponse} (depending on if it used to handle requests or responses)
 * with no following {@link HttpContent}s.  It is useful when you don't want to take
 * care of HTTP messages whose transfer encoding is 'chunked'.  Insert this
 * handler after {@link HttpObjectDecoder} in the {@link ChannelPipeline}:
 * <pre>
 * {@link ChannelPipeline} p = ...;
 * ...
 * p.addLast("encoder", new {@link HttpResponseEncoder}());
 * p.addLast("decoder", new {@link HttpRequestDecoder}());
 * p.addLast("aggregator", <b>new {@link HttpObjectAggregator}(1048576)</b>);
 * ...
 * p.addLast("handler", new HttpRequestHandler());
 * </pre>
 * Be aware that you need to have the {@link HttpResponseEncoder} or {@link HttpRequestEncoder}
 * before the {@link HttpObjectAggregator} in the {@link ChannelPipeline}.
 */

    public class HttpObjectAggregator
        : MessageAggregator<HttpObject, HttpMessage, HttpContent, FullHttpMessage>
    {
        static readonly IInternalLogger logger = InternalLoggerFactory.GetInstance<HttpObjectAggregator>();

        static readonly FullHttpResponse CONTINUE =
            new DefaultFullHttpResponse(HttpVersion.HTTP_1_1, HttpResponseStatus.CONTINUE, Unpooled.EMPTY_BUFFER);

        static readonly FullHttpResponse EXPECTATION_FAILED = new DefaultFullHttpResponse(
            HttpVersion.HTTP_1_1, HttpResponseStatus.EXPECTATION_FAILED, Unpooled.EMPTY_BUFFER);

        static readonly FullHttpResponse TOO_LARGE = new DefaultFullHttpResponse(
            HttpVersion.HTTP_1_1, HttpResponseStatus.REQUEST_ENTITY_TOO_LARGE, Unpooled.EMPTY_BUFFER);

        static HttpObjectAggregator()
        {
            EXPECTATION_FAILED.headers().set(CONTENT_LENGTH, 0);
            TOO_LARGE.headers().set(CONTENT_LENGTH, 0);
        }

        readonly bool closeOnExpectationFailed;

        /**
         * Creates a new instance.
         * @param maxContentLength the maximum length of the aggregated content in bytes.
         * If the length of the aggregated content exceeds this value,
         * {@link #handleOversizedMessage(IChannelHandlerContext, HttpMessage)} will be called.
         */

        public HttpObjectAggregator(int maxContentLength)
        {
            this(maxContentLength, false);
        }

        /**
         * Creates a new instance.
         * @param maxContentLength the maximum length of the aggregated content in bytes.
         * If the length of the aggregated content exceeds this value,
         * {@link #handleOversizedMessage(IChannelHandlerContext, HttpMessage)} will be called.
         * @param closeOnExpectationFailed If a 100-continue response is detected but the content length is too large
         * then {@code true} means close the connection. otherwise the connection will remain open and data will be
         * consumed and discarded until the next request is received.
         */

        public HttpObjectAggregator(int maxContentLength, bool closeOnExpectationFailed)
        {
            base(maxContentLength);
            this.closeOnExpectationFailed = closeOnExpectationFailed;
        }

        // @Override
        protected bool isStartMessage(HttpObject msg)
        {
            return msg is HttpMessage;
        }

        // @Override
        protected bool isContentMessage(HttpObject msg)
        {
            return msg is HttpContent;
        }

        // @Override
        protected bool isLastContentMessage(HttpContent msg)
        {
            return msg is LastHttpContent;
        }

        // @Override
        protected bool isAggregated(HttpObject msg)
        {
            return msg is FullHttpMessage;
        }

        // @Override
        protected bool isContentLengthInvalid(HttpMessage start, int maxContentLength)
        {
            return getContentLength(start, -1L) > maxContentLength;
        }

        // @Override
        protected object newContinueResponse(HttpMessage start, int maxContentLength, ChannelPipeline pipeline)
        {
            if (HttpUtil.is100ContinueExpected(start))
            {
                if (getContentLength(start, -1L) <= maxContentLength)
                {
                    return CONTINUE.retainedDuplicate();
                }

                pipeline.fireUserEventTriggered(HttpExpectationFailedEvent.INSTANCE);
                return EXPECTATION_FAILED.retainedDuplicate();
            }
            return null;
        }

        // @Override
        protected bool closeAfterContinueResponse(object msg)
        {
            return this.closeOnExpectationFailed && this.ignoreContentAfterContinueResponse(msg);
        }

        // @Override
        protected bool ignoreContentAfterContinueResponse(object msg)
        {
            return msg is HttpResponse &&
                (((HttpResponse)msg).status().code() == HttpResponseStatus.EXPECTATION_FAILED.code());
        }

        // @Override
        protected FullHttpMessage beginAggregation(HttpMessage start, IByteBuffer content)
        {
            Contract.Assert(!(start is FullHttpMessage));

            HttpUtil.setTransferEncodingChunked(start, false);

            AggregatedFullHttpMessage ret;
            if (start is HttpRequest)
            {
                ret = new AggregatedFullHttpRequest((HttpRequest)start, content, null);
            }
            else if (start is HttpResponse)
            {
                ret = new AggregatedFullHttpResponse((HttpResponse)start, content, null);
            }
            else
            {
                throw new Error();
            }
            return ret;
        }

        // @Override
        protected void aggregate(FullHttpMessage aggregated, HttpContent content)
        {
            if (content is LastHttpContent)
            {
                // Merge trailing headers into the message.
                ((AggregatedFullHttpMessage)aggregated).setTrailingHeaders(((LastHttpContent)content).trailingHeaders());
            }
        }

        // @Override
        protected void finishAggregation(FullHttpMessage aggregated)
        {
            // Set the 'Content-Length' header. If one isn't already set.
            // This is important as HEAD responses will use a 'Content-Length' header which
            // does not match the actual body, but the number of bytes that would be
            // transmitted if a GET would have been used.
            //
            // See rfc2616 14.13 Content-Length
            if (!HttpUtil.isContentLengthSet(aggregated))
            {
                aggregated.headers().set(
                    CONTENT_LENGTH,
                    string.valueOf(aggregated.content().readableBytes()));
            }
        }

        // @Override
        protected void handleOversizedMessage(IChannelHandlerContext ctx, HttpMessage oversized)
        {
            if (oversized is HttpRequest)
            {
                // send back a 413 and close the connection
                ChannelFuture future = ctx.writeAndFlush(TOO_LARGE.retainedDuplicate()).addListener(
                    new ChannelFutureListener()
                    {
                        // @Override
                        public void operationComplete(ChannelFuture future)  {
                        if (!future.isSuccess()) {
                        logger.debug("Failed to send a 413 Request Entity Too Large.",
                        future.cause());
                        ctx.close();
                    }
                }
                }
                )
                ;

                // If the client started to send data already, close because it's impossible to recover.
                // If keep-alive is off and 'Expect: 100-continue' is missing, no need to leave the connection open.
                if (oversized is FullHttpMessage ||
                    (!HttpUtil.is100ContinueExpected(oversized) && !HttpUtil.isKeepAlive(oversized)))
                {
                    future.addListener(ChannelFutureListener.CLOSE);
                }

                // If an oversized request was handled properly and the connection is still alive
                // (i.e. rejected 100-continue). the decoder should prepare to handle a new message.
                HttpObjectDecoder decoder = ctx.pipeline().get(HttpObjectDecoder.class)
                ;
                if (decoder != null)
                {
                    decoder.reset();
                }
            }
            else if (oversized is HttpResponse)
            {
                ctx.close();
                throw new TooLongFrameException("Response entity too large: " + oversized);
            }
            else
            {
                throw new IllegalStateException();
            }
        }

        abstract static class AggregatedFullHttpMessage : FullHttpMessage
        {
            protected readonly HttpMessage message;
            readonly IByteBuffer content;
            HttpHeaders trailingHeaders;

            AggregatedFullHttpMessage(HttpMessage message, IByteBuffer content, HttpHeaders trailingHeaders)
            {
                this.message = message;
                this.content = content;
                this.trailingHeaders = trailingHeaders;
            }

            // @Override
            public HttpHeaders trailingHeaders()
            {
                HttpHeaders trailingHeaders = this.trailingHeaders;
                if (trailingHeaders == null)
                {
                    return EmptyHttpHeaders.INSTANCE;
                }
                else
                {
                    return trailingHeaders;
                }
            }

            void setTrailingHeaders(HttpHeaders trailingHeaders)
            {
                this.trailingHeaders = trailingHeaders;
            }

            // @Override
            public HttpVersion getProtocolVersion()
            {
                return this.message.protocolVersion();
            }

            // @Override
            public HttpVersion protocolVersion()
            {
                return this.message.protocolVersion();
            }

            // @Override
            public FullHttpMessage setProtocolVersion(HttpVersion version)
            {
                this.message.setProtocolVersion(version);
                return this;
            }

            // @Override
            public HttpHeaders headers()
            {
                return this.message.headers();
            }

            // @Override
            public DecoderResult decoderResult()
            {
                return this.message.decoderResult();
            }

            // @Override
            public DecoderResult getDecoderResult()
            {
                return this.message.decoderResult();
            }

            // @Override
            public void setDecoderResult(DecoderResult result)
            {
                this.message.setDecoderResult(result);
            }

            // @Override
            public IByteBuffer content()
            {
                return content;
            }

            // @Override
            public int refCnt()
            {
                return content.refCnt();
            }

            // @Override
            public FullHttpMessage retain()
            {
                content.retain();
                return this;
            }

            // @Override
            public FullHttpMessage retain(int increment)
            {
                content.retain(increment);
                return this;
            }

            // @Override
            public FullHttpMessage touch(object hint)
            {
                content.touch(hint);
                return this;
            }

            // @Override
            public FullHttpMessage touch()
            {
                content.touch();
                return this;
            }

            // @Override
            public bool release()
            {
                return content.release();
            }

            // @Override
            public bool release(int decrement)
            {
                return content.release(decrement);
            }

            // @Override
            public abstract FullHttpMessage copy();

            // @Override
            public abstract FullHttpMessage duplicate();

            // @Override
            public abstract FullHttpMessage retainedDuplicate();
        }

        sealed static class AggregatedFullHttpRequest : AggregatedFullHttpMessage, FullHttpRequest
        {
            AggregatedFullHttpRequest(HttpRequest request, IByteBuffer content, HttpHeaders trailingHeaders)
            {
                base(request, content, trailingHeaders);
            }

            // @Override
            public FullHttpRequest copy()
            {
                return this.replace(this.content().copy());
            }

            // @Override
            public FullHttpRequest duplicate()
            {
                return this.replace(this.content().duplicate());
            }

            // @Override
            public FullHttpRequest retainedDuplicate()
            {
                return this.replace(this.content().retainedDuplicate());
            }

            // @Override
            public FullHttpRequest replace(IByteBuffer content)
            {
                var dup = new DefaultFullHttpRequest(this.protocolVersion(), this.method(), this.uri(), content);
                dup.headers().set(this.headers());
                dup.trailingHeaders().set(this.trailingHeaders());
                return dup;
            }

            // @Override
            public FullHttpRequest retain(int increment)
            {
                base.retain(increment);
                return this;
            }

            // @Override
            public FullHttpRequest retain()
            {
                base.retain();
                return this;
            }

            // @Override
            public FullHttpRequest touch()
            {
                base.touch();
                return this;
            }

            // @Override
            public FullHttpRequest touch(object hint)
            {
                base.touch(hint);
                return this;
            }

            // @Override
            public FullHttpRequest setMethod(HttpMethod method)
            {
                ((HttpRequest)this.message).setMethod(method);
                return this;
            }

            // @Override
            public FullHttpRequest setUri(string uri)
            {
                ((HttpRequest)this.message).setUri(uri);
                return this;
            }

            // @Override
            public HttpMethod getMethod()
            {
                return ((HttpRequest)this.message).method();
            }

            // @Override
            public string getUri()
            {
                return ((HttpRequest)this.message).uri();
            }

            // @Override
            public HttpMethod method()
            {
                return this.getMethod();
            }

            // @Override
            public string uri()
            {
                return this.getUri();
            }

            // @Override
            public FullHttpRequest setProtocolVersion(HttpVersion version)
            {
                base.setProtocolVersion(version);
                return this;
            }

            // @Override
            public string toString()
            {
                return HttpMessageUtil.appendFullRequest(new StringBuilder(256), this).ToString();
            }
        }

        sealed static class AggregatedFullHttpResponse : AggregatedFullHttpMessage, FullHttpResponse
        {
            AggregatedFullHttpResponse(HttpResponse message, IByteBuffer content, HttpHeaders trailingHeaders)
            {
                base(message, content, trailingHeaders);
            }

            // @Override
            public FullHttpResponse copy()
            {
                return this.replace(this.content().copy());
            }

            // @Override
            public FullHttpResponse duplicate()
            {
                return this.replace(this.content().duplicate());
            }

            // @Override
            public FullHttpResponse retainedDuplicate()
            {
                return this.replace(this.content().retainedDuplicate());
            }

            // @Override
            public FullHttpResponse replace(IByteBuffer content)
            {
                var dup = new DefaultFullHttpResponse(this.getProtocolVersion(), this.getStatus(), content);
                dup.headers().set(this.headers());
                dup.trailingHeaders().set(this.trailingHeaders());
                return dup;
            }

            // @Override
            public FullHttpResponse setStatus(HttpResponseStatus status)
            {
                ((HttpResponse)this.message).setStatus(status);
                return this;
            }

            // @Override
            public HttpResponseStatus getStatus()
            {
                return ((HttpResponse)this.message).status();
            }

            // @Override
            public HttpResponseStatus status()
            {
                return this.getStatus();
            }

            // @Override
            public FullHttpResponse setProtocolVersion(HttpVersion version)
            {
                base.setProtocolVersion(version);
                return this;
            }

            // @Override
            public FullHttpResponse retain(int increment)
            {
                base.retain(increment);
                return this;
            }

            // @Override
            public FullHttpResponse retain()
            {
                base.retain();
                return this;
            }

            // @Override
            public FullHttpResponse touch(object hint)
            {
                base.touch(hint);
                return this;
            }

            // @Override
            public FullHttpResponse touch()
            {
                base.touch();
                return this;
            }

            // @Override
            public string ToString()
            {
                return HttpMessageUtil.appendFullResponse(new StringBuilder(256), this).ToString();
            }
        }
    }
}