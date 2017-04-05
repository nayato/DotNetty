// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http.Multipart
{
    using System.Collections.Generic;
    using DotNetty.Common.Utilities;

    public interface IHttpPostRequestDecoder
    {
        bool IsMultipart { get; }

        int DiscardThreshold { get; set; }

        List<IPostHttpData> GetBodyDataList();

        List<IPostHttpData> GetBodyDataList(AsciiString name);

        IPostHttpData GetBodyData(AsciiString name);

        IHttpPostRequestDecoder Offer(IHttpContent content);

        bool HasNext { get; }

        IPostHttpData Next();

        IPostHttpData CurrentPartialHttpData { get; }

        void Destroy();

        void CleanFiles();

        void RemoveHttpDataFromClean(IPostHttpData data);
    }
}
