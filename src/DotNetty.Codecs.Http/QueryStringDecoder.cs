// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Text;

    public class QueryStringDecoder
    {
        const int DefaultMaxParams = 1024;

        readonly Encoding encoding;
        readonly bool hasPath;
        readonly int maxParams;
        string path;
        int nParams;
        Dictionary<string, List<string>> parameters;

        public QueryStringDecoder(string uri) : this(uri, HttpConstants.DefaultEncoding)
        {
        }

        public QueryStringDecoder(string uri, bool hasPath) : this(uri, HttpConstants.DefaultEncoding, hasPath)
        {
        }

        public QueryStringDecoder(string uri, Encoding encoding, bool hasPath = true, int maxParams = DefaultMaxParams)
        {
            Contract.Requires(uri != null);
            Contract.Requires(encoding != null);
            Contract.Requires(maxParams > 0);

            this.UriString = uri;
            this.encoding = encoding;
            this.maxParams = maxParams;
            this.hasPath = hasPath;
        }

        public QueryStringDecoder(Uri uri) : this(uri, HttpConstants.DefaultEncoding)
        {
        }

        public QueryStringDecoder(Uri uri, Encoding encoding, int maxParams = DefaultMaxParams)
        {
            Contract.Requires(uri != null);
            Contract.Requires(encoding != null);
            Contract.Requires(maxParams > 0);

            this.UriString = uri.PathAndQuery;
            this.hasPath = true; // Note Uri always has root path '/'

            this.encoding = encoding;
            this.maxParams = maxParams;
        }

        public string UriString { get; }

        public string GetPath()
        {
            if (this.path != null)
            {
                return this.path;
            }
           
            if (!this.hasPath)
            {
                this.path = "/";
            }
            else
            {
                int pathEndPos = this.UriString.IndexOf('?');
                this.path = DecodeComponent(
                    pathEndPos < 0 ? this.UriString : this.UriString.Substring(0, pathEndPos), this.encoding);
            }

            return this.path;
        }

        public IDictionary<string, List<string>> Parameters()
        {
            if (this.parameters != null)
            {
                return this.parameters;
            }

            if (this.hasPath)
            {
                int pathEndPos = this.UriString.IndexOf('?');
                if (pathEndPos >= 0 && pathEndPos < this.UriString.Length - 1)
                {
                    this.DecodeParams(this.UriString.Substring(pathEndPos + 1));
                }
                else
                {
                    this.parameters = new Dictionary<string, List<string>>();
                }
            }
            else
            {
                if (this.UriString.Length == 0)
                {
                    this.parameters = new Dictionary<string, List<string>>();
                }
                else
                {
                    this.DecodeParams(this.UriString);
                }
            }

            return this.parameters;
        }

        void DecodeParams(string s)
        {
            this.parameters = new Dictionary<string, List<string>>();

            this.nParams = 0;
            string name = null;
            int pos = 0; // Beginning of the unprocessed region
            int i; // End of the unprocessed region

            for (i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '=' && name == null)
                {
                    if (pos != i)
                    {
                        name = DecodeComponent(s.Substring(pos, i - pos), this.encoding);
                    }
                    pos = i + 1;
                    // http://www.w3.org/TR/html401/appendix/notes.html#h-B.2.2
                }
                else if (c == '&' || c == ';')
                {
                    if (name == null && pos != i)
                    {
                        // We haven't seen an `=' so far but moved forward.
                        // Must be a param of the form '&a&' so add it with
                        // an empty value.
                        if (!this.AddParam(this.parameters, DecodeComponent(s.Substring(pos, i - pos), this.encoding), string.Empty))
                        {
                            return;
                        }
                    }
                    else if (name != null)
                    {
                        if (!this.AddParam(this.parameters, name, DecodeComponent(s.Substring(pos, i - pos), this.encoding)))
                        {
                            return;
                        }
                        name = null;
                    }
                    pos = i + 1;
                }
            }

            if (pos != i)
            {  
                // Are there characters we haven't dealt with?
                if (name == null)
                {     // Yes and we haven't seen any `='.
                    this.AddParam(this.parameters, DecodeComponent(s.Substring(pos, i - pos), this.encoding), string.Empty);
                }
                else
                {                // Yes and this must be the last value.
                    this.AddParam(this.parameters, name, DecodeComponent(s.Substring(pos, i - pos), this.encoding));
                }
            }
            else if (name != null)
            {  
                // Have we seen a name without value?
                this.AddParam(this.parameters, name, string.Empty);
            }
        }

        bool AddParam(IDictionary<string, List<string>> paramsSet, string name, string value)
        {
            if (this.nParams >= this.maxParams)
            {
                return false;
            }

            if (!paramsSet.TryGetValue(name, out List<string> values))
            {
                values = new List<string>(1); // Often there's only 1 value.
                paramsSet.Add(name, values);
            }

            values.Add(value);
            this.nParams++;

            return true;
        }

        public static string DecodeComponent(string s) => DecodeComponent(s, HttpConstants.DefaultEncoding);

        public static string DecodeComponent(string s, Encoding encoding)
        {
            if (s == null)
            {
                return string.Empty;
            }

            int size = s.Length;
            bool modified = false;
            for (int i = 0; i < size; i++)
            {
                char c = s[i];
                if (c == '%' || c == '+')
                {
                    modified = true;
                    break;
                }
            }
            if (!modified)
            {
                return s;
            }

            var buf = new char[size];
            int pos = 0;  // position in `buf'.
            for (int i = 0; i < size; i++)
            {
                char c = s[i];
                switch (c)
                {
                    case '+':
                        buf[pos++] = ' ';  // "+" -> " "
                        break;
                    case '%':
                        if (i == size - 1)
                        {
                            throw new ArgumentException($"unterminated escape sequence at end of string: {s}");
                        }
                        c = s[++i];
                        if (c == '%')
                        {
                            buf[pos++] = '%';  // "%%" -> "%"
                            break;
                        }
                        if (i == size - 1)
                        {
                            throw new ArgumentException($"partial escape sequence at end of string: {s}");
                        }
                        c = DecodeHexNibble(c);
                        char c2 = DecodeHexNibble(s[++i]);
                        if (c == char.MaxValue || c2 == char.MaxValue)
                        {
                            throw new ArgumentException($"invalid escape sequence `%{s[i - 1]}{s[i]}' at index {i - 2} of: {s}");
                        }

                        buf[pos++] = (char)(c * 16 + c2);
                        break;
                    default:
                        buf[pos++] = c;
                        break;
                }
            }

            return new string(buf, 0, pos);
        }

        //
        // Helper to decode half of a hexadecimal number from a string.
        // @param c The ASCII character of the hexadecimal number to decode.
        // Must be in the range {@code [0-9a-fA-F]}.
        // @return The hexadecimal value represented in the ASCII character
        // given, or {@link Character#MAX_VALUE} if the character is invalid.
        // 
        static char DecodeHexNibble(char c)
        {
            if ('0' <= c && c <= '9')
            {
                return (char)(c - '0');
            }
            else if ('a' <= c && c <= 'f')
            {
                return (char)(c - 'a' + 10);
            }
            else if ('A' <= c && c <= 'F')
            {
                return (char)(c - 'A' + 10);
            }

            return char.MaxValue;
        }
    }
}
