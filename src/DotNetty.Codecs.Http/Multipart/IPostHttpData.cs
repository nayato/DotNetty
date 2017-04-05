// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http.Multipart
{
    using System;
    using DotNetty.Common;

    public enum HttpDataType
    {
        Attribute,
        FileUpload,
        InternalAttribute
    }

    /// <summary>
    ///  Interface for all Objects that could be encoded/decoded using HttpPostRequestEncoder/Decoder
    /// </summary>
    public interface IPostHttpData : IComparable<IPostHttpData>, IReferenceCounted
    {
        string Name { get; }

        HttpDataType DataType { get; }
    }
}
