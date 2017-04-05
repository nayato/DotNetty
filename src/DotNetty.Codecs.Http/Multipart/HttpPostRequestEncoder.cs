// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http.Multipart
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Text;
    using DotNetty.Buffers;
    using DotNetty.Common;
    using DotNetty.Common.Internal;
    using DotNetty.Common.Utilities;
    using DotNetty.Handlers.Streams;

    public class HttpPostRequestEncoder : IChunkedInput<IHttpContent>
    {
        public enum EncoderMode
        {
            // Legacy mode which should work for most. It is known to not work with OAUTH. For OAUTH use
            // {@link EncoderMode#RFC3986}. The W3C form recommendations this for submitting post form data.
            RFC1738,

            // Mode which is more new and is used for OAUTH
            RFC3986,

            // The HTML5 spec disallows mixed mode in multipart/form-data
            // requests. More concretely this means that more files submitted
            // under the same name will not be encoded using mixed mode, but
            // will be treated as distinct fields.
            // Reference: http://www.w3.org/TR/html5/forms.html#multipart-form-data
            HTML5
        }

        readonly IHttpDataFactory factory;
        readonly IHttpRequest request;
        readonly Encoding encoding;
        readonly List<IHttpData> bodyList;
        bool headerFinalized;
        readonly EncoderMode encoderMode;

        // Does the last non empty chunk already encoded so that next chunk will be empty (last chunk)
        bool isLastChunk;

        // Last chunk already sent
        bool isLastChunkSent;

        // The current FileUpload that is currently in encode process
        IFileUpload currentFileUpload;

        //* While adding a FileUpload, is the multipart currently in Mixed Mode
        bool duringMixedMode;

        // Global Body size
        long globalBodySize;

        public HttpPostRequestEncoder(IHttpRequest request, bool multipart)
            : this(new DefaultHttpDataFactory(DefaultHttpDataFactory.MinSize),
                request, multipart, HttpConstants.DefaultEncoding, EncoderMode.RFC1738)
        {
        }

        public HttpPostRequestEncoder(IHttpDataFactory factory, IHttpRequest request, bool multipart)
            : this(factory, request, multipart, HttpConstants.DefaultEncoding, EncoderMode.RFC1738)
        {
        }

        public HttpPostRequestEncoder(IHttpDataFactory factory, IHttpRequest request, bool multipart,
            Encoding encoding, EncoderMode encoderMode)
        {
            Contract.Requires(factory != null);
            Contract.Requires(request != null);
            Contract.Requires(encoding != null);

            HttpMethod method = request.Method;
            if (method.Equals(HttpMethod.Trace))
            {
                throw new ErrorDataEncoderException("Cannot create a Encoder if request is a TRACE");
            }

            this.factory = factory;
            this.request = request;
            this.encoding = encoding;

            // Fill default values
            this.bodyList = new List<IHttpData>();

            // default mode
            this.isLastChunk = false;
            this.isLastChunkSent = false;
            this.IsMultipart = multipart;
            this.MultipartList = new LinkedList<IPostHttpData>();
            this.encoderMode = encoderMode;
            if (this.IsMultipart)
            {
                this.InitDataMultipart();
            }
        }

        internal string MultipartDataBoundary { get; private set; }

        internal string MultipartMixedBoundary { get; private set; }

        internal LinkedList<IPostHttpData> MultipartList { get; }

        public void CleanFiles() => this.factory.CleanRequestHttpData(this.request);

        public bool IsMultipart { get; }

        void InitDataMultipart() => this.MultipartDataBoundary = GetNewMultipartDelimiter();

        void InitMixedMultipart() => this.MultipartMixedBoundary = GetNewMultipartDelimiter();

        // construct a generated delimiter
        static string GetNewMultipartDelimiter() => Convert.ToString(PlatformDependent.ThreadLocalRandom.NextLong(), 16).ToLower();

        public List<IHttpData> GetBodyListAttributes() => this.bodyList;

        public void SetBodyHttpDataList(List<IHttpData> list)
        {
            Contract.Requires(list != null);

            this.globalBodySize = 0;
            this.bodyList.Clear();
            this.currentFileUpload = null;
            this.duringMixedMode = false;
            this.MultipartList.Clear();

            foreach (IHttpData data in list)
            {
                this.AddBodyHttpData(data);
            }
        }

        public void AddBodyAttribute(string name, string value)
        {
            Contract.Requires(name != null);
            IAttribute data = this.factory.CreateAttribute(this.request, name, value ?? "");
            this.AddBodyHttpData(data);
        }

        public void AddBodyFileUpload(string name, FileStream fileStream, string contentType, bool isText)
        {
            Contract.Requires(name != null);
            Contract.Requires(fileStream != null);

            string fileName = Path.GetFileName(fileStream.Name);
            this.AddBodyFileUpload(name, fileName, fileStream, contentType, isText);
        }

        public void AddBodyFileUpload(string name, string fileName, FileStream fileStream, string contentType, bool isText)
        {
            Contract.Requires(name != null);
            Contract.Requires(fileStream != null);

            string scontentType = contentType;
            string contentTransferEncoding = null;
            if (contentType == null)
            {
                scontentType = isText
                    ? HttpPostBodyUtil.DefaultTextContentType
                    : HttpPostBodyUtil.DefaultBinaryContentType;
            }

            if (!isText)
            {
                contentTransferEncoding = TransferEncodingMechanism.Binary.Value;
            }

            IFileUpload fileUpload = this.factory.CreateFileUpload(
                this.request, name, fileName, scontentType, contentTransferEncoding, null, fileStream.Length);
            try
            {
                fileUpload.SetContent(fileStream);
            }
            catch (IOException e)
            {
                throw new ErrorDataEncoderException(e);
            }

            this.AddBodyHttpData(fileUpload);
        }

        public void AddBodyFileUploads(string name, params Tuple<FileStream, string, bool>[] uploads)
        {
            foreach (Tuple<FileStream, string, bool> upload in uploads)
            {
                this.AddBodyFileUpload(name, upload.Item1, upload.Item2, upload.Item3);
            }
        }

        public void AddBodyHttpData(IHttpData data)
        {
            Contract.Requires(data != null);

            if (this.headerFinalized)
            {
                throw new ErrorDataEncoderException("Cannot add value once finalized");
            }

            this.bodyList.Add(data);

            if (!this.IsMultipart)
            {
                if (data is IAttribute)
                {
                    var attribute = (IAttribute)data;
                    try
                    {
                        // name=value& with encoded name and attribute
                        string key = this.EncodeAttribute(attribute.Name, this.encoding);
                        string value = this.EncodeAttribute(attribute.Value, this.encoding);
                        IAttribute newattribute = this.factory.CreateAttribute(this.request, key, value);
                        this.MultipartList.AddLast(newattribute);
                        this.globalBodySize += newattribute.Name.Length + 1 + newattribute.Length + 1;
                    }
                    catch (IOException e)
                    {
                        throw new ErrorDataEncoderException(e);
                    }
                }
                else if (data is IFileUpload)
                {
                    // since not Multipart, only name=filename => Attribute
                    var fileUpload = (IFileUpload)data;
                    // name=filename& with encoded name and filename
                    string key = this.EncodeAttribute(fileUpload.Name, this.encoding);
                    string value = this.EncodeAttribute(fileUpload.FileName, this.encoding);
                    IAttribute newattribute = this.factory.CreateAttribute(this.request, key, value);
                    this.MultipartList.AddLast(newattribute);
                    this.globalBodySize += newattribute.Name.Length + 1 + newattribute.Length + 1;
                }

                return;
            }

            //  Logic:
            //  if not Attribute:
            //       add Data to body list
            //       if (duringMixedMode)
            //           add endmixedmultipart delimiter
            //           currentFileUpload = null
            //           duringMixedMode = false;
            //       add multipart delimiter, multipart body header and Data to multipart list
            //       reset currentFileUpload, duringMixedMode
            //  if FileUpload: take care of multiple file for one field => mixed mode
            //       if (duringMixedMode)
            //           if (currentFileUpload.name == data.name)
            //               add mixedmultipart delimiter, mixedmultipart body header and Data to multipart list
            //           else
            //               add endmixedmultipart delimiter, multipart body header and Data to multipart list
            //               currentFileUpload = data
            //               duringMixedMode = false;
            //       else
            //           if (currentFileUpload.name == data.name)
            //               change multipart body header of previous file into multipart list to
            //                       mixedmultipart start, mixedmultipart body header
            //               add mixedmultipart delimiter, mixedmultipart body header and Data to multipart list
            //               duringMixedMode = true
            //           else
            //               add multipart delimiter, multipart body header and Data to multipart list
            //               currentFileUpload = data
            //               duringMixedMode = false;
            //  Do not add last delimiter! Could be:
            //  if duringmixedmode: endmixedmultipart + endmultipart
            //  else only endmultipart
            // 
            if (data is IAttribute)
            {
                if (this.duringMixedMode)
                {
                    var internalAttribute = new InternalAttribute(this.encoding);
                    internalAttribute.AddValue($"\r\n--{this.MultipartMixedBoundary}--");
                    this.MultipartList.AddLast(internalAttribute);
                    this.MultipartMixedBoundary = null;
                    this.currentFileUpload = null;
                    this.duringMixedMode = false;
                }

                var newAttribute = new InternalAttribute(this.encoding);
                if (this.MultipartList.Count > 0)
                {
                    // previously a data field so CRLF
                    newAttribute.AddValue("\r\n");
                }
                newAttribute.AddValue($"--{this.MultipartDataBoundary}\r\n");
                // content-disposition: form-data; name="field1"
                var attribute = (IAttribute)data;
                newAttribute.AddValue($"{HttpHeaderNames.ContentDisposition}: {HttpHeaderValues.FormData}; {HttpHeaderValues.Name}=\"{attribute.Name}\"\r\n");
                // Add Content-Length: xxx
                newAttribute.AddValue($"{HttpHeaderNames.ContentLength}: {attribute.Length}\r\n");
                Encoding contentEncoding = attribute.ContentEncoding;
                if (contentEncoding != null)
                {
                    // Content-Type: text/plain; charset=charset
                    newAttribute.AddValue($"{HttpHeaderNames.ContentType}: {HttpPostBodyUtil.DefaultTextContentType}; {HttpHeaderValues.Charset}={contentEncoding.WebName}\r\n");
                }

                // CRLF between body header and data
                newAttribute.AddValue("\r\n");
                this.MultipartList.AddLast(newAttribute);
                this.MultipartList.AddLast(data);
                this.globalBodySize += attribute.Length + newAttribute.Size;
            }
            else if (data is IFileUpload)
            {
                var fileUpload = (IFileUpload)data;
                var internalAttribute = new InternalAttribute(this.encoding);
                if (this.MultipartList.Count > 0)
                {
                    // previously a data field so CRLF
                    internalAttribute.AddValue("\r\n");
                }

                bool localMixed;
                if (this.duringMixedMode)
                {
                    if (this.currentFileUpload != null
                        && this.currentFileUpload.Name.Equals(fileUpload.Name))
                    {
                        // continue a mixed mode
                        localMixed = true;
                    }
                    else
                    {
                        // end a mixed mode

                        // add endmixedmultipart delimiter, multipart body header
                        // and
                        // Data to multipart list
                        internalAttribute.AddValue($"--{this.MultipartMixedBoundary}--");
                        this.MultipartList.AddLast(internalAttribute);
                        this.MultipartMixedBoundary = null;
                        // start a new one (could be replaced if mixed start again
                        // from here
                        internalAttribute = new InternalAttribute(this.encoding);
                        internalAttribute.AddValue("\r\n");
                        localMixed = false;
                        // new currentFileUpload and no more in Mixed mode
                        this.currentFileUpload = fileUpload;
                        this.duringMixedMode = false;
                    }
                }
                else
                {
                    if (this.encoderMode != EncoderMode.HTML5
                        && this.currentFileUpload != null
                        && this.currentFileUpload.Name.Equals(fileUpload.Name))
                    {
                        // create a new mixed mode (from previous file)

                        // change multipart body header of previous file into
                        // multipart list to
                        // mixedmultipart start, mixedmultipart body header

                        // change Internal (size()-2 position in multipartHttpDatas)
                        // from (line starting with *)
                        // --AaB03x
                        // * Content-Disposition: form-data; name="files";
                        // filename="file1.txt"
                        // Content-Type: text/plain
                        // to (lines starting with *)
                        // --AaB03x
                        // * Content-Disposition: form-data; name="files"
                        // * Content-Type: multipart/mixed; boundary=BbC04y
                        // *
                        // * --BbC04y
                        // * Content-Disposition: attachment; filename="file1.txt"
                        // Content-Type: text/plain

                        this.InitMixedMultipart();

                        LinkedListNode<IPostHttpData> pastValue = this.MultipartList.Last?.Previous;
                        Contract.Assert(pastValue != null);

                        var pastAttribute = (InternalAttribute)pastValue.Value;
                        // remove past size
                        this.globalBodySize -= pastAttribute.Size;

                        StringBuilder replacement = new StringBuilder(139
                                + this.MultipartDataBoundary.Length
                                + this.MultipartMixedBoundary.Length * 2
                                + fileUpload.FileName.Length
                                + fileUpload.Name.Length)
                            .Append("--")
                            .Append(this.MultipartDataBoundary)
                            .Append("\r\n")
                            .Append(HttpHeaderNames.ContentDisposition)
                            .Append(": ")
                            .Append(HttpHeaderValues.FormData)
                            .Append("; ")
                            .Append(HttpHeaderValues.Name)
                            .Append("=\"")
                            .Append(fileUpload.Name)
                            .Append("\"\r\n")

                            .Append(HttpHeaderNames.ContentType)
                            .Append(": ")
                            .Append(HttpHeaderValues.MultipartMixed)
                            .Append("; ")
                            .Append(HttpHeaderValues.Boundary)
                            .Append('=')
                            .Append(this.MultipartMixedBoundary)
                            .Append("\r\n\r\n")

                            .Append("--")
                            .Append(this.MultipartMixedBoundary)
                            .Append("\r\n")

                            .Append(HttpHeaderNames.ContentDisposition)
                            .Append(": ")
                            .Append(HttpHeaderValues.Attachment);

                        if (fileUpload.FileName.Length > 0)
                        {
                            replacement.Append("; ")
                                .Append(HttpHeaderValues.FileName)
                                .Append("=\"")
                                .Append(fileUpload.FileName)
                                .Append('"');
                        }

                        replacement.Append("\r\n");

                        pastAttribute.SetValue(replacement.ToString(), 1);
                        pastAttribute.SetValue("", 2);

                        // update past size
                        this.globalBodySize += pastAttribute.Size;

                        // now continue
                        // add mixedmultipart delimiter, mixedmultipart body header
                        // and
                        // Data to multipart list
                        localMixed = true;
                        this.duringMixedMode = true;
                    }
                    else
                    {
                        // a simple new multipart
                        // add multipart delimiter, multipart body header and Data
                        // to multipart list
                        localMixed = false;
                        this.currentFileUpload = fileUpload;
                        this.duringMixedMode = false;
                    }
                }

                if (localMixed)
                {
                    // add mixedmultipart delimiter, mixedmultipart body header and
                    // Data to multipart list
                    internalAttribute.AddValue($"--{this.MultipartMixedBoundary}\r\n");
                    if (fileUpload.FileName.Length == 0)
                    {
                        // Content-Disposition: attachment
                        internalAttribute.AddValue($"{HttpHeaderNames.ContentDisposition}: {HttpHeaderValues.Attachment}\r\n");
                    }
                    else
                    {
                        // Content-Disposition: attachment; filename="file1.txt"
                        internalAttribute.AddValue($"{HttpHeaderNames.ContentDisposition}: {HttpHeaderValues.Attachment}; {HttpHeaderValues.FileName}=\"{fileUpload.FileName}\"\r\n");
                    }
                }
                else
                {
                    internalAttribute.AddValue($"--{this.MultipartDataBoundary}\r\n");
                    if (fileUpload.FileName.Length == 0)
                    {
                        // Content-Disposition: form-data; name="files";
                        internalAttribute.AddValue($"{HttpHeaderNames.ContentDisposition}: {HttpHeaderValues.FormData}; {HttpHeaderValues.Name}=\"{fileUpload.Name}\"\r\n");
                    }
                    else
                    {
                        // Content-Disposition: form-data; name="files";
                        // filename="file1.txt"
                        internalAttribute.AddValue($"{HttpHeaderNames.ContentDisposition}: {HttpHeaderValues.FormData}; {HttpHeaderValues.Name}=\"{fileUpload.Name}\"; {HttpHeaderValues.FileName}=\"{fileUpload.FileName}\"\r\n");
                    }
                }

                // Add Content-Length: xxx
                internalAttribute.AddValue($"{HttpHeaderNames.ContentLength}: {fileUpload.Length}\r\n");

                // Content-Type: image/gif
                // Content-Type: text/plain; charset=ISO-8859-1
                // Content-Transfer-Encoding: binary
                internalAttribute.AddValue($"{HttpHeaderNames.ContentType}: {fileUpload.ContentType}");
                string contentTransferEncoding = fileUpload.TransferEncoding;
                if (contentTransferEncoding != null
                    && contentTransferEncoding.Equals(TransferEncodingMechanism.Binary.Value))
                {
                    internalAttribute.AddValue($"\r\n{HttpHeaderNames.ContentTransferEncoding}: {TransferEncodingMechanism.Binary.Value}\r\n\r\n");
                }
                else if (fileUpload.ContentEncoding != null)
                {
                    internalAttribute.AddValue($"; {HttpHeaderValues.Charset}={fileUpload.ContentEncoding.WebName}\r\n\r\n");
                }
                else
                {
                    internalAttribute.AddValue("\r\n\r\n");
                }
                this.MultipartList.AddLast(internalAttribute);
                this.MultipartList.AddLast(data);
                this.globalBodySize += fileUpload.Length + internalAttribute.Size;
            }
        }

        public IHttpRequest FinalizeRequest()
        {
            // Finalize the multipartHttpDatas
            if (!this.headerFinalized)
            {
                if (this.IsMultipart)
                {
                    var attribute = new InternalAttribute(this.encoding);
                    if (this.duringMixedMode)
                    {
                        attribute.AddValue($"\r\n--{this.MultipartMixedBoundary}--");
                    }

                    attribute.AddValue($"\r\n--{this.MultipartDataBoundary}--\r\n");
                    this.MultipartList.AddLast(attribute);
                    this.MultipartMixedBoundary = null;
                    this.currentFileUpload = null;
                    this.duringMixedMode = false;
                    this.globalBodySize += attribute.Size;
                }

                this.headerFinalized = true;
            }
            else
            {
                throw new ErrorDataEncoderException("Header already encoded");
            }

            HttpHeaders headers = this.request.Headers;
            IList<ICharSequence> contentTypes = headers.GetAll(HttpHeaderNames.ContentType);
            IList<ICharSequence> transferEncoding = headers.GetAll(HttpHeaderNames.TransferEncoding);
            if (contentTypes != null)
            {
                headers.Remove(HttpHeaderNames.ContentType);
                foreach (ICharSequence contentType in contentTypes)
                {
                    // "multipart/form-data; boundary=--89421926422648"
                    string lowercased = contentType.ToString().ToLower();
                    if (lowercased.StartsWith(HttpHeaderValues.MultipartFormData.ToString()) 
                        || lowercased.StartsWith(HttpHeaderValues.ApplicationXWwwFormUrlencoded.ToString()))
                    {
                        // ignore
                    }
                    else
                    {
                        headers.Add(HttpHeaderNames.ContentType, contentType);
                    }
                }
            }
            if (this.IsMultipart)
            {
                string value = $"{HttpHeaderValues.MultipartFormData}; {HttpHeaderValues.Boundary}={this.MultipartDataBoundary}";
                headers.Add(HttpHeaderNames.ContentType, value);
            }
            else
            {
                // Not multipart
                headers.Add(HttpHeaderNames.ContentType, HttpHeaderValues.ApplicationXWwwFormUrlencoded);
            }

            // Now consider size for chunk or not
            long realSize = this.globalBodySize;
            if (!this.IsMultipart)
            { 
                realSize -= 1; // last '&' removed
            }

            headers.Set(HttpHeaderNames.ContentLength, Convert.ToString(realSize));
            if (realSize > HttpPostBodyUtil.ChunkSize || this.IsMultipart)
            {
                this.IsChunked = true;
                if (transferEncoding != null)
                {
                    headers.Remove(HttpHeaderNames.TransferEncoding);
                    foreach (ICharSequence v in transferEncoding)
                    {
                        if (HttpHeaderValues.Chunked.ContentEqualsIgnoreCase(v))
                        {
                            // ignore
                        }
                        else
                        {
                            headers.Add(HttpHeaderNames.TransferEncoding, v);
                        }
                    }
                }
                HttpUtil.SetTransferEncodingChunked(this.request, true);

                // wrap to hide the possible content
                return new WrappedHttpRequest(this.request);
            }
            else
            {
                // get the only one body and set it to the request
                IHttpContent chunk = this.NextChunk();
                var fullRequest = this.request as IFullHttpRequest;
                if (fullRequest != null)
                {
                    IByteBuffer chunkContent = chunk.Content;
                    if (!ReferenceEquals(fullRequest.Content, chunkContent))
                    {
                        fullRequest.Content.Clear();
                        fullRequest.Content.WriteBytes(chunkContent);

                        chunkContent.Release();
                    }

                    return fullRequest;
                }

                return new WrappedFullHttpRequest(this.request, chunk);
            }
        }

        public bool IsChunked { get; private set; }

        string EncodeAttribute(string value, Encoding stringEncoding)
        {
            if (value == null)
            {
                return string.Empty;
            }
            var buf = new StringBuilder();

            int count = stringEncoding.GetMaxByteCount(1);
            var bytes = new byte[count];
            var array = new char[1];

            foreach (char ch in value)
            {
                if (ch >= 'a' && ch <= 'z'
                    || ch >= 'A' && ch <= 'Z'
                    || ch >= '0' && ch <= '9')
                {
                    buf.Append(ch);
                }
                else
                {
                    if (this.encoderMode == EncoderMode.RFC3986)
                    {
                        string replace = null;
                        if (ch == '*')
                        {
                            replace = "%2A";
                        }
                        else if (ch == '+')
                        {
                            replace = "%20";
                        }
                        else if (ch == '~')
                        {
                            replace = "%7E";
                        }

                        if (replace != null)
                        {
                            buf.Append(replace);
                            continue;
                        }
                    }

                    array[0] = ch;
                    count = stringEncoding.GetBytes(array, 0, 1, bytes, 0);
                    for (int i = 0; i < count; i++)
                    {
                        buf.Append('%');
                        buf.Append(CharUtil.Digits[(bytes[i] & 0xf0) >> 4]);
                        buf.Append(CharUtil.Digits[bytes[i] & 0xf]);
                    }
                }
            }

            return buf.ToString();
        }

        // The ByteBuf currently used by the encoder
        IByteBuffer currentBuffer;

        // The current InterfaceHttpData to encode (used if more chunks are available)
        IPostHttpData currentData;

        // If not multipart, does the currentBuffer stands for the Key or for the Value
        bool isKey = true;

        IByteBuffer FillByteBuffer()
        {
            int length = this.currentBuffer.ReadableBytes;
            if (length > HttpPostBodyUtil.ChunkSize)
            {
                var buffer = (IByteBuffer)this.currentBuffer
                    .Slice(this.currentBuffer.ReaderIndex, HttpPostBodyUtil.ChunkSize)
                    .Retain();
                this.currentBuffer.SetReaderIndex(this.currentBuffer.ReaderIndex + HttpPostBodyUtil.ChunkSize);
                
                return (IByteBuffer)buffer.Retain();
            }
            else
            {
                // to continue
                IByteBuffer slice = this.currentBuffer;
                this.currentBuffer = null;

                return slice;
            }
        }

        // From the current context(currentBuffer and currentData), returns the next 
        // HttpChunk(if possible) trying to get sizeleft bytes more into the currentBuffer.
        // This is the Multipart version.
        IHttpContent EncodeNextChunkMultipart(int sizeleft)
        {
            if (this.currentData == null)
            {
                return null;
            }

            IByteBuffer buffer;
            if (this.currentData is InternalAttribute)
            {
                buffer = ((InternalAttribute)this.currentData).ToByteBuffer();
                this.currentData = null;
            }
            else
            {
                if (this.currentData is IAttribute)
                {
                    try
                    {
                        buffer = ((IAttribute)this.currentData).GetChunk(sizeleft);
                    }
                    catch (IOException e)
                    {
                        throw new ErrorDataEncoderException(e);
                    }
                }
                else
                {
                    try
                    {
                        buffer = ((IHttpData)this.currentData).GetChunk(sizeleft);
                    }
                    catch (IOException e)
                    {
                        throw new ErrorDataEncoderException(e);
                    }
                }
                if (buffer.Capacity == 0)
                {
                    // end for current InterfaceHttpData, need more data
                    this.currentData = null;
                    return null;
                }
            }

            this.currentBuffer = this.currentBuffer == null 
                ? buffer 
                : Unpooled.WrappedBuffer(this.currentBuffer, buffer);

            if (this.currentBuffer.ReadableBytes < HttpPostBodyUtil.ChunkSize)
            {
                this.currentData = null;
                return null;
            }

            buffer = this.FillByteBuffer();
            return new DefaultHttpContent(buffer);
        }

        // From the current context(currentBuffer and currentData), returns the next HttpChunk(if possible)
        // trying to get* sizeleft bytes more into the currentBuffer.This is the UrlEncoded version.
        IHttpContent EncodeNextChunkUrlEncoded(int sizeleft)
        {
            if (this.currentData == null)
            {
                return null;
            }

            int size = sizeleft;
            IByteBuffer buffer;

            // Set name=
            if (this.isKey)
            {
                string key = this.currentData.Name;
                buffer = Unpooled.WrappedBuffer(Encoding.UTF8.GetBytes(key));
                this.isKey = false;
                if (this.currentBuffer == null)
                {
                    this.currentBuffer = Unpooled.WrappedBuffer(
                        buffer,  Unpooled.WrappedBuffer(Encoding.UTF8.GetBytes("=")));
                    // continue
                    size -= buffer.ReadableBytes + 1;
                }
                else
                {
                    this.currentBuffer = Unpooled.WrappedBuffer(this.currentBuffer, 
                        buffer, Unpooled.WrappedBuffer(Encoding.UTF8.GetBytes("=")));
                    // continue
                    size -= buffer.ReadableBytes + 1;
                }
                if (this.currentBuffer.ReadableBytes >= HttpPostBodyUtil.ChunkSize)
                {
                    buffer = this.FillByteBuffer();
                    return new DefaultHttpContent(buffer);
                }
            }

            // Put value into buffer
            try
            {
                buffer = ((IHttpData)this.currentData).GetChunk(size);
            }
            catch (IOException e)
            {
                throw new ErrorDataEncoderException(e);
            }

            // Figure out delimiter
            IByteBuffer delimiter = null;
            if (buffer.ReadableBytes < size)
            {
                this.isKey = true;
                delimiter = this.MultipartList.First != null 
                    ? Unpooled.WrappedBuffer(Encoding.UTF8.GetBytes("&")) 
                    : null;
            }

            // End for current InterfaceHttpData, need potentially more data
            if (buffer.Capacity == 0)
            {
                this.currentData = null;
                if (this.currentBuffer == null)
                {
                    this.currentBuffer = delimiter;
                }
                else
                {
                    if (delimiter != null)
                    {
                        this.currentBuffer = Unpooled.WrappedBuffer(this.currentBuffer, delimiter);
                    }
                }

                if (this.currentBuffer != null 
                    && this.currentBuffer.ReadableBytes >= HttpPostBodyUtil.ChunkSize)
                {
                    buffer = this.FillByteBuffer();
                    return new DefaultHttpContent(buffer);
                }

                return null;
            }

            // Put it all together: name=value&
            if (this.currentBuffer == null)
            {
                this.currentBuffer = delimiter != null 
                    ? Unpooled.WrappedBuffer(buffer, delimiter) 
                    : buffer;
            }
            else
            {
                this.currentBuffer = delimiter != null 
                    ? Unpooled.WrappedBuffer(this.currentBuffer, buffer, delimiter) 
                    : Unpooled.WrappedBuffer(this.currentBuffer, buffer);
            }

            // end for current InterfaceHttpData, need more data
            if (this.currentBuffer.ReadableBytes < HttpPostBodyUtil.ChunkSize)
            {
                this.currentData = null;
                this.isKey = true;
                return null;
            }

            buffer = this.FillByteBuffer();
            return new DefaultHttpContent(buffer);
        }

        public void Close()
        {
            //NOP
        }

        public IHttpContent ReadChunk(IByteBufferAllocator allocator)
        {
            if (this.isLastChunkSent)
            {
                return null;
            }

            IHttpContent nextChunk = this.NextChunk();
            this.Progress += nextChunk.Content.ReadableBytes;
            return nextChunk;
        }

        IHttpContent NextChunk()
        {
            if (this.isLastChunk)
            {
                this.isLastChunkSent = true;
                return EmptyLastHttpContent.Default;
            }

            IByteBuffer buffer;
            int size = HttpPostBodyUtil.ChunkSize;
            // first test if previous buffer is not empty
            if (this.currentBuffer != null)
            {
                size -= this.currentBuffer.ReadableBytes;
            }
            if (size <= 0)
            {
                // NextChunk from buffer
                buffer = this.FillByteBuffer();
                return new DefaultHttpContent(buffer);
            }
            // size > 0
            if (this.currentData != null)
            {
                // continue to read data
                if (this.IsMultipart)
                {
                    IHttpContent chunk = this.EncodeNextChunkMultipart(size);
                    if (chunk != null)
                    {
                        return chunk;
                    }
                }
                else
                {
                    IHttpContent chunk = this.EncodeNextChunkUrlEncoded(size);
                    if (chunk != null)
                    {
                        // NextChunk Url from currentData
                        return chunk;
                    }
                }
                size = HttpPostBodyUtil.ChunkSize - this.currentBuffer.ReadableBytes;
            }
            if (this.MultipartList.Count < 2)
            {
                this.isLastChunk = true;
                // NextChunk as last non empty from buffer
                buffer = this.currentBuffer;
                this.currentBuffer = null;
                return new DefaultHttpContent(buffer);
            }
            while (size > 0 && this.MultipartList.First != null)
            {
                this.currentData = this.MultipartList.First.Value;
                this.MultipartList.RemoveFirst();

                IHttpContent chunk;
                if (this.IsMultipart)
                {
                    chunk = this.EncodeNextChunkMultipart(size);
                }
                else
                {
                    chunk = this.EncodeNextChunkUrlEncoded(size);
                }
                if (chunk == null)
                {
                    // not enough
                    size = HttpPostBodyUtil.ChunkSize - this.currentBuffer.ReadableBytes;
                    continue;
                }
                // NextChunk from data
                return chunk;
            }
            // end since no more data
            this.isLastChunk = true;
            if (this.currentBuffer == null)
            {
                this.isLastChunkSent = true;
                // LastChunk with no more data
                return EmptyLastHttpContent.Default;
            }
            // Previous LastChunk with no more data
            buffer = this.currentBuffer;
            this.currentBuffer = null;

            return new DefaultHttpContent(buffer);
        }

        public bool IsEndOfInput => this.isLastChunkSent;

        public long Length => this.IsMultipart? this.globalBodySize : this.globalBodySize - 1;

        // Global Transfer progress
        public long Progress { get; private set; }

        class WrappedHttpRequest : IHttpRequest
        {
            readonly IHttpRequest request;

            internal WrappedHttpRequest(IHttpRequest request)
            {
                this.request = request;
            }

            public DecoderResult Result
            {
                get
                {
                    return this.request.Result;
                }
                set
                {
                    this.request.Result = value;
                }
            }

            public HttpVersion ProtocolVersion
            {
                get
                {
                    return this.request.ProtocolVersion;
                }
                set
                {
                    this.request.ProtocolVersion = value;
                }
            }

            public HttpHeaders Headers => this.request.Headers;

            public HttpMethod Method
            {
                get
                {
                    return this.request.Method;
                }
                set
                {
                    this.request.Method = value;
                }
            }

            public string Uri
            {
                get
                {
                    return this.request.Uri;
                }
                set
                {
                    this.request.Uri = value;
                }
            }
        }

        sealed class WrappedFullHttpRequest : WrappedHttpRequest, IFullHttpRequest
        {
            readonly IHttpContent content;

            public WrappedFullHttpRequest(IHttpRequest request, IHttpContent content)
                : base(request)
            {
                this.content = content;
            }

            public int ReferenceCount => this.content.ReferenceCount;

            public IReferenceCounted Retain()
            {
                this.content.Retain();
                return this;
            }

            public IReferenceCounted Retain(int increment)
            {
                this.content.Retain(increment);
                return this;
            }

            public IReferenceCounted Touch()
            {
                this.content.Touch();
                return this;
            }

            public IReferenceCounted Touch(object hint)
            {
                this.content.Touch(hint);
                return this;
            }

            public bool Release() => this.content.Release();

            public bool Release(int decrement) => this.content.Release(decrement);

            public IByteBuffer Content => this.content.Content;

            public IByteBufferHolder Copy() => this.Replace(this.Content.Copy());

            public IByteBufferHolder Duplicate() => this.Replace(this.Content.Duplicate());

            public HttpHeaders TrailingHeaders
            {
                get
                {
                    var httpContent = this.content as ILastHttpContent;
                    return httpContent != null ? httpContent.TrailingHeaders : EmptyHttpHeaders.Default;
                }
            }

            public IFullHttpRequest Replace(IByteBuffer newContent)
            {
                var duplicate = new DefaultFullHttpRequest(this.ProtocolVersion, this.Method, this.Uri, newContent);
                duplicate.Headers.Set(this.Headers);
                duplicate.TrailingHeaders.Set(this.TrailingHeaders);
                return duplicate;
            }
        }
    }
}
