// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http
{
    using DotNetty.Common.Utilities;

    /**
 * Defines the common schemes used for the HTTP protocol as defined by
 * <a href="https://tools.ietf.org/html/rfc7230">rfc7230</a>.
 */

    public sealed class HttpScheme
    {
        public static readonly HttpScheme HTTP = new HttpScheme(80, "http");
        public static readonly HttpScheme HTTPS = new HttpScheme(443, "https");

        readonly int port;
        readonly AsciiString name;

        HttpScheme(int port, string name)
        {
            this.port = port;
            this.name = new AsciiString(name);
        }

        public AsciiString Name => this.name;

        public int Port => this.port;

        // @Override
        public override bool Equals(object o)
        {
            if (!(o is HttpScheme))
            {
                return false;
            }
            var other = (HttpScheme)o;
            return (other.Port == this.port) && other.Name.Equals(this.name);
        }

        // @Override
        public override int GetHashCode() => this.port * 31 + this.name.GetHashCode();

        // @Override
        public override string ToString() => this.name.ToString();
    }
}