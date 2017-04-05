// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Handlers.Tests
{
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using DotNetty.Buffers;
    using DotNetty.Handlers.Streams;
    using DotNetty.Transport.Channels.Embedded;
    using Xunit;

    public sealed class ChunkedWriteHandlerTest
    {
        static readonly byte[] Bytes = new byte[1024 * 64];

        static ChunkedWriteHandlerTest()
        {
            for (int i = 0; i < Bytes.Length; i++)
            {
                Bytes[i] = (byte)i;
            }
        }

        [Fact]
        public void ChunkedStream()
        {
            Check(new ChunkedStream(new MemoryStream(Bytes)));

            Check(new ChunkedStream(new MemoryStream(Bytes)),
                new ChunkedStream(new MemoryStream(Bytes)),
                new ChunkedStream(new MemoryStream(Bytes)));
        }

        [Fact]
        public void UnchunkedData()
        {
            Check(Unpooled.WrappedBuffer(Bytes));

            Check(Unpooled.WrappedBuffer(Bytes), 
                Unpooled.WrappedBuffer(Bytes), 
                Unpooled.WrappedBuffer(Bytes));
        }

        // Test case http://stackoverflow.com/a/10426305
        [Fact]
        public void NotifiedWhenIsEnd()
        {
            IByteBuffer buffer = TestChunkedInput.CreateBuffer();

            var ch = new EmbeddedChannel(new ChunkedWriteHandler<IByteBuffer>());
            var input = new TestChunkedInput();
            bool notified = false;
            ch.WriteAndFlushAsync(input)
                .ContinueWith(_ => notified = true, TaskContinuationOptions.ExecuteSynchronously);
            ch.CheckException();
            ch.Finish();

            Assert.True(notified);
            var buffer2 = ch.ReadOutbound<IByteBuffer>();
            Assert.Equal(input.Buffer, buffer2);
            Assert.Null(ch.ReadOutbound<IByteBuffer>());

            buffer.Release();
            buffer2.Release();
        }

        sealed class TestChunkedInput : IChunkedInput<IByteBuffer>
        {
            public TestChunkedInput()
            {
                this.Buffer = CreateBuffer();
            }

            public static IByteBuffer CreateBuffer()
            {
                byte[] data = Encoding.UTF8.GetBytes("Test");
                return Unpooled.CopiedBuffer(data);
            }

            public IByteBuffer Buffer { get; }

            public bool IsEndOfInput { get; private set; }

            public void Close() => this.Buffer.Release();

            public IByteBuffer ReadChunk(IByteBufferAllocator allocator)
            {
                if (this.IsEndOfInput)
                {
                    return null;
                }

                this.IsEndOfInput = true;
                IByteBuffer result = this.Buffer.Duplicate();
                result.Retain();
                return result;
            }

            public long Length => -1;

            public long Progress => 1;
        }

        [Fact]
        public void ChunkedMessageInput()
        {
            var ch = new EmbeddedChannel(new ChunkedWriteHandler<object>());
            var input = new MessageChunckedInput();
            bool notified = false;
            ch.WriteAndFlushAsync(input)
                .ContinueWith(_ => notified = true, TaskContinuationOptions.ExecuteSynchronously);
            ch.CheckException();
            ch.Finish();

            Assert.True(notified);
            Assert.Equal(0, ch.ReadOutbound<object>());
            Assert.Null(ch.ReadOutbound<object>());
        }

        sealed class MessageChunckedInput : IChunkedInput<object>
        {
            public bool IsEndOfInput { get; private set; }

            public void Close()
            {
                // NOOP
            }

            public object ReadChunk(IByteBufferAllocator allocator)
            {
                if (this.IsEndOfInput)
                {
                    return false;
                }

                this.IsEndOfInput = true;
                return 0;
            }

            public long Length => -1;

            public long Progress => 1;
        }

        static void Check(params object[] inputs)
        {
            var ch = new EmbeddedChannel(new ChunkedWriteHandler<IByteBuffer>());

            foreach (object input in inputs)
            {
                ch.WriteOutbound(input);
            }
            Assert.True(ch.Finish());

            int i = 0;
            int read = 0;
            for (;;)
            {
                var buffer = ch.ReadOutbound<IByteBuffer>();
                if (buffer == null)
                {
                    break;
                }

                while (buffer.IsReadable())
                {
                    Assert.Equal(Bytes[i++], buffer.ReadByte());
                    read++;
                    if (i == Bytes.Length)
                    {
                        i = 0;
                    }
                }
                buffer.Release();
            }

            Assert.Equal(Bytes.Length * inputs.Length, read);
        }
    }
}
