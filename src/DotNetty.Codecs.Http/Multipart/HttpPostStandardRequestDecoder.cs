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
    using DotNetty.Common.Utilities;

    public class HttpPostStandardRequestDecoder : IHttpPostRequestDecoder
    {
        readonly IHttpDataFactory factory;
        readonly IHttpRequest request;
        readonly Encoding encoding;
        readonly List<IPostHttpData> bodyListHttpData = new List<IPostHttpData>();
        readonly Dictionary<AsciiString, List<IPostHttpData>> bodyMapHttpData = 
            new Dictionary<AsciiString, List<IPostHttpData>>(CaseIgnoringComparator.Default);

        bool isLastChunk;
        IByteBuffer undecodedChunk;
        int bodyListHttpDataRank;

        MultiPartStatus currentStatus = MultiPartStatus.Notstarted;

        IAttribute currentAttribute;
        bool destroyed;

        int discardThreshold = HttpPostRequestDecoder.DefaultDiscardThreshold;

        public HttpPostStandardRequestDecoder(IHttpRequest request)
            : this(new DefaultHttpDataFactory(DefaultHttpDataFactory.MinSize), request, HttpConstants.DefaultEncoding)
        {
        }

        public HttpPostStandardRequestDecoder(IHttpDataFactory factory, IHttpRequest request)
            : this(factory, request, HttpConstants.DefaultEncoding)
        {
        }

        public HttpPostStandardRequestDecoder(IHttpDataFactory factory, IHttpRequest request, Encoding encoding)
        {
            Contract.Requires(factory != null);
            Contract.Requires(request != null);
            Contract.Requires(encoding != null);

            this.factory = factory;
            this.request = request;
            this.encoding = encoding;
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

        void CheckDestroyed()
        {
            if (this.destroyed)
            {
                throw new InvalidOperationException($"{StringUtil.SimpleClassName<HttpPostStandardRequestDecoder>()} was destroyed already");
            }
        }

        public bool IsMultipart
        {
            get
            {
                this.CheckDestroyed();
                return false;
            }
        }

        public int DiscardThreshold
        {
            get
            {
                return this.discardThreshold;
            }
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
                throw new NotEnoughDataDecoderException(nameof(HttpPostStandardRequestDecoder));
            }

            return this.bodyListHttpData;
        }

        public List<IPostHttpData> GetBodyDataList(AsciiString name)
        {
            this.CheckDestroyed();

            if (!this.isLastChunk)
            {
                throw new NotEnoughDataDecoderException(nameof(HttpPostStandardRequestDecoder));
            }

            return this.bodyMapHttpData[name];
        }

        public IPostHttpData GetBodyData(AsciiString name)
        {
            this.CheckDestroyed();

            if (!this.isLastChunk)
            {
                throw new NotEnoughDataDecoderException(nameof(HttpPostStandardRequestDecoder));
            }

            if (!this.bodyMapHttpData.TryGetValue(name, out List<IPostHttpData> list))
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

            if (content is ILastHttpContent) {
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
                    && this.bodyListHttpDataRank >= this.bodyListHttpData.Count) // OK except if end of list
                {
                    throw new EndOfDataDecoderException(nameof(HttpPostStandardRequestDecoder));
                }

                return this.bodyListHttpData.Count > 0
                    && this.bodyListHttpDataRank < this.bodyListHttpData.Count;
            }
        }

        public IPostHttpData Next()
        {
            this.CheckDestroyed();

            return this.HasNext 
                ? this.bodyListHttpData[this.bodyListHttpDataRank++] 
                : null;
        }

        public IPostHttpData CurrentPartialHttpData => this.currentAttribute;

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

            this.ParseBodyAttributes();
        }

        protected void AddHttpData(IPostHttpData data)
        {
            if (data == null)
            {
                return;
            }

            var name = new AsciiString(data.Name);
            if (!this.bodyMapHttpData.TryGetValue(name, out List<IPostHttpData> dataList))
            {
                dataList = new List<IPostHttpData>();
                this.bodyMapHttpData.Add(name, dataList);
            }

            dataList.Add(data);
            this.bodyListHttpData.Add(data);
        }

        void ParseBodyAttributesStandard()
        {
            int firstpos = this.undecodedChunk.ReaderIndex;
            int currentpos = firstpos;
            if (this.currentStatus == MultiPartStatus.Notstarted)
            {
                this.currentStatus = MultiPartStatus.Disposition;
            }
            bool contRead = true;

            try
            {
                int ampersandpos;
                while (this.undecodedChunk.IsReadable() && contRead)
                {
                    char read = (char)this.undecodedChunk.ReadByte();
                    currentpos++;

                    switch (this.currentStatus)
                    {
                        case MultiPartStatus.Disposition:// search '='
                            if (read == '=')
                            {
                                this.currentStatus = MultiPartStatus.Field;
                                int equalpos = currentpos - 1;
                                string key = DecodeAttribute(
                                    this.undecodedChunk.ToString(firstpos, equalpos - firstpos, this.encoding),
                                    this.encoding);
                                this.currentAttribute = this.factory.CreateAttribute(this.request, key);
                                firstpos = currentpos;
                            }
                            else if (read == '&')
                            { // special empty FIELD
                                this.currentStatus = MultiPartStatus.Disposition;
                                ampersandpos = currentpos - 1;
                                string key = DecodeAttribute(
                                    this.undecodedChunk.ToString(firstpos, ampersandpos - firstpos, this.encoding), 
                                    this.encoding);
                                this.currentAttribute = this.factory.CreateAttribute(this.request, key);
                                this.currentAttribute.Value = ""; // empty
                                this.AddHttpData(this.currentAttribute);
                                this.currentAttribute = null;
                                firstpos = currentpos;
                                contRead = true;
                            }
                            break;
                        case MultiPartStatus.Field:// search '&' or end of line
                            if (read == '&')
                            {
                                this.currentStatus = MultiPartStatus.Disposition;
                                ampersandpos = currentpos - 1;
                                this.SetFinalBuffer(this.undecodedChunk.Copy(firstpos, ampersandpos - firstpos));
                                firstpos = currentpos;
                                contRead = true;
                            }
                            else if (read == HttpConstants.CarriageReturn)
                            {
                                if (this.undecodedChunk.IsReadable())
                                {
                                    read = (char)this.undecodedChunk.ReadByte();
                                    currentpos++;
                                    if (read == HttpConstants.LineFeed)
                                    {
                                        this.currentStatus = MultiPartStatus.PreEpilogue;
                                        ampersandpos = currentpos - 2;
                                        this.SetFinalBuffer(this.undecodedChunk.Copy(firstpos, ampersandpos - firstpos));
                                        firstpos = currentpos;
                                        contRead = false;
                                    }
                                    else
                                    {
                                        // Error
                                        throw new ErrorDataDecoderException("Bad end of line");
                                    }
                                }
                                else
                                {
                                    currentpos--;
                                }
                            }
                            else if (read == HttpConstants.LineFeed)
                            {
                                this.currentStatus = MultiPartStatus.PreEpilogue;
                                ampersandpos = currentpos - 1;
                                this.SetFinalBuffer(this.undecodedChunk.Copy(firstpos, ampersandpos - firstpos));
                                firstpos = currentpos;
                                contRead = false;
                            }
                            break;
                        default:
                            // just stop
                            contRead = false;
                            break;
                    }
                }

                if (this.isLastChunk && this.currentAttribute != null)
                {
                    // special case
                    ampersandpos = currentpos;
                    if (ampersandpos > firstpos)
                    {
                        this.SetFinalBuffer(this.undecodedChunk.Copy(firstpos, ampersandpos - firstpos));
                    }
                    else if (!this.currentAttribute.Completed)
                    {
                        this.SetFinalBuffer(Unpooled.Empty);
                    }
                    firstpos = currentpos;
                    this.currentStatus = MultiPartStatus.Epilogue;
                    this.undecodedChunk.SetReaderIndex(firstpos);

                    return;
                }

                if (contRead && this.currentAttribute != null)
                {
                    // reset index except if to continue in case of FIELD getStatus
                    if (this.currentStatus == MultiPartStatus.Field)
                    {
                        this.currentAttribute.AddContent(
                            this.undecodedChunk.Copy(firstpos, currentpos - firstpos), false);
                        firstpos = currentpos;
                    }
                    this.undecodedChunk.SetReaderIndex(firstpos);
                }
                else
                {
                    // end of line or end of block so keep index to last valid position
                    this.undecodedChunk.SetReaderIndex(firstpos);
                }
            }
            catch (ErrorDataDecoderException)
            {
                // error while decoding
                this.undecodedChunk.SetReaderIndex(firstpos);
                throw;
            }
            catch (IOException e)
            {
                // error while decoding
                this.undecodedChunk.SetReaderIndex(firstpos);
                throw new ErrorDataDecoderException(e);
            }
        }

        void ParseBodyAttributes()
        {
            HttpPostBodyUtil.SeekAheadOptimize seekAhead;
            try
            {
                seekAhead = new HttpPostBodyUtil.SeekAheadOptimize(this.undecodedChunk);
            }
            catch (HttpPostBodyUtil.SeekAheadNoBackArrayException)
            {
                this.ParseBodyAttributesStandard();
                return;
            }

            int firstpos = this.undecodedChunk.ReaderIndex;
            int currentpos = firstpos;
            if (this.currentStatus == MultiPartStatus.Notstarted)
            {
                this.currentStatus = MultiPartStatus.Disposition;
            }
            try
            {
                bool contRead = true;
                int ampersandpos;
                while (seekAhead.Position < seekAhead.Limit)
                {
                    char read = (char)(seekAhead.Bytes[seekAhead.Position++] & 0xFF);
                    currentpos++;
                    switch (this.currentStatus)
                    {
                        case MultiPartStatus.Disposition: // search '='
                            if (read == '=')
                            {
                                this.currentStatus = MultiPartStatus.Field;
                                int equalpos = currentpos - 1;
                                string key = DecodeAttribute(
                                    this.undecodedChunk.ToString(firstpos, equalpos - firstpos, this.encoding),
                                    this.encoding);
                                this.currentAttribute = this.factory.CreateAttribute(this.request, key);
                                firstpos = currentpos;
                            }
                            else if (read == '&') // special empty FIELD
                            {
                                this.currentStatus = MultiPartStatus.Disposition;
                                ampersandpos = currentpos - 1;
                                string key = DecodeAttribute(
                                    this.undecodedChunk.ToString(firstpos, ampersandpos - firstpos, this.encoding),
                                    this.encoding);
                                this.currentAttribute = this.factory.CreateAttribute(this.request, key);
                                this.currentAttribute.Value = ""; // empty
                                this.AddHttpData(this.currentAttribute);
                                this.currentAttribute = null;
                                firstpos = currentpos;
                                contRead = true;
                            }
                            break;
                        case MultiPartStatus.Field: // search '&' or end of line
                            if (read == '&')
                            {
                                this.currentStatus = MultiPartStatus.Disposition;
                                ampersandpos = currentpos - 1;
                                this.SetFinalBuffer(this.undecodedChunk.Copy(firstpos, ampersandpos - firstpos));
                                firstpos = currentpos;
                                contRead = true;
                            }
                            else if (read == HttpConstants.CarriageReturn)
                            {
                                if (seekAhead.Position < seekAhead.Limit)
                                {
                                    read = (char)(seekAhead.Bytes[seekAhead.Position++] & 0xFF);
                                    currentpos++;
                                    if (read == HttpConstants.LineFeed)
                                    {
                                        this.currentStatus = MultiPartStatus.PreEpilogue;
                                        ampersandpos = currentpos - 2;
                                        seekAhead.SetReadPosition(0);
                                        this.SetFinalBuffer(this.undecodedChunk.Copy(firstpos, ampersandpos - firstpos));
                                        firstpos = currentpos;
                                        contRead = false;
                                        goto outer;
                                    }
                                    else
                                    {
                                        // Error
                                        seekAhead.SetReadPosition(0);
                                        throw new ErrorDataDecoderException("Bad end of line");
                                    }
                                }
                                else
                                {
                                    if (seekAhead.Limit > 0)
                                    {
                                        currentpos--;
                                    }
                                }
                            }
                            else if (read == HttpConstants.LineFeed)
                            {
                                this.currentStatus = MultiPartStatus.PreEpilogue;
                                ampersandpos = currentpos - 1;
                                seekAhead.SetReadPosition(0);
                                this.SetFinalBuffer(this.undecodedChunk.Copy(firstpos, ampersandpos - firstpos));
                                firstpos = currentpos;
                                contRead = false;
                                goto outer;
                            }
                            break;
                        default:
                            // just stop
                            seekAhead.SetReadPosition(0);
                            contRead = false;
                            goto outer;
                    }
                }

                outer:
                if(this.isLastChunk && this.currentAttribute != null)
                {
                    // special case
                    ampersandpos = currentpos;
                    if (ampersandpos > firstpos)
                    {
                        this.SetFinalBuffer(this.undecodedChunk.Copy(firstpos, ampersandpos - firstpos));
                    }
                    else if (!this.currentAttribute.Completed)
                    {
                        this.SetFinalBuffer(Unpooled.Empty);
                    }
                    firstpos = currentpos;
                    this.currentStatus = MultiPartStatus.Epilogue;
                    this.undecodedChunk.SetReaderIndex(firstpos);

                    return;
                }

                if (contRead && this.currentAttribute != null)
                {
                    // reset index except if to continue in case of FIELD getStatus
                    if (this.currentStatus == MultiPartStatus.Field)
                    {
                        this.currentAttribute.AddContent(
                            this.undecodedChunk.Copy(firstpos, currentpos - firstpos), false);
                        firstpos = currentpos;
                    }

                    this.undecodedChunk.SetReaderIndex(firstpos);
                }
                else
                {
                    // end of line or end of block so keep index to last valid position
                    this.undecodedChunk.SetReaderIndex(firstpos);
                }
            }
            catch (ErrorDataDecoderException)
            {
                this.undecodedChunk.SetReaderIndex(firstpos);
                throw;
            }
            catch (IOException e)
            {
                this.undecodedChunk.SetReaderIndex(firstpos);
                throw new ErrorDataDecoderException(e);
            }
            catch (ArgumentException e)
            {
                this.undecodedChunk.SetReaderIndex(firstpos);
                throw new ErrorDataDecoderException(e);
            }
        }

        void SetFinalBuffer(IByteBuffer buffer)
        {
            this.currentAttribute.AddContent(buffer, true);
            string value = DecodeAttribute(
                this.currentAttribute.GetByteBuffer().ToString(this.encoding), 
                this.encoding);
            this.currentAttribute.Value = value;
            this.AddHttpData(this.currentAttribute);
            this.currentAttribute = null;
        }

        static string DecodeAttribute(string s, Encoding encoding)
        {
            try
            {
                return QueryStringDecoder.DecodeComponent(s, encoding);
            }
            catch (ArgumentException exception)
            {
                throw new ErrorDataDecoderException($"Bad string: '{s}\'", exception);
            }
        }

        internal void SkipControlCharacters()
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
                catch (IndexOutOfRangeException exception)
                {
                    throw new NotEnoughDataDecoderException(exception);
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

        internal void SkipControlCharactersStandard()
        {
            while (true)
            {
                char c = (char)this.undecodedChunk.ReadByte();
                if (!CharUtil.IsISOControl(c) && !char.IsWhiteSpace(c))
                {
                    this.undecodedChunk.SetReaderIndex(this.undecodedChunk.ReaderIndex - 1);
                    break;
                }
            }
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
            for (int i = this.bodyListHttpDataRank; i < this.bodyListHttpData.Count; i++)
            {
                this.bodyListHttpData[i].Release();
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
    }
}
