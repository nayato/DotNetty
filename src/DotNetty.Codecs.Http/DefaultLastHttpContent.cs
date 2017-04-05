// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http
{
    using System;
    using System.Text;
    using DotNetty.Buffers;
    using DotNetty.Common.Utilities;

    public class DefaultLastHttpContent : DefaultHttpContent, ILastHttpContent
    {
        static readonly TrailerNameValidator NameValidator = new TrailerNameValidator();

        readonly bool validateHeaders;

        public DefaultLastHttpContent() : this(Unpooled.Buffer(0))
        {
        }

        public DefaultLastHttpContent(IByteBuffer content, bool validateHeaders = true)
            : base(content)
        {
            this.TrailingHeaders = new TrailingHttpHeaders(validateHeaders);
            this.validateHeaders = validateHeaders;
        }

        public HttpHeaders TrailingHeaders { get; }

        public override IHttpContent Replace(IByteBuffer buffer)
        {
            var dup = new DefaultLastHttpContent(this.Content, this.validateHeaders);
            dup.TrailingHeaders.Set(this.TrailingHeaders);

            return dup;
        }

        public override string ToString()
        {
            var buf = new StringBuilder(base.ToString());
            buf.Append(StringUtil.Newline);
            this.AppendHeaders(buf);

            // Remove the last newline.
            buf.Length = buf.Length - StringUtil.Newline.Length;

            return buf.ToString();
        }

        void AppendHeaders(StringBuilder buf)
        {
            foreach (HeaderEntry<ICharSequence, ICharSequence> e in this.TrailingHeaders)
            {
                buf.Append($"{e.Key}: {e.Value}{StringUtil.Newline}");
            }
        }

        sealed class TrailerNameValidator : INameValidator<ICharSequence>
        {
            public void ValidateName(ICharSequence name)
            {
                DefaultHttpHeaders.HttpNameValidator.ValidateName(name);
                if (HttpHeaderNames.ContentLength.ContentEqualsIgnoreCase(name)
                    || HttpHeaderNames.TransferEncoding.ContentEqualsIgnoreCase(name)
                    || HttpHeaderNames.Trailer.ContentEqualsIgnoreCase(name))
                {
                    throw new ArgumentException($"prohibited trailing header: {name}");
                }
            }
        }

        sealed class TrailingHttpHeaders : DefaultHttpHeaders
        {
            public TrailingHttpHeaders(bool validate) : base(validate, validate? NameValidator : NotNullValidator)
            {
            }
        }
    }
}
