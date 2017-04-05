// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http
{
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Text;
    using System.Threading.Tasks;
    using DotNetty.Buffers;
    using DotNetty.Common;
    using DotNetty.Common.Utilities;
    using DotNetty.Transport.Channels;

    public class HttpServerUpgradeHandler : HttpObjectAggregator
    {
        public interface ISourceCodec
        {
            void UpgradeFrom(IChannelHandlerContext ctx);
        }

        public interface IUpgradeCodec
        {
            ICollection<ICharSequence> RequiredUpgradeHeaders { get; }

            bool PrepareUpgradeResponse(IChannelHandlerContext ctx, IFullHttpRequest upgradeRequest, HttpHeaders upgradeHeaders);


            void UpgradeTo(IChannelHandlerContext ctx, IFullHttpRequest upgradeRequest);
        }

        public interface IUpgradeCodecFactory
        {
            IUpgradeCodec NewUpgradeCodec(ICharSequence protocol);
        }

        public sealed class UpgradeEvent : IReferenceCounted
        {
            internal UpgradeEvent(ICharSequence protocol, IFullHttpRequest upgradeRequest)
            {
                this.Protocol = protocol;
                this.UpgradeRequest = upgradeRequest;
            }

            public ICharSequence Protocol { get; }

            public IFullHttpRequest UpgradeRequest { get; }

            public int ReferenceCount => this.UpgradeRequest.ReferenceCount;

            public IReferenceCounted Retain()
            {
                this.UpgradeRequest.Retain();
                return this;
            }

            public IReferenceCounted Retain(int increment)
            {
                this.UpgradeRequest.Retain(increment);
                return this;
            }

            public IReferenceCounted Touch()
            {
                this.UpgradeRequest.Touch();
                return this;
            }

            public IReferenceCounted Touch(object hint)
            {
                this.UpgradeRequest.Touch(hint);
                return this;
            }

            public bool Release() => this.UpgradeRequest.Release();

            public bool Release(int decrement) => this.UpgradeRequest.Release(decrement);

            public override string ToString() => $"UpgradeEvent [protocol={this.Protocol}, upgradeRequest={this.UpgradeRequest}]";
        }

        readonly ISourceCodec sourceCodec;
        readonly IUpgradeCodecFactory upgradeCodecFactory;
        bool handlingUpgrade;

        public HttpServerUpgradeHandler(ISourceCodec sourceCodec, IUpgradeCodecFactory upgradeCodecFactory, int maxContentLength = 0) 
            : base(maxContentLength)
        {
            Contract.Requires(sourceCodec != null);
            Contract.Requires(upgradeCodecFactory != null);

            this.sourceCodec = sourceCodec;
            this.upgradeCodecFactory = upgradeCodecFactory;
        }

        protected override void Decode(IChannelHandlerContext context, IHttpObject message, List<object> output)
        {
            // Determine if we're already handling an upgrade request or just starting a new one.
            this.handlingUpgrade |= IsUpgradeRequest(message);
            if (!this.handlingUpgrade)
            {
                // Not handling an upgrade request, just pass it to the next handler.
                ReferenceCountUtil.Retain(message);
                output.Add(message);
                return;
            }

            var fullRequest = message as IFullHttpRequest;
            if (fullRequest != null)
            {
                ReferenceCountUtil.Retain(fullRequest);
                output.Add(fullRequest);
            }
            else
            {
                // Call the base class to handle the aggregation of the full request.
                base.Decode(context, message, output);
                if (output.Count == 0)
                {
                    // The full request hasn't been created yet, still awaiting more data.
                    return;
                }

                // Finished aggregating the full request, get it from the output list.
                Contract.Assert(output.Count == 1);
                this.handlingUpgrade = false;
                fullRequest = (IFullHttpRequest)output[0];
            }

            if (this.Upgrade(context, fullRequest))
            {
                // The upgrade was successful, remove the message from the output list
                // so that it's not propagated to the next handler. This request will
                // be propagated as a user event instead.
                output.Clear();
            }

            // The upgrade did not succeed, just allow the full request to propagate to the
            // next handler.
        }

        static bool IsUpgradeRequest(IHttpObject msg) => (msg as IHttpRequest)?.Headers.Get(HttpHeaderNames.Upgrade) != null;

        bool Upgrade(IChannelHandlerContext ctx, IFullHttpRequest request)
        {
            // Select the best protocol based on those requested in the UPGRADE header.
            List<ICharSequence> requestedProtocols = SplitHeader(request.Headers.Get(HttpHeaderNames.Upgrade));
            int numRequestedProtocols = requestedProtocols.Count;

            IUpgradeCodec upgradeCodec = null;
            ICharSequence upgradeProtocol = null;
            for (int i = 0; i < numRequestedProtocols; i++)
            {
                ICharSequence p = requestedProtocols[i];
                IUpgradeCodec c = this.upgradeCodecFactory.NewUpgradeCodec(p);
                if (c != null)
                {
                    upgradeProtocol = p;
                    upgradeCodec = c;
                    break;
                }
            }

            if (upgradeCodec == null)
            {
                // None of the requested protocols are supported, don't upgrade.
                return false;
            }

            // Make sure the CONNECTION header is present.
            ICharSequence connectionHeader = request.Headers.Get(HttpHeaderNames.Connection);
            if (connectionHeader == null)
            {
                return false;
            }

            // Make sure the CONNECTION header contains UPGRADE as well as all protocol-specific headers.
            ICollection<ICharSequence> requiredHeaders = upgradeCodec.RequiredUpgradeHeaders;
            List<ICharSequence> values = SplitHeader(connectionHeader);
            if (!AsciiString.ContainsContentEqualsIgnoreCase(values, HttpHeaderNames.Upgrade) 
                || !AsciiString.ContainsAllContentEqualsIgnoreCase(values, requiredHeaders))
            {
                return false;
            }

            // Ensure that all required protocol-specific headers are found in the request.
            foreach (ICharSequence requiredHeader in requiredHeaders)
            {
                if (!request.Headers.Contains(requiredHeader))
                {
                    return false;
                }
            }

            // Prepare and send the upgrade response. Wait for this write to complete before upgrading,
            // since we need the old codec in-place to properly encode the response.
            IFullHttpResponse upgradeResponse = CreateUpgradeResponse(upgradeProtocol);
            if (!upgradeCodec.PrepareUpgradeResponse(ctx, request, upgradeResponse.Headers))
            {
                return false;
            }

            // Create the user event to be fired once the upgrade completes.
            var upgradeEvent = new UpgradeEvent(upgradeProtocol, request);
            IUpgradeCodec finalUpgradeCodec = upgradeCodec;
            ctx.WriteAndFlushAsync(upgradeResponse)
                .ContinueWith(t =>
                {
                    try
                    {
                        if (t.Status == TaskStatus.RanToCompletion)
                        {
                            // Perform the upgrade to the new protocol.
                            this.sourceCodec.UpgradeFrom(ctx);
                            finalUpgradeCodec.UpgradeTo(ctx, request);

                            // Notify that the upgrade has occurred. Retain the event to offset
                            // the release() in the finally block.
                            ctx.FireUserEventTriggered(upgradeEvent.Retain());

                            // Remove this handler from the pipeline.
                            ctx.Channel.Pipeline.Remove(this);
                        }
                        else
                        {
                            ctx.Channel.CloseAsync();
                        }
                    }
                    finally
                    {
                        upgradeEvent.Release();
                    }
                });

            return true;
        }

        static IFullHttpResponse CreateUpgradeResponse(ICharSequence upgradeProtocol)
        {
            var res = new DefaultFullHttpResponse(HttpVersion.Http11, HttpResponseStatus.SwitchingProtocols, Unpooled.Empty, false);
            res.Headers.Add(HttpHeaderNames.Connection, HttpHeaderValues.Upgrade);
            res.Headers.Add(HttpHeaderNames.Upgrade, upgradeProtocol);
            res.Headers.Add(HttpHeaderNames.ContentLength, HttpHeaderValues.Zero);

            return res;
        }

        static List<ICharSequence> SplitHeader(ICharSequence header)
        {
            var builder = new StringBuilder(header.Count);
            var protocols = new List<ICharSequence>(4);
            foreach (char c in header)
            {
                if (char.IsWhiteSpace(c))
                {
                    // Don't include any whitespace.
                    continue;
                }
                if (c == ',')
                {
                    // Add the string and reset the builder for the next protocol.
                    protocols.Add(new AsciiString(builder.ToString()));
                    builder.Length = 0;
                }
                else
                {
                    builder.Append(c);
                }
            }

            // Add the last protocol
            if (builder.Length > 0)
            {
                protocols.Add(new AsciiString(builder.ToString()));
            }

            return protocols;
        }
    }
}
