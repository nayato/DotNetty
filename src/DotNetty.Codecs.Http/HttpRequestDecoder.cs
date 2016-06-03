// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http
{
    /**
         * Decodes {@link IByteBuffer}s into {@link HttpRequest}s and {@link HttpContent}s.
         *
         * <h3>Parameters that prevents excessive memory consumption</h3>
         * <table border="1">
         * <tr>
         * <th>Name</th><th>Meaning</th>
         * </tr>
         * <tr>
         * <td>{@code maxInitialLineLength}</td>
         * <td>The maximum length of the initial line (e.g. {@code "GET / HTTP/1.0"})
         *     If the length of the initial line exceeds this value, a
         *     {@link TooLongFrameException} will be raised.</td>
         * </tr>
         * <tr>
         * <td>{@code maxHeaderSize}</td>
         * <td>The maximum length of all headers.  If the sum of the length of each
         *     header exceeds this value, a {@link TooLongFrameException} will be raised.</td>
         * </tr>
         * <tr>
         * <td>{@code maxChunkSize}</td>
         * <td>The maximum length of the content or each chunk.  If the content length
         *     exceeds this value, the transfer encoding of the decoded request will be
         *     converted to 'chunked' and the content will be split into multiple
         *     {@link HttpContent}s.  If the transfer encoding of the HTTP request is
         *     'chunked' already, each chunk will be split into smaller chunks if the
         *     length of the chunk exceeds this value.  If you prefer not to handle
         *     {@link HttpContent}s in your handler, insert {@link HttpObjectAggregator}
         *     after this decoder in the {@link ChannelPipeline}.</td>
         * </tr>
         * </table>
         */

    public class HttpRequestDecoder : HttpObjectDecoder
    {
        /**
                 * Creates a new instance with the default
                 * {@code maxInitialLineLength (4096)}, {@code maxHeaderSize (8192)}, and
                 * {@code maxChunkSize (8192)}.
                 */

        public HttpRequestDecoder()
        {
        }

        /**
         * Creates a new instance with the specified parameters.
         */

        public HttpRequestDecoder(
            int maxInitialLineLength, int maxHeaderSize, int maxChunkSize)
        {
            base(maxInitialLineLength, maxHeaderSize, maxChunkSize, true);
        }

        public HttpRequestDecoder(
            int maxInitialLineLength, int maxHeaderSize, int maxChunkSize, bool validateHeaders)
        {
            base(maxInitialLineLength, maxHeaderSize, maxChunkSize, true, validateHeaders);
        }

        public HttpRequestDecoder(
            int maxInitialLineLength, int maxHeaderSize, int maxChunkSize, bool validateHeaders,
            int initialBufferSize)
        {
            base(maxInitialLineLength, maxHeaderSize, maxChunkSize, true, validateHeaders, initialBufferSize);
        }

        // @Override
        protected HttpMessage createMessage(string[] initialLine)
        {
            return new DefaultHttpRequest(
                HttpVersion.valueOf(initialLine[2]),
                HttpMethod.valueOf(initialLine[0]), initialLine[1], this.validateHeaders);
        }

        // @Override
        protected HttpMessage createInvalidMessage()
        {
            return new DefaultFullHttpRequest(HttpVersion.HTTP_1_0, HttpMethod.GET, "/bad-request", this.validateHeaders);
        }

        // @Override
        protected bool isDecodingRequest()
        {
            return true;
        }
    }
}