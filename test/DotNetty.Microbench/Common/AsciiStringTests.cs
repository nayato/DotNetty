// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Microbench.Common
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using DotNetty.Common.Internal;
    using DotNetty.Common.Utilities;
    using DotNetty.Microbench.Utilities;
    using Xunit;
    using Xunit.Abstractions;

    public sealed class AsciiStringTests
    {
        static readonly int[] Sizes = { 3, 5, 7, 8, 10, 20, 50, 100, 1000 };

        readonly ITestOutputHelper output;

        public AsciiStringTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void AsciiStringHash()
        {
            var list = new List<AsciiString>();
            var random = new Random();
            foreach (int size in Sizes)
            {
                var bytes = new byte[size];
                random.NextBytes(bytes);
                var asciiString = new AsciiString(bytes, false);
                list.Add(asciiString);
            }

            CodeTimer.Time(true, this.output, "Compute ascii hash for AsciiString", 1000, () =>
                {
                    foreach (AsciiString asciiString in list)
                    {
                        PlatformDependent.HashCodeAscii(asciiString.Array, asciiString.Offset, asciiString.Count);
                    }
                });
        }

        [Fact]
        public void StringHash()
        {
            var list = new List<StringCharSequence>();
            var random = new Random();
            foreach (int size in Sizes)
            {
                var bytes = new byte[size];
                random.NextBytes(bytes);
                list.Add(new StringCharSequence(Encoding.ASCII.GetString(bytes)));
            }

            CodeTimer.Time(true, this.output, "Compute ascii hash for string", 1000, () =>
            {
                foreach (StringCharSequence charSequence in list)
                {
                    PlatformDependent.HashCodeAscii(charSequence);
                }
            });
        }
    }
}
