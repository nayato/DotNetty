// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http {
    using System;
    using DotNetty.Buffers;

    /**
 * The version of HTTP or its derived protocols, such as
 * <a href="http://en.wikipedia.org/wiki/Real_Time_Streaming_Protocol">RTSP</a> and
 * <a href="http://en.wikipedia.org/wiki/Internet_Content_Adaptation_Protocol">ICAP</a>.
 */
public class HttpVersion : IComparable<HttpVersion> {

    private static readonly Pattern VERSION_PATTERN =
        Pattern.compile("(\\S+)/(\\d+)\\.(\\d+)");

    private static readonly string HTTP_1_0_STRING = "HTTP/1.0";
    private static readonly string HTTP_1_1_STRING = "HTTP/1.1";

    /**
     * HTTP/1.0
     */
    public static readonly HttpVersion HTTP_1_0 = new HttpVersion("HTTP", 1, 0, false, true);

    /**
     * HTTP/1.1
     */
    public static readonly HttpVersion HTTP_1_1 = new HttpVersion("HTTP", 1, 1, true, true);

    /**
     * Returns an existing or new {@link HttpVersion} instance which matches to
     * the specified protocol version string.  If the specified {@code text} is
     * equal to {@code "HTTP/1.0"}, {@link #HTTP_1_0} will be returned.  If the
     * specified {@code text} is equal to {@code "HTTP/1.1"}, {@link #HTTP_1_1}
     * will be returned.  Otherwise, a new {@link HttpVersion} instance will be
     * returned.
     */
    public static HttpVersion valueOf(string text) {
        if (text == null) {
            throw new ArgumentNullException(nameof(text));
        }

        text = text.trim();

        if (text.isEmpty()) {
            throw new ArgumentException("text is empty");
        }

        // Try to match without convert to uppercase first as this is what 99% of all clients
        // will send anyway. Also there is a change to the RFC to make it clear that it is
        // expected to be case-sensitive
        //
        // See:
        // * http://trac.tools.ietf.org/wg/httpbis/trac/ticket/1
        // * http://trac.tools.ietf.org/wg/httpbis/trac/wiki
        //
        HttpVersion version = version0(text);
        if (version == null) {
            version = new HttpVersion(text, true);
        }
        return version;
    }

    private static HttpVersion version0(string text) {
        if (HTTP_1_1_STRING.Equals(text)) {
            return HTTP_1_1;
        }
        if (HTTP_1_0_STRING.Equals(text)) {
            return HTTP_1_0;
        }
        return null;
    }

    private readonly string protocolName;
    private readonly int majorVersion;
    private readonly int minorVersion;
    private readonly string text;
    private readonly bool keepAliveDefault;
    private readonly byte[] bytes;

    /**
     * Creates a new HTTP version with the specified version string.  You will
     * not need to create a new instance unless you are implementing a protocol
     * derived from HTTP, such as
     * <a href="http://en.wikipedia.org/wiki/Real_Time_Streaming_Protocol">RTSP</a> and
     * <a href="http://en.wikipedia.org/wiki/Internet_Content_Adaptation_Protocol">ICAP</a>.
     *
     * @param keepAliveDefault
     *        {@code true} if and only if the connection is kept alive unless
     *        the {@code "Connection"} header is set to {@code "close"} explicitly.
     */
    public HttpVersion(string text, bool keepAliveDefault) {
        if (text == null) {
            throw new ArgumentNullException(nameof(text");
        }

        text = text.trim().toUpperCase();
        if (text.isEmpty()) {
            throw new ArgumentException("empty text");
        }

        Matcher m = VERSION_PATTERN.matcher(text);
        if (!m.matches()) {
            throw new ArgumentException("invalid version format: " + text);
        }

        protocolName = m.group(1);
        majorVersion = Integer.parseInt(m.group(2));
        minorVersion = Integer.parseInt(m.group(3));
        this.text = protocolName + '/' + majorVersion + '.' + minorVersion;
        this.keepAliveDefault = keepAliveDefault;
        bytes = null;
    }

    /**
     * Creates a new HTTP version with the specified protocol name and version
     * numbers.  You will not need to create a new instance unless you are
     * implementing a protocol derived from HTTP, such as
     * <a href="http://en.wikipedia.org/wiki/Real_Time_Streaming_Protocol">RTSP</a> and
     * <a href="http://en.wikipedia.org/wiki/Internet_Content_Adaptation_Protocol">ICAP</a>
     *
     * @param keepAliveDefault
     *        {@code true} if and only if the connection is kept alive unless
     *        the {@code "Connection"} header is set to {@code "close"} explicitly.
     */
    public HttpVersion(
            string protocolName, int majorVersion, int minorVersion,
            bool keepAliveDefault) {
        this(protocolName, majorVersion, minorVersion, keepAliveDefault, false);
    }

    private HttpVersion(
            string protocolName, int majorVersion, int minorVersion,
            bool keepAliveDefault, bool bytes) {
        if (protocolName == null) {
            throw new ArgumentNullException(nameof(protocolName");
        }

        protocolName = protocolName.trim().toUpperCase();
        if (protocolName.isEmpty()) {
            throw new ArgumentException("empty protocolName");
        }

        for (int i = 0; i < protocolName.length(); i ++) {
            if (Character.isISOControl(protocolName.charAt(i)) ||
                    Character.isWhitespace(protocolName.charAt(i))) {
                throw new ArgumentException("invalid character in protocolName");
            }
        }

        if (majorVersion < 0) {
            throw new ArgumentException("negative majorVersion");
        }
        if (minorVersion < 0) {
            throw new ArgumentException("negative minorVersion");
        }

        this.protocolName = protocolName;
        this.majorVersion = majorVersion;
        this.minorVersion = minorVersion;
        text = protocolName + '/' + majorVersion + '.' + minorVersion;
        this.keepAliveDefault = keepAliveDefault;

        if (bytes) {
            this.bytes = text.getBytes(CharsetUtil.US_ASCII);
        } else {
            this.bytes = null;
        }
    }

    /**
     * Returns the name of the protocol such as {@code "HTTP"} in {@code "HTTP/1.0"}.
     */
    public string protocolName() {
        return protocolName;
    }

    /**
     * Returns the name of the protocol such as {@code 1} in {@code "HTTP/1.0"}.
     */
    public int majorVersion() {
        return majorVersion;
    }

    /**
     * Returns the name of the protocol such as {@code 0} in {@code "HTTP/1.0"}.
     */
    public int minorVersion() {
        return minorVersion;
    }

    /**
     * Returns the full protocol version text such as {@code "HTTP/1.0"}.
     */
    public string text() {
        return text;
    }

    /**
     * Returns {@code true} if and only if the connection is kept alive unless
     * the {@code "Connection"} header is set to {@code "close"} explicitly.
     */
    public bool isKeepAliveDefault() {
        return keepAliveDefault;
    }

    /**
     * Returns the full protocol version text such as {@code "HTTP/1.0"}.
     */
    // @Override
    public string toString() {
        return text();
    }

    // @Override
    public int GetHashCode() {
        return (protocolName().GetHashCode() * 31 + majorVersion()) * 31 +
               minorVersion();
    }

    // @Override
    public bool Equals(object o) {
        if (!(o is HttpVersion)) {
            return false;
        }

        HttpVersion that = (HttpVersion) o;
        return minorVersion() == that.minorVersion() &&
               majorVersion() == that.majorVersion() &&
               protocolName().Equals(that.protocolName());
    }

    // @Override
    public int compareTo(HttpVersion o) {
        int v = protocolName().compareTo(o.protocolName());
        if (v != 0) {
            return v;
        }

        v = majorVersion() - o.majorVersion();
        if (v != 0) {
            return v;
        }

        return minorVersion() - o.minorVersion();
    }

    void encode(IByteBuffer buf) {
        if (bytes == null) {
            HttpUtil.encodeAscii0(text, buf);
        } else {
            buf.writeBytes(bytes);
        }
    }
}
