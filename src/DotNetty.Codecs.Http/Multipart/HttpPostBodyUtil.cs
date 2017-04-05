// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http.Multipart
{
    using System;
    using System.Collections.Generic;
    using DotNetty.Buffers;

    static class HttpPostBodyUtil
    {
        public static readonly int ChunkSize = 8096;

        public static readonly string DefaultBinaryContentType = "application/octet-stream";

        public static readonly string DefaultTextContentType = "text/plain";

        internal class SeekAheadNoBackArrayException : Exception
        {
        }

        internal class SeekAheadOptimize
        {
            static readonly SeekAheadNoBackArrayException Error = new SeekAheadNoBackArrayException();

            int readerIndex;
            readonly int origPos;
            IByteBuffer buffer;

            internal SeekAheadOptimize(IByteBuffer buffer)
            {
                if (!buffer.HasArray)
                {
                    throw Error;
                }

                this.buffer = buffer;
                this.Bytes = buffer.Array;
                this.readerIndex = buffer.ReaderIndex;
                this.origPos = this.Position = buffer.ArrayOffset + this.readerIndex;
                this.Limit = buffer.ArrayOffset + buffer.WriterIndex;
            }

            public int Position { get; set; }

            public int Limit { get; private set; }

            public byte[] Bytes { get; private set; }

            internal void SetReadPosition(int minus)
            {
                this.Position -= minus;
                this.readerIndex = this.GetReadPosition(this.Position);
                this.buffer.SetReaderIndex(this.readerIndex);
            }

            internal int GetReadPosition(int index) => index - this.origPos + this.readerIndex;

            internal void Clear()
            {
                this.buffer = null;
                this.Bytes = null;
                this.Limit = 0;
                this.Position = 0;
                this.readerIndex = 0;
            }
        }

        internal static int FindNonWhitespace(IReadOnlyList<char> sb, int offset)
        {
            int result;
            for (result = offset; result < sb.Count; result++)
            {
                if (!char.IsWhiteSpace(sb[result]))
                {
                    break;
                }
            }

            return result;
        }

        internal static int FindWhitespace(IReadOnlyList<char> sb, int offset)
        {
            int result;
            for (result = offset; result < sb.Count; result++)
            {
                if (char.IsWhiteSpace(sb[result]))
                {
                    break;
                }
            }

            return result;
        }

        internal static int FindEndOfString(IReadOnlyList<char> sb)
        {
            int result;
            for (result = sb.Count; result > 0; result--)
            {
                if (!char.IsWhiteSpace(sb[result - 1]))
                {
                    break;
                }
            }

            return result;
        }
    }

    public class TransferEncodingMechanism
    {
        public static readonly TransferEncodingMechanism Bit7 = new TransferEncodingMechanism("7bit");

        public static readonly TransferEncodingMechanism Bit8 = new TransferEncodingMechanism("8bit");

        public static readonly TransferEncodingMechanism Binary = new TransferEncodingMechanism("binary");

        TransferEncodingMechanism(string name)
        {
            this.Value = name;
        }

        public string Value { get; }
    }
}
