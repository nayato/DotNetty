// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http.Cookies
{
    using DotNetty.Common.Utilities;

    public static class CookieHeaderNames
    {
        public static readonly AsciiString Path = new AsciiString("Path");

        public static readonly AsciiString Expires = new AsciiString("Expires");

        public static readonly AsciiString MaxAge = new AsciiString("Max-Age");

        public static readonly AsciiString Domain = new AsciiString("Domain");

        public static readonly AsciiString Secure = new AsciiString("Secure");

        public static readonly AsciiString HttpOnly = new AsciiString("HTTPOnly");
    }
}
