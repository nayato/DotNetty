// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http
{
    using System;

    /**
 * An interface that defines a HTTP message, providing common properties for
 * {@link HttpRequest} and {@link HttpResponse}.
 *
 * @see HttpResponse
 * @see HttpRequest
 * @see HttpHeaders
 */

    public interface HttpMessage : HttpObject
    {
        /**
             * @deprecated Use {@link #protocolVersion()} instead.
             */

        [Obsolete]
        HttpVersion getProtocolVersion();

        /**
         * Returns the protocol version of this {@link HttpMessage}
         */

        HttpVersion protocolVersion();

        /**
         * Set the protocol version of this {@link HttpMessage}
         */

        HttpMessage setProtocolVersion(HttpVersion version);

        /**
         * Returns the headers of this message.
         */

        HttpHeaders headers();
    }
}