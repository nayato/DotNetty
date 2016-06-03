// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http
{
    using DotNetty.Buffers;

    /**
 * An HTTP chunk which is used for HTTP chunked transfer-encoding.
 * {@link HttpObjectDecoder} generates {@link HttpContent} after
 * {@link HttpMessage} when the content is large or the encoding of the content
 * is 'chunked.  If you prefer not to receive {@link HttpContent} in your handler,
 * place {@link HttpObjectAggregator} after {@link HttpObjectDecoder} in the
 * {@link ChannelPipeline}.
 */

    public interface HttpContent : HttpObject, IByteBufferHolder
    {
        // @Override
        HttpContent copy();

        // @Override
        HttpContent duplicate();

        // @Override
        HttpContent retainedDuplicate();

        // @Override
        HttpContent replace(IByteBuffer content);

        // @Override
        HttpContent retain();

        // @Override
        HttpContent retain(int increment);

        // @Override
        HttpContent touch();

        // @Override
        HttpContent touch(object hint);
    }
}