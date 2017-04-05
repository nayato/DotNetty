// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using DotNetty.Common.Utilities;
    using Xunit;

    public sealed class CombinedHttpHeadersTest
    {
        static readonly ICharSequence HeaderName = (StringCharSequence)"testHeader";

        [Fact]
        public void AddCharSequencesCsv()
        {
            var headers = new CombinedHttpHeaders();
            headers.Add(HeaderName, HttpHeadersTestUtils.HeaderValue.Three.AsList());
            AssertCsvValues(headers, HttpHeadersTestUtils.HeaderValue.Three);
        }

        [Fact]
        public void AddCharSequencesCsvWithExistingHeader()
        {
            var headers = new CombinedHttpHeaders();
            headers.Add(HeaderName, HttpHeadersTestUtils.HeaderValue.Three.AsList());
            headers.Add(HeaderName, HttpHeadersTestUtils.HeaderValue.Five.Subset(4));
            AssertCsvValues(headers, HttpHeadersTestUtils.HeaderValue.Five);
        }

        [Fact]
        public void AddCombinedHeadersWhenEmpty()
        {
            var headers = new CombinedHttpHeaders();
            var otherHeaders = new CombinedHttpHeaders();
            otherHeaders.Add(HeaderName, "a");
            otherHeaders.Add(HeaderName, "b");
            headers.Add(otherHeaders);
            Assert.Equal("a,b", headers.Get(HeaderName).ToString());
        }

        [Fact]
        public void AddCombinedHeadersWhenNotEmpty()
        {
            var headers = new CombinedHttpHeaders();
            headers.Add(HeaderName, "a");
            var otherHeaders = new CombinedHttpHeaders();
            otherHeaders.Add(HeaderName, "b");
            otherHeaders.Add(HeaderName, "c");
            headers.Add(otherHeaders);
            Assert.Equal("a,b,c", headers.Get(HeaderName).ToString());
        }

        [Fact]
        public void SetCombinedHeadersWhenNotEmpty()
        {
            var headers = new CombinedHttpHeaders();
            headers.Add(HeaderName, "a");
            var otherHeaders = new CombinedHttpHeaders();
            otherHeaders.Add(HeaderName, "b");
            otherHeaders.Add(HeaderName, "c");
            headers.Set(otherHeaders);
            Assert.Equal("b,c", headers.Get(HeaderName).ToString());
        }

        [Fact]
        public void AddUncombinedHeaders()
        {
            var headers = new CombinedHttpHeaders();
            headers.Add(HeaderName, "a");
            var otherHeaders = new DefaultHttpHeaders();
            otherHeaders.Add(HeaderName, "b");
            otherHeaders.Add(HeaderName, "c");
            headers.Add(otherHeaders);
            Assert.Equal("a,b,c", headers.Get(HeaderName).ToString());
        }

        [Fact]
        public void SetUncombinedHeaders()
        {
            var headers = new CombinedHttpHeaders();
            headers.Add(HeaderName, "a");
            var otherHeaders = new DefaultHttpHeaders();
            otherHeaders.Add(HeaderName, "b");
            otherHeaders.Add(HeaderName, "c");
            headers.Set(otherHeaders);
            Assert.Equal("b,c", headers.Get(HeaderName).ToString());
        }

        [Fact]
        public void AddCharSequencesCsvWithValueContainingComma()
        {
            var headers = new CombinedHttpHeaders();
            headers.Add(HeaderName, HttpHeadersTestUtils.HeaderValue.SixQuoted.Subset(4));
            Assert.True(AsciiString.ContentEquals((StringCharSequence)HttpHeadersTestUtils.HeaderValue.SixQuoted.SubsetAsCsvString(4), headers.Get(HeaderName)));
            Assert.Equal(HttpHeadersTestUtils.HeaderValue.SixQuoted.Subset(4), headers.GetAll(HeaderName));
        }

        [Fact]
        public void AddCharSequencesCsvWithValueContainingCommas()
        {
            var headers = new CombinedHttpHeaders();
            headers.Add(HeaderName, HttpHeadersTestUtils.HeaderValue.Eight.Subset(6));
            Assert.True(AsciiString.ContentEquals((StringCharSequence)HttpHeadersTestUtils.HeaderValue.Eight.SubsetAsCsvString(6), headers.Get(HeaderName)));
            Assert.Equal(HttpHeadersTestUtils.HeaderValue.Eight.Subset(6), headers.GetAll(HeaderName));
        }

        [Fact]
        public void AddCharSequencesCsvMultipleTimes()
        {
            var headers = new CombinedHttpHeaders();
            for (int i = 0; i < 5; ++i)
            {
                headers.Add(HeaderName, "value");
            }
            Assert.True(AsciiString.ContentEquals((StringCharSequence)"value,value,value,value,value", headers.Get(HeaderName)));
        }

        [Fact]
        public void AddCharSequenceCsv()
        {
            var headers = new CombinedHttpHeaders();
            AddValues(headers, HttpHeadersTestUtils.HeaderValue.One, HttpHeadersTestUtils.HeaderValue.Two, HttpHeadersTestUtils.HeaderValue.Three);
            AssertCsvValues(headers, HttpHeadersTestUtils.HeaderValue.Three);
        }

        [Fact]
        public void AddCharSequenceCsvSingleValue()
        {
            var headers = new CombinedHttpHeaders();
            AddValues(headers, HttpHeadersTestUtils.HeaderValue.One);
            AssertCsvValue(headers, HttpHeadersTestUtils.HeaderValue.One);
        }

        [Fact]
        public void AddIterableCsv()
        {
            var headers = new CombinedHttpHeaders();
            headers.Add(HeaderName, HttpHeadersTestUtils.HeaderValue.Three.AsList());
            AssertCsvValues(headers, HttpHeadersTestUtils.HeaderValue.Three);
        }

        [Fact]
        public void AddIterableCsvWithExistingHeader()
        {
            var headers = new CombinedHttpHeaders();
            headers.Add(HeaderName, HttpHeadersTestUtils.HeaderValue.Three.AsList());
            headers.Add(HeaderName, HttpHeadersTestUtils.HeaderValue.Five.Subset(4));
            AssertCsvValues(headers, HttpHeadersTestUtils.HeaderValue.Five);
        }

        [Fact]
        public void AddIterableCsvSingleValue()
        {
            var headers = new CombinedHttpHeaders();
            headers.Add(HeaderName, HttpHeadersTestUtils.HeaderValue.One.AsList());
            AssertCsvValue(headers, HttpHeadersTestUtils.HeaderValue.One);
        }

        [Fact]
        public void AddIterableCsvEmpty()
        {
            var headers = new CombinedHttpHeaders();
            headers.Add(HeaderName, new List<ICharSequence>());
            Assert.Equal(0, headers.GetAll(HeaderName).Count);
        }

        [Fact]
        public void AddObjectCsv()
        {
            var headers = new CombinedHttpHeaders();
            AddObjectValues(headers, HttpHeadersTestUtils.HeaderValue.One, HttpHeadersTestUtils.HeaderValue.Two, HttpHeadersTestUtils.HeaderValue.Three);
            AssertCsvValues(headers, HttpHeadersTestUtils.HeaderValue.Three);
        }

        [Fact]
        public void AddObjectsCsv()
        {
            var headers = new CombinedHttpHeaders();
            List<ICharSequence> list = HttpHeadersTestUtils.HeaderValue.Three.AsList();
            Assert.Equal(3, list.Count);
            headers.Add(HeaderName, list);
            AssertCsvValues(headers, HttpHeadersTestUtils.HeaderValue.Three);
        }

        [Fact]
        public void AddObjectsIterableCsv()
        {
            var headers = new CombinedHttpHeaders();
            headers.Add(HeaderName, HttpHeadersTestUtils.HeaderValue.Three.AsList());
            AssertCsvValues(headers, HttpHeadersTestUtils.HeaderValue.Three);
        }

        [Fact]
        public void AddObjectsCsvWithExistingHeader()
        {
            var headers = new CombinedHttpHeaders();
            headers.Add(HeaderName, HttpHeadersTestUtils.HeaderValue.Three.AsList());
            headers.Add(HeaderName, HttpHeadersTestUtils.HeaderValue.Five.Subset(4));
            AssertCsvValues(headers, HttpHeadersTestUtils.HeaderValue.Five);
        }

        [Fact]
        public void SetCharSequenceCsv()
        {
            var headers = new CombinedHttpHeaders();
            headers.Set(HeaderName, HttpHeadersTestUtils.HeaderValue.Three.AsList());
            AssertCsvValues(headers, HttpHeadersTestUtils.HeaderValue.Three);
        }

        [Fact]
        public void SetIterableCsv()
        {
            var headers = new CombinedHttpHeaders();
            headers.Set(HeaderName, HttpHeadersTestUtils.HeaderValue.Three.AsList());
            AssertCsvValues(headers, HttpHeadersTestUtils.HeaderValue.Three);
        }

        [Fact]
        public void SetObjectObjectsCsv()
        {
            var headers = new CombinedHttpHeaders();
            headers.Set(HeaderName, HttpHeadersTestUtils.HeaderValue.Three.AsList());
            AssertCsvValues(headers, HttpHeadersTestUtils.HeaderValue.Three);
        }

        [Fact]
        public void SetObjectIterableCsv()
        {
            var headers = new CombinedHttpHeaders();
            headers.Set(HeaderName, HttpHeadersTestUtils.HeaderValue.Three.AsList());
            AssertCsvValues(headers, HttpHeadersTestUtils.HeaderValue.Three);
        }

        [Fact]
        public void GetAll()
        {
            var headers = new CombinedHttpHeaders();
            headers.Set(HeaderName, new List<ICharSequence> { (StringCharSequence)"a", (StringCharSequence)"b", (StringCharSequence)"c" });
            var expected = new ICharSequence[] { (StringCharSequence)"a", (StringCharSequence)"b", (StringCharSequence)"c" };
            IList<ICharSequence> actual = headers.GetAll(HeaderName);
            Assert.True(expected.SequenceEqual(actual));

            headers.Set(HeaderName, new List<ICharSequence> { (StringCharSequence)"a,", (StringCharSequence)"b,", (StringCharSequence)"c," });
            expected = new ICharSequence[] { (StringCharSequence)"a,", (StringCharSequence)"b,", (StringCharSequence)"c," };
            actual = headers.GetAll(HeaderName);
            Assert.True(expected.SequenceEqual(actual));

            headers.Set(HeaderName, new List<ICharSequence> { (StringCharSequence)"a\"", (StringCharSequence)"b\"", (StringCharSequence)"c\"" });
            expected = new ICharSequence[] { (StringCharSequence)"a\"", (StringCharSequence)"b\"", (StringCharSequence)"c\"" };
            actual = headers.GetAll(HeaderName);
            Assert.True(expected.SequenceEqual(actual));

            headers.Set(HeaderName, new List<ICharSequence> { (StringCharSequence)"\"a\"", (StringCharSequence)"\"b\"", (StringCharSequence)"\"c\"" });
            expected = new ICharSequence[] { (StringCharSequence)"a", (StringCharSequence)"b", (StringCharSequence)"c" };
            actual = headers.GetAll(HeaderName);
            Assert.True(expected.SequenceEqual(actual));

            headers.Set(HeaderName, (StringCharSequence)"a,b,c");
            expected = new ICharSequence[] { (StringCharSequence)"a,b,c" };
            actual = headers.GetAll(HeaderName);
            Assert.True(expected.SequenceEqual(actual));

            headers.Set(HeaderName, (StringCharSequence)"\"a,b,c\"");
            actual = headers.GetAll(HeaderName);
            Assert.True(expected.SequenceEqual(actual));
        }

        [Fact]
        public void OwsTrimming()
        {
            var headers = new CombinedHttpHeaders();
            headers.Set(HeaderName, new List<ICharSequence> { (StringCharSequence)"\ta", (StringCharSequence)"   ", (StringCharSequence)"  b ", (StringCharSequence)"\t \t"});
            headers.Add(HeaderName, new List<ICharSequence> { (StringCharSequence)" c, d \t" });

            var expected = new List<ICharSequence> { (StringCharSequence)"a", (StringCharSequence)"", (StringCharSequence)"b", (StringCharSequence)"", (StringCharSequence)"c, d" };
            IList<ICharSequence> actual = headers.GetAll(HeaderName);
            Assert.True(expected.SequenceEqual(actual));
            Assert.Equal("a,,b,,\"c, d\"", headers.Get(HeaderName).ToString());

            Assert.True(headers.ContainsValue(HeaderName, (StringCharSequence)"a", true));
            Assert.True(headers.ContainsValue(HeaderName, (StringCharSequence)" a ", true));
            Assert.True(headers.ContainsValue(HeaderName, (StringCharSequence)"a", true));
            Assert.False(headers.ContainsValue(HeaderName, (StringCharSequence)"a,b", true));

            Assert.False(headers.ContainsValue(HeaderName, (StringCharSequence)" c, d ", true));
            Assert.False(headers.ContainsValue(HeaderName, (StringCharSequence)"c, d", true));
            Assert.True(headers.ContainsValue(HeaderName, (StringCharSequence)" c ", true));
            Assert.True(headers.ContainsValue(HeaderName, (StringCharSequence)"d", true));

            Assert.True(headers.ContainsValue(HeaderName, (StringCharSequence)"\t", true));
            Assert.True(headers.ContainsValue(HeaderName, (StringCharSequence)"", true));

            Assert.False(headers.ContainsValue(HeaderName, (StringCharSequence)"e", true));

            var copiedHeaders = new CombinedHttpHeaders();
            copiedHeaders.Add(headers);
            Assert.Equal(new List<ICharSequence>{ (StringCharSequence)"a", (StringCharSequence)"", (StringCharSequence)"b", (StringCharSequence)"", (StringCharSequence)"c, d" }, copiedHeaders.GetAll(HeaderName));
        }

        static void AddValues(CombinedHttpHeaders headers, params HttpHeadersTestUtils.HeaderValue[] headerValues)
        {
            foreach (HttpHeadersTestUtils.HeaderValue v in headerValues)
            {
                headers.Add(HeaderName, (StringCharSequence)v.ToString());
            }
        }

        static void AddObjectValues(CombinedHttpHeaders headers, params HttpHeadersTestUtils.HeaderValue[] headerValues)
        {
            foreach (HttpHeadersTestUtils.HeaderValue v in headerValues)
            {
                headers.Add(HeaderName, v.ToString());
            }
        }

        static void AssertCsvValues(CombinedHttpHeaders headers, HttpHeadersTestUtils.HeaderValue headerValue)
        {
            Assert.True(AsciiString.ContentEquals(headerValue.AsCsv(), headers.Get(HeaderName)));

            List<ICharSequence> expected = headerValue.AsList();
            IList<ICharSequence> values = headers.GetAll(HeaderName);

            Assert.Equal(expected.Count, values.Count);
            for (int i = 0; i < expected.Count; i++)
            {
                Assert.True(expected[i].SequenceEquals(values[i], false));
            }
        }

        static void AssertCsvValue(CombinedHttpHeaders headers, HttpHeadersTestUtils.HeaderValue headerValue)
        {
            Assert.True(AsciiString.ContentEquals((StringCharSequence)headerValue.ToString(), headers.Get(HeaderName)));
            Assert.True(AsciiString.ContentEquals((StringCharSequence)headerValue.ToString(), headers.GetAll(HeaderName)[0]));
        }
    }
}
