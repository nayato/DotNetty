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
    using DotNetty.Common.Utilities;

    public class HttpPostMultipartRequestDecoder : IHttpPostRequestDecoder
    {
        const string DelimiterSeperator = "--";
        static readonly AsciiString DelimiterSeperatorString = new AsciiString(DelimiterSeperator);

        readonly IHttpDataFactory factory;
        readonly IHttpRequest request;
        readonly List<IPostHttpData> bodyList = new List<IPostHttpData>();

        readonly Dictionary<AsciiString, List<IPostHttpData>> bodyMapHttpData =
            new Dictionary<AsciiString, List<IPostHttpData>>(CaseIgnoringComparator.Default);

        Encoding encoding;
        bool isLastChunk;
        IByteBuffer undecodedChunk;
        int bodyListHttpDataRank;
        ICharSequence multipartDataBoundary;
        ICharSequence multipartMixedBoundary;
        MultiPartStatus currentStatus = MultiPartStatus.Notstarted;
        Dictionary<ICharSequence, IAttribute> currentFieldAttributes;
        IFileUpload currentFileUpload;
        IAttribute currentAttribute;
        bool destroyed;
        int discardThreshold = HttpPostRequestDecoder.DefaultDiscardThreshold;

        public HttpPostMultipartRequestDecoder(IHttpDataFactory factory, IHttpRequest request, Encoding encoding)
        {
            Contract.Requires(factory != null);
            Contract.Requires(request != null);
            Contract.Requires(encoding != null);

            this.factory = factory;
            this.request = request;
            this.encoding = encoding;

            // Fill default values
            this.SetMultipart(this.request.Headers.Get(HttpHeaderNames.ContentType));
            var content = request as IHttpContent;
            if (content != null)
            {
                // Offer automatically if the given request is als type of HttpContent
                // See #1089
                this.Offer(content);
            }
            else
            {
                this.undecodedChunk = Unpooled.Buffer();
                this.ParseBody();
            }
        }

        void SetMultipart(ICharSequence contentType)
        {
            ICharSequence[] dataBoundary = HttpPostRequestDecoder.GetMultipartDataBoundary(contentType);
            if (dataBoundary != null)
            {
                this.multipartDataBoundary = new AsciiString(dataBoundary[0]);
                if (dataBoundary.Length > 1 && dataBoundary[1] != null)
                {
                    this.encoding = Encoding.GetEncoding(dataBoundary[1].ToString());
                }
            }
            else
            {
                this.multipartDataBoundary = null;
            }

            this.currentStatus = MultiPartStatus.HeaderDelimiter;
        }

        void CheckDestroyed()
        {
            if (this.destroyed)
            {
                throw new InvalidOperationException(
                    $"{StringUtil.SimpleClassName<HttpPostMultipartRequestDecoder>()} was destroyed already");
            }
        }

        public bool IsMultipart
        {
            get
            {
                this.CheckDestroyed();
                return true;
            }
        }

        public int DiscardThreshold
        {
            get { return this.discardThreshold; }
            set
            {
                Contract.Requires(value >= 0);

                this.discardThreshold = value;
            }
        }

        public List<IPostHttpData> GetBodyDataList()
        {
            this.CheckDestroyed();
            if (!this.isLastChunk)
            {
                throw new NotEnoughDataDecoderException(nameof(HttpPostMultipartRequestDecoder));
            }

            return this.bodyList;
        }

        public List<IPostHttpData> GetBodyDataList(AsciiString name)
        {
            this.CheckDestroyed();
            if (!this.isLastChunk)
            {
                throw new NotEnoughDataDecoderException(nameof(HttpPostMultipartRequestDecoder));
            }

            return this.bodyMapHttpData.TryGetValue(name, out List<IPostHttpData> list) ? list : null;
        }

        public IPostHttpData GetBodyData(AsciiString name)
        {
            this.CheckDestroyed();
            if (!this.isLastChunk)
            {
                throw new NotEnoughDataDecoderException(nameof(HttpPostMultipartRequestDecoder));
            }

            List<IPostHttpData> list;
            if (!this.bodyMapHttpData.TryGetValue(name, out list))
            {
                return null;
            }

            return list.Count > 0 ? list[0] : null;
        }

        public IHttpPostRequestDecoder Offer(IHttpContent content)
        {
            this.CheckDestroyed();

            // Maybe we should better not copy here for performance reasons but this will need
            // more care by the caller to release the content in a correct manner later
            // So maybe something to optimize on a later stage
            IByteBuffer buf = content.Content;
            if (this.undecodedChunk == null)
            {
                this.undecodedChunk = buf.Copy();
            }
            else
            {
                this.undecodedChunk.WriteBytes(buf);
            }
            if (content is ILastHttpContent)
            {
                this.isLastChunk = true;
            }
            this.ParseBody();
            if (this.undecodedChunk != null
                && this.undecodedChunk.WriterIndex > this.discardThreshold)
            {
                this.undecodedChunk.DiscardReadBytes();
            }

            return this;
        }

        public bool HasNext
        {
            get
            {
                this.CheckDestroyed();

                if (this.currentStatus == MultiPartStatus.Epilogue
                    && this.bodyListHttpDataRank >= this.bodyList.Count) // OK except if end of list
                {
                    throw new EndOfDataDecoderException(nameof(HttpPostMultipartRequestDecoder));
                }

                return this.bodyList.Count > 0
                    && this.bodyListHttpDataRank < this.bodyList.Count;
            }
        }

        public IPostHttpData Next()
        {
            this.CheckDestroyed();

            return this.HasNext ? this.bodyList[this.bodyListHttpDataRank++] : null;
        }

        public IPostHttpData CurrentPartialHttpData
        {
            get
            {
                if (this.currentFileUpload != null)
                {
                    return this.currentFileUpload;
                }

                return this.currentAttribute;
            }
        }

        void ParseBody()
        {
            if (this.currentStatus == MultiPartStatus.PreEpilogue
                || this.currentStatus == MultiPartStatus.Epilogue)
            {
                if (this.isLastChunk)
                {
                    this.currentStatus = MultiPartStatus.Epilogue;
                }

                return;
            }

            this.ParseBodyMultipart();
        }

        protected void AddHttpData(IPostHttpData data)
        {
            if (data == null)
            {
                return;
            }
            var name = (AsciiString)data.Name;
            if (!this.bodyMapHttpData.TryGetValue((AsciiString)data.Name, out List<IPostHttpData> list))
            {
                list = new List<IPostHttpData>(1);
                this.bodyMapHttpData.Add(name, list);
            }

            list.Add(data);
            this.bodyList.Add(data);
        }

        void ParseBodyMultipart()
        {
            if (this.undecodedChunk == null
                || this.undecodedChunk.ReadableBytes == 0)
            {
                // nothing to decode
                return;
            }

            IPostHttpData data = this.DecodeMultipart(this.currentStatus);
            while (data != null)
            {
                this.AddHttpData(data);
                if (this.currentStatus == MultiPartStatus.PreEpilogue
                    || this.currentStatus == MultiPartStatus.Epilogue)
                {
                    break;
                }

                data = this.DecodeMultipart(this.currentStatus);
            }
        }

        IPostHttpData DecodeMultipart(MultiPartStatus state)
        {
            switch (state)
            {
                case MultiPartStatus.Notstarted:
                    throw new ErrorDataDecoderException("Should not be called with the current getStatus");
                case MultiPartStatus.Preamble:
                    // Content-type: multipart/form-data, boundary=AaB03x
                    throw new ErrorDataDecoderException("Should not be called with the current getStatus");
                case MultiPartStatus.HeaderDelimiter:
                {
                    // --AaB03x or --AaB03x--
                    return this.FindMultipartDelimiter(
                        this.multipartDataBoundary,
                        MultiPartStatus.Disposition,
                        MultiPartStatus.PreEpilogue);
                }
                case MultiPartStatus.Disposition:
                {
                    // content-disposition: form-data; name="field1"
                    // content-disposition: form-data; name="pics"; filename="file1.txt"
                    // and other immediate values like
                    // Content-type: image/gif
                    // Content-Type: text/plain
                    // Content-Type: text/plain; charset=ISO-8859-1
                    // Content-Transfer-Encoding: binary
                    // The following line implies a change of mode (mixed mode)
                    // Content-type: multipart/mixed, boundary=BbC04y
                    return this.FindMultipartDisposition();
                }
                case MultiPartStatus.Field:
                {
                    // Now get value according to Content-Type and Charset
                    Encoding localEncoding = null;
                    this.currentFieldAttributes.TryGetValue(HttpHeaderValues.Charset, out IAttribute charsetAttribute);
                    if (charsetAttribute != null)
                    {
                        try
                        {
                            localEncoding = Encoding.GetEncoding(charsetAttribute.Value);
                        }
                        catch (IOException e)
                        {
                            throw new ErrorDataDecoderException(e);
                        }
                        catch (ArgumentException e)
                        {
                            throw new ErrorDataDecoderException(e);
                        }
                    }

                    IAttribute nameAttribute = this.currentFieldAttributes[HttpHeaderValues.Name];
                    if (this.currentAttribute == null)
                    {
                        this.currentFieldAttributes.TryGetValue(HttpHeaderNames.ContentLength, out IAttribute lengthAttribute);
                        long size;
                        try
                        {
                            size = lengthAttribute != null ? long.Parse(lengthAttribute.Value) : 0L;
                        }
                        catch (IOException e)
                        {
                            throw new ErrorDataDecoderException(e);
                        }
                        catch (FormatException)
                        {
                            size = 0;
                        }
                        try
                        {
                            if (size > 0)
                            {
                                this.currentAttribute = this.factory.CreateAttribute(
                                    this.request,
                                    CleanString(nameAttribute.Value).ToString(),
                                    size);
                            }
                            else
                            {
                                this.currentAttribute = this.factory.CreateAttribute(
                                    this.request,
                                    CleanString(nameAttribute.Value).ToString());
                            }
                        }
                        catch (ArgumentNullException e)
                        {
                            throw new ErrorDataDecoderException(e);
                        }
                        catch (ArgumentException e)
                        {
                            throw new ErrorDataDecoderException(e);
                        }
                        catch (IOException e)
                        {
                            throw new ErrorDataDecoderException(e);
                        }
                        if (localEncoding != null)
                        {
                            this.currentAttribute.ContentEncoding = localEncoding;
                        }
                    }
                    // load data
                    try
                    {
                        this.LoadFieldMultipart(this.multipartDataBoundary);
                    }
                    catch (NotEnoughDataDecoderException)
                    {
                        return null;
                    }
                    IAttribute finalAttribute = this.currentAttribute;
                    this.currentAttribute = null;
                    this.currentFieldAttributes = null;
                    // ready to load the next one
                    this.currentStatus = MultiPartStatus.HeaderDelimiter;
                    return finalAttribute;
                }
                case MultiPartStatus.Fileupload:
                {
                    // eventually restart from existing FileUpload
                    return this.GetFileUpload(this.multipartDataBoundary);
                }
                case MultiPartStatus.MixedDelimiter:
                {
                    // --AaB03x or --AaB03x--
                    // Note that currentFieldAttributes exists
                    return this.FindMultipartDelimiter(
                        this.multipartMixedBoundary,
                        MultiPartStatus.MixedDisposition,
                        MultiPartStatus.HeaderDelimiter);
                }
                case MultiPartStatus.MixedDisposition:
                {
                    return this.FindMultipartDisposition();
                }
                case MultiPartStatus.MixedFileUpload:
                {
                    // eventually restart from existing FileUpload
                    return this.GetFileUpload(this.multipartMixedBoundary);
                }
                case MultiPartStatus.PreEpilogue:
                case MultiPartStatus.Epilogue:
                    return null;
                default:
                    throw new ErrorDataDecoderException("Shouldn't reach here.");
            }
        }

        void SkipControlCharacters()
        {
            HttpPostBodyUtil.SeekAheadOptimize seekAhead;
            try
            {
                seekAhead = new HttpPostBodyUtil.SeekAheadOptimize(this.undecodedChunk);
            }
            catch (HttpPostBodyUtil.SeekAheadNoBackArrayException)
            {
                try
                {
                    this.SkipControlCharactersStandard();
                }
                catch (IndexOutOfRangeException e)
                {
                    throw new NotEnoughDataDecoderException(e);
                }
                return;
            }

            while (seekAhead.Position < seekAhead.Limit)
            {
                char c = (char)(seekAhead.Bytes[seekAhead.Position++] & 0xFF);
                if (!CharUtil.IsISOControl(c) && !char.IsWhiteSpace(c))
                {
                    seekAhead.SetReadPosition(1);
                    return;
                }
            }

            throw new NotEnoughDataDecoderException("Access out of bounds");
        }

        void SkipControlCharactersStandard()
        {
            while(true)
            {
                char c = (char)this.undecodedChunk.ReadByte();
                if (!CharUtil.IsISOControl(c) && !char.IsWhiteSpace(c))
                {
                    this.undecodedChunk.SetReaderIndex(this.undecodedChunk.ReaderIndex - 1);
                    break;
                }
            }
        }

        IPostHttpData FindMultipartDelimiter(ICharSequence delimiter, MultiPartStatus dispositionStatus, MultiPartStatus closeDelimiterStatus)
        {
            // --AaB03x or --AaB03x--
            int readerIndex = this.undecodedChunk.ReaderIndex;
            try
            {
                this.SkipControlCharacters();
            }
            catch (NotEnoughDataDecoderException)
            {
                this.undecodedChunk.SetReaderIndex(readerIndex);

                return null;
            }
            this.SkipOneLine();
            ICharSequence newline;
            try
            {
                newline = this.ReadDelimiter(delimiter);
            }
            catch (NotEnoughDataDecoderException)
            {
                this.undecodedChunk.SetReaderIndex(readerIndex);
                return null;
            }
            if (newline.Equals(delimiter))
            {
                this.currentStatus = dispositionStatus;
                return this.DecodeMultipart(dispositionStatus);
            }

            if (newline.RegionMatches(true, 0, delimiter, 0, delimiter.Count) 
                &&  newline.RegionMatches(true, delimiter.Count, DelimiterSeperatorString, 0, DelimiterSeperatorString.Count)) 
            {
                // CloseDelimiter or MIXED CloseDelimiter found
                this.currentStatus = closeDelimiterStatus;
                if (this.currentStatus == MultiPartStatus.HeaderDelimiter)
                {
                    // MixedCloseDelimiter
                    // end of the Mixed part
                    this.currentFieldAttributes = null;
                    return this.DecodeMultipart(MultiPartStatus.HeaderDelimiter);
                }

                return null;
            }

            this.undecodedChunk.SetReaderIndex(readerIndex);
            throw new ErrorDataDecoderException("No Multipart delimiter found");
        }

        IPostHttpData FindMultipartDisposition()
        {
            int readerIndex = this.undecodedChunk.ReaderIndex;
            if (this.currentStatus == MultiPartStatus.Disposition)
            {
                this.currentFieldAttributes = new Dictionary<ICharSequence, IAttribute>(CaseIgnoringComparator.Default);
            }

            // read many lines until empty line with newline found! Store all data
            while (!this.SkipOneLine())
            {
                string newline;
                try
                {
                    this.SkipControlCharacters();
                    newline = this.ReadLine();
                }
                catch (NotEnoughDataDecoderException)
                {
                    this.undecodedChunk.SetReaderIndex(readerIndex);
                    return null;
                }
                ICharSequence[] contents = SplitMultipartHeader(newline);
                if (HttpHeaderNames.ContentDisposition.ContentEqualsIgnoreCase(contents[0]))
                {
                    bool checkSecondArg;
                    if (this.currentStatus == MultiPartStatus.Disposition)
                    {
                        checkSecondArg = HttpHeaderValues.FormData.ContentEqualsIgnoreCase(contents[1]);
                    }
                    else
                    {
                        checkSecondArg = HttpHeaderValues.Attachment.ContentEqualsIgnoreCase(contents[1])
                            || HttpHeaderValues.File.ContentEqualsIgnoreCase(contents[1]);
                    }
                    if (checkSecondArg)
                    {
                        // read next values and store them in the map as Attribute
                        for (int i = 2; i < contents.Length; i++)
                        {
                            ICharSequence[] values = CharUtil.Split(contents[i], 2, '=');
                            IAttribute attribute;
                            try
                            {
                                ICharSequence name = CleanString(values[0]);
                                ICharSequence value = values[1];

                                // See http://www.w3.org/Protocols/rfc2616/rfc2616-sec19.html
                                if (HttpHeaderValues.FileName.ContentEquals(name))
                                {
                                    // filename value is quoted string so strip them
                                    value = value.SubSequence(1, value.Count - 1);
                                }
                                else
                                {
                                    // otherwise we need to clean the value
                                    value = CleanString(value);
                                }

                                attribute = this.factory.CreateAttribute(
                                    this.request, name.ToString(), value.ToString());
                            }
                            catch (ArgumentNullException e)
                            {
                                throw new ErrorDataDecoderException(e);
                            }
                            catch (ArgumentException e)
                            {
                                throw new ErrorDataDecoderException(e);
                            }

                            this.currentFieldAttributes.Add(new AsciiString(attribute.Name), attribute);
                        }
                    }
                }
                else if (HttpHeaderNames.ContentTransferEncoding.ContentEqualsIgnoreCase(contents[0]))
                {
                    IAttribute attribute;
                    try
                    {
                        attribute = this.factory.CreateAttribute(
                            this.request, 
                            HttpHeaderNames.ContentTransferEncoding.ToString(),
                            CleanString(contents[1]).ToString());
                    }
                    catch (ArgumentNullException e)
                    {
                        throw new ErrorDataDecoderException(e);
                    }
                    catch (ArgumentException e)
                    {
                        throw new ErrorDataDecoderException(e);
                    }

                    this.currentFieldAttributes.Add(HttpHeaderNames.ContentTransferEncoding, attribute);
                }
                else if (HttpHeaderNames.ContentLength.ContentEqualsIgnoreCase(contents[0]))
                {
                    IAttribute attribute;
                    try
                    {
                        attribute = this.factory.CreateAttribute(
                            this.request, 
                            HttpHeaderNames.ContentLength.ToString(),
                            CleanString(contents[1]).ToString());
                    }
                    catch (ArgumentNullException e)
                    {
                        throw new ErrorDataDecoderException(e);
                    }
                    catch (ArgumentException e)
                    {
                        throw new ErrorDataDecoderException(e);
                    }

                    this.currentFieldAttributes.Add(HttpHeaderNames.ContentLength, attribute);
                }
                else if (HttpHeaderNames.ContentType.ContentEqualsIgnoreCase(contents[0]))
                {
                    // Take care of possible "multipart/mixed"
                    if (HttpHeaderValues.MultipartMixed.ContentEqualsIgnoreCase(contents[1]))
                    {
                        if (this.currentStatus == MultiPartStatus.Disposition)
                        {
                            ICharSequence values = contents[2].SubstringAfter('=');
                            this.multipartMixedBoundary = new StringCharSequence(DelimiterSeperator+ values.ToString());
                            this.currentStatus = MultiPartStatus.MixedDelimiter;

                            return this.DecodeMultipart(MultiPartStatus.MixedDelimiter);
                        }
                        else
                        {
                            throw new ErrorDataDecoderException("Mixed Multipart found in a previous Mixed Multipart");
                        }
                    }
                    else
                    {
                        for (int i = 1; i < contents.Length; i++)
                        {
                            ICharSequence charsetHeader = HttpHeaderValues.Charset;
                            if (contents[i].RegionMatches(true, 0, charsetHeader, 0, charsetHeader.Count))
                            {
                                ICharSequence values = contents[i].SubstringAfter('=');
                                IAttribute attribute;
                                try
                                {
                                    attribute = this.factory.CreateAttribute(
                                        this.request,
                                        charsetHeader.ToString(),
                                        CleanString(values).ToString());
                                }
                                catch (ArgumentNullException e)
                                {
                                    throw new ErrorDataDecoderException(e);
                                }
                                catch (ArgumentException e)
                                {
                                    throw new ErrorDataDecoderException(e);
                                }
                                this.currentFieldAttributes.Add(HttpHeaderValues.Charset, attribute);
                            }
                            else
                            {
                                IAttribute attribute;
                                ICharSequence name;
                                try
                                {
                                    name = CleanString(contents[0]);
                                    attribute = this.factory.CreateAttribute(
                                        this.request,
                                        name.ToString(),
                                        contents[i].ToString());
                                }
                                catch (ArgumentNullException e)
                                {
                                    throw new ErrorDataDecoderException(e);
                                }
                                catch (ArgumentException e)
                                {
                                    throw new ErrorDataDecoderException(e);
                                }

                                this.currentFieldAttributes.Add(name, attribute);
                            }
                        }
                    }
                }
                else
                {
                    throw new ErrorDataDecoderException($"Unknown Params: {newline}");
                }
            }

            // Is it a FileUpload
            this.currentFieldAttributes.TryGetValue(HttpHeaderValues.FileName, out IAttribute filenameAttribute);
            if (this.currentStatus == MultiPartStatus.Disposition)
            {
                if (filenameAttribute != null)
                {
                    // FileUpload
                    this.currentStatus = MultiPartStatus.Fileupload;

                    // do not change the buffer position
                    return this.DecodeMultipart(MultiPartStatus.Fileupload);
                }
                else
                {
                    // Field
                    this.currentStatus = MultiPartStatus.Field;

                    // do not change the buffer position
                    return this.DecodeMultipart(MultiPartStatus.Field);
                }
            }
            else
            {
                if (filenameAttribute != null)
                {
                    // FileUpload
                    this.currentStatus = MultiPartStatus.MixedFileUpload;

                    // do not change the buffer position
                    return this.DecodeMultipart(MultiPartStatus.MixedFileUpload);
                }
                else
                {
                    // Field is not supported in MIXED mode
                    throw new ErrorDataDecoderException("Filename not found");
                }
            }
        }

        protected IPostHttpData GetFileUpload(ICharSequence delimiter)
        {
            // eventually restart from existing FileUpload
            // Now get value according to Content-Type and Charset
            this.currentFieldAttributes.TryGetValue(HttpHeaderNames.ContentTransferEncoding, out IAttribute encodingAttribute);
            Encoding localCharset = this.encoding;
            // Default
            TransferEncodingMechanism mechanism = TransferEncodingMechanism.Bit7;
            if (encodingAttribute != null)
            {
                string code;
                try
                {
                    code = encodingAttribute.Value.ToLower();
                }
                catch (IOException e)
                {
                    throw new ErrorDataDecoderException(e);
                }
                if (code.Equals(TransferEncodingMechanism.Bit7.Value))
                {
                    localCharset = Encoding.ASCII;
                }
                else if (code.Equals(TransferEncodingMechanism.Bit8.Value))
                {
                    localCharset = Encoding.UTF8;
                    mechanism = TransferEncodingMechanism.Bit8;
                }
                else if (code.Equals(TransferEncodingMechanism.Binary.Value))
                {
                    // no real charset, so let the default
                    mechanism = TransferEncodingMechanism.Binary;
                }
                else
                {
                    throw new ErrorDataDecoderException("TransferEncoding Unknown: " + code);
                }
            }
            this.currentFieldAttributes.TryGetValue(HttpHeaderValues.Charset, out IAttribute charsetAttribute);
            if (charsetAttribute != null)
            {
                try
                {
                    localCharset = Encoding.GetEncoding(charsetAttribute.Value);
                }
                catch (IOException e)
                {
                    throw new ErrorDataDecoderException(e);
                }
                catch (ArgumentException e)
                {
                    throw new ErrorDataDecoderException(e);
                }
            }
            if (this.currentFileUpload == null)
            {
                IAttribute filenameAttribute = this.currentFieldAttributes[HttpHeaderValues.FileName];
                IAttribute nameAttribute = this.currentFieldAttributes[HttpHeaderValues.Name];
                long size;
                try
                {
                    this.currentFieldAttributes.TryGetValue(HttpHeaderNames.ContentLength, out IAttribute lengthAttribute);
                    size = lengthAttribute != null ? long.Parse(lengthAttribute.Value) : 0L;
                }
                catch (IOException e)
                {
                    throw new ErrorDataDecoderException(e);
                }
                catch (FormatException)
                {
                    size = 0;
                }
                try
                {
                    this.currentFieldAttributes.TryGetValue(HttpHeaderNames.ContentType, out IAttribute contentTypeAttribute);
                    string contentType;
                    if (contentTypeAttribute != null)
                    {
                        contentType = contentTypeAttribute.Value;
                    }
                    else
                    {
                        contentType = HttpPostBodyUtil.DefaultBinaryContentType;
                    }

                    this.currentFileUpload = this.factory.CreateFileUpload(
                        this.request,
                        CleanString(nameAttribute.Value).ToString(), 
                        CleanString(filenameAttribute.Value).ToString(),
                        contentType, 
                        mechanism.Value, 
                        localCharset,
                        size);
                }
                catch (ArgumentNullException e)
                {
                    throw new ErrorDataDecoderException(e);
                }
                catch (ArgumentException e)
                {
                    throw new ErrorDataDecoderException(e);
                }
                catch (IOException e)
                {
                    throw new ErrorDataDecoderException(e);
                }
            }
            // load data as much as possible
            try
            {
                this.ReadFileUploadByteMultipart(delimiter);
            }
            catch (NotEnoughDataDecoderException)
            {
                // do not change the buffer position
                // since some can be already saved into FileUpload
                // So do not change the currentStatus
                return null;
            }
            if (this.currentFileUpload.Completed)
            {
                // ready to load the next one
                if (this.currentStatus == MultiPartStatus.Fileupload)
                {
                    this.currentStatus = MultiPartStatus.HeaderDelimiter;
                    this.currentFieldAttributes = null;
                }
                else
                {
                    this.currentStatus = MultiPartStatus.MixedDelimiter;
                    this.CleanMixedAttributes();
                }
                IFileUpload fileUpload = this.currentFileUpload;
                this.currentFileUpload = null;
                return fileUpload;
            }

            // do not change the buffer position
            // since some can be already saved into FileUpload
            // So do not change the currentStatus
            return null;
        }

        public void Destroy()
        {
            this.CheckDestroyed();
            this.CleanFiles();
            this.destroyed = true;

            if (this.undecodedChunk != null && this.undecodedChunk.ReferenceCount > 0)
            {
                this.undecodedChunk.Release();
                this.undecodedChunk = null;
            }

            // release all data which was not yet pulled
            for (int i = this.bodyListHttpDataRank; i < this.bodyList.Count; i++)
            {
                this.bodyList[i].Release();
            }
        }

        public void CleanFiles()
        {
            this.CheckDestroyed();
            this.factory.CleanRequestHttpData(this.request);
        }

        public void RemoveHttpDataFromClean(IPostHttpData data)
        {
            this.CheckDestroyed();
            this.factory.RemoveHttpDataFromClean(this.request, data);
        }

        void CleanMixedAttributes()
        {
            this.currentFieldAttributes.Remove(HttpHeaderValues.Charset);
            this.currentFieldAttributes.Remove(HttpHeaderNames.ContentLength);
            this.currentFieldAttributes.Remove(HttpHeaderNames.ContentTransferEncoding);
            this.currentFieldAttributes.Remove(HttpHeaderNames.ContentType);
            this.currentFieldAttributes.Remove(HttpHeaderValues.FileName);
        }

        string ReadLineStandard()
        {
            int readerIndex = this.undecodedChunk.ReaderIndex;
            try
            {
                IByteBuffer line = Unpooled.Buffer(64);
                while (this.undecodedChunk.IsReadable())
                {
                    byte nextByte = this.undecodedChunk.ReadByte();
                    if (nextByte == HttpConstants.CarriageReturn)
                    {
                        // check but do not changed readerIndex
                        nextByte = this.undecodedChunk.GetByte(this.undecodedChunk.ReaderIndex);
                        if (nextByte == HttpConstants.LineFeed)
                        {
                            // force read
                            this.undecodedChunk.ReadByte();
                            return line.ToString(this.encoding);
                        }
                        else
                        {
                            // Write CR (not followed by LF)
                            line.WriteByte(HttpConstants.CarriageReturn);
                        }
                    }
                    else if (nextByte == HttpConstants.LineFeed)
                    {
                        return line.ToString(this.encoding);
                    }
                    else
                    {
                        line.WriteByte(nextByte);
                    }
                }
            }
            catch (IndexOutOfRangeException e)
            {
                this.undecodedChunk.SetReaderIndex(readerIndex);
                throw new NotEnoughDataDecoderException(e);
            }

            this.undecodedChunk.SetReaderIndex(readerIndex);
            throw new NotEnoughDataDecoderException(nameof(HttpPostMultipartRequestDecoder));
        }

        string ReadLine()
        {
            HttpPostBodyUtil.SeekAheadOptimize seekAhead;
            try
            {
                seekAhead = new HttpPostBodyUtil.SeekAheadOptimize(this.undecodedChunk);
            }
            catch (HttpPostBodyUtil.SeekAheadNoBackArrayException)
            {
                return this.ReadLineStandard();
            }

            int readerIndex = this.undecodedChunk.ReaderIndex;
            try
            {
                IByteBuffer line = Unpooled.Buffer(64);
                while (seekAhead.Position < seekAhead.Limit)
                {
                    byte nextByte = seekAhead.Bytes[seekAhead.Position++];
                    if (nextByte == HttpConstants.CarriageReturn)
                    {
                        if (seekAhead.Position < seekAhead.Limit)
                        {
                            nextByte = seekAhead.Bytes[seekAhead.Position++];
                            if (nextByte == HttpConstants.LineFeed)
                            {
                                seekAhead.SetReadPosition(0);
                                return line.ToString(this.encoding);
                            }
                            else
                            {
                                // Write CR (not followed by LF)
                                seekAhead.Position--;
                                line.WriteByte(HttpConstants.CarriageReturn);
                            }
                        }
                        else
                        {
                            line.WriteByte(nextByte);
                        }
                    }
                    else if (nextByte == HttpConstants.LineFeed)
                    {
                        seekAhead.SetReadPosition(0);
                        return line.ToString(this.encoding);
                    }
                    else
                    {
                        line.WriteByte(nextByte);
                    }
                }
            }
            catch (IndexOutOfRangeException e)
            {
                this.undecodedChunk.SetReaderIndex(readerIndex);
                throw new NotEnoughDataDecoderException(e);
            }

            this.undecodedChunk.SetReaderIndex(readerIndex);
            throw new NotEnoughDataDecoderException(nameof(HttpPostMultipartRequestDecoder));
        }

        ICharSequence ReadDelimiterStandard(ICharSequence delimiter)
        {
            int readerIndex = this.undecodedChunk.ReaderIndex;
            try
            {
                var sb = new StringBuilderCharSequence(64);
                int delimiterPos = 0;
                int len = delimiter.Count;
                while (this.undecodedChunk.IsReadable() && delimiterPos < len)
                {
                    byte nextByte = this.undecodedChunk.ReadByte();
                    if (nextByte == delimiter[delimiterPos])
                    {
                        delimiterPos++;
                        sb.Append((char)nextByte);
                    }
                    else
                    {
                        // delimiter not found so break here !
                        this.undecodedChunk.SetReaderIndex(readerIndex);
                        throw new NotEnoughDataDecoderException(nameof(HttpPostMultipartRequestDecoder));
                    }
                }

                // Now check if either opening delimiter or closing delimiter
                if (this.undecodedChunk.IsReadable())
                {
                    byte nextByte = this.undecodedChunk.ReadByte();
                    // first check for opening delimiter
                    if (nextByte == HttpConstants.CarriageReturn)
                    {
                        nextByte = this.undecodedChunk.ReadByte();
                        if (nextByte == HttpConstants.LineFeed)
                        {
                            return sb;
                        }
                        else
                        {
                            // error since CR must be followed by LF
                            // delimiter not found so break here !
                            this.undecodedChunk.SetReaderIndex(readerIndex);
                            throw new NotEnoughDataDecoderException(nameof(HttpPostMultipartRequestDecoder));
                        }
                    }
                    else if (nextByte == HttpConstants.LineFeed)
                    {
                        return sb;
                    }
                    else if (nextByte == '-')
                    {
                        sb.Append('-');
                        // second check for closing delimiter
                        nextByte = this.undecodedChunk.ReadByte();
                        if (nextByte == '-')
                        {
                            sb.Append('-');
                            // now try to find if CRLF or LF there
                            if (this.undecodedChunk.IsReadable())
                            {
                                nextByte = this.undecodedChunk.ReadByte();
                                if (nextByte == HttpConstants.CarriageReturn)
                                {
                                    nextByte = this.undecodedChunk.ReadByte();
                                    if (nextByte == HttpConstants.LineFeed)
                                    {
                                        return sb;
                                    }
                                    else
                                    {
                                        // error CR without LF
                                        // delimiter not found so break here !
                                        this.undecodedChunk.SetReaderIndex(readerIndex);
                                        throw new NotEnoughDataDecoderException(nameof(HttpPostMultipartRequestDecoder));
                                    }
                                }
                                else if (nextByte == HttpConstants.LineFeed)
                                {
                                    return sb;
                                }
                                else
                                {
                                    // No CRLF but ok however (Adobe Flash uploader)
                                    // minus 1 since we read one char ahead but
                                    // should not
                                    this.undecodedChunk.SetReaderIndex(this.undecodedChunk.ReaderIndex - 1);
                                    return sb;
                                }
                            }
                            // FIXME what do we do here?
                            // either considering it is fine, either waiting for
                            // more data to come?
                            // lets try considering it is fine...
                            return sb;
                        }
                        // only one '-' => not enough
                        // whatever now => error since incomplete
                    }
                }
            }
            catch (IndexOutOfRangeException e)
            {
                this.undecodedChunk.SetReaderIndex(readerIndex);
                throw new NotEnoughDataDecoderException(e);
            }

            this.undecodedChunk.SetReaderIndex(readerIndex);
            throw new NotEnoughDataDecoderException(nameof(HttpPostMultipartRequestDecoder));
        }

        ICharSequence ReadDelimiter(ICharSequence delimiter)
        {
            HttpPostBodyUtil.SeekAheadOptimize seekAhead;
            try
            {
                seekAhead = new HttpPostBodyUtil.SeekAheadOptimize(this.undecodedChunk);
            }
            catch (HttpPostBodyUtil.SeekAheadNoBackArrayException)
            {
                return this.ReadDelimiterStandard(delimiter);
            }

            int readerIndex = this.undecodedChunk.ReaderIndex;
            int delimiterPos = 0;
            int len = delimiter.Count;
            try
            {
                var sb = new StringBuilderCharSequence(64);
                // check conformity with delimiter
                while (seekAhead.Position < seekAhead.Limit && delimiterPos < len)
                {
                    byte nextByte = seekAhead.Bytes[seekAhead.Position++];
                    if (nextByte == delimiter[delimiterPos])
                    {
                        delimiterPos++;
                        sb.Append((char)nextByte);
                    }
                    else
                    {
                        // delimiter not found so break here !
                        this.undecodedChunk.SetReaderIndex(readerIndex);
                        throw new NotEnoughDataDecoderException(nameof(HttpPostMultipartRequestDecoder));
                    }
                }

                // Now check if either opening delimiter or closing delimiter
                if (seekAhead.Position < seekAhead.Limit)
                {
                    byte nextByte = seekAhead.Bytes[seekAhead.Position++];
                    if (nextByte == HttpConstants.CarriageReturn)
                    {
                        // first check for opening delimiter
                        if (seekAhead.Position < seekAhead.Limit)
                        {
                            nextByte = seekAhead.Bytes[seekAhead.Position++];
                            if (nextByte == HttpConstants.LineFeed)
                            {
                                seekAhead.SetReadPosition(0);
                                return sb;
                            }
                            else
                            {
                                // error CR without LF
                                // delimiter not found so break here !
                                this.undecodedChunk.SetReaderIndex(readerIndex);
                                throw new NotEnoughDataDecoderException(nameof(HttpPostMultipartRequestDecoder));
                            }
                        }
                        else
                        {
                            // error since CR must be followed by LF
                            // delimiter not found so break here !
                            this.undecodedChunk.SetReaderIndex(readerIndex);
                            throw new NotEnoughDataDecoderException(nameof(HttpPostMultipartRequestDecoder));
                        }
                    }
                    else if (nextByte == HttpConstants.LineFeed)
                    {
                        // same first check for opening delimiter where LF used with
                        // no CR
                        seekAhead.SetReadPosition(0);
                        return sb;
                    }
                    else if (nextByte == '-')
                    {
                        sb.Append('-');
                        // second check for closing delimiter
                        if (seekAhead.Position < seekAhead.Limit)
                        {
                            nextByte = seekAhead.Bytes[seekAhead.Position++];
                            if (nextByte == '-')
                            {
                                sb.Append('-');
                                // now try to find if CRLF or LF there
                                if (seekAhead.Position < seekAhead.Limit)
                                {
                                    nextByte = seekAhead.Bytes[seekAhead.Position++];
                                    if (nextByte == HttpConstants.CarriageReturn)
                                    {
                                        if (seekAhead.Position < seekAhead.Limit)
                                        {
                                            nextByte = seekAhead.Bytes[seekAhead.Position++];
                                            if (nextByte == HttpConstants.LineFeed)
                                            {
                                                seekAhead.SetReadPosition(0);
                                                return sb;
                                            }
                                            else
                                            {
                                                // error CR without LF
                                                // delimiter not found so break here !
                                                this.undecodedChunk.SetReaderIndex(readerIndex);
                                                throw new NotEnoughDataDecoderException(nameof(HttpPostMultipartRequestDecoder));
                                            }
                                        }
                                        else
                                        {
                                            // error CR without LF
                                            // delimiter not found so break here !
                                            this.undecodedChunk.SetReaderIndex(readerIndex);
                                            throw new NotEnoughDataDecoderException(nameof(HttpPostMultipartRequestDecoder));
                                        }
                                    }
                                    else if (nextByte == HttpConstants.LineFeed)
                                    {
                                        seekAhead.SetReadPosition(0);
                                        return sb;
                                    }
                                    else
                                    {
                                        // No CRLF but ok however (Adobe Flash
                                        // uploader)
                                        // minus 1 since we read one char ahead but
                                        // should not
                                        seekAhead.SetReadPosition(1);
                                        return sb;
                                    }
                                }
                                // FIXME what do we do here?
                                // either considering it is fine, either waiting for
                                // more data to come?
                                // lets try considering it is fine...
                                seekAhead.SetReadPosition(0);
                                return sb;
                            }
                            // whatever now => error since incomplete
                            // only one '-' => not enough or whatever not enough
                            // element
                        }
                    }
                }
            }
            catch (IndexOutOfRangeException e)
            {
                this.undecodedChunk.SetReaderIndex(readerIndex);
                throw new NotEnoughDataDecoderException(e);
            }

            this.undecodedChunk.SetReaderIndex(readerIndex);
            throw new NotEnoughDataDecoderException(nameof(HttpPostMultipartRequestDecoder));
        }

        void ReadFileUploadByteMultipartStandard(ICharSequence delimiter)
        {
            int readerIndex = this.undecodedChunk.ReaderIndex;
            // found the decoder limit
            bool newLine = true;
            int index = 0;
            int lastPosition = this.undecodedChunk.ReaderIndex;
            bool found = false;

            while (this.undecodedChunk.IsReadable())
            {
                byte nextByte = this.undecodedChunk.ReadByte();
                if (newLine)
                {
                    // Check the delimiter
                    if (nextByte == CharUtil.CodePointAt(delimiter, index))
                    {
                        index++;
                        if (delimiter.Count == index)
                        {
                            found = true;
                            break;
                        }
                    }
                    else
                    {
                        newLine = false;
                        index = 0;
                        // continue until end of line
                        if (nextByte == HttpConstants.CarriageReturn)
                        {
                            if (this.undecodedChunk.IsReadable())
                            {
                                nextByte = this.undecodedChunk.ReadByte();
                                if (nextByte == HttpConstants.LineFeed)
                                {
                                    newLine = true;
                                    index = 0;
                                    lastPosition = this.undecodedChunk.ReaderIndex - 2;
                                }
                                else
                                {
                                    // save last valid position
                                    lastPosition = this.undecodedChunk.ReaderIndex - 1;

                                    // Unread next byte.
                                    this.undecodedChunk.SetReaderIndex(lastPosition);
                                }
                            }
                        }
                        else if (nextByte == HttpConstants.LineFeed)
                        {
                            newLine = true;
                            index = 0;
                            lastPosition = this.undecodedChunk.ReaderIndex - 1;
                        }
                        else
                        {
                            // save last valid position
                            lastPosition = this.undecodedChunk.ReaderIndex;
                        }
                    }
                }
                else
                {
                    // continue until end of line
                    if (nextByte == HttpConstants.CarriageReturn)
                    {
                        if (this.undecodedChunk.IsReadable())
                        {
                            nextByte = this.undecodedChunk.ReadByte();
                            if (nextByte == HttpConstants.LineFeed)
                            {
                                newLine = true;
                                index = 0;
                                lastPosition = this.undecodedChunk.ReaderIndex - 2;
                            }
                            else
                            {
                                // save last valid position
                                lastPosition = this.undecodedChunk.ReaderIndex - 1;

                                // Unread next byte.
                                this.undecodedChunk.SetReaderIndex(lastPosition);
                            }
                        }
                    }
                    else if (nextByte == HttpConstants.LineFeed)
                    {
                        newLine = true;
                        index = 0;
                        lastPosition = this.undecodedChunk.ReaderIndex - 1;
                    }
                    else
                    {
                        // save last valid position
                        lastPosition = this.undecodedChunk.ReaderIndex;
                    }
                }
            }
            IByteBuffer buffer = this.undecodedChunk.Copy(readerIndex, lastPosition - readerIndex);
            if (found)
            {
                // found so lastPosition is correct and final
                try
                {
                    this.currentFileUpload.AddContent(buffer, true);
                    // just before the CRLF and delimiter
                    this.undecodedChunk.SetReaderIndex(lastPosition);
                }
                catch (IOException e)
                {
                    throw new ErrorDataDecoderException(e);
                }
            }
            else
            {
                // possibly the delimiter is partially found but still the last
                // position is OK
                try
                {
                    this.currentFileUpload.AddContent(buffer, false);
                    // last valid char (not CR, not LF, not beginning of delimiter)
                    this.undecodedChunk.SetReaderIndex(lastPosition);

                    throw new NotEnoughDataDecoderException(nameof(HttpPostMultipartRequestDecoder));
                }
                catch (IOException e)
                {
                    throw new ErrorDataDecoderException(e);
                }
            }
        }

        void ReadFileUploadByteMultipart(ICharSequence delimiter)
        {
            HttpPostBodyUtil.SeekAheadOptimize seekAhead;
            try
            {
                seekAhead = new HttpPostBodyUtil.SeekAheadOptimize(this.undecodedChunk);
            }
            catch (HttpPostBodyUtil.SeekAheadNoBackArrayException)
            {
                this.ReadFileUploadByteMultipartStandard(delimiter);
                return;
            }

            int readerIndex = this.undecodedChunk.ReaderIndex;
            // found the decoder limit
            bool newLine = true;
            int index = 0;
            int lastrealpos = seekAhead.Position;
            bool found = false;

            while (seekAhead.Position < seekAhead.Limit)
            {
                byte nextByte = seekAhead.Bytes[seekAhead.Position++];
                if (newLine)
                {
                    // Check the delimiter
                    if (nextByte == CharUtil.CodePointAt(delimiter, index))
                    {
                        index++;
                        if (delimiter.Count == index)
                        {
                            found = true;
                            break;
                        }
                    }
                    else
                    {
                        newLine = false;
                        index = 0;
                        // continue until end of line
                        if (nextByte == HttpConstants.CarriageReturn)
                        {
                            if (seekAhead.Position < seekAhead.Limit)
                            {
                                nextByte = seekAhead.Bytes[seekAhead.Position++];
                                if (nextByte == HttpConstants.LineFeed)
                                {
                                    newLine = true;
                                    index = 0;
                                    lastrealpos = seekAhead.Position - 2;
                                }
                                else
                                {
                                    // unread next byte
                                    seekAhead.Position--;

                                    // save last valid position
                                    lastrealpos = seekAhead.Position;
                                }
                            }
                        }
                        else if (nextByte == HttpConstants.LineFeed)
                        {
                            newLine = true;
                            index = 0;
                            lastrealpos = seekAhead.Position - 1;
                        }
                        else
                        {
                            // save last valid position
                            lastrealpos = seekAhead.Position;
                        }
                    }
                }
                else
                {
                    // continue until end of line
                    if (nextByte == HttpConstants.CarriageReturn)
                    {
                        if (seekAhead.Position < seekAhead.Limit)
                        {
                            nextByte = seekAhead.Bytes[seekAhead.Position++];
                            if (nextByte == HttpConstants.LineFeed)
                            {
                                newLine = true;
                                index = 0;
                                lastrealpos = seekAhead.Position - 2;
                            }
                            else
                            {
                                // unread next byte
                                seekAhead.Position--;

                                // save last valid position
                                lastrealpos = seekAhead.Position;
                            }
                        }
                    }
                    else if (nextByte == HttpConstants.LineFeed)
                    {
                        newLine = true;
                        index = 0;
                        lastrealpos = seekAhead.Position - 1;
                    }
                    else
                    {
                        // save last valid position
                        lastrealpos = seekAhead.Position;
                    }
                }
            }
            int lastPosition = seekAhead.GetReadPosition(lastrealpos);
            IByteBuffer buffer = this.undecodedChunk.Copy(readerIndex, lastPosition - readerIndex);
            if (found)
            {
                // found so lastPosition is correct and final
                try
                {
                    this.currentFileUpload.AddContent(buffer, true);
                    // just before the CRLF and delimiter
                    this.undecodedChunk.SetReaderIndex(lastPosition);
                }
                catch (IOException e)
                {
                    throw new ErrorDataDecoderException(e);
                }
            }
            else
            {
                // possibly the delimiter is partially found but still the last
                // position is OK
                try
                {
                    this.currentFileUpload.AddContent(buffer, false);
                    // last valid char (not CR, not LF, not beginning of delimiter)
                    this.undecodedChunk.SetReaderIndex(lastPosition);

                    throw new NotEnoughDataDecoderException(nameof(HttpPostMultipartRequestDecoder));
                }
                catch (IOException e)
                {
                    throw new ErrorDataDecoderException(e);
                }
            }
        }

        void LoadFieldMultipartStandard(ICharSequence delimiter)
        {
            int readerIndex = this.undecodedChunk.ReaderIndex;
            try
            {
                // found the decoder limit
                bool newLine = true;
                int index = 0;
                int lastPosition = this.undecodedChunk.ReaderIndex;
                bool found = false;

                while (this.undecodedChunk.IsReadable())
                {
                    byte nextByte = this.undecodedChunk.ReadByte();
                    if (newLine)
                    {
                        // Check the delimiter
                        if (nextByte == CharUtil.CodePointAt(delimiter, index))
                        {
                            index++;
                            if (delimiter.Count == index)
                            {
                                found = true;
                                break;
                            }
                        }
                        else
                        {
                            newLine = false;
                            index = 0;
                            // continue until end of line
                            if (nextByte == HttpConstants.CarriageReturn)
                            {
                                if (this.undecodedChunk.IsReadable())
                                {
                                    nextByte = this.undecodedChunk.ReadByte();
                                    if (nextByte == HttpConstants.LineFeed)
                                    {
                                        newLine = true;
                                        index = 0;
                                        lastPosition = this.undecodedChunk.ReaderIndex - 2;
                                    }
                                    else
                                    {
                                        // Unread second nextByte
                                        lastPosition = this.undecodedChunk.ReaderIndex - 1;
                                        this.undecodedChunk.SetReaderIndex(lastPosition);
                                    }
                                }
                                else
                                {
                                    lastPosition = this.undecodedChunk.ReaderIndex - 1;
                                }
                            }
                            else if (nextByte == HttpConstants.LineFeed)
                            {
                                newLine = true;
                                index = 0;
                                lastPosition = this.undecodedChunk.ReaderIndex - 1;
                            }
                            else
                            {
                                lastPosition = this.undecodedChunk.ReaderIndex;
                            }
                        }
                    }
                    else
                    {
                        // continue until end of line
                        if (nextByte == HttpConstants.CarriageReturn)
                        {
                            if (this.undecodedChunk.IsReadable())
                            {
                                nextByte = this.undecodedChunk.ReadByte();
                                if (nextByte == HttpConstants.LineFeed)
                                {
                                    newLine = true;
                                    index = 0;
                                    lastPosition = this.undecodedChunk.ReaderIndex - 2;
                                }
                                else
                                {
                                    // Unread second nextByte
                                    lastPosition = this.undecodedChunk.ReaderIndex - 1;
                                    this.undecodedChunk.SetReaderIndex(lastPosition);
                                }
                            }
                            else
                            {
                                lastPosition = this.undecodedChunk.ReaderIndex - 1;
                            }
                        }
                        else if (nextByte == HttpConstants.LineFeed)
                        {
                            newLine = true;
                            index = 0;
                            lastPosition = this.undecodedChunk.ReaderIndex - 1;
                        }
                        else
                        {
                            lastPosition = this.undecodedChunk.ReaderIndex;
                        }
                    }
                }
                if (found)
                {
                    // found so lastPosition is correct
                    // but position is just after the delimiter (either close
                    // delimiter or simple one)
                    // so go back of delimiter size
                    try
                    {
                        this.currentAttribute.AddContent(
                            this.undecodedChunk.Copy(readerIndex, lastPosition - readerIndex), true);
                    }
                    catch (IOException e)
                    {
                        throw new ErrorDataDecoderException(e);
                    }
                    this.undecodedChunk.SetReaderIndex(lastPosition);
                }
                else
                {
                    try
                    {
                        this.currentAttribute.AddContent(
                            this.undecodedChunk.Copy(readerIndex, lastPosition - readerIndex), false);
                    }
                    catch (IOException e)
                    {
                        throw new ErrorDataDecoderException(e);
                    }
                    this.undecodedChunk.SetReaderIndex(lastPosition);

                    throw new NotEnoughDataDecoderException(nameof(HttpPostMultipartRequestDecoder));
                }
            }
            catch (IndexOutOfRangeException e)
            {
                this.undecodedChunk.SetReaderIndex(readerIndex);
                throw new NotEnoughDataDecoderException(e);
            }
        }

        void LoadFieldMultipart(ICharSequence delimiter)
        {
            HttpPostBodyUtil.SeekAheadOptimize seekAhead;
            try
            {
                seekAhead = new HttpPostBodyUtil.SeekAheadOptimize(this.undecodedChunk);
            }
            catch (HttpPostBodyUtil.SeekAheadNoBackArrayException)
            {
                this.LoadFieldMultipartStandard(delimiter);
                return;
            }

            int readerIndex = this.undecodedChunk.ReaderIndex;
            try
            {
                // found the decoder limit
                bool newLine = true;
                int index = 0;
                int lastrealpos = seekAhead.Position;
                bool found = false;

                while (seekAhead.Position < seekAhead.Limit)
                {
                    byte nextByte = seekAhead.Bytes[seekAhead.Position++];
                    if (newLine)
                    {
                        // Check the delimiter
                        if (nextByte == CharUtil.CodePointAt(delimiter, index))
                        {
                            index++;
                            if (delimiter.Count == index)
                            {
                                found = true;
                                break;
                            }
                        }
                        else
                        {
                            newLine = false;
                            index = 0;
                            // continue until end of line
                            if (nextByte == HttpConstants.CarriageReturn)
                            {
                                if (seekAhead.Position < seekAhead.Limit)
                                {
                                    nextByte = seekAhead.Bytes[seekAhead.Position++];
                                    if (nextByte == HttpConstants.LineFeed)
                                    {
                                        newLine = true;
                                        index = 0;
                                        lastrealpos = seekAhead.Position - 2;
                                    }
                                    else
                                    {
                                        // Unread last nextByte
                                        seekAhead.Position--;
                                        lastrealpos = seekAhead.Position;
                                    }
                                }
                            }
                            else if (nextByte == HttpConstants.LineFeed)
                            {
                                newLine = true;
                                index = 0;
                                lastrealpos = seekAhead.Position - 1;
                            }
                            else
                            {
                                lastrealpos = seekAhead.Position;
                            }
                        }
                    }
                    else
                    {
                        // continue until end of line
                        if (nextByte == HttpConstants.CarriageReturn)
                        {
                            if (seekAhead.Position < seekAhead.Limit)
                            {
                                nextByte = seekAhead.Bytes[seekAhead.Position++];
                                if (nextByte == HttpConstants.LineFeed)
                                {
                                    newLine = true;
                                    index = 0;
                                    lastrealpos = seekAhead.Position - 2;
                                }
                                else
                                {
                                    // Unread last nextByte
                                    seekAhead.Position--;
                                    lastrealpos = seekAhead.Position;
                                }
                            }
                        }
                        else if (nextByte == HttpConstants.LineFeed)
                        {
                            newLine = true;
                            index = 0;
                            lastrealpos = seekAhead.Position - 1;
                        }
                        else
                        {
                            lastrealpos = seekAhead.Position;
                        }
                    }
                }
                int lastPosition = seekAhead.GetReadPosition(lastrealpos);
                if (found)
                {
                    // found so lastPosition is correct
                    // but position is just after the delimiter (either close
                    // delimiter or simple one)
                    // so go back of delimiter size
                    try
                    {
                        this.currentAttribute.AddContent(
                            this.undecodedChunk.Copy(readerIndex, lastPosition - readerIndex), true);
                    }
                    catch (IOException e)
                    {
                        throw new ErrorDataDecoderException(e);
                    }
                    this.undecodedChunk.SetReaderIndex(lastPosition);
                }
                else
                {
                    try
                    {
                        this.currentAttribute.AddContent(
                            this.undecodedChunk.Copy(readerIndex, lastPosition - readerIndex), false);
                    }
                    catch (IOException e)
                    {
                        throw new ErrorDataDecoderException(e);
                    }
                    this.undecodedChunk.SetReaderIndex(lastPosition);

                    throw new NotEnoughDataDecoderException(nameof(HttpPostMultipartRequestDecoder));
                }
            }
            catch (IndexOutOfRangeException e)
            {
                this.undecodedChunk.SetReaderIndex(readerIndex);
                throw new NotEnoughDataDecoderException(e);
            }
        }

        static ICharSequence CleanString(IEnumerable<char> field)
        {
            var sb = new StringBuilderCharSequence();
            foreach (char nextChar in field)
            {
                if (nextChar == HttpConstants.Colon)
                {
                    sb.Append(HttpConstants.CharHorizontalSpace);
                }
                else if (nextChar == HttpConstants.Comma)
                {
                    sb.Append(HttpConstants.CharHorizontalSpace);
                }
                else if (nextChar == HttpConstants.EqualsSign)
                {
                    sb.Append(HttpConstants.CharHorizontalSpace);
                }
                else if (nextChar == HttpConstants.Semicolon)
                {
                    sb.Append(HttpConstants.CharHorizontalSpace);
                }
                else if (nextChar == HttpConstants.HorizontalTab)
                {
                    sb.Append(HttpConstants.CharHorizontalSpace);
                }
                else if (nextChar == HttpConstants.DoubleQuote)
                {
                    // nothing added, just removes it
                }
                else
                {
                    sb.Append(nextChar);
                }
            }

            return CharUtil.Trim(sb);
        }

        bool SkipOneLine()
        {
            if (!this.undecodedChunk.IsReadable())
            {
                return false;
            }

            byte nextByte = this.undecodedChunk.ReadByte();
            if (nextByte == HttpConstants.CarriageReturn)
            {
                if (!this.undecodedChunk.IsReadable())
                {
                    this.undecodedChunk.SetReaderIndex(this.undecodedChunk.ReaderIndex - 1);
                    return false;
                }

                nextByte = this.undecodedChunk.ReadByte();
                if (nextByte == HttpConstants.LineFeed)
                {
                    return true;
                }

                this.undecodedChunk.SetReaderIndex(this.undecodedChunk.ReaderIndex - 2);
                return false;
            }

            if (nextByte == HttpConstants.LineFeed)
            {
                return true;
            }

            this.undecodedChunk.SetReaderIndex(this.undecodedChunk.ReaderIndex - 1);
            return false;
        }

        static ICharSequence[] SplitMultipartHeader(string sb)
        {
            var sequence = new StringCharSequence(sb);
            var headers = new List<ICharSequence>(1);
            int nameEnd;
            int colonEnd;
            int nameStart = HttpPostBodyUtil.FindNonWhitespace(sequence, 0);
            for (nameEnd = nameStart; nameEnd < sequence.Count; nameEnd++)
            {
                char ch = sequence[nameEnd];
                if (ch == ':' || char.IsWhiteSpace(ch))
                {
                    break;
                }
            }
            for (colonEnd = nameEnd; colonEnd < sequence.Count; colonEnd++)
            {
                if (sequence[colonEnd] == ':')
                {
                    colonEnd++;
                    break;
                }
            }

            int valueStart = HttpPostBodyUtil.FindNonWhitespace(sequence, colonEnd);
            int valueEnd = HttpPostBodyUtil.FindEndOfString(sequence);
            headers.Add(sequence.SubSequence(nameStart, nameEnd));
            ICharSequence svalue = sequence.SubSequence(valueStart, valueEnd);
            ICharSequence[] values;
            if (svalue.IndexOf(';') >= 0)
            {
                values = SplitMultipartHeaderValues(svalue);
            }
            else
            {
                values = CharUtil.Split(svalue, ',');
            }

            foreach (ICharSequence value in values)
            {
                headers.Add(CharUtil.Trim(value));
            }

            return headers.ToArray();
        }

        static ICharSequence[] SplitMultipartHeaderValues(ICharSequence sequence)
        {
            List<ICharSequence> values = InternalThreadLocalMap.Get().CharSequenceList(1);
            bool inQuote = false;
            bool escapeNext = false;
            int start = 0;
            for (int i = 0; i < sequence.Count; i++)
            {
                char c = sequence[i];
                if (inQuote)
                {
                    if (escapeNext)
                    {
                        escapeNext = false;
                    }
                    else
                    {
                        if (c == '\\')
                        {
                            escapeNext = true;
                        }
                        else if (c == '"')
                        {
                            inQuote = false;
                        }
                    }
                }
                else
                {
                    if (c == '"')
                    {
                        inQuote = true;
                    }
                    else if (c == ';')
                    {
                        values.Add(sequence.SubSequence(start, i));
                        start = i + 1;
                    }
                }
            }

            values.Add(sequence.SubSequence(start, sequence.Count));
            return values.ToArray();
        }
    }
}
