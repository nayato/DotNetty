// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Common.Utilities
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Runtime.CompilerServices;
    using System.Text;
    using DotNetty.Common.Internal;

    public sealed class AsciiString : ICharSequence, IEquatable<AsciiString>, IComparable<AsciiString>, IComparable
    {
        public static readonly AsciiString Empty = new AsciiString(string.Empty);

        public static readonly IHashingStrategy<ICharSequence> CaseInsensitiveHasher = new CaseInsensitiveHashingStrategy();
        public static readonly IHashingStrategy<ICharSequence> CaseSensitiveHasher = new CaseSensitiveHashingStrategy();

        static readonly ICharEqualityComparator DefaultCharComparator = new DefaultCharEqualityComparator();
        static readonly ICharEqualityComparator GeneralCaseInsensitiveComparator = new GeneralCaseInsensitiveCharEqualityComparator();
        static readonly ICharEqualityComparator AsciiCaseInsensitiveCharComparator = new AsciiCaseInsensitiveCharEqualityComparator();

        sealed class CaseInsensitiveHashingStrategy : IHashingStrategy<ICharSequence>
        {
            int IEqualityComparer<ICharSequence>.GetHashCode(ICharSequence obj) => AsciiString.GetHashCode(obj);

            public bool Equals(ICharSequence a, ICharSequence b) => ContentEqualsIgnoreCase(a, b);
        }

        sealed class CaseSensitiveHashingStrategy : IHashingStrategy<ICharSequence>
        {
            int IEqualityComparer<ICharSequence>.GetHashCode(ICharSequence obj) => AsciiString.GetHashCode(obj);

            public bool Equals(ICharSequence a, ICharSequence b) => ContentEquals(a, b);
        }

        const int MaxCharValue = 255;
        public static readonly int IndexNotFound = -1;

        readonly byte[] value;
        readonly int offset;
        int hash;

        //Used to cache the ToString() value.
        string stringValue;

        public AsciiString(byte[] value) : this(value, true)
        {
        }

        public AsciiString(byte[] value, bool copy) : this(value, 0, value.Length, copy)
        {
        }

        public AsciiString(byte[] value, int start, int length, bool copy)
        {
            if (copy)
            {
                this.value = new byte[length];
                Buffer.BlockCopy(value, start, this.value, 0, length);
                this.offset = 0;
            }
            else
            {
                if (MathUtil.IsOutOfBounds(start, length, value.Length))
                {
                    throw new IndexOutOfRangeException(
                        $"expected: 0 <= start({start}) <= start + length({length}) <= value.length({value.Length})");
                }

                this.value = value;
                this.offset = start;
            }

            this.Count = length;
        }

        public AsciiString(IReadOnlyList<char> value) : this(value, 0, value.Count)
        {
        }

        public AsciiString(IReadOnlyList<char> value, int start, int length)
        {
            if (MathUtil.IsOutOfBounds(start, length, value.Count))
            {
                throw new IndexOutOfRangeException(
                    $"expected: 0 <= start({start}) <= start + length({length}) <= value.length({value.Count})");
            }

            this.value = new byte[length];
            for (int i = 0, j = start; i < length; i++, j++)
            {
                this.value[i] = CharToByte(value[j]);
            }

            this.offset = 0;
            this.Count = length;
        }

        public AsciiString(char[] value, Encoding encoding) : this(value, encoding, 0, value.Length)
        {
        }

        public AsciiString(char[] value, Encoding encoding, int start, int length)
        {
            this.value = encoding.GetBytes(value, start, length);
            this.offset = 0;
            this.Count = this.value.Length;
        }

        public AsciiString(ICharSequence value) : this(value, 0, value.Count)
        {
        }

        public AsciiString(ICharSequence value, int start, int length)
        {
            if (MathUtil.IsOutOfBounds(start, length, value.Count))
            {
                throw new IndexOutOfRangeException(
                    $"expected: 0 <= start({start}) <= start + length({length}) <= value.length({value.Count})");
            }

            this.value = new byte[length];
            for (int i = 0, j = start; i < length; i++, j++)
            {
                this.value[i] = CharToByte(value[j]);
            }

            this.offset = 0;
            this.Count = length;
        }

        public AsciiString(string value, Encoding encoding) : this(value, encoding, 0, value.Length)
        {
        }

        public AsciiString(string value, Encoding encoding, int start, int length)
        {
            int count = encoding.GetMaxByteCount(length);
            var bytes = new byte[count];
            count = encoding.GetBytes(value, start, length, bytes, 0);

            this.value = new byte[count];
            Buffer.BlockCopy(bytes, 0, this.value, 0, count);

            this.offset = 0;
            this.Count = this.value.Length;
        }

        public AsciiString(string value)
        {
            this.stringValue = value;

            this.value = new byte[value.Length];
            for (int i = 0; i < value.Length; i++)
            {
                this.value[i] = CharToByte(value[i]);
            }

            this.offset = 0;
            this.Count = value.Length;
        }

        public IEnumerator<char> GetEnumerator() => new CharSequenceEnumerator(this);

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        public byte[] Array => this.value;

        public int Offset => this.offset;

        public int Count { get; }

        /// <summary>
        /// During normal use cases the AsciiString should be immutable, but if the
        /// underlying array is shared, and changes then this needs to be called.
        /// </summary>
        public void ArrayChanged()
        {
            this.stringValue = null;
            this.hash = 0;
        }

        public char this[int index] => ByteToChar(this.ByteAt(index));

        public byte ByteAt(int index)
        {
            // We must do a range check here to enforce the access does not go outside our sub region of the array.
            // We rely on the array access itself to pick up the array out of bounds conditions
            if (index < 0 || index >= this.Count)
            {
                throw new IndexOutOfRangeException($"index: {index} must be in the range [0,{this.Count})");
            }

            return this.value[index + this.offset];
        }

        public static bool ContentEqualsIgnoreCase(ICharSequence a, ICharSequence b)
        {
            if (a == null || b == null)
            {
                return ReferenceEquals(a, b);
            }

            if (a is AsciiString)
            {
                return ((AsciiString)a).ContentEqualsIgnoreCase(b);
            }
            if (b is AsciiString)
            {
                return ((AsciiString)b).ContentEqualsIgnoreCase(a);
            }

            if (a.Count != b.Count)
            {
                return false;
            }

            for (int i = 0, j = 0; i < a.Count; ++i, ++j)
            {
                if (!EqualsIgnoreCase(a[i], b[j]))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool ContentEquals(ICharSequence a, ICharSequence b)
        {
            if (a == null || b == null)
            {
                return ReferenceEquals(a, b);
            }

            if (a is AsciiString)
            {
                return ((AsciiString)a).ContentEquals(b);
            }
            if (b is AsciiString)
            {
                return ((AsciiString)b).ContentEquals(a);
            }

            if (a.Count != b.Count)
            {
                return false;
            }

            for (int i = 0; i < a.Count; ++i)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }

            return true;
        }

        public bool ContentEqualsIgnoreCase(ICharSequence other)
        {
            if (other == null || other.Count != this.Count)
            {
                return false;
            }

            var rhs = other as AsciiString;
            if (rhs != null)
            {
                for (int i = this.offset, j = rhs.offset; i < this.Count; ++i, ++j)
                {
                    if (!EqualsIgnoreCase(this.value[i], rhs.value[j]))
                    {
                        return false;
                    }
                }
                return true;
            }

            for (int i = this.offset, j = 0; i < this.Count; ++i, ++j)
            {
                if (!EqualsIgnoreCase(ByteToChar(this.value[i]), other[j]))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool Contains(ICharSequence a, ICharSequence b) => Contains(a, b, DefaultCharComparator);

        public static bool ContainsIgnoreCase(ICharSequence a, ICharSequence b) => Contains(a, b, AsciiCaseInsensitiveCharComparator);

        static bool Contains(ICharSequence a, ICharSequence b, ICharEqualityComparator comparator)
        {
            if (a == null || b == null || a.Count < b.Count)
            {
                return false;
            }
            if (b.Count == 0)
            {
                return true;
            }

            int bStart = 0;
            for (int i = 0; i < a.Count; ++i)
            {
                if (comparator.CharEquals(b[bStart], a[i]))
                {
                    // If b is consumed then true.
                    if (++bStart == b.Count)
                    {
                        return true;
                    }
                }
                else if (a.Count - i < b.Count)
                {
                    // If there are not enough characters left in a for b to be contained, then false.
                    return false;
                }
                else
                {
                    bStart = 0;
                }
            }

            return false;
        }

        public static bool ContainsContentEqualsIgnoreCase(ICollection<ICharSequence> collection, ICharSequence value)
        {
            foreach (ICharSequence v in collection)
            {
                if (ContentEqualsIgnoreCase(value, v))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool ContainsAllContentEqualsIgnoreCase(ICollection<ICharSequence> a, ICollection<ICharSequence> b)
        {
            foreach (ICharSequence v in b)
            {
                if (!ContainsContentEqualsIgnoreCase(a, v))
                {
                    return false;
                }
            }

            return true;
        }

        static bool RegionMatchesCharSequences(ICharSequence cs, int csStart, 
            ICharSequence seq, int start, int length, ICharEqualityComparator charEqualityComparator)
        {
            if (csStart < 0 || length > cs.Count - csStart)
            {
                return false;
            }
            if (start < 0 || length > seq.Count - start)
            {
                return false;
            }

            int csIndex = csStart;
            int csEnd = csIndex + length;
            int stringIndex = start;

            while (csIndex < csEnd)
            {
                char c1 = cs[csIndex++];
                char c2 = seq[stringIndex++];

                if (!charEqualityComparator.CharEquals(c1, c2))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool RegionMatches(ICharSequence cs, bool ignoreCase, int csStart, ICharSequence seq, int start, int length)
        {
            if (cs == null || seq == null)
            {
                return false;
            }
            if (cs is StringCharSequence && seq is StringCharSequence) {
                return ((StringCharSequence)cs).RegionMatches(ignoreCase, csStart, seq, start, length);
            }
            if (cs is AsciiString) {
                return ((AsciiString)cs).RegionMatches(ignoreCase, csStart, seq, start, length);
            }

            return RegionMatchesCharSequences(cs, csStart, seq, start, length,
                ignoreCase ? GeneralCaseInsensitiveComparator : DefaultCharComparator);
        }

        public static bool RegionMatchesAscii(ICharSequence cs, bool ignoreCase, int csStart, ICharSequence seq, int start, int length)
        {
            if (cs == null || seq == null)
            {
                return false;
            }

            if (!ignoreCase && cs is StringCharSequence && seq is StringCharSequence) {
                //we don't call regionMatches from String for ignoreCase==true. It's a general purpose method,
                //which make complex comparison in case of ignoreCase==true, which is useless for ASCII-only strings.
                //To avoid applying this complex ignore-case comparison, we will use regionMatchesCharSequences
                return ((StringCharSequence)cs).RegionMatches(false, csStart, seq, start, length);
            }

            var asciiString = cs as AsciiString;
            if (asciiString != null) {
                return asciiString.RegionMatches(ignoreCase, csStart, seq, start, length);
            }

            return RegionMatchesCharSequences(cs, csStart, seq, start, length,
                ignoreCase ? AsciiCaseInsensitiveCharComparator : DefaultCharComparator);
        }

        public bool RegionMatches(int thisStart, ICharSequence seq, int start, int length)
        {
            Contract.Requires(seq != null);

            if (start < 0 || seq.Count - start < length)
            {
                return false;
            }

            int thisLen = this.Count;
            if (thisStart < 0 || thisLen - thisStart < length)
            {
                return false;
            }

            if (length <= 0)
            {
                return true;
            }

            int thatEnd = start + length;
            for (int i = start, j = thisStart + this.offset; i < thatEnd; i++, j++)
            {
                if (ByteToChar(this.value[j]) != seq[i])
                {
                    return false;
                }
            }

            return true;
        }

        public bool RegionMatches(bool ignoreCase, int thisStart, ICharSequence seq, int start, int length)
        {
            Contract.Requires(seq != null);

            if (!ignoreCase)
            {
                return this.RegionMatches(thisStart, seq, start, length);
            }

            int thisLen = this.Count;
            if (thisStart < 0 || length > thisLen - thisStart)
            {
                return false;
            }
            if (start < 0 || length > seq.Count - start)
            {
                return false;
            }

            thisStart += this.offset;
            int thisEnd = thisStart + length;
            while (thisStart < thisEnd)
            {
                if (!EqualsIgnoreCase(ByteToChar(this.value[thisStart++]), seq[start++]))
                {
                    return false;
                }
            }

            return true;
        }

        public static int IndexOfIgnoreCase(ICharSequence str, ICharSequence searchStr, int startPos)
        {
            if (str == null || searchStr == null)
            {
                return IndexNotFound;
            }

            if (startPos < 0)
            {
                startPos = 0;
            }
            int searchStrLen = searchStr.Count;
            int endLimit = str.Count - searchStrLen + 1;
            if (startPos > endLimit)
            {
                return IndexNotFound;
            }
            if (searchStrLen == 0)
            {
                return startPos;
            }
            for (int i = startPos; i < endLimit; i++)
            {
                if (RegionMatches(str, true, i, searchStr, 0, searchStrLen))
                {
                    return i;
                }
            }

            return IndexNotFound;
        }

        public ICharSequence SubSequence(int start) => (AsciiString)this.SubSequence(start, this.Count);

        public ICharSequence SubSequence(int start, int end) => this.SubSequence(start, end, false);

        public AsciiString SubSequence(int start, int end, bool copy)
        {
            if (MathUtil.IsOutOfBounds(start, end - start, this.Count))
            {
                throw new IndexOutOfRangeException(
                    $"expected: 0 <= start({start}) <= end ({end}) <= length({this.Count})");
            }

            if (start == 0 && end == this.Count)
            {
                return this;
            }

            return end == start ? Empty : new AsciiString(this.value, start + this.offset, end - start, copy);
        }

        public AsciiString ToLowerCase()
        {
            bool lowercased = true;
            int i, j;
            int len = this.Count + this.offset;
            for (i = this.offset; i < len; ++i)
            {
                byte b = this.value[i];
                if (b >= 'A' && b <= 'Z')
                {
                    lowercased = false;
                    break;
                }
            }

            // Check if this string does not contain any uppercase characters.
            if (lowercased)
            {
                return this;
            }

            var newValue = new byte[this.Count];
            for (i = 0, j = this.offset; i < newValue.Length; ++i, ++j)
            {
                newValue[i] = ToLowerCase(this.value[j]);
            }

            return new AsciiString(newValue, false);
        }

        public AsciiString ToUpperCase()
        {
            bool uppercased = true;
            int i, j;
            int len = this.Count + this.offset;
            for (i = this.offset; i < len; ++i)
            {
                byte b = this.value[i];
                if (b >= 'a' && b <= 'z')
                {
                    uppercased = false;
                    break;
                }
            }

            // Check if this string does not contain any lowercase characters.
            if (uppercased)
            {
                return this;
            }

            var newValue = new byte[this.Count];
            for (i = 0, j = this.offset; i < newValue.Length; ++i, ++j)
            {
                newValue[i] = ToUpperCase(this.value[j]);
            }

            return new AsciiString(newValue, false);
        }

        public AsciiString Trim()
        {
            int start = this.offset;
            int last = this.offset + this.Count - 1;
            int end = last;
            while (start <= end && this.value[start] <= ' ')
            {
                start++;
            }
            while (end >= start && this.value[end] <= ' ')
            {
                end--;
            }
            if (start == 0 && end == last)
            {
                return this;
            }

            return new AsciiString(this.value, start, end - start + 1, false);
        }

        public bool ContentEquals(ICharSequence a)
        {
            if (a == null || a.Count != this.Count)
            {
                return false;
            }

            var asciiString = a as AsciiString;
            if (asciiString != null)
            {
                return this.Equals(asciiString);
            }

            for (int i = this.offset, j = 0; j < a.Count; ++i, ++j)
            {
                if (ByteToChar(this.value[i]) != a[j])
                {
                    return false;
                }
            }

            return true;
        }

        public int ForEachByte(ByteProcessor visitor) => this.ForEachByte0(0, this.Count, visitor);

        public int ForEachByte(int index, int length, ByteProcessor visitor)
        {
            if (MathUtil.IsOutOfBounds(index, length, this.Count))
            {
                throw new IndexOutOfRangeException(
                    $"expected: 0 <= index({index} <= start + length({length}) <= length({this.Count})");
            }

            return this.ForEachByte0(index, length, visitor);
        }

        int ForEachByte0(int index, int length, ByteProcessor visitor)
        {
            int len = this.offset + index + length;
            for (int i = this.offset + index; i < len; ++i)
            {
                if (!visitor.Process(this.value[i]))
                {
                    return i - this.offset;
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool EqualsIgnoreCase(byte a, byte b) => a == b || ToLowerCase(a) == ToLowerCase(b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool EqualsIgnoreCase(char a, char b) => a == b || ToLowerCase(a) == ToLowerCase(b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static byte ToLowerCase(byte b) => IsUpperCase(b) ? (byte)(b + 32) : b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static char ToLowerCase(char c) => IsUpperCase(c) ? (char)(c + 32) : c;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static byte ToUpperCase(byte b) => IsLowerCase(b) ? (byte)(b - 32) : b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool IsLowerCase(byte value) => value >= 'a' && value <= 'z';

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsUpperCase(byte value) => value >= 'A' && value <= 'Z';

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsUpperCase(char value) => value >= 'A' && value <= 'Z';

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte CharToByte(char c) => (byte)(c > MaxCharValue ? '?' : c);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static char ByteToChar(byte b) => (char)(b & 0xFF);

        public override int GetHashCode()
        {
            // ReSharper disable NonReadonlyMemberInGetHashCode
            if (this.hash == 0)
            {
                this.hash = PlatformDependent.HashCodeAscii(this.value, this.offset, this.Count);
            }

            return this.hash;
            // ReSharper restore NonReadonlyMemberInGetHashCode
        }

        public bool Equals(AsciiString other)
        {
            if (other == null)
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return this.Count == other.Count 
                && this.GetHashCode() == other.GetHashCode()
                && PlatformDependent.ByteArrayEquals(this.value, this.offset, other.value, other.offset, this.Count);
        }

        public override bool Equals(object obj) =>
            !ReferenceEquals(this, null) 
            && obj is AsciiString && this.Equals((AsciiString)obj);

        public static explicit operator string(AsciiString value) =>
            value != null ? value.ToString() : string.Empty;

        public static explicit operator AsciiString(string value) =>
            value != null ? new AsciiString(value) : Empty;

        public override string ToString()
        {
            if (this.stringValue != null)
            {
                return this.stringValue;
            }

            this.stringValue = this.ToString(0);
            return this.stringValue;
        }

        public string ToString(int start) => this.ToString(start, this.Count);

        public string ToString(int start, int end)
        {
            int length = end - start;
            if (length == 0)
            {
                return string.Empty;
            }

            if (MathUtil.IsOutOfBounds(start, length, this.Count))
            {
                throw new IndexOutOfRangeException(
                    $"expected: 0 <= start({start}) <= srcIdx + length({length}) <= srcLen({this.Count})");
            }

            var chars = new char[length];
            for (int i = 0; i < length; i++)
            {
                chars[i] = ByteToChar(this.value[this.offset + start + i]);
            }

            return new string(chars);
        }

        public bool ParseBoolean() => this.Count >= 1 && this.value[this.offset] != 0;

        public char ParseChar() => this.ParseChar(0);

        public char ParseChar(int start)
        {
            if (start + 1 >= this.Count)
            {
                throw new IndexOutOfRangeException(
                    $"2 bytes required to convert to character. index {start} would go out of bounds.");
            }

            int startWithOffset = start + this.offset;

            return (char)((ByteToChar(this.value[startWithOffset]) << 8) 
                | ByteToChar(this.value[startWithOffset + 1]));
        }

        public short ParseShort() => this.ParseShort(0, this.Count, 10);

        public short ParseShort(int radix) => this.ParseShort(0, this.Count, radix);

        public short ParseShort(int start, int end) => this.ParseShort(start, end, 10);

        public short ParseShort(int start, int end, int radix)
        {
            int intValue = this.ParseInt(start, end, radix);
            short result = (short)intValue;
            if (result != intValue)
            {
                throw new FormatException(this.SubSequence(start, end).ToString());
            }

            return result;
        }

        public long ParseLong() => this.ParseLong(0, this.Count, 10);

        public long ParseLong(int radix) => this.ParseLong(0, this.Count, radix);

        public long ParseLong(int start, int end) => this.ParseLong(start, end, 10);

        public long ParseLong(int start, int end, int radix)
        {
            if (radix < CharUtil.MinRadix || radix > CharUtil.MaxRadix)
            {
                throw new FormatException($"Radix must be from {CharUtil.MinRadix} to {CharUtil.MaxRadix}");
            }

            if (start == end)
            {
                throw new FormatException($"Content is empty because {start} and {end} are the same.");
            }

            int i = start;
            bool negative = this.ByteAt(i) == '-';
            if (negative && ++i == end)
            {
                throw new FormatException(this.SubSequence(start, end).ToString());
            }

            return this.ParseLong(i, end, radix, negative);
        }

        long ParseLong(int start, int end, int radix, bool negative)
        {
            long max = long.MinValue / radix;
            long result = 0;
            int currOffset = start;
            while (currOffset < end)
            {
                int digit = CharUtil.Digit((char)(this.value[currOffset++ + this.offset] & 0xFF), radix);
                if (digit == -1)
                {
                    throw new FormatException(this.SubSequence(start, end).ToString());
                }
                if (max > result)
                {
                    throw new FormatException(this.SubSequence(start, end).ToString());
                }
                long next = result * radix - digit;
                if (next > result)
                {
                    throw new FormatException(this.SubSequence(start, end).ToString());
                }
                result = next;
            }

            if (!negative)
            {
                result = -result;
                if (result < 0)
                {
                    throw new FormatException(this.SubSequence(start, end).ToString());
                }
            }

            return result;
        }

        public int ParseInt() => this.ParseInt(0, this.Count, 10);

        public int ParseInt(int radix) => this.ParseInt(0, this.Count, radix);

        public int ParseInt(int start, int end) => this.ParseInt(start, end, 10);

        public int ParseInt(int start, int end, int radix)
        {
            if (radix < CharUtil.MinRadix || radix > CharUtil.MaxRadix)
            {
                throw new FormatException($"Radix must be from {CharUtil.MinRadix} to {CharUtil.MaxRadix}");
            }

            if (start == end)
            {
                throw new FormatException();
            }

            int i = start;
            bool negative = this.ByteAt(i) == '-';
            if (negative && ++i == end)
            {
                throw new FormatException(this.SubSequence(start, end).ToString());
            }

            return this.ParseInt(i, end, radix, negative);
        }

        int ParseInt(int start, int end, int radix, bool negative)
        {
            int max = int.MinValue / radix;
            int result = 0;
            int currOffset = start;
            while (currOffset < end)
            {
                int digit = CharUtil.Digit((char)(this.value[currOffset++ + this.offset] & 0xFF), radix);
                if (digit == -1)
                {
                    throw new FormatException(this.SubSequence(start, end).ToString());
                }
                if (max > result)
                {
                    throw new FormatException(this.SubSequence(start, end).ToString());
                }
                int next = result * radix - digit;
                if (next > result)
                {
                    throw new FormatException(this.SubSequence(start, end).ToString());
                }
                result = next;
            }

            if (!negative)
            {
                result = -result;
                if (result < 0)
                {
                    throw new FormatException(this.SubSequence(start, end).ToString());
                }
            }

            return result;
        }

        public float ParseFloat() => this.ParseFloat(0, this.Count);

        public float ParseFloat(int start, int end) => Convert.ToSingle(this.ToString(start, end));

        public double ParseDouble() => this.ParseDouble(0, this.Count);

        public double ParseDouble(int start, int end) => Convert.ToDouble(this.ToString(start, end));

        public int HashCode(bool ignoreCase) => 
            !ignoreCase ? this.GetHashCode() : CaseInsensitiveHasher.GetHashCode(this);

        public static int GetHashCode(ICharSequence value)
        {
            if (value == null)
            {
                return 0;
            }

            var asciiString = value as AsciiString;
            return asciiString != null 
                ? asciiString.GetHashCode() 
                : PlatformDependent.HashCodeAscii(value);
        }

        ///
        /// Compares the specified string to this string using the ASCII values of the characters. Returns 0 if the strings
        /// contain the same characters in the same order. Returns a negative integer if the first non-equal character in
        /// this string has an ASCII value which is less than the ASCII value of the character at the same position in the
        /// specified string, or if this string is a prefix of the specified string. Returns a positive integer if the first
        /// non-equal character in this string has a ASCII value which is greater than the ASCII value of the character at
        /// the same position in the specified string, or if the specified string is a prefix of this string.
        /// 
        public int CompareTo(AsciiString other)
        {
            if (ReferenceEquals(this, other))
            {
                return 0;
            }

            int length1 = this.Count;
            int length2 = other.Count;
            int minLength = Math.Min(length1, length2);
            for (int i = 0, j = this.offset; i < minLength; i++, j++)
            {
                int result = ByteToChar(this.value[j]) - other[i];
                if (result != 0)
                {
                    return result;
                }
            }

            return length1 - length2;
        }

        public int CompareTo(ICharSequence other)
        {
            if (ReferenceEquals(this, other))
            {
                return 0;
            }

            int length1 = this.Count;
            int length2 = other.Count;
            int minLength = Math.Min(length1, length2);
            for (int i = 0, j = this.offset; i < minLength; i++, j++)
            {
                int result = ByteToChar(this.value[j]) - other[i];
                if (result != 0)
                {
                    return result;
                }
            }

            return length1 - length2;
        }

        public int CompareTo(object obj) => this.CompareTo(obj as AsciiString);

        public bool Contains(ICharSequence sequence) => this.IndexOf(sequence) >= 0;

        public int IndexOf(ICharSequence sequence) => this.IndexOf(sequence, 0);

        public int IndexOf(ICharSequence subString, int start)
        {
            if (start < 0)
            {
                start = 0;
            }

            int thisLen = this.Count;

            int subCount = subString.Count;
            if (subCount <= 0)
            {
                return start < thisLen ? start : thisLen;
            }
            if (subCount > thisLen - start)
            {
                return -1;
            }

            char firstChar = subString[0];
            if (firstChar > MaxCharValue)
            {
                return -1;
            }

            var indexOfVisitor = new ByteProcessor.IndexOfProcessor((byte)firstChar);
            for (;;)
            {
                int i = this.ForEachByte(start, thisLen - start, indexOfVisitor);
                if (i == -1 || subCount + i > thisLen)
                {
                    return -1; // handles subCount > count || start >= count
                }
                int o1 = i, o2 = 0;
                while (++o2 < subCount && ByteToChar(this.value[++o1 + this.offset]) == subString[o2])
                {
                    // Intentionally empty
                }
                if (o2 == subCount)
                {
                    return i;
                }
                start = i + 1;
            }
        }

        public static int IndexOfIgnoreCaseAscii(ICharSequence str, ICharSequence searchStr, int startPos)
        {
            if (str == null || searchStr == null)
            {
                return IndexNotFound;
            }

            if (startPos < 0)
            {
                startPos = 0;
            }
            int searchStrLen = searchStr.Count;
            int endLimit = str.Count - searchStrLen + 1;
            if (startPos > endLimit)
            {
                return IndexNotFound;
            }
            if (searchStrLen == 0)
            {
                return startPos;
            }
            for (int i = startPos; i < endLimit; i++)
            {
                if (RegionMatchesAscii(str, true, i, searchStr, 0, searchStrLen))
                {
                    return i;
                }
            }

            return IndexNotFound;
        }

        public int IndexOf(char ch, int start = 0)
        {
            if (start < 0)
            {
                start = 0;
            }

            int thisLen = this.Count;

            if (ch > MaxCharValue)
            {
                return -1;
            }

            return this.ForEachByte(start, thisLen - start, new ByteProcessor.IndexOfProcessor((byte)ch));
        }

        internal byte[] ToByteArray()
        {
            var bytes = new byte[this.Count];
            Buffer.BlockCopy(this.value, this.offset, bytes, 0, this.Count);

            return bytes;
        }

        public bool SequenceEquals(ICharSequence other, bool ignoreCase) => 
            other != null 
            && ignoreCase ? this.ContentEqualsIgnoreCase(other) : this.ContentEquals(other);

        interface ICharEqualityComparator
        {
            bool CharEquals(char a, char b);
        }

        sealed class DefaultCharEqualityComparator : ICharEqualityComparator
        {
            public bool CharEquals(char a, char b) => a == b;
        }

        sealed class GeneralCaseInsensitiveCharEqualityComparator : ICharEqualityComparator
        {
            public bool CharEquals(char a, char b) => 
                char.ToUpper(a) == char.ToUpper(b) || char.ToLower(a) == char.ToLower(b);
        }

        sealed class AsciiCaseInsensitiveCharEqualityComparator : ICharEqualityComparator
        {
            public bool CharEquals(char a, char b) => EqualsIgnoreCase(a, b);
        }
    }
}
