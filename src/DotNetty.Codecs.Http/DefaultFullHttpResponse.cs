// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http {
    using System.Text;
    using DotNetty.Buffers;

    /**
 * Default implementation of a {@link FullHttpResponse}.
 */
public class DefaultFullHttpResponse : DefaultHttpResponse, FullHttpResponse {

    private readonly IByteBuffer content;
    private readonly HttpHeaders trailingHeaders;

    /**
     * Used to cache the value of the hash code and avoid {@link IllegalReferenceCountException}.
     */
    private int hash;

    public DefaultFullHttpResponse(HttpVersion version, HttpResponseStatus status) {
        this(version, status, Unpooled.buffer(0));
    }

    public DefaultFullHttpResponse(HttpVersion version, HttpResponseStatus status, IByteBuffer content) {
        this(version, status, content, true);
    }

    public DefaultFullHttpResponse(HttpVersion version, HttpResponseStatus status, bool validateHeaders) {
        this(version, status, Unpooled.buffer(0), validateHeaders, false);
    }

    public DefaultFullHttpResponse(HttpVersion version, HttpResponseStatus status, bool validateHeaders,
                                   bool singleFieldHeaders) {
        this(version, status, Unpooled.buffer(0), validateHeaders, singleFieldHeaders);
    }

    public DefaultFullHttpResponse(HttpVersion version, HttpResponseStatus status,
                                   IByteBuffer content, bool validateHeaders) {
        this(version, status, content, validateHeaders, false);
    }

    public DefaultFullHttpResponse(HttpVersion version, HttpResponseStatus status,
                                   IByteBuffer content, bool validateHeaders, bool singleFieldHeaders) {
        base(version, status, validateHeaders, singleFieldHeaders);
        this.content = checkNotNull(content, "content");
        this.trailingHeaders = singleFieldHeaders ? new CombinedHttpHeaders(validateHeaders)
                                                  : new DefaultHttpHeaders(validateHeaders);
    }

    public DefaultFullHttpResponse(HttpVersion version, HttpResponseStatus status,
            IByteBuffer content, HttpHeaders headers, HttpHeaders trailingHeaders) {
        base(version, status, headers);
        this.content = checkNotNull(content, "content");
        this.trailingHeaders = checkNotNull(trailingHeaders, "trailingHeaders");
    }

    // @Override
    public HttpHeaders trailingHeaders() {
        return trailingHeaders;
    }

    // @Override
    public IByteBuffer content() {
        return content;
    }

    // @Override
    public int refCnt() {
        return content.refCnt();
    }

    // @Override
    public FullHttpResponse retain() {
        content.retain();
        return this;
    }

    // @Override
    public FullHttpResponse retain(int increment) {
        content.retain(increment);
        return this;
    }

    // @Override
    public FullHttpResponse touch() {
        content.touch();
        return this;
    }

    // @Override
    public FullHttpResponse touch(object hint) {
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
    public FullHttpResponse setProtocolVersion(HttpVersion version) {
        base.setProtocolVersion(version);
        return this;
    }

    // @Override
    public FullHttpResponse setStatus(HttpResponseStatus status) {
        base.setStatus(status);
        return this;
    }

    // @Override
    public FullHttpResponse copy() {
        return replace(content().copy());
    }

    // @Override
    public FullHttpResponse duplicate() {
        return replace(content().duplicate());
    }

    // @Override
    public FullHttpResponse retainedDuplicate() {
        return replace(content().retainedDuplicate());
    }

    // @Override
    public FullHttpResponse replace(IByteBuffer content) {
        return new DefaultFullHttpResponse(protocolVersion(), status(), content, headers(), trailingHeaders());
    }

    // @Override
    public int GetHashCode() {
        int hash = this.hash;
        if (hash == 0) {
            if (content().refCnt() != 0) {
                try {
                    hash = 31 + content().GetHashCode();
                } catch (IllegalReferenceCountException ignored) {
                    // Handle race condition between checking refCnt() == 0 and using the object.
                    hash = 31;
                }
            } else {
                hash = 31;
            }
            hash = 31 * hash + trailingHeaders().GetHashCode();
            hash = 31 * hash + base.GetHashCode();
            this.hash = hash;
        }
        return hash;
    }

    // @Override
    public bool Equals(object o) {
        if (!(o is DefaultFullHttpResponse)) {
            return false;
        }

        DefaultFullHttpResponse other = (DefaultFullHttpResponse) o;

        return base.Equals(other) &&
               content().Equals(other.content()) &&
               trailingHeaders().Equals(other.trailingHeaders());
    }

    // @Override
    public string toString() {
        return HttpMessageUtil.appendFullResponse(new StringBuilder(256), this).ToString();
    }
}
