// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http
{
    using System.Threading.Tasks;
    using DotNetty.Common.Utilities;
    using DotNetty.Transport.Channels;

    public class HttpServerKeepAliveHandler : ChannelDuplexHandler
    {
        static readonly AsciiString MultipartPrefix = new AsciiString("multipart");

        bool persistentConnection = true;

        // Track pending responses to support client pipelining: 
        //https://tools.ietf.org/html/rfc7230#section-6.3.2
        int pendingResponses;

        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            var request = message as IHttpRequest;

            // read message and track if it was keepAlive
            if (request != null) {
                if (this.persistentConnection)
                {
                    this.pendingResponses += 1;
                    this.persistentConnection = HttpUtil.IsKeepAlive(request);
                }
            }

            base.ChannelRead(context, message);
        }

        public override Task WriteAsync(IChannelHandlerContext context, object message)
        {
            var response = (IHttpResponse)message;
            // modify message on way out to add headers if needed
            if (response != null)
            {
                this.TrackResponse(response);
                // Assume the response writer knows if they can persist or not and sets isKeepAlive on the response
                if (!HttpUtil.IsKeepAlive(response) || !IsSelfDefinedMessageLength(response))
                {
                    // No longer keep alive as the client can't tell when the message is done unless we close connection
                    this.pendingResponses = 0;
                    this.persistentConnection = false;
                }
                // Server might think it can keep connection alive, but we should fix response header if we know better
                if (!this.ShouldKeepAlive())
                {
                    HttpUtil.SetKeepAlive(response, false);
                }
            }

            if (response is ILastHttpContent && !this.ShouldKeepAlive())
            {
                return base.WriteAsync(context, message)
                    .ContinueWith(t => context.CloseAsync());
            }
            else
            {
                return base.WriteAsync(context, message);
            }
        }

        void TrackResponse(IHttpResponse response)
        {
            if (!IsInformational(response))
            {
                this.pendingResponses -= 1;
            }
        }

        bool ShouldKeepAlive() => this.pendingResponses != 0 || this.persistentConnection;

        static bool IsSelfDefinedMessageLength(IHttpResponse response) => 
            HttpUtil.IsContentLengthSet(response) 
            || HttpUtil.IsTransferEncodingChunked(response) 
            || IsMultipart(response) 
            || IsInformational(response) 
            || response.Status.Code == HttpResponseStatus.NoContent.Code;

        static bool IsInformational(IHttpResponse response) => 
            response.Status.CodeClass == HttpStatusClass.Informational;

        static bool IsMultipart(IHttpResponse response)
        {
            ICharSequence contentType = response.Headers.Get(HttpHeaderNames.ContentType);
            return contentType != null 
                && contentType.RegionMatches(true, 0, MultipartPrefix, 0, MultipartPrefix.Count);
        }
    }
}
