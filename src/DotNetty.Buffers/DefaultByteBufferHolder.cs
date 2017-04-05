// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Buffers
{
    using System.Diagnostics.Contracts;
    using DotNetty.Common;

    public class DefaultByteBufferHolder : IByteBufferHolder
    {
        readonly IByteBuffer buffer;

        public DefaultByteBufferHolder(IByteBuffer buffer)
        {
            Contract.Requires(buffer != null);

            this.buffer = buffer;
        }

        public IByteBuffer Content
        {
            get
            {
                if (this.buffer.ReferenceCount <= 0)
                {
                    throw new IllegalReferenceCountException(this.buffer.ReferenceCount);
                }

                return this.buffer;
            }
        }

        public virtual int ReferenceCount => this.buffer.ReferenceCount;

        public virtual IReferenceCounted Retain()
        {
            this.buffer.Retain();
            return this;
        }

        public virtual IReferenceCounted Retain(int increment)
        {
            this.buffer.Retain(increment);
            return this;
        }

        public virtual IReferenceCounted Touch()
        {
            this.buffer.Touch();
            return this;
        }

        public virtual IReferenceCounted Touch(object hint)
        {
            this.buffer.Touch(hint);
            return this;
        }

        public virtual bool Release() => this.buffer.Release();

        public virtual bool Release(int decrement) => this.buffer.Release(decrement);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            var holder = obj as IByteBufferHolder;
            return holder != null 
                && this.buffer.Equals(holder.Content);
        }

        public override int GetHashCode() => this.buffer.GetHashCode();

        public virtual IByteBufferHolder Copy() => this.Replace(this.Content.Copy());

        public virtual IByteBufferHolder Duplicate() => this.Replace(this.Content.Duplicate());

        public IByteBufferHolder Replace(IByteBuffer content) => new DefaultByteBufferHolder(content);
    }
}
