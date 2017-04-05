// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Tests
{
    using System.Text;
    using DotNetty.Buffers;
    using DotNetty.Transport.Channels;
    using DotNetty.Transport.Channels.Embedded;
    using Xunit;

    public sealed class MessageAggregatorTest
    {
        sealed class ReadCounter : ChannelHandlerAdapter
        {
            public int Value;

            public override void Read(IChannelHandlerContext context)
            {
                this.Value++;
                base.Read(context);
            }
        }

        sealed class MockMessageAggregator : MessageAggregator<IByteBufferHolder, IByteBufferHolder, IByteBufferHolder, IByteBufferHolder>
        {
            readonly IByteBufferHolder first;
            readonly IByteBufferHolder chunk;
            readonly IByteBufferHolder last;

            public MockMessageAggregator(IByteBufferHolder first, IByteBufferHolder chunk, IByteBufferHolder last)
                : base(1024)
            {
                this.first = first;
                this.chunk = chunk;
                this.last = last;
            }

            protected override bool IsStartMessage(IByteBufferHolder msg) => ReferenceEquals(msg, this.first);

            protected override bool IsContentMessage(IByteBufferHolder msg) => ReferenceEquals(msg, this.chunk) || ReferenceEquals(msg, this.last);

            protected override bool IsLastContentMessage(IByteBufferHolder msg) => ReferenceEquals(msg, this.last);

            protected override bool IsAggregated(IByteBufferHolder msg) => false;

            protected override bool IsContentLengthInvalid(IByteBufferHolder start, int maxContentLength) => false;

            protected override object NewContinueResponse(IByteBufferHolder start, int maxContentLength, IChannelPipeline pipeline) => null;

            protected override bool CloseAfterContinueResponse(object msg)
            {
                throw new System.NotImplementedException();
            }

            protected override bool IgnoreContentAfterContinueResponse(object msg)
            {
                throw new System.NotImplementedException();
            }

            protected override IByteBufferHolder BeginAggregation(IByteBufferHolder start, IByteBuffer content) => new DefaultByteBufferHolder(content);
        }

        static IByteBufferHolder CreateMessage(string stringValue) => new DefaultByteBufferHolder(Unpooled.CopiedBuffer(Encoding.ASCII.GetBytes(stringValue)));

        [Fact]
        public void ReadFlowManagement()
        {
            var counter = new ReadCounter();
            IByteBufferHolder first = CreateMessage("first");
            IByteBufferHolder chunk = CreateMessage("chunk");
            IByteBufferHolder last = CreateMessage("last");

            var agg = new MockMessageAggregator(first, chunk, last);

            var embedded = new EmbeddedChannel(counter, agg);
            embedded.Configuration.AutoRead = false;


            Assert.False(embedded.WriteInbound(first));
            Assert.False(embedded.WriteInbound(chunk));
            Assert.True(embedded.WriteInbound(last));

            Assert.Equal(3, counter.Value); // 2 reads issued from MockMessageAggregator
                                            // 1 read issued from EmbeddedChannel constructor

            var buffer = new CompositeByteBuffer(UnpooledByteBufferAllocator.Default, 3, 
                (IByteBuffer)first.Content.Retain(), (IByteBuffer)chunk.Content.Retain(), (IByteBuffer)last.Content.Retain());
            var all = new DefaultByteBufferHolder(buffer);

            var output = embedded.ReadInbound<IByteBufferHolder>();

            Assert.Equal(all, output);
            Assert.True(all.Release() && output.Release());
            Assert.False(embedded.Finish());
        }
    }
}
