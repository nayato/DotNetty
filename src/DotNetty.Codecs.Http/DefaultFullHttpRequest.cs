// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http {
    using System.Text;
    using DotNetty.Buffers;

    /**
 * Default implementation of {@link FullHttpRequest}.
 */
public class DefaultFullHttpRequest : DefaultHttpRequest, FullHttpRequest {
    private readonly IByteBuffer content;
    private readonly HttpHeaders trailingHeader;

    /**
     * Used to cache the value of the hash code and avoid {@link IllegalReferenceCountException}.
     */
    private int hash;

    public DefaultFullHttpRequest(HttpVersion httpVersion, HttpMethod method, string uri) {
        this(httpVersion, method, uri, Unpooled.buffer(0));
    }

    public DefaultFullHttpRequest(HttpVersion httpVersion, HttpMethod method, string uri, IByteBuffer content) {
        this(httpVersion, method, uri, content, true);
    }

    public DefaultFullHttpRequest(HttpVersion httpVersion, HttpMethod method, string uri, bool validateHeaders) {
        this(httpVersion, method, uri, Unpooled.buffer(0), validateHeaders);
    }

    public DefaultFullHttpRequest(HttpVersion httpVersion, HttpMethod method, string uri,
                                  IByteBuffer content, bool validateHeaders) {
        base(httpVersion, method, uri, validateHeaders);
        this.content = checkNotNull(content, "content");
        trailingHeader = new DefaultHttpHeaders(validateHeaders);
    }

    public DefaultFullHttpRequest(HttpVersion httpVersion, HttpMethod method, string uri,
            IByteBuffer content, HttpHeaders headers, HttpHeaders trailingHeader) {
        base(httpVersion, method, uri, headers);
        this.content = checkNotNull(content, "content");
        this.trailingHeader = checkNotNull(trailingHeader, "trailingHeader");
    }

    // @Override
    public HttpHeaders trailingHeaders() {
        return trailingHeader;
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
    public FullHttpRequest retain() {
        content.retain();
        return this;
    }

    // @Override
    public FullHttpRequest retain(int increment) {
        content.retain(increment);
        return this;
    }

    // @Override
    public FullHttpRequest touch() {
        content.touch();
        return this;
    }

    // @Override
    public FullHttpRequest touch(object hint) {
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
    public FullHttpRequest setProtocolVersion(HttpVersion version) {
        base.setProtocolVersion(version);
        return this;
    }

    // @Override
    public FullHttpRequest setMethod(HttpMethod method) {
        base.setMethod(method);
        return this;
    }

    // @Override
    public FullHttpRequest setUri(string uri) {
        base.setUri(uri);
        return this;
    }

    // @Override
    public FullHttpRequest copy() {
        return replace(content().copy());
    }

    // @Override
    public FullHttpRequest duplicate() {
        return replace(content().duplicate());
    }

    // @Override
    public FullHttpRequest retainedDuplicate() {
        return replace(content().retainedDuplicate());
    }

    // @Override
    public FullHttpRequest replace(IByteBuffer content) {
        return new DefaultFullHttpRequest(protocolVersion(), method(), uri(), content, headers(), trailingHeaders());
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
        if (!(o is DefaultFullHttpRequest)) {
            return false;
        }

        DefaultFullHttpRequest other = (DefaultFullHttpRequest) o;

        return base.Equals(other) &&
               content().Equals(other.content()) &&
               trailingHeaders().Equals(other.trailingHeaders());
    }

    // @Override
    public string toString() {
        return HttpMessageUtil.appendFullRequest(new StringBuilder(256), this).ToString();
    }
}
