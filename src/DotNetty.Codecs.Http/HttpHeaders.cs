// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http {
    using System;
    using System.Collections.Generic;
    using DotNetty.Buffers;
    using DotNetty.Common.Utilities;

    /**
 * Provides the constants for the standard HTTP header names and values and
 * commonly used utility methods that accesses an {@link HttpMessage}.
 */
public abstract class HttpHeaders : IEnumerable<KeyValuePair<string, string>> {
    /**
     * @deprecated Use {@link EmptyHttpHeaders#INSTANCE}.
     * <p>
     * The instance is instantiated here to break the cyclic static initialization between {@link EmptyHttpHeaders} and
     * {@link HttpHeaders}. The issue is that if someone accesses {@link EmptyHttpHeaders#INSTANCE} before
     * {@link HttpHeaders#EMPTY_HEADERS} then {@link HttpHeaders#EMPTY_HEADERS} will be {@code null}.
     */
    [Obsolete]
    public static readonly HttpHeaders EMPTY_HEADERS = EmptyHttpHeaders.instance();

    /**
     * @deprecated Use {@link HttpHeaderNames} instead.
     *
     * Standard HTTP header names.
     */
    [Obsolete]
    public static class Names {
        /**
         * {@code "Accept"}
         */
        public static readonly string ACCEPT = "Accept";
        /**
         * {@code "Accept-Charset"}
         */
        public static readonly string ACCEPT_CHARSET = "Accept-Charset";
        /**
         * {@code "Accept-Encoding"}
         */
        public static readonly string ACCEPT_ENCODING = "Accept-Encoding";
        /**
         * {@code "Accept-Language"}
         */
        public static readonly string ACCEPT_LANGUAGE = "Accept-Language";
        /**
         * {@code "Accept-Ranges"}
         */
        public static readonly string ACCEPT_RANGES = "Accept-Ranges";
        /**
         * {@code "Accept-Patch"}
         */
        public static readonly string ACCEPT_PATCH = "Accept-Patch";
        /**
         * {@code "Access-Control-Allow-Credentials"}
         */
        public static readonly string ACCESS_CONTROL_ALLOW_CREDENTIALS = "Access-Control-Allow-Credentials";
        /**
         * {@code "Access-Control-Allow-Headers"}
         */
        public static readonly string ACCESS_CONTROL_ALLOW_HEADERS = "Access-Control-Allow-Headers";
        /**
         * {@code "Access-Control-Allow-Methods"}
         */
        public static readonly string ACCESS_CONTROL_ALLOW_METHODS = "Access-Control-Allow-Methods";
        /**
         * {@code "Access-Control-Allow-Origin"}
         */
        public static readonly string ACCESS_CONTROL_ALLOW_ORIGIN = "Access-Control-Allow-Origin";
        /**
         * {@code "Access-Control-Expose-Headers"}
         */
        public static readonly string ACCESS_CONTROL_EXPOSE_HEADERS = "Access-Control-Expose-Headers";
        /**
         * {@code "Access-Control-Max-Age"}
         */
        public static readonly string ACCESS_CONTROL_MAX_AGE = "Access-Control-Max-Age";
        /**
         * {@code "Access-Control-Request-Headers"}
         */
        public static readonly string ACCESS_CONTROL_REQUEST_HEADERS = "Access-Control-Request-Headers";
        /**
         * {@code "Access-Control-Request-Method"}
         */
        public static readonly string ACCESS_CONTROL_REQUEST_METHOD = "Access-Control-Request-Method";
        /**
         * {@code "Age"}
         */
        public static readonly string AGE = "Age";
        /**
         * {@code "Allow"}
         */
        public static readonly string ALLOW = "Allow";
        /**
         * {@code "Authorization"}
         */
        public static readonly string AUTHORIZATION = "Authorization";
        /**
         * {@code "Cache-Control"}
         */
        public static readonly string CACHE_CONTROL = "Cache-Control";
        /**
         * {@code "Connection"}
         */
        public static readonly string CONNECTION = "Connection";
        /**
         * {@code "Content-Base"}
         */
        public static readonly string CONTENT_BASE = "Content-Base";
        /**
         * {@code "Content-Encoding"}
         */
        public static readonly string CONTENT_ENCODING = "Content-Encoding";
        /**
         * {@code "Content-Language"}
         */
        public static readonly string CONTENT_LANGUAGE = "Content-Language";
        /**
         * {@code "Content-Length"}
         */
        public static readonly string CONTENT_LENGTH = "Content-Length";
        /**
         * {@code "Content-Location"}
         */
        public static readonly string CONTENT_LOCATION = "Content-Location";
        /**
         * {@code "Content-Transfer-Encoding"}
         */
        public static readonly string CONTENT_TRANSFER_ENCODING = "Content-Transfer-Encoding";
        /**
         * {@code "Content-MD5"}
         */
        public static readonly string CONTENT_MD5 = "Content-MD5";
        /**
         * {@code "Content-Range"}
         */
        public static readonly string CONTENT_RANGE = "Content-Range";
        /**
         * {@code "Content-Type"}
         */
        public static readonly string CONTENT_TYPE = "Content-Type";
        /**
         * {@code "Cookie"}
         */
        public static readonly string COOKIE = "Cookie";
        /**
         * {@code "Date"}
         */
        public static readonly string DATE = "Date";
        /**
         * {@code "ETag"}
         */
        public static readonly string ETAG = "ETag";
        /**
         * {@code "Expect"}
         */
        public static readonly string EXPECT = "Expect";
        /**
         * {@code "Expires"}
         */
        public static readonly string EXPIRES = "Expires";
        /**
         * {@code "From"}
         */
        public static readonly string FROM = "From";
        /**
         * {@code "Host"}
         */
        public static readonly string HOST = "Host";
        /**
         * {@code "If-Match"}
         */
        public static readonly string IF_MATCH = "If-Match";
        /**
         * {@code "If-Modified-Since"}
         */
        public static readonly string IF_MODIFIED_SINCE = "If-Modified-Since";
        /**
         * {@code "If-None-Match"}
         */
        public static readonly string IF_NONE_MATCH = "If-None-Match";
        /**
         * {@code "If-Range"}
         */
        public static readonly string IF_RANGE = "If-Range";
        /**
         * {@code "If-Unmodified-Since"}
         */
        public static readonly string IF_UNMODIFIED_SINCE = "If-Unmodified-Since";
        /**
         * {@code "Last-Modified"}
         */
        public static readonly string LAST_MODIFIED = "Last-Modified";
        /**
         * {@code "Location"}
         */
        public static readonly string LOCATION = "Location";
        /**
         * {@code "Max-Forwards"}
         */
        public static readonly string MAX_FORWARDS = "Max-Forwards";
        /**
         * {@code "Origin"}
         */
        public static readonly string ORIGIN = "Origin";
        /**
         * {@code "Pragma"}
         */
        public static readonly string PRAGMA = "Pragma";
        /**
         * {@code "Proxy-Authenticate"}
         */
        public static readonly string PROXY_AUTHENTICATE = "Proxy-Authenticate";
        /**
         * {@code "Proxy-Authorization"}
         */
        public static readonly string PROXY_AUTHORIZATION = "Proxy-Authorization";
        /**
         * {@code "Range"}
         */
        public static readonly string RANGE = "Range";
        /**
         * {@code "Referer"}
         */
        public static readonly string REFERER = "Referer";
        /**
         * {@code "Retry-After"}
         */
        public static readonly string RETRY_AFTER = "Retry-After";
        /**
         * {@code "Sec-WebSocket-Key1"}
         */
        public static readonly string SEC_WEBSOCKET_KEY1 = "Sec-WebSocket-Key1";
        /**
         * {@code "Sec-WebSocket-Key2"}
         */
        public static readonly string SEC_WEBSOCKET_KEY2 = "Sec-WebSocket-Key2";
        /**
         * {@code "Sec-WebSocket-Location"}
         */
        public static readonly string SEC_WEBSOCKET_LOCATION = "Sec-WebSocket-Location";
        /**
         * {@code "Sec-WebSocket-Origin"}
         */
        public static readonly string SEC_WEBSOCKET_ORIGIN = "Sec-WebSocket-Origin";
        /**
         * {@code "Sec-WebSocket-Protocol"}
         */
        public static readonly string SEC_WEBSOCKET_PROTOCOL = "Sec-WebSocket-Protocol";
        /**
         * {@code "Sec-WebSocket-Version"}
         */
        public static readonly string SEC_WEBSOCKET_VERSION = "Sec-WebSocket-Version";
        /**
         * {@code "Sec-WebSocket-Key"}
         */
        public static readonly string SEC_WEBSOCKET_KEY = "Sec-WebSocket-Key";
        /**
         * {@code "Sec-WebSocket-Accept"}
         */
        public static readonly string SEC_WEBSOCKET_ACCEPT = "Sec-WebSocket-Accept";
        /**
         * {@code "Server"}
         */
        public static readonly string SERVER = "Server";
        /**
         * {@code "Set-Cookie"}
         */
        public static readonly string SET_COOKIE = "Set-Cookie";
        /**
         * {@code "Set-Cookie2"}
         */
        public static readonly string SET_COOKIE2 = "Set-Cookie2";
        /**
         * {@code "TE"}
         */
        public static readonly string TE = "TE";
        /**
         * {@code "Trailer"}
         */
        public static readonly string TRAILER = "Trailer";
        /**
         * {@code "Transfer-Encoding"}
         */
        public static readonly string TRANSFER_ENCODING = "Transfer-Encoding";
        /**
         * {@code "Upgrade"}
         */
        public static readonly string UPGRADE = "Upgrade";
        /**
         * {@code "User-Agent"}
         */
        public static readonly string USER_AGENT = "User-Agent";
        /**
         * {@code "Vary"}
         */
        public static readonly string VARY = "Vary";
        /**
         * {@code "Via"}
         */
        public static readonly string VIA = "Via";
        /**
         * {@code "Warning"}
         */
        public static readonly string WARNING = "Warning";
        /**
         * {@code "WebSocket-Location"}
         */
        public static readonly string WEBSOCKET_LOCATION = "WebSocket-Location";
        /**
         * {@code "WebSocket-Origin"}
         */
        public static readonly string WEBSOCKET_ORIGIN = "WebSocket-Origin";
        /**
         * {@code "WebSocket-Protocol"}
         */
        public static readonly string WEBSOCKET_PROTOCOL = "WebSocket-Protocol";
        /**
         * {@code "WWW-Authenticate"}
         */
        public static readonly string WWW_AUTHENTICATE = "WWW-Authenticate";
    }

    /**
     * @deprecated Use {@link HttpHeaderValues} instead.
     *
     * Standard HTTP header values.
     */
    [Obsolete]
    public static class Values {
        /**
         * {@code "application/x-www-form-urlencoded"}
         */
        public static readonly string APPLICATION_X_WWW_FORM_URLENCODED =
            "application/x-www-form-urlencoded";
        /**
         * {@code "base64"}
         */
        public static readonly string BASE64 = "base64";
        /**
         * {@code "binary"}
         */
        public static readonly string BINARY = "binary";
        /**
         * {@code "boundary"}
         */
        public static readonly string BOUNDARY = "boundary";
        /**
         * {@code "bytes"}
         */
        public static readonly string BYTES = "bytes";
        /**
         * {@code "charset"}
         */
        public static readonly string CHARSET = "charset";
        /**
         * {@code "chunked"}
         */
        public static readonly string CHUNKED = "chunked";
        /**
         * {@code "close"}
         */
        public static readonly string CLOSE = "close";
        /**
         * {@code "compress"}
         */
        public static readonly string COMPRESS = "compress";
        /**
         * {@code "100-continue"}
         */
        public static readonly string CONTINUE =  "100-continue";
        /**
         * {@code "deflate"}
         */
        public static readonly string DEFLATE = "deflate";
        /**
         * {@code "gzip"}
         */
        public static readonly string GZIP = "gzip";
        /**
         * {@code "identity"}
         */
        public static readonly string IDENTITY = "identity";
        /**
         * {@code "keep-alive"}
         */
        public static readonly string KEEP_ALIVE = "keep-alive";
        /**
         * {@code "max-age"}
         */
        public static readonly string MAX_AGE = "max-age";
        /**
         * {@code "max-stale"}
         */
        public static readonly string MAX_STALE = "max-stale";
        /**
         * {@code "min-fresh"}
         */
        public static readonly string MIN_FRESH = "min-fresh";
        /**
         * {@code "multipart/form-data"}
         */
        public static readonly string MULTIPART_FORM_DATA = "multipart/form-data";
        /**
         * {@code "must-revalidate"}
         */
        public static readonly string MUST_REVALIDATE = "must-revalidate";
        /**
         * {@code "no-cache"}
         */
        public static readonly string NO_CACHE = "no-cache";
        /**
         * {@code "no-store"}
         */
        public static readonly string NO_STORE = "no-store";
        /**
         * {@code "no-transform"}
         */
        public static readonly string NO_TRANSFORM = "no-transform";
        /**
         * {@code "none"}
         */
        public static readonly string NONE = "none";
        /**
         * {@code "only-if-cached"}
         */
        public static readonly string ONLY_IF_CACHED = "only-if-cached";
        /**
         * {@code "private"}
         */
        public static readonly string PRIVATE = "private";
        /**
         * {@code "proxy-revalidate"}
         */
        public static readonly string PROXY_REVALIDATE = "proxy-revalidate";
        /**
         * {@code "public"}
         */
        public static readonly string PUBLIC = "public";
        /**
         * {@code "quoted-printable"}
         */
        public static readonly string QUOTED_PRINTABLE = "quoted-printable";
        /**
         * {@code "s-maxage"}
         */
        public static readonly string S_MAXAGE = "s-maxage";
        /**
         * {@code "trailers"}
         */
        public static readonly string TRAILERS = "trailers";
        /**
         * {@code "Upgrade"}
         */
        public static readonly string UPGRADE = "Upgrade";
        /**
         * {@code "WebSocket"}
         */
        public static readonly string WEBSOCKET = "WebSocket";
    }

    /**
     * @deprecated Use {@link HttpUtil#isKeepAlive(HttpMessage)} instead.
     *
     * Returns {@code true} if and only if the connection can remain open and
     * thus 'kept alive'.  This methods respects the value of the
     * {@code "Connection"} header first and then the return value of
     * {@link HttpVersion#isKeepAliveDefault()}.
     */
    [Obsolete]
    public static bool isKeepAlive(HttpMessage message) {
        return HttpUtil.isKeepAlive(message);
    }

    /**
     * @deprecated Use {@link HttpUtil#setKeepAlive(HttpMessage, bool)} instead.
     *
     * Sets the value of the {@code "Connection"} header depending on the
     * protocol version of the specified message.  This getMethod sets or removes
     * the {@code "Connection"} header depending on what the default keep alive
     * mode of the message's protocol version is, as specified by
     * {@link HttpVersion#isKeepAliveDefault()}.
     * <ul>
     * <li>If the connection is kept alive by default:
     *     <ul>
     *     <li>set to {@code "close"} if {@code keepAlive} is {@code false}.</li>
     *     <li>remove otherwise.</li>
     *     </ul></li>
     * <li>If the connection is closed by default:
     *     <ul>
     *     <li>set to {@code "keep-alive"} if {@code keepAlive} is {@code true}.</li>
     *     <li>remove otherwise.</li>
     *     </ul></li>
     * </ul>
     */
    [Obsolete]
    public static void setKeepAlive(HttpMessage message, bool keepAlive) {
        HttpUtil.setKeepAlive(message, keepAlive);
    }

    /**
     * @deprecated Use {@link #get(CharSequence)} instead.
     */
    [Obsolete]
    public static string getHeader(HttpMessage message, string name) {
        return message.headers().get(name);
    }

    /**
     * @deprecated Use {@link #get(CharSequence)} instead.
     *
     * Returns the header value with the specified header name.  If there are
     * more than one header value for the specified header name, the first
     * value is returned.
     *
     * @return the header value or {@code null} if there is no such header
     */
    [Obsolete]
    public static string getHeader(HttpMessage message, CharSequence name) {
        return message.headers().get(name);
    }

    /**
     * @deprecated Use {@link #get(CharSequence, string)} instead.
     *
     * @see {@link #getHeader(HttpMessage, CharSequence, string)}
     */
    [Obsolete]
    public static string getHeader(HttpMessage message, string name, string defaultValue) {
        return message.headers().get(name, defaultValue);
    }

    /**
     * @deprecated Use {@link #get(CharSequence, string)} instead.
     *
     * Returns the header value with the specified header name.  If there are
     * more than one header value for the specified header name, the first
     * value is returned.
     *
     * @return the header value or the {@code defaultValue} if there is no such
     *         header
     */
    [Obsolete]
    public static string getHeader(HttpMessage message, CharSequence name, string defaultValue) {
        return message.headers().get(name, defaultValue);
    }

    /**
     * @deprecated Use {@link #set(CharSequence, object)} instead.
     *
     * @see {@link #setHeader(HttpMessage, CharSequence, object)}
     */
    [Obsolete]
    public static void setHeader(HttpMessage message, string name, object value) {
        message.headers().set(name, value);
    }

    /**
     * @deprecated Use {@link #set(CharSequence, object)} instead.
     *
     * Sets a new header with the specified name and value.  If there is an
     * existing header with the same name, the existing header is removed.
     * If the specified value is not a {@link string}, it is converted into a
     * {@link string} by {@link object#toString()}, except for {@link Date}
     * and {@link Calendar} which are formatted to the date format defined in
     * <a href="http://www.w3.org/Protocols/rfc2616/rfc2616-sec3.html#sec3.3.1">RFC2616</a>.
     */
    [Obsolete]
    public static void setHeader(HttpMessage message, CharSequence name, object value) {
        message.headers().set(name, value);
    }

    /**
     * @deprecated Use {@link #set(CharSequence, Iterable)} instead.
     *
     * @see {@link #setHeader(HttpMessage, CharSequence, Iterable)}
     */
    [Obsolete]
    public static void setHeader(HttpMessage message, string name, Iterable<?> values) {
        message.headers().set(name, values);
    }

    /**
     * @deprecated Use {@link #set(CharSequence, Iterable)} instead.
     *
     * Sets a new header with the specified name and values.  If there is an
     * existing header with the same name, the existing header is removed.
     * This getMethod can be represented approximately as the following code:
     * <pre>
     * removeHeader(message, name);
     * for (object v: values) {
     *     if (v == null) {
     *         break;
     *     }
     *     addHeader(message, name, v);
     * }
     * </pre>
     */
    [Obsolete]
    public static void setHeader(HttpMessage message, CharSequence name, Iterable<?> values) {
        message.headers().set(name, values);
    }

    /**
     * @deprecated Use {@link #add(CharSequence, object)} instead.
     *
     * @see {@link #addHeader(HttpMessage, CharSequence, object)}
     */
    [Obsolete]
    public static void addHeader(HttpMessage message, string name, object value) {
        message.headers().add(name, value);
    }

    /**
     * @deprecated Use {@link #add(CharSequence, object)} instead.
     *
     * Adds a new header with the specified name and value.
     * If the specified value is not a {@link string}, it is converted into a
     * {@link string} by {@link object#toString()}, except for {@link Date}
     * and {@link Calendar} which are formatted to the date format defined in
     * <a href="http://www.w3.org/Protocols/rfc2616/rfc2616-sec3.html#sec3.3.1">RFC2616</a>.
     */
    [Obsolete]
    public static void addHeader(HttpMessage message, CharSequence name, object value) {
        message.headers().add(name, value);
    }

    /**
     * @deprecated Use {@link #remove(CharSequence)} instead.
     *
     * @see {@link #removeHeader(HttpMessage, CharSequence)}
     */
    [Obsolete]
    public static void removeHeader(HttpMessage message, string name) {
        message.headers().remove(name);
    }

    /**
     * @deprecated Use {@link #remove(CharSequence)} instead.
     *
     * Removes the header with the specified name.
     */
    [Obsolete]
    public static void removeHeader(HttpMessage message, CharSequence name) {
        message.headers().remove(name);
    }

    /**
     * @deprecated Use {@link #clear()} instead.
     *
     * Removes all headers from the specified message.
     */
    [Obsolete]
    public static void clearHeaders(HttpMessage message) {
        message.headers().clear();
    }

    /**
     * @deprecated Use {@link #getInt(CharSequence)} instead.
     *
     * @see {@link #getIntHeader(HttpMessage, CharSequence)}
     */
    [Obsolete]
    public static int getIntHeader(HttpMessage message, string name) {
        return getIntHeader(message, (CharSequence) name);
    }

    /**
     * @deprecated Use {@link #getInt(CharSequence)} instead.
     *
     * Returns the integer header value with the specified header name.  If
     * there are more than one header value for the specified header name, the
     * first value is returned.
     *
     * @return the header value
     * @
     *         if there is no such header or the header value is not a number
     */
    [Obsolete]
    public static int getIntHeader(HttpMessage message, CharSequence name) {
        string value = message.headers().get(name);
        if (value == null) {
            throw new NumberFormatException("header not found: " + name);
        }
        return Integer.parseInt(value);
    }

    /**
     * @deprecated Use {@link #getInt(CharSequence, int)} instead.
     *
     * @see {@link #getIntHeader(HttpMessage, CharSequence, int)}
     */
    [Obsolete]
    public static int getIntHeader(HttpMessage message, string name, int defaultValue) {
        return message.headers().getInt(name, defaultValue);
    }

    /**
     * @deprecated Use {@link #getInt(CharSequence, int)} instead.
     *
     * Returns the integer header value with the specified header name.  If
     * there are more than one header value for the specified header name, the
     * first value is returned.
     *
     * @return the header value or the {@code defaultValue} if there is no such
     *         header or the header value is not a number
     */
    [Obsolete]
    public static int getIntHeader(HttpMessage message, CharSequence name, int defaultValue) {
        return message.headers().getInt(name, defaultValue);
    }

    /**
     * @deprecated Use {@link #setInt(CharSequence, int)} instead.
     *
     * @see {@link #setIntHeader(HttpMessage, CharSequence, int)}
     */
    [Obsolete]
    public static void setIntHeader(HttpMessage message, string name, int value) {
        message.headers().setInt(name, value);
    }

    /**
     * @deprecated Use {@link #setInt(CharSequence, int)} instead.
     *
     * Sets a new integer header with the specified name and value.  If there
     * is an existing header with the same name, the existing header is removed.
     */
    [Obsolete]
    public static void setIntHeader(HttpMessage message, CharSequence name, int value) {
        message.headers().setInt(name, value);
    }

    /**
     * @deprecated Use {@link #set(CharSequence, Iterable)} instead.
     *
     * @see {@link #setIntHeader(HttpMessage, CharSequence, Iterable)}
     */
    [Obsolete]
    public static void setIntHeader(HttpMessage message, string name, Iterable<Integer> values) {
        message.headers().set(name, values);
    }

    /**
     * @deprecated Use {@link #set(CharSequence, Iterable)} instead.
     *
     * Sets a new integer header with the specified name and values.  If there
     * is an existing header with the same name, the existing header is removed.
     */
    [Obsolete]
    public static void setIntHeader(HttpMessage message, CharSequence name, Iterable<Integer> values) {
        message.headers().set(name, values);
    }

    /**
     * @deprecated Use {@link #add(CharSequence, Iterable)} instead.
     *
     * @see {@link #addIntHeader(HttpMessage, CharSequence, int)}
     */
    [Obsolete]
    public static void addIntHeader(HttpMessage message, string name, int value) {
        message.headers().add(name, value);
    }

    /**
     * @deprecated Use {@link #addInt(CharSequence, int)} instead.
     *
     * Adds a new integer header with the specified name and value.
     */
    [Obsolete]
    public static void addIntHeader(HttpMessage message, CharSequence name, int value) {
        message.headers().addInt(name, value);
    }

    /**
     * @deprecated Use {@link #getTimeMillis(CharSequence)} instead.
     *
     * @see {@link #getDateHeader(HttpMessage, CharSequence)}
     */
    [Obsolete]
    public static Date getDateHeader(HttpMessage message, string name)  {
        return getDateHeader(message, (CharSequence) name);
    }

    /**
     * @deprecated Use {@link #getTimeMillis(CharSequence)} instead.
     *
     * Returns the date header value with the specified header name.  If
     * there are more than one header value for the specified header name, the
     * first value is returned.
     *
     * @return the header value
     * @
     *         if there is no such header or the header value is not a formatted date
     */
    [Obsolete]
    public static Date getDateHeader(HttpMessage message, CharSequence name)  {
        string value = message.headers().get(name);
        if (value == null) {
            throw new ParseException("header not found: " + name, 0);
        }
        return HttpHeaderDateFormat.get().parse(value);
    }

    /**
     * @deprecated Use {@link #getTimeMillis(CharSequence, long)} instead.
     *
     * @see {@link #getDateHeader(HttpMessage, CharSequence, Date)}
     */
    [Obsolete]
    public static Date getDateHeader(HttpMessage message, string name, Date defaultValue) {
        return getDateHeader(message, (CharSequence) name, defaultValue);
    }

    /**
     * @deprecated Use {@link #getTimeMillis(CharSequence, long)} instead.
     *
     * Returns the date header value with the specified header name.  If
     * there are more than one header value for the specified header name, the
     * first value is returned.
     *
     * @return the header value or the {@code defaultValue} if there is no such
     *         header or the header value is not a formatted date
     */
    [Obsolete]
    public static Date getDateHeader(HttpMessage message, CharSequence name, Date defaultValue) {
        readonly string value = getHeader(message, name);
        if (value == null) {
            return defaultValue;
        }

        try {
            return HttpHeaderDateFormat.get().parse(value);
        } catch (ParseException ignored) {
            return defaultValue;
        }
    }

    /**
     * @deprecated Use {@link #set(CharSequence, object)} instead.
     *
     * @see {@link #setDateHeader(HttpMessage, CharSequence, Date)}
     */
    [Obsolete]
    public static void setDateHeader(HttpMessage message, string name, Date value) {
        setDateHeader(message, (CharSequence) name, value);
    }

    /**
     * @deprecated Use {@link #set(CharSequence, object)} instead.
     *
     * Sets a new date header with the specified name and value.  If there
     * is an existing header with the same name, the existing header is removed.
     * The specified value is formatted as defined in
     * <a href="http://www.w3.org/Protocols/rfc2616/rfc2616-sec3.html#sec3.3.1">RFC2616</a>
     */
    [Obsolete]
    public static void setDateHeader(HttpMessage message, CharSequence name, Date value) {
        if (value != null) {
            message.headers().set(name, HttpHeaderDateFormat.get().format(value));
        } else {
            message.headers().set(name, null);
        }
    }

    /**
     * @deprecated Use {@link #set(CharSequence, Iterable)} instead.
     *
     * @see {@link #setDateHeader(HttpMessage, CharSequence, Iterable)}
     */
    [Obsolete]
    public static void setDateHeader(HttpMessage message, string name, Iterable<Date> values) {
        message.headers().set(name, values);
    }

    /**
     * @deprecated Use {@link #set(CharSequence, Iterable)} instead.
     *
     * Sets a new date header with the specified name and values.  If there
     * is an existing header with the same name, the existing header is removed.
     * The specified values are formatted as defined in
     * <a href="http://www.w3.org/Protocols/rfc2616/rfc2616-sec3.html#sec3.3.1">RFC2616</a>
     */
    [Obsolete]
    public static void setDateHeader(HttpMessage message, CharSequence name, Iterable<Date> values) {
        message.headers().set(name, values);
    }

    /**
     * @deprecated Use {@link #add(CharSequence, object)} instead.
     *
     * @see {@link #addDateHeader(HttpMessage, CharSequence, Date)}
     */
    [Obsolete]
    public static void addDateHeader(HttpMessage message, string name, Date value) {
        message.headers().add(name, value);
    }

    /**
     * @deprecated Use {@link #add(CharSequence, object)} instead.
     *
     * Adds a new date header with the specified name and value.  The specified
     * value is formatted as defined in
     * <a href="http://www.w3.org/Protocols/rfc2616/rfc2616-sec3.html#sec3.3.1">RFC2616</a>
     */
    [Obsolete]
    public static void addDateHeader(HttpMessage message, CharSequence name, Date value) {
        message.headers().add(name, value);
    }

    /**
     * @deprecated Use {@link HttpUtil#getContentLength(HttpMessage)} instead.
     *
     * Returns the length of the content.  Please note that this value is
     * not retrieved from {@link HttpContent#content()} but from the
     * {@code "Content-Length"} header, and thus they are independent from each
     * other.
     *
     * @return the content length
     *
     * @
     *         if the message does not have the {@code "Content-Length"} header
     *         or its value is not a number
     */
    [Obsolete]
    public static long getContentLength(HttpMessage message) {
        return HttpUtil.getContentLength(message);
    }

    /**
     * @deprecated Use {@link HttpUtil#getContentLength(HttpMessage, long)} instead.
     *
     * Returns the length of the content.  Please note that this value is
     * not retrieved from {@link HttpContent#content()} but from the
     * {@code "Content-Length"} header, and thus they are independent from each
     * other.
     *
     * @return the content length or {@code defaultValue} if this message does
     *         not have the {@code "Content-Length"} header or its value is not
     *         a number
     */
    [Obsolete]
    public static long getContentLength(HttpMessage message, long defaultValue) {
        return HttpUtil.getContentLength(message, defaultValue);
    }

    /**
     * @deprecated Use {@link HttpUtil#setContentLength(HttpMessage, long)} instead.
     */
    [Obsolete]
    public static void setContentLength(HttpMessage message, long length) {
        HttpUtil.setContentLength(message, length);
    }

    /**
     * @deprecated Use {@link #get(CharSequence)} instead.
     *
     * Returns the value of the {@code "Host"} header.
     */
    [Obsolete]
    public static string getHost(HttpMessage message) {
        return message.headers().get(HttpHeaderNames.HOST);
    }

    /**
     * @deprecated Use {@link #get(CharSequence, string)} instead.
     *
     * Returns the value of the {@code "Host"} header.  If there is no such
     * header, the {@code defaultValue} is returned.
     */
    [Obsolete]
    public static string getHost(HttpMessage message, string defaultValue) {
        return message.headers().get(HttpHeaderNames.HOST, defaultValue);
    }

    /**
     * @deprecated Use {@link #set(CharSequence, object)} instead.
     *
     * @see {@link #setHost(HttpMessage, CharSequence)}
     */
    [Obsolete]
    public static void setHost(HttpMessage message, string value) {
        message.headers().set(HttpHeaderNames.HOST, value);
    }

    /**
     * @deprecated Use {@link #set(CharSequence, object)} instead.
     *
     * Sets the {@code "Host"} header.
     */
    [Obsolete]
    public static void setHost(HttpMessage message, CharSequence value) {
        message.headers().set(HttpHeaderNames.HOST, value);
    }

    /**
     * @deprecated Use {@link #getTimeMillis(CharSequence)} instead.
     *
     * Returns the value of the {@code "Date"} header.
     *
     * @
     *         if there is no such header or the header value is not a formatted date
     */
    [Obsolete]
    public static Date getDate(HttpMessage message)  {
        return getDateHeader(message, HttpHeaderNames.DATE);
    }

    /**
     * @deprecated Use {@link #getTimeMillis(CharSequence, long)} instead.
     *
     * Returns the value of the {@code "Date"} header. If there is no such
     * header or the header is not a formatted date, the {@code defaultValue}
     * is returned.
     */
    [Obsolete]
    public static Date getDate(HttpMessage message, Date defaultValue) {
        return getDateHeader(message, HttpHeaderNames.DATE, defaultValue);
    }

    /**
     * @deprecated Use {@link #set(CharSequence, object)} instead.
     *
     * Sets the {@code "Date"} header.
     */
    [Obsolete]
    public static void setDate(HttpMessage message, Date value) {
        message.headers().set(HttpHeaderNames.DATE, value);
    }

    /**
     * @deprecated Use {@link HttpUtil#is100ContinueExpected(HttpMessage)} instead.
     *
     * Returns {@code true} if and only if the specified message contains the
     * {@code "Expect: 100-continue"} header.
     */
    [Obsolete]
    public static bool is100ContinueExpected(HttpMessage message) {
        return HttpUtil.is100ContinueExpected(message);
    }

    /**
     * @deprecated Use {@link HttpUtil#set100ContinueExpected(HttpMessage, bool)} instead.
     *
     * Sets the {@code "Expect: 100-continue"} header to the specified message.
     * If there is any existing {@code "Expect"} header, they are replaced with
     * the new one.
     */
    [Obsolete]
    public static void set100ContinueExpected(HttpMessage message) {
        HttpUtil.set100ContinueExpected(message, true);
    }

    /**
     * @deprecated Use {@link HttpUtil#set100ContinueExpected(HttpMessage, bool)} instead.
     *
     * Sets or removes the {@code "Expect: 100-continue"} header to / from the
     * specified message.  If the specified {@code value} is {@code true},
     * the {@code "Expect: 100-continue"} header is set and all other previous
     * {@code "Expect"} headers are removed.  Otherwise, all {@code "Expect"}
     * headers are removed completely.
     */
    [Obsolete]
    public static void set100ContinueExpected(HttpMessage message, bool set) {
        HttpUtil.set100ContinueExpected(message, set);
    }

    /**
     * @deprecated Use {@link HttpUtil#isTransferEncodingChunked(HttpMessage)} instead.
     *
     * Checks to see if the transfer encoding in a specified {@link HttpMessage} is chunked
     *
     * @param message The message to check
     * @return True if transfer encoding is chunked, otherwise false
     */
    [Obsolete]
    public static bool isTransferEncodingChunked(HttpMessage message) {
        return HttpUtil.isTransferEncodingChunked(message);
    }

    /**
     * @deprecated Use {@link HttpUtil#setTransferEncodingChunked(HttpMessage, bool)} instead.
     */
    [Obsolete]
    public static void removeTransferEncodingChunked(HttpMessage m) {
        HttpUtil.setTransferEncodingChunked(m, false);
    }

    /**
     * @deprecated Use {@link HttpUtil#setTransferEncodingChunked(HttpMessage, bool)} instead.
     */
    [Obsolete]
    public static void setTransferEncodingChunked(HttpMessage m) {
        HttpUtil.setTransferEncodingChunked(m, true);
    }

    /**
     * @deprecated Use {@link HttpUtil#isContentLengthSet(HttpMessage)} instead.
     */
    [Obsolete]
    public static bool isContentLengthSet(HttpMessage m) {
        return HttpUtil.isContentLengthSet(m);
    }

    /**
     * @deprecated Use {@link AsciiString#contentEqualsIgnoreCase(CharSequence, CharSequence)} instead.
     */
    [Obsolete]
    public static bool EqualsIgnoreCase(CharSequence name1, CharSequence name2) {
        return AsciiString.ContentEqualsIgnoreCase(name1, name2);
    }

    static void encode(HttpHeaders headers, IByteBuffer buf)  {
        Iterator<Entry<CharSequence, CharSequence>> iter = headers.iteratorCharSequence();
        while (iter.hasNext()) {
            Entry<CharSequence, CharSequence> header = iter.next();
            HttpHeadersEncoder.encoderHeader(header.getKey(), header.getValue(), buf);
        }
    }

    public static void encodeAscii(CharSequence seq, IByteBuffer buf) {
        if (seq is AsciiString) {
            IByteBufferUtil.copy((AsciiString) seq, 0, buf, seq.length());
        } else {
            HttpUtil.encodeAscii0(seq, buf);
        }
    }

    /**
     * @deprecated Use {@link AsciiString} instead.
     * <p>
     * Create a new {@link CharSequence} which is optimized for reuse as {@link HttpHeaders} name or value.
     * So if you have a Header name or value that you want to reuse you should make use of this.
     */
    [Obsolete]
    public static CharSequence newEntity(string name) {
        return new AsciiString(name);
    }

    protected HttpHeaders() { }

    /**
     * @see #get(CharSequence)
     */
    public abstract string get(string name);

    /**
     * Returns the value of a header with the specified name.  If there are
     * more than one values for the specified name, the first value is returned.
     *
     * @param name The name of the header to search
     * @return The first header value or {@code null} if there is no such header
     * @see #getAsString(CharSequence)
     */
    public string get(CharSequence name) => this.get(name.ToString());

    /**
     * Returns the value of a header with the specified name.  If there are
     * more than one values for the specified name, the first value is returned.
     *
     * @param name The name of the header to search
     * @return The first header value or {@code defaultValue} if there is no such header
     */
    public string get(CharSequence name, string defaultValue) {
        string value = get(name);
        if (value == null) {
            return defaultValue;
        }
        return value;
    }

    /**
     * Returns the integer value of a header with the specified name. If there are more than one values for the
     * specified name, the first value is returned.
     *
     * @param name the name of the header to search
     * @return the first header value if the header is found and its value is an integer. {@code null} if there's no
     *         such header or its value is not an integer.
     */
    public abstract int getInt(CharSequence name);

    /**
     * Returns the integer value of a header with the specified name. If there are more than one values for the
     * specified name, the first value is returned.
     *
     * @param name the name of the header to search
     * @param defaultValue the default value
     * @return the first header value if the header is found and its value is an integer. {@code defaultValue} if
     *         there's no such header or its value is not an integer.
     */
    public abstract int getInt(CharSequence name, int defaultValue);

    /**
     * Returns the short value of a header with the specified name. If there are more than one values for the
     * specified name, the first value is returned.
     *
     * @param name the name of the header to search
     * @return the first header value if the header is found and its value is a short. {@code null} if there's no
     *         such header or its value is not a short.
     */
    public abstract Short getShort(CharSequence name);

    /**
     * Returns the short value of a header with the specified name. If there are more than one values for the
     * specified name, the first value is returned.
     *
     * @param name the name of the header to search
     * @param defaultValue the default value
     * @return the first header value if the header is found and its value is a short. {@code defaultValue} if
     *         there's no such header or its value is not a short.
     */
    public abstract short getShort(CharSequence name, short defaultValue);

    /**
     * Returns the date value of a header with the specified name. If there are more than one values for the
     * specified name, the first value is returned.
     *
     * @param name the name of the header to search
     * @return the first header value if the header is found and its value is a date. {@code null} if there's no
     *         such header or its value is not a date.
     */
    public abstract long getTimeMillis(CharSequence name);

    /**
     * Returns the date value of a header with the specified name. If there are more than one values for the
     * specified name, the first value is returned.
     *
     * @param name the name of the header to search
     * @param defaultValue the default value
     * @return the first header value if the header is found and its value is a date. {@code defaultValue} if
     *         there's no such header or its value is not a date.
     */
    public abstract long getTimeMillis(CharSequence name, long defaultValue);

    /**
     * @see #getAll(CharSequence)
     */
    public abstract List<string> getAll(string name);

    /**
     * Returns the values of headers with the specified name
     *
     * @param name The name of the headers to search
     * @return A {@link List} of header values which will be empty if no values
     *         are found
     * @see #getAllAsString(CharSequence)
     */
    public List<string> getAll(CharSequence name) {
        return getAll(name.ToString());
    }

    /**
     * Returns a new {@link List} that contains all headers in this object.  Note that modifying the
     * returned {@link List} will not affect the state of this object.  If you intend to enumerate over the header
     * entries only, use {@link #iterator()} instead, which has much less overhead.
     * @see #iteratorCharSequence()
     */
    public abstract List<KeyValuePair<string, string>> entries();

    /**
     * @see {@link #contains(CharSequence)}
     */
    public abstract bool contains(string name);

    /**
     * @deprecated It is preferred to use {@link #iteratorCharSequence()} unless you need {@link string}.
     * If {@link string} is required then use {@link #iteratorAsString()}.
     */
    [Obsolete]
    // @Override
    public abstract Iterator<Entry<string, string>> iterator();

    /**
     * @return Iterator over the name/value header pairs.
     */
    public abstract Iterator<Entry<CharSequence, CharSequence>> iteratorCharSequence();

    /**
     * Checks to see if there is a header with the specified name
     *
     * @param name The name of the header to search for
     * @return True if at least one header is found
     */
    public bool contains(CharSequence name) {
        return contains(name.ToString());
    }

    /**
     * Checks if no header exists.
     */
    public abstract bool isEmpty();

    /**
     * Returns the number of headers in this object.
     */
    public abstract int size();

    /**
     * Returns a new {@link Set} that contains the names of all headers in this object.  Note that modifying the
     * returned {@link Set} will not affect the state of this object.  If you intend to enumerate over the header
     * entries only, use {@link #iterator()} instead, which has much less overhead.
     */
    public abstract Set<string> names();

    /**
     * @see #add(CharSequence, object)
     */
    public abstract HttpHeaders add(string name, object value);

    /**
     * Adds a new header with the specified name and value.
     *
     * If the specified value is not a {@link string}, it is converted
     * into a {@link string} by {@link object#toString()}, except in the cases
     * of {@link Date} and {@link Calendar}, which are formatted to the date
     * format defined in <a href="http://www.w3.org/Protocols/rfc2616/rfc2616-sec3.html#sec3.3.1">RFC2616</a>.
     *
     * @param name The name of the header being added
     * @param value The value of the header being added
     *
     * @return {@code this}
     */
    public HttpHeaders add(CharSequence name, object value) {
        return add(name.ToString(), value);
    }

    /**
     * @see #add(CharSequence, Iterable)
     */
    public abstract HttpHeaders add(string name, Iterable<?> values);

    /**
     * Adds a new header with the specified name and values.
     *
     * This getMethod can be represented approximately as the following code:
     * <pre>
     * for (object v: values) {
     *     if (v == null) {
     *         break;
     *     }
     *     headers.add(name, v);
     * }
     * </pre>
     *
     * @param name The name of the headers being set
     * @param values The values of the headers being set
     * @return {@code this}
     */
    public HttpHeaders add(CharSequence name, Iterable<?> values) {
        return add(name.ToString(), values);
    }

    /**
     * Adds all header entries of the specified {@code headers}.
     *
     * @return {@code this}
     */
    public HttpHeaders add(HttpHeaders headers) {
        if (headers == null) {
            throw new ArgumentNullException(nameof(headers");
        }
        for (Map.Entry<string, string> e: headers) {
            add(e.getKey(), e.getValue());
        }
        return this;
    }

    /**
     * Add the {@code name} to {@code value}.
     * @param name The name to modify
     * @param value The value
     * @return {@code this}
     */
    public abstract HttpHeaders addInt(CharSequence name, int value);

    /**
     * Add the {@code name} to {@code value}.
     * @param name The name to modify
     * @param value The value
     * @return {@code this}
     */
    public abstract HttpHeaders addShort(CharSequence name, short value);

    /**
     * @see #set(CharSequence, object)
     */
    public abstract HttpHeaders set(string name, object value);

    /**
     * Sets a header with the specified name and value.
     *
     * If there is an existing header with the same name, it is removed.
     * If the specified value is not a {@link string}, it is converted into a
     * {@link string} by {@link object#toString()}, except for {@link Date}
     * and {@link Calendar}, which are formatted to the date format defined in
     * <a href="http://www.w3.org/Protocols/rfc2616/rfc2616-sec3.html#sec3.3.1">RFC2616</a>.
     *
     * @param name The name of the header being set
     * @param value The value of the header being set
     * @return {@code this}
     */
    public HttpHeaders set(CharSequence name, object value) {
        return set(name.ToString(), value);
    }

    /**
     * @see #set(CharSequence, Iterable)
     */
    public abstract HttpHeaders set(string name, Iterable<?> values);

    /**
     * Sets a header with the specified name and values.
     *
     * If there is an existing header with the same name, it is removed.
     * This getMethod can be represented approximately as the following code:
     * <pre>
     * headers.remove(name);
     * for (object v: values) {
     *     if (v == null) {
     *         break;
     *     }
     *     headers.add(name, v);
     * }
     * </pre>
     *
     * @param name The name of the headers being set
     * @param values The values of the headers being set
     * @return {@code this}
     */
    public HttpHeaders set(CharSequence name, Iterable<?> values) {
        return set(name.ToString(), values);
    }

    /**
     * Cleans the current header entries and copies all header entries of the specified {@code headers}.
     *
     * @return {@code this}
     */
    public HttpHeaders set(HttpHeaders headers) {
        checkNotNull(headers, "headers");

        clear();

        if (headers.isEmpty()) {
            return this;
        }

        for (Entry<string, string> entry : headers) {
            add(entry.getKey(), entry.getValue());
        }
        return this;
    }

    /**
     * Retains all current headers but calls {@link #set(string, object)} for each entry in {@code headers}
     *
     * @param headers The headers used to {@link #set(string, object)} values in this instance
     * @return {@code this}
     */
    public HttpHeaders setAll(HttpHeaders headers) {
        checkNotNull(headers, "headers");

        if (headers.isEmpty()) {
            return this;
        }

        for (Entry<string, string> entry : headers) {
            set(entry.getKey(), entry.getValue());
        }
        return this;
    }

    /**
     * Set the {@code name} to {@code value}. This will remove all previous values associated with {@code name}.
     * @param name The name to modify
     * @param value The value
     * @return {@code this}
     */
    public abstract HttpHeaders setInt(CharSequence name, int value);

    /**
     * Set the {@code name} to {@code value}. This will remove all previous values associated with {@code name}.
     * @param name The name to modify
     * @param value The value
     * @return {@code this}
     */
    public abstract HttpHeaders setShort(CharSequence name, short value);

    /**
     * @see #remove(CharSequence)
     */
    public abstract HttpHeaders remove(string name);

    /**
     * Removes the header with the specified name.
     *
     * @param name The name of the header to remove
     * @return {@code this}
     */
    public HttpHeaders remove(CharSequence name) {
        return remove(name.ToString());
    }

    /**
     * Removes all headers from this {@link HttpMessage}.
     *
     * @return {@code this}
     */
    public abstract HttpHeaders clear();

    /**
     * @see #contains(CharSequence, CharSequence, bool)
     */
    public bool contains(string name, string value, bool ignoreCase) {
        List<string> values = getAll(name);
        if (values.isEmpty()) {
            return false;
        }

        for (string v: values) {
            if (ignoreCase) {
                if (v.EqualsIgnoreCase(value)) {
                    return true;
                }
            } else {
                if (v.Equals(value)) {
                    return true;
                }
            }
        }
        return false;
    }

    /**
     * Returns {@code true} if a header with the {@code name} and {@code value} exists, {@code false} otherwise.
     * This also handles multiple values that are seperated with a {@code ,}.
     * <p>
     * If {@code ignoreCase} is {@code true} then a case insensitive compare is done on the value.
     * @param name the name of the header to find
     * @param value the value of the header to find
     * @param ignoreCase {@code true} then a case insensitive compare is run to compare values.
     * otherwise a case sensitive compare is run to compare values.
     */
    public bool containsValue(CharSequence name, CharSequence value, bool ignoreCase) {
        List<string> values = getAll(name);
        if (values.isEmpty()) {
            return false;
        }

        for (string v: values) {
            if (contains(v, value, ignoreCase)) {
                return true;
            }
        }
        return false;
    }

    private static bool contains(string value, CharSequence expected, bool ignoreCase) {
        string[] parts = StringUtil.split(value, ',');
        if (ignoreCase) {
            for (string s: parts) {
                if (AsciiString.ContentEqualsIgnoreCase(expected, s.trim())) {
                    return true;
                }
            }
        } else {
            for (string s: parts) {
                if (AsciiString.ContentEquals(expected, s.trim())) {
                    return true;
                }
            }
        }
        return false;
    }

    /**
     * {@link Headers#get(object)} and convert the result to a {@link string}.
     * @param name the name of the header to retrieve
     * @return the first header value if the header is found. {@code null} if there's no such header.
     */
    public readonly string getAsString(CharSequence name) {
        return get(name);
    }

    /**
     * {@link Headers#getAll(object)} and convert each element of {@link List} to a {@link string}.
     * @param name the name of the header to retrieve
     * @return a {@link List} of header values or an empty {@link List} if no values are found.
     */
    public readonly List<string> getAllAsString(CharSequence name) {
        return getAll(name);
    }

    /**
     * {@link Iterator} that converts each {@link Entry}'s key and value to a {@link string}.
     */
    public readonly Iterator<Entry<string, string>> iteratorAsString() {
        return iterator();
    }

    /**
     * Returns {@code true} if a header with the {@code name} and {@code value} exists, {@code false} otherwise.
     * <p>
     * If {@code ignoreCase} is {@code true} then a case insensitive compare is done on the value.
     * @param name the name of the header to find
     * @param value the value of the header to find
     * @param ignoreCase {@code true} then a case insensitive compare is run to compare values.
     * otherwise a case sensitive compare is run to compare values.
     */
    public bool contains(CharSequence name, CharSequence value, bool ignoreCase) {
        return contains(name.ToString(), value.ToString(), ignoreCase);
    }
}
