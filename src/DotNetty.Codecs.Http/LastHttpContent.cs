// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http {





/**
 * The last {@link HttpContent} which has trailing headers.
 */
public interface LastHttpContent : HttpContent {

    /**
     * The 'end of content' marker in chunked encoding.
     */
    LastHttpContent EMPTY_LAST_CONTENT = new LastHttpContent() {

        // @Override
        public IByteBuffer content() {
            return Unpooled.EMPTY_BUFFER;
        }

        // @Override
        public LastHttpContent copy() {
            return EMPTY_LAST_CONTENT;
        }

        // @Override
        public LastHttpContent duplicate() {
            return this;
        }

        // @Override
        public LastHttpContent replace(IByteBuffer content) {
            return new DefaultLastHttpContent(content);
        }

        // @Override
        public LastHttpContent retainedDuplicate() {
            return this;
        }

        // @Override
        public HttpHeaders trailingHeaders() {
            return EmptyHttpHeaders.INSTANCE;
        }

        // @Override
        public DecoderResult decoderResult() {
            return DecoderResult.SUCCESS;
        }

        // @Override
        [Obsolete]
        public DecoderResult getDecoderResult() {
            return decoderResult();
        }

        // @Override
        public void setDecoderResult(DecoderResult result) {
            throw new UnsupportedOperationException("read only");
        }

        // @Override
        public int refCnt() {
            return 1;
        }

        // @Override
        public LastHttpContent retain() {
            return this;
        }

        // @Override
        public LastHttpContent retain(int increment) {
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
        public bool release() {
            return false;
        }

        // @Override
        public bool release(int decrement) {
            return false;
        }

        // @Override
        public string toString() {
            return "EmptyLastHttpContent";
        }
    };

    HttpHeaders trailingHeaders();

    // @Override
    LastHttpContent copy();

    // @Override
    LastHttpContent duplicate();

    // @Override
    LastHttpContent retainedDuplicate();

    // @Override
    LastHttpContent replace(IByteBuffer content);

    // @Override
    LastHttpContent retain(int increment);

    // @Override
    LastHttpContent retain();

    // @Override
    LastHttpContent touch();

    // @Override
    LastHttpContent touch(object hint);
}
