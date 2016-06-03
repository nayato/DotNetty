// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http
{
    using System;

    /**
 * The default {@link HttpMessage} implementation.
 */

    public abstract class DefaultHttpMessage : DefaultHttpObject, HttpMessage
    {
        private static readonly int HASH_CODE_PRIME = 31;
        private HttpVersion version;
        private readonly HttpHeaders headers;

        /**
     * Creates a new instance.
     */

        protected DefaultHttpMessage(HttpVersion version)
        {
            this(version, true, false);
        }

        /**
     * Creates a new instance.
     */

        protected DefaultHttpMessage(HttpVersion version, bool validateHeaders, bool singleFieldHeaders)
        {
            this(version,
                singleFieldHeaders ? new CombinedHttpHeaders(validateHeaders)
                    : new DefaultHttpHeaders(validateHeaders));
        }

        /**
     * Creates a new instance.
     */

        protected DefaultHttpMessage(HttpVersion version, HttpHeaders headers)
        {
            this.version = checkNotNull(version, "version");
            this.headers = checkNotNull(headers, "headers");
        }

        // @Override
        public HttpHeaders headers()
        {
            return headers;
        }

        // @Override
        [Obsolete]
        public HttpVersion getProtocolVersion()
        {
            return protocolVersion();
        }

        // @Override
        public HttpVersion protocolVersion()
        {
            return version;
        }

        // @Override
        public override int GetHashCode()
        {
            int result = 1;
            result = HASH_CODE_PRIME * result + headers.GetHashCode();
            result = HASH_CODE_PRIME * result + version.GetHashCode();
            result = HASH_CODE_PRIME * result + base.GetHashCode();
            return result;
        }

        // @Override
        public override bool Equals(object o)
        {
            if (!(o is DefaultHttpMessage))
            {
                return false;
            }

            DefaultHttpMessage other = (DefaultHttpMessage)o;

            return headers().Equals(other.headers()) &&
                protocolVersion().Equals(other.protocolVersion()) &&
                base.Equals(o);
        }

        // @Override
        public HttpMessage setProtocolVersion(HttpVersion version)
        {
            if (version == null)
            {
                throw new ArgumentNullException(nameof(version));
            }
            this.version = version;
            return this;
        }
    }
}