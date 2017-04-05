// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http.Multipart
{
    using System;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using DotNetty.Buffers;
    using DotNetty.Common.Utilities;
    using DotNetty.Transport.Channels;

    public abstract class AbstractHttpData : AbstractReferenceCounted, IHttpData
    {
        const string StripPattern = "(?:^\\s+|\\s+$|\\n)";
        const string ReplacePattern = "[\\r\\t]";

        protected long DefinedSize;
        protected long Size;
        Encoding contentEncoding = HttpConstants.DefaultEncoding;

        protected AbstractHttpData(string name, Encoding contentEncoding, long size)
        {
            Contract.Requires(name != null);

            name = Regex.Replace(name, StripPattern, " ");
            name = Regex.Replace(name, ReplacePattern, "");
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("empty name");
            }

            this.Name = name;
            if (contentEncoding != null)
            {
                this.contentEncoding = contentEncoding;
            }

            this.DefinedSize = size;
        }

        public long MaxSize { get; set; } = DefaultHttpDataFactory.MaxSize;

        public void CheckSize(long newSize)
        {
            if (this.MaxSize >= 0 && newSize > this.MaxSize)
            {
                throw new IOException("Size exceed allowed maximum capacity");
            }
        }

        public string Name { get; }

        public bool Completed { get; protected set; }

        public Encoding ContentEncoding
        {
            get => this.contentEncoding;
            set
            {
                Contract.Requires(value != null);
                this.contentEncoding = value;
            }
        }

        public long Length => this.Size;

        public long DefinedLength => this.DefinedSize;

        public IByteBuffer Content
        {
            get
            {
                try
                {
                    return this.GetByteBuffer();
                }
                catch (IOException e)
                {
                    throw new ChannelException(e);
                }
            }
        }

        protected override void Deallocate() => this.Delete();

        public abstract int CompareTo(IPostHttpData other);

        public abstract HttpDataType DataType { get; }

        public abstract IByteBufferHolder Copy();

        public abstract IByteBufferHolder Duplicate();

        public abstract void SetContent(IByteBuffer buffer);

        public abstract void SetContent(Stream source);

        public abstract void AddContent(IByteBuffer buffer, bool last);

        public abstract void Delete();

        public abstract byte[] GetBytes();

        public abstract IByteBuffer GetByteBuffer();

        public abstract IByteBuffer GetChunk(int length);

        public virtual string GetString() => this.GetString(this.contentEncoding);

        public abstract string GetString(Encoding encoding);

        public abstract bool RenameTo(FileStream destination);

        public abstract bool InMemory { get; }

        public abstract FileStream GetFileStream();

        public abstract IHttpData Replace(IByteBuffer content);
    }
}
