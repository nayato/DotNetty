// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Common.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.Runtime.CompilerServices;
    using System.Text;

    /// <summary>
    ///     String utility class.
    /// </summary>
    public static class StringUtil
    {
        public const char DoubleQuote = '\"';
        public const char Comma = ',';
        public const char LineFeed = '\n';
        public const char CarriageReturn = '\r';
        public const char Tab = '\t';
        public const char Space = '\x20';
        public const byte UpperCaseToLowerCaseAsciiOffset = 'a' - 'A';
        public static readonly string Newline;
        static readonly string[] Byte2HexPad = new string[256];
        static readonly string[] Byte2HexNopad = new string[256];
        /**
         * 2 - Quote character at beginning and end.
         * 5 - Extra allowance for anticipated escape characters that may be added.
        */
        static readonly int CsvNumberEscapeCharacters = 2 + 5;
        static readonly char PackageSeparatorChar = '.';

        static StringUtil()
        {
            Newline = Environment.NewLine;

            // Generate the lookup table that converts a byte into a 2-digit hexadecimal integer.
            int i;
            for (i = 0; i < 10; i++)
            {
                var buf = new StringBuilder(2);
                buf.Append('0');
                buf.Append(i);
                Byte2HexPad[i] = buf.ToString();
                Byte2HexNopad[i] = (i).ToString();
            }
            for (; i < 16; i++)
            {
                var buf = new StringBuilder(2);
                char c = (char)('a' + i - 10);
                buf.Append('0');
                buf.Append(c);
                Byte2HexPad[i] = buf.ToString();
                Byte2HexNopad[i] = c.ToString(); /* String.valueOf(c);*/
            }
            for (; i < Byte2HexPad.Length; i++)
            {
                var buf = new StringBuilder(2);
                buf.Append(i.ToString("X") /*Integer.toHexString(i)*/);
                string str = buf.ToString();
                Byte2HexPad[i] = str;
                Byte2HexNopad[i] = str;
            }
        }

        /// <summary>
        ///     Splits the specified {@link String} with the specified delimiter in maxParts maximum parts.
        ///     This operation is a simplified and optimized
        ///     version of {@link String#split(String, int)}.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="delim"></param>
        /// <param name="maxParts"></param>
        /// <returns></returns>
        public static string[] Split(string value, char delim, int maxParts)
        {
            int end = value.Length;
            var res = new List<string>();

            int start = 0;
            int cpt = 1;
            for (int i = 0; i < end && cpt < maxParts; i++)
            {
                if (value[i] == delim)
                {
                    if (start == i)
                    {
                        res.Add(string.Empty);
                    }
                    else
                    {
                        res.Add(value.Substring(start, i));
                    }
                    start = i + 1;
                    cpt++;
                }
            }

            if (start == 0)
            {
                // If no delimiter was found in the value
                res.Add(value);
            }
            else
            {
                if (start != end)
                {
                    // Add the last element if it's not empty.
                    res.Add(value.Substring(start, end));
                }
                else
                {
                    // Truncate trailing empty elements.
                    for (int i = res.Count - 1; i >= 0; i--)
                    {
                        if (res[i] == "")
                        {
                            res.Remove(res[i]);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }

            return res.ToArray();
        }

        /// <summary>
        ///     Converts the specified byte value into a 2-digit hexadecimal integer.
        /// </summary>
        public static string ByteToHexStringPadded(int value) => Byte2HexPad[value & 0xff];

        //todo: port
        //    /**
        // * Converts the specified byte value into a 2-digit hexadecimal integer and appends it to the specified buffer.
        // */
        //public static <T extends Appendable> T byteToHexStringPadded(T buf, int value) {
        //    try {
        //        buf.append(byteToHexStringPadded(value));
        //    } catch (IOException e) {
        //        PlatformDependent.throwException(e);
        //    }
        //    return buf;
        //}

        /// <summary>
        ///     Converts the specified byte array into a hexadecimal value.
        /// </summary>
        public static string ToHexStringPadded(byte[] src) => ToHexStringPadded(src, 0, src.Length);

        /// <summary>
        ///     Converts the specified byte array into a hexadecimal value.
        /// </summary>
        public static string ToHexStringPadded(byte[] src, int offset, int length)
        {
            int end = offset + length;
            var sb = new StringBuilder(length << 1);
            for (int i = offset; i < end; i++)
            {
                sb.Append(ByteToHexStringPadded(src[i]));
            }
            return sb.ToString();
        }

        public static StringBuilder ToHexStringPadded(StringBuilder sb, byte[] src, int offset, int length)
        {
            Contract.Requires((offset + length) <= src.Length);
            int end = offset + length;
            for (int i = offset; i < end; i++)
            {
                sb.Append(ByteToHexStringPadded(src[i]));
            }
            return sb;
        }

        /// <summary>
        ///     Converts the specified byte value into a hexadecimal integer.
        /// </summary>
        public static string ByteToHexString(byte value) => Byte2HexNopad[value & 0xff];

        public static StringBuilder ByteToHexString(StringBuilder buf, byte value) => buf.Append(ByteToHexString(value));

        public static string ToHexString(byte[] src) => ToHexString(src, 0, src.Length);

        public static string ToHexString(byte[] src, int offset, int length) => ToHexString(new StringBuilder(length << 1), src, offset, length).ToString();

        public static StringBuilder ToHexString(StringBuilder dst, byte[] src) => ToHexString(dst, src, 0, src.Length);

        /// <summary>
        ///     Converts the specified byte array into a hexadecimal value and appends it to the specified buffer.
        /// </summary>
        public static StringBuilder ToHexString(StringBuilder dst, byte[] src, int offset, int length)
        {
            Debug.Assert(length >= 0);
            if (length == 0)
            {
                return dst;
            }
            int end = offset + length;
            int endMinusOne = end - 1;
            int i;
            // Skip preceding zeroes.
            for (i = offset; i < endMinusOne; i++)
            {
                if (src[i] != 0)
                {
                    break;
                }
            }

            ByteToHexString(dst, src[i++]);
            int remaining = end - i;
            ToHexStringPadded(dst, src, i, remaining);

            return dst;
        }

        /// <summary>
        ///     Escapes the specified value, if necessary according to
        ///     <a href="https://tools.ietf.org/html/rfc4180#section-2">RFC-4180</a>.
        /// </summary>
        /// <param name="value">
        ///     The value which will be escaped according to
        ///     <a href="https://tools.ietf.org/html/rfc4180#section-2">RFC-4180</a>
        /// </param>
        /// <param name="trimWhiteSpace">
        ///     The value will first be trimmed of its optional white-space characters, according to 
        ///     <a href= "https://tools.ietf.org/html/rfc7230#section-7" >RFC-7230</a>
        /// </param>
        /// <returns>the escaped value if necessary, or the value unchanged</returns>
        public static ICharSequence EscapeCsv(ICharSequence value, bool trimWhiteSpace = false)
        {
            int length = value.Count;
            if (length == 0)
            {
                return value;
            }

            int start = 0;
            int last = length - 1;
            bool trimmed = false;
            if (trimWhiteSpace)
            {
                start = IndexOfFirstNonOwsChar(value, length);
                if (start == length)
                {
                    return StringCharSequence.Empty;
                }

                last = IndexOfLastNonOwsChar(value, start, length);
                trimmed = start > 0 || last < length - 1;
                if (trimmed)
                {
                    length = last - start + 1;
                }
            }

            var result = new StringBuilderCharSequence(length + CsvNumberEscapeCharacters);
            bool quoted = IsDoubleQuote(value[start]) && IsDoubleQuote(value[last]) && length != 1;
            bool foundSpecialCharacter = false;
            bool escapedDoubleQuote = false;

            for (int i = start; i <= last; i++)
            {
                char current = value[i];
                switch (current)
                {
                    case DoubleQuote:
                        if (i == 0 || i == last)
                        {
                            if (!quoted)
                            {
                                result.Append(DoubleQuote);
                            }
                            else
                            {
                                continue;
                            }
                        }
                        else
                        {
                            bool isNextCharDoubleQuote = IsDoubleQuote(value[i + 1]);
                            if (!IsDoubleQuote(value[i - 1]) &&
                                (!isNextCharDoubleQuote || i + 1 == last))
                            {
                                result.Append(DoubleQuote);
                                escapedDoubleQuote = true;
                            }
                        }
                        break;
                    case LineFeed:
                    case CarriageReturn:
                    case Comma:
                        foundSpecialCharacter = true;
                        break;
                }
                result.Append(current);
            }

            if (escapedDoubleQuote || foundSpecialCharacter && !quoted)
            {
                return Quote(result);
            }
            if (trimmed)
            {
                return quoted ? Quote(result) : result;
            }

            return value;
        }

        static StringBuilderCharSequence Quote(StringBuilderCharSequence builder)
        {
            builder.Insert(0, DoubleQuote);
            builder.Append(DoubleQuote);

            return builder;
        }

        public static IList<ICharSequence> UnescapeCsvFields(ICharSequence value)
        {
            var unescaped = new List<ICharSequence>(2);
            StringBuilder current = InternalThreadLocalMap.Get().StringBuilder;
            bool quoted = false;
            int last = value.Count - 1;
            for (int i = 0; i <= last; i++)
            {
                char c = value[i];
                if (quoted)
                {
                    switch (c)
                    {
                        case DoubleQuote:
                            if (i == last)
                            {
                                // Add the last field and return
                                unescaped.Add((StringCharSequence)current.ToString());
                                return unescaped;
                            }
                            char next = value[++i];
                            if (next == DoubleQuote)
                            {
                                // 2 double-quotes should be unescaped to one
                                current.Append(DoubleQuote);
                            }
                            else if (next == Comma)
                            {
                                // This is the end of a field. Let's start to parse the next field.
                                quoted = false;
                                unescaped.Add((StringCharSequence)current.ToString());
                                current.Length = 0;
                            }
                            else
                            {
                                // double-quote followed by other character is invalid
                                throw new ArgumentException($"invalid escaped CSV field: {value} index: {i - 1}");
                            }
                            break;
                        default:
                            current.Append(c);
                            break;
                    }
                }
                else
                {
                    switch (c)
                    {
                        case Comma:
                            // Start to parse the next field
                            unescaped.Add((StringCharSequence)current.ToString());
                            current.Length = 0;
                            break;
                        case DoubleQuote:
                            if (current.Length == 0)
                            {
                                quoted = true;
                            }
                            else
                            {
                                // double-quote appears without being enclosed with double-quotes
                                current.Append(c);
                            }
                            break;
                        case LineFeed:
                        case CarriageReturn:
                            // special characters appears without being enclosed with double-quotes
                            throw new ArgumentException($"invalid escaped CSV field: {value} index: {i}");
                        default:
                            current.Append(c);
                            break;
                    }
                }
            }
            if (quoted)
            {
                throw new ArgumentException($"invalid escaped CSV field: {value} index: {last}");
            }

            unescaped.Add((StringCharSequence)current.ToString());
            return unescaped;
        }

        public static int IndexOfNonWhiteSpace(IReadOnlyList<char> seq, int offset)
        {
            for (; offset < seq.Count; ++offset)
            {
                if (!char.IsWhiteSpace(seq[offset]))
                {
                    return offset;
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSurrogate(char c) => c >= '\uD800' && c <= '\uDFFF';

        public static bool EndsWith(IReadOnlyList<char> s, char c)
        {
            int len = s.Count;
            return len > 0 && s[len - 1] == c;
        }

        public static ICharSequence TrimOws(ICharSequence value)
        {
            int length = value.Count;
            if (length == 0)
            {
                return value;
            }

            int start = IndexOfFirstNonOwsChar(value, length);
            int end = IndexOfLastNonOwsChar(value, start, length);
            return start == 0 && end == length - 1 ? value : value.SubSequence(start, end + 1);
        }

        internal static int IndexOf(IReadOnlyList<char> value, char ch, int start)
        {
            Contract.Requires(start >= 0 && start < value.Count);

            char upper = char.ToUpper(ch);
            char lower = char.ToLower(ch);
            int i = start;
            while (i < value.Count)
            {
                char c1 = value[i];
                if (c1 == ch
                    && char.ToUpper(c1).CompareTo(upper) != 0
                    && char.ToLower(c1).CompareTo(lower) != 0)
                {
                    return i;
                }

                i++;
            }

            return -1;
        }

        static int IndexOfFirstNonOwsChar(IReadOnlyList<char> value, int length)
        {
            int i = 0;
            while (i < length && IsOws(value[i]))
            {
                i++;
            }

            return i;
        }

        static int IndexOfLastNonOwsChar(IReadOnlyList<char> value, int start, int length)
        {
            int i = length - 1;
            while (i > start && IsOws(value[i]))
            {
                i--;
            }

            return i;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool IsOws(char c) => c == Space || c == Tab;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool IsDoubleQuote(char c) => c == DoubleQuote;

        /// <summary>
        ///     The shortcut to <see cref="SimpleClassName(Type)">SimpleClassName(o.GetType())</see>.
        /// </summary>
        public static string SimpleClassName(object o) => o?.GetType().Name ?? "null_object";

        /// <summary>
        ///     The shortcut to <see cref="SimpleClassName(Type)">SimpleClassName(o.GetType())</see>.
        /// </summary>
        public static string SimpleClassName<T>() => typeof(T).Name;

        /// <summary>
        ///     Generates a simplified name from a <see cref="Type" />.  Similar to {@link Class#getSimpleName()}, but it works
        ///     fine
        ///     with anonymous classes.
        /// </summary>
        public static string SimpleClassName(Type type) => type.Name;
    }
}