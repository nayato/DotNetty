// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http.Tests.Multipart
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using DotNetty.Buffers;
    using DotNetty.Codecs.Http.Multipart;
    using DotNetty.Common.Utilities;
    using Xunit;

    public sealed class HttpPostRequestEncoderTest : IDisposable
    {
        readonly List<IDisposable> files = new List<IDisposable>();

        [Fact]
        public void AllowedMethods()
        {
            FileStream fileStream = File.Open("./Multipart/file-01.txt", FileMode.Open, FileAccess.Read);
            this.files.Add(fileStream);

            ShouldThrowExceptionIfNotAllowed(HttpMethod.Connect, fileStream);
            ShouldThrowExceptionIfNotAllowed(HttpMethod.Put, fileStream);
            ShouldThrowExceptionIfNotAllowed(HttpMethod.Post, fileStream);
            ShouldThrowExceptionIfNotAllowed(HttpMethod.Patch, fileStream);
            ShouldThrowExceptionIfNotAllowed(HttpMethod.Delete, fileStream);
            ShouldThrowExceptionIfNotAllowed(HttpMethod.Get, fileStream);
            ShouldThrowExceptionIfNotAllowed(HttpMethod.Head, fileStream);
            ShouldThrowExceptionIfNotAllowed(HttpMethod.Options, fileStream);
            Assert.Throws<ErrorDataEncoderException>(
                () => ShouldThrowExceptionIfNotAllowed(HttpMethod.Trace, fileStream));
        }

        static void ShouldThrowExceptionIfNotAllowed(HttpMethod method, FileStream fileStream)
        {
            fileStream.Position = 0; // Reset to the begining
            var request = new DefaultFullHttpRequest(HttpVersion.Http11, method, "http://localhost");

            var encoder = new HttpPostRequestEncoder(request, true);
            encoder.AddBodyAttribute("foo", "bar");
            encoder.AddBodyFileUpload("quux", fileStream, "text/plain", false);

            string multipartDataBoundary = encoder.MultipartDataBoundary;
            string content = GetRequestBody(encoder);

            string expected = "--" + multipartDataBoundary + "\r\n" +
                HttpHeaderNames.ContentDisposition + ": form-data; name=\"foo\"" + "\r\n" +
                HttpHeaderNames.ContentLength + ": 3" + "\r\n" +
                HttpHeaderNames.ContentType + ": text/plain; charset=utf-8" + "\r\n" +
                "\r\n" +
                "bar" +
                "\r\n" +
                "--" + multipartDataBoundary + "\r\n" +
                HttpHeaderNames.ContentDisposition + ": form-data; name=\"quux\"; filename=\"file-01.txt\"" + "\r\n" +
                HttpHeaderNames.ContentLength + ": " + fileStream.Length + "\r\n" +
                HttpHeaderNames.ContentType + ": text/plain" + "\r\n" +
                HttpHeaderNames.ContentTransferEncoding + ": binary" + "\r\n" +
                "\r\n" +
                "File 01" + StringUtil.Newline +
                "\r\n" +
                "--" + multipartDataBoundary + "--" + "\r\n";

            Assert.Equal(expected, content);
        }

        [Fact]
        public void SingleFileUploadNoName()
        {
            var request = new DefaultFullHttpRequest(HttpVersion.Http11, HttpMethod.Post, "http://localhost");
            var encoder = new HttpPostRequestEncoder(request, true);

            FileStream fileStream = File.Open("./Multipart/file-01.txt", FileMode.Open, FileAccess.Read);
            this.files.Add(fileStream);
            encoder.AddBodyAttribute("foo", "bar");
            encoder.AddBodyFileUpload("quux", "", fileStream, "text/plain", false);

            string multipartDataBoundary = encoder.MultipartDataBoundary;
            string content = GetRequestBody(encoder);

            string expected = "--" + multipartDataBoundary + "\r\n" +
                    HttpHeaderNames.ContentDisposition + ": form-data; name=\"foo\"" + "\r\n" +
                    HttpHeaderNames.ContentLength + ": 3" + "\r\n" +
                    HttpHeaderNames.ContentType + ": text/plain; charset=utf-8" + "\r\n" +
                    "\r\n" +
                    "bar" +
                    "\r\n" +
                    "--" + multipartDataBoundary + "\r\n" +
                    HttpHeaderNames.ContentDisposition + ": form-data; name=\"quux\"\r\n" +
                    HttpHeaderNames.ContentLength + ": " + fileStream.Length + "\r\n" +
                    HttpHeaderNames.ContentType + ": text/plain" + "\r\n" +
                    HttpHeaderNames.ContentTransferEncoding + ": binary" + "\r\n" +
                    "\r\n" +
                    "File 01" + StringUtil.Newline +
                    "\r\n" +
                    "--" + multipartDataBoundary + "--" + "\r\n";

            Assert.Equal(expected, content);
        }

        [Fact]
        public void MultiFileUploadInMixedMode()
        {
            var request = new DefaultFullHttpRequest(HttpVersion.Http11, HttpMethod.Post, "http://localhost");
            var encoder = new HttpPostRequestEncoder(request, true);

            FileStream fileStream1 = File.Open("./Multipart/file-01.txt", FileMode.Open, FileAccess.Read);
            this.files.Add(fileStream1);
            FileStream fileStream2 = File.Open("./Multipart/file-02.txt", FileMode.Open, FileAccess.Read);
            this.files.Add(fileStream2);

            encoder.AddBodyAttribute("foo", "bar");
            encoder.AddBodyFileUpload("quux", fileStream1, "text/plain", false);
            encoder.AddBodyFileUpload("quux", fileStream2, "text/plain", false);

            // We have to query the value of these two fields before finalizing
            // the request, which unsets one of them.
            string multipartDataBoundary = encoder.MultipartDataBoundary;
            string multipartMixedBoundary = encoder.MultipartMixedBoundary;
            string content = GetRequestBody(encoder);

            string expected = "--" + multipartDataBoundary + "\r\n" +
                    HttpHeaderNames.ContentDisposition + ": form-data; name=\"foo\"" + "\r\n" +
                    HttpHeaderNames.ContentLength + ": 3" + "\r\n" +
                    HttpHeaderNames.ContentType + ": text/plain; charset=utf-8" + "\r\n" +
                    "\r\n" +
                    "bar" + "\r\n" +
                    "--" + multipartDataBoundary + "\r\n" +
                    HttpHeaderNames.ContentDisposition + ": form-data; name=\"quux\"" + "\r\n" +
                    HttpHeaderNames.ContentType + ": multipart/mixed; boundary=" + multipartMixedBoundary + "\r\n" +
                    "\r\n" +
                    "--" + multipartMixedBoundary + "\r\n" +
                    HttpHeaderNames.ContentDisposition + ": attachment; filename=\"file-02.txt\"" + "\r\n" +
                    HttpHeaderNames.ContentLength + ": " + fileStream1.Length + "\r\n" +
                    HttpHeaderNames.ContentType + ": text/plain" + "\r\n" +
                    HttpHeaderNames.ContentTransferEncoding + ": binary" + "\r\n" +
                    "\r\n" +
                    "File 01" + StringUtil.Newline +
                    "\r\n" +
                    "--" + multipartMixedBoundary + "\r\n" +
                    HttpHeaderNames.ContentDisposition + ": attachment; filename=\"file-02.txt\"" + "\r\n" +
                    HttpHeaderNames.ContentLength + ": " + fileStream2.Length + "\r\n" +
                    HttpHeaderNames.ContentType + ": text/plain" + "\r\n" +
                    HttpHeaderNames.ContentTransferEncoding + ": binary" + "\r\n" +
                    "\r\n" +
                    "File 02" + StringUtil.Newline +
                    "\r\n" +
                    "--" + multipartMixedBoundary + "--" + "\r\n" +
                    "--" + multipartDataBoundary + "--" + "\r\n";

            Assert.Equal(expected, content);
        }

        [Fact]
        public void MultiFileUploadInMixedModeNoName()
        {
            var request = new DefaultFullHttpRequest(HttpVersion.Http11, HttpMethod.Post, "http://localhost");
            var encoder = new HttpPostRequestEncoder(request, true);

            FileStream fileStream1 = File.Open("./Multipart/file-01.txt", FileMode.Open, FileAccess.Read);
            this.files.Add(fileStream1);
            FileStream fileStream2 = File.Open("./Multipart/file-02.txt", FileMode.Open, FileAccess.Read);
            this.files.Add(fileStream2);

            encoder.AddBodyAttribute("foo", "bar");
            encoder.AddBodyFileUpload("quux", "", fileStream1, "text/plain", false);
            encoder.AddBodyFileUpload("quux", "", fileStream2, "text/plain", false);

            // We have to query the value of these two fields before finalizing
            // the request, which unsets one of them.
            string multipartDataBoundary = encoder.MultipartDataBoundary;
            string multipartMixedBoundary = encoder.MultipartMixedBoundary;
            string content = GetRequestBody(encoder);

            string expected = "--" + multipartDataBoundary + "\r\n" +
                HttpHeaderNames.ContentDisposition + ": form-data; name=\"foo\"" + "\r\n" +
                HttpHeaderNames.ContentLength + ": 3" + "\r\n" +
                HttpHeaderNames.ContentType + ": text/plain; charset=utf-8" + "\r\n" +
                "\r\n" +
                "bar" + "\r\n" +
                "--" + multipartDataBoundary + "\r\n" +
                HttpHeaderNames.ContentDisposition + ": form-data; name=\"quux\"" + "\r\n" +
                HttpHeaderNames.ContentType + ": multipart/mixed; boundary=" + multipartMixedBoundary + "\r\n" +
                "\r\n" +
                "--" + multipartMixedBoundary + "\r\n" +
                HttpHeaderNames.ContentDisposition + ": attachment\r\n" +
                HttpHeaderNames.ContentLength + ": " + fileStream1.Length + "\r\n" +
                HttpHeaderNames.ContentType + ": text/plain" + "\r\n" +
                HttpHeaderNames.ContentTransferEncoding + ": binary" + "\r\n" +
                "\r\n" +
                "File 01" + StringUtil.Newline +
                "\r\n" +
                "--" + multipartMixedBoundary + "\r\n" +
                HttpHeaderNames.ContentDisposition + ": attachment\r\n" +
                HttpHeaderNames.ContentLength + ": " + fileStream2.Length + "\r\n" +
                HttpHeaderNames.ContentType + ": text/plain" + "\r\n" +
                HttpHeaderNames.ContentTransferEncoding + ": binary" + "\r\n" +
                "\r\n" +
                "File 02" + StringUtil.Newline +
                "\r\n" +
                "--" + multipartMixedBoundary + "--" + "\r\n" +
                "--" + multipartDataBoundary + "--" + "\r\n";

            Assert.Equal(expected, content);
        }

        [Fact]
        public void SingleFileUploadInHtml5Mode()
        {
            var request = new DefaultFullHttpRequest(HttpVersion.Http11, HttpMethod.Post, "http://localhost");
            var factory = new DefaultHttpDataFactory(DefaultHttpDataFactory.MinSize);
            var encoder = new HttpPostRequestEncoder(factory, request, true, Encoding.UTF8,
                HttpPostRequestEncoder.EncoderMode.HTML5);

            FileStream fileStream1 = File.Open("./Multipart/file-01.txt", FileMode.Open, FileAccess.Read);
            this.files.Add(fileStream1);
            FileStream fileStream2 = File.Open("./Multipart/file-02.txt", FileMode.Open, FileAccess.Read);
            this.files.Add(fileStream2);

            encoder.AddBodyAttribute("foo", "bar");
            encoder.AddBodyFileUpload("quux", fileStream1, "text/plain", false);
            encoder.AddBodyFileUpload("quux", fileStream2, "text/plain", false);

            string multipartDataBoundary = encoder.MultipartDataBoundary;
            string content = GetRequestBody(encoder);

            string expected = "--" + multipartDataBoundary + "\r\n" +
                HttpHeaderNames.ContentDisposition + ": form-data; name=\"foo\"" + "\r\n" +
                HttpHeaderNames.ContentLength + ": 3" + "\r\n" +
                HttpHeaderNames.ContentType + ": text/plain; charset=utf-8" + "\r\n" +
                "\r\n" +
                "bar" + "\r\n" +
                "--" + multipartDataBoundary + "\r\n" +
                HttpHeaderNames.ContentDisposition + ": form-data; name=\"quux\"; filename=\"file-01.txt\"" + "\r\n" +
                HttpHeaderNames.ContentLength + ": " + fileStream1.Length + "\r\n" +
                HttpHeaderNames.ContentType + ": text/plain" + "\r\n" +
                HttpHeaderNames.ContentTransferEncoding + ": binary" + "\r\n" +
                "\r\n" +
                "File 01" + StringUtil.Newline + "\r\n" +
                "--" + multipartDataBoundary + "\r\n" +
                HttpHeaderNames.ContentDisposition + ": form-data; name=\"quux\"; filename=\"file-02.txt\"" + "\r\n" +
                HttpHeaderNames.ContentLength + ": " + fileStream2.Length + "\r\n" +
                HttpHeaderNames.ContentType + ": text/plain" + "\r\n" +
                HttpHeaderNames.ContentTransferEncoding + ": binary" + "\r\n" +
                "\r\n" +
                "File 02" + StringUtil.Newline +
                "\r\n" +
                "--" + multipartDataBoundary + "--" + "\r\n";

            Assert.Equal(expected, content);
        }

        [Fact]
        public void MultiFileUploadInHtml5Mode()
        {
            var request = new DefaultFullHttpRequest(HttpVersion.Http11, HttpMethod.Post, "http://localhost");
            var factory = new DefaultHttpDataFactory(DefaultHttpDataFactory.MinSize);

            var encoder = new HttpPostRequestEncoder(factory, request, true, Encoding.UTF8,
                HttpPostRequestEncoder.EncoderMode.HTML5);
            FileStream fileStream1 = File.Open("./Multipart/file-01.txt", FileMode.Open, FileAccess.Read);
            this.files.Add(fileStream1);

            encoder.AddBodyAttribute("foo", "bar");
            encoder.AddBodyFileUpload("quux", fileStream1, "text/plain", false);

            string multipartDataBoundary = encoder.MultipartDataBoundary;
            string content = GetRequestBody(encoder);

            string expected = "--" + multipartDataBoundary + "\r\n" +
                HttpHeaderNames.ContentDisposition + ": form-data; name=\"foo\"" + "\r\n" +
                HttpHeaderNames.ContentLength + ": 3" + "\r\n" +
                HttpHeaderNames.ContentType + ": text/plain; charset=utf-8" + "\r\n" +
                "\r\n" +
                "bar" +
                "\r\n" +
                "--" + multipartDataBoundary + "\r\n" +
                HttpHeaderNames.ContentDisposition + ": form-data; name=\"quux\"; filename=\"file-01.txt\"" + "\r\n" +
                HttpHeaderNames.ContentLength + ": " + fileStream1.Length + "\r\n" +
                HttpHeaderNames.ContentType + ": text/plain" + "\r\n" +
                HttpHeaderNames.ContentTransferEncoding + ": binary" + "\r\n" +
                "\r\n" +
                "File 01" + StringUtil.Newline +
                "\r\n" +
                "--" + multipartDataBoundary + "--" + "\r\n";

            Assert.Equal(expected, content);
        }

        [Fact]
        public void HttpPostRequestEncoderSlicedBuffer()
        {
            var request = new DefaultFullHttpRequest(HttpVersion.Http11, HttpMethod.Post, "http://localhost");

            var encoder = new HttpPostRequestEncoder(request, true);
            // add Form attribute
            encoder.AddBodyAttribute("getform", "POST");
            encoder.AddBodyAttribute("info", "first value");
            encoder.AddBodyAttribute("secondinfo", "secondvalue a&");
            encoder.AddBodyAttribute("thirdinfo", "short text");

            const int Length = 100000;
            var array = new char[Length];
            array.Fill('a');
            string longText = new string(array);
            encoder.AddBodyAttribute("fourthinfo", longText.Substring(0, 7470));

            FileStream fileStream1 = File.Open("./Multipart/file-01.txt", FileMode.Open, FileAccess.Read);
            this.files.Add(fileStream1);
            encoder.AddBodyFileUpload("myfile", fileStream1, "application/x-zip-compressed", false);
            encoder.FinalizeRequest();

            while (!encoder.IsEndOfInput)
            {
                IHttpContent httpContent = encoder.ReadChunk(null);
                IByteBuffer content = httpContent.Content;
                int refCnt = content.ReferenceCount;
                Assert.True((ReferenceEquals(content.Unwrap(), content) || content.Unwrap() == null) && refCnt == 1 
                    || !ReferenceEquals(content.Unwrap(), content) && refCnt == 2, 
                    "content: " + content + " content.unwrap(): " + content.Unwrap() + " refCnt: " + refCnt);
                httpContent.Release();
            }

            encoder.CleanFiles();
            encoder.Close();
        }

        static string GetRequestBody(HttpPostRequestEncoder encoder)
        {
            encoder.FinalizeRequest();
            LinkedList<IPostHttpData> chunks = encoder.MultipartList;

            var buffers = new List<IByteBuffer>();
            LinkedListNode<IPostHttpData> node = chunks.First;
            while (node != null)
            {
                IPostHttpData data = node.Value;
                var attribute = data as InternalAttribute;
                if (attribute != null)
                {
                    buffers.Add(attribute.ToByteBuffer());
                }
                else if (data is IHttpData)
                {
                    buffers.Add(((IHttpData)data).GetByteBuffer());
                }

                node = node.Next;
            }

            IByteBuffer content = Unpooled.WrappedBuffer(buffers.ToArray());
            string result = content.ToString(Encoding.UTF8);
            content.Release();

            return result;
        }

        public void Dispose()
        {
            foreach (IDisposable file in this.files)
            {
                file.Dispose();
            }
        }
    }
}
