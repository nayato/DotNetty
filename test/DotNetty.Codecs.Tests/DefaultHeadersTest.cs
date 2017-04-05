// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using DotNetty.Common.Utilities;
    using Xunit;

    public sealed class DefaultHeadersTest
    {
        sealed class TestDefaultHeaders : DefaultHeaders<ICharSequence, ICharSequence>
        {
            public TestDefaultHeaders() : base(CharSequenceValueConverter.Instance)
            {
            }
        }

        static TestDefaultHeaders NewInstance() => new TestDefaultHeaders();

        [Fact]
        public void AddShouldIncreaseAndRemoveShouldDecreaseTheSize()
        {
            TestDefaultHeaders headers = NewInstance();
            Assert.Equal(0, headers.Size);
            headers.Add(new AsciiString("name1"), new [] { new AsciiString("value1"), new AsciiString("value2") });
            Assert.Equal(2, headers.Size);
            headers.Add(new AsciiString("name2"), new [] { new AsciiString("value3"), new AsciiString("value4")});
            Assert.Equal(4, headers.Size);
            headers.Add(new AsciiString("name3"), new AsciiString("value5"));
            Assert.Equal(5, headers.Size);

            headers.Remove(new AsciiString("name3"));
            Assert.Equal(4, headers.Size);
            headers.Remove(new AsciiString("name1"));
            Assert.Equal(2, headers.Size);
            headers.Remove(new AsciiString("name2"));
            Assert.Equal(0, headers.Size);
            Assert.True(headers.IsEmpty);
        }

        [Fact]
        public void AfterClearHeadersShouldBeEmpty()
        {
            TestDefaultHeaders headers = NewInstance();
            headers.Add(new AsciiString("name1"), new AsciiString("value1"));
            headers.Add(new AsciiString("name2"), new AsciiString("value2"));
            Assert.Equal(2, headers.Size);
            headers.Clear();
            Assert.Equal(0, headers.Size);
            Assert.True(headers.IsEmpty);
            Assert.False(headers.Contains(new AsciiString("name1")));
            Assert.False(headers.Contains(new AsciiString("name2")));
        }

        [Fact]
        public void RemovingANameForASecondTimeShouldReturnFalse()
        {
            TestDefaultHeaders headers = NewInstance();
            headers.Add(new AsciiString("name1"), new AsciiString("value1"));
            headers.Add(new AsciiString("name2"), new AsciiString("value2"));
            Assert.True(headers.Remove(new AsciiString("name2")));
            Assert.False(headers.Remove(new AsciiString("name2")));
        }

        [Fact]
        public void MultipleValuesPerNameShouldBeAllowed()
        {
            TestDefaultHeaders headers = NewInstance();
            headers.Add(new AsciiString("name"), new AsciiString("value1"));
            headers.Add(new AsciiString("name"), new AsciiString("value2"));
            headers.Add(new AsciiString("name"), new AsciiString("value3"));
            Assert.Equal(3, headers.Size);

            IList<ICharSequence> values = headers.GetAll(new AsciiString("name"));
            Assert.Equal(3, values.Count);
            Assert.True(values.SequenceEqual(new[] { new AsciiString("value1"), new AsciiString("value2"), new AsciiString("value3") }));
        }

        [Fact]
        public void Contains()
        {
            TestDefaultHeaders headers = NewInstance();
            headers.AddBoolean(new AsciiString("boolean"), true);
            Assert.True(headers.ContainsBoolean(new AsciiString("boolean"), true));
            Assert.False(headers.ContainsBoolean(new AsciiString("boolean"), false));

            headers.AddLong(new AsciiString("long"), long.MaxValue);
            Assert.True(headers.ContainsLong(new AsciiString("long"), long.MaxValue));
            Assert.False(headers.ContainsLong(new AsciiString("long"), long.MinValue));

            headers.AddInt(new AsciiString("int"), int.MinValue);
            Assert.True(headers.ContainsInt(new AsciiString("int"), int.MinValue));
            Assert.False(headers.ContainsInt(new AsciiString("int"), int.MaxValue));

            headers.AddShort(new AsciiString("short"), short.MaxValue);
            Assert.True(headers.ContainsShort(new AsciiString("short"), short.MaxValue));
            Assert.False(headers.ContainsShort(new AsciiString("short"), short.MinValue));

            headers.AddChar(new AsciiString("char"), char.MaxValue);
            Assert.True(headers.ContainsChar(new AsciiString("char"), char.MaxValue));
            Assert.False(headers.ContainsChar(new AsciiString("char"), char.MinValue));

            headers.AddByte(new AsciiString("byte"), byte.MaxValue);
            Assert.True(headers.ContainsByte(new AsciiString("byte"), byte.MaxValue));
            Assert.False(headers.ContainsLong(new AsciiString("byte"), byte.MinValue));

            headers.AddDouble(new AsciiString("double"), double.MaxValue);
            Assert.True(headers.ContainsDouble(new AsciiString("double"), double.MaxValue));
            Assert.False(headers.ContainsDouble(new AsciiString("double"), double.MinValue));

            headers.AddFloat(new AsciiString("float"), float.MaxValue);
            Assert.True(headers.ContainsFloat(new AsciiString("float"), float.MaxValue));
            Assert.False(headers.ContainsFloat(new AsciiString("float"), float.MinValue));

            TimeSpan timeSpan = DateTime.UtcNow - new DateTime(1970, 1, 1);
            long millis = (long)timeSpan.TotalMilliseconds;
            headers.AddTimeMillis(new AsciiString("millis"), millis);
            Assert.True(headers.ContainsTimeMillis(new AsciiString("millis"), millis));
            // This test doesn't work on midnight, January 1, 1970 UTC
            Assert.False(headers.ContainsTimeMillis(new AsciiString("millis"), 0));

            headers.AddObject(new AsciiString("object"), "Hello World");
            Assert.True(headers.ContainsObject(new AsciiString("object"), "Hello World"));
            Assert.False(headers.ContainsObject(new AsciiString("object"), ""));

            headers.Add(new AsciiString("name"), new AsciiString("value"));
            Assert.True(headers.Contains(new AsciiString("name"), new AsciiString("value")));
            Assert.False(headers.Contains(new AsciiString("name"), new AsciiString("value1")));
        }

        [Fact]
        public void Copy()
        {
            TestDefaultHeaders headers = NewInstance();
            headers.AddBoolean(new AsciiString("boolean"), true);
            headers.AddLong(new AsciiString("long"), long.MaxValue);
            headers.AddInt(new AsciiString("int"), int.MinValue);
            headers.AddShort(new AsciiString("short"), short.MaxValue);
            headers.AddChar(new AsciiString("char"), char.MaxValue);
            headers.AddByte(new AsciiString("byte"), byte.MaxValue);
            headers.AddDouble(new AsciiString("double"), double.MaxValue);
            headers.AddFloat(new AsciiString("float"), float.MaxValue);
            TimeSpan timeSpan = DateTime.UtcNow - new DateTime(1970, 1, 1);
            long millis = (long)timeSpan.TotalMilliseconds;
            headers.AddTimeMillis(new AsciiString("millis"), millis);
            headers.AddObject(new AsciiString("object"), "Hello World");
            headers.Add(new AsciiString("name"), new AsciiString("value"));

            headers = (TestDefaultHeaders)NewInstance().Add(headers);

            Assert.True(headers.ContainsBoolean(new AsciiString("boolean"), true));
            Assert.False(headers.ContainsBoolean(new AsciiString("boolean"), false));

            Assert.True(headers.ContainsLong(new AsciiString("long"), long.MaxValue));
            Assert.False(headers.ContainsLong(new AsciiString("long"), long.MinValue));

            Assert.True(headers.ContainsInt(new AsciiString("int"), int.MinValue));
            Assert.False(headers.ContainsInt(new AsciiString("int"), int.MaxValue));

            Assert.True(headers.ContainsShort(new AsciiString("short"), short.MaxValue));
            Assert.False(headers.ContainsShort(new AsciiString("short"), short.MinValue));

            Assert.True(headers.ContainsChar(new AsciiString("char"), char.MaxValue));
            Assert.False(headers.ContainsChar(new AsciiString("char"), char.MinValue));

            Assert.True(headers.ContainsByte(new AsciiString("byte"), byte.MaxValue));
            Assert.False(headers.ContainsLong(new AsciiString("byte"), byte.MinValue));

            Assert.True(headers.ContainsDouble(new AsciiString("double"), double.MaxValue));
            Assert.False(headers.ContainsDouble(new AsciiString("double"), double.MinValue));

            Assert.True(headers.ContainsFloat(new AsciiString("float"), float.MaxValue));
            Assert.False(headers.ContainsFloat(new AsciiString("float"), float.MinValue));

            Assert.True(headers.ContainsTimeMillis(new AsciiString("millis"), millis));
            // This test doesn't work on midnight, January 1, 1970 UTC
            Assert.False(headers.ContainsTimeMillis(new AsciiString("millis"), 0));

            Assert.True(headers.ContainsObject(new AsciiString("object"), "Hello World"));
            Assert.False(headers.ContainsObject(new AsciiString("object"), ""));

            Assert.True(headers.Contains(new AsciiString("name"), new AsciiString("value")));
            Assert.False(headers.Contains(new AsciiString("name"), new AsciiString("value1")));
        }

        [Fact]
        public void CanMixConvertedAndNormalValues()
        {
            TestDefaultHeaders headers = NewInstance();
            headers.Add(new AsciiString("name"), new AsciiString("value"));
            headers.AddInt(new AsciiString("name"), 100);
            headers.AddBoolean(new AsciiString("name"), false);

            Assert.Equal(3, headers.Size);
            Assert.True(headers.Contains(new AsciiString("name")));
            Assert.True(headers.Contains(new AsciiString("name"), new AsciiString("value")));
            Assert.True(headers.ContainsInt(new AsciiString("name"), 100));
            Assert.True(headers.ContainsBoolean(new AsciiString("name"), false));
        }

        [Fact]
        public void GetAndRemove()
        {
            TestDefaultHeaders headers = NewInstance();
            headers.Add(new AsciiString("name1"), new AsciiString("value1"));
            headers.Add(new AsciiString("name2"), new[] { new AsciiString("value2"), new AsciiString("value3")});
            headers.Add(new AsciiString("name3"), new[] { new AsciiString("value4"), new AsciiString("value5"), new AsciiString("value6")});

            Assert.Equal(new AsciiString("value1"), headers.GetAndRemove(new AsciiString("name1"), new AsciiString("defaultvalue")));
            Assert.Equal(new AsciiString("value2"), headers.GetAndRemove(new AsciiString("name2")));
            Assert.Null(headers.GetAndRemove(new AsciiString("name2")));
            var list = new[] { new AsciiString("value4"), new AsciiString("value5"), new AsciiString("value6") };
            Assert.True(list.SequenceEqual(headers.GetAllAndRemove(new AsciiString("name3"))));
            Assert.Equal(0, headers.Size);
            Assert.Null(headers.GetAndRemove(new AsciiString("noname")));
            Assert.Equal(new AsciiString("defaultvalue"), headers.GetAndRemove(new AsciiString("noname"), new AsciiString("defaultvalue")));
        }

        [Fact]
        public void WhenNameContainsMultipleValuesGetShouldReturnTheFirst()
        {
            TestDefaultHeaders headers = NewInstance();
            headers.Add(new AsciiString("name1"), new[] { new AsciiString("value1"), new AsciiString("value2") });
            Assert.Equal(new AsciiString("value1"), headers.Get(new AsciiString("name1")));
        }

        [Fact]
        public void GetWithDefaultValue()
        {
            TestDefaultHeaders headers = NewInstance();
            headers.Add(new AsciiString("name1"), new AsciiString("value1"));

            Assert.Equal(new AsciiString("value1"), headers.Get(new AsciiString("name1"), new AsciiString("defaultvalue")));
            Assert.Equal(new AsciiString("defaultvalue"), headers.Get(new AsciiString("noname"), new AsciiString("defaultvalue")));
        }

        [Fact]
        public void SetShouldOverWritePreviousValue()
        {
            TestDefaultHeaders headers = NewInstance();
            headers.Set(new AsciiString("name"), new AsciiString("value1"));
            headers.Set(new AsciiString("name"), new AsciiString("value2"));
            Assert.Equal(1, headers.Size);
            Assert.Equal(1, headers.GetAll(new AsciiString("name")).Count);
            Assert.Equal(new AsciiString("value2"), headers.GetAll(new AsciiString("name"))[0]);
            Assert.Equal(new AsciiString("value2"), headers.Get(new AsciiString("name")));
        }

        [Fact]
        public void SetAllShouldOverwriteSomeAndLeaveOthersUntouched()
        {
            TestDefaultHeaders h1 = NewInstance();
            h1.Add(new AsciiString("name1"), new AsciiString("value1"));
            h1.Add(new AsciiString("name2"), new AsciiString("value2"));
            h1.Add(new AsciiString("name2"), new AsciiString("value3"));
            h1.Add(new AsciiString("name3"), new AsciiString("value4"));

            TestDefaultHeaders h2 = NewInstance();
            h2.Add(new AsciiString("name1"), new AsciiString("value5"));
            h2.Add(new AsciiString("name2"), new AsciiString("value6"));
            h2.Add(new AsciiString("name1"), new AsciiString("value7"));

            TestDefaultHeaders expected = NewInstance();
            expected.Add(new AsciiString("name1"), new AsciiString("value5"));
            expected.Add(new AsciiString("name2"), new AsciiString("value6"));
            expected.Add(new AsciiString("name1"), new AsciiString("value7"));
            expected.Add(new AsciiString("name3"), new AsciiString("value4"));

            h1.SetAll(h2);
            Assert.True(expected.Equals(h1));
        }

        [Fact]
        public void HeadersWithSameNamesAndValuesShouldBeEquivalent()
        {
            TestDefaultHeaders headers1 = NewInstance();
            headers1.Add(new AsciiString("name1"), new AsciiString("value1"));
            headers1.Add(new AsciiString("name2"), new AsciiString("value2"));
            headers1.Add(new AsciiString("name2"), new AsciiString("value3"));

            TestDefaultHeaders headers2 = NewInstance();
            headers2.Add(new AsciiString("name1"), new AsciiString("value1"));
            headers2.Add(new AsciiString("name2"), new AsciiString("value2"));
            headers2.Add(new AsciiString("name2"), new AsciiString("value3"));

            Assert.True(headers1.Equals(headers2));
            Assert.True(headers2.Equals(headers1));
            Assert.Equal(headers1.GetHashCode(), headers2.GetHashCode());
            Assert.Equal(headers1.GetHashCode(), headers1.GetHashCode());
            Assert.Equal(headers2.GetHashCode(), headers2.GetHashCode());
        }

        [Fact]
        public void EmptyHeadersShouldBeEqual()
        {
            TestDefaultHeaders headers1 = NewInstance();
            TestDefaultHeaders headers2 = NewInstance();

            Assert.True(headers1.Equals(headers2));
            Assert.True(headers2.Equals(headers1));
            Assert.Equal(headers1.GetHashCode(), headers2.GetHashCode());
        }

        [Fact]
        public void HeadersWithSameNamesButDifferentValuesShouldNotBeEquivalent()
        {
            TestDefaultHeaders headers1 = NewInstance();
            headers1.Add(new AsciiString("name1"), new AsciiString("value1"));
            TestDefaultHeaders headers2 = NewInstance();
            headers2.Add(new AsciiString("name1"), new AsciiString("value2"));
            Assert.False(headers1.Equals(headers2));
        }

        [Fact]
        public void SubsetOfHeadersShouldNotBeEquivalent()
        {
            TestDefaultHeaders headers1 = NewInstance();
            headers1.Add(new AsciiString("name1"), new AsciiString("value1"));
            headers1.Add(new AsciiString("name2"), new AsciiString("value2"));
            TestDefaultHeaders headers2 = NewInstance();
            headers2.Add(new AsciiString("name1"), new AsciiString("value1"));
            Assert.False(headers1.Equals(headers2));
        }

        [Fact]
        public void HeadersWithDifferentNamesAndValuesShouldNotBeEquivalent()
        {
            TestDefaultHeaders h1 = NewInstance();
            h1.Set(new AsciiString("name1"), new AsciiString("value1"));
            TestDefaultHeaders h2 = NewInstance();
            h2.Set(new AsciiString("name2"), new AsciiString("value2"));
            Assert.False(h1.Equals(h2));
            Assert.False(h2.Equals(h1));
        }

        [Fact]
        public void EnumerateShouldReturnAllNameValuePairs()
        {
            TestDefaultHeaders headers1 = NewInstance();
            headers1.Add(new AsciiString("name1"), new[] { new AsciiString("value1"), new AsciiString("value2")});
            headers1.Add(new AsciiString("name2"), new AsciiString("value3"));
            headers1.Add(new AsciiString("name3"), new[] { new AsciiString("value4"), new AsciiString("value5"), new AsciiString("value6")});
            headers1.Add(new AsciiString("name1"), new[] { new AsciiString("value7"), new AsciiString("value8") });
            Assert.Equal(8, headers1.Size);

            TestDefaultHeaders headers2 = NewInstance();
            foreach (HeaderEntry<ICharSequence, ICharSequence> entry in headers1)
            {
                headers2.Add(entry.Key, entry.Value);
            }

            Assert.True(headers1.Equals(headers2));
        }

        [Fact]
        public void EnumerateSetValueShouldChangeHeaderValue()
        {
            TestDefaultHeaders headers = NewInstance();
            headers.Add(new AsciiString("name1"), new[] { new AsciiString("value1"), new AsciiString("value2"), new AsciiString("value3") });
            headers.Add(new AsciiString("name2"), new AsciiString("value4"));
            Assert.Equal(4, headers.Size);

            foreach(HeaderEntry<ICharSequence, ICharSequence> header in headers)
            {
                if (new AsciiString("name1").Equals(header.Key) 
                    && new AsciiString("value2").Equals(header.Value))
                {
                    header.SetValue(new AsciiString("updatedvalue2"));
                    Assert.Equal(new AsciiString("updatedvalue2"), header.Value);
                }
                if (new AsciiString("name1").Equals(header.Key) 
                    && new AsciiString("value3").Equals(header.Value))
                {
                    header.SetValue(new AsciiString("updatedvalue3"));
                    Assert.Equal(new AsciiString("updatedvalue3"), header.Value);
                }
            }

            Assert.Equal(4, headers.Size);
            Assert.True(headers.Contains(new AsciiString("name1"), new AsciiString("updatedvalue2")));
            Assert.False(headers.Contains(new AsciiString("name1"), new AsciiString("value2")));
            Assert.True(headers.Contains(new AsciiString("name1"), new AsciiString("updatedvalue3")));
            Assert.False(headers.Contains(new AsciiString("name1"), new AsciiString("value3")));
        }

        [Fact]
        public void GetAllReturnsEmptyListForUnknownName()
        {
            TestDefaultHeaders headers = NewInstance();
            Assert.Equal(0, headers.GetAll(new AsciiString("noname")).Count);
        }

        [Fact]
        public void SetHeadersShouldClearAndOverwrite()
        {
            TestDefaultHeaders headers1 = NewInstance();
            headers1.Add(new AsciiString("name"), new AsciiString("value"));

            TestDefaultHeaders headers2 = NewInstance();
            headers2.Add(new AsciiString("name"), new AsciiString("newvalue"));
            headers2.Add(new AsciiString("name1"), new AsciiString("value1"));

            headers1.Set(headers2);
            Assert.True(headers1.Equals(headers2));
        }

        [Fact]
        public void SetAllHeadersShouldOnlyOverwriteHeaders()
        {
            TestDefaultHeaders headers1 = NewInstance();
            headers1.Add(new AsciiString("name"), new AsciiString("value"));
            headers1.Add(new AsciiString("name1"), new AsciiString("value1"));

            TestDefaultHeaders headers2 = NewInstance();
            headers2.Add(new AsciiString("name"), new AsciiString("newvalue"));
            headers2.Add(new AsciiString("name2"), new AsciiString("value2"));

            TestDefaultHeaders expected = NewInstance();
            expected.Add(new AsciiString("name"), new AsciiString("newvalue"));
            expected.Add(new AsciiString("name1"), new AsciiString("value1"));
            expected.Add(new AsciiString("name2"), new AsciiString("value2"));

            headers1.SetAll(headers2);
            Assert.True(headers1.Equals(expected));
        }

        [Fact]
        public void AddSelf()
        {
            TestDefaultHeaders headers = NewInstance();
            Assert.Throws<ArgumentException>(() => headers.Add(headers));
        }

        [Fact]
        public void SetSelfIsNoOp()
        {
            TestDefaultHeaders headers = NewInstance();
            headers.Add(new AsciiString("name"), new AsciiString("value"));
            headers.Set(headers);
            Assert.Equal(1, headers.Size);
        }

        [Fact]
        public void ConvertToString()
        {
            TestDefaultHeaders headers = NewInstance();
            headers.Add(new AsciiString("name1"), new AsciiString("value1"));
            headers.Add(new AsciiString("name1"), new AsciiString("value2"));
            headers.Add(new AsciiString("name2"), new AsciiString("value3"));
            Assert.Equal("TestDefaultHeaders[name1: value1, name1: value2, name2: value3]", headers.ToString());

            headers = NewInstance();
            headers.Add(new AsciiString("name1"), new AsciiString("value1"));
            headers.Add(new AsciiString("name2"), new AsciiString("value2"));
            headers.Add(new AsciiString("name3"), new AsciiString("value3"));
            Assert.Equal("TestDefaultHeaders[name1: value1, name2: value2, name3: value3]", headers.ToString());

            headers = NewInstance();
            headers.Add(new AsciiString("name1"), new AsciiString("value1"));
            Assert.Equal("TestDefaultHeaders[name1: value1]", headers.ToString());

            headers = NewInstance();
            Assert.Equal("TestDefaultHeaders[]", headers.ToString());
        }
    }
}
