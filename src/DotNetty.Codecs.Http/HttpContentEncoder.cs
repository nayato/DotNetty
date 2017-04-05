// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using DotNetty.Buffers;
    using DotNetty.Common.Utilities;
    using DotNetty.Transport.Channels;
    using DotNetty.Transport.Channels.Embedded;

    public abstract class HttpContentEncoder : MessageToMessageCodec<IHttpRequest, IHttpObject>
    {
        enum State
        {
            PassThrough,
            AwaitHeaders,
            AwaitContent
        }

        static readonly ICharSequence ZeroLengthHead = new AsciiString("HEAD");
        static readonly ICharSequence ZeroLengthConnect = new AsciiString("CONNECT");
        static readonly int ContinueCode = HttpResponseStatus.Continue.Code;

        readonly Queue<ICharSequence> acceptEncodingQueue = new Queue<ICharSequence>();
        EmbeddedChannel encoder;
        State state = State.AwaitHeaders;

        public override bool AcceptOutboundMessage(object msg) => msg is IHttpContent || msg is IHttpResponse;

        protected override void Decode(IChannelHandlerContext ctx, IHttpRequest msg, List<object> output)
        {
            ICharSequence acceptedEncoding = msg.Headers.Get(HttpHeaderNames.AcceptEncoding)
                ?? HttpContentDecoder.Identity;

            HttpMethod meth = msg.Method;
            if (Equals(meth, HttpMethod.Head))
            {
                acceptedEncoding = ZeroLengthHead;
            }
            else if (Equals(meth, HttpMethod.Connect))
            {
                acceptedEncoding = ZeroLengthConnect;
            }

            this.acceptEncodingQueue.Enqueue(acceptedEncoding);
            output.Add(ReferenceCountUtil.Retain(msg));
        }

        protected override void Encode(IChannelHandlerContext ctx, IHttpObject msg, List<object> output)
        {
            bool isFull = msg is IHttpResponse && msg is ILastHttpContent;

            if (this.state == State.AwaitHeaders)
            {
                EnsureHeaders(msg);

                var res = (IHttpResponse)msg;
                int code = res.Status.Code;
                ICharSequence acceptEncoding;
                if (code == ContinueCode)
                {
                    // We need to not poll the encoding when response with CONTINUE as another response will follow
                    // for the issued request. See https://github.com/netty/netty/issues/4079
                    acceptEncoding = null;
                }
                else
                {
                    // Get the list of encodings accepted by the peer.
                    acceptEncoding = this.acceptEncodingQueue.Count > 0 ? this.acceptEncodingQueue.Dequeue() : null;
                    if (acceptEncoding == null)
                    {
                        throw new InvalidOperationException("cannot send more responses than requests");
                    }
                }

                //
                // per rfc2616 4.3 Message Body
                // All 1xx (informational), 204 (no content), and 304 (not modified) responses MUST NOT include a
                // message-body. All other responses do include a message-body, although it MAY be of zero length.
                //
                // 9.4 HEAD
                // The HEAD method is identical to GET except that the server MUST NOT return a message-body
                // in the response.
                //
                // Also we should pass through HTTP/1.0 as transfer-encoding: chunked is not supported.
                //
                // See https://github.com/netty/netty/issues/5382
                //
                if (IsPassthru(res.ProtocolVersion, code, acceptEncoding))
                {
                    if (isFull)
                    {
                        output.Add(ReferenceCountUtil.Retain(res));
                    }
                    else
                    {
                        output.Add(res);
                        // Pass through all following contents.
                        this.state = State.PassThrough;
                    }
                    return;
                }

                if (isFull)
                {
                    // Pass through the full response with empty content and continue waiting for the the next resp.
                    if (!((IByteBufferHolder)res).Content.IsReadable())
                    {
                        output.Add(ReferenceCountUtil.Retain(res));
                        return;
                    }
                }

                // Prepare to encode the content.
                Result result = this.BeginEncode(res, acceptEncoding);

                // If unable to encode, pass through.
                if (result == null)
                {
                    if (isFull)
                    {
                        output.Add(ReferenceCountUtil.Retain(res));
                    }
                    else
                    {
                        output.Add(res);
                        // Pass through all following contents.
                        this.state = State.PassThrough;
                    }
                    return;
                }

                this.encoder = result.ContentEncoder;

                // Encode the content and remove or replace the existing headers
                // so that the message looks like a decoded message.
                res.Headers.Set(HttpHeaderNames.ContentEncoding, result.TargetContentEncoding);

                // Make the response chunked to simplify content transformation.
                res.Headers.Remove(HttpHeaderNames.ContentLength);
                res.Headers.Set(HttpHeaderNames.TransferEncoding, HttpHeaderValues.Chunked);

                // Output the rewritten response.
                if (isFull)
                {
                    // Convert full message into unfull one.
                    var newRes = new DefaultHttpResponse(res.ProtocolVersion, res.Status);
                    newRes.Headers.Set(res.Headers);
                    output.Add(newRes);
                    // Fall through to encode the content of the full response.
                }
                else
                {
                    output.Add(res);
                    this.state = State.AwaitContent;

                    if (!(msg is IHttpContent)) {
                        // only break out the switch statement if we have not content to process
                        // See https://github.com/netty/netty/issues/2006
                        return;
                    }
                    // Fall through to encode the content
                }
            }
            if (this.state == State.AwaitContent || isFull)
            {
                EnsureContent(msg);
                if (this.EncodeContent((IHttpContent)msg, output))
                {
                    this.state = State.AwaitHeaders;
                }
                return;
            }
            if (this.state == State.PassThrough)
            {
                EnsureContent(msg);
                output.Add(ReferenceCountUtil.Retain(msg));
                // Passed through all following contents of the current response.
                if (msg is ILastHttpContent)
                {
                    this.state = State.AwaitHeaders;
                }
            }
        }

        static bool IsPassthru(HttpVersion version, int code, ICharSequence httpMethod) =>
            code < 200 || code == 204 || code == 304
            || Equals(httpMethod, ZeroLengthHead)
            || Equals(httpMethod, ZeroLengthConnect) && code == 200
            || Equals(version, HttpVersion.Http10);

        static void EnsureHeaders(IHttpObject msg)
        {
            if (!(msg is IHttpResponse))
            {
                throw new CodecException($"unexpected message type: {msg.GetType().Name} (expected: {StringUtil.SimpleClassName<IHttpResponse>()})");
            }
        }

        static void EnsureContent(IHttpObject msg)
        {
            if (!(msg is IHttpContent))
            {
                throw new CodecException($"unexpected message type: {msg.GetType().Name} (expected: {StringUtil.SimpleClassName<IHttpContent>()})");
            }
        }

        bool EncodeContent(IHttpContent c, ICollection<object> output)
        {
            IByteBuffer content = c.Content;

            this.Encode(content, output);

            var httpContent = c as ILastHttpContent;
            if (httpContent == null)
            {
                return false;
            }

            this.FinishEncode(output);

            // Generate an additional chunk if the decoder produced
            // the last product on closure,
            HttpHeaders headers = httpContent.TrailingHeaders;
            if (headers.IsEmpty)
            {
                output.Add(EmptyLastHttpContent.Default);
            }
            else
            {
                output.Add(new ComposedLastHttpContent(headers));
            }

            return true;
        }

        protected abstract Result BeginEncode(IHttpResponse headers, ICharSequence acceptEncoding);

        public override void HandlerRemoved(IChannelHandlerContext context)
        {
            this.Cleanup();
            base.HandlerRemoved(context);
        }

        public override void ChannelInactive(IChannelHandlerContext context)
        {
            this.Cleanup();
            base.ChannelInactive(context);
        }

        void Cleanup()
        {
            if (this.encoder == null)
            {
                return;
            }

            // Clean-up the previous encoder if not cleaned up correctly.
            if (this.encoder.Finish())
            {
                for (;;)
                {
                    var buf = this.encoder.ReadOutbound<IByteBuffer>();
                    if (buf == null)
                    {
                        break;
                    }
                    // Release the buffer
                    // https://github.com/netty/netty/issues/1524
                    buf.Release();
                }
            }

            this.encoder = null;
        }

        void Encode(IByteBuffer buf, ICollection<object> output)
        {
            // call retain here as it will call release after its written to the channel
            this.encoder.WriteOutbound(buf.Retain());
            this.FetchEncoderOutput(output);
        }

        void FinishEncode(ICollection<object> output)
        {
            if (this.encoder.Finish())
            {
                this.FetchEncoderOutput(output);
            }

            this.encoder = null;
        }

        void FetchEncoderOutput(ICollection<object> output)
        {
            for (;;)
            {
                var buf = this.encoder.ReadOutbound<IByteBuffer>();
                if (buf == null)
                {
                    break;
                }
                if (!buf.IsReadable())
                {
                    buf.Release();
                    continue;
                }
                output.Add(new DefaultHttpContent(buf));
            }
        }

        public sealed class Result
        {
            public Result(ICharSequence targetContentEncoding, EmbeddedChannel contentEncoder)
            {
                Contract.Requires(targetContentEncoding != null);
                Contract.Requires(contentEncoder != null);

                this.TargetContentEncoding = targetContentEncoding;
                this.ContentEncoder = contentEncoder;
            }

            public ICharSequence TargetContentEncoding { get; }

            public EmbeddedChannel ContentEncoder { get; }
        }
    }
}
