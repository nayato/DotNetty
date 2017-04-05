// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http
{
    using System.Diagnostics.Contracts;
    using System.Text;
    using DotNetty.Buffers;
    using DotNetty.Common;

    public class DefaultFullHttpResponse : DefaultHttpResponse, IFullHttpResponse
    {
        /**
          * Used to cache the value of the hash code and avoid {@link IllegalReferenceCountException}.
          */
        int hash;

        public DefaultFullHttpResponse(HttpVersion version, HttpResponseStatus status, bool validateHeaders = true) 
            : this(version, status, Unpooled.Buffer(0), validateHeaders)
        {
        }

        public DefaultFullHttpResponse(HttpVersion version, HttpResponseStatus status, bool validateHeaders, bool singleFieldHeaders)
            : this(version, status, Unpooled.Buffer(0), validateHeaders, singleFieldHeaders)
        {
        }

        public DefaultFullHttpResponse(HttpVersion version, HttpResponseStatus status, IByteBuffer content, 
            bool validateHeaders = true, bool singleFieldHeaders = false)
            : base(version, status, validateHeaders, singleFieldHeaders)
        {
            Contract.Requires(content != null);

            this.Content = content;
            this.TrailingHeaders = singleFieldHeaders 
                ? new CombinedHttpHeaders(validateHeaders)
                : new DefaultHttpHeaders(validateHeaders);
        }

        public DefaultFullHttpResponse(HttpVersion version, HttpResponseStatus status, IByteBuffer content, HttpHeaders headers, HttpHeaders trailingHeaders)
            : base(version, status, headers)
        {
            Contract.Requires(content != null);
            Contract.Requires(trailingHeaders != null);

            this.Content = content;
            this.TrailingHeaders = trailingHeaders;
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

        public IFullHttpResponse Replace(IByteBuffer newContent) => 
            new DefaultFullHttpResponse(this.ProtocolVersion, this.Status, newContent, this.Headers, this.TrailingHeaders);

        public HttpHeaders TrailingHeaders { get; }

        public override int GetHashCode()
        {
            // ReSharper disable once NonReadonlyMemberInGetHashCode
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
            
            return hashCode;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is DefaultFullHttpResponse)) {
                return false;
            }

            var other = (DefaultFullHttpResponse)obj;

            return base.Equals(other) 
                && this.Content.Equals(other.Content) 
                && this.TrailingHeaders.Equals(other.TrailingHeaders);
        }

        public override string ToString() => 
            HttpMessageUtil.AppendFullResponse(new StringBuilder(256), this).ToString();
    }
}
