// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http
{
    public sealed class HttpExpectationFailedEvent
    {
        public static readonly HttpExpectationFailedEvent Default = new HttpExpectationFailedEvent();

        HttpExpectationFailedEvent()
        {
        }
    }
}
