// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http
{
    using System.Diagnostics.Contracts;
    using System.Text;
    using DotNetty.Buffers;
    using DotNetty.Common;

    public class DefaultFullHttpRequest : DefaultHttpRequest, IFullHttpRequest
    {
        /**
         * Used to cache the value of the hash code and avoid {@link IllegalReferenceCountException}.
         */
        int hash;

        public DefaultFullHttpRequest(HttpVersion httpVersion, HttpMethod method, string uri, bool validateHeaders = true) 
            : this(httpVersion, method, uri, Unpooled.Buffer(0), validateHeaders)
        {
        }

        public DefaultFullHttpRequest(HttpVersion httpVersion, HttpMethod method, string uri, IByteBuffer content)
            : this(httpVersion, method, uri, content, true)
        {
        }

        public DefaultFullHttpRequest(HttpVersion httpVersion, HttpMethod method, string uri, IByteBuffer content, bool validateHeaders)
            : base(httpVersion, method, uri, validateHeaders)
        {
            Contract.Requires(content != null);

            this.Content = content;
            this.TrailingHeaders = new DefaultHttpHeaders(validateHeaders);
        }

        public DefaultFullHttpRequest(HttpVersion httpVersion, HttpMethod method, string uri, IByteBuffer content, HttpHeaders headers, HttpHeaders trailingHeader) 
            : base(httpVersion, method, uri, headers)
        {
            Contract.Requires(content != null);
            Contract.Requires(trailingHeader != null);

            this.Content = content;
            this.TrailingHeaders = trailingHeader;
        }

        public int ReferenceCount => this.Content.ReferenceCount;

        public IReferenceCounted Retain()
        {
            this.Content.Retain();
            return this;
        }

        public IReferenceCounted Retain(int increment)
        {
            this.Content.Retain(increment);
            return this;
        }

        public IReferenceCounted Touch()
        {
            this.Content.Touch();
            return this;
        }

        public IReferenceCounted Touch(object hint)
        {
            this.Content.Touch(hint);
            return this;
        }

        public bool Release() => this.Content.Release();

        public bool Release(int decrement) => this.Content.Release(decrement);

        public IByteBuffer Content { get; }

        public IByteBufferHolder Copy() => this.Replace(this.Content.Copy());

        public IByteBufferHolder Duplicate() => this.Replace(this.Content.Duplicate());

        public IFullHttpRequest Replace(IByteBuffer newContent) => 
            new DefaultFullHttpRequest(this.ProtocolVersion, this.Method, this.Uri, newContent, this.Headers, this.TrailingHeaders);

        public HttpHeaders TrailingHeaders { get; }

        public override int GetHashCode()
        {
            // ReSharper disable NonReadonlyMemberInGetHashCode
            int hashCode = this.hash;
            if (hashCode == 0)
            {
                if (this.Content.ReferenceCount != 0)
                {
                    try
                    {
                        hashCode = 31 + this.Content.GetHashCode();
                    }
                    catch (IllegalReferenceCountException)
                    {
                        // Handle race condition between checking refCnt() == 0 and using the object.
                        hashCode = 31;
                    }
                }
                else
                {
                    hashCode = 31;
                }
                hashCode = 31 * hashCode + this.TrailingHeaders.GetHashCode();
                hashCode = 31 * hashCode + base.GetHashCode();

                this.hash = hashCode;
            }
            // ReSharper restore NonReadonlyMemberInGetHashCode

            return hashCode;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is DefaultFullHttpRequest))
            {
                return false;
            }
            var other = (DefaultFullHttpRequest)obj;

            return base.Equals(other) 
                &&this.Content.Equals(other.Content) 
                && this.TrailingHeaders.Equals(other.TrailingHeaders);
        }

        public override string ToString() => 
            HttpMessageUtil.AppendFullRequest(new StringBuilder(256), this).ToString();
    }
}
