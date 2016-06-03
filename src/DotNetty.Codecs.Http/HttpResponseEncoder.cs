// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http
{
    using DotNetty.Buffers;

    /**
 * Encodes an {@link HttpResponse} or an {@link HttpContent} into
 * a {@link IByteBuffer}.
 */

    public class HttpResponseEncoder : HttpObjectEncoder<HttpResponse>
    {
        static readonly byte[] CRLF = { CR, LF };

        // @Override
        public bool acceptOutboundMessage(object msg)
        {
            return base.acceptOutboundMessage(msg) && !(msg is HttpRequest);
        }

        // @Override
        protected void encodeInitialLine(IByteBuffer buf, HttpResponse response)
        {
            response.protocolVersion().encode(buf);
            buf.WriteByte(SP);
            response.status().encode(buf);
            buf.WriteBytes(CRLF);
        }
    }