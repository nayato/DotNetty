// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http {
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Text;
    using DotNetty.Buffers;
    using DotNetty.Common;
    using DotNetty.Common.Utilities;
    using DotNetty.Transport.Channels;

    /**
 * A server-side handler that receives HTTP requests and optionally performs a protocol switch if
 * the requested protocol is supported. Once an upgrade is performed, this handler removes itself
 * from the pipeline.
 */
public class HttpServerUpgradeHandler : HttpObjectAggregator {

    /**
     * The source codec that is used in the pipeline initially.
     */
    public interface SourceCodec {
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
         * Gets all protocol-specific headers required by this protocol for a successful upgrade.
         * Any supplied header will be required to appear in the {@link HttpHeaderNames#CONNECTION} header as well.
         */
        Collection<CharSequence> requiredUpgradeHeaders();

        /**
         * Prepares the {@code upgradeHeaders} for a protocol update based upon the contents of {@code upgradeRequest}.
         * This method returns a bool value to proceed or abort the upgrade in progress. If {@code false} is
         * returned, the upgrade is aborted and the {@code upgradeRequest} will be passed through the inbound pipeline
         * as if no upgrade was performed. If {@code true} is returned, the upgrade will proceed to the next
         * step which invokes {@link #upgradeTo}. When returning {@code true}, you can add headers to
         * the {@code upgradeHeaders} so that they are added to the 101 Switching protocols response.
         */
        bool prepareUpgradeResponse(IChannelHandlerContext ctx, FullHttpRequest upgradeRequest,
                                    HttpHeaders upgradeHeaders);

        /**
         * Performs an HTTP protocol upgrade from the source codec. This method is responsible for
         * adding all handlers required for the new protocol.
         *
         * @param ctx the context for the current handler.
         * @param upgradeRequest the request that triggered the upgrade to this protocol.
         */
        void upgradeTo(IChannelHandlerContext ctx, FullHttpRequest upgradeRequest);
    }

    /**
     * Creates a new {@link UpgradeCodec} for the requested protocol name.
     */
    public interface UpgradeCodecFactory {
        /**
         * Invoked by {@link HttpServerUpgradeHandler} for all the requested protocol names in the order of
         * the client preference. The first non-{@code null} {@link UpgradeCodec} returned by this method
         * will be selected.
         *
         * @return a new {@link UpgradeCodec}, or {@code null} if the specified protocol name is not supported
         */
        UpgradeCodec newUpgradeCodec(CharSequence protocol);
    }

    /**
     * User event that is fired to notify about the completion of an HTTP upgrade
     * to another protocol. Contains the original upgrade request so that the response
     * (if required) can be sent using the new protocol.
     */
    public static sealed class UpgradeEvent : IReferenceCounted {
        private readonly CharSequence protocol;
        private readonly FullHttpRequest upgradeRequest;

        UpgradeEvent(CharSequence protocol, FullHttpRequest upgradeRequest) {
            this.protocol = protocol;
            this.upgradeRequest = upgradeRequest;
        }

        /**
         * The protocol that the channel has been upgraded to.
         */
        public CharSequence protocol() {
            return protocol;
        }

        /**
         * Gets the request that triggered the protocol upgrade.
         */
        public FullHttpRequest upgradeRequest() {
            return upgradeRequest;
        }

        // @Override
        public int refCnt() {
            return upgradeRequest.refCnt();
        }

        // @Override
        public UpgradeEvent retain() {
            upgradeRequest.retain();
            return this;
        }

        // @Override
        public UpgradeEvent retain(int increment) {
            upgradeRequest.retain(increment);
            return this;
        }

        // @Override
        public UpgradeEvent touch() {
            upgradeRequest.touch();
            return this;
        }

        // @Override
        public UpgradeEvent touch(object hint) {
            upgradeRequest.touch(hint);
            return this;
        }

        // @Override
        public bool release() {
            return upgradeRequest.release();
        }

        // @Override
        public bool release(int decrement) {
            return upgradeRequest.release(decrement);
        }

        // @Override
        public string toString() {
            return "UpgradeEvent [protocol=" + protocol + ", upgradeRequest=" + upgradeRequest + ']';
        }
    }

    private readonly SourceCodec sourceCodec;
    private readonly UpgradeCodecFactory upgradeCodecFactory;
    private bool handlingUpgrade;

    /**
     * Constructs the upgrader with the supported codecs.
     * <p>
     * The handler instantiated by this constructor will reject an upgrade request with non-empty content.
     * It should not be a concern because an upgrade request is most likely a GET request.
     * If you have a client that sends a non-GET upgrade request, please consider using
     * {@link #HttpServerUpgradeHandler(SourceCodec, UpgradeCodecFactory, int)} to specify the maximum
     * length of the content of an upgrade request.
     * </p>
     *
     * @param sourceCodec the codec that is being used initially
     * @param upgradeCodecFactory the factory that creates a new upgrade codec
     *                            for one of the requested upgrade protocols
     */
    public HttpServerUpgradeHandler(SourceCodec sourceCodec, UpgradeCodecFactory upgradeCodecFactory) {
        this(sourceCodec, upgradeCodecFactory, 0);
    }

    /**
     * Constructs the upgrader with the supported codecs.
     *
     * @param sourceCodec the codec that is being used initially
     * @param upgradeCodecFactory the factory that creates a new upgrade codec
     *                            for one of the requested upgrade protocols
     * @param maxContentLength the maximum length of the content of an upgrade request
     */
    public HttpServerUpgradeHandler(
            SourceCodec sourceCodec, UpgradeCodecFactory upgradeCodecFactory, int maxContentLength) {
        base(maxContentLength);

        this.sourceCodec = checkNotNull(sourceCodec, "sourceCodec");
        this.upgradeCodecFactory = checkNotNull(upgradeCodecFactory, "upgradeCodecFactory");
    }

    // @Override
    protected void decode(IChannelHandlerContext ctx, HttpObject msg, List<object> output)
             {
        // Determine if we're already handling an upgrade request or just starting a new one.
        handlingUpgrade |= isUpgradeRequest(msg);
        if (!handlingUpgrade) {
            // Not handling an upgrade request, just pass it to the next handler.
            ReferenceCountUtil.retain(msg);
            output.add(msg);
            return;
        }

        FullHttpRequest fullRequest;
        if (msg is FullHttpRequest) {
            fullRequest = (FullHttpRequest) msg;
            ReferenceCountUtil.retain(msg);
            output.add(msg);
        } else {
            // Call the base class to handle the aggregation of the full request.
            base.decode(ctx, msg, output);
            if (output.isEmpty()) {
                // The full request hasn't been created yet, still awaiting more data.
                return;
            }

            // Finished aggregating the full request, get it from the outputput list.
            assert output.size() == 1;
            handlingUpgrade = false;
            fullRequest = (FullHttpRequest) output.get(0);
        }

        if (upgrade(ctx, fullRequest)) {
            // The upgrade was successful, remove the message from the outputput list
            // so that it's not propagated to the next handler. This request will
            // be propagated as a user event instead.
            output.clear();
        }

        // The upgrade did not succeed, just allow the full request to propagate to the
        // next handler.
    }

    /**
     * Determines whether or not the message is an HTTP upgrade request.
     */
    private static bool isUpgradeRequest(HttpObject msg) {
        return msg is HttpRequest && ((HttpRequest) msg).headers().get(HttpHeaderNames.UPGRADE) != null;
    }

    /**
     * Attempts to upgrade to the protocol(s) identified by the {@link HttpHeaderNames#UPGRADE} header (if provided
     * in the request).
     *
     * @param ctx the context for this handler.
     * @param request the HTTP request.
     * @return {@code true} if the upgrade occurred, otherwise {@code false}.
     */
    private bool upgrade(IChannelHandlerContext ctx, FullHttpRequest request) {
        // Select the best protocol based on those requested in the UPGRADE header.
        readonly List<CharSequence> requestedProtocols = splitHeader(request.headers().get(HttpHeaderNames.UPGRADE));
        readonly int numRequestedProtocols = requestedProtocols.size();
        UpgradeCodec upgradeCodec = null;
        CharSequence upgradeProtocol = null;
        for (int i = 0; i < numRequestedProtocols; i ++) {
            readonly CharSequence p = requestedProtocols.get(i);
            readonly UpgradeCodec c = upgradeCodecFactory.newUpgradeCodec(p);
            if (c != null) {
                upgradeProtocol = p;
                upgradeCodec = c;
                break;
            }
        }

        if (upgradeCodec == null) {
            // None of the requested protocols are supported, don't upgrade.
            return false;
        }

        // Make sure the CONNECTION header is present.
        CharSequence connectionHeader = request.headers().get(HttpHeaderNames.CONNECTION);
        if (connectionHeader == null) {
            return false;
        }

        // Make sure the CONNECTION header contains UPGRADE as well as all protocol-specific headers.
        Collection<CharSequence> requiredHeaders = upgradeCodec.requiredUpgradeHeaders();
        List<CharSequence> values = splitHeader(connectionHeader);
        if (!containsContentEqualsIgnoreCase(values, HttpHeaderNames.UPGRADE) ||
            !containsAllContentEqualsIgnoreCase(values, requiredHeaders)) {
            return false;
        }

        // Ensure that all required protocol-specific headers are found in the request.
        for (CharSequence requiredHeader : requiredHeaders) {
            if (!request.headers().contains(requiredHeader)) {
                return false;
            }
        }

        // Prepare and send the upgrade response. Wait for this write to complete before upgrading,
        // since we need the old codec in-place to properly encode the response.
        readonly FullHttpResponse upgradeResponse = createUpgradeResponse(upgradeProtocol);
        if (!upgradeCodec.prepareUpgradeResponse(ctx, request, upgradeResponse.headers())) {
            return false;
        }

        // Create the user event to be fired once the upgrade completes.
        readonly UpgradeEvent event = new UpgradeEvent(upgradeProtocol, request);

        readonly UpgradeCodec finalUpgradeCodec = upgradeCodec;
        ctx.writeAndFlush(upgradeResponse).addListener(new ChannelFutureListener() {
            // @Override
            public void operationComplete(ChannelFuture future)  {
                try {
                    if (future.isSuccess()) {
                        // Perform the upgrade to the new protocol.
                        sourceCodec.upgradeFrom(ctx);
                        finalUpgradeCodec.upgradeTo(ctx, request);

                        // Notify that the upgrade has occurred. Retain the event to offset
                        // the release() in the finally block.
                        ctx.fireUserEventTriggered(event.retain());

                        // Remove this handler from the pipeline.
                        ctx.pipeline().remove(HttpServerUpgradeHandler.this);
                    } else {
                        future.channel().close();
                    }
                } finally {
                    // Release the event if the upgrade event wasn't fired.
                    event.release();
                }
            }
        });
        return true;
    }

    /**
     * Creates the 101 Switching Protocols response message.
     */
    private static FullHttpResponse createUpgradeResponse(CharSequence upgradeProtocol) {
        DefaultFullHttpResponse res = new DefaultFullHttpResponse(HTTP_1_1, SWITCHING_PROTOCOLS,
                Unpooled.EMPTY_BUFFER, false);
        res.headers().add(HttpHeaderNames.CONNECTION, HttpHeaderValues.UPGRADE);
        res.headers().add(HttpHeaderNames.UPGRADE, upgradeProtocol);
        res.headers().add(HttpHeaderNames.CONTENT_LENGTH, HttpHeaderValues.ZERO);
        return res;
    }

    /**
     * Splits a comma-separated header value. The returned set is case-insensitive and contains each
     * part with whitespace removed.
     */
    private static List<CharSequence> splitHeader(CharSequence header) {
        readonly StringBuilder builder = new StringBuilder(header.length());
        readonly List<CharSequence> protocols = new ArrayList<CharSequence>(4);
        for (int i = 0; i < header.length(); ++i) {
            char c = header.charAt(i);
            if (Character.isWhitespace(c)) {
                // Don't include any whitespace.
                continue;
            }
            if (c == ',') {
                // Add the string and reset the builder for the next protocol.
                protocols.add(builder.ToString());
                builder.setLength(0);
            } else {
                builder.append(c);
            }
        }

        // Add the last protocol
        if (builder.length() > 0) {
            protocols.add(builder.ToString());
        }

        return protocols;
    }
}
}