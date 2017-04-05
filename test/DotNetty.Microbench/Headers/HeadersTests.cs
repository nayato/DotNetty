// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Microbench.Headers
{
    using System.Collections.Generic;
    using DotNetty.Codecs;
    using DotNetty.Codecs.Http;
    using DotNetty.Common.Utilities;
    using DotNetty.Microbench.Utilities;
    using Xunit;
    using Xunit.Abstractions;

    public sealed class HeadersTests
    {
        readonly ITestOutputHelper output;
        readonly Blackhole blackhole = new Blackhole();

        class TestHeaderSet
        {
            public readonly DefaultHttpHeaders HttpHeaders;
            public readonly List<AsciiString> Keys;
            public readonly List<AsciiString> Values;

            public TestHeaderSet(Dictionary<string, string> headers)
            {
                this.HttpHeaders = new DefaultHttpHeaders();
                this.Keys = new List<AsciiString>();
                this.Values = new List<AsciiString>();

                foreach (string name in headers.Keys)
                {
                    string httpName = ToHttpName(name);
                    var asciiKey = new AsciiString(httpName);
                    var asciiValue = new AsciiString(headers[name]);

                    this.Keys.Add(asciiKey);
                    this.Values.Add(asciiValue);

                    this.HttpHeaders.Add(asciiKey, asciiValue);
                }
            }
        }

        public HeadersTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        static Dictionary<string, TestHeaderSet> GetSamples()
        {
            var testSamples = new Dictionary<string, TestHeaderSet>();
            Dictionary<HeaderExample, Dictionary<string, string>> sample = ExampleHeaders.GetExamples();
            foreach (HeaderExample headerExample in sample.Keys)
            {
                testSamples.Add(headerExample.ToString(), new TestHeaderSet(sample[headerExample]));
            }

            return testSamples;
        }

        [Fact]
        public void AddAllAndRemove()
        {
            Dictionary<string, TestHeaderSet> testSamples = GetSamples();
            CodeTimer.Benchmark(testSamples, "Remove {0}", 1000, this.output,
                sample =>
                {
                    var httpHeaders = new DefaultHttpHeaders();
                    httpHeaders.Add(sample.HttpHeaders);
                    foreach (AsciiString name in sample.Keys)
                    {
                        this.blackhole.Consume(httpHeaders.Remove(name));
                    }
                });
        }

        [Fact]
        public void Get()
        {
            Dictionary<string, TestHeaderSet> testSamples = GetSamples();
            CodeTimer.Benchmark(testSamples, "Get {0}", 1000, this.output,
                sample =>
                {
                    foreach (AsciiString name in sample.Keys)
                    {
                        this.blackhole.Consume(sample.HttpHeaders.Get(name));
                    }
                });
        }

        [Fact]
        public void Put()
        {
            Dictionary<string, TestHeaderSet> testSamples = GetSamples();
            CodeTimer.Benchmark(testSamples, "Put {0}", 1000, this.output,
            sample =>
            {
                var headers = new DefaultHttpHeaders(false);
                for (int i = 0; i < sample.Keys.Count; i++)
                {
                    headers.Add(sample.Keys[i], sample.Values[i]);
                }
            });
        }

        [Fact]
        public void Iterate()
        {
            Dictionary<string, TestHeaderSet> testSamples = GetSamples();
            CodeTimer.Benchmark(testSamples, "Iterate {0}", 1000, this.output,
                sample =>
                {
                    foreach (HeaderEntry<ICharSequence, ICharSequence> entry in sample.HttpHeaders)
                    {
                        this.blackhole.Consume(entry);
                    }
                });
        }

        [Fact]
        public void AddAll()
        {
            Dictionary<string, TestHeaderSet> testSamples = GetSamples();
            CodeTimer.Benchmark(testSamples, "AddAll {0}", 1000, this.output,
            sample =>
            {
                var emptyHttpHeaders = new DefaultHttpHeaders();
                this.blackhole.Consume(emptyHttpHeaders.Add(sample.HttpHeaders));
            });
        }

        [Fact]
        public void AddAllNoValidate()
        {
            Dictionary<string, TestHeaderSet> testSamples = GetSamples();
            CodeTimer.Benchmark(testSamples, "AddAll {0} (No validate)", 1000, this.output,
            sample =>
            {
                var emptyHttpHeadersNoValidate = new DefaultHttpHeaders(false);
                this.blackhole.Consume(emptyHttpHeadersNoValidate.Add(sample.HttpHeaders));
            });
        }


        static string ToHttpName(string name) => (name.StartsWith(":")) ? name.Substring(1) : name;
    }
}
