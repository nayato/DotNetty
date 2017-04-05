// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http.Multipart
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    public class DefaultHttpDataFactory : IHttpDataFactory
    {
        public static readonly long MinSize = 0x4000;
        public static readonly long MaxSize = -1;

        readonly bool useDisk;
        readonly bool checkSize;
        readonly long minSize;
        long maxSize = MaxSize;
        readonly Encoding contentEncoding = HttpConstants.DefaultEncoding;
        readonly ConcurrentDictionary<IHttpRequest, List<IHttpData>> requestFileDeleteMap = new ConcurrentDictionary<IHttpRequest, List<IHttpData>>();

        public DefaultHttpDataFactory()
        {
            this.useDisk = false;
            this.checkSize = true;
            this.minSize = MinSize;
        }

        public DefaultHttpDataFactory(Encoding contentEncoding) : this()
        {
            this.contentEncoding = contentEncoding;
        }

        public DefaultHttpDataFactory(bool useDisk)
        {
            this.useDisk = useDisk;
            this.checkSize = false;
        }

        public DefaultHttpDataFactory(bool useDisk, Encoding contentEncoding) : this(useDisk)
        {
            this.contentEncoding = contentEncoding;
        }

        public DefaultHttpDataFactory(long minSize)
        {
            this.useDisk = false;
            this.checkSize = true;
            this.minSize = minSize;
        }

        public DefaultHttpDataFactory(long minSize, Encoding contentEncoding) : this(minSize)
        {
            this.contentEncoding = contentEncoding;
        }

        public void SetMaxLimit(long max) => this.maxSize = max;

        List<IHttpData> GetList(IHttpRequest request) => this.requestFileDeleteMap.GetOrAdd(request, _ => new List<IHttpData>());

        public IAttribute CreateAttribute(IHttpRequest request, string name)
        {
            if (this.useDisk)
            {
                var diskAttribute = new DiskAttribute(name, this.contentEncoding);
                diskAttribute.MaxSize = this.maxSize;
                List<IHttpData> fileToDelete = this.GetList(request);
                fileToDelete.Add(diskAttribute);

                return diskAttribute;
            }

            if (this.checkSize)
            {
                var mixedAttribute = new MixedAttribute(name, this.minSize, this.contentEncoding);
                mixedAttribute.MaxSize = this.maxSize;
                List<IHttpData> fileToDelete = this.GetList(request);
                fileToDelete.Add(mixedAttribute);

                return mixedAttribute;
            }

            var attribute = new MemoryAttribute(name);
            attribute.MaxSize = this.maxSize;
            return attribute;
        }

        public IAttribute CreateAttribute(IHttpRequest request, string name, long definedSize)
        {
            if (this.useDisk)
            {
                var diskAttribute = new DiskAttribute(name, definedSize, this.contentEncoding);
                diskAttribute.MaxSize = this.maxSize;
                List<IHttpData> fileToDelete = this.GetList(request);
                fileToDelete.Add(diskAttribute);

                return diskAttribute;
            }

            if (this.checkSize)
            {
                var mixedAttribute = new MixedAttribute(name, definedSize, this.minSize, this.contentEncoding);
                mixedAttribute.MaxSize = this.maxSize;
                List<IHttpData> fileToDelete = this.GetList(request);
                fileToDelete.Add(mixedAttribute);

                return mixedAttribute;
            }

            var attribute = new MemoryAttribute(name, definedSize);
            attribute.MaxSize = this.maxSize;
            return attribute;
        }

        static void CheckHttpDataSize(IHttpData data)
        {
            try
            {
                data.CheckSize(data.Length);
            }
            catch (IOException)
            {
                throw new ArgumentException($"Attribute {data.DataType} bigger than maxSize allowed");
            }
        }

        public IAttribute CreateAttribute(IHttpRequest request, string name, string value)
        {
            if (this.useDisk)
            {
                IAttribute attribute;
                try
                {
                    attribute = new DiskAttribute(name, value, this.contentEncoding);
                    attribute.MaxSize = this.maxSize;
                }
                catch (IOException)
                {
                    // revert to Mixed mode
                    attribute = new MixedAttribute(name, value, this.minSize, this.contentEncoding);
                    attribute.MaxSize = this.maxSize;
                }

                CheckHttpDataSize(attribute);
                List<IHttpData> fileToDelete = this.GetList(request);
                fileToDelete.Add(attribute);
                return attribute;
            }

            if (this.checkSize)
            {
                var mixedAttribute = new MixedAttribute(name, value, this.minSize, this.contentEncoding);
                mixedAttribute.MaxSize = this.maxSize;
                CheckHttpDataSize(mixedAttribute);
                List<IHttpData> fileToDelete = this.GetList(request);
                fileToDelete.Add(mixedAttribute);
                return mixedAttribute;
            }

            try
            {
                var attribute = new MemoryAttribute(name, value, this.contentEncoding);
                attribute.MaxSize = this.maxSize;
                CheckHttpDataSize(attribute);
                return attribute;
            }
            catch (IOException e)
            {
                throw new ArgumentException($"{request}", e);
            }
        }

        public IFileUpload CreateFileUpload(IHttpRequest request, string name, string fileName, string contentType, string contentTransferEncoding, Encoding encoding, long size)
        {
            if (this.useDisk)
            {
                var fileUpload = new DiskFileUpload(
                    name, fileName, contentType, contentTransferEncoding, encoding, size)
                {
                    MaxSize = this.maxSize
                };
                CheckHttpDataSize(fileUpload);
                List<IHttpData> fileToDelete = this.GetList(request);
                fileToDelete.Add(fileUpload);

                return fileUpload;
            }

            if (this.checkSize)
            {
                var fileUpload = new MixedFileUpload(
                    name, fileName, contentType, contentTransferEncoding, encoding, size, this.minSize)
                {
                    MaxSize = this.maxSize
                };
                CheckHttpDataSize(fileUpload);
                List<IHttpData> fileToDelete = this.GetList(request);
                fileToDelete.Add(fileUpload);
                return fileUpload;
            }

            var memoryFileUpload = new MemoryFileUpload(
                name, fileName, contentType, contentTransferEncoding, encoding, size)
            {
                MaxSize = this.maxSize
            };

            CheckHttpDataSize(memoryFileUpload);

            return memoryFileUpload;
        }

        public void RemoveHttpDataFromClean(IHttpRequest request, IPostHttpData data)
        {
            var httpData = data as IHttpData;
            if (httpData == null)
            {
                return;
            }

            List<IHttpData> fileToDelete = this.GetList(request);
            fileToDelete.Remove(httpData);
        }

        public void CleanRequestHttpData(IHttpRequest request)
        {
            if (!this.requestFileDeleteMap.TryRemove(request, out List<IHttpData> fileToDelete))
            {
                return;
            }

            foreach (IHttpData httpData in fileToDelete)
            {
                httpData.Delete();
            }

            fileToDelete.Clear();
        }
    }
}
