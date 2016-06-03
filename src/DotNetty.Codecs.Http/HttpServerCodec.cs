// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http
{
    using DotNetty.Transport.Channels;

    /**
         * A combination of {@link HttpRequestDecoder} and {@link HttpResponseEncoder}
         * which enables easier server side HTTP implementation.
         *
         * @see HttpClientCodec
         */

    public sealed class HttpServerCodec : CombinedChannelDuplexHandler<HttpRequestDecoder, HttpResponseEncoder>,
        HttpServerUpgradeHandler.SourceCodec
    {
        /**
                 * Creates a new instance with the default decoder options
                 * ({@code maxInitialLineLength (4096}}, {@code maxHeaderSize (8192)}, and
                 * {@code maxChunkSize (8192)}).
                 */

        public HttpServerCodec()
        {
            this(4096, 8192, 8192);
        }

        /**
         * Creates a new instance with the specified decoder options.
         */

        public HttpServerCodec(int maxInitialLineLength, int maxHeaderSize, int maxChunkSize)
        {
            base(new HttpRequestDecoder(maxInitialLineLength, maxHeaderSize, maxChunkSize), new HttpResponseEncoder());
        }

        /**
         * Creates a new instance with the specified decoder options.
         */

        public HttpServerCodec(int maxInitialLineLength, int maxHeaderSize, int maxChunkSize, bool validateHeaders)
        {
            base(new HttpRequestDecoder(maxInitialLineLength, maxHeaderSize, maxChunkSize, validateHeaders),
                new HttpResponseEncoder());
        }

        /**
         * Creates a new instance with the specified decoder options.
         */

        public HttpServerCodec(int maxInitialLineLength, int maxHeaderSize, int maxChunkSize, bool validateHeaders,
            int initialBufferSize)
        {
            base(
                new HttpRequestDecoder(maxInitialLineLength, maxHeaderSize, maxChunkSize, validateHeaders, initialBufferSize),
                new HttpResponseEncoder());
        }

        /**
         * Upgrades to another protocol from HTTP. Removes the {@link HttpRequestDecoder} and
         * {@link HttpResponseEncoder} from the pipeline.
         */
        // @Override
        public void upgradeFrom(IChannelHandlerContext ctx)
        {
            ctx.Channel.Pipeline.Remove(this);
        }
    }
}