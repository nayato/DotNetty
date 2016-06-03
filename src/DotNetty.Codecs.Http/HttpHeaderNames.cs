// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http
{
    using System;
    using DotNetty.Common.Utilities;

    /**
 * Standard HTTP header names.
 * <p>
 * These are all defined as lowercase to support HTTP/2 requirements while also not
 * violating HTTP/1.x requirements.  New header names should always be lowercase.
 */

    public static class HttpHeaderNames
    {
        /**
         * {@code "accept"}
         */
        public static readonly AsciiString ACCEPT = new AsciiString("accept");
        /**
         * {@code "accept-charset"}
         */
        public static readonly AsciiString ACCEPT_CHARSET = new AsciiString("accept-charset");
        /**
         * {@code "accept-encoding"}
         */
        public static readonly AsciiString ACCEPT_ENCODING = new AsciiString("accept-encoding");
        /**
         * {@code "accept-language"}
         */
        public static readonly AsciiString ACCEPT_LANGUAGE = new AsciiString("accept-language");
        /**
         * {@code "accept-ranges"}
         */
        public static readonly AsciiString ACCEPT_RANGES = new AsciiString("accept-ranges");
        /**
         * {@code "accept-patch"}
         */
        public static readonly AsciiString ACCEPT_PATCH = new AsciiString("accept-patch");
        /**
         * {@code "access-control-allow-credentials"}
         */

        public static readonly AsciiString ACCESS_CONTROL_ALLOW_CREDENTIALS =
            new AsciiString("access-control-allow-credentials");

        /**
             * {@code "access-control-allow-headers"}
             */

        public static readonly AsciiString ACCESS_CONTROL_ALLOW_HEADERS =
            new AsciiString("access-control-allow-headers");

        /**
             * {@code "access-control-allow-methods"}
             */

        public static readonly AsciiString ACCESS_CONTROL_ALLOW_METHODS =
            new AsciiString("access-control-allow-methods");

        /**
             * {@code "access-control-allow-origin"}
             */

        public static readonly AsciiString ACCESS_CONTROL_ALLOW_ORIGIN =
            new AsciiString("access-control-allow-origin");

        /**
             * {@code "access-control-expose-headers"}
             */

        public static readonly AsciiString ACCESS_CONTROL_EXPOSE_HEADERS =
            new AsciiString("access-control-expose-headers");

        /**
             * {@code "access-control-max-age"}
             */
        public static readonly AsciiString ACCESS_CONTROL_MAX_AGE = new AsciiString("access-control-max-age");
        /**
         * {@code "access-control-request-headers"}
         */

        public static readonly AsciiString ACCESS_CONTROL_REQUEST_HEADERS =
            new AsciiString("access-control-request-headers");

        /**
             * {@code "access-control-request-method"}
             */

        public static readonly AsciiString ACCESS_CONTROL_REQUEST_METHOD =
            new AsciiString("access-control-request-method");

        /**
             * {@code "age"}
             */
        public static readonly AsciiString AGE = new AsciiString("age");
        /**
         * {@code "allow"}
         */
        public static readonly AsciiString ALLOW = new AsciiString("allow");
        /**
         * {@code "authorization"}
         */
        public static readonly AsciiString AUTHORIZATION = new AsciiString("authorization");
        /**
         * {@code "cache-control"}
         */
        public static readonly AsciiString CACHE_CONTROL = new AsciiString("cache-control");
        /**
         * {@code "connection"}
         */
        public static readonly AsciiString CONNECTION = new AsciiString("connection");
        /**
         * {@code "content-base"}
         */
        public static readonly AsciiString CONTENT_BASE = new AsciiString("content-base");
        /**
         * {@code "content-encoding"}
         */
        public static readonly AsciiString CONTENT_ENCODING = new AsciiString("content-encoding");
        /**
         * {@code "content-language"}
         */
        public static readonly AsciiString CONTENT_LANGUAGE = new AsciiString("content-language");
        /**
         * {@code "content-length"}
         */
        public static readonly AsciiString CONTENT_LENGTH = new AsciiString("content-length");
        /**
         * {@code "content-location"}
         */
        public static readonly AsciiString CONTENT_LOCATION = new AsciiString("content-location");
        /**
         * {@code "content-transfer-encoding"}
         */
        public static readonly AsciiString CONTENT_TRANSFER_ENCODING = new AsciiString("content-transfer-encoding");
        /**
         * {@code "content-disposition"}
         */
        public static readonly AsciiString CONTENT_DISPOSITION = new AsciiString("content-disposition");
        /**
         * {@code "content-md5"}
         */
        public static readonly AsciiString CONTENT_MD5 = new AsciiString("content-md5");
        /**
         * {@code "content-range"}
         */
        public static readonly AsciiString CONTENT_RANGE = new AsciiString("content-range");
        /**
         * {@code "content-type"}
         */
        public static readonly AsciiString CONTENT_TYPE = new AsciiString("content-type");
        /**
         * {@code "cookie"}
         */
        public static readonly AsciiString COOKIE = new AsciiString("cookie");
        /**
         * {@code "date"}
         */
        public static readonly AsciiString DATE = new AsciiString("date");
        /**
         * {@code "etag"}
         */
        public static readonly AsciiString ETAG = new AsciiString("etag");
        /**
         * {@code "expect"}
         */
        public static readonly AsciiString EXPECT = new AsciiString("expect");
        /**
         * {@code "expires"}
         */
        public static readonly AsciiString EXPIRES = new AsciiString("expires");
        /**
         * {@code "from"}
         */
        public static readonly AsciiString FROM = new AsciiString("from");
        /**
         * {@code "host"}
         */
        public static readonly AsciiString HOST = new AsciiString("host");
        /**
         * {@code "if-match"}
         */
        public static readonly AsciiString IF_MATCH = new AsciiString("if-match");
        /**
         * {@code "if-modified-since"}
         */
        public static readonly AsciiString IF_MODIFIED_SINCE = new AsciiString("if-modified-since");
        /**
         * {@code "if-none-match"}
         */
        public static readonly AsciiString IF_NONE_MATCH = new AsciiString("if-none-match");
        /**
         * {@code "if-range"}
         */
        public static readonly AsciiString IF_RANGE = new AsciiString("if-range");
        /**
         * {@code "if-unmodified-since"}
         */
        public static readonly AsciiString IF_UNMODIFIED_SINCE = new AsciiString("if-unmodified-since");
        /**
         * @deprecated use {@link #CONNECTION}
         *
         * {@code "keep-alive"}
         */

        [Obsolete]
        public static readonly AsciiString KEEP_ALIVE = new AsciiString("keep-alive");

        /**
             * {@code "last-modified"}
             */
        public static readonly AsciiString LAST_MODIFIED = new AsciiString("last-modified");
        /**
         * {@code "location"}
         */
        public static readonly AsciiString LOCATION = new AsciiString("location");
        /**
         * {@code "max-forwards"}
         */
        public static readonly AsciiString MAX_FORWARDS = new AsciiString("max-forwards");
        /**
         * {@code "origin"}
         */
        public static readonly AsciiString ORIGIN = new AsciiString("origin");
        /**
         * {@code "pragma"}
         */
        public static readonly AsciiString PRAGMA = new AsciiString("pragma");
        /**
         * {@code "proxy-authenticate"}
         */
        public static readonly AsciiString PROXY_AUTHENTICATE = new AsciiString("proxy-authenticate");
        /**
         * {@code "proxy-authorization"}
         */
        public static readonly AsciiString PROXY_AUTHORIZATION = new AsciiString("proxy-authorization");
        /**
         * @deprecated use {@link #CONNECTION}
         *
         * {@code "proxy-connection"}
         */

        [Obsolete]
        public static readonly AsciiString PROXY_CONNECTION = new AsciiString("proxy-connection");

        /**
             * {@code "range"}
             */
        public static readonly AsciiString RANGE = new AsciiString("range");
        /**
         * {@code "referer"}
         */
        public static readonly AsciiString REFERER = new AsciiString("referer");
        /**
         * {@code "retry-after"}
         */
        public static readonly AsciiString RETRY_AFTER = new AsciiString("retry-after");
        /**
         * {@code "sec-websocket-key1"}
         */
        public static readonly AsciiString SEC_WEBSOCKET_KEY1 = new AsciiString("sec-websocket-key1");
        /**
         * {@code "sec-websocket-key2"}
         */
        public static readonly AsciiString SEC_WEBSOCKET_KEY2 = new AsciiString("sec-websocket-key2");
        /**
         * {@code "sec-websocket-location"}
         */
        public static readonly AsciiString SEC_WEBSOCKET_LOCATION = new AsciiString("sec-websocket-location");
        /**
         * {@code "sec-websocket-origin"}
         */
        public static readonly AsciiString SEC_WEBSOCKET_ORIGIN = new AsciiString("sec-websocket-origin");
        /**
         * {@code "sec-websocket-protocol"}
         */
        public static readonly AsciiString SEC_WEBSOCKET_PROTOCOL = new AsciiString("sec-websocket-protocol");
        /**
         * {@code "sec-websocket-version"}
         */
        public static readonly AsciiString SEC_WEBSOCKET_VERSION = new AsciiString("sec-websocket-version");
        /**
         * {@code "sec-websocket-key"}
         */
        public static readonly AsciiString SEC_WEBSOCKET_KEY = new AsciiString("sec-websocket-key");
        /**
         * {@code "sec-websocket-accept"}
         */
        public static readonly AsciiString SEC_WEBSOCKET_ACCEPT = new AsciiString("sec-websocket-accept");
        /**
         * {@code "sec-websocket-protocol"}
         */
        public static readonly AsciiString SEC_WEBSOCKET_EXTENSIONS = new AsciiString("sec-websocket-extensions");
        /**
         * {@code "server"}
         */
        public static readonly AsciiString SERVER = new AsciiString("server");
        /**
         * {@code "set-cookie"}
         */
        public static readonly AsciiString SET_COOKIE = new AsciiString("set-cookie");
        /**
         * {@code "set-cookie2"}
         */
        public static readonly AsciiString SET_COOKIE2 = new AsciiString("set-cookie2");
        /**
         * {@code "te"}
         */
        public static readonly AsciiString TE = new AsciiString("te");
        /**
         * {@code "trailer"}
         */
        public static readonly AsciiString TRAILER = new AsciiString("trailer");
        /**
         * {@code "transfer-encoding"}
         */
        public static readonly AsciiString TRANSFER_ENCODING = new AsciiString("transfer-encoding");
        /**
         * {@code "upgrade"}
         */
        public static readonly AsciiString UPGRADE = new AsciiString("upgrade");
        /**
         * {@code "user-agent"}
         */
        public static readonly AsciiString USER_AGENT = new AsciiString("user-agent");
        /**
         * {@code "vary"}
         */
        public static readonly AsciiString VARY = new AsciiString("vary");
        /**
         * {@code "via"}
         */
        public static readonly AsciiString VIA = new AsciiString("via");
        /**
         * {@code "warning"}
         */
        public static readonly AsciiString WARNING = new AsciiString("warning");
        /**
         * {@code "websocket-location"}
         */
        public static readonly AsciiString WEBSOCKET_LOCATION = new AsciiString("websocket-location");
        /**
         * {@code "websocket-origin"}
         */
        public static readonly AsciiString WEBSOCKET_ORIGIN = new AsciiString("websocket-origin");
        /**
         * {@code "websocket-protocol"}
         */
        public static readonly AsciiString WEBSOCKET_PROTOCOL = new AsciiString("websocket-protocol");
        /**
         * {@code "www-authenticate"}
         */
        public static readonly AsciiString WWW_AUTHENTICATE = new AsciiString("www-authenticate");
    }
}