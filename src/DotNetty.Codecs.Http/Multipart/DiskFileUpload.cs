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

    public class DiskFileUpload : AbstractDiskHttpData, IFileUpload
    {
        public static string FileBaseDirectory;
        public static bool DeleteOnExitTemporaryFile = true;
        public static string FilePrefix = "FUp_";
        public static readonly string FilePostfix = ".tmp";

        string filename;
        string contentType;

        public DiskFileUpload(string name, string filename, string contentType, string transferEncoding, Encoding contentEncoding, long size)
            : base(name, contentEncoding, size)
        {
            Contract.Requires(filename != null);
            Contract.Requires(contentType != null);

            this.filename = filename;
            this.contentType = contentType;
            this.TransferEncoding = transferEncoding;
        }

        public override HttpDataType DataType => HttpDataType.FileUpload;

        public string FileName
        {
            get => this.filename;
            set
            {
                Contract.Requires(value != null);
                this.filename = value;
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
                throw new ArgumentException($"Cannot compare {this.DataType}  with {other.DataType}");
            }

            return this.CompareTo((IFileUpload)other);
        }

        public int CompareTo(IFileUpload other) => FileUploadUtil.CompareTo(this, other);

        public string ContentType
        {
            get => this.contentType;
            set
            {
                Contract.Requires(value != null);

                this.contentType = value;
            }
        }

        public string TransferEncoding { get; set; }

        public override string ToString()
        {
            FileStream fileStream = null;
            try
            {
                fileStream = this.GetFileStream();
            }
            catch (IOException)
            {
                // Should not occur.
            }

            return HttpHeaderNames.ContentDisposition  + ": " +
               HttpHeaderValues.FormData + "; " + HttpHeaderValues.Name + "=\"" + this.Name +
                "\"; " + HttpHeaderValues.FileName + "=\"" + this.filename + "\"\r\n" +
                HttpHeaderNames.ContentType + ": " + this.contentType +
                (this.ContentEncoding != null ? "; " + HttpHeaderValues.Charset + '=' + this.ContentEncoding.WebName + "\r\n" : "\r\n") +
                HttpHeaderNames.ContentLength + ": " + this.Length + "\r\n" +
                "Completed: " + this.Completed +
                "\r\nIsInMemory: " + this.InMemory + "\r\nRealFile: " +
                (fileStream != null ? fileStream.Name : "null") + " DefaultDeleteAfter: " +
                DeleteOnExitTemporaryFile;
        }

        protected override bool DeleteOnExit => DeleteOnExitTemporaryFile;

        protected override string BaseDirectory => FileBaseDirectory;

        protected override string DiskFilename => "upload";

        protected override string Prefix => FilePrefix;

        protected override string Postfix => FilePostfix;

        public override IByteBufferHolder Copy()
        {
            IByteBuffer content = this.Content;
            return this.Replace(content?.Copy());
        }

        public override IByteBufferHolder Duplicate()
        {
            IByteBuffer content = this.Content;
            return this.Replace(content?.Duplicate());
        }

        public override IHttpData Replace(IByteBuffer content)
        {
            var upload = new DiskFileUpload(
                this.Name, this.FileName, this.ContentType, this.TransferEncoding, this.ContentEncoding, this.Size);
            if (content != null)
            {
                try
                {
                    upload.SetContent(content);
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
