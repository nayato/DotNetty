// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http
{
    using DotNetty.Common.Utilities;

    public static class HttpHeaderValues
    {
        public static readonly AsciiString ApplicationJson = new AsciiString("application/json");

        public static readonly AsciiString ApplicationXWwwFormUrlencoded = new AsciiString("application/x-www-form-urlencoded");

        public static readonly AsciiString ApplicationOctetStream = new AsciiString("application/octet-stream");

        public static readonly AsciiString Attachment = new AsciiString("attachment");

        public static readonly AsciiString Base64 = new AsciiString("base64");

        public static readonly AsciiString Binary = new AsciiString("binary");

        public static readonly AsciiString Boundary = new AsciiString("boundary");

        public static readonly AsciiString Bytes = new AsciiString("bytes");

        public static readonly AsciiString Charset = new AsciiString("charset");

        public static readonly AsciiString Chunked = new AsciiString("chunked");

        public static readonly AsciiString Close = new AsciiString("close");

        public static readonly AsciiString Compress = new AsciiString("compress");

        public static readonly AsciiString Continue = new AsciiString("100-continue");

        public static readonly AsciiString Deflate = new AsciiString("deflate");

        public static readonly AsciiString XDeflate = new AsciiString("x-deflate");

        public static readonly AsciiString File = new AsciiString("file");

        public static readonly AsciiString FileName = new AsciiString("filename");

        public static readonly AsciiString FormData = new AsciiString("form-data");

        public static readonly AsciiString Gzip = new AsciiString("gzip");

        public static readonly AsciiString GzipDeflate = new AsciiString("gzip,deflate");

        public static readonly AsciiString XGzip = new AsciiString("x-gzip");

        public static readonly AsciiString Identity = new AsciiString("identity");

        public static readonly AsciiString KeepAlive = new AsciiString("keep-alive");

        public static readonly AsciiString MaxAge = new AsciiString("max-age");

        public static readonly AsciiString MaxStale = new AsciiString("max-stale");

        public static readonly AsciiString MinFresh = new AsciiString("min-fresh");

        public static readonly AsciiString MultipartFormData = new AsciiString("multipart/form-data");

        public static readonly AsciiString MultipartMixed = new AsciiString("multipart/mixed");

        public static readonly AsciiString MustRevalidate = new AsciiString("must-revalidate");

        public static readonly AsciiString Name = new AsciiString("name");

        public static readonly AsciiString NoCache = new AsciiString("no-cache");

        public static readonly AsciiString NoStore = new AsciiString("no-store");

        public static readonly AsciiString NoTransform = new AsciiString("no-transform");

        public static readonly AsciiString None = new AsciiString("none");

        public static readonly AsciiString Zero = new AsciiString("0");

        public static readonly AsciiString OnlyIfCached = new AsciiString("only-if-cached");

        public static readonly AsciiString Private = new AsciiString("private");

        public static readonly AsciiString ProxyRevalidate = new AsciiString("proxy-revalidate");

        public static readonly AsciiString Public = new AsciiString("public");

        public static readonly AsciiString QuotedPrintable = new AsciiString("quoted-printable");

        public static readonly AsciiString SMaxage = new AsciiString("s-maxage");

        public static readonly AsciiString TextPlain = new AsciiString("text/plain");

        public static readonly AsciiString Trailers = new AsciiString("trailers");

        public static readonly AsciiString Upgrade = new AsciiString("upgrade");

        public static readonly AsciiString Websocket = new AsciiString("websocket");
    }
}
