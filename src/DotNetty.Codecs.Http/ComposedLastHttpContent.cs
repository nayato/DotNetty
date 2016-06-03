// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http {






sealed class ComposedLastHttpContent : LastHttpContent {
    private readonly HttpHeaders trailingHeaders;
    private DecoderResult result;

    ComposedLastHttpContent(HttpHeaders trailingHeaders) {
        this.trailingHeaders = trailingHeaders;
    }

    // @Override
    public HttpHeaders trailingHeaders() {
        return trailingHeaders;
    }

    // @Override
    public LastHttpContent copy() {
        LastHttpContent content = new DefaultLastHttpContent(Unpooled.EMPTY_BUFFER);
        content.trailingHeaders().set(trailingHeaders());
        return content;
    }

    // @Override
    public LastHttpContent duplicate() {
        return copy();
    }

    // @Override
    public LastHttpContent retainedDuplicate() {
        return copy();
    }

    // @Override
    public LastHttpContent replace(IByteBuffer content) {
        readonly LastHttpContent dup = new DefaultLastHttpContent(content);
        dup.trailingHeaders().setAll(trailingHeaders());
        return dup;
    }

    // @Override
    public LastHttpContent retain(int increment) {
        return this;
    }

    // @Override
    public LastHttpContent retain() {
        return this;
    }

    // @Override
    public LastHttpContent touch() {
        return this;
    }

    // @Override
    public LastHttpContent touch(object hint) {
        return this;
    }

    // @Override
    public IByteBuffer content() {
        return Unpooled.EMPTY_BUFFER;
    }

    // @Override
    public DecoderResult decoderResult() {
        return result;
    }

    // @Override
    public DecoderResult getDecoderResult() {
        return decoderResult();
    }

    // @Override
    public void setDecoderResult(DecoderResult result) {
        this.result = result;
    }

    // @Override
    public int refCnt() {
        return 1;
    }

    // @Override
    public bool release() {
        return false;
    }

    // @Override
    public bool release(int decrement) {
        return false;
    }
}
