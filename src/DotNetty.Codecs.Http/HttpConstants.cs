// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http
{
    using System.Text;
    using DotNetty.Buffers;

    public static class HttpConstants
    {
        // Horizontal space
        public static readonly byte HorizontalSpace = 32;

        // Horizontal tab
        public static readonly byte HorizontalTab = 9;

        // Carriage return
        public static readonly byte CarriageReturn = 13;

        // Equals '='
        public static readonly byte EqualsSign = 61;

        // Line feed character
        public static readonly byte LineFeed = 10;

        // Colon ':'
        public static readonly byte Colon = 58;

        // Semicolon ';'
        public static readonly byte Semicolon = 59;

        // Comma ','
        public static readonly byte Comma = 44;

        // Double quote '"'
        public static readonly byte DoubleQuote = (byte)'"';

         // Default character set (UTF-8)
        public static readonly Encoding DefaultEncoding = Encoding.UTF8;

        // Horizontal space in char
        public static readonly char CharHorizontalSpace = (char)HorizontalSpace;

        internal static readonly int CrlfShort = (CarriageReturn << 8) | LineFeed;

        internal static readonly int ZeroCrlfMedium = ('0' << 16) | CrlfShort;

        internal static readonly byte[] ZeroCrlfCrlf = { (byte)'0', CarriageReturn, LineFeed, CarriageReturn, LineFeed };

        internal static readonly IByteBuffer CrlfBuf = Unpooled.WrappedBuffer(new [] { CarriageReturn, LineFeed }).Unreleasable();

        internal static readonly IByteBuffer ZeroCrlfCrlfBuf = Unpooled.WrappedBuffer(ZeroCrlfCrlf).Unreleasable();
    }
}
