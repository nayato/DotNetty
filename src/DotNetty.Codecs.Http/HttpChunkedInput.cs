// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http
{
    using System;
    using DotNetty.Buffers;
    using DotNetty.Transport.Channels;

    /**
 * A {@link ChunkedInput} that fetches data chunk by chunk for use with HTTP chunked transfers.
 * <p>
 * Each chunk from the input data will be wrapped within a {@link HttpContent}. At the end of the input data,
 * {@link LastHttpContent} will be written.
 * <p>
 * Ensure that your HTTP response header contains {@code Transfer-Encoding: chunked}.
 * <p>
 * <pre>
 * public void messageReceived(IChannelHandlerContext ctx, FullHttpRequest request)  {
 *     HttpResponse response = new DefaultHttpResponse(HTTP_1_1, OK);
 *     response.headers().set(TRANSFER_ENCODING, CHUNKED);
 *     ctx.write(response);
 *
 *     HttpContentChunkedInput httpChunkWriter = new HttpChunkedInput(
 *         new ChunkedFile(&quot;/tmp/myfile.txt&quot;));
 *     ChannelFuture sendFileFuture = ctx.write(httpChunkWriter);
 * }
 * </pre>
 */

    public class HttpChunkedInput : ChunkedInput<HttpContent>
    {
        readonly ChunkedInput<IByteBuffer> input;
        readonly LastHttpContent lastHttpContent;
        bool sentLastChunk;

        /**
         * Creates a new instance using the specified input.
         * @param input {@link ChunkedInput} containing data to write
         */

        public HttpChunkedInput(ChunkedInput<IByteBuffer> input)
        {
            this.input = input;
            this.lastHttpContent = LastHttpContent.EMPTY_LAST_CONTENT;
        }

        /**
         * Creates a new instance using the specified input. {@code lastHttpContent} will be written as the terminating
         * chunk.
         * @param input {@link ChunkedInput} containing data to write
         * @param lastHttpContent {@link LastHttpContent} that will be written as the terminating chunk. Use this for
         *            training headers.
         */

        public HttpChunkedInput(ChunkedInput<IByteBuffer> input, LastHttpContent lastHttpContent)
        {
            this.input = input;
            this.lastHttpContent = lastHttpContent;
        }

        // @Override
        public bool isEndOfInput()
        {
            if (this.input.isEndOfInput())
            {
                // Only end of input after last HTTP chunk has been sent
                return this.sentLastChunk;
            }
            else
            {
                return false;
            }
        }

        // @Override
        public void close()
        {
            this.input.close();
        }

        [Obsolete]
        // @Override
        public HttpContent readChunk(IChannelHandlerContext ctx)
        {
            return readChunk(ctx.Allocator);
        }

        // @Override
        public HttpContent readChunk(IByteBufferAllocator allocator)
        {
            if (this.input.isEndOfInput())
            {
                if (this.sentLastChunk)
                {
                    return null;
                }
                else
                {
                    // Send last chunk for this input
                    this.sentLastChunk = true;
                    return this.lastHttpContent;
                }
            }
            else
            {
                IByteBuffer buf = this.input.readChunk(allocator);
                return new DefaultHttpContent(buf);
            }
        }

        // @Override
        public long length()
        {
            return this.input.length();
        }

        // @Override
        public long progress()
        {
            return this.input.progress();
        }
    }