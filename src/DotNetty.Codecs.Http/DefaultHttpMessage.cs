// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http
{
    using System.Diagnostics.Contracts;

    public abstract class DefaultHttpMessage : DefaultHttpObject, IHttpMessage
    {
        const int HashCodePrime = 31;
        HttpVersion version;

        protected DefaultHttpMessage(HttpVersion version, bool validateHeaders = true, bool singleFieldHeaders = false)
        {
            Contract.Requires(version != null);

            this.version = version;
            this.Headers = singleFieldHeaders
                ? new CombinedHttpHeaders(validateHeaders)
                : new DefaultHttpHeaders(validateHeaders);
        }

        protected DefaultHttpMessage(HttpVersion version, HttpHeaders headers)
        {
            Contract.Requires(version != null);
            Contract.Requires(headers != null);

            this.version = version;
            this.Headers = headers;
        }

        public HttpVersion ProtocolVersion
        {
            get => this.version;
            set
            {
                Contract.Requires(value != null);
                this.version = value;
            }
        }

        public HttpHeaders Headers { get; }

        public override int GetHashCode()
        {
            int result = 1;
            result = HashCodePrime * result + this.Headers.GetHashCode();
            // ReSharper disable once NonReadonlyMemberInGetHashCode
            result = HashCodePrime * result + this.version.GetHashCode();
            result = HashCodePrime * result + base.GetHashCode();
            return result;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is DefaultHttpMessage)) {
                return false;
            }

            var other = (DefaultHttpMessage)obj;

            return this.Headers.Equals(other.Headers) 
                && this.ProtocolVersion.Equals(other.ProtocolVersion) 
                && base.Equals(obj);
        }
    }
}
