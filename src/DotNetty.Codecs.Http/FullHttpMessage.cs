// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http
{
    using DotNetty.Buffers;

    /**
 * Combines {@link HttpMessage} and {@link LastHttpContent} into one
 * message. So it represent a <i>complete</i> http message.
 */

    public interface FullHttpMessage : HttpMessage, LastHttpContent
    {
        // @Override
        FullHttpMessage copy();

        // @Override
        FullHttpMessage duplicate();

        // @Override
        FullHttpMessage retainedDuplicate();

        // @Override
        FullHttpMessage replace(IByteBuffer content);

        // @Override
        FullHttpMessage retain(int increment);

        // @Override
        FullHttpMessage retain();

        // @Override
        FullHttpMessage touch();

        // @Override
        FullHttpMessage touch(object hint);
    }
}