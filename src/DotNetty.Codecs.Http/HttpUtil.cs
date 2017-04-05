// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Text;
    using DotNetty.Common.Utilities;

    public static class HttpUtil
    {
        static readonly AsciiString CharsetEquals = new AsciiString(HttpHeaderValues.Charset + "=");
        static readonly AsciiString Semicolon = new AsciiString(";");

        public static ICharSequence GetCharset(IHttpMessage message)
        {
            ICharSequence contentTypeValue = message.Headers.Get(HttpHeaderNames.ContentType);
            return contentTypeValue != null ? GetCharset(contentTypeValue) : null;
        }

        public static ICharSequence GetCharset(ICharSequence contentTypeValue)
        {
            Contract.Requires(contentTypeValue != null);

            int indexOfCharset = AsciiString.IndexOfIgnoreCaseAscii(contentTypeValue, CharsetEquals, 0);
            if (indexOfCharset != AsciiString.IndexNotFound)
            {
                int indexOfEncoding = indexOfCharset + CharsetEquals.Count;
                if (indexOfEncoding < contentTypeValue.Count)
                {
                    return contentTypeValue.SubSequence(indexOfEncoding, contentTypeValue.Count);
                }
            }

            return null;
        }

        public static Encoding GetEncoding(IHttpMessage message) => GetEncoding(message, Encoding.UTF8);

        public static Encoding GetEncoding(ICharSequence contentTypeValue) => 
            contentTypeValue != null ? GetEncoding(contentTypeValue, Encoding.UTF8) : Encoding.UTF8;

        public static Encoding GetEncoding(IHttpMessage message, Encoding defaultEncoding)
        {
            ICharSequence contentTypeValue = message.Headers.Get(HttpHeaderNames.ContentType);
            return contentTypeValue != null 
                ? GetEncoding(contentTypeValue, defaultEncoding) 
                : defaultEncoding;
        }

        public static Encoding GetEncoding(ICharSequence contentTypeValue, Encoding defaultEncoding)
        {
            if (contentTypeValue != null)
            {
                ICharSequence charsetCharSequence = GetCharset(contentTypeValue);
                if (charsetCharSequence != null)
                {
                    try
                    {
                        return Encoding.GetEncoding(charsetCharSequence.ToString());
                    }
                    catch (ArgumentException)
                    {
                        return defaultEncoding;
                    }
                }
            }

            return defaultEncoding;
        }

        public static ICharSequence GetMimeType(IHttpMessage message)
        {
            ICharSequence contentTypeValue = message.Headers.Get(HttpHeaderNames.ContentType);
            return contentTypeValue != null ? GetMimeType(contentTypeValue) : null;
        }

        public static ICharSequence GetMimeType(ICharSequence contentTypeValue)
        {
            Contract.Requires(contentTypeValue != null);

            int indexOfSemicolon = AsciiString.IndexOfIgnoreCaseAscii(contentTypeValue, Semicolon, 0);
            if (indexOfSemicolon != AsciiString.IndexNotFound)
            {
                return contentTypeValue.SubSequence(0, indexOfSemicolon);
            }

            return contentTypeValue.Count > 0 ? contentTypeValue : null;
        }

        public static void Set100ContinueExpected(IHttpMessage message, bool expected)
        {
            if (expected)
            {
                message.Headers.Set(HttpHeaderNames.Expect, HttpHeaderValues.Continue);
            }
            else
            {
                message.Headers.Remove(HttpHeaderNames.Expect);
            }
        }

        public static bool Is100ContinueExpected(IHttpMessage message)
        {
            if (!IsExpectHeaderValid(message))
            {
                return false;
            }

            ICharSequence expectValue = message.Headers.Get(HttpHeaderNames.Expect);
            // unquoted tokens in the expect header are case-insensitive, thus 100-continue is case insensitive
            return HttpHeaderValues.Continue.ContentEqualsIgnoreCase(expectValue);
        }

        internal static bool IsUnsupportedExpectation(IHttpMessage message)
        {
            if (!IsExpectHeaderValid(message))
            {
                return false;
            }

            ICharSequence expectValue = message.Headers.Get(HttpHeaderNames.Expect);
            return expectValue != null && !HttpHeaderValues.Continue.ContentEqualsIgnoreCase(expectValue);
        }

        /*
         * Expect: 100-continue is for requests only and it works only on HTTP/1.1 or later. Note further that RFC 7231
         * section 5.1.1 says "A server that receives a 100-continue expectation in an HTTP/1.0 request MUST ignore
         * that expectation."
         */
        static bool IsExpectHeaderValid(IHttpMessage message) => message is IHttpRequest 
            && message.ProtocolVersion.CompareTo(HttpVersion.Http11) >= 0;


        public static bool IsKeepAlive(IHttpMessage message)
        {
            ICharSequence connection = message.Headers.Get(HttpHeaderNames.Connection);
            if (connection != null && HttpHeaderValues.Close.ContentEqualsIgnoreCase(connection))
            {
                return false;
            }

            if (message.ProtocolVersion.IsKeepAliveDefault)
            {
                return !HttpHeaderValues.Close.ContentEqualsIgnoreCase(connection);
            }
            else
            {
                return HttpHeaderValues.KeepAlive.ContentEqualsIgnoreCase(connection);
            }
        }

        public static void SetKeepAlive(IHttpMessage message, bool keepAlive) => SetKeepAlive(message.Headers, message.ProtocolVersion, keepAlive);

        public static void SetKeepAlive(HttpHeaders headers, HttpVersion httpVersion, bool keepAlive)
        {
            if (httpVersion.IsKeepAliveDefault)
            {
                if (keepAlive)
                {
                    headers.Remove(HttpHeaderNames.Connection);
                }
                else
                {
                    headers.Set(HttpHeaderNames.Connection, HttpHeaderValues.Close);
                }
            }
            else
            {
                if (keepAlive)
                {
                    headers.Set(HttpHeaderNames.Connection, HttpHeaderValues.KeepAlive);
                }
                else
                {
                    headers.Remove(HttpHeaderNames.Connection);
                }
            }
        }

        public static long GetContentLength(IHttpMessage message)
        {
            ICharSequence value = message.Headers.Get(HttpHeaderNames.ContentLength);
            if (value != null)
            {
                return CharUtil.ParseLong(value);
            }

            // We know the content length if it's a Web Socket message even if
            // Content-Length header is missing.
            long webSocketContentLength = GetWebSocketContentLength(message);
            if (webSocketContentLength >= 0)
            {
                return webSocketContentLength;
            }

            // Otherwise we don't.
            throw new FormatException($"header not found: {HttpHeaderNames.ContentLength}");
        }

        public static long GetContentLength(IHttpMessage message, long defaultValue)
        {
            ICharSequence value = message.Headers.Get(HttpHeaderNames.ContentLength);
            if (value != null)
            {
                try
                {
                    return CharUtil.ParseLong(value);
                }
                catch (FormatException)
                {
                    return defaultValue;
                }
            }

            // We know the content length if it's a Web Socket message even if
            // Content-Length header is missing.
            long webSocketContentLength = GetWebSocketContentLength(message);
            if (webSocketContentLength >= 0)
            {
                return webSocketContentLength;
            }

            // Otherwise we don't.
            return defaultValue;
        }

        static int GetWebSocketContentLength(IHttpMessage message)
        {
            // WebSocket messages have constant content-lengths.
            HttpHeaders h = message.Headers;

            var request = message as IHttpRequest;
            if (request != null)
            {
                if (HttpMethod.Get.Equals(request.Method)
                    && h.Contains(HttpHeaderNames.SecWebsocketKey1) 
                    && h.Contains(HttpHeaderNames.SecWebsocketKey2))
                {
                    return 8;
                }
            }
            else
            {
                var res = message as IHttpResponse;
                if (res?.Status.Code == 101 
                    && h.Contains(HttpHeaderNames.SecWebsocketOrigin) 
                    && h.Contains(HttpHeaderNames.SecWebsocketLocation))
                {
                    return 16;
                }
            }

            // Not a web socket message
            return -1;
        }

        public static void SetContentLength(IHttpMessage message, long length) => message.Headers.Set(HttpHeaderNames.ContentLength, length);

        public static bool IsContentLengthSet(IHttpMessage message) => message.Headers.Contains(HttpHeaderNames.ContentLength);

        public static bool IsTransferEncodingChunked(IHttpMessage message) => 
            message.Headers.Contains(HttpHeaderNames.TransferEncoding, HttpHeaderValues.Chunked, true);

        public static void SetTransferEncodingChunked(IHttpMessage m, bool chunked)
        {
            if (chunked)
            {
                m.Headers.Set(HttpHeaderNames.TransferEncoding, HttpHeaderValues.Chunked);
                m.Headers.Remove(HttpHeaderNames.ContentLength);
            }
            else
            {
                IList<ICharSequence> encodings = m.Headers.GetAll(HttpHeaderNames.TransferEncoding);
                if (encodings.Count == 0)
                {
                    return;
                }

                var values = new List<ICharSequence>(encodings);
                foreach (ICharSequence value in encodings)
                {
                    if (HttpHeaderValues.Chunked.ContentEqualsIgnoreCase(value))
                    {
                        values.Remove(value);
                    }
                }

                if (values.Count == 0)
                {
                    m.Headers.Remove(HttpHeaderNames.TransferEncoding);
                }
                else
                {
                    m.Headers.Set(HttpHeaderNames.TransferEncoding, values);
                }
            }
        }
    }
}
