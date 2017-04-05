// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
/*
 * Copyright (c) 2000-2011 ymnk, JCraft,Inc. All rights reserved.
 * 
 * This program is based on zlib-1.1.3, so all credit should go authors
 * Jean-loup Gailly(jloup@gzip.org) and Mark Adler(madler@alumni.caltech.edu)
 * and contributors of zlib.
 * 
 * https://github.com/ymnk/jzlib/blob/master/src/test/scala/DeflateInflateTest.scala
 */
namespace DotNetty.Codecs.Tests.Compression
{
    using System;
    using System.Linq;
    using System.Text;
    using DotNetty.Codecs.Compression;
    using Xunit;

    public sealed class DeflateInflateTest
    {
        const int ComprLen = 40000;
        const int UncomprLen = ComprLen;

        [Fact]
        public void LargeBuffer()
        {
            var compr = new byte[ComprLen];
            var uncompr = new byte[UncomprLen];

            var deflater = new Deflater();
            var inflater = new Inflater();

            int err = deflater.Init(JZlib.Z_BEST_SPEED);
            Assert.Equal(JZlib.Z_OK, err);

            deflater.SetInput(uncompr);
            deflater.SetOutput(compr);

            err = deflater.Deflate(JZlib.Z_NO_FLUSH);
            Assert.Equal(JZlib.Z_OK, err);

            Assert.Equal(0, deflater.avail_in);

            deflater.Params(JZlib.Z_NO_COMPRESSION, JZlib.Z_DEFAULT_STRATEGY);
            deflater.SetInput(compr);
            deflater.avail_in = ComprLen / 2;

            err = deflater.Deflate(JZlib.Z_NO_FLUSH);
            Assert.Equal(JZlib.Z_OK, err);

            deflater.Params(JZlib.Z_BEST_COMPRESSION, JZlib.Z_FILTERED);
            deflater.SetInput(uncompr);
            deflater.avail_in = UncomprLen;

            err = deflater.Deflate(JZlib.Z_NO_FLUSH);
            Assert.Equal(JZlib.Z_OK, err);

            err = deflater.Deflate(JZlib.Z_FINISH);
            Assert.Equal(JZlib.Z_STREAM_END, err);

            err = deflater.End();
            Assert.Equal(JZlib.Z_OK, err);

            inflater.SetInput(compr);

            err = inflater.Init();
            Assert.Equal(JZlib.Z_OK, err);

            bool loop = true;
            while (loop)
            {
                inflater.SetOutput(uncompr);
                err = inflater.Inflate(JZlib.Z_NO_FLUSH);
                if (err == JZlib.Z_STREAM_END)
                {
                    loop = false;
                }
                else
                {
                    Assert.Equal(JZlib.Z_OK, err);
                }
            }

            err = inflater.End();
            Assert.Equal(JZlib.Z_OK, err);

            int totalOut = (int)inflater.total_out;
            Assert.Equal(2 * UncomprLen + ComprLen / 2, totalOut);
        }

        [Fact]
        public void SmallBuffer()
        {
            var compr = new byte[ComprLen];
            var uncompr = new byte[UncomprLen];

            var deflater = new Deflater();
            var inflater = new Inflater();

            byte[] data = Encoding.UTF8.GetBytes("hello, hello!");

            int err = deflater.Init(JZlib.Z_DEFAULT_COMPRESSION);
            Assert.Equal(JZlib.Z_OK, err);

            deflater.SetInput(data);
            deflater.SetOutput(compr);

            while (deflater.total_in < data.Length
                && deflater.total_out < ComprLen)
            {
                deflater.avail_in = 1;
                deflater.avail_out = 1;
                err = deflater.Deflate(JZlib.Z_NO_FLUSH);
                Assert.Equal(JZlib.Z_OK, err);
            }

            do
            {
                deflater.avail_out = 1;
                err = deflater.Deflate(JZlib.Z_FINISH);
            }
            while (err != JZlib.Z_STREAM_END);

            err = deflater.End();
            Assert.Equal(JZlib.Z_OK, err);

            inflater.SetInput(compr);
            inflater.SetOutput(uncompr);

            err = inflater.Init();
            Assert.Equal(JZlib.Z_OK, err);

            bool loop = true;
            while (inflater.total_out < UncomprLen
                && inflater.total_in < ComprLen
                && loop)
            {
                inflater.avail_in = 1; // force small buffers
                inflater.avail_out = 1; // force small buffers
                err = inflater.Inflate(JZlib.Z_NO_FLUSH);
                if (err == JZlib.Z_STREAM_END)
                {
                    loop = false;
                }
                else
                {
                    Assert.Equal(JZlib.Z_OK, err);
                }
            }

            err = inflater.End();
            Assert.Equal(JZlib.Z_OK, err);

            int totalOut = (int)inflater.total_out;
            var actual = new byte[totalOut];
            Array.Copy(uncompr, 0, actual, 0, totalOut);
            Assert.True(data.SequenceEqual(actual));
        }

        [Fact]
        public void Dictionary()
        {
            var compr = new byte[ComprLen];
            var uncompr = new byte[UncomprLen];

            var deflater = new Deflater();
            var inflater = new Inflater();

            byte[] hello = Encoding.UTF8.GetBytes("hello");
            byte[] dictionary = Encoding.UTF8.GetBytes("hello, hello!");

            int err = deflater.Init(JZlib.Z_DEFAULT_COMPRESSION);
            Assert.Equal(JZlib.Z_OK, err);

            deflater.SetDictionary(dictionary, dictionary.Length);
            Assert.Equal(JZlib.Z_OK, err);

            long dictId = deflater.GetAdler();

            deflater.SetInput(hello);
            deflater.SetOutput(compr);

            err = deflater.Deflate(JZlib.Z_FINISH);
            Assert.Equal(JZlib.Z_STREAM_END, err);

            err = deflater.End();
            Assert.Equal(JZlib.Z_OK, err);

            err = inflater.Init();
            Assert.Equal(JZlib.Z_OK, err);

            inflater.SetInput(compr);
            inflater.SetOutput(uncompr);

            bool loop = true;
            do
            {
                err = inflater.Inflate(JZlib.Z_NO_FLUSH);
                if (err == JZlib.Z_STREAM_END)
                {
                    loop = false;
                }
                else if (err == JZlib.Z_NEED_DICT)
                {
                    Assert.Equal(dictId, inflater.GetAdler());
                    err = inflater.SetDictionary(dictionary, dictionary.Length);
                    Assert.Equal(JZlib.Z_OK, err);
                }
                else
                {
                    Assert.Equal(JZlib.Z_OK, err);
                }
            }
            while (loop);


            err = inflater.End();
            Assert.Equal(JZlib.Z_OK, err);

            int totalOut = (int)inflater.total_out;
            var actual = new byte[totalOut];
            Array.Copy(uncompr, 0, actual, 0, totalOut);
            Assert.True(hello.SequenceEqual(actual));
        }

        [Fact]
        public void Sync()
        {
            var compr = new byte[ComprLen];
            var uncompr = new byte[UncomprLen];

            var deflater = new Deflater();
            var inflater = new Inflater();

            byte[] hello = Encoding.UTF8.GetBytes("hello");
            int err = deflater.Init(JZlib.Z_DEFAULT_COMPRESSION);
            Assert.Equal(JZlib.Z_OK, err);

            deflater.SetInput(hello);
            deflater.avail_in = 3;
            deflater.SetOutput(compr);

            err = deflater.Deflate(JZlib.Z_FULL_FLUSH);
            Assert.Equal(JZlib.Z_OK, err);

            compr[3] = (byte)(compr[3] + 1);
            deflater.avail_in = hello.Length - 3;

            err = deflater.Deflate(JZlib.Z_FINISH);
            Assert.Equal(JZlib.Z_STREAM_END, err);
            int comprLen = (int)deflater.total_out;

            err = deflater.End();
            Assert.Equal(JZlib.Z_OK, err);

            err = inflater.Init();
            Assert.Equal(JZlib.Z_OK, err);

            inflater.SetInput(compr);
            inflater.avail_in = 2;

            inflater.SetOutput(uncompr);

            err = inflater.Inflate(JZlib.Z_NO_FLUSH);
            Assert.Equal(JZlib.Z_OK, err);

            inflater.avail_in = comprLen - 2;
            inflater.Sync();

            err = inflater.Inflate(JZlib.Z_FINISH);
            Assert.Equal(JZlib.Z_DATA_ERROR, err);

            err = inflater.End();
            Assert.Equal(JZlib.Z_OK, err);

            int totalOut = (int)inflater.total_out;
            var actual = new byte[totalOut];
            Array.Copy(uncompr, 0, actual, 0, totalOut);

            string expected = Encoding.UTF8.GetString(actual);
            Assert.Equal("hello", "hel" + expected);
        }

        [Fact]
        public void InflateGZip()
        {
            var uncompr = new byte[UncomprLen];
            var inflater = new Inflater();

            byte[] hello = Encoding.UTF8.GetBytes("foo");
            var data = new byte[]
            {
                0x1f, 0x8b, 0x08, 0x18, 0x08, 0xeb, 0x7a, 0x0b, 0x00, 0x0b,
                0x58, 0x00, 0x59, 0x00, 0x4b, 0xcb, 0xcf, 0x07, 0x00, 0x21,
                0x65, 0x73, 0x8c, 0x03, 0x00, 0x00, 0x00
            };

            int err = inflater.Init(15 + 32);
            Assert.Equal(JZlib.Z_OK, err);

            inflater.SetInput(data);
            inflater.SetOutput(uncompr);

            int comprLen = data.Length;

            bool loop = true;
            while (inflater.total_out < UncomprLen
                && inflater.total_in < comprLen
                && loop)
            {
                err = inflater.Inflate(JZlib.Z_NO_FLUSH);
                if (err == JZlib.Z_STREAM_END)
                {
                    loop = false;
                }
                else
                {
                    Assert.Equal(JZlib.Z_OK, err);
                }
            }

            err = inflater.End();
            Assert.Equal(JZlib.Z_OK, err);

            int totalOut = (int)inflater.total_out;
            var actual = new byte[totalOut];
            Array.Copy(uncompr, 0, actual, 0, totalOut);
            Assert.True(hello.SequenceEqual(actual));
        }

        [Fact]
        public void DeflateGZip()
        {
            var compr = new byte[ComprLen];
            var uncompr = new byte[UncomprLen];

            var deflater = new Deflater();
            var inflater = new Inflater();

            byte[] data = Encoding.UTF8.GetBytes("hello, hello!");

            int err = deflater.Init(JZlib.Z_DEFAULT_COMPRESSION, 15 + 16);
            Assert.Equal(JZlib.Z_OK, err);

            deflater.SetInput(data);
            deflater.SetOutput(compr);

            while (deflater.total_in < data.Length
                && deflater.total_out < ComprLen)
            {
                deflater.avail_in = 1;
                deflater.avail_out = 1;
                err = deflater.Deflate(JZlib.Z_NO_FLUSH);
                Assert.Equal(JZlib.Z_OK, err);
            }

            do
            {
                deflater.avail_out = 1;
                err = deflater.Deflate(JZlib.Z_FINISH);
            }
            while (err != JZlib.Z_STREAM_END);

            err = deflater.End();
            Assert.Equal(JZlib.Z_OK, err);

            inflater.SetInput(compr);
            inflater.SetOutput(uncompr);

            err = inflater.Init(15 + 32);
            Assert.Equal(JZlib.Z_OK, err);

            bool loop = true;
            while (inflater.total_out < UncomprLen
                && inflater.total_in < ComprLen
                && loop)
            {
                inflater.avail_in = 1; // force small buffers
                inflater.avail_out = 1; // force small buffers
                err = inflater.Inflate(JZlib.Z_NO_FLUSH);
                if (err == JZlib.Z_STREAM_END)
                {
                    loop = false;
                }
                else
                {
                    Assert.Equal(JZlib.Z_OK, err);
                }
            }

            err = inflater.End();
            Assert.Equal(JZlib.Z_OK, err);

            int totalOut = (int)inflater.total_out;
            var actual = new byte[totalOut];
            Array.Copy(uncompr, 0, actual, 0, totalOut);
            Assert.True(data.SequenceEqual(actual));
        }
    }
}
