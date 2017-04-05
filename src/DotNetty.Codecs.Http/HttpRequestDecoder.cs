// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http
{
    using DotNetty.Common.Internal;
    using DotNetty.Common.Utilities;

    public class HttpRequestDecoder : HttpObjectDecoder
    {
        public HttpRequestDecoder(int maxInitialLineLength, int maxHeaderSize, int maxChunkSize, bool validateHeaders, int initialBufferSize) 
            : this(maxInitialLineLength, maxHeaderSize, maxChunkSize, true, validateHeaders, initialBufferSize)
        {
        }

        public HttpRequestDecoder(int maxInitialLineLength = 4096, int maxHeaderSize = 8192, int maxChunkSize = 8192, 
            bool chunkedSupported = true, bool validateHeaders = true, int initialBufferSize = 128) 
            : base(maxInitialLineLength, maxHeaderSize, maxChunkSize, chunkedSupported, validateHeaders, initialBufferSize)
        {
        }

        protected override bool IsDecodingRequest() => true;

        protected override IHttpMessage CreateMessage(ICharSequence[] initialLine) => 
            new DefaultHttpRequest(HttpVersion.ValueOf(initialLine[2]), HttpMethod.ValueOf(initialLine[0]), initialLine[1], this.ValidateHeaders);

        protected override IHttpMessage CreateInvalidMessage() => 
            new DefaultFullHttpRequest(HttpVersion.Http10, HttpMethod.Get, "/bad-request", this.ValidateHeaders);
    }
}
