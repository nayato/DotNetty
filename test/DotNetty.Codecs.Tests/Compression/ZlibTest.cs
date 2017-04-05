// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Tests.Compression
{
    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Text;
    using DotNetty.Buffers;
    using DotNetty.Codecs.Compression;
    using DotNetty.Common.Utilities;
    using DotNetty.Transport.Channels.Embedded;
    using Xunit;

    public sealed class ZlibTest
    {
        const int SmallByteSize = 128;
        const int LargeByteSize = 1024 * 1024;

        static readonly string LargeString = "<!--?xml version=\"1.0\" encoding=\"ISO-8859-1\"?-->\n" +
            "<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Strict//EN\" " +
            "\"http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd\">\n" +
            "<html xmlns=\"http://www.w3.org/1999/xhtml\" xml:lang=\"en\" lang=\"en\"><head>\n" +
            "    <title>Apache Tomcat</title>\n" +
            "</head>\n" +
            '\n' +
            "<body>\n" +
            "<h1>It works !</h1>\n" +
            '\n' +
            "<p>If you're seeing this page via a web browser, it means you've setup Tomcat successfully." +
            " Congratulations!</p>\n" +
            " \n" +
            "<p>This is the default Tomcat home page." +
            " It can be found on the local filesystem at: <code>/var/lib/tomcat7/webapps/ROOT/index.html</code></p>\n" +
            '\n' +
            "<p>Tomcat7 veterans might be pleased to learn that this system instance of Tomcat is installed with" +
            " <code>CATALINA_HOME</code> in <code>/usr/share/tomcat7</code> and <code>CATALINA_BASE</code> in" +
            " <code>/var/lib/tomcat7</code>, following the rules from" +
            " <code>/usr/share/doc/tomcat7-common/RUNNING.txt.gz</code>.</p>\n" +
            '\n' +
            "<p>You might consider installing the following packages, if you haven't already done so:</p>\n" +
            '\n' +
            "<p><b>tomcat7-docs</b>: This package installs a web application that allows to browse the Tomcat 7" +
            " documentation locally. Once installed, you can access it by clicking <a href=\"docs/\">here</a>.</p>\n" +
            '\n' +
            "<p><b>tomcat7-examples</b>: This package installs a web application that allows to access the Tomcat" +
            " 7 Servlet and JSP examples. Once installed, you can access it by clicking" +
            " <a href=\"examples/\">here</a>.</p>\n" +
            '\n' +
            "<p><b>tomcat7-admin</b>: This package installs two web applications that can help managing this Tomcat" +
            " instance. Once installed, you can access the <a href=\"manager/html\">manager webapp</a> and" +
            " the <a href=\"host-manager/html\">host-manager webapp</a>.</p><p>\n" +
            '\n' +
            "</p><p>NOTE: For security reasons, using the manager webapp is restricted" +
            " to users with role \"manager\"." +
            " The host-manager webapp is restricted to users with role \"admin\". Users are " +
            "defined in <code>/etc/tomcat7/tomcat-users.xml</code>.</p>\n" +
            '\n' +
            '\n' +
            '\n' +
            "</body></html>";

        static byte[] CreateSmallBytes(int count)
        {
            var buffer = new byte[count];
            var random = new Random();
            random.NextBytes(buffer);

            return buffer;
        }

        static ZlibEncoder CreateEncoder(ZlibWrapper wrapper) => new JZlibEncoder(wrapper);

        static ZlibDecoder CreateDecoder(ZlibWrapper wrapper) => new JZlibDecoder(wrapper);

        [Fact]
        public void GZip2()
        {
            byte[] bytes = Encoding.UTF8.GetBytes("message");
            IByteBuffer data = Unpooled.WrappedBuffer(bytes);
            IByteBuffer deflatedData = Unpooled.WrappedBuffer(GetGZip(bytes));

            var ch = new EmbeddedChannel(CreateDecoder(ZlibWrapper.Gzip));
            try
            {
                ch.WriteInbound(deflatedData);
                Assert.True(ch.Finish());
                var buf = ch.ReadInbound<IByteBuffer>();
                Assert.Equal(data, buf);
                Assert.Null(ch.ReadInbound<IByteBuffer>());
                data.Release();
                buf.Release();
            }
            finally
            {
                Dispose(ch);
            }
        }

        static void Compress0(ZlibWrapper encoderWrapper, ZlibWrapper decoderWrapper, IByteBuffer data)
        {
            var chEncoder = new EmbeddedChannel(CreateEncoder(encoderWrapper));
            var chDecoderZlib = new EmbeddedChannel(CreateDecoder(decoderWrapper));

            try
            {
                chEncoder.WriteOutbound(data.Retain());
                chEncoder.Flush();
                data.ResetReaderIndex();

                for (;;)
                {
                    var deflatedData = chEncoder.ReadOutbound<IByteBuffer>();
                    if (deflatedData == null)
                    {
                        break;
                    }
                    chDecoderZlib.WriteInbound(deflatedData);
                }

                var decompressed = new byte[data.ReadableBytes];
                int offset = 0;
                for (;;)
                {
                    var buf = chDecoderZlib.ReadInbound<IByteBuffer>();
                    if (buf == null)
                    {
                        break;
                    }
                    int length = buf.ReadableBytes;
                    buf.ReadBytes(decompressed, offset, length);
                    offset += length;
                    buf.Release();
                    if (offset == decompressed.Length)
                    {
                        break;
                    }
                }

                Assert.Equal(data, Unpooled.WrappedBuffer(decompressed));
                Assert.Null(chDecoderZlib.ReadInbound<IByteBuffer>());

                Assert.False(chDecoderZlib.Finish());
                data.Release();
            }
            finally
            {
                Dispose(chEncoder);
                Dispose(chDecoderZlib);
            }
        }

        static void CompressNone(ZlibWrapper encoderWrapper, ZlibWrapper decoderWrapper)
        {
            var chEncoder = new EmbeddedChannel(CreateEncoder(encoderWrapper));
            var chDecoderZlib = new EmbeddedChannel(CreateDecoder(decoderWrapper));

            try
            {
                for (;;)
                {
                    var deflatedData = chEncoder.ReadOutbound<IByteBuffer>();
                    if (deflatedData == null)
                    {
                        break;
                    }
                    chDecoderZlib.WriteInbound(deflatedData);
                }

                // Decoder should not generate anything at all.
                bool decoded = false;
                for (;;)
                {
                    var buf = chDecoderZlib.ReadInbound<IByteBuffer>();
                    if (buf == null)
                    {
                        break;
                    }

                    buf.Release();
                    decoded = true;
                }
                Assert.False(decoded, "should decode nothing");
                Assert.False(chDecoderZlib.Finish());
            }
            finally
            {
                Dispose(chEncoder);
                Dispose(chDecoderZlib);
            }
        }

        // Test for https://github.com/netty/netty/issues/2572
        static void DecompressOnly(ZlibWrapper decoderWrapper, byte[] compressed, byte[] data)
        {
            var chDecoder = new EmbeddedChannel(CreateDecoder(decoderWrapper));
            chDecoder.WriteInbound(Unpooled.WrappedBuffer(compressed));
            Assert.True(chDecoder.Finish());

            IByteBuffer decoded = Unpooled.Buffer(data.Length);

            for (;;)
            {
                var buf = chDecoder.ReadInbound<IByteBuffer>();
                if (buf == null)
                {
                    break;
                }
                decoded.WriteBytes(buf);
                buf.Release();
            }
            Assert.Equal(Unpooled.WrappedBuffer(data), decoded);
            decoded.Release();
        }

        static void CompressSmall(ZlibWrapper encoderWrapper, ZlibWrapper decoderWrapper)
        {
            byte[] data = CreateSmallBytes(SmallByteSize);
            Compress0(encoderWrapper, decoderWrapper, Unpooled.WrappedBuffer(data));
        }

        static void CompressLarge(ZlibWrapper encoderWrapper, ZlibWrapper decoderWrapper)
        {
            byte[] data = CreateSmallBytes(LargeByteSize);
            Compress0(encoderWrapper, decoderWrapper, Unpooled.WrappedBuffer(data));
        }

        [Fact]
        public void Zlib()
        {
            CompressNone(ZlibWrapper.Zlib, ZlibWrapper.Zlib);
            CompressSmall(ZlibWrapper.Zlib, ZlibWrapper.Zlib);
            CompressLarge(ZlibWrapper.Zlib, ZlibWrapper.Zlib);

            byte[] data = Encoding.UTF8.GetBytes(LargeString);
            // Originall Netty use ZLib but .NET DeflateStream uses another
            // format, so we have to set the flag to be ZlibOrNone
            DecompressOnly(ZlibWrapper.ZlibOrNone, GetDeflate(data), data);
        }

        [Fact]
        public void GZip()
        {
            CompressNone(ZlibWrapper.Gzip, ZlibWrapper.Gzip);
            CompressSmall(ZlibWrapper.Gzip, ZlibWrapper.Gzip);
            CompressLarge(ZlibWrapper.Gzip, ZlibWrapper.Gzip);

            byte[] data = Encoding.UTF8.GetBytes(LargeString);
            DecompressOnly(ZlibWrapper.Gzip, GetGZip(data), data);
        }

        [Fact]
        public void GZipCompressOnly()
        {
            GZipCompressOnly0(null); // Do not write anything; just finish the stream.
            GZipCompressOnly0(new byte[0]); // Write an empty array.


            byte[] data = CreateSmallBytes(SmallByteSize);
            GZipCompressOnly0(data);

            data = CreateSmallBytes(LargeByteSize);
            GZipCompressOnly0(data);
        }

        static void GZipCompressOnly0(byte[] data)
        {
            var chEncoder = new EmbeddedChannel(CreateEncoder(ZlibWrapper.Gzip));
            if (data != null)
            {
                chEncoder.WriteOutbound(Unpooled.WrappedBuffer(data));
            }

            IByteBuffer encoded = Unpooled.Buffer();
            for (;;)
            {
                var buf = chEncoder.ReadOutbound<IByteBuffer>();
                if (buf == null)
                {
                    break;
                }
                encoded.WriteBytes(buf);
                buf.Release();
            }

            ArraySegment<byte> buffer = encoded.GetIoBuffer();
            var memoryStream = new MemoryStream(buffer.Array, buffer.Offset, buffer.Count);
            var stream = new GZipStream(memoryStream, CompressionMode.Decompress);

            IByteBuffer decoded = Unpooled.Buffer();
            try
            {
                var buf = new byte[8192];
                for (;;)
                {
                    int readBytes = stream.Read(buf, 0, buf.Length);
                    if (readBytes > 0)
                    {
                        decoded.WriteBytes(buf, 0, readBytes);
                    }
                    else
                    {
                        break;
                    }
                }
            }
            finally
            {
                stream.Dispose();
            }

            if (data != null)
            {
                Assert.Equal(Unpooled.WrappedBuffer(data), decoded);
            }
            else
            {
                Assert.False(decoded.IsReadable());
            }

            decoded.Release();
        }

        static byte[] GetGZip(byte[] bytes)
        {
            MemoryStream outputStream = null;

            try
            {
                outputStream = new MemoryStream();
                using (var stream = new GZipStream(outputStream, CompressionMode.Compress, false))
                {
                    stream.Write(bytes, 0, bytes.Length);
                }

                return outputStream.ToArray();
            }
            finally 
            {
                outputStream?.Dispose();
            }
        }

        static byte[] GetDeflate(byte[] bytes)
        {
            MemoryStream outputStream = null;

            try
            {
                outputStream = new MemoryStream();
                using (var stream = new DeflateStream(outputStream, CompressionMode.Compress, false))
                {
                    stream.Write(bytes, 0, bytes.Length);
                }

                return outputStream.ToArray();
            }
            finally
            {
                outputStream?.Dispose();
            }
        }

        static void Dispose(EmbeddedChannel ch)
        {
            if (ch.Finish())
            {
                for (;;)
                {
                    var msg = ch.ReadInbound<object>();
                    if (msg == null)
                    {
                        break;
                    }
                    ReferenceCountUtil.Release(msg);
                }
                for (;;)
                {
                    var msg = ch.ReadOutbound<object>();
                    if (msg == null)
                    {
                        break;
                    }
                    ReferenceCountUtil.Release(msg);
                }
            }
        }
    }
}
