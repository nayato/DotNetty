// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Microbench.Handler.Codec
{
    using System;
    using DotNetty.Codecs;
    using DotNetty.Microbench.Utilities;
    using Xunit;
    using Xunit.Abstractions;

    public sealed class DateFormatterTests
    {
        const string DateString = "Sun, 27 Nov 2016 19:18:46 GMT";
        static readonly DateTime Date = new DateTime(784111777000L);

        readonly ITestOutputHelper output;

        public DateFormatterTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void ParseHttpDate() => CodeTimer.Time(true, this.output, "Parse http date", 1000, 
            () => DateFormatter.ParseHttpDate(DateString));

        [Fact]
        public void FormatDateTime() => CodeTimer.Time(true, this.output, "Format to http date string", 1000,
            () => DateFormatter.Format(Date));
    }
}
