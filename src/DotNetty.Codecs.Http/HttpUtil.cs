// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http {
    using System;
    using System.Collections.Generic;
    using DotNetty.Buffers;
    using DotNetty.Common.Utilities;

    /**
 * Utility methods useful in the HTTP context.
 */
public sealed class HttpUtil {
    /**
     * @deprecated Use {@link EmptyHttpHeaders#INSTANCE}
     * <p>
     * The instance is instantiated here to break the cyclic static initialization between {@link EmptyHttpHeaders} and
     * {@link HttpHeaders}. The issue is that if someone accesses {@link EmptyHttpHeaders#INSTANCE} before
     * {@link HttpHeaders#EMPTY_HEADERS} then {@link HttpHeaders#EMPTY_HEADERS} will be {@code null}.
     */
    [Obsolete]
    static readonly EmptyHttpHeaders EMPTY_HEADERS = new EmptyHttpHeaders();
    private static readonly AsciiString CHARSET_EQUALS = new AsciiString(HttpHeaderValues.CHARSET + "=");
    private static readonly AsciiString SEMICOLON = new AsciiString(";");

    private HttpUtil() { }

    /**
     * Determine if a uri is in origin-form according to
     * <a href="https://tools.ietf.org/html/rfc7230#section-5.3">rfc7230, 5.3</a>.
     */
    public static bool isOriginForm(URI uri) {
        return uri.getScheme() == null && uri.getSchemeSpecificPart() == null &&
               uri.getHost() == null && uri.getAuthority() == null;
    }

    /**
     * Determine if a uri is in asteric-form according to
     * <a href="https://tools.ietf.org/html/rfc7230#section-5.3">rfc7230, 5.3</a>.
     */
    public static bool isAsteriskForm(URI uri) {
        return "*".Equals(uri.getPath()) &&
                uri.getScheme() == null && uri.getSchemeSpecificPart() == null &&
                uri.getHost() == null && uri.getAuthority() == null && uri.getQuery() == null &&
                uri.getFragment() == null;
    }

    /**
     * Returns {@code true} if and only if the connection can remain open and
     * thus 'kept alive'.  This methods respects the value of the.
     * {@code "Connection"} header first and then the return value of
     * {@link HttpVersion#isKeepAliveDefault()}.
     */
    public static bool isKeepAlive(HttpMessage message) {
        CharSequence connection = message.headers().get(HttpHeaderNames.CONNECTION);
        if (connection != null && HttpHeaderValues.CLOSE.ContentEqualsIgnoreCase(connection)) {
            return false;
        }

        if (message.protocolVersion().isKeepAliveDefault()) {
            return !HttpHeaderValues.CLOSE.ContentEqualsIgnoreCase(connection);
        } else {
            return HttpHeaderValues.KEEP_ALIVE.ContentEqualsIgnoreCase(connection);
        }
    }

    /**
     * Sets the value of the {@code "Connection"} header depending on the
     * protocol version of the specified message. This getMethod sets or removes
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
     * @see #setKeepAlive(HttpHeaders, HttpVersion, bool)
     */
    public static void setKeepAlive(HttpMessage message, bool keepAlive) {
        setKeepAlive(message.headers(), message.protocolVersion(), keepAlive);
    }

    /**
     * Sets the value of the {@code "Connection"} header depending on the
     * protocol version of the specified message. This getMethod sets or removes
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
    public static void setKeepAlive(HttpHeaders h, HttpVersion httpVersion, bool keepAlive) {
        if (httpVersion.isKeepAliveDefault()) {
            if (keepAlive) {
                h.remove(HttpHeaderNames.CONNECTION);
            } else {
                h.set(HttpHeaderNames.CONNECTION, HttpHeaderValues.CLOSE);
            }
        } else {
            if (keepAlive) {
                h.set(HttpHeaderNames.CONNECTION, HttpHeaderValues.KEEP_ALIVE);
            } else {
                h.remove(HttpHeaderNames.CONNECTION);
            }
        }
    }

    /**
     * Returns the length of the content. Please note that this value is
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
    public static long getContentLength(HttpMessage message) {
        string value = message.headers().get(HttpHeaderNames.CONTENT_LENGTH);
        if (value != null) {
            return Long.parseLong(value);
        }

        // We know the content length if it's a Web Socket message even if
        // Content-Length header is missing.
        long webSocketContentLength = getWebSocketContentLength(message);
        if (webSocketContentLength >= 0) {
            return webSocketContentLength;
        }

        // Otherwise we don't.
        throw new NumberFormatException("header not found: " + HttpHeaderNames.CONTENT_LENGTH);
    }

    /**
     * Returns the length of the content. Please note that this value is
     * not retrieved from {@link HttpContent#content()} but from the
     * {@code "Content-Length"} header, and thus they are independent from each
     * other.
     *
     * @return the content length or {@code defaultValue} if this message does
     *         not have the {@code "Content-Length"} header or its value is not
     *         a number
     */
    public static long getContentLength(HttpMessage message, long defaultValue) {
        string value = message.headers().get(HttpHeaderNames.CONTENT_LENGTH);
        if (value != null) {
            return Long.parseLong(value);
        }

        // We know the content length if it's a Web Socket message even if
        // Content-Length header is missing.
        long webSocketContentLength = getWebSocketContentLength(message);
        if (webSocketContentLength >= 0) {
            return webSocketContentLength;
        }

        // Otherwise we don't.
        return defaultValue;
    }

    /**
     * Get an {@code int} representation of {@link #getContentLength(HttpMessage, long)}.
     * @return the content length or {@code defaultValue} if this message does
     *         not have the {@code "Content-Length"} header or its value is not
     *         a number. Not to exceed the boundaries of integer.
     */
    public static int getContentLength(HttpMessage message, int defaultValue) {
        return (int) Math.Min(Integer.MAX_VALUE, getContentLength(message, (long) defaultValue));
    }

    /**
     * Returns the content length of the specified web socket message. If the
     * specified message is not a web socket message, {@code -1} is returned.
     */
    private static int getWebSocketContentLength(HttpMessage message) {
        // WebSockset messages have constant content-lengths.
        HttpHeaders h = message.headers();
        if (message is HttpRequest) {
            HttpRequest req = (HttpRequest) message;
            if (HttpMethod.GET.Equals(req.method()) &&
                    h.contains(HttpHeaderNames.SEC_WEBSOCKET_KEY1) &&
                    h.contains(HttpHeaderNames.SEC_WEBSOCKET_KEY2)) {
                return 8;
            }
        } else if (message is HttpResponse) {
            HttpResponse res = (HttpResponse) message;
            if (res.status().code() == 101 &&
                    h.contains(HttpHeaderNames.SEC_WEBSOCKET_ORIGIN) &&
                    h.contains(HttpHeaderNames.SEC_WEBSOCKET_LOCATION)) {
                return 16;
            }
        }

        // Not a web socket message
        return -1;
    }

    /**
     * Sets the {@code "Content-Length"} header.
     */
    public static void setContentLength(HttpMessage message, long length) {
        message.headers().set(HttpHeaderNames.CONTENT_LENGTH, length);
    }

    public static bool isContentLengthSet(HttpMessage m) {
        return m.headers().contains(HttpHeaderNames.CONTENT_LENGTH);
    }

    /**
     * Returns {@code true} if and only if the specified message contains the
     * {@code "Expect: 100-continue"} header.
     */
    public static bool is100ContinueExpected(HttpMessage message) {
        // Expect: 100-continue is for requests only.
        if (!(message is HttpRequest)) {
            return false;
        }

        // It works only on HTTP/1.1 or later.
        if (message.protocolVersion().compareTo(HttpVersion.HTTP_1_1) < 0) {
            return false;
        }

        // In most cases, there will be one or zero 'Expect' header.
        string value = message.headers().get(HttpHeaderNames.EXPECT);
        if (value == null) {
            return false;
        }
        if (HttpHeaderValues.CONTINUE.ContentEqualsIgnoreCase(value)) {
            return true;
        }

        // Multiple 'Expect' headers.  Search through them.
        return message.headers().contains(HttpHeaderNames.EXPECT, HttpHeaderValues.CONTINUE, true);
    }

    /**
     * Sets or removes the {@code "Expect: 100-continue"} header to / from the
     * specified message. If the specified {@code value} is {@code true},
     * the {@code "Expect: 100-continue"} header is set and all other previous
     * {@code "Expect"} headers are removed.  Otherwise, all {@code "Expect"}
     * headers are removed completely.
     */
    public static void set100ContinueExpected(HttpMessage message, bool expected) {
        if (expected) {
            message.headers().set(HttpHeaderNames.EXPECT, HttpHeaderValues.CONTINUE);
        } else {
            message.headers().remove(HttpHeaderNames.EXPECT);
        }
    }

    /**
     * Checks to see if the transfer encoding in a specified {@link HttpMessage} is chunked
     *
     * @param message The message to check
     * @return True if transfer encoding is chunked, otherwise false
     */
    public static bool isTransferEncodingChunked(HttpMessage message) {
        return message.headers().contains(HttpHeaderNames.TRANSFER_ENCODING, HttpHeaderValues.CHUNKED, true);
    }

    /**
     * Set the {@link HttpHeaderNames#TRANSFER_ENCODING} to either include {@link HttpHeaderValues#CHUNKED} if
     * {@code chunked} is {@code true}, or remove {@link HttpHeaderValues#CHUNKED} if {@code chunked} is {@code false}.
     * @param m The message which contains the headers to modify.
     * @param chunked if {@code true} then include {@link HttpHeaderValues#CHUNKED} in the headers. otherwise remove
     * {@link HttpHeaderValues#CHUNKED} from the headers.
     */
    public static void setTransferEncodingChunked(HttpMessage m, bool chunked) {
        if (chunked) {
            m.headers().add(HttpHeaderNames.TRANSFER_ENCODING, HttpHeaderValues.CHUNKED);
            m.headers().remove(HttpHeaderNames.CONTENT_LENGTH);
        } else {
            List<string> encodings = m.headers().getAll(HttpHeaderNames.TRANSFER_ENCODING);
            if (encodings.isEmpty()) {
                return;
            }
            List<CharSequence> values = new ArrayList<CharSequence>(encodings);
            Iterator<CharSequence> valuesIt = values.iterator();
            while (valuesIt.hasNext()) {
                CharSequence value = valuesIt.next();
                if (HttpHeaderValues.CHUNKED.ContentEqualsIgnoreCase(value)) {
                    valuesIt.remove();
                }
            }
            if (values.isEmpty()) {
                m.headers().remove(HttpHeaderNames.TRANSFER_ENCODING);
            } else {
                m.headers().set(HttpHeaderNames.TRANSFER_ENCODING, values);
            }
        }
    }

    /**
     * Fetch charset from message's Content-Type header.
     *
     * @return the charset from message's Content-Type header or {@link io.netty.util.CharsetUtil#ISO_8859_1}
     * if charset is not presented or unparsable
     */
    public static Charset getCharset(HttpMessage message) {
        return getCharset(message, CharsetUtil.ISO_8859_1);
    }

    /**
     * Fetch charset from message's Content-Type header.
     *
     * @return the charset from message's Content-Type header or {@code defaultCharset}
     * if charset is not presented or unparsable
     */
    public static Charset getCharset(HttpMessage message, Charset defaultCharset) {
        CharSequence charsetCharSequence = getCharsetAsString(message);
        if (charsetCharSequence != null) {
            try {
                return Charset.forName(charsetCharSequence.ToString());
            } catch (UnsupportedCharsetException unsupportedException) {
                return defaultCharset;
            }
        } else {
            return defaultCharset;
        }
    }

    /**
     * Fetch charset from message's Content-Type header as a char sequence.
     *
     * A lot of sites/possibly clients have charset="CHARSET", for example charset="utf-8". Or "utf8" instead of "utf-8"
     * This is not according to standard, but this method provide an ability to catch desired mistakes manually in code
     *
     * @return the {@code CharSequence} with charset from message's Content-Type header
     * or {@code null} if charset is not presented
     */
    public static CharSequence getCharsetAsString(HttpMessage message) {
        CharSequence contentTypeValue = message.headers().get(HttpHeaderNames.CONTENT_TYPE);
        if (contentTypeValue != null) {
            int indexOfCharset = AsciiString.indexOfIgnoreCaseAscii(contentTypeValue, CHARSET_EQUALS, 0);
            if (indexOfCharset != AsciiString.INDEX_NOT_FOUND) {
                int indexOfEncoding = indexOfCharset + CHARSET_EQUALS.length();
                if (indexOfEncoding < contentTypeValue.length()) {
                    return contentTypeValue.subSequence(indexOfEncoding, contentTypeValue.length());
                }
            }
        }
        return null;
    }

    /**
     * Fetch MIME type part from message's Content-Type header as a char sequence.
     *
     * @return the MIME type as a {@code CharSequence} from message's Content-Type header
     * or {@code null} if content-type header or MIME type part of this header are not presented
     * <p/>
     * "content-type: text/html; charset=utf-8" - "text/html" will be returned <br/>
     * "content-type: text/html" - "text/html" will be returned <br/>
     * "content-type: " or no header - {@code null} we be returned
     */
    public static CharSequence getMimeType(HttpMessage message) {
        CharSequence contentTypeValue = message.headers().get(HttpHeaderNames.CONTENT_TYPE);
        if (contentTypeValue != null) {
            int indexOfSemicolon = AsciiString.indexOfIgnoreCaseAscii(contentTypeValue, SEMICOLON, 0);
            if (indexOfSemicolon != AsciiString.INDEX_NOT_FOUND) {
                return contentTypeValue.subSequence(0, indexOfSemicolon);
            } else {
                return contentTypeValue.length() > 0 ? contentTypeValue : null;
            }
        }
        return null;
    }

    static void encodeAscii0(CharSequence seq, IByteBuffer buf) {
        int length = seq.length();
        for (int i = 0 ; i < length; i++) {
            buf.writeByte(c2b(seq.charAt(i)));
        }
    }

    private static byte c2b(char c) {
        return c > 255 ? (byte) '?' : (byte) c;
    }
}
