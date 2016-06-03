// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Common.Utilities
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Text;
    using System.Text.RegularExpressions;
    using DotNetty.Common.Internal;
    using static Internal.MathUtil;

    /// <summary>
    ///     A string which has been encoded into a character encoding whose character always takes a single byte, similarly to
    ///     ASCII. It internally keeps its content in a byte array unlike {@link String}, which uses a character array, for
    ///     reduced memory footprint and faster data transfer from/to byte-based data structures such as a byte array and
    ///     {@link ByteBuffer}. It is often used in conjunction with {@link Headers} that require a {@link CharSequence}.
    ///     <p>
    ///         This class was designed to provide an immutable array of bytes, and caches some internal state based upon the
    ///         value
    ///         of this array. However underlying access to this byte array is provided via not copying the array on
    ///         construction or
    ///         {@link #array()}. If any changes are made to the underlying byte array it is the user's responsibility to call
    ///         {@link #arrayChanged()} so the state of this class can be reset.
    /// </summary>
    public sealed class AsciiString : CharSequence, IComparable<CharSequence>
    {
        public static readonly AsciiString EMPTY_STRING = new AsciiString(string.Empty);
        const char MAX_CHAR_VALUE = '\xff'; //255;

        public const int INDEX_NOT_FOUND = -1;

        /// <summary>
        ///     If this value is modified outside the constructor then call {@link #arrayChanged()}.
        /// </summary>
        readonly byte[] value;

        /// <summary>
        ///     Offset into {@link #value} that all operations should use when acting upon {@link #value}.
        /// </summary>
        readonly int offset;

        /// <summary>
        ///     Length in bytes for {@link #value} that we care about. This is independent from {@code value.length}
        ///     because we may be looking at a subsection of the array.
        /// </summary>
        readonly int length;

        /// <summary>
        ///     The hash code is cached after it is first computed. It can be reset with {@link #arrayChanged()}.
        /// </summary>
        int hash;

        /// <summary>
        ///     Used to cache the {@link #toString()} value.
        /// </summary>
        string str;

        /// <summary>
        ///     Initialize this byte string based upon a byte array. A copy will be made.
        /// </summary>
        public AsciiString(byte[] value)
            : this(value, true)
        {
        }

        /// <summary>
        ///     Initialize this byte string based upon a byte array.
        ///     {@code copy} determines if a copy is made or the array is shared.
        /// </summary>
        public AsciiString(byte[] value, bool copy)
            : this(value, 0, value.Length, copy)
        {
        }

        /// <summary>
        ///     Construct a new instance from a {@code byte[]} array.
        ///     @param copy {@code true} then a copy of the memory will be made. {@code false} the underlying memory
        ///     will be shared.
        /// </summary>
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
                if (isOutOfBounds(start, length, value.Length))
                {
                    throw new ArgumentOutOfRangeException(nameof(length),
                        $"expected: 0 <= start({start.ToString()}) <= start + length({length.ToString()}) <= value.length({value.Length.ToString()})");
                }
                this.value = value;
                this.offset = start;
            }
            this.length = length;
        }

        /// <summary>
        ///     Create a copy of {@code value} into this instance assuming ASCII encoding.
        /// </summary>
        public AsciiString(char[] value)
            : this(value, 0, value.Length)
        {
        }

        /// <summary>
        ///     Create a copy of {@code value} into this instance assuming ASCII encoding.
        ///     The copy will start at index {@code start} and copy {@code length} bytes.
        /// </summary>
        public AsciiString(char[] value, int start, int length)
        {
            if (isOutOfBounds(start, length, value.Length))
            {
                throw new ArgumentOutOfRangeException(nameof(length),
                    $"expected: 0 <= start({start.ToString()}) <= start + length({length.ToString()}) <= value.length({value.Length.ToString()})");
            }

            this.value = new byte[length];
            for (int i = 0, j = start; i < length; i++, j++)
            {
                this.value[i] = CharToByte(value[j]);
            }
            this.offset = 0;
            this.length = length;
        }

        /// <summary>
        ///     Create a copy of {@code value} into this instance using the encoding type of {@code charset}.
        /// </summary>
        public AsciiString(char[] value, Encoding encoding)
            : this(value, encoding, 0, value.Length)
        {
        }

        /// <summary>
        ///     Create a copy of {@code value} into a this instance using the encoding type of {@code charset}.
        ///     The copy will start at index {@code start} and copy {@code length} bytes.
        /// </summary>
        public AsciiString(char[] value, Encoding encoding, int start, int length)
        {
            this.value = encoding.GetBytes(value, start, length);
            this.offset = 0;
            this.length = this.value.Length;
        }

        public AsciiString(string s)
            : this(s, 0, s.Length)
        {
        }

        public AsciiString(string s, int offset, int length)
        {
            var buffer = new byte[Encoding.ASCII.GetMaxByteCount(length)];
            int actualLength = Encoding.ASCII.GetBytes(s, offset, length, buffer, 0);
            if (actualLength < buffer.Length)
            {
                this.value = new byte[actualLength];
                Buffer.BlockCopy(buffer, 0, this.value, 0, actualLength);
            }
            else
            {
                this.value = buffer;
            }
            this.offset = 0;
            this.length = actualLength;
        }

        /// <summary>
        ///     Create a copy of {@code value} into this instance assuming ASCII encoding.
        /// </summary>
        public AsciiString(CharSequence charSequence)
            : this(charSequence, 0, charSequence.Count)
        {
        }

        /// <summary>
        ///     Create a copy of {@code value} into this instance assuming ASCII encoding.
        ///     The copy will start at index {@code start} and copy {@code length} bytes.
        /// </summary>
        public AsciiString(CharSequence charSequence, int start, int length)
        {
            if (isOutOfBounds(start, length, charSequence.Length))
            {
                throw new ArgumentOutOfRangeException(
                    $"expected: 0 <= start({start.ToString()}) <= start + length({length.ToString()}) <= value.Length({charSequence.Length.ToString()})");
            }

            var v = new byte[length];
            for (int i = 0, j = start; i < length; i++, j++)
            {
                v[i] = CharToByte(charSequence[j]);
            }
            this.value = v;
        }

        /// <summary>
        ///     Create a copy of {@code value} into this instance using the encoding type of {@code charset}.
        /// </summary>
        public static AsciiString Create(CharSequence charSequence, Encoding charset) => Create(charSequence, charset, 0, charSequence.Length);

        /// <summary>
        ///     Create a copy of {@code value} into this instance using the encoding type of {@code charset}.
        ///     The copy will start at index {@code start} and copy {@code length} bytes.
        /// </summary>
        public static AsciiString Create(CharSequence charSequence, Encoding charset, int start, int length)
        {
            var buffer = new byte[charset.GetMaxByteCount(charSequence.Length)];
            int actualLength = charSequence.Encode(charset, start, length, buffer, 0);
            var val = new byte[actualLength];
            Buffer.BlockCopy(buffer, 0, val, 0, actualLength);
            return new AsciiString(val);
        }

        /// <summary>
        ///     Iterates over the readable bytes of this buffer with the specified {@code processor} in ascending order.
        ///     @return {@code -1} if the processor iterated to or beyond the end of the readable bytes.
        ///     The last-visited index If the {@link ByteProcessor#process(byte)} returned {@code false}.
        /// </summary>
        public int ForEachByte(ByteProcessor visitor) => this.ForEachByte0(0, this.Length, visitor);

        /// <summary>
        ///     Iterates over the specified area of this buffer with the specified {@code processor} in ascending order.
        ///     (i.e. {@code index}, {@code (index + 1)},  .. {@code (index + length - 1)}).
        ///     @return {@code -1} if the processor iterated to or beyond the end of the specified area.
        ///     The last-visited index If the {@link ByteProcessor#process(byte)} returned {@code false}.
        /// </summary>
        public int ForEachByte(int index, int length, ByteProcessor visitor)
        {
            if (isOutOfBounds(index, length, this.Length))
            {
                throw new ArgumentOutOfRangeException($"expected: 0 <= index({index}) <= start + length({length}) <= length({this.Length})");
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

        /// <summary>
        ///     Iterates over the readable bytes of this buffer with the specified {@code processor} in descending order.
        ///     @return {@code -1} if the processor iterated to or beyond the beginning of the readable bytes.
        ///     The last-visited index If the {@link ByteProcessor#process(byte)} returned {@code false}.
        /// </summary>
        public int ForEachByteDesc(ByteProcessor visitor) => this.ForEachByteDesc0(0, this.Length, visitor);

        /// <summary>
        ///     Iterates over the specified area of this buffer with the specified {@code processor} in descending order.
        ///     (i.e. {@code (index + length - 1)}, {@code (index + length - 2)}, ... {@code index}).
        ///     @return {@code -1} if the processor iterated to or beyond the beginning of the specified area.
        ///     The last-visited index If the {@link ByteProcessor#process(byte)} returned {@code false}.
        /// </summary>
        public int ForEachByteDesc(int index, int length, ByteProcessor visitor)
        {
            if (isOutOfBounds(index, length, this.Length))
            {
                throw new ArgumentOutOfRangeException($"expected: 0 <= index({index}) <= start + length({length}) <= length({this.Length})");
            }
            return this.ForEachByteDesc0(index, length, visitor);
        }

        int ForEachByteDesc0(int index, int length, ByteProcessor visitor)
        {
            int end = this.offset + index;
            for (int i = this.offset + index + length - 1; i >= end; --i)
            {
                if (!visitor.Process(this.value[i]))
                {
                    return i - this.offset;
                }
            }
            return -1;
        }

        public byte ByteAt(int index)
        {
            /// We must do a range check here to enforce the access does not go outside our sub region of the array.
            /// We rely on the array access itself to pick up the array out of bounds conditions
            if (index < 0 || index >= this.length)
            {
                throw new ArgumentOutOfRangeException("index: " + index + " must be in the range [0," + this.length.ToString() + ")");
            }
            /// Try to use unsafe to avoid double checking the index bounds
            return this.value[index + this.offset];
        }

        /// <summary>
        ///     Determine if this instance has 0 length.
        /// </summary>
        public bool IsEmpty => this.length == 0;

        /// <summary>
        ///     The length in bytes of this instance.
        /// </summary>
        /// @Override
        public int Length => this.length;

        /// <summary>
        ///     During normal use cases the {@link AsciiString} should be immutable, but if the underlying array is shared,
        ///     and changes then this needs to be called.
        /// </summary>
        public void ArrayChanged()
        {
            this.str = null;
            this.hash = 0;
        }

        /// <summary>
        ///     This gives direct access to the underlying storage array.
        ///     The {@link #toByteArray()} should be preferred over this method.
        ///     If the return value is changed then {@link #arrayChanged()} must be called.
        ///     @see #arrayOffset()
        ///     @see #isEntireArrayUsed()
        /// </summary>
        public byte[] Array() => this.value;

        /// <summary>
        ///     The offset into {@link #array()} for which data for this ByteString begins.
        ///     @see #array()
        ///     @see #isEntireArrayUsed()
        /// </summary>
        public int ArrayOffset() => this.offset;

        /// <summary>
        ///     Determine if the storage represented by {@link #array()} is entirely used.
        ///     @see #array()
        /// </summary>
        public bool IsEntireArrayUsed() => this.offset == 0 && this.length == this.value.Length;

        /// <summary>
        ///     Converts this string to a byte array.
        /// </summary>
        public byte[] ToByteArray() => this.ToByteArray(0, this.Length);

        /// <summary>
        ///     Converts a subset of this string to a byte array.
        ///     The subset is defined by the range [{@code start}, {@code end}).
        /// </summary>
        public byte[] ToByteArray(int start, int end)
        {
            Contract.Requires(end >= start);

            return this.value.Slice(start + this.offset, end - start + 1 + this.offset);
        }

        /// <summary>
        ///     Copies the content of this string to a byte array.
        ///     @param srcIdx the starting offset of characters to copy.
        ///     @param dst the destination byte array.
        ///     @param dstIdx the starting offset in the destination byte array.
        ///     @param length the number of characters to copy.
        /// </summary>
        public void Copy(int srcIdx, byte[] dst, int dstIdx, int length)
        {
            Contract.Requires(dst != null);

            if (isOutOfBounds(srcIdx, length, this.Length))
            {
                throw new ArgumentOutOfRangeException("expected: " + "0 <= srcIdx(" + srcIdx + ") <= srcIdx + length("
                    + length + ") <= srcLen(" + this.Length + ')');
            }

            System.Array.Copy(this.value, srcIdx + this.offset, dst, dstIdx, length);
        }

        public char this[int index] => ByteToChar(this.ByteAt(index));

        public void CopyTo(char[] array)
        {
            for (int index = 0; index < this.value.Length; index++)
            {
                array[index] = ByteToChar(this.value[index]);
            }
        }

        public int Encode(Encoding encoding, int startIndex, int length, byte[] destination, int destinationStartIndex)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Determines if this {@code String} contains the sequence of characters in the {@code CharSequence} passed.
        ///     @param cs the character sequence to search for.
        ///     @return {@code true} if the sequence of characters are contained in this string, otherwise {@code false}.
        /// </summary>
        public bool Contains(CharSequence cs) => this.IndexOf(cs) >= 0;

        /// <summary>
        ///     Compares the specified string to this string using the ASCII values of the characters. Returns 0 if the strings
        ///     contain the same characters in the same order. Returns a negative integer if the first non-equal character in
        ///     this string has an ASCII value which is less than the ASCII value of the character at the same position in the
        ///     specified string, or if this string is a prefix of the specified string. Returns a positive integer if the first
        ///     non-equal character in this string has a ASCII value which is greater than the ASCII value of the character at
        ///     the same position in the specified string, or if the specified string is a prefix of this string.
        ///     @param string the string to compare.
        ///     @return 0 if the strings are equal, a negative integer if this string is before the specified string, or a
        ///     positive integer if this string is after the specified string.
        ///     @throws NullPointerException if {@code string} is {@code null}.
        /// </summary>
        /// @Override
        public int CompareTo(CharSequence str)
        {
            if (this == str)
            {
                return 0;
            }

            int length1 = this.Length;
            int length2 = str.Length;
            int minLength = Math.Min(length1, length2);
            for (int i = 0, j = this.ArrayOffset(); i < minLength; i++, j++)
            {
                int result = ByteToChar(this.value[j]) - str[i];
                if (result != 0)
                {
                    return result;
                }
            }

            return length1 - length2;
        }

        /// <summary>
        ///     Concatenates this string and the specified string.
        ///     @param string the string to concatenate
        ///     @return a new string which is the concatenation of this string and the specified string.
        /// </summary>
        public AsciiString Concat(CharSequence str)
        {
            int thisLen = this.Length;
            int thatLen = str.Length;
            if (thatLen == 0)
            {
                return this;
            }

            var strAsAscii = str as AsciiString;
            if (strAsAscii != null)
            {
                if (this.IsEmpty)
                {
                    return strAsAscii;
                }

                var newAsciiValue = new byte[thisLen + thatLen];
                Buffer.BlockCopy(this.value, this.ArrayOffset(), newAsciiValue, 0, thisLen);
                Buffer.BlockCopy(strAsAscii.value, strAsAscii.ArrayOffset(), newAsciiValue, thisLen, thatLen);
                return new AsciiString(newAsciiValue, false);
            }

            if (this.IsEmpty)
            {
                return new AsciiString(str);
            }

            var newValue = new byte[thisLen + thatLen];
            Buffer.BlockCopy(this.value, this.ArrayOffset(), newValue, 0, thisLen);
            for (int i = thisLen, j = 0; i < newValue.Length; i++, j++)
            {
                newValue[i] = CharToByte(str[j]);
            }

            return new AsciiString(newValue, false);
        }

        /// <summary>
        ///     Compares the specified string to this string to determine if the specified string is a suffix.
        ///     @param suffix the suffix to look for.
        ///     @return {@code true} if the specified string is a suffix of this string, {@code false} otherwise.
        ///     @throws NullPointerException if {@code suffix} is {@code null}.
        /// </summary>
        public bool EndsWith(CharSequence suffix)
        {
            int suffixLen = suffix.Length;
            return this.RegionMatches(this.Length - suffixLen, suffix, 0, suffixLen);
        }

        /// <summary>
        ///     Compares the specified string to this string ignoring the case of the characters and returns true if they are
        ///     equal.
        ///     @param string the string to compare.
        ///     @return {@code true} if the specified string is equal to this string, {@code false} otherwise.
        /// </summary>
        public bool ContentEqualsIgnoreCase(CharSequence str)
        {
            if (str == null || str.Length != this.Length)
            {
                return false;
            }

            var ascii = str as AsciiString;
            if (ascii != null)
            {
                AsciiString rhs = ascii;
                for (int i = this.ArrayOffset(), j = rhs.ArrayOffset(); i < this.Length; ++i, ++j)
                {
                    if (!EqualsIgnoreCase(this.value[i], rhs.value[j]))
                    {
                        return false;
                    }
                }
                return true;
            }

            for (int i = this.ArrayOffset(), j = 0; i < this.Length; ++i, ++j)
            {
                if (!EqualsIgnoreCase(ByteToChar(this.value[i]), str[j]))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        ///     Copies the characters in this string to a character array.
        ///     @return a character array containing the characters of this string.
        /// </summary>
        public char[] ToCharArray() => this.ToCharArray(0, this.Length);

        /// <summary>
        ///     Copies the characters in this string to a character array.
        ///     @return a character array containing the characters of this string.
        /// </summary>
        public char[] ToCharArray(int start, int end)
        {
            int length = end - start;
            if (length == 0)
            {
                return ArrayExtensions.ZeroChars;
            }

            if (isOutOfBounds(start, length, this.Length))
            {
                throw new ArgumentOutOfRangeException("expected: " + "0 <= start(" + start + ") <= srcIdx + length("
                    + length + ") <= srcLen(" + this.Length + ')');
            }

            var buffer = new char[length];
            for (int i = 0, j = start + this.ArrayOffset(); i < length; i++, j++)
            {
                buffer[i] = ByteToChar(this.value[j]);
            }
            return buffer;
        }

        /// <summary>
        ///     Copied the content of this string to a character array.
        ///     @param srcIdx the starting offset of characters to copy.
        ///     @param dst the destination character array.
        ///     @param dstIdx the starting offset in the destination byte array.
        ///     @param length the number of characters to copy.
        /// </summary>
        public void Copy(int srcIdx, char[] dst, int dstIdx, int length)
        {
            if (dst == null)
            {
                throw new ArgumentNullException(nameof(dst));
            }

            if (isOutOfBounds(srcIdx, length, this.Length))
            {
                throw new ArgumentOutOfRangeException("expected: " + "0 <= srcIdx(" + srcIdx + ") <= srcIdx + length("
                    + length + ") <= srcLen(" + this.Length + ')');
            }

            int dstEnd = dstIdx + length;
            for (int i = dstIdx, j = srcIdx + this.ArrayOffset(); i < dstEnd; i++, j++)
            {
                dst[i] = ByteToChar(this.value[j]);
            }
        }

        /// <summary>
        ///     Copies a range of characters into a new string.
        ///     @param start the offset of the first character (inclusive).
        ///     @return a new string containing the characters from start to the end of the string.
        ///     @throws ArgumentOutOfRangeException if {@code start < 0} or {@ code start> Length}.
        /// </summary>
        public AsciiString SubSequence(int start) => this.SubSequence(start, this.Length);

        /// <summary>
        ///     Copies a range of characters into a new string.
        ///     @param start the offset of the first character (inclusive).
        ///     @param end The index to stop at (exclusive).
        ///     @return a new string containing the characters from start to the end of the string.
        ///     @throws ArgumentOutOfRangeException if {@code start < 0} or {@ code start> Length}.
        /// </summary>
        /// @Override
        public AsciiString SubSequence(int start, int end) => this.SubSequence(start, end, true);

        /// <summary>
        ///     Either copy or share a subset of underlying sub-sequence of bytes.
        ///     @param start the offset of the first character (inclusive).
        ///     @param end The index to stop at (exclusive).
        ///     @param copy If {@code true} then a copy of the underlying storage will be made.
        ///     If {@code false} then the underlying storage will be shared.
        ///     @return a new string containing the characters from start to the end of the string.
        ///     @throws ArgumentOutOfRangeException if {@code start < 0} or {@ code start> Length}.
        /// </summary>
        public AsciiString SubSequence(int start, int end, bool copy)
        {
            if (isOutOfBounds(start, end - start, this.Length))
            {
                throw new ArgumentOutOfRangeException("expected: 0 <= start(" + start + ") <= end (" + end + ") <= length("
                    + this.Length + ')');
            }

            if (start == 0 && end == this.Length)
            {
                return this;
            }

            if (end == start)
            {
                return EMPTY_STRING;
            }

            return new AsciiString(this.value, start + this.offset, end - start, copy);
        }

        /// <summary>
        ///     Searches in this string for the first index of the specified string. The search for the string starts at the
        ///     beginning and moves towards the end of this string.
        ///     @param string the string to find.
        ///     @return the index of the first character of the specified string in this string, -1 if the specified string is
        ///     not a substring.
        ///     @throws NullPointerException if {@code string} is {@code null}.
        /// </summary>
        public int IndexOf(CharSequence str) => this.IndexOf(str, 0);

        /// <summary>
        ///     Searches in this string for the index of the specified string. The search for the string starts at the specified
        ///     offset and moves towards the end of this string.
        ///     @param subString the string to find.
        ///     @param start the starting offset.
        ///     @return the index of the first character of the specified string in this string, -1 if the specified string is
        ///     not a substring.
        ///     @throws NullPointerException if {@code subString} is {@code null}.
        /// </summary>
        public int IndexOf(CharSequence subString, int start)
        {
            if (start < 0)
            {
                start = 0;
            }

            int thisLen = this.Length;

            int subCount = subString.Length;
            if (subCount <= 0)
            {
                return start < thisLen ? start : thisLen;
            }
            if (subCount > thisLen - start)
            {
                return -1;
            }

            char firstChar = subString[0];
            if (firstChar > MAX_CHAR_VALUE)
            {
                return -1;
            }
            ByteProcessor IndexOfVisitor = new ByteProcessor.IndexOfProcessor((byte)firstChar);
            for (;;)
            {
                int i = this.ForEachByte(start, thisLen - start, IndexOfVisitor);
                if (i == -1 || subCount + i > thisLen)
                {
                    return -1; // handles subCount > count || start >= count
                }
                int o1 = i, o2 = 0;
                while (++o2 < subCount && ByteToChar(this.value[++o1 + this.ArrayOffset()]) == subString[o2])
                {
                    /// Intentionally empty
                }
                if (o2 == subCount)
                {
                    return i;
                }
                start = i + 1;
            }
        }

        /// <summary>
        ///     Searches in this string for the index of the specified char {@code ch}.
        ///     The search for the char starts at the specified offset {@code start} and moves towards the end of this string.
        ///     @param ch the char to find.
        ///     @param start the starting offset.
        ///     @return the index of the first occurrence of the specified char {@code ch} in this string,
        ///     -1 if found no occurrence.
        /// </summary>
        public int IndexOf(char ch, int start)
        {
            if (start < 0)
            {
                start = 0;
            }

            int thisLen = this.Length;

            if (ch > MAX_CHAR_VALUE)
            {
                return -1;
            }

            return this.ForEachByte(start, thisLen - start, new ByteProcessor.IndexOfProcessor((byte)ch));
        }

        /// <summary>
        ///     Searches in this string for the last index of the specified string. The search for the string starts at the end
        ///     and moves towards the beginning of this string.
        ///     @param string the string to find.
        ///     @return the index of the first character of the specified string in this string, -1 if the specified string is
        ///     not a substring.
        ///     @throws NullPointerException if {@code string} is {@code null}.
        /// </summary>
        public int LastIndexOf(CharSequence str) => this.LastIndexOf(str, this.Length);

        /// <summary>
        ///     Searches in this string for the index of the specified string. The search for the string starts at the specified
        ///     offset and moves towards the beginning of this string.
        ///     @param subString the string to find.
        ///     @param start the starting offset.
        ///     @return the index of the first character of the specified string in this string , -1 if the specified string is
        ///     not a substring.
        ///     @throws NullPointerException if {@code subString} is {@code null}.
        /// </summary>
        public int LastIndexOf(CharSequence subString, int start)
        {
            int thisLen = this.Length;
            int subCount = subString.Length;

            if (subCount > thisLen || start < 0)
            {
                return -1;
            }

            if (subCount <= 0)
            {
                return start < thisLen ? start : thisLen;
            }

            start = Math.Min(start, thisLen - subCount);

            /// count and subCount are both >= 1
            char firstChar = subString[0];
            if (firstChar > MAX_CHAR_VALUE)
            {
                return -1;
            }
            ByteProcessor IndexOfVisitor = new ByteProcessor.IndexOfProcessor((byte)firstChar);
            for (;;)
            {
                int i = this.ForEachByteDesc(start, thisLen - start, IndexOfVisitor);
                if (i == -1)
                {
                    return -1;
                }
                int o1 = i, o2 = 0;
                while (++o2 < subCount && ByteToChar(this.value[++o1 + this.ArrayOffset()]) == subString[o2])
                {
                    /// Intentionally empty
                }
                if (o2 == subCount)
                {
                    return i;
                }
                start = i - 1;
            }
        }

        /// <summary>
        ///     Compares the specified string to this string and compares the specified range of characters to determine if they
        ///     are the same.
        ///     @param thisStart the starting offset in this string.
        ///     @param string the string to compare.
        ///     @param start the starting offset in the specified string.
        ///     @param length the number of characters to compare.
        ///     @return {@code true} if the ranges of characters are equal, {@code false} otherwise
        ///     @throws NullPointerException if {@code string} is {@code null}.
        /// </summary>
        public bool RegionMatches(int thisStart, CharSequence str, int start, int length)
        {
            if (str == null)
            {
                throw new ArgumentNullException(nameof(str));
            }

            if (start < 0 || str.Length - start < length)
            {
                return false;
            }

            int thisLen = this.Length;
            if (thisStart < 0 || thisLen - thisStart < length)
            {
                return false;
            }

            if (length <= 0)
            {
                return true;
            }

            int thatEnd = start + length;
            for (int i = start, j = thisStart + this.ArrayOffset(); i < thatEnd; i++, j++)
            {
                if (ByteToChar(this.value[j]) != str[i])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        ///     Compares the specified string to this string and compares the specified range of characters to determine if they
        ///     are the same. When ignoreCase is true, the case of the characters is ignored during the comparison.
        ///     @param ignoreCase specifies if case should be ignored.
        ///     @param thisStart the starting offset in this string.
        ///     @param string the string to compare.
        ///     @param start the starting offset in the specified string.
        ///     @param length the number of characters to compare.
        ///     @return {@code true} if the ranges of characters are equal, {@code false} otherwise.
        ///     @throws NullPointerException if {@code string} is {@code null}.
        /// </summary>
        public bool RegionMatches(bool ignoreCase, int thisStart, CharSequence str, int start, int length)
        {
            if (!ignoreCase)
            {
                return this.RegionMatches(thisStart, str, start, length);
            }

            if (str == null)
            {
                throw new ArgumentNullException(nameof(str));
            }

            int thisLen = this.Length;
            if (thisStart < 0 || length > thisLen - thisStart)
            {
                return false;
            }
            if (start < 0 || length > str.Length - start)
            {
                return false;
            }

            thisStart += this.ArrayOffset();
            int thisEnd = thisStart + length;
            while (thisStart < thisEnd)
            {
                if (!EqualsIgnoreCase(ByteToChar(this.value[thisStart++]), str[start++]))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        ///     Copies this string replacing occurrences of the specified character with another character.
        ///     @param oldChar the character to replace.
        ///     @param newChar the replacement character.
        ///     @return a new string with occurrences of oldChar replaced by newChar.
        /// </summary>
        public AsciiString Replace(char oldChar, char newChar)
        {
            if (oldChar > MAX_CHAR_VALUE)
            {
                return this;
            }

            byte oldCharByte = CharToByte(oldChar);
            int index = this.ForEachByte(new ByteProcessor.IndexOfProcessor(oldCharByte));
            if (index == -1)
            {
                return this;
            }

            byte newCharByte = CharToByte(newChar);
            var buffer = new byte[this.Length];
            for (int i = 0, j = this.ArrayOffset(); i < buffer.Length; i++, j++)
            {
                byte b = this.value[j];
                if (b == oldCharByte)
                {
                    b = newCharByte;
                }
                buffer[i] = b;
            }

            return new AsciiString(buffer, false);
        }

        /// <summary>
        ///     Compares the specified string to this string to determine if the specified string is a prefix.
        ///     @param prefix the string to look for.
        ///     @return {@code true} if the specified string is a prefix of this string, {@code false} otherwise
        ///     @throws NullPointerException if {@code prefix} is {@code null}.
        /// </summary>
        public bool StartsWith(CharSequence prefix) => this.StartsWith(prefix, 0);

        /// <summary>
        ///     Compares the specified string to this string, starting at the specified offset, to determine if the specified
        ///     string is a prefix.
        ///     @param prefix the string to look for.
        ///     @param start the starting offset.
        ///     @return {@code true} if the specified string occurs in this string at the specified offset, {@code false}
        ///     otherwise.
        ///     @throws NullPointerException if {@code prefix} is {@code null}.
        /// </summary>
        public bool StartsWith(CharSequence prefix, int start) => this.RegionMatches(start, prefix, 0, prefix.Length);

        /// <summary>
        ///     Converts the characters in this string to lowercase, using the default Locale.
        ///     @return a new string containing the lowercase characters equivalent to the characters in this string.
        /// </summary>
        public AsciiString ToLowerCase()
        {
            bool lowercased = true;
            int i, j;
            int len = this.Length + this.ArrayOffset();
            for (i = this.ArrayOffset(); i < len; ++i)
            {
                byte b = this.value[i];
                if (b >= 'A' && b <= 'Z')
                {
                    lowercased = false;
                    break;
                }
            }

            /// Check if this string does not contain any uppercase characters.
            if (lowercased)
            {
                return this;
            }

            var newValue = new byte[this.Length];
            for (i = 0, j = this.ArrayOffset(); i < newValue.Length; ++i, ++j)
            {
                newValue[i] = ToLowerCase(this.value[j]);
            }

            return new AsciiString(newValue, false);
        }

        /// <summary>
        ///     Converts the characters in this string to uppercase, using the default Locale.
        ///     @return a new string containing the uppercase characters equivalent to the characters in this string.
        /// </summary>
        public AsciiString ToUpperCase()
        {
            bool uppercased = true;
            int i, j;
            int len = this.Length + this.ArrayOffset();
            for (i = this.ArrayOffset(); i < len; ++i)
            {
                byte b = this.value[i];
                if (b >= 'a' && b <= 'z')
                {
                    uppercased = false;
                    break;
                }
            }

            /// Check if this string does not contain any lowercase characters.
            if (uppercased)
            {
                return this;
            }

            var newValue = new byte[this.Length];
            for (i = 0, j = this.ArrayOffset(); i < newValue.Length; ++i, ++j)
            {
                newValue[i] = ToUpperCase(this.value[j]);
            }

            return new AsciiString(newValue, false);
        }

        /// <summary>
        ///     Copies this string removing white space characters from the beginning and end of the string.
        ///     @return a new string with characters {@code <= \\u0020} removed from the beginning and the end.
        /// </summary>
        public AsciiString Trim()
        {
            int start = this.ArrayOffset(), last = this.ArrayOffset() + this.Length - 1;
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

        /// <summary>
        ///     Compares a {@code CharSequence} to this {@code String} to determine if their contents are equal.
        ///     @param a the character sequence to compare to.
        ///     @return {@code true} if equal, otherwise {@code false}
        /// </summary>
        public bool ContentEquals(CharSequence a)
        {
            if (a == null || a.Length != this.Length)
            {
                return false;
            }
            if (a is AsciiString)
            {
                return this.Equals(a);
            }

            for (int i = this.ArrayOffset(), j = 0; j < a.Length; ++i, ++j)
            {
                if (ByteToChar(this.value[i]) != a[j])
                {
                    return false;
                }
            }
            return true;
        }
        
        // todo: port: revisit
        ///// <summary>
        /////     Determines whether this string matches a given regular expression.
        /////     @param expr the regular expression to be matched.
        /////     @return {@code true} if the expression matches, otherwise {@code false}.
        /////     @throws PatternSyntaxException if the syntax of the supplied regular expression is not valid.
        /////     @throws NullPointerException if {@code expr} is {@code null}.
        ///// </summary>
        //public bool Matches(string expr) => Pattern.matches(expr, this);

        /// <summary>
        ///     Splits this string using the supplied regular expression {@code expr}. The parameter {@code max} controls the
        ///     behavior how many times the pattern is applied to the string.
        ///     @param expr the regular expression used to divide the string.
        ///     @param max the number of entries in the resulting array.
        ///     @return an array of Strings created by separating the string along matches of the regular expression.
        ///     @throws NullPointerException if {@code expr} is {@code null}.
        ///     @throws PatternSyntaxException if the syntax of the supplied regular expression is not valid.
        ///     @see Pattern#split(CharSequence, int)
        /// </summary>
        public AsciiString[] Split(string expr, int max) => ToAsciiStringArray(new Regex(expr).Split(this.ToString(), max));

        /// <summary>
        ///     Splits the specified {@link String} with the specified delimiter..
        /// </summary>
        public AsciiString[] Split(char delim)
        {
            List<object> res = InternalThreadLocalMap.Get().List;

            int start = 0;
            int length = this.Length;
            for (int i = start; i < length; i++)
            {
                if (this[i] == delim)
                {
                    if (start == i)
                    {
                        res.Add(EMPTY_STRING);
                    }
                    else
                    {
                        res.Add(new AsciiString(this.value, start + this.ArrayOffset(), i - start, false));
                    }
                    start = i + 1;
                }
            }

            if (start == 0)
            {
                // If no delimiter was found in the value
                res.Add(this);
            }
            else
            {
                if (start != length)
                {
                    // Add the last element if it's not empty.
                    res.Add(new AsciiString(this.value, start + this.ArrayOffset(), length - start, false));
                }
                else
                {
                    // Truncate trailing empty elements.
                    for (int i = res.Count - 1; i >= 0; i--)
                    {
                        if (((CharSequence)res[i]).IsEmpty)
                        {
                            res.RemoveAt(i);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }

            var result = new AsciiString[res.Count];
            res.CopyTo(result, 0);
            res.Clear();
            return result;
        }

        /// <summary>
        ///     {@inheritDoc}
        ///     <p>
        ///         Provides a case-insensitive hash code for Ascii like byte strings.
        /// </summary>
        public override int GetHashCode() => this.hash == 0 ? (this.hash = PlatformDependent.hashCodeAscii(this.value, this.offset, this.length)) : this.hash;

        public override bool Equals(object obj)
        {
            if (!(obj is AsciiString))
            {
                return false;
            }

            if (this == obj)
            {
                return true;
            }

            var other = (AsciiString)obj;
            return this.Length == other.Length && this.GetHashCode() == other.GetHashCode() &&
                PlatformDependent.equals(this.Array(), this.ArrayOffset(), other.Array(), other.ArrayOffset(), this.Length);
        }

        public IEnumerator<char> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Translates the entire byte string to a {@link String}.
        ///     @see {@link #toString(int)}
        /// </summary>
        public override string ToString() => this.str ?? (this.str = this.ToString(0));

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        /// <summary>
        ///     Translates the entire byte string to a {@link String} using the {@code charset} encoding.
        ///     @see {@link #toString(int, int)}
        /// </summary>
        public string ToString(int start) => this.ToString(start, this.Length);

        /// <summary>
        ///     Translates the [{@code start}, {@code end}) range of this byte string to a {@link String}.
        /// </summary>
        public string ToString(int start, int end)
        {
            int length = end - start;
            if (length == 0)
            {
                return "";
            }

            if (isOutOfBounds(start, length, this.Length))
            {
                throw new ArgumentOutOfRangeException("expected: " + "0 <= start(" + start + ") <= srcIdx + length("
                    + length + ") <= srcLen(" + this.Length + ')');
            }

            //@SuppressWarnings("deprecation")
            string s = Encoding.ASCII.GetString(this.value, start + this.offset, length); // todo: optimize: re-use char[]
            return s;
        }

        public bool ParseBool() => this.length >= 1 && this.value[this.offset] != 0;

        public char ParseChar() => this.ParseChar(0);

        public char ParseChar(int start)
        {
            if (start + 1 >= this.Length)
            {
                throw new ArgumentOutOfRangeException("2 bytes required to convert to character. index " +
                    start + " would go out of bounds.");
            }
            int startWithOffset = start + this.offset;
            return (char)((ByteToChar(this.value[startWithOffset]) << 8) | ByteToChar(this.value[startWithOffset + 1]));
        }

        public short ParseShort() => this.ParseShort(0, this.Length, 10);

        public short ParseShort(int radix) => this.ParseShort(0, this.Length, radix);

        public short ParseShort(int start, int end) => this.ParseShort(start, end, 10);

        public short ParseShort(int start, int end, int radix)
        {
            int intValue = this.ParseInt(start, end, radix);
            short result = (short)intValue;
            if (result != intValue)
            {
                throw new FormatException(this.SubSequence(start, end, false).ToString());
            }
            return result;
        }

        public int ParseInt() => this.ParseInt(0, this.Length, 10);

        public int ParseInt(int radix) => this.ParseInt(0, this.Length, radix);

        public int ParseInt(int start, int end) => this.ParseInt(start, end, 10);

        public int ParseInt(int start, int end, int radix)
        {
            if (radix != 10)
            {
                throw new NotSupportedException("Only Radix of 10 is supported.");
            }

            // todo
            //if (radix < Char.MIN_RADIX || radix > Char.MAX_RADIX)
            //{
            //    throw new FormatException();
            //}

            if (start == end)
            {
                throw new FormatException();
            }

            int i = start;
            bool negative = this.ByteAt(i) == '-';
            if (negative && ++i == end)
            {
                throw new FormatException(this.SubSequence(start, end, false).ToString());
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
                int digit = this.value[currOffset++ + this.offset] & 0xFF - '0';
                if (digit == -1)
                {
                    throw new FormatException(this.SubSequence(start, end, false).ToString());
                }
                if (max > result)
                {
                    throw new FormatException(this.SubSequence(start, end, false).ToString());
                }
                int next = result * radix - digit;
                if (next > result)
                {
                    throw new FormatException(this.SubSequence(start, end, false).ToString());
                }
                result = next;
            }
            if (!negative)
            {
                result = -result;
                if (result < 0)
                {
                    throw new FormatException(this.SubSequence(start, end, false).ToString());
                }
            }
            return result;
        }

        public long ParseLong() => this.ParseLong(0, this.Length, 10);

        public long ParseLong(int radix) => this.ParseLong(0, this.Length, radix);

        public long ParseLong(int start, int end) => this.ParseLong(start, end, 10);

        public long ParseLong(int start, int end, int radix)
        {
            if (radix != 10)
            {
                throw new NotSupportedException("Only Radix of 10 is supported.");
            }

            /// todo
            //if (radix < Char.MIN_RADIX || radix > Char.MAX_RADIX)
            //{
            ///    throw new FormatException();
            //}

            if (start == end)
            {
                throw new FormatException();
            }

            int i = start;
            bool negative = this.ByteAt(i) == '-';
            if (negative && ++i == end)
            {
                throw new FormatException(this.SubSequence(start, end, false).ToString());
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
                int digit = this.value[currOffset++ + this.offset] & 0xFF - '0'; //Char.digit((char)(value[currOffset++ + offset] & 0xFF), radix);
                if (digit == -1)
                {
                    throw new FormatException(this.SubSequence(start, end, false).ToString());
                }
                if (max > result)
                {
                    throw new FormatException(this.SubSequence(start, end, false).ToString());
                }
                long next = result * radix - digit;
                if (next > result)
                {
                    throw new FormatException(this.SubSequence(start, end, false).ToString());
                }
                result = next;
            }
            if (!negative)
            {
                result = -result;
                if (result < 0)
                {
                    throw new FormatException(this.SubSequence(start, end, false).ToString());
                }
            }
            return result;
        }

        public float parseFloat() => this.parseFloat(0, this.Length);

        public float parseFloat(int start, int end) => float.Parse(this.ToString(start, end));

        public double parseDouble() => this.parseDouble(0, this.Length);

        public double parseDouble(int start, int end) => double.Parse(this.ToString(start, end));

        public static readonly IEqualityComparer<CharSequence> CASE_INSENSITIVE_HASHER = new CaseInsensitiveCharSequenceComparer();

        sealed class CaseInsensitiveCharSequenceComparer : IEqualityComparer<CharSequence>
        {
            public int GetHashCode(CharSequence o) => AsciiString.GetHashCode(o);

            public bool Equals(CharSequence a, CharSequence b) => ContentEqualsIgnoreCase(a, b);
        }

        public static readonly IEqualityComparer<CharSequence> CASE_SENSITIVE_HASHER = new CaseSensitiveCharSequenceComparer();

        sealed class CaseSensitiveCharSequenceComparer : IEqualityComparer<CharSequence>
        {
            public int GetHashCode(CharSequence o) => AsciiString.GetHashCode(o);

            public bool Equals(CharSequence a, CharSequence b) => ContentEquals(a, b);
        }

        /// <summary>
        ///     Returns an {@link AsciiString} containing the given character sequence. If the given string is already a
        ///     {@link AsciiString}, just returns the same instance.
        /// </summary>
        public static AsciiString of(CharSequence s) => s as AsciiString ?? new AsciiString(s);

        /// <summary>
        ///     Returns the case-insensitive hash code of the specified string. Note that this method uses the same hashing
        ///     algorithm with {@link #hashCode()} so that you can put both {@link AsciiString}s and arbitrary
        ///     {@link CharSequence}s into the same headers.
        /// </summary>
        public static int GetHashCode(CharSequence value)
        {
            if (value == null)
            {
                return 0;
            }
            if (value is AsciiString)
            {
                return value.GetHashCode();
            }

            return PlatformDependent.hashCodeAscii(value);
        }

        /// <summary>
        ///     Determine if {@code a} contains {@code b} in a case sensitive manner.
        /// </summary>
        public static bool Contains(CharSequence a, CharSequence b) => Contains(a, b, DefaultCharEqualityComparator.Instance);

        /// <summary>
        ///     Determine if {@code a} contains {@code b} in a case insensitive manner.
        /// </summary>
        public static bool ContainsIgnoreCase(CharSequence a, CharSequence b) => Contains(a, b, AsciiCaseInsensitiveCharEqualityComparator.Instance);

        /// <summary>
        ///     Returns {@code true} if both {@link CharSequence}'s are equals when ignore the case. This only supports 8-bit
        ///     ASCII.
        /// </summary>
        public static bool ContentEqualsIgnoreCase(CharSequence a, CharSequence b)
        {
            if (a == null || b == null)
            {
                return a == b;
            }

            var aAsAscii = a as AsciiString;
            if (aAsAscii != null)
            {
                return aAsAscii.ContentEqualsIgnoreCase(b);
            }

            var bAsAscii = b as AsciiString;
            if (bAsAscii != null)
            {
                return bAsAscii.ContentEqualsIgnoreCase(a);
            }

            if (a.Length != b.Length)
            {
                return false;
            }
            for (int i = 0, j = 0; i < a.Length; ++i, ++j)
            {
                if (!EqualsIgnoreCase(a[i], b[j]))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        ///     Determine if {@code collection} contains {@code value} and using
        ///     {@link #contentEqualsIgnoreCase(CharSequence, CharSequence)} to compare values.
        ///     @param collection The collection to look for and equivalent element as {@code value}.
        ///     @param value The value to look for in {@code collection}.
        ///     @return {@code true} if {@code collection} contains {@code value} according to
        ///     {@link #contentEqualsIgnoreCase(CharSequence, CharSequence)}. {@code false} otherwise.
        ///     @see #contentEqualsIgnoreCase(CharSequence, CharSequence)
        /// </summary>
        public static bool ContainsContentEqualsIgnoreCase(IEnumerable<CharSequence> collection, CharSequence value)
        {
            foreach (CharSequence v in collection)
            {
                if (ContentEqualsIgnoreCase(value, v))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        ///     Determine if {@code a} contains all of the values in {@code b} using
        ///     {@link #contentEqualsIgnoreCase(CharSequence, CharSequence)} to compare values.
        ///     @param a The collection under test.
        ///     @param b The values to test for.
        ///     @return {@code true} if {@code a} contains all of the values in {@code b} using
        ///     {@link #contentEqualsIgnoreCase(CharSequence, CharSequence)} to compare values. {@code false} otherwise.
        ///     @see #contentEqualsIgnoreCase(CharSequence, CharSequence)
        /// </summary>
        public static bool ContainsAllContentEqualsIgnoreCase(IEnumerable<CharSequence> a, IEnumerable<CharSequence> b)
        {
            foreach (CharSequence v in b)
            {
                if (!ContainsContentEqualsIgnoreCase(a, v))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        ///     Returns {@code true} if the content of both {@link CharSequence}'s are equals. This only supports 8-bit ASCII.
        /// </summary>
        public static bool ContentEquals(CharSequence a, CharSequence b)
        {
            if (a == null || b == null)
            {
                return a == b;
            }

            var aAsAscii = a as AsciiString;
            if (aAsAscii != null)
            {
                return aAsAscii.ContentEquals(b);
            }

            var bAsAscii = b as AsciiString;
            if (bAsAscii != null)
            {
                return bAsAscii.ContentEquals(a);
            }

            if (a.Length != b.Length)
            {
                return false;
            }
            for (int i = 0; i < a.Length; ++i)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }
            return true;
        }

        static AsciiString[] ToAsciiStringArray(string[] jdkResult)
        {
            var res = new AsciiString[jdkResult.Length];
            for (int i = 0; i < jdkResult.Length; i++)
            {
                res[i] = new AsciiString(jdkResult[i]);
            }
            return res;
        }

        sealed class DefaultCharEqualityComparator : IEqualityComparer<char>
        {
            internal static readonly DefaultCharEqualityComparator Instance = new DefaultCharEqualityComparator();

            DefaultCharEqualityComparator()
            {
            }

            public bool Equals(char x, char y) => x == y;

            public int GetHashCode(char obj) => obj.GetHashCode();
        }

        sealed class AsciiCaseInsensitiveCharEqualityComparator : IEqualityComparer<char>
        {
            internal static readonly AsciiCaseInsensitiveCharEqualityComparator Instance = new AsciiCaseInsensitiveCharEqualityComparator();

            AsciiCaseInsensitiveCharEqualityComparator()
            {
            }

            public bool Equals(char x, char y) => EqualsIgnoreCase(x, y);

            public int GetHashCode(char obj)
            {
                throw new NotImplementedException();
            }
        }

        sealed class GeneralCaseInsensitiveCharEqualityComparator : IEqualityComparer<char>
        {
            internal static readonly GeneralCaseInsensitiveCharEqualityComparator Instance = new GeneralCaseInsensitiveCharEqualityComparator();

            GeneralCaseInsensitiveCharEqualityComparator()
            {
            }

            public bool Equals(char a, char b) => char.ToUpper(a) == char.ToUpper(b) ||
                char.ToLower(a) == char.ToLower(b);

            public int GetHashCode(char obj)
            {
                throw new NotImplementedException();
            }
        }

        static bool Contains(CharSequence a, CharSequence b, IEqualityComparer<char> cmp)
        {
            if (a == null || b == null || a.Length < b.Length)
            {
                return false;
            }
            if (b.Length == 0)
            {
                return true;
            }
            int bStart = 0;
            for (int i = 0; i < a.Length; ++i)
            {
                if (cmp.Equals(b[bStart], a[i]))
                {
                    /// If b is consumed then true.
                    if (++bStart == b.Length)
                    {
                        return true;
                    }
                }
                else if (a.Length - i < b.Length)
                {
                    /// If there are not enough characters left in a for b to be contained, then false.
                    return false;
                }
                else
                {
                    bStart = 0;
                }
            }
            return false;
        }

        static bool regionMatchesCharSequences(CharSequence cs, int csStart,
            CharSequence str, int start, int length,
            IEqualityComparer<char> charEqualityComparator)
        {
            //general purpose implementation for CharSequences
            if (csStart < 0 || length > cs.Length - csStart)
            {
                return false;
            }
            if (start < 0 || length > str.Length - start)
            {
                return false;
            }

            int csIndex = csStart;
            int csEnd = csIndex + length;
            int stringIndex = start;

            while (csIndex < csEnd)
            {
                char c1 = cs[csIndex++];
                char c2 = str[stringIndex++];

                if (!charEqualityComparator.Equals(c1, c2))
                {
                    return false;
                }
            }
            return true;
        }

        // todo: port: revisit
        ///// <summary>
        /////     This methods make regionMatches operation correctly for any chars in strings
        /////     @param cs the {@code CharSequence} to be processed
        /////     @param ignoreCase specifies if case should be ignored.
        /////     @param csStart the starting offset in the {@code cs} CharSequence
        /////     @param string the {@code CharSequence} to compare.
        /////     @param start the starting offset in the specified {@code string}.
        /////     @param length the number of characters to compare.
        /////     @return {@code true} if the ranges of characters are equal, {@code false} otherwise.
        ///// </summary>
        //public static bool RegionMatches(CharSequence cs, bool ignoreCase, int csStart,
        //    CharSequence str, int start, int length)
        //{
        //    if (cs == null || str == null)
        //    {
        //        return false;
        //    }

        //    if (cs is string && str is string)
        //    {
        //        return ((string)cs).regionMatches(ignoreCase, csStart, (string)str, start, length);
        //    }

        //    if (cs is AsciiString)
        //    {
        //        return ((AsciiString)cs).RegionMatches(ignoreCase, csStart, str, start, length);
        //    }

        //    return regionMatchesCharSequences(cs, csStart, str, start, length,
        //        ignoreCase ? (IEqualityComparer<char>)GeneralCaseInsensitiveCharEqualityComparator.Instance :
        //            DefaultCharEqualityComparator.Instance);
        //}

        ///// <summary>
        /////     This is optimized version of regionMatches for string with ASCII chars only
        /////     @param cs the {@code CharSequence} to be processed
        /////     @param ignoreCase specifies if case should be ignored.
        /////     @param csStart the starting offset in the {@code cs} CharSequence
        /////     @param string the {@code CharSequence} to compare.
        /////     @param start the starting offset in the specified {@code string}.
        /////     @param length the number of characters to compare.
        /////     @return {@code true} if the ranges of characters are equal, {@code false} otherwise.
        ///// </summary>
        //public static bool regionMatchesAscii(CharSequence cs, bool ignoreCase, int csStart,
        //    CharSequence str, int start, int length)
        //{
        //    if (cs == null || str == null)
        //    {
        //        return false;
        //    }

        //    if (!ignoreCase && cs is string && str is string)
        //    {
        //        //we don't call regionMatches from String for ignoreCase==true. It's a general purpose method,
        //        //which make complex comparison in case of ignoreCase==true, which is useless for ASCII-only strings.
        //        //To avoid applying this complex ignore-case comparison, we will use regionMatchesCharSequences
        //        return ((string)cs).regionMatches(false, csStart, (string)str, start, length);
        //    }

        //    if (cs is AsciiString)
        //    {
        //        return ((AsciiString)cs).RegionMatches(ignoreCase, csStart, str, start, length);
        //    }

        //    return regionMatchesCharSequences(cs, csStart, str, start, length,
        //        ignoreCase ? (IEqualityComparer<char>)AsciiCaseInsensitiveCharEqualityComparator.Instance :
        //            DefaultCharEqualityComparator.Instance);
        //}

        // todo: port: revisit
        ///// <summary>
        /////     <p>
        /////         Case in-sensitive find of the first index within a CharSequence
        /////         from the specified position.
        /////     </p>
        /////     <p>
        /////         A {@code null} CharSequence will return {@code -1}.
        /////         A negative start position is treated as zero.
        /////         An empty ("") search CharSequence always matches.
        /////         A start position greater than the string length only matches
        /////         an empty search CharSequence.
        /////     </p>
        /////     <pre>
        /////         AsciiString.indexOfIgnoreCase(null, *, *)          = -1
        /////         AsciiString.indexOfIgnoreCase(*, null, *)          = -1
        /////         AsciiString.indexOfIgnoreCase("", "", 0)           = 0
        /////         AsciiString.indexOfIgnoreCase("aabaabaa", "A", 0)  = 0
        /////         AsciiString.indexOfIgnoreCase("aabaabaa", "B", 0)  = 2
        /////         AsciiString.indexOfIgnoreCase("aabaabaa", "AB", 0) = 1
        /////         AsciiString.indexOfIgnoreCase("aabaabaa", "B", 3)  = 5
        /////         AsciiString.indexOfIgnoreCase("aabaabaa", "B", 9)  = -1
        /////         AsciiString.indexOfIgnoreCase("aabaabaa", "B", -1) = 2
        /////         AsciiString.indexOfIgnoreCase("aabaabaa", "", 2)   = 2
        /////         AsciiString.indexOfIgnoreCase("abc", "", 9)        = -1
        /////     </pre>
        /////     @param str  the CharSequence to check, may be null
        /////     @param searchStr  the CharSequence to find, may be null
        /////     @param startPos  the start position, negative treated as zero
        /////     @return the first index of the search CharSequence (always &ge; startPos),
        /////     -1 if no match or {@code null} string input
        /////     @throws NullPointerException if {@code cs} or {@code string} is {@code null}.
        ///// </summary>
        //public static int IndexOfIgnoreCase(CharSequence str, CharSequence searchStr, int startPos)
        //{
        //    if (str == null || searchStr == null)
        //    {
        //        return INDEX_NOT_FOUND;
        //    }
        //    if (startPos < 0)
        //    {
        //        startPos = 0;
        //    }
        //    int searchStrLen = searchStr.Length;
        //    int endLimit = str.Length - searchStrLen + 1;
        //    if (startPos > endLimit)
        //    {
        //        return INDEX_NOT_FOUND;
        //    }
        //    if (searchStrLen == 0)
        //    {
        //        return startPos;
        //    }
        //    for (int i = startPos; i < endLimit; i++)
        //    {
        //        if (RegionMatches(str, true, i, searchStr, 0, searchStrLen))
        //        {
        //            return i;
        //        }
        //    }
        //    return INDEX_NOT_FOUND;
        //}

        // todo: port: revisit
        ///// <summary>
        /////     <p>
        /////         Case in-sensitive find of the first index within a CharSequence
        /////         from the specified position. This method optimized and works correctly for ASCII CharSequences only
        /////     </p>
        /////     <p>
        /////         A {@code null} CharSequence will return {@code -1}.
        /////         A negative start position is treated as zero.
        /////         An empty ("") search CharSequence always matches.
        /////         A start position greater than the string length only matches
        /////         an empty search CharSequence.
        /////     </p>
        /////     <pre>
        /////         AsciiString.indexOfIgnoreCase(null, *, *)          = -1
        /////         AsciiString.indexOfIgnoreCase(*, null, *)          = -1
        /////         AsciiString.indexOfIgnoreCase("", "", 0)           = 0
        /////         AsciiString.indexOfIgnoreCase("aabaabaa", "A", 0)  = 0
        /////         AsciiString.indexOfIgnoreCase("aabaabaa", "B", 0)  = 2
        /////         AsciiString.indexOfIgnoreCase("aabaabaa", "AB", 0) = 1
        /////         AsciiString.indexOfIgnoreCase("aabaabaa", "B", 3)  = 5
        /////         AsciiString.indexOfIgnoreCase("aabaabaa", "B", 9)  = -1
        /////         AsciiString.indexOfIgnoreCase("aabaabaa", "B", -1) = 2
        /////         AsciiString.indexOfIgnoreCase("aabaabaa", "", 2)   = 2
        /////         AsciiString.indexOfIgnoreCase("abc", "", 9)        = -1
        /////     </pre>
        /////     @param str  the CharSequence to check, may be null
        /////     @param searchStr  the CharSequence to find, may be null
        /////     @param startPos  the start position, negative treated as zero
        /////     @return the first index of the search CharSequence (always &ge; startPos),
        /////     -1 if no match or {@code null} string input
        /////     @throws NullPointerException if {@code cs} or {@code string} is {@code null}.
        ///// </summary>
        //public static int IndexOfIgnoreCaseAscii(CharSequence str, CharSequence searchStr, int startPos)
        //{
        //    if (str == null || searchStr == null)
        //    {
        //        return INDEX_NOT_FOUND;
        //    }
        //    if (startPos < 0)
        //    {
        //        startPos = 0;
        //    }
        //    int searchStrLen = searchStr.Length;
        //    int endLimit = str.Length - searchStrLen + 1;
        //    if (startPos > endLimit)
        //    {
        //        return INDEX_NOT_FOUND;
        //    }
        //    if (searchStrLen == 0)
        //    {
        //        return startPos;
        //    }
        //    for (int i = startPos; i < endLimit; i++)
        //    {
        //        if (regionMatchesAscii(str, true, i, searchStr, 0, searchStrLen))
        //        {
        //            return i;
        //        }
        //    }
        //    return INDEX_NOT_FOUND;
        //}

        /// <summary>
        ///     <p>
        ///         Finds the first index in the {@code CharSequence} that matches the
        ///         specified character.
        ///     </p>
        ///     @param cs  the {@code CharSequence} to be processed, not null
        ///     @param searchChar the char to be searched for
        ///     @param start  the start index, negative starts at the string start
        ///     @return the index where the search char was found,
        ///     -1 if char {@code searchChar} is not found or {@code cs == null}
        /// </summary>
        //-----------------------------------------------------------------------
        public static int IndexOf(CharSequence cs, char searchChar, int start)
        {
            if (cs is StringCharSequence)
            {
                return ((StringCharSequence)cs).Value.IndexOf(searchChar, start);
            }
            else if (cs is AsciiString)
            {
                return ((AsciiString)cs).IndexOf(searchChar, start);
            }

            if (cs == null)
            {
                return INDEX_NOT_FOUND;
            }
            int sz = cs.Length;
            if (start < 0)
            {
                start = 0;
            }
            for (int i = start; i < sz; i++)
            {
                if (cs[i] == searchChar)
                {
                    return i;
                }
            }
            return INDEX_NOT_FOUND;
        }

        static bool EqualsIgnoreCase(byte a, byte b) => a == b || ToLowerCase(a) == ToLowerCase(b);

        static bool EqualsIgnoreCase(char a, char b) => a == b || ToLowerCase(a) == ToLowerCase(b);

        static byte ToLowerCase(byte b) => IsUpperCase(b) ? (byte)(b + 32) : b;

        static char ToLowerCase(char c) => IsUpperCase(c) ? (char)(c + 32) : c;

        static byte ToUpperCase(byte b) => IsLowerCase(b) ? (byte)(b - 32) : b;

        static bool IsLowerCase(byte value) => value >= 'a' && value <= 'z';

        public static bool IsUpperCase(byte value) => value >= 'A' && value <= 'Z';

        public static bool IsUpperCase(char value) => value >= 'A' && value <= 'Z';

        public static byte CharToByte(char c) => (byte)((c > MAX_CHAR_VALUE) ? '?' : c);

        public static char ByteToChar(byte b) => (char)(b & 0xFF);

        public int Count => this.length;
    }
}