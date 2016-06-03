// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http {
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics.Contracts;
    using System.Net;
    using System.Text;
    using DotNetty.Common.Utilities;
    using DotNetty.Transport.Channels;

    /**
 * Client-side handler for handling an HTTP upgrade handshake to another protocol. When the first
 * HTTP request is sent, this handler will add all appropriate headers to perform an upgrade to the
 * new protocol. If the upgrade fails (i.e. response is not 101 Switching Protocols), this handler
 * simply removes itself from the pipeline. If the upgrade is successful, upgrades the pipeline to
 * the new protocol.
 */
public class HttpClientUpgradeHandler : HttpObjectAggregator, ChannelOutboundHandler {

    /**
     * User events that are fired to notify about upgrade status.
     */
    public enum UpgradeEvent {
        /**
         * The Upgrade request was sent to the server.
         */
        UPGRADE_ISSUED,

        /**
         * The Upgrade to the new protocol was successful.
         */
        UPGRADE_SUCCESSFUL,

        /**
         * The Upgrade was unsuccessful due to the server not issuing
         * with a 101 Switching Protocols response.
         */
        UPGRADE_REJECTED
    }

    /**
     * The source codec that is used in the pipeline initially.
     */
    public interface SourceCodec {

        /**
         * Removes or disables the encoder of this codec so that the {@link UpgradeCodec} can send an initial greeting
         * (if any).
         */
        void prepareUpgradeFrom(IChannelHandlerContext ctx);

        /**
         * Removes this codec (i.e. all associated handlers) from the pipeline.
         */
        void upgradeFrom(IChannelHandlerContext ctx);
    }

    /**
     * A codec that the source can be upgraded to.
     */
    public interface UpgradeCodec {
        /**
         * Returns the name of the protocol supported by this codec, as indicated by the {@code 'UPGRADE'} header.
         */
        CharSequence protocol();

        /**
         * Sets any protocol-specific headers required to the upgrade request. Returns the names of
         * all headers that were added. These headers will be used to populate the CONNECTION header.
         */
        Collection<CharSequence> setUpgradeHeaders(IChannelHandlerContext ctx, HttpRequest upgradeRequest);

        /**
         * Performs an HTTP protocol upgrade from the source codec. This method is responsible for
         * adding all handlers required for the new protocol.
         *
         * @param ctx the context for the current handler.
         * @param upgradeResponse the 101 Switching Protocols response that indicates that the server
         *            has switched to this protocol.
         */
        void upgradeTo(IChannelHandlerContext ctx, FullHttpResponse upgradeResponse) ;
    }

    private readonly SourceCodec sourceCodec;
    private readonly UpgradeCodec upgradeCodec;
    private bool upgradeRequested;

    /**
     * Constructs the client upgrade handler.
     *
     * @param sourceCodec the codec that is being used initially.
     * @param upgradeCodec the codec that the client would like to upgrade to.
     * @param maxContentLength the maximum length of the aggregated content.
     */
    public HttpClientUpgradeHandler(SourceCodec sourceCodec, UpgradeCodec upgradeCodec,
                                    int maxContentLength) {
        base(maxContentLength);
        if (sourceCodec == null) {
            throw new ArgumentNullException(nameof(sourceCodec");
        }
        if (upgradeCodec == null) {
            throw new ArgumentNullException(nameof(upgradeCodec");
        }
        this.sourceCodec = sourceCodec;
        this.upgradeCodec = upgradeCodec;
    }

    // @Override
    public void bind(IChannelHandlerContext ctx, SocketAddress localAddress, ChannelPromise promise)  {
        ctx.bind(localAddress, promise);
    }

    // @Override
    public void connect(IChannelHandlerContext ctx, SocketAddress remoteAddress, SocketAddress localAddress,
                        ChannelPromise promise)  {
        ctx.connect(remoteAddress, localAddress, promise);
    }

    // @Override
    public void disconnect(IChannelHandlerContext ctx, ChannelPromise promise)  {
        ctx.disconnect(promise);
    }

    // @Override
    public void close(IChannelHandlerContext ctx, ChannelPromise promise)  {
        ctx.close(promise);
    }

    // @Override
    public void deregister(IChannelHandlerContext ctx, ChannelPromise promise)  {
        ctx.deregister(promise);
    }

    // @Override
    public void read(IChannelHandlerContext ctx)  {
        ctx.Read();
    }

    // @Override
    public void write(IChannelHandlerContext ctx, object msg, ChannelPromise promise)
             {
        if (!(msg is HttpRequest)) {
            ctx.write(msg, promise);
            return;
        }

        if (upgradeRequested) {
            promise.setFailure(new IllegalStateException(
                    "Attempting to write HTTP request with upgrade in progress"));
            return;
        }

        upgradeRequested = true;
        setUpgradeRequestHeaders(ctx, (HttpRequest) msg);

        // Continue writing the request.
        ctx.write(msg, promise);

        // Notify that the upgrade request was issued.
        ctx.fireUserEventTriggered(UpgradeEvent.UPGRADE_ISSUED);
        // Now we wait for the next HTTP response to see if we switch protocols.
    }

    // @Override
    public void flush(IChannelHandlerContext ctx)  {
        ctx.Flush();
    }

    // @Override
    protected void decode(IChannelHandlerContext ctx, HttpObject msg, List<object> output)
             {
        FullHttpResponse response = null;
        try {
            if (!upgradeRequested) {
                throw new IllegalStateException("Read HTTP response withoutput requesting protocol switch");
            }

            if (msg is FullHttpResponse) {
                response = (FullHttpResponse) msg;
                // Need to retain since the base class will release after returning from this method.
                response.retain();
                output.Add(response);
            } else {
                // Call the base class to handle the aggregation of the full request.
                base.Decode(ctx, msg, output);
                if (output.Count == 0) {
                    // The full request hasn't been created yet, still awaiting more data.
                    return;
                }

                Contract.Assert(output.Count == 1);
                response = (FullHttpResponse) output[0];
            }

            if (!SWITCHING_PROTOCOLS.Equals(response.status())) {
                // The server does not support the requested protocol, just remove this handler
                // and continue processing HTTP.
                // NOTE: not releasing the response since we're letting it propagate to the
                // next handler.
                ctx.FireUserEventTriggered(UpgradeEvent.UPGRADE_REJECTED);
                removeThisHandler(ctx);
                return;
            }

            CharSequence upgradeHeader = response.headers().get(HttpHeaderNames.UPGRADE);
            if (upgradeHeader != null && !AsciiString.contentEqualsIgnoreCase(upgradeCodec.protocol(), upgradeHeader)) {
                throw new IllegalStateException(
                        "Switching Protocols response with unexpected UPGRADE protocol: " + upgradeHeader);
            }

            // Upgrade to the new protocol.
            sourceCodec.prepareUpgradeFrom(ctx);
            upgradeCodec.upgradeTo(ctx, response);

            // Notify that the upgrade to the new protocol completed successfully.
            ctx.FireUserEventTriggered(UpgradeEvent.UPGRADE_SUCCESSFUL);

            // We guarantee UPGRADE_SUCCESSFUL event will be arrived at the next handler
            // before http2 setting frame and http response.
            sourceCodec.upgradeFrom(ctx);

            // We switched protocols, so we're done with the upgrade response.
            // Release it and clear it from the outputput.
            response.release();
            output.clear();
            removeThisHandler(ctx);
        } catch (Exception t) {
            release(response);
            ctx.FireExceptionCaught(t);
            removeThisHandler(ctx);
        }
    }

    private static void removeThisHandler(IChannelHandlerContext ctx) {
        ctx.Channel.Pipeline.Remove(ctx.Name);
    }

    /**
     * Adds all upgrade request headers necessary for an upgrade to the supported protocols.
     */
    private void setUpgradeRequestHeaders(IChannelHandlerContext ctx, HttpRequest request) {
        // Set the UPGRADE header on the request.
        request.headers().set(HttpHeaderNames.UPGRADE, upgradeCodec.protocol());

        // Add all protocol-specific headers to the request.
        Set<CharSequence> connectionParts = new LinkedHashSet<CharSequence>(2);
        connectionParts.addAll(upgradeCodec.setUpgradeHeaders(ctx, request));

        // Set the CONNECTION header from the set of all protocol-specific headers that were added.
        StringBuilder builder = new StringBuilder();
        for (CharSequence part : connectionParts) {
            builder.append(part);
            builder.append(',');
        }
        builder.append(HttpHeaderValues.UPGRADE);
        request.headers().set(HttpHeaderNames.CONNECTION, builder.ToString());
    }
}
