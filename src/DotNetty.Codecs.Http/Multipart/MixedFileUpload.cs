// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http.Multipart
{
    using System.IO;
    using System.Text;
    using DotNetty.Buffers;
    using DotNetty.Common;

    public class MixedFileUpload : IFileUpload
    {
        readonly long limitSize;
        readonly long definedSize;

        IFileUpload fileUpload;
        long maxSize = DefaultHttpDataFactory.MaxSize;


        public MixedFileUpload(string name, string fileName, string contentType, 
            string transferEncoding, Encoding contentEncoding, long size, long limitSize)
        {
            this.limitSize = limitSize;
            if (size > this.limitSize)
            {
                this.fileUpload = new DiskFileUpload(name, fileName, contentType, 
                    transferEncoding, contentEncoding, size);
            }
            else
            {
                this.fileUpload = new MemoryFileUpload(name, fileName, contentType,
                        transferEncoding, contentEncoding, size);
            }

            this.definedSize = size;
        }

        public long MaxSize
        {
            get { return this.maxSize; }
            set
            {
                this.maxSize = value;
                this.fileUpload.MaxSize = value;
            }
        }

        public void CheckSize(long newSize)
        {
            if (this.maxSize >= 0 && newSize > this.maxSize)
            {
                throw new IOException($"{this.DataType} Size exceed allowed maximum capacity");
            }
        }

        public void AddContent(IByteBuffer buffer, bool last)
        {
            if (this.fileUpload is MemoryFileUpload)
            {
                this.CheckSize(this.fileUpload.Length + buffer.ReadableBytes);
                if (this.fileUpload.Length + buffer.ReadableBytes > this.limitSize)
                {
                    var diskFileUpload = new DiskFileUpload(this.fileUpload.Name, this.fileUpload.FileName, 
                        this.fileUpload.ContentType, 
                        this.fileUpload.TransferEncoding, this.fileUpload.ContentEncoding, this.definedSize);
                    diskFileUpload.MaxSize = this.maxSize;
                    IByteBuffer data = this.fileUpload.GetByteBuffer();
                    if (data != null && data.IsReadable())
                    {
                        diskFileUpload.AddContent((IByteBuffer)data.Retain(), false);
                    }
                    // release old upload
                    this.fileUpload.Release();
                    this.fileUpload = diskFileUpload;
                }
            }

            this.fileUpload.AddContent(buffer, last);
        }

        public void Delete() => this.fileUpload.Delete();

        public byte[] GetBytes() => this.fileUpload.GetBytes();

        public IByteBuffer GetByteBuffer() => this.fileUpload.GetByteBuffer();

        public Encoding ContentEncoding
        {
            get
            {
                return this.fileUpload.ContentEncoding;
            }
            set
            {
                this.fileUpload.ContentEncoding = value;
            }
        }

        public string ContentType
        {
            get
            {
                return this.fileUpload.ContentType;
            }
            set
            {
                this.fileUpload.ContentType = value;
            }
        }

        public string FileName
        {
            get
            {
                return this.fileUpload.FileName;
            }
            set
            {
                this.fileUpload.FileName = value;
            }
        }

        public string TransferEncoding
        {
            get
            {
                return this.fileUpload.TransferEncoding;
            }
            set
            {
                this.fileUpload.TransferEncoding = value;
            }
        }


        public HttpDataType DataType => this.fileUpload.DataType;

        public string GetString() => this.fileUpload.GetString();

        public string GetString(Encoding encoding) => this.fileUpload.GetString(encoding);

        public bool Completed => this.fileUpload.Completed;

        public bool InMemory => this.fileUpload.InMemory;

        public long Length => this.fileUpload.Length;

        public long DefinedLength => this.fileUpload.DefinedLength;

        public bool RenameTo(FileStream destination) => this.fileUpload.RenameTo(destination);

        public void SetContent(IByteBuffer buffer)
        {
            this.CheckSize(buffer.ReadableBytes);

            if (buffer.ReadableBytes > this.limitSize)
            {
                if (this.fileUpload is MemoryFileUpload)
                {
                    IFileUpload memoryUpload = this.fileUpload;

                    // change to Disk
                    this.fileUpload = new DiskFileUpload(memoryUpload.Name, memoryUpload.FileName, memoryUpload.ContentType,
                        memoryUpload.TransferEncoding, memoryUpload.ContentEncoding, this.definedSize);
                    this.fileUpload.MaxSize = this.maxSize;

                    // release old upload
                    memoryUpload.Release();
                }
            }

            this.fileUpload.SetContent(buffer);
        }

        public void SetContent(Stream source)
        {
            this.CheckSize(source.Length);
            if (source.Length > this.limitSize)
            {
                if (this.fileUpload is MemoryFileUpload) {
                    IFileUpload memoryUpload = this.fileUpload;

                    // change to Disk
                    this.fileUpload = new DiskFileUpload(memoryUpload.Name, memoryUpload.FileName, memoryUpload.ContentType, 
                        memoryUpload.TransferEncoding, memoryUpload.ContentEncoding, this.definedSize);
                    this.fileUpload.MaxSize = this.maxSize;

                    // release old upload
                    memoryUpload.Release();
                }
            }

            this.fileUpload.SetContent(source);
        }

        public string Name => this.fileUpload.Name;

        public override int GetHashCode() => this.fileUpload.GetHashCode();

        public override bool Equals(object obj) => this.fileUpload.Equals(obj);

        public int CompareTo(IPostHttpData other) => this.fileUpload.CompareTo(other);

        public override string ToString() => $"Mixed: {this.fileUpload}";

        public IByteBuffer GetChunk(int length) => this.fileUpload.GetChunk(length);

        public FileStream GetFileStream() => this.fileUpload.GetFileStream();

        public IByteBufferHolder Copy() => this.fileUpload.Copy();

        public IByteBufferHolder Duplicate() => this.fileUpload.Duplicate();

        public IHttpData Replace(IByteBuffer content) => this.fileUpload.Replace(content);

        public IByteBuffer Content => this.fileUpload.Content;

        public int ReferenceCount => this.fileUpload.ReferenceCount;

        public IReferenceCounted Retain()
        {
            this.fileUpload.Retain();
            return this;
        }

        public IReferenceCounted Retain(int increment)
        {
            this.fileUpload.Retain(increment);
            return this;
        }

        public IReferenceCounted Touch()
        {
            this.fileUpload.Touch();
            return this;
        }

        public IReferenceCounted Touch(object hint)
        {
            this.fileUpload.Touch(hint);
            return this;
        }

        public bool Release() => this.fileUpload.Release();

        public bool Release(int decrement) => this.fileUpload.Release(decrement);
    }
}
