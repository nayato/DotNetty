// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http
{
    using DotNetty.Buffers;
    using DotNetty.Common.Utilities;

    sealed class HttpHeadersEncoder
    {
        HttpHeadersEncoder()
        {
        }

        public static void encoderHeader(CharSequence name, CharSequence value, IByteBuffer buf)
        {
            int nameLen = name.Length;
            int valueLen = value.Length;
            int entryLen = nameLen + valueLen + 4;
            buf.EnsureWritable(entryLen);
            int offset = buf.WriterIndex;
            writeAscii(buf, offset, name, nameLen);
            offset += nameLen;
            buf.SetByte(offset++, ':');
            buf.SetByte(offset++, ' ');
            writeAscii(buf, offset, value, valueLen);
            offset += valueLen;
            buf.SetByte(offset++, '\r');
            buf.SetByte(offset++, '\n');
            buf.SetWriterIndex(offset);
        }

        static void writeAscii(IByteBuffer buf, int offset, CharSequence value, int valueLen)
        {
            if (value is AsciiString)
            {
                ByteBufferUtil.Copy((AsciiString)value, 0, buf, offset, valueLen);
            }
            else
            {
                writeCharSequence(buf, offset, value, valueLen);
            }
        }

        static void writeCharSequence(IByteBuffer buf, int offset, CharSequence value, int valueLen)
        {
            for (int i = 0; i < valueLen; ++i)
            {
                buf.SetByte(offset++, c2b(value[i]));
            }
        }
    }