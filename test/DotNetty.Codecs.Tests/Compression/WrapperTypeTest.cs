// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
/*
 * Copyright (c) 2000-2011 ymnk, JCraft,Inc. All rights reserved.
 * 
 * This program is based on zlib-1.1.3, so all credit should go authors
 * Jean-loup Gailly(jloup@gzip.org) and Mark Adler(madler@alumni.caltech.edu)
 * and contributors of zlib.
 * 
 * https://github.com/ymnk/jzlib/blob/master/src/test/scala/WrapperTypeTest.scala
 */
namespace DotNetty.Codecs.Tests.Compression
{
    using System.Collections.Generic;
    using System.Text;
    using DotNetty.Codecs.Compression;
    using Xunit;

    public sealed class WrapperTypeTest
    {
        const int ComprLen = 40000;
        const int UncomprLen = ComprLen;

        static IEnumerable<object[]> Cases()
        {
            // GOOD
            yield return new object[] { true, JZlib.W_ZLIB, JZlib.W_ZLIB, JZlib.W_ANY };
            yield return new object[] { true, JZlib.W_GZIP, JZlib.W_GZIP, JZlib.W_ANY };
            yield return new object[] { true, JZlib.W_NONE, JZlib.W_NONE, JZlib.W_ANY };

            // BAD
            yield return new object[] { false, JZlib.W_ZLIB, JZlib.W_GZIP, JZlib.W_NONE };
            yield return new object[] { false, JZlib.W_GZIP, JZlib.W_ZLIB, JZlib.W_NONE };
            yield return new object[] { false, JZlib.W_NONE, JZlib.W_ZLIB, JZlib.W_GZIP };
        }

        [Theory]
        [MemberData(nameof(Cases))]
        public void DetectDataInputType(bool flag, JZlib.WrapperType wrapper, JZlib.WrapperType type1, JZlib.WrapperType type2)
        {
            var compr = new byte[ComprLen];
            var uncompr = new byte[UncomprLen];
            byte[] data = Encoding.UTF8.GetBytes("hello, hello!");

            var deflater = new ZStream();
            int err = deflater.DeflateInit(JZlib.Z_BEST_SPEED, JZlib.DEF_WBITS, 9, wrapper);
            Assert.Equal(JZlib.Z_OK, err);

            Deflate(deflater, data, compr);
            if (flag)
            {
                ZStream inflater = Inflate(compr, uncompr, type1);
                int totalOut = (int)inflater.total_out;
                string actual = Encoding.UTF8.GetString(uncompr, 0, totalOut);
                Assert.Equal("hello, hello!", actual);

                inflater = Inflate(compr, uncompr, type2);
                totalOut = (int)inflater.total_out;
                actual = Encoding.UTF8.GetString(uncompr, 0, totalOut);
                Assert.Equal("hello, hello!", actual);
            }
            else
            {
                InflateFail(compr, uncompr, type1);
                InflateFail(compr, uncompr, type2);
            }
        }

        [Fact]
        public void DeflaterWbitsPlus32()
        {
            var compr = new byte[ComprLen];
            var uncompr = new byte[UncomprLen];
            byte[] data = Encoding.UTF8.GetBytes("hello, hello!");

            var deflater = new Deflater();
            int err = deflater.Init(JZlib.Z_BEST_SPEED, JZlib.DEF_WBITS, 9);
            Assert.Equal(JZlib.Z_OK, err);

            Deflate(deflater, data, compr);

            var inflater = new Inflater();
            err = inflater.Init(JZlib.DEF_WBITS + 32);
            Assert.Equal(JZlib.Z_OK, err);

            inflater.SetInput(compr);

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
            string actual = Encoding.UTF8.GetString(uncompr, 0, totalOut);
            Assert.Equal("hello, hello!", actual);

            deflater = new Deflater();
            err = deflater.Init(JZlib.Z_BEST_SPEED, JZlib.DEF_WBITS + 16, 9);
            Assert.Equal(JZlib.Z_OK, err);

            Deflate(deflater, data, compr);

            inflater = new Inflater();
            err = inflater.Init(JZlib.DEF_WBITS + 32);
            Assert.Equal(JZlib.Z_OK, err);

            inflater.SetInput(compr);
            loop = true;
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

            totalOut = (int)inflater.total_out;
            actual = Encoding.UTF8.GetString(uncompr, 0, totalOut);
            Assert.Equal("hello, hello!", actual);
        }

        static void Deflate(ZStream deflater, byte[] data, byte[] compr)
        {
            deflater.SetInput(data);
            deflater.SetOutput(compr);

            int err = deflater.Deflate_z(JZlib.Z_FINISH);
            Assert.Equal(JZlib.Z_STREAM_END, err);

            err = deflater.End();
            Assert.Equal(JZlib.Z_OK, err);
        }

        static ZStream Inflate(byte[] compr, byte[] uncompr, JZlib.WrapperType w)
        {
            var inflater = new ZStream();
            int err = inflater.InflateInit(w);
            Assert.Equal(JZlib.Z_OK, err);

            inflater.SetInput(compr);

            bool loop = true;
            while (loop)
            {
                inflater.SetOutput(uncompr);
                err = inflater.Inflate_z(JZlib.Z_NO_FLUSH);
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

            return inflater;
        }

        static void InflateFail(byte[] compr, byte[] uncompr, JZlib.WrapperType w)
        {
            var inflater = new ZStream();

            int err = inflater.InflateInit(w);
            Assert.Equal(JZlib.Z_OK, err);

            inflater.SetInput(compr);

            bool loop = true;
            while (loop)
            {
                inflater.SetOutput(uncompr);
                err = inflater.Inflate_z(JZlib.Z_NO_FLUSH);
                if (err == JZlib.Z_STREAM_END)
                {
                    loop = false;
                }
                else
                {
                    Assert.Equal(JZlib.Z_DATA_ERROR, err);
                    loop = false;
                }
            }
        }
    }
}
