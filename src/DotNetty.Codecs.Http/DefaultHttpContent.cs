// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http
{
    using System.Diagnostics.Contracts;
    using DotNetty.Buffers;
    using DotNetty.Common;
    using DotNetty.Common.Utilities;

    public class DefaultHttpContent : DefaultHttpObject, IHttpContent
    {
        public DefaultHttpContent(IByteBuffer content)
        {
            Contract.Requires(content != null);

            this.Content = content;
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

        public virtual IHttpContent Replace(IByteBuffer buffer) => new DefaultHttpContent(buffer);

        public IByteBufferHolder Copy() => this.Replace(this.Content.Copy());

        public IByteBufferHolder Duplicate() => this.Replace(this.Content.Duplicate());

        public override string ToString() => 
            $"{StringUtil.SimpleClassName(this)} (data: {this.Content}, decoderResult: {this.Result})";
    }
}
