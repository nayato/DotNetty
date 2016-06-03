// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http
{
    using DotNetty.Common.Utilities;

    /**
     * Standard HTTP header values.
     */

    public sealed class HttpHeaderValues
    {
        /**
         * {@code "application/x-www-form-urlencoded"}
         */

        public static readonly AsciiString APPLICATION_X_WWW_FORM_URLENCODED =
            new AsciiString("application/x-www-form-urlencoded");

        /**
         * {@code "application/octet-stream"}
         */
        public static readonly AsciiString APPLICATION_OCTET_STREAM = new AsciiString("application/octet-stream");
        /**
         * {@code "attachment"}
         * See {@link HttpHeaderNames#CONTENT_DISPOSITION}
         */
        public static readonly AsciiString ATTACHMENT = new AsciiString("attachment");
        /**
         * {@code "base64"}
         */
        public static readonly AsciiString BASE64 = new AsciiString("base64");
        /**
         * {@code "binary"}
         */
        public static readonly AsciiString BINARY = new AsciiString("binary");
        /**
         * {@code "boundary"}
         */
        public static readonly AsciiString BOUNDARY = new AsciiString("boundary");
        /**
         * {@code "bytes"}
         */
        public static readonly AsciiString BYTES = new AsciiString("bytes");
        /**
         * {@code "charset"}
         */
        public static readonly AsciiString CHARSET = new AsciiString("charset");
        /**
         * {@code "chunked"}
         */
        public static readonly AsciiString CHUNKED = new AsciiString("chunked");
        /**
         * {@code "close"}
         */
        public static readonly AsciiString CLOSE = new AsciiString("close");
        /**
         * {@code "compress"}
         */
        public static readonly AsciiString COMPRESS = new AsciiString("compress");
        /**
         * {@code "100-continue"}
         */
        public static readonly AsciiString CONTINUE = new AsciiString("100-continue");
        /**
         * {@code "deflate"}
         */
        public static readonly AsciiString DEFLATE = new AsciiString("deflate");
        /**
         * {@code "x-deflate"}
         */
        public static readonly AsciiString X_DEFLATE = new AsciiString("x-deflate");
        /**
         * {@code "file"}
         * See {@link HttpHeaderNames#CONTENT_DISPOSITION}
         */
        public static readonly AsciiString FILE = new AsciiString("file");
        /**
         * {@code "filename"}
         * See {@link HttpHeaderNames#CONTENT_DISPOSITION}
         */
        public static readonly AsciiString FILENAME = new AsciiString("filename");
        /**
         * {@code "form-data"}
         * See {@link HttpHeaderNames#CONTENT_DISPOSITION}
         */
        public static readonly AsciiString FORM_DATA = new AsciiString("form-data");
        /**
         * {@code "gzip"}
         */
        public static readonly AsciiString GZIP = new AsciiString("gzip");
        /**
         * {@code "x-gzip"}
         */
        public static readonly AsciiString X_GZIP = new AsciiString("x-gzip");
        /**
         * {@code "identity"}
         */
        public static readonly AsciiString IDENTITY = new AsciiString("identity");
        /**
         * {@code "keep-alive"}
         */
        public static readonly AsciiString KEEP_ALIVE = new AsciiString("keep-alive");
        /**
         * {@code "max-age"}
         */
        public static readonly AsciiString MAX_AGE = new AsciiString("max-age");
        /**
         * {@code "max-stale"}
         */
        public static readonly AsciiString MAX_STALE = new AsciiString("max-stale");
        /**
         * {@code "min-fresh"}
         */
        public static readonly AsciiString MIN_FRESH = new AsciiString("min-fresh");
        /**
         * {@code "multipart/form-data"}
         */
        public static readonly AsciiString MULTIPART_FORM_DATA = new AsciiString("multipart/form-data");
        /**
         * {@code "multipart/mixed"}
         */
        public static readonly AsciiString MULTIPART_MIXED = new AsciiString("multipart/mixed");
        /**
         * {@code "must-revalidate"}
         */
        public static readonly AsciiString MUST_REVALIDATE = new AsciiString("must-revalidate");
        /**
         * {@code "name"}
         * See {@link HttpHeaderNames#CONTENT_DISPOSITION}
         */
        public static readonly AsciiString NAME = new AsciiString("name");
        /**
         * {@code "no-cache"}
         */
        public static readonly AsciiString NO_CACHE = new AsciiString("no-cache");
        /**
         * {@code "no-store"}
         */
        public static readonly AsciiString NO_STORE = new AsciiString("no-store");
        /**
         * {@code "no-transform"}
         */
        public static readonly AsciiString NO_TRANSFORM = new AsciiString("no-transform");
        /**
         * {@code "none"}
         */
        public static readonly AsciiString NONE = new AsciiString("none");
        /**
         * {@code "0"}
         */
        public static readonly AsciiString ZERO = new AsciiString("0");
        /**
         * {@code "only-if-cached"}
         */
        public static readonly AsciiString ONLY_IF_CACHED = new AsciiString("only-if-cached");
        /**
         * {@code "private"}
         */
        public static readonly AsciiString PRIVATE = new AsciiString("private");
        /**
         * {@code "proxy-revalidate"}
         */
        public static readonly AsciiString PROXY_REVALIDATE = new AsciiString("proxy-revalidate");
        /**
         * {@code "public"}
         */
        public static readonly AsciiString PUBLIC = new AsciiString("public");
        /**
         * {@code "quoted-printable"}
         */
        public static readonly AsciiString QUOTED_PRINTABLE = new AsciiString("quoted-printable");
        /**
         * {@code "s-maxage"}
         */
        public static readonly AsciiString S_MAXAGE = new AsciiString("s-maxage");
        /**
         * {@code "text/plain"}
         */
        public static readonly AsciiString TEXT_PLAIN = new AsciiString("text/plain");
        /**
         * {@code "trailers"}
         */
        public static readonly AsciiString TRAILERS = new AsciiString("trailers");
        /**
         * {@code "upgrade"}
         */
        public static readonly AsciiString UPGRADE = new AsciiString("upgrade");
        /**
         * {@code "websocket"}
         */
        public static readonly AsciiString WEBSOCKET = new AsciiString("websocket");

        HttpHeaderValues()
        {
        }
    }
}