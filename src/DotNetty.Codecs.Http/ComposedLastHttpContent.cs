// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http
{
    using DotNetty.Buffers;
    using DotNetty.Common;

    public sealed class ComposedLastHttpContent : ILastHttpContent
    {
        internal ComposedLastHttpContent(HttpHeaders trailingHeaders)
        {
            this.TrailingHeaders = trailingHeaders;
        }
        public HttpHeaders TrailingHeaders { get; }

        public IByteBufferHolder Copy()
        {
            var content = new DefaultLastHttpContent(Unpooled.Empty);
            content.TrailingHeaders.Set(this.TrailingHeaders);

            return content;
        }

        public IByteBufferHolder Duplicate() => this.Copy();

        public DecoderResult Result { get; set; }

        public int ReferenceCount => 1;

        public IReferenceCounted Retain() => this;

        public IReferenceCounted Retain(int increment) => this;

        public IReferenceCounted Touch() => this;

        public IReferenceCounted Touch(object hint) => this;

        public bool Release() => false;

        public bool Release(int decrement) => false;

        public IByteBuffer Content => Unpooled.Empty;
    }
}
