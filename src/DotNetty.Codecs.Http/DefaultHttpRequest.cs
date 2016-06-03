// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http {



/**
 * The default {@link HttpRequest} implementation.
 */
public class DefaultHttpRequest : DefaultHttpMessage, HttpRequest {
    private static readonly int HASH_CODE_PRIME = 31;
    private HttpMethod method;
    private string uri;

    /**
     * Creates a new instance.
     *
     * @param httpVersion the HTTP version of the request
     * @param method      the HTTP method of the request
     * @param uri         the URI or path of the request
     */
    public DefaultHttpRequest(HttpVersion httpVersion, HttpMethod method, string uri) {
        this(httpVersion, method, uri, true);
    }

    /**
     * Creates a new instance.
     *
     * @param httpVersion       the HTTP version of the request
     * @param method            the HTTP method of the request
     * @param uri               the URI or path of the request
     * @param validateHeaders   validate the header names and values when adding them to the {@link HttpHeaders}
     */
    public DefaultHttpRequest(HttpVersion httpVersion, HttpMethod method, string uri, bool validateHeaders) {
        base(httpVersion, validateHeaders, false);
        this.method = checkNotNull(method, "method");
        this.uri = checkNotNull(uri, "uri");
    }

    /**
     * Creates a new instance.
     *
     * @param httpVersion       the HTTP version of the request
     * @param method            the HTTP method of the request
     * @param uri               the URI or path of the request
     * @param headers           the Headers for this Request
     */
    public DefaultHttpRequest(HttpVersion httpVersion, HttpMethod method, string uri, HttpHeaders headers) {
        base(httpVersion, headers);
        this.method = checkNotNull(method, "method");
        this.uri = checkNotNull(uri, "uri");
    }

    // @Override
    [Obsolete]
    public HttpMethod getMethod() {
        return method();
    }

    // @Override
    public HttpMethod method() {
        return method;
    }

    // @Override
    [Obsolete]
    public string getUri() {
        return uri();
    }

    // @Override
    public string uri() {
        return uri;
    }

    // @Override
    public HttpRequest setMethod(HttpMethod method) {
        if (method == null) {
            throw new ArgumentNullException(nameof(method");
        }
        this.method = method;
        return this;
    }

    // @Override
    public HttpRequest setUri(string uri) {
        if (uri == null) {
            throw new ArgumentNullException(nameof(uri");
        }
        this.uri = uri;
        return this;
    }

    // @Override
    public HttpRequest setProtocolVersion(HttpVersion version) {
        base.setProtocolVersion(version);
        return this;
    }

    // @Override
    public int GetHashCode() {
        int result = 1;
        result = HASH_CODE_PRIME * result + method.GetHashCode();
        result = HASH_CODE_PRIME * result + uri.GetHashCode();
        result = HASH_CODE_PRIME * result + base.GetHashCode();
        return result;
    }

    // @Override
    public bool Equals(object o) {
        if (!(o is DefaultHttpRequest)) {
            return false;
        }

        DefaultHttpRequest other = (DefaultHttpRequest) o;

        return method().Equals(other.method()) &&
               uri().EqualsIgnoreCase(other.uri()) &&
               base.Equals(o);
    }

    // @Override
    public string toString() {
        return HttpMessageUtil.appendRequest(new StringBuilder(256), this).ToString();
    }
}
