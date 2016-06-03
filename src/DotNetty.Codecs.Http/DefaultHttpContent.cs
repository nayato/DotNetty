// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http {
    using System;
    using DotNetty.Buffers;
    using DotNetty.Common.Utilities;

    /**
 * The default {@link HttpContent} implementation.
 */
public class DefaultHttpContent : DefaultHttpObject, HttpContent {

    private readonly IByteBuffer content;

    /**
     * Creates a new instance with the specified chunk content.
     */
    public DefaultHttpContent(IByteBuffer content) {
        if (content == null) {
            throw new ArgumentNullException(nameof(content");
        }
        this.content = content;
    }

    // @Override
    public IByteBuffer content() {
        return content;
    }

    // @Override
    public HttpContent copy() {
        return replace(content.copy());
    }

    // @Override
    public HttpContent duplicate() {
        return replace(content.duplicate());
    }

    // @Override
    public HttpContent retainedDuplicate() {
        return replace(content.retainedDuplicate());
    }

    // @Override
    public HttpContent replace(IByteBuffer content) {
        return new DefaultHttpContent(content);
    }

    // @Override
    public int refCnt() {
        return content.refCnt();
    }

    // @Override
    public HttpContent retain() {
        content.retain();
        return this;
    }

    // @Override
    public HttpContent retain(int increment) {
        content.retain(increment);
        return this;
    }

    // @Override
    public HttpContent touch() {
        content.touch();
        return this;
    }

    // @Override
    public HttpContent touch(object hint) {
        content.touch(hint);
        return this;
    }

    // @Override
    public bool release() {
        return content.release();
    }

    // @Override
    public bool release(int decrement) {
        return content.release(decrement);
    }

    // @Override
    public string toString() {
        return StringUtil.simpleClassName(this) +
               "(data: " + content() + ", decoderResult: " + decoderResult() + ')';
    }
}
