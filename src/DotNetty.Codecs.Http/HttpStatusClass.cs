// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http
{
    using System;
    using DotNetty.Common.Utilities;

    public struct HttpStatusClass : IEquatable<HttpStatusClass>
    {
        public static readonly HttpStatusClass Informational = new HttpStatusClass(100, 200, "Informational");

        public static readonly HttpStatusClass Success = new HttpStatusClass(200, 300, "Success");

        public static readonly HttpStatusClass Redirection = new HttpStatusClass(300, 400, "Redirection");

        public static readonly HttpStatusClass ClientError = new HttpStatusClass(400, 500, "Client Error");

        public static readonly HttpStatusClass ServerError = new HttpStatusClass(500, 600, "Server Error");

        public static readonly HttpStatusClass Unknown = new HttpStatusClass(0, 0, "Unknown Status");

        public static HttpStatusClass ValueOf(int code)
        {
            if (Informational.Contains(code))
            {
                return Informational;
            }

            if (Success.Contains(code))
            {
                return Success;
            }

            if (Redirection.Contains(code))
            {
                return Redirection;
            }

            if (ClientError.Contains(code))
            {
                return ClientError;
            }

            if (ServerError.Contains(code))
            {
                return ServerError;
            }

            return Unknown;
        }

        readonly int min;
        readonly int max;

        HttpStatusClass(int min, int max, string defaultReasonPhrase)
        {
            this.min = min;
            this.max = max;
            this.DefaultReasonPhrase = new AsciiString(defaultReasonPhrase);
        }

        public bool Contains(int code)
        {
            if ((this.min & this.max) == 0)
            {
                return code < 100 || code >= 600;
            }

            return code >= this.min && code < this.max;
        }

        public AsciiString DefaultReasonPhrase { get; }

        public bool Equals(HttpStatusClass other) => this.min == other.min && this.max == other.max;

        public override bool Equals(object obj) => 
            obj is HttpStatusClass && this.Equals((HttpStatusClass)obj);

        public override int GetHashCode() => this.min.GetHashCode() ^ this.max.GetHashCode();

        public static bool operator !=(HttpStatusClass left, HttpStatusClass right) => !(left == right);

        public static bool operator ==(HttpStatusClass left, HttpStatusClass right) => left.Equals(right);
    }
}
