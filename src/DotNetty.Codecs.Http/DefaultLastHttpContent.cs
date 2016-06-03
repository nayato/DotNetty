// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http {
    using System.Text;
    using DotNetty.Buffers;
    using DotNetty.Common.Utilities;

    /**
 * The default {@link LastHttpContent} implementation.
 */
public class DefaultLastHttpContent : DefaultHttpContent, LastHttpContent {
    private readonly HttpHeaders trailingHeaders;
    private readonly bool validateHeaders;

    public DefaultLastHttpContent() {
        this(Unpooled.buffer(0));
    }

    public DefaultLastHttpContent(IByteBuffer content) {
        this(content, true);
    }

    public DefaultLastHttpContent(IByteBuffer content, bool validateHeaders) {
        base(content);
        trailingHeaders = new TrailingHttpHeaders(validateHeaders);
        this.validateHeaders = validateHeaders;
    }

    // @Override
    public LastHttpContent copy() {
        return replace(content().copy());
    }

    // @Override
    public LastHttpContent duplicate() {
        return replace(content().duplicate());
    }

    // @Override
    public LastHttpContent retainedDuplicate() {
        return replace(content().retainedDuplicate());
    }

    // @Override
    public LastHttpContent replace(IByteBuffer content) {
        readonly DefaultLastHttpContent dup = new DefaultLastHttpContent(content, validateHeaders);
        dup.trailingHeaders().set(trailingHeaders());
        return dup;
    }

    // @Override
    public LastHttpContent retain(int increment) {
        base.retain(increment);
        return this;
    }

    // @Override
    public LastHttpContent retain() {
        base.retain();
        return this;
    }

    // @Override
    public LastHttpContent touch() {
        base.touch();
        return this;
    }

    // @Override
    public LastHttpContent touch(object hint) {
        base.touch(hint);
        return this;
    }

    // @Override
    public HttpHeaders trailingHeaders() {
        return trailingHeaders;
    }

    // @Override
    public string toString() {
        StringBuilder buf = new StringBuilder(base.ToString());
        buf.append(StringUtil.NEWLINE);
        appendHeaders(buf);

        // Remove the last newline.
        buf.setLength(buf.length() - StringUtil.NEWLINE.length());
        return buf.ToString();
    }

    private void appendHeaders(StringBuilder buf) {
        for (Entry<string, string> e : trailingHeaders()) {
            buf.append(e.getKey());
            buf.append(": ");
            buf.append(e.getValue());
            buf.append(StringUtil.NEWLINE);
        }
    }

    private static sealed class TrailingHttpHeaders : DefaultHttpHeaders {
        private static readonly NameValidator<CharSequence> TrailerNameValidator = new NameValidator<CharSequence>() {
            // @Override
            public void validateName(CharSequence name) {
                DefaultHttpHeaders.HttpNameValidator.validateName(name);
                if (HttpHeaderNames.CONTENT_LENGTH.contentEqualsIgnoreCase(name)
                        || HttpHeaderNames.TRANSFER_ENCODING.contentEqualsIgnoreCase(name)
                        || HttpHeaderNames.TRAILER.contentEqualsIgnoreCase(name)) {
                    throw new ArgumentException("prohibited trailing header: " + name);
                }
            }
        };

        @SuppressWarnings({ "unchecked" })
        TrailingHttpHeaders(bool validate) {
            base(validate, validate ? TrailerNameValidator : NameValidator.NOT_NULL);
        }
    }
}
