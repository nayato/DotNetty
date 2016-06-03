// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http
{
    using System.Collections.Generic;
    using System.Text;
    using DotNetty.Common.Utilities;

    /**
 * Provides some utility methods for HTTP message implementations.
 */

    sealed class HttpMessageUtil
    {
        static StringBuilder appendRequest(StringBuilder buf, HttpRequest req)
        {
            appendCommon(buf, req);
            appendInitialLine(buf, req);
            appendHeaders(buf, req.headers());
            removeLastNewLine(buf);
            return buf;
        }

        static StringBuilder appendResponse(StringBuilder buf, HttpResponse res)
        {
            appendCommon(buf, res);
            appendInitialLine(buf, res);
            appendHeaders(buf, res.headers());
            removeLastNewLine(buf);
            return buf;
        }

        static void appendCommon(StringBuilder buf, HttpMessage msg)
        {
            buf.Append(StringUtil.SimpleClassName(msg));
            buf.Append("(decodeResult: ");
            buf.Append(msg.decoderResult());
            buf.Append(", version: ");
            buf.Append(msg.protocolVersion());
            buf.Append(')');
            buf.Append(StringUtil.Newline);
        }

        static StringBuilder appendFullRequest(StringBuilder buf, FullHttpRequest req)
        {
            appendFullCommon(buf, req);
            appendInitialLine(buf, req);
            appendHeaders(buf, req.headers());
            appendHeaders(buf, req.trailingHeaders());
            removeLastNewLine(buf);
            return buf;
        }

        static StringBuilder appendFullResponse(StringBuilder buf, FullHttpResponse res)
        {
            appendFullCommon(buf, res);
            appendInitialLine(buf, res);
            appendHeaders(buf, res.headers());
            appendHeaders(buf, res.trailingHeaders());
            removeLastNewLine(buf);
            return buf;
        }

        static void appendFullCommon(StringBuilder buf, FullHttpMessage msg)
        {
            buf.Append(StringUtil.SimpleClassName(msg));
            buf.Append("(decodeResult: ");
            buf.Append(msg.decoderResult());
            buf.Append(", version: ");
            buf.Append(msg.protocolVersion());
            buf.Append(", content: ");
            buf.Append(msg.content());
            buf.Append(')');
            buf.Append(StringUtil.Newline);
        }

        static void appendInitialLine(StringBuilder buf, HttpRequest req)
        {
            buf.Append(req.method());
            buf.Append(' ');
            buf.Append(req.uri());
            buf.Append(' ');
            buf.Append(req.protocolVersion());
            buf.Append(StringUtil.Newline);
        }

        static void appendInitialLine(StringBuilder buf, HttpResponse res)
        {
            buf.Append(res.protocolVersion());
            buf.Append(' ');
            buf.Append(res.status());
            buf.Append(StringUtil.Newline);
        }

        static void appendHeaders(StringBuilder buf, HttpHeaders headers)
        {
            foreach (KeyValuePair<string, string> e in headers)
            {
                buf.Append(e.Key);
                buf.Append(": ");
                buf.Append(e.Value);
                buf.Append(StringUtil.Newline);
            }
        }

        static void removeLastNewLine(StringBuilder buf) => buf.Length = buf.Length - StringUtil.Newline.Length;

        HttpMessageUtil()
        {
        }
    }
}