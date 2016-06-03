// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using DotNetty.Buffers;
    using DotNetty.Common.Utilities;
    using DotNetty.Transport.Channels;

    /**
 * Encodes an {@link HttpMessage} or an {@link HttpContent} into
 * a {@link IByteBuffer}.
 *
 * <h3>Extensibility</h3>
 *
 * Please note that this encoder is designed to be extended to implement
 * a protocol derived from HTTP, such as
 * <a href="http://en.wikipedia.org/wiki/Real_Time_Streaming_Protocol">RTSP</a> and
 * <a href="http://en.wikipedia.org/wiki/Internet_Content_Adaptation_Protocol">ICAP</a>.
 * To implement the encoder of such a derived protocol, extend this class and
 * implement all abstract methods properly.
 */

    public abstract class HttpObjectEncoder<H  : HttpMessage> : MessageToMessageEncoder<object>
    {
        static readonly byte[] CRLF = { CR, LF };
        static readonly byte[] ZERO_CRLF = { '0', CR, LF };
        static readonly byte[] ZERO_CRLF_CRLF = { '0', CR, LF, CR, LF };
        static readonly IByteBuffer CRLF_BUF = unreleasableBuffer(directBuffer(CRLF.length).writeBytes(CRLF));

        static readonly IByteBuffer ZERO_CRLF_CRLF_BUF = unreleasableBuffer(directBuffer(ZERO_CRLF_CRLF.Length)
            .writeBytes(ZERO_CRLF_CRLF));

        static readonly int ST_INIT = 0;
        static readonly int ST_CONTENT_NON_CHUNK = 1;
        static readonly int ST_CONTENT_CHUNK = 2;

        int state = ST_INIT;

        // @Override
        protected void encode(IChannelHandlerContext ctx, object msg, List<object> output)
        {
            IByteBuffer buf = null;
            if (msg is HttpMessage)
            {
                if (this.state != ST_INIT)
                {
                    throw new IllegalStateException("unexpected message type: " + StringUtil.SimpleClassName(msg));
                }

                var m = (H)msg;

                buf = ctx.Allocator.Buffer();
                // Encode the message.
                this.encodeInitialLine(buf, m);
                this.encodeHeaders(m.headers(), buf);
                buf.WriteBytes(CRLF);
                this.state = HttpUtil.isTransferEncodingChunked(m) ? ST_CONTENT_CHUNK : ST_CONTENT_NON_CHUNK;
            }

            // Bypass the encoder in case of an empty buffer, so that the following idiom works:
            //
            //     ch.write(Unpooled.EMPTY_BUFFER).addListener(ChannelFutureListener.CLOSE);
            //
            // See https://github.com/netty/netty/issues/2983 for more information.

            if (msg is IByteBuffer && !((IByteBuffer)msg).IsReadable())
            {
                output.Add(EMPTY_BUFFER);
                return;
            }

            if (msg is HttpContent || msg is IByteBuffer || msg is FileRegion)
            {
                if (this.state == ST_INIT)
                {
                    throw new IllegalStateException("unexpected message type: " + StringUtil.simpleClassName(msg));
                }

                long contentLength = contentLength(msg);
                if (this.state == ST_CONTENT_NON_CHUNK)
                {
                    if (contentLength > 0)
                    {
                        if ((buf != null) && (buf.WritableBytes() >= contentLength) && msg is HttpContent)
                        {
                            // merge into other buffer for performance reasons
                            buf.writeBytes(((HttpContent)msg).content());
                            output.Add(buf);
                        }
                        else
                        {
                            if (buf != null)
                            {
                                output.Add(buf);
                            }
                            output.Add(encodeAndRetain(msg));
                        }
                    }
                    else
                    {
                        if (buf != null)
                        {
                            output.Add(buf);
                        }
                        else
                        {
                            // Need to produce some output otherwise an
                            // IllegalStateException will be thrown
                            output.Add(EMPTY_BUFFER);
                        }
                    }

                    if (msg is LastHttpContent)
                    {
                        this.state = ST_INIT;
                    }
                }
                else if (this.state == ST_CONTENT_CHUNK)
                {
                    if (buf != null)
                    {
                        output.Add(buf);
                    }
                    this.encodeChunkedContent(ctx, msg, contentLength, output);
                }
                else
                {
                    throw new Error();
                }
            }
            else
            {
                if (buf != null)
                {
                    output.Add(buf);
                }
            }
        }

        /**
         * Encode the {@link HttpHeaders} into a {@link IByteBuffer}.
         */

        protected void encodeHeaders(HttpHeaders headers, IByteBuffer buf)
        {
            Iterator<Entry<CharSequence, CharSequence>> iter = headers.iteratorCharSequence();
            while (iter.hasNext())
            {
                Entry<CharSequence, CharSequence> header = iter.next();
                HttpHeadersEncoder.encoderHeader(header.getKey(), header.getValue(), buf);
            }
        }

        void encodeChunkedContent(IChannelHandlerContext ctx, object msg, long contentLength, List<object> output)
        {
            if (contentLength > 0)
            {
                byte[] length = Encoding.ASCII.GetBytes(contentLength.ToString("x"));
                IByteBuffer buf = ctx.Allocator.Buffer(length.Length + 2);
                buf.WriteBytes(length);
                buf.WriteBytes(CRLF);
                output.Add(buf);
                output.Add(encodeAndRetain(msg));
                output.Add(CRLF_BUF.Duplicate());
            }

            if (msg is LastHttpContent)
            {
                HttpHeaders headers = ((LastHttpContent)msg).trailingHeaders();
                if (headers.isEmpty())
                {
                    output.Add(ZERO_CRLF_CRLF_BUF.Duplicate());
                }
                else
                {
                    IByteBuffer buf = ctx.Allocator.Buffer();
                    buf.WriteBytes(ZERO_CRLF);
                    try
                    {
                        this.encodeHeaders(headers, buf);
                    }
                    catch (Exception ex)
                    {
                        buf.Release();
                        throw;
                    }
                    buf.WriteBytes(CRLF);
                    output.Add(buf);
                }

                this.state = ST_INIT;
            }
            else
            {
                if (contentLength == 0)
                {
                    // Need to produce some output otherwise an
                    // IllegalstateException will be thrown
                    output.Add(EMPTY_BUFFER);
                }
            }
        }

        // @Override
        public bool acceptOutboundMessage(object msg)
        {
            return msg is HttpObject || msg is IByteBuffer || msg is FileRegion;
        }

        static object encodeAndRetain(object msg)
        {
            if (msg is IByteBuffer)
            {
                return ((IByteBuffer)msg).Retain();
            }
            if (msg is HttpContent)
            {
                return ((HttpContent)msg).content().retain();
            }
            if (msg is FileRegion)
            {
                return ((FileRegion)msg).retain();
            }
            throw new IllegalStateException("unexpected message type: " + StringUtil.SimpleClassName(msg));
        }

        static long contentLength(object msg)
        {
            if (msg is HttpContent)
            {
                return ((HttpContent)msg).content().readableBytes();
            }
            if (msg is IByteBuffer)
            {
                return ((IByteBuffer)msg).ReadableBytes();
            }
            if (msg is FileRegion)
            {
                return ((FileRegion)msg).count();
            }
            throw new IllegalStateException("unexpected message type: " + StringUtil.SimpleClassName(msg));
        }

        [Obsolete]
        protected static void encodeAscii(string s, IByteBuffer buf)
        {
            HttpUtil.encodeAscii0(s, buf);
        }

        protected abstract void encodeInitialLine(IByteBuffer buf, H message);
    }
}