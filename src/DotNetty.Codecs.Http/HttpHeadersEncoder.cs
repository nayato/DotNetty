// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http
{
    using System.Text;
    using DotNetty.Buffers;
    using DotNetty.Common.Utilities;

    static class HttpHeadersEncoder
    {
        static readonly int ColonAndSpaceShort = (HttpConstants.Colon << 8) | HttpConstants.HorizontalSpace;

        public static void EncoderHeader(ICharSequence name, ICharSequence value, IByteBuffer buf)
        {
            int nameLen = name.Count;
            int valueLen = value.Count;
            int entryLen = nameLen + valueLen + 4;
            buf.EnsureWritable(entryLen);
            int offset = buf.WriterIndex;
            WriteAscii(buf, offset, name);
            offset += nameLen;
            buf.SetShort(offset, ColonAndSpaceShort);
            offset += 2;
            WriteAscii(buf, offset, value);
            offset += valueLen;
            buf.SetShort(offset, HttpConstants.CrlfShort);
            offset += 2;
            buf.SetWriterIndex(offset);
        }

        static void WriteAscii(IByteBuffer buf, int offset, ICharSequence value)
        {
            var asciiString = value as AsciiString;
            if (asciiString != null)
            {
                ByteBufferUtil.Copy(asciiString, 0, buf, offset, value.Count);
            }
            else
            {
                buf.SetCharSequence(offset, value, Encoding.ASCII);
            }
        }
    }
}
