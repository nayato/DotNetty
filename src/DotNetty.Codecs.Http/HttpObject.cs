// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http
{
    using System;

    public interface HttpObject : DecoderResultProvider
    {
        /**
         * @deprecated Use {@link #decoderResult()} instead.
         */

        [Obsolete]
        DecoderResult getDecoderResult();
    }
}