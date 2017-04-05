// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http.Multipart
{
    using System;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Text;
    using DotNetty.Buffers;
    using DotNetty.Transport.Channels;

    public class MemoryFileUpload : AbstractMemoryHttpData, IFileUpload
    {
        string fileName;
        string contentType;

        public MemoryFileUpload(string name, string fileName, string contentType, string transferEncoding, Encoding contentEncoding, long size)
            : base(name, contentEncoding, size)
        {
            Contract.Requires(fileName != null);
            Contract.Requires(contentType != null);

            this.fileName = fileName;
            this.contentType = contentType;
            this.TransferEncoding = transferEncoding;
        }

        public override HttpDataType DataType => HttpDataType.FileUpload;

        public string FileName
        {
            get
            {
                return this.fileName;
            }
            set
            {
                Contract.Requires(value != null);
                this.fileName = value;
            }
        }

        public override int GetHashCode() => FileUploadUtil.HashCode(this);

        public override bool Equals(object obj)
        {
            var fileUpload = obj as IFileUpload;
            return fileUpload != null && FileUploadUtil.Equals(this, fileUpload);
        }

        public override int CompareTo(IPostHttpData other)
        {
            if (!(other is IFileUpload))
            {
                throw new ArgumentException($"Cannot compare {this.DataType} with {other.DataType}");
            }

            return this.CompareTo((IFileUpload)other);
        }

        public int CompareTo(IFileUpload other) => FileUploadUtil.CompareTo(this, other);

        public string ContentType
        {
            get
            {
                return this.contentType;
            }
            set
            {
                Contract.Requires(value != null);

                this.contentType = value;
            }
        }

        public string TransferEncoding { get; set; }

        public override IByteBufferHolder Copy() => this.Replace(this.Content?.Copy());

        public override IByteBufferHolder Duplicate() => this.Replace(this.Content?.Duplicate());

        public override IHttpData Replace(IByteBuffer content)
        {
            var upload = new MemoryFileUpload(this.Name, this.FileName, this.ContentType, 
                this.TransferEncoding, this.ContentEncoding, this.Size);
            if (content != null)
            {
                try
                {
                    upload.SetContent(content);
                    return upload;
                }
                catch (IOException e)
                {
                    throw new ChannelException(e);
                }
            }

            return upload;
        }
    }
}
