// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http
{
    using System.Collections.Generic;
    using DotNetty.Buffers;
    using DotNetty.Common.Utilities;
    using DotNetty.Transport.Channels;
    using DotNetty.Transport.Channels.Embedded;

    /**
 * Decodes the content of the received {@link HttpRequest} and {@link HttpContent}.
 * The original content is replaced with the new content decoded by the
 * {@link EmbeddedChannel}, which is created by {@link #newContentDecoder(string)}.
 * Once decoding is finished, the value of the <tt>'Content-Encoding'</tt>
 * header is set to the target content encoding, as returned by {@link #getTargetContentEncoding(string)}.
 * Also, the <tt>'Content-Length'</tt> header is updated to the length of the
 * decoded content.  If the content encoding of the original is not supported
 * by the decoder, {@link #newContentDecoder(string)} should return {@code null}
 * so that no decoding occurs (i.e. pass-through).
 * <p>
 * Please note that this is an abstract class.  You have to extend this class
 * and implement {@link #newContentDecoder(string)} properly to make this class
 * functional.  For example, refer to the source code of {@link HttpContentDecompressor}.
 * <p>
 * This handler must be placed after {@link HttpObjectDecoder} in the pipeline
 * so that this handler can intercept HTTP requests after {@link HttpObjectDecoder}
 * converts {@link IByteBuffer}s into HTTP requests.
 */

    public abstract class HttpContentDecoder : MessageToMessageDecoder<HttpObject>
    {
        static readonly string IDENTITY = HttpHeaderValues.IDENTITY.ToString();

        protected IChannelHandlerContext ctx;
        EmbeddedChannel decoder;
        bool continueResponse;

        // @Override
        protected void decode(IChannelHandlerContext ctx, HttpObject msg, List<object> output)
        {
            if (msg is HttpResponse && (((HttpResponse)msg).status().code() == 100))
            {
                if (!(msg is LastHttpContent))
                {
                    this.continueResponse = true;
                }
                // 100-continue response must be passed through.
                output.Add(ReferenceCountUtil.Retain(msg));
                return;
            }

            if (this.continueResponse)
            {
                if (msg is LastHttpContent)
                {
                    this.continueResponse = false;
                }
                // 100-continue response must be passed through.
                output.Add(ReferenceCountUtil.Retain(msg));
                return;
            }

            if (msg is HttpMessage)
            {
                this.cleanup();
                var message = (HttpMessage)msg;
                HttpHeaders headers = message.headers();

                // Determine the content encoding.
                string contentEncoding = headers.get(HttpHeaderNames.CONTENT_ENCODING);
                if (contentEncoding != null)
                {
                    contentEncoding = contentEncoding.Trim();
                }
                else
                {
                    contentEncoding = IDENTITY;
                }
                this.decoder = this.newContentDecoder(contentEncoding);

                if (this.decoder == null)
                {
                    if (message is HttpContent)
                    {
                        ((HttpContent)message).retain();
                    }
                    output.Add(message);
                    return;
                }

                // Remove content-length header:
                // the correct value can be set only after all chunks are processed/decoded.
                // If buffering is not an issue, add HttpObjectAggregator down the chain, it will set the header.
                // Otherwise, rely on LastHttpContent message.
                headers.remove(HttpHeaderNames.CONTENT_LENGTH);

                // set new content encoding,
                CharSequence targetContentEncoding = this.getTargetContentEncoding(contentEncoding);
                if (HttpHeaderValues.IDENTITY.ContentEquals(targetContentEncoding))
                {
                    // Do NOT set the 'Content-Encoding' header if the target encoding is 'identity'
                    // as per: http://tools.ietf.org/html/rfc2616#section-14.11
                    headers.remove(HttpHeaderNames.CONTENT_ENCODING);
                }
                else
                {
                    headers.set(HttpHeaderNames.CONTENT_ENCODING, targetContentEncoding);
                }

                if (message is HttpContent)
                {
                    // If message is a full request or response object (headers + data), don't copy data part into output.
                    // Output headers only; data part will be decoded below.
                    // Note: "copy" object must not be an instance of LastHttpContent class,
                    // as this would (erroneously) indicate the end of the HttpMessage to other handlers.
                    HttpMessage copy;
                    if (message is HttpRequest)
                    {
                        var r = (HttpRequest)message; // HttpRequest or FullHttpRequest
                        copy = new DefaultHttpRequest(r.protocolVersion(), r.method(), r.uri());
                    }
                    else if (message is HttpResponse)
                    {
                        var r = (HttpResponse)message; // HttpResponse or FullHttpResponse
                        copy = new DefaultHttpResponse(r.protocolVersion(), r.status());
                    }
                    else
                    {
                        throw new CodecException("object of class " + message.GetType().Name +
                            " is not a HttpRequest or HttpResponse");
                    }
                    copy.headers().set(message.headers());
                    copy.setDecoderResult(message.decoderResult());
                    output.Add(copy);
                }
                else
                {
                    output.Add(message);
                }
            }

            if (msg is HttpContent)
            {
                var c = (HttpContent)msg;
                if (this.decoder == null)
                {
                    output.Add(c.retain());
                }
                else
                {
                    this.decodeContent(c, output);
                }
            }
        }

        void decodeContent(HttpContent c, List<object> output)
        {
            IByteBuffer content = c.content();

            this.decode(content, output);

            if (c is LastHttpContent)
            {
                this.finishDecode(output);

                var last = (LastHttpContent)c;
                // Generate an additional chunk if the decoder produced
                // the last product on closure,
                HttpHeaders headers = last.trailingHeaders();
                if (headers.isEmpty())
                {
                    output.Add(LastHttpContent.EMPTY_LAST_CONTENT);
                }
                else
                {
                    output.Add(new ComposedLastHttpContent(headers));
                }
            }
        }

        /**
         * Returns a new {@link EmbeddedChannel} that decodes the HTTP message
         * content encoded in the specified <tt>contentEncoding</tt>.
         *
         * @param contentEncoding the value of the {@code "Content-Encoding"} header
         * @return a new {@link EmbeddedChannel} if the specified encoding is supported.
         *         {@code null} otherwise (alternatively, you can throw an exception
         *         to block unknown encoding).
         */

        protected abstract EmbeddedChannel newContentDecoder(string contentEncoding);

        /**
         * Returns the expected content encoding of the decoded content.
         * This getMethod returns {@code "identity"} by default, which is the case for
         * most decoders.
         *
         * @param contentEncoding the value of the {@code "Content-Encoding"} header
         * @return the expected content encoding of the new content
         */

        protected string getTargetContentEncoding(
            string contentEncoding)
        {
            return IDENTITY;
        }

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

        // @Override
        public void handlerAdded(IChannelHandlerContext ctx)
        {
            this.ctx = ctx;
            base.handlerAdded(ctx);
        }

        void cleanup()
        {
            if (this.decoder != null)
            {
                // Clean-up the previous decoder if not cleaned up correctly.
                if (this.decoder.finish())
                {
                    for (;;)
                    {
                        IByteBuffer buf = this.decoder.readInbound();
                        if (buf == null)
                        {
                            break;
                        }
                        // Release the buffer
                        buf.release();
                    }
                }
                this.decoder = null;
            }
        }

        void decode(IByteBuffer input, List<object> output)
        {
            // call retain here as it will call release after its written to the channel
            this.decoder.writeInbound(input.retain());
            this.fetchDecoderOutput(output);
        }

        void finishDecode(List<object> output)
        {
            if (this.decoder.finish())
            {
                this.fetchDecoderOutput(output);
            }
            this.decoder = null;
        }

        void fetchDecoderOutput(List<object> output)
        {
            for (;;)
            {
                IByteBuffer buf = this.decoder.readInbound();
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
    }
}