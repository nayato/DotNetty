// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http
{
    using DotNetty.Common.Utilities;

    ///
    /// Standard HTTP header names.
    /// 
    /// These are all defined as lowercase to support HTTP/2 requirements while also not
    /// violating HTTP/1.x requirements.New header names should always be lowercase.
    /// 
    public static class HttpHeaderNames
    {
        public static readonly AsciiString Accept = new AsciiString("accept");

        public static readonly AsciiString AcceptCharset = new AsciiString("accept-charset");

        public static readonly AsciiString AcceptEncoding = new AsciiString("accept-encoding");

        public static readonly AsciiString AcceptLanguage = new AsciiString("accept-language");

        public static readonly AsciiString AcceptRanges = new AsciiString("accept-ranges");

        public static readonly AsciiString AcceptPatch = new AsciiString("accept-patch");
        
        public static readonly AsciiString AccessControlAllowCredentials = new AsciiString("access-control-allow-credentials");

        public static readonly AsciiString AccessControlAllowHeaders = new AsciiString("access-control-allow-headers");

        public static readonly AsciiString AccessControlAllowMethods = new AsciiString("access-control-allow-methods");

        public static readonly AsciiString AccessControlAllowOrigin = new AsciiString("access-control-allow-origin");

        public static readonly AsciiString AccessControlExposeHeaders = new AsciiString("access-control-expose-headers");

        public static readonly AsciiString AccessControlMaxAge = new AsciiString("access-control-max-age");

        public static readonly AsciiString AccessControlRequestHeaders = new AsciiString("access-control-request-headers");

        public static readonly AsciiString AccessControlRequestMethod = new AsciiString("access-control-request-method");

        public static readonly AsciiString Age = new AsciiString("age");

        public static readonly AsciiString Allow = new AsciiString("allow");

        public static readonly AsciiString Authorization = new AsciiString("authorization");

        public static readonly AsciiString CacheControl = new AsciiString("cache-control");

        public static readonly AsciiString Connection = new AsciiString("connection");

        public static readonly AsciiString ContentBase = new AsciiString("content-base");

        public static readonly AsciiString ContentEncoding = new AsciiString("content-encoding");

        public static readonly AsciiString ContentLanguage = new AsciiString("content-language");

        public static readonly AsciiString ContentLength = new AsciiString("content-length");

        public static readonly AsciiString ContentLocation = new AsciiString("content-location");

        public static readonly AsciiString ContentTransferEncoding = new AsciiString("content-transfer-encoding");

        public static readonly AsciiString ContentDisposition = new AsciiString("content-disposition");

        public static readonly AsciiString ContentMD5 = new AsciiString("content-md5");

        public static readonly AsciiString ContentRange = new AsciiString("content-range");

        public static readonly AsciiString ContentType = new AsciiString("content-type");

        public static readonly AsciiString Cookie = new AsciiString("cookie");

        public static readonly AsciiString Date = new AsciiString("date");

        public static readonly AsciiString Etag = new AsciiString("etag");

        public static readonly AsciiString Expect = new AsciiString("expect");

        public static readonly AsciiString Expires = new AsciiString("expires");

        public static readonly AsciiString From = new AsciiString("from");

        public static readonly AsciiString Host = new AsciiString("host");

        public static readonly AsciiString IfMatch = new AsciiString("if-match");

        public static readonly AsciiString IfModifiedSince = new AsciiString("if-modified-since");

        public static readonly AsciiString IfNoneMatch = new AsciiString("if-none-match");

        public static readonly AsciiString IfRange = new AsciiString("if-range");

        public static readonly AsciiString IfUnmodifiedSince = new AsciiString("if-unmodified-since");

        public static readonly AsciiString LastModified = new AsciiString("last-modified");

        public static readonly AsciiString Location = new AsciiString("location");

        public static readonly AsciiString MaxForwards = new AsciiString("max-forwards");

        public static readonly AsciiString Origin = new AsciiString("origin");

        public static readonly AsciiString Pragma = new AsciiString("pragma");

        public static readonly AsciiString ProxyAuthenticate = new AsciiString("proxy-authenticate");

        public static readonly AsciiString ProxyAuthorization = new AsciiString("proxy-authorization");

        public static readonly AsciiString Range = new AsciiString("range");

        public static readonly AsciiString Referer = new AsciiString("referer");

        public static readonly AsciiString RetryAfter = new AsciiString("retry-after");

        public static readonly AsciiString SecWebsocketKey1 = new AsciiString("sec-websocket-key1");

        public static readonly AsciiString SecWebsocketKey2 = new AsciiString("sec-websocket-key2");

        public static readonly AsciiString SecWebsocketLocation = new AsciiString("sec-websocket-location");

        public static readonly AsciiString SecWebsocketOrigin = new AsciiString("sec-websocket-origin");

        public static readonly AsciiString SecWebsocketProtocol = new AsciiString("sec-websocket-protocol");

        public static readonly AsciiString SecWebsocketVersion = new AsciiString("sec-websocket-version");

        public static readonly AsciiString SecWebsocketKey = new AsciiString("sec-websocket-key");

        public static readonly AsciiString SecWebsocketAccept = new AsciiString("sec-websocket-accept");

        public static readonly AsciiString SecWebsocketExtensions = new AsciiString("sec-websocket-extensions");

        public static readonly AsciiString Server = new AsciiString("server");

        public static readonly AsciiString SetCookie = new AsciiString("set-cookie");

        public static readonly AsciiString SetCookie2 = new AsciiString("set-cookie2");

        public static readonly AsciiString Te = new AsciiString("te");

        public static readonly AsciiString Trailer = new AsciiString("trailer");

        public static readonly AsciiString TransferEncoding = new AsciiString("transfer-encoding");

        public static readonly AsciiString Upgrade = new AsciiString("upgrade");

        public static readonly AsciiString UserAgent = new AsciiString("user-agent");

        public static readonly AsciiString Vary = new AsciiString("vary");

        public static readonly AsciiString Via = new AsciiString("via");

        public static readonly AsciiString Warning = new AsciiString("warning");

        public static readonly AsciiString WebsocketLocation = new AsciiString("websocket-location");

        public static readonly AsciiString WebsocketOrigin = new AsciiString("websocket-origin");

        public static readonly AsciiString WebsocketProtocol = new AsciiString("websocket-protocol");

        public static readonly AsciiString WwwAuthenticate = new AsciiString("www-authenticate");
    }
}
