// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http
{
    using System.Diagnostics.Contracts;
    using System.Text;
    using DotNetty.Common.Utilities;

    /// <summary>
    /// * Creates an URL-encoded URI from a path string and key-value parameter pairs.
    /// * This encoder is for one time use only.  Create a new instance for each URI.
    /// *
    /// * <pre>
    /// * {@link QueryStringEncoder} encoder = new {@link QueryStringEncoder}("/hello");
    /// * encoder.addParam("recipient", "world");
    /// * assert encoder.toString().equals("/hello?recipient=world");
    /// * </pre>
    /// * @see QueryStringDecoder
    /// </summary>
    public class QueryStringEncoder
    {
        const string EncodedSpace = "%20";

        readonly Encoding encoding;
        readonly StringBuilder uriBuilder;
        bool hasParams;

        public QueryStringEncoder(string uri)
            : this(uri, HttpConstants.DefaultEncoding)
        {
        }

        public QueryStringEncoder(string uri, Encoding encoding)
        {
            Contract.Requires(uri != null);
            Contract.Requires(encoding != null);

            this.uriBuilder = new StringBuilder(uri);
            this.encoding = encoding;
        }

        public void AddParam(string name, string value)
        {
            Contract.Requires(name != null);

            if (this.hasParams)
            {
                this.uriBuilder.Append('&');
            }
            else
            {
                this.uriBuilder.Append('?');
                this.hasParams = true;
            }

            AppendComponent(name, this.encoding, this.uriBuilder);
            if (value != null)
            {
                this.uriBuilder.Append('=');
                AppendComponent(value, this.encoding, this.uriBuilder);
            }
        }

        public string ToUriString() => this.uriBuilder.ToString();

        static void AppendComponent(string s, Encoding encoding, StringBuilder buf)
        {
            int count = encoding.GetMaxByteCount(1);
            var bytes = new byte[count];
            var array = new char[1];

            foreach (char ch in s)
            {
                if (ch >= 'a' && ch <= 'z'
                    || ch >= 'A' && ch <= 'Z'
                    || ch >= '0' && ch <= '9')
                {
                    buf.Append(ch);
                }
                else
                {
                    if (ch == '+')
                    {
                        buf.Append(EncodedSpace);
                    }
                    else
                    {
                        array[0] = ch;
                        count = encoding.GetBytes(array, 0, 1, bytes, 0);
                        for (int i = 0; i < count; i++)
                        {
                            buf.Append('%');
                            buf.Append(CharUtil.Digits[(bytes[i] & 0xf0) >> 4]);
                            buf.Append(CharUtil.Digits[bytes[i] & 0xf]);
                        }
                    }
                }
            }
        }
    }
}
