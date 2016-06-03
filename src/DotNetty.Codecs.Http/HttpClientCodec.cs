// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http
{
    using System.Collections.Generic;
    using DotNetty.Buffers;
    using DotNetty.Common.Utilities;
    using DotNetty.Transport.Channels;
    using Nito;

    /**
     * A combination of {@link HttpRequestEncoder} and {@link HttpResponseDecoder}
     * which enables easier client side HTTP implementation. {@link HttpClientCodec}
     * provides additional state management for <tt>HEAD</tt> and <tt>CONNECT</tt>
     * requests, which {@link HttpResponseDecoder} lacks.  Please refer to
     * {@link HttpResponseDecoder} to learn what additional state management needs
     * to be done for <tt>HEAD</tt> and <tt>CONNECT</tt> and why
     * {@link HttpResponseDecoder} can not handle it by itself.
     *
     * If the {@link Channel} is closed and there are missing responses,
     * a {@link PrematureChannelClosureException} is thrown.
     *
     * @see HttpServerCodec
     */

    public sealed class HttpClientCodec : CombinedChannelDuplexHandler<HttpResponseDecoder, HttpRequestEncoder>,
        HttpClientUpgradeHandler.SourceCodec
    {
        /** A queue that is used for correlating a request and a response. */
        readonly Queue<HttpMethod> queue = new Deque<HttpMethod>();

        /** If true, decoding stops (i.e. pass-through) */
        bool done;

        readonly AtomicLong requestResponseCounter = new AtomicLong();
        readonly bool failOnMissingResponse;

        /**
         * Creates a new instance with the default decoder options
         * ({@code maxInitialLineLength (4096}}, {@code maxHeaderSize (8192)}, and
         * {@code maxChunkSize (8192)}).
         */

        public HttpClientCodec()
            : this(4096, 8192, 8192, false)
        {
            ;
        }

        /**
         * Creates a new instance with the specified decoder options.
         */

        public HttpClientCodec(int maxInitialLineLength, int maxHeaderSize, int maxChunkSize)
            : this(maxInitialLineLength, maxHeaderSize, maxChunkSize, false)
        {
        }

        /**
         * Creates a new instance with the specified decoder options.
         */

        public HttpClientCodec(
            int maxInitialLineLength, int maxHeaderSize, int maxChunkSize, bool failOnMissingResponse)
            : this(maxInitialLineLength, maxHeaderSize, maxChunkSize, failOnMissingResponse, true)
        {
        }

        /**
         * Creates a new instance with the specified decoder options.
         */

        public HttpClientCodec(
            int maxInitialLineLength, int maxHeaderSize, int maxChunkSize, bool failOnMissingResponse,
            bool validateHeaders)
        {
            init(new Decoder(maxInitialLineLength, maxHeaderSize, maxChunkSize, validateHeaders), new Encoder());
            this.failOnMissingResponse = failOnMissingResponse;
        }

        /**
         * Creates a new instance with the specified decoder options.
         */

        public HttpClientCodec(
            int maxInitialLineLength, int maxHeaderSize, int maxChunkSize, bool failOnMissingResponse,
            bool validateHeaders, int initialBufferSize)
        {
            init(new Decoder(maxInitialLineLength, maxHeaderSize, maxChunkSize, validateHeaders, initialBufferSize),
                new Encoder());
            this.failOnMissingResponse = failOnMissingResponse;
        }

        /**
         * Prepares to upgrade to another protocol from HTTP. Disables the {@link Encoder}.
         */
        // @Override
        public void prepareUpgradeFrom(IChannelHandlerContext ctx)
        {
            ((Encoder)outboundHandler()).upgraded = true;
        }

        /**
         * Upgrades to another protocol from HTTP. Removes the {@link Decoder} and {@link Encoder} from
         * the pipeline.
         */
        // @Override
        public void upgradeFrom(IChannelHandlerContext ctx)
        {
            IChannelPipeline p = ctx.Channel.Pipeline;
            p.Remove(this);
        }

        public void setSingleDecode(bool singleDecode)
        {
            inboundHandler().setSingleDecode(singleDecode);
        }

        public bool isSingleDecode()
        {
            return inboundHandler().isSingleDecode();
        }

        sealed class Encoder : HttpRequestEncoder
        {
            bool upgraded;

            // @Override
            protected void encode(
                IChannelHandlerContext ctx, object msg, List<object> output)
            {
                if (this.upgraded)
                {
                    output.Add(ReferenceCountUtil.Retain(msg));
                    return;
                }

                if (msg is HttpRequest && !done)
                {
                    queue.offer(((HttpRequest)msg).method());
                }

                base.encode(ctx, msg, output);

                if (failOnMissingResponse)
                {
                    // check if the request is chunked if so do not increment
                    if (msg is LastHttpContent)
                    {
                        // increment as its the last chunk
                        requestResponseCounter.incrementAndGet();
                    }
                }
            }
        }

        sealed class Decoder : HttpResponseDecoder
        {
            Decoder(int maxInitialLineLength, int maxHeaderSize, int maxChunkSize, bool validateHeaders)
            {
                base(maxInitialLineLength, maxHeaderSize, maxChunkSize, validateHeaders);
            }

            Decoder(int maxInitialLineLength, int maxHeaderSize, int maxChunkSize, bool validateHeaders,
                int initialBufferSize)
            {
                base(maxInitialLineLength, maxHeaderSize, maxChunkSize, validateHeaders, initialBufferSize);
            }

            // @Override
            protected void decode(
                IChannelHandlerContext ctx, IByteBuffer buffer, List<object> output)
            {
                if (done)
                {
                    int readable = this.ActualReadableBytes;
                    if (readable == 0)
                    {
                        // if non is readable just return null
                        // https://github.com/netty/netty/issues/1159
                        return;
                    }
                    output.Add(buffer.ReadBytes(readable));
                }
                else
                {
                    int oldSize = output.Count;
                    base.decode(ctx, buffer, output);
                    if (failOnMissingResponse)
                    {
                        int size = output.Count;
                        for (int i = oldSize; i < size; i++)
                        {
                            this.decrement(output[i]);
                        }
                    }
                }
            }

            void decrement(object msg)
            {
                if (msg == null)
                {
                    return;
                }

                // check if it's an Header and its transfer encoding is not chunked.
                if (msg is LastHttpContent)
                {
                    requestResponseCounter.decrementAndGet();
                }
            }

            // @Override
            protected bool isContentAlwaysEmpty(HttpMessage msg)
            {
                readonly
                int statusCode = ((HttpResponse)msg).status().code();
                if (statusCode == 100)
                {
                    // 100-continue response should be excluded from paired comparison.
                    return true;
                }

                // Get the getMethod of the HTTP request that corresponds to the
                // current response.
                HttpMethod method = queue.poll();

                char firstChar = method.Name[0];
                switch (firstChar)
                {
                    case 'H':
                        // According to 4.3, RFC2616:
                        // All responses to the HEAD request getMethod MUST NOT include a
                        // message-body, even though the presence of entity-header fields
                        // might lead one to believe they do.
                        if (HttpMethod.HEAD.Equals(method))
                        {
                            return true;

                            // The following code was inserted to work around the servers
                            // that behave incorrectly.  It has been commented out
                            // because it does not work with well behaving servers.
                            // Please note, even if the 'Transfer-Encoding: chunked'
                            // header exists in the HEAD response, the response should
                            // have absolutely no content.
                            //
                            //// Interesting edge case:
                            //// Some poorly implemented servers will send a zero-byte
                            //// chunk if Transfer-Encoding of the response is 'chunked'.
                            ////
                            //// return !msg.isChunked();
                        }
                        break;
                    case 'C':
                        // Successful CONNECT request results in a response with empty body.
                        if (statusCode == 200)
                        {
                            if (HttpMethod.CONNECT.Equals(method))
                            {
                                // Proxy connection established - Not HTTP anymore.
                                done = true;
                                queue.clear();
                                return true;
                            }
                        }
                        break;
                }

                return base.isContentAlwaysEmpty(msg);
            }

            // @Override
            public override void ChannelInactive(IChannelHandlerContext ctx)
            {
                base.ChannelInactive(ctx);

                if (failOnMissingResponse)
                {
                    long missingResponses = requestResponseCounter.get();
                    if (missingResponses > 0)
                    {
                        ctx.FireExceptionCaught(new PrematureChannelClosureException(
                            "channel gone inactive with " + missingResponses +
                            " missing response(s)"));
                    }
                }
            }
        }
    }
}