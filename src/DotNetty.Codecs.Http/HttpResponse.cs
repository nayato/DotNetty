// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http
{
    using System;

    /**
 * An HTTP response.
 *
 * <h3>Accessing Cookies</h3>
 * <p>
 * Unlike the Servlet API, {@link io.netty.handler.codec.http.cookie.Cookie} support is provided
 * separately via {@link io.netty.handler.codec.http.cookie.ServerCookieDecoder},
 * {@link io.netty.handler.codec.http.cookie.ClientCookieDecoder},
 * {@link io.netty.handler.codec.http.cookie.ServerCookieEncoder},
 * and {@link io.netty.handler.codec.http.cookie.ClientCookieEncoder}.
 *
 * @see HttpRequest
 * @see io.netty.handler.codec.http.cookie.ServerCookieDecoder
 * @see io.netty.handler.codec.http.cookie.ClientCookieDecoder
 * @see io.netty.handler.codec.http.cookie.ServerCookieEncoder
 * @see io.netty.handler.codec.http.cookie.ClientCookieEncoder
 */

    public interface HttpResponse : HttpMessage
    {
        /**
                 * @deprecated Use {@link #status()} instead.
                 */

        [Obsolete]
        HttpResponseStatus getStatus();

        /**
         * Returns the status of this {@link HttpResponse}.
         *
         * @return The {@link HttpResponseStatus} of this {@link HttpResponse}
         */

        HttpResponseStatus status();

        /**
         * Set the status of this {@link HttpResponse}.
         */

        HttpResponse setStatus(HttpResponseStatus status);

        // @Override
        HttpResponse setProtocolVersion(HttpVersion version);
    }
}