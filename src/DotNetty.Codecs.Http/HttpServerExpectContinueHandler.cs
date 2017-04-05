// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http
{
    using System.Threading.Tasks;
    using DotNetty.Buffers;
    using DotNetty.Common.Utilities;
    using DotNetty.Transport.Channels;

    public class HttpServerExpectContinueHandler : ChannelHandlerAdapter
    {
        static readonly IFullHttpResponse ExpectationFailed = new DefaultFullHttpResponse(
            HttpVersion.Http11, HttpResponseStatus.ExpectationFailed, Unpooled.Empty);

        static readonly IFullHttpResponse Accept = new DefaultFullHttpResponse(
            HttpVersion.Http11, HttpResponseStatus.Continue, Unpooled.Empty);

        protected virtual IHttpResponse AcceptMessage(IHttpRequest request) => 
            (IHttpResponse)Accept.Duplicate().Retain();

        protected virtual IHttpResponse RejectResponse(IHttpRequest request) => 
            (IHttpResponse)ExpectationFailed.Duplicate().Retain();

        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            var req = message as IHttpRequest;
            if (req != null)
            {
                if (HttpUtil.Is100ContinueExpected(req))
                {
                    IHttpResponse accept = this.AcceptMessage(req);
                    if (accept == null)
                    {
                        // the expectation failed so we refuse the request.
                        IHttpResponse rejection = this.RejectResponse(req);
                        ReferenceCountUtil.Release(message);
                        context.WriteAndFlushAsync(rejection)
                            .ContinueWith(t => context.CloseAsync());
                        return;
                    }
                    context.WriteAndFlushAsync(accept)
                        .ContinueWith(t =>
                        {
                            if (t.Status != TaskStatus.RanToCompletion)
                            {
                                context.CloseAsync();
                            }
                        });
                    req.Headers.Remove(HttpHeaderNames.Expect);
                }

                base.ChannelRead(context, message);
            }
        }
    }
}
