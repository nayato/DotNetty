// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs
{
    using System;
    using System.Globalization;
    using DotNetty.Common.Utilities;

    public class CharSequenceValueConverter : IValueConverter<ICharSequence>
    {
        public static readonly CharSequenceValueConverter Instance = new CharSequenceValueConverter();

        public virtual ICharSequence ConvertObject(object value)
        {
            if (value is ICharSequence)
            {
                return (ICharSequence)value;
            }

            return new StringCharSequence(value.ToString());
        }

        public ICharSequence ConvertBoolean(bool value) => new StringCharSequence(value.ToString());

        public bool ConvertToBoolean(ICharSequence value)
        {
            if (value is AsciiString)
            {
                return ((AsciiString)value).ParseBoolean();
            }

            return bool.Parse(value.ToString());
        }

        public ICharSequence ConvertByte(byte value) => new StringCharSequence(value.ToString());

        public byte ConvertToByte(ICharSequence value)
        {
            if (value is AsciiString)
            {
                return ((AsciiString)value).ByteAt(0);
            }

            return byte.Parse(value.ToString());
        }

        public ICharSequence ConvertChar(char value) => new StringCharSequence(value.ToString());

        public char ConvertToChar(ICharSequence value) => value[0];

        public ICharSequence ConvertShort(short value) => new StringCharSequence(value.ToString());

        public short ConvertToShort(ICharSequence value)
        {
            if (value is AsciiString)
            {
                return ((AsciiString)value).ParseShort();
            }

            return short.Parse(value.ToString());
        }

        public ICharSequence ConvertInt(int value) => new StringCharSequence(value.ToString());

        public int ConvertToInt(ICharSequence value)
        {
            if (value is AsciiString)
            {
                return ((AsciiString)value).ParseInt();
            }

            return int.Parse(value.ToString());
        }

        public ICharSequence ConvertLong(long value) => new StringCharSequence(value.ToString());

        public long ConvertToLong(ICharSequence value)
        {
            if (value is AsciiString)
            {
                return ((AsciiString)value).ParseLong();
            }

            return long.Parse(value.ToString());
        }

        public ICharSequence ConvertTimeMillis(long value) => new StringCharSequence(value.ToString());

        public long ConvertToTimeMillis(ICharSequence value)
        {
            DateTime? dateTime = DateFormatter.ParseHttpDate(value);
            if (dateTime == null)
            {
                throw new FormatException($"header can't be parsed into a Date: {value}");
            }

            return dateTime.Value.Ticks / TimeSpan.TicksPerMillisecond;
        }

        public ICharSequence ConvertFloat(float value) => 
            new StringCharSequence(value.ToString(CultureInfo.InvariantCulture));

        public float ConvertToFloat(ICharSequence value)
        {
            if (value is AsciiString)
            {
                return ((AsciiString)value).ParseFloat();
            }

            return float.Parse(value.ToString());
        }

        public ICharSequence ConvertDouble(double value) =>
            new StringCharSequence(value.ToString(CultureInfo.InvariantCulture));

        public double ConvertToDouble(ICharSequence value)
        {
            if (value is AsciiString)
            {
                return ((AsciiString)value).ParseDouble();
            }

            return double.Parse(value.ToString());
        }
    }
}
