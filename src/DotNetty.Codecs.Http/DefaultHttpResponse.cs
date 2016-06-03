// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http {



/**
 * The default {@link HttpResponse} implementation.
 */
public class DefaultHttpResponse : DefaultHttpMessage, HttpResponse {

    private HttpResponseStatus status;

    /**
     * Creates a new instance.
     *
     * @param version the HTTP version of this response
     * @param status  the status of this response
     */
    public DefaultHttpResponse(HttpVersion version, HttpResponseStatus status) {
        this(version, status, true, false);
    }

    /**
     * Creates a new instance.
     *
     * @param version           the HTTP version of this response
     * @param status            the status of this response
     * @param validateHeaders   validate the header names and values when adding them to the {@link HttpHeaders}
     */
    public DefaultHttpResponse(HttpVersion version, HttpResponseStatus status, bool validateHeaders) {
        this(version, status, validateHeaders, false);
    }

    /**
     * Creates a new instance.
     *
     * @param version           the HTTP version of this response
     * @param status            the status of this response
     * @param validateHeaders   validate the header names and values when adding them to the {@link HttpHeaders}
     * @param singleFieldHeaders {@code true} to check and enforce that headers with the same name are appended
     * to the same entry and comma separated.
     * See <a href="https://tools.ietf.org/html/rfc7230#section-3.2.2">RFC 7230, 3.2.2</a>.
     * {@code false} to allow multiple header entries with the same name to
     * coexist.
     */
    public DefaultHttpResponse(HttpVersion version, HttpResponseStatus status, bool validateHeaders,
                               bool singleFieldHeaders) {
        base(version, validateHeaders, singleFieldHeaders);
        this.status = checkNotNull(status, "status");
    }

    /**
     * Creates a new instance.
     *
     * @param version           the HTTP version of this response
     * @param status            the status of this response
     * @param headers           the headers for this HTTP Response
     */
    public DefaultHttpResponse(HttpVersion version, HttpResponseStatus status, HttpHeaders headers) {
        base(version, headers);
        this.status = checkNotNull(status, "status");
    }

    // @Override
    [Obsolete]
    public HttpResponseStatus getStatus() {
        return status();
    }

    // @Override
    public HttpResponseStatus status() {
        return status;
    }

    // @Override
    public HttpResponse setStatus(HttpResponseStatus status) {
        if (status == null) {
            throw new ArgumentNullException(nameof(status");
        }
        this.status = status;
        return this;
    }

    // @Override
    public HttpResponse setProtocolVersion(HttpVersion version) {
        base.setProtocolVersion(version);
        return this;
    }

    // @Override
    public string toString() {
        return HttpMessageUtil.appendResponse(new StringBuilder(256), this).ToString();
    }
}
