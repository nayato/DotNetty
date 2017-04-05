// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http
{
    using System.Diagnostics.Contracts;
    using System.Text;
    using System.Threading.Tasks;
    using DotNetty.Buffers;
    using DotNetty.Common;
    using DotNetty.Common.Internal.Logging;
    using DotNetty.Common.Utilities;
    using DotNetty.Transport.Channels;

    public class HttpObjectAggregator : MessageAggregator<IHttpObject, IHttpMessage, IHttpContent, IFullHttpMessage>
    {
        static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<HttpObjectAggregator>();
        static readonly IFullHttpResponse Continue = new DefaultFullHttpResponse(HttpVersion.Http11, HttpResponseStatus.Continue, Unpooled.Empty);
        static readonly IFullHttpResponse ExpectationFailed = new DefaultFullHttpResponse(HttpVersion.Http11, HttpResponseStatus.ExpectationFailed, Unpooled.Empty);
        static readonly IFullHttpResponse TooLargeClose = new DefaultFullHttpResponse(HttpVersion.Http11, HttpResponseStatus.RequestEntityTooLarge, Unpooled.Empty);
        static readonly IFullHttpResponse TooLarge = new DefaultFullHttpResponse(HttpVersion.Http11, HttpResponseStatus.RequestEntityTooLarge, Unpooled.Empty);
        static readonly AsciiString ZeroLengthString = new AsciiString("0");
        readonly bool closeOnExpectationFailed;

        static HttpObjectAggregator()
        {
            ExpectationFailed.Headers.Set(HttpHeaderNames.ContentLength, ZeroLengthString);
            TooLarge.Headers.Set(HttpHeaderNames.ContentLength, ZeroLengthString);

            TooLargeClose.Headers.Set(HttpHeaderNames.ContentLength, ZeroLengthString);
            TooLargeClose.Headers.Set(HttpHeaderNames.Connection, HttpHeaderValues.Close);
        }

        public HttpObjectAggregator(int maxContentLength, bool closeOnExpectationFailed = false)
            : base(maxContentLength)
        {
            this.closeOnExpectationFailed = closeOnExpectationFailed;
        }

        protected override bool IsStartMessage(IHttpObject msg) => msg is IHttpMessage;

        protected override bool IsContentMessage(IHttpObject msg) => msg is IHttpContent;

        protected override bool IsLastContentMessage(IHttpContent msg) => msg is ILastHttpContent;

        protected override bool IsAggregated(IHttpObject msg) => msg is IFullHttpMessage;

        protected override bool IsContentLengthInvalid(IHttpMessage start, int maxContentLength) => HttpUtil.GetContentLength(start, -1) > maxContentLength;

        protected override object NewContinueResponse(IHttpMessage start, int maxContentLength, IChannelPipeline pipeline)
        {
            if (HttpUtil.IsUnsupportedExpectation(start))
            {
                // if the request contains an unsupported expectation, we return 417
                pipeline.FireUserEventTriggered(HttpExpectationFailedEvent.Default);
                return ExpectationFailed.Duplicate().Retain();
            }
            else if (HttpUtil.Is100ContinueExpected(start))
            {
                // if the request contains 100-continue but the content-length is too large, we return 413
                if (HttpUtil.GetContentLength(start, -1L) <= maxContentLength)
                {
                    return Continue.Duplicate().Retain();
                }
                pipeline.FireUserEventTriggered(HttpExpectationFailedEvent.Default);
                return TooLarge.Duplicate().Retain();
            }

            return null;
        }

        protected override bool CloseAfterContinueResponse(object msg) => this.closeOnExpectationFailed && this.IgnoreContentAfterContinueResponse(msg);

        protected override bool IgnoreContentAfterContinueResponse(object msg)
        {
            var response = msg as IHttpResponse;
            return response != null && response.Status.CodeClass.Equals(HttpStatusClass.ClientError);
        }

        protected override IFullHttpMessage BeginAggregation(IHttpMessage start, IByteBuffer content)
        {
            Contract.Assert(!(start is IFullHttpMessage));

            HttpUtil.SetTransferEncodingChunked(start, false);

            var request = start as IHttpRequest;
            if (request != null)
            {
                return new AggregatedFullHttpRequest(request, content, null);
            }

            var message = start as IHttpResponse;
            if (message != null)
            {
                return new AggregatedFullHttpResponse(message, content, null);
            }

            throw new CodecException($"Invalid type {StringUtil.SimpleClassName(start)} expecting {nameof(IHttpRequest)} or {nameof(IHttpResponse)}");
        }

        protected override void Aggregate(IFullHttpMessage aggregated, IHttpContent content)
        {
            var httpContent = content as ILastHttpContent;
            if (httpContent != null)
            {
                // Merge trailing headers into the message.
                ((AggregatedFullHttpMessage)aggregated).TrailingHeaders = httpContent.TrailingHeaders;
            }
        }

        protected override void FinishAggregation(IFullHttpMessage aggregated)
        {
            // Set the 'Content-Length' header. If one isn't already set.
            // This is important as HEAD responses will use a 'Content-Length' header which
            // does not match the actual body, but the number of bytes that would be
            // transmitted if a GET would have been used.
            //
            // See rfc2616 14.13 Content-Length
            if (!HttpUtil.IsContentLengthSet(aggregated))
            {
                aggregated.Headers.Set(HttpHeaderNames.ContentLength, aggregated.Content.ReadableBytes);
            }
        }

        protected override void HandleOversizedMessage(IChannelHandlerContext ctx, IHttpMessage oversized)
        {
            if (oversized is IHttpRequest)
            {
                // send back a 413 and close the connection

                // If the client started to send data already, close because it's impossible to recover.
                // If keep-alive is off and 'Expect: 100-continue' is missing, no need to leave the connection open.
                if (oversized is IFullHttpMessage ||
                    !HttpUtil.Is100ContinueExpected(oversized) && !HttpUtil.IsKeepAlive(oversized))
                {
                    ctx.WriteAndFlushAsync(TooLargeClose.Duplicate().Retain())
                        .ContinueWith(t =>
                        {
                            if (t.Status != TaskStatus.RanToCompletion)
                            {
                                if (Logger.DebugEnabled)
                                {
                                    Logger.Debug("Failed to send a 413 Request Entity Too Large.", t.Exception);
                                }
                            }

                            ctx.CloseAsync();
                        }, 
                        TaskContinuationOptions.ExecuteSynchronously);
                }
                else
                {
                    ctx.WriteAndFlushAsync(TooLarge.Duplicate().Retain())
                        .ContinueWith(t =>
                        {
                            if (t.Status != TaskStatus.RanToCompletion)
                            {
                                if (Logger.DebugEnabled)
                                {
                                    Logger.Debug("Failed to send a 413 Request Entity Too Large.", t.Exception);
                                }

                                ctx.CloseAsync();
                            }
                        }, 
                        TaskContinuationOptions.ExecuteSynchronously);
                }
                // If an oversized request was handled properly and the connection is still alive
                // (i.e. rejected 100-continue). the decoder should prepare to handle a new message.
                var decoder = ctx.Channel.Pipeline.Get<HttpObjectDecoder>();
                decoder?.Reset();
            }
            else if (oversized is IHttpResponse)
            {
                ctx.CloseAsync();
                throw new TooLongFrameException($"Response entity too large: {oversized}");
            }
            else
            {
                throw new CodecException($"Invalid type {StringUtil.SimpleClassName(oversized)}, expecting {nameof(IHttpRequest)} or {nameof(IHttpResponse)}");
            }
        }

        abstract class AggregatedFullHttpMessage : IFullHttpMessage
        {
            protected readonly IHttpMessage Message;
            HttpHeaders trailingHeaders;

            protected AggregatedFullHttpMessage(IHttpMessage message, IByteBuffer content, HttpHeaders trailingHeaders)
            {
                this.Message = message;
                this.Content = content;
                this.trailingHeaders = trailingHeaders;
            }

            public HttpHeaders TrailingHeaders
            {
                get
                {
                    HttpHeaders headers = this.trailingHeaders;
                    return headers ?? EmptyHttpHeaders.Default;
                }
                internal set => this.trailingHeaders = value;
            }

            public HttpVersion ProtocolVersion
            {
                get => this.Message.ProtocolVersion;
                set => this.Message.ProtocolVersion = value;
            }

            public HttpHeaders Headers => this.Message.Headers;

            public DecoderResult Result
            {
                get => this.Message.Result;
                set => this.Message.Result = value;
            }

            public IByteBuffer Content { get; }

            public int ReferenceCount => this.Content.ReferenceCount;

            public IReferenceCounted Retain()
            {
                this.Content.Retain();
                return this;
            }

            public IReferenceCounted Retain(int increment)
            {
                this.Content.Retain(increment);
                return this;
            }

            public IReferenceCounted Touch()
            {
                this.Content.Touch();
                return this;
            }

            public IReferenceCounted Touch(object hint)
            {
                this.Content.Touch(hint);
                return this;
            }

            public bool Release() => this.Content.Release();

            public bool Release(int decrement) => this.Content.Release(decrement);


            public abstract IByteBufferHolder Copy();

            public abstract IByteBufferHolder Duplicate();

        }

        sealed class AggregatedFullHttpRequest : AggregatedFullHttpMessage, IFullHttpRequest
        {
            public AggregatedFullHttpRequest(IHttpMessage message, IByteBuffer content, HttpHeaders trailingHeaders)
                : base(message, content, trailingHeaders)
            {
            }

            public override IByteBufferHolder Copy() => this.Replace(this.Content.Copy());

            public override IByteBufferHolder Duplicate() => this.Replace(this.Content.Duplicate());

            public HttpMethod Method
            {
                get => ((IHttpRequest)this.Message).Method;
                set => ((IHttpRequest)this.Message).Method = value;
            }

            public string Uri
            {
                get => ((IHttpRequest)this.Message).Uri;
                set => ((IHttpRequest)this.Message).Uri = value;
            }

            public IFullHttpRequest Replace(IByteBuffer content)
            {
                var dup = new DefaultFullHttpRequest(this.ProtocolVersion, this.Method, this.Uri, content);
                dup.Headers.Set(this.Headers);
                dup.TrailingHeaders.Set(this.TrailingHeaders);
                dup.Result = this.Result;

                return dup;
            }

            public override string ToString() => HttpMessageUtil.AppendFullRequest(new StringBuilder(256), this).ToString();
        }

        sealed class AggregatedFullHttpResponse : AggregatedFullHttpMessage, IFullHttpResponse
        {
            public AggregatedFullHttpResponse(IHttpMessage message, IByteBuffer content, HttpHeaders trailingHeaders)
                : base(message, content, trailingHeaders)
            {
            }

            public override IByteBufferHolder Copy() => this.Replace(this.Content.Copy());

            public override IByteBufferHolder Duplicate() => this.Replace(this.Content.Duplicate());

            public IFullHttpResponse Replace(IByteBuffer content)
            {
                var dup = new DefaultFullHttpResponse(this.ProtocolVersion, this.Status, content);
                dup.Headers.Set(this.Headers);
                dup.TrailingHeaders.Set(this.TrailingHeaders);
                dup.Result = this.Result;
                return dup;
            }

            public HttpResponseStatus Status
            {
                get => ((IHttpResponse)this.Message).Status;
                set => ((IHttpResponse)this.Message).Status = value;
            }
        }
    }
}
