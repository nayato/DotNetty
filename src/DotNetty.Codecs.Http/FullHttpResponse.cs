// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http
{
    using DotNetty.Buffers;

    /**
 * Combination of a {@link HttpResponse} and {@link FullHttpMessage}.
 * So it represent a <i>complete</i> http response.
 */

    public interface FullHttpResponse : HttpResponse, FullHttpMessage
    {
        // @Override
        FullHttpResponse copy();

        // @Override
        FullHttpResponse duplicate();

        // @Override
        FullHttpResponse retainedDuplicate();

        // @Override
        FullHttpResponse replace(IByteBuffer content);

        // @Override
        FullHttpResponse retain(int increment);

        // @Override
        FullHttpResponse retain();

        // @Override
        FullHttpResponse touch();

        // @Override
        FullHttpResponse touch(object hint);

        // @Override
        FullHttpResponse setProtocolVersion(HttpVersion version);

        // @Override
        FullHttpResponse setStatus(HttpResponseStatus status);
    }
}