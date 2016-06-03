// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http
{
    using DotNetty.Transport.Channels.Embedded;

    /**
 * Decompresses an {@link HttpMessage} and an {@link HttpContent} compressed in
 * {@code gzip} or {@code deflate} encoding.  For more information on how this
 * handler modifies the message, please refer to {@link HttpContentDecoder}.
 */

    public class HttpContentDecompressor : HttpContentDecoder
    {
        readonly bool strict;

        /**
         * Create a new {@link HttpContentDecompressor} in non-strict mode.
         */

        public HttpContentDecompressor(): this(false)
        {
            
        }

        /**
         * Create a new {@link HttpContentDecompressor}.
         *
         * @param strict    if {@code true} use strict handling of deflate if used, otherwise handle it in a
         *                  more lenient fashion.
         */

        public HttpContentDecompressor(bool strict)
        {
            this.strict = strict;
        }

        // @Override
        protected EmbeddedChannel newContentDecoder(string contentEncoding)
        {
            if (GZIP.contentEqualsIgnoreCase(contentEncoding) ||
                X_GZIP.contentEqualsIgnoreCase(contentEncoding))
            {
                return new EmbeddedChannel(this.ctx.channel().id(), this.ctx.channel().metadata().hasDisconnect(), this.ctx.channel().config(), ZlibCodecFactory.newZlibDecoder(ZlibWrapper.GZIP));
            }
            if (DEFLATE.contentEqualsIgnoreCase(contentEncoding) ||
                X_DEFLATE.contentEqualsIgnoreCase(contentEncoding))
            {
                readonly
                ZlibWrapper wrapper = this.strict ? ZlibWrapper.ZLIB : ZlibWrapper.ZLIB_OR_NONE;
                // To be strict, 'deflate' means ZLIB, but some servers were not implemented correctly.
                return new EmbeddedChannel(this.ctx.channel().id(), this.ctx.channel().metadata().hasDisconnect(), this.ctx.channel().config(), ZlibCodecFactory.newZlibDecoder(wrapper));
            }

            // 'identity' or unsupported
            return null;
        }
    }
}