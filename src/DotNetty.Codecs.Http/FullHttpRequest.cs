// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http
{
    using DotNetty.Buffers;

    /**
 * Combine the {@link HttpRequest} and {@link FullHttpMessage}, so the request is a <i>complete</i> HTTP
 * request.
 */

    public interface FullHttpRequest : HttpRequest, FullHttpMessage
    {
        // @Override
        FullHttpRequest copy();

        // @Override
        FullHttpRequest duplicate();

        // @Override
        FullHttpRequest retainedDuplicate();

        // @Override
        FullHttpRequest replace(IByteBuffer content);

        // @Override
        FullHttpRequest retain(int increment);

        // @Override
        FullHttpRequest retain();

        // @Override
        FullHttpRequest touch();

        // @Override
        FullHttpRequest touch(object hint);

        // @Override
        FullHttpRequest setProtocolVersion(HttpVersion version);

        // @Override
        FullHttpRequest setMethod(HttpMethod method);

        // @Override
        FullHttpRequest setUri(string uri);
    }
}