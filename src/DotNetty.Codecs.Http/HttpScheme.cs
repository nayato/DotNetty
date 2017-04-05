// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http
{
    using DotNetty.Common.Utilities;

    public sealed class HttpScheme
    {
        HttpScheme(int port, string name)
        {
            this.Port = port;
            this.Name = new AsciiString(name);
        }

        public AsciiString Name { get; }

        public int Port { get; }

        public override bool Equals(object obj)
        {
            if (!(obj is HttpScheme)) {
                return false;
            }

            var other = (HttpScheme)obj;
            return other.Port == this.Port 
                && other.Name.Equals(this.Name);
        }

        public override int GetHashCode() => this.Port * 31 + this.Name.GetHashCode();

        public override string ToString() => this.Name.ToString();
    }
}
