// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http {
    using System.Text;
    using DotNetty.Buffers;
    using DotNetty.Common.Utilities;

    /**
 * Encodes an {@link HttpRequest} or an {@link HttpContent} into
 * a {@link IByteBuffer}.
 */
public class HttpRequestEncoder : HttpObjectEncoder<HttpRequest> {
    private static readonly char SLASH = '/';
    private static readonly char QUESTION_MARK = '?';
    private static readonly byte[] CRLF = { CR, LF };

    // @Override
    public bool acceptOutboundMessage(object msg)  {
        return base.acceptOutboundMessage(msg) && !(msg is HttpResponse);
    }

    // @Override
    protected void encodeInitialLine(IByteBuffer buf, HttpRequest request)  {
        AsciiString method = request.method().asciiName();
        ByteBufferUtil.Copy(method, method.arrayOffset(), buf, method.Length);
        buf.WriteByte(SP);

        // Add / as absolute path if no is present.
        // See http://tools.ietf.org/html/rfc2616#section-5.1.2
        string uri = request.uri();

        if (uri.Length == 0) {
            uri += SLASH;
        } else {
            int start = uri.IndexOf("://");
            if (start != -1 && uri[0] != SLASH) {
                int startIndex = start + 3;
                // Correctly handle query params.
                // See https://github.com/netty/netty/issues/2732
                int index = uri.IndexOf(QUESTION_MARK, startIndex);
                if (index == -1) {
                    if (uri.LastIndexOf(SLASH) <= startIndex) {
                        uri += SLASH;
                    }
                } else {
                    if (uri.LastIndexOf(SLASH, index) <= startIndex) {
                        int len = uri.Length;
                        StringBuilder sb = new StringBuilder(len + 1);
                        sb.Append(uri, 0, index)
                          .Append(SLASH)
                          .Append(uri, index, len);
                        uri = sb.ToString();
                    }
                }
            }
        }

        buf.WriteBytes(Encoding.UTF8.GetBytes(uri));

        buf.WriteByte(SP);
        request.protocolVersion().encode(buf);
        buf.WriteBytes(CRLF);
    }
}
