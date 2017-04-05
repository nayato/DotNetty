// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http
{
    using DotNetty.Buffers;

    public class HttpResponseEncoder : HttpObjectEncoder<IHttpResponse>
    {
        public override bool AcceptOutboundMessage(object msg) => 
            base.AcceptOutboundMessage(msg) && !(msg is IHttpRequest);

        protected internal override void EncodeInitialLine(IByteBuffer buf, IHttpResponse response)
        {
            response.ProtocolVersion.Encode(buf);
            buf.WriteByte(HttpConstants.HorizontalSpace);
            response.Status.Encode(buf);
            buf.WriteShort(HttpConstants.CrlfShort);
        }
    }
}
