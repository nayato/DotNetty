// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http
{
    using System;
    using System.Collections.Generic;
    using DotNetty.Buffers;
    using DotNetty.Common.Utilities;
    using DotNetty.Transport.Channels;
    using DotNetty.Transport.Channels.Embedded;

    /**
 * Encodes the content of the outbound {@link HttpResponse} and {@link HttpContent}.
 * The original content is replaced with the new content encoded by the
 * {@link EmbeddedChannel}, which is created by {@link #beginEncode(HttpResponse, string)}.
 * Once encoding is finished, the value of the <tt>'Content-Encoding'</tt> header
 * is set to the target content encoding, as returned by
 * {@link #beginEncode(HttpResponse, string)}.
 * Also, the <tt>'Content-Length'</tt> header is updated to the length of the
 * encoded content.  If there is no supported or allowed encoding in the
 * corresponding {@link HttpRequest}'s {@code "Accept-Encoding"} header,
 * {@link #beginEncode(HttpResponse, string)} should return {@code null} so that
 * no encoding occurs (i.e. pass-through).
 * <p>
 * Please note that this is an abstract class.  You have to extend this class
 * and implement {@link #beginEncode(HttpResponse, string)} properly to make
 * this class functional.  For example, refer to the source code of
 * {@link HttpContentCompressor}.
 * <p>
 * This handler must be placed after {@link HttpObjectEncoder} in the pipeline
 * so that this handler can intercept HTTP responses before {@link HttpObjectEncoder}
 * converts them into {@link IByteBuffer}s.
 */

    public abstract class HttpContentEncoder : MessageToMessageCodec<HttpRequest, HttpObject>
    {
        enum State
        {
            PASS_THROUGH,
            AWAIT_HEADERS,
            AWAIT_CONTENT
        }

        static readonly CharSequence ZERO_LENGTH_HEAD = new StringCharSequence("HEAD");
        static readonly CharSequence ZERO_LENGTH_CONNECT = new StringCharSequence("CONNECT");
        static readonly int CONTINUE_CODE = HttpResponseStatus.CONTINUE.code();

        readonly Queue<CharSequence> acceptEncodingQueue = new ArrayDeque<CharSequence>();
        CharSequence acceptEncoding;
        EmbeddedChannel encoder;
        State state = State.AWAIT_HEADERS;

        // @Override
        public bool acceptOutboundMessage(object msg)
        {
            return msg is HttpContent || msg is HttpResponse;
        }

        // @Override
        protected void decode(IChannelHandlerContext ctx, HttpRequest msg, List<object> output)
        {
            CharSequence acceptedEncoding = msg.headers().get(HttpHeaderNames.ACCEPT_ENCODING);
            if (acceptedEncoding == null)
            {
                acceptedEncoding = HttpContentDecoder.IDENTITY;
            }

            HttpMethod meth = msg.method();
            if (meth == HttpMethod.HEAD)
            {
                acceptedEncoding = ZERO_LENGTH_HEAD;
            }
            else if (meth == HttpMethod.CONNECT)
            {
                acceptedEncoding = ZERO_LENGTH_CONNECT;
            }

            this.acceptEncodingQueue.add(acceptedEncoding);
            output.add(ReferenceCountUtil.retain(msg));
        }

        // @Override
        protected void encode(IChannelHandlerContext ctx, HttpObject msg, List<object> out)
        {
            readonly
            bool isFull = msg is HttpResponse && msg is LastHttpContent;
            switch (this.state)
            {
                case AWAIT_HEADERS:
                {
                    ensureHeaders(msg);
                    assert
                    this.encoder == null;

                        readonly
                    var res = (HttpResponse)msg;
                        readonly
                    int code = res.status().code();
                    if (code == CONTINUE_CODE)
                    {
                        // We need to not poll the encoding when response with CONTINUE as another response will follow
                        // for the issued request. See https://github.com/netty/netty/issues/4079
                        this.acceptEncoding = null;
                    }
                    else
                    {
                        // Get the list of encodings accepted by the peer.
                        this.acceptEncoding = this.acceptEncodingQueue.poll();
                        if (this.acceptEncoding == null)
                        {
                            throw new IllegalStateException("cannot send more responses than requests");
                        }
                    }

                    /*
                     * per rfc2616 4.3 Message Body
                     * All 1xx (informational), 204 (no content), and 304 (not modified) responses MUST NOT include a
                     * message-body. All other responses do include a message-body, although it MAY be of zero length.
                     *
                     * 9.4 HEAD
                     * The HEAD method is identical to GET except that the server MUST NOT return a message-body
                     * in the response.
                     *
                     * This code is now inline with HttpClientDecoder.Decoder
                     */
                    if (isPassthru(code, this.acceptEncoding))
                    {
                        if (isFull)
                        {
                            out.
                            add(ReferenceCountUtil.retain(res));
                        }
                        else
                        {
                            out.
                            add(res);
                            // Pass through all following contents.
                            this.state = State.PASS_THROUGH;
                        }
                        break;
                    }

                    if (isFull)
                    {
                        // Pass through the full response with empty content and continue waiting for the the next resp.
                        if (!((IByteBufferHolder)res).content().isReadable())
                        {
                            out.
                            add(ReferenceCountUtil.retain(res));
                            break;
                        }
                    }

                    // Prepare to encode the content.
                    readonly
                    Result result = this.beginEncode(res, this.acceptEncoding.ToString());

                    // If unable to encode, pass through.
                    if (result == null)
                    {
                        if (isFull)
                        {
                            out.
                            add(ReferenceCountUtil.retain(res));
                        }
                        else
                        {
                            out.
                            add(res);
                            // Pass through all following contents.
                            this.state = State.PASS_THROUGH;
                        }
                        break;
                    }

                    this.encoder = result.contentEncoder();

                    // Encode the content and remove or replace the existing headers
                    // so that the message looks like a decoded message.
                    res.headers().set(HttpHeaderNames.CONTENT_ENCODING, result.targetContentEncoding());

                    // Make the response chunked to simplify content transformation.
                    res.headers().remove(HttpHeaderNames.CONTENT_LENGTH);
                    res.headers().set(HttpHeaderNames.TRANSFER_ENCODING, HttpHeaderValues.CHUNKED);

                    // Output the rewritten response.
                    if (isFull)
                    {
                        // Convert full message into unfull one.
                        HttpResponse newRes = new DefaultHttpResponse(res.protocolVersion(), res.status());
                        newRes.headers().set(res.headers());
                            out.
                        add(newRes);
                        // Fall through to encode the content of the full response.
                    }
                    else
                    {
                        out.
                        add(res);
                        this.state = State.AWAIT_CONTENT;
                        if (!(msg is HttpContent))
                        {
                            // only break out the switch statement if we have not content to process
                            // See https://github.com/netty/netty/issues/2006
                            break;
                        }
                        // Fall through to encode the content
                    }
                }
                case AWAIT_CONTENT:
                {
                    ensureContent(msg);
                    if (encodeContent((HttpContent)msg, out ))
                    {
                        this.state = State.AWAIT_HEADERS;
                    }
                    break;
                }
                case PASS_THROUGH:
                {
                    ensureContent(msg);
                        out.
                    add(ReferenceCountUtil.retain(msg));
                    // Passed through all following contents of the current response.
                    if (msg is LastHttpContent)
                    {
                        this.state = State.AWAIT_HEADERS;
                    }
                    break;
                }
            }
        }

        static bool isPassthru(int code, CharSequence httpMethod)
        {
            return (code < 200) || (code == 204) || (code == 304) ||
                ((httpMethod == ZERO_LENGTH_HEAD) || ((httpMethod == ZERO_LENGTH_CONNECT) && (code == 200)));
        }

        static void ensureHeaders(HttpObject msg)
        {
            if (!(msg is HttpResponse))
            {
                throw new IllegalStateException(
                    "unexpected message type: " +
                    msg.getClass().getName() + " (expected: " + HttpResponse. class.
                getSimpleName() + ')')
                ;
            }
        }

        static void ensureContent(HttpObject msg)
        {
            if (!(msg is HttpContent))
            {
                throw new IllegalStateException(
                    "unexpected message type: " +
                    msg.getClass().getName() + " (expected: " + HttpContent. class.
                getSimpleName() + ')')
                ;
            }
        }

        bool encodeContent(HttpContent c, List<object> output)
        {
            IByteBuffer content = c.content();

            this.encode(content, output);

            if (c is LastHttpContent)
            {
                this.finishEncode(output);
                var last = (LastHttpContent)c;

                // Generate an additional chunk if the decoder produced
                // the last product on closure,
                HttpHeaders headers = last.trailingHeaders();
                if (headers.isEmpty())
                {
                    output.add(LastHttpContent.EMPTY_LAST_CONTENT);
                }
                else
                {
                    output.add(new ComposedLastHttpContent(headers));
                }
                return true;
            }
            return false;
        }

        /**
         * Prepare to encode the HTTP message content.
         *
         * @param headers
         *        the headers
         * @param acceptEncoding
         *        the value of the {@code "Accept-Encoding"} header
         *
         * @return the result of preparation, which is composed of the determined
         *         target content encoding and a new {@link EmbeddedChannel} that
         *         encodes the content into the target content encoding.
         *         {@code null} if {@code acceptEncoding} is unsupported or rejected
         *         and thus the content should be handled as-is (i.e. no encoding).
         */

        protected abstract Result beginEncode(HttpResponse headers, string acceptEncoding);

        // @Override
        public void handlerRemoved(IChannelHandlerContext ctx)
        {
            this.cleanup();
            base.handlerRemoved(ctx);
        }

        // @Override
        public void channelInactive(IChannelHandlerContext ctx)
        {
            this.cleanup();
            base.channelInactive(ctx);
        }

        void cleanup()
        {
            if (this.encoder != null)
            {
                // Clean-up the previous encoder if not cleaned up correctly.
                if (this.encoder.finish())
                {
                    for (;;)
                    {
                        IByteBuffer buf = this.encoder.readOutbound();
                        if (buf == null)
                        {
                            break;
                        }
                        // Release the buffer
                        // https://github.com/netty/netty/issues/1524
                        buf.release();
                    }
                }
                this.encoder = null;
            }
        }

        void encode(IByteBuffer input, List<object> output)
        {
            // call retain here as it will call release after its written to the channel
            this.encoder.writeOutbound(input.retain());
            this.fetchEncoderOutput(output);
        }

        void finishEncode(List<object> output)
        {
            if (this.encoder.finish())
            {
                this.fetchEncoderOutput(output);
            }
            this.encoder = null;
        }

        void fetchEncoderOutput(List<object> output)
        {
            for (;;)
            {
                IByteBuffer buf = this.encoder.ReadOutbound();
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
            readonly string targetContentEncoding;
            readonly EmbeddedChannel contentEncoder;

            public Result(string targetContentEncoding, EmbeddedChannel contentEncoder)
            {
                if (targetContentEncoding == null)
                {
                    throw new ArgumentNullException(nameof(targetContentEncoding));
                }
                if (contentEncoder == null)
                {
                    throw new ArgumentNullException(nameof(contentEncoder));
                }

                this.targetContentEncoding = targetContentEncoding;
                this.contentEncoder = contentEncoder;
            }

            public string TargetContentEncoding => this.targetContentEncoding;

            public EmbeddedChannel ContentEncoder => this.contentEncoder;
        }
    }
}