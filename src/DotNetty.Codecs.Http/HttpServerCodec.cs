// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http
{
    using System.Collections.Generic;
    using DotNetty.Buffers;
    using DotNetty.Transport.Channels;

    public class HttpServerCodec : CombinedChannelDuplexHandler<HttpRequestDecoder, HttpResponseEncoder>,
        HttpServerUpgradeHandler.ISourceCodec
    {
        /** A queue that is used for correlating a request and a response. */
        readonly Queue<HttpMethod> queue = new Queue<HttpMethod>();

        public HttpServerCodec(int maxInitialLineLength = 4096, int maxHeaderSize = 8192, int maxChunkSize = 8192, bool validateHeaders = true)
        {
            this.Init(new Decoder(this, maxInitialLineLength, maxHeaderSize, maxChunkSize, validateHeaders), new Encoder(this));
        }

        public HttpServerCodec(int maxInitialLineLength, int maxHeaderSize, int maxChunkSize, bool validateHeaders, int initialBufferSize)
        {
            this.Init(new Decoder(this, maxInitialLineLength, maxHeaderSize, maxChunkSize, validateHeaders, initialBufferSize), new Encoder(this));
        }

        public void UpgradeFrom(IChannelHandlerContext ctx) => ctx.Channel.Pipeline.Remove(this);

        sealed class Decoder : HttpRequestDecoder
        {
            readonly HttpServerCodec serverCodec;

            public Decoder(HttpServerCodec serverCodec, int maxInitialLineLength, int maxHeaderSize, int maxChunkSize, bool validateHeaders = true)
                : base(maxInitialLineLength, maxHeaderSize, maxChunkSize, validateHeaders)
            {
                this.serverCodec = serverCodec;
            }

            public Decoder(HttpServerCodec serverCodec, int maxInitialLineLength, int maxHeaderSize, int maxChunkSize, bool validateHeaders, int initialBufferSize)
                : base(maxInitialLineLength, maxHeaderSize, maxChunkSize, validateHeaders, initialBufferSize)
            {
                this.serverCodec = serverCodec;
            }

            protected override void Decode(IChannelHandlerContext context, IByteBuffer buffer, List<object> output)
            {
                int oldSize = output.Count;
                base.Decode(context, buffer, output);
                int size = output.Count;
                for (int i = oldSize; i < size; i++)
                {
                    var request = output[i] as IHttpRequest;
                    if (request != null)
                    {
                        this.serverCodec.queue.Enqueue(request.Method);
                    }
                }
            }
        }

        sealed class Encoder : HttpResponseEncoder
        {
            readonly HttpServerCodec serverCodec;

            public Encoder(HttpServerCodec serverCodec)
            {
                this.serverCodec = serverCodec;
            }

            protected override bool IsContentAlwaysEmpty(IHttpResponse msg) => 
                this.serverCodec.queue.Count > 0 && HttpMethod.Head.Equals(this.serverCodec.queue.Dequeue());
        }
    }
}
