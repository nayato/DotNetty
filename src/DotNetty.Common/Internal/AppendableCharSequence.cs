// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Common.Internal
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using DotNetty.Common.Utilities;

    public sealed class AppendableCharSequence : ICharSequence, IAppendable, IEquatable<AppendableCharSequence>
    {
        char[] chars;
        int pos;

        public AppendableCharSequence(int length)
        {
            Contract.Requires(length > 0);

            this.chars = new char[length];
        }

        public AppendableCharSequence(char[] chars)
        {
            Contract.Requires(chars.Length > 0);

            this.chars = chars;
            this.pos = chars.Length;
        }

        public IEnumerator<char> GetEnumerator() => new CharSequenceEnumerator(this);

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        public int Count => this.pos;

        public char this[int index]
        {
            get
            {
                Contract.Requires(index <= this.pos);
                return this.chars[index];
            }
        }

        public char[] CharArray => this.chars;

        public ICharSequence SubSequence(int start) => this.SubSequence(start, this.Count);

        public ICharSequence SubSequence(int start, int end) => this.SubStringUnsafe(start, end);

        public int IndexOf(char ch, int start = 0) => StringUtil.IndexOf(this.chars, ch, start);

        public bool RegionMatches(bool ignoreCase, int thisStart, ICharSequence seq, int start, int length) =>
            CharUtil.RegionMatches(this, ignoreCase, thisStart, seq, start, length);

        public bool RegionMatches(int thisStart, ICharSequence seq, int start, int length) =>
            CharUtil.RegionMatches(this, thisStart, seq, start, length);

        public bool Equals(AppendableCharSequence other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }
            if (ReferenceEquals(this, other) 
                || ReferenceEquals(this.chars, other.chars) && this.pos == other.pos)
            {
                return true;
            }

            return this.SequenceEquals(other, false);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, null))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            var other = obj as AppendableCharSequence;
            if (other != null)
            {
                return this.Equals(other);
            }

            var sequence = obj as ICharSequence;
            if (sequence != null)
            {
                return this.SequenceEquals(sequence, false);
            }

            return false;
        }

        public int HashCode(bool ignoreCase) => ignoreCase
            ? StringComparer.OrdinalIgnoreCase.GetHashCode(this.ToString())
            : StringComparer.Ordinal.GetHashCode(this.ToString());

        public override int GetHashCode() => this.HashCode(true);

        public bool SequenceEquals(ICharSequence other, bool ignoreCase) =>
            CharUtil.SequenceEquals(this, other, ignoreCase);

        public IAppendable Append(char c)
        {
            if (this.pos == this.chars.Length)
            {
                char[] old = this.chars;
                this.chars = new char[old.Length << 1];
                Array.Copy(old, this.chars, old.Length);
            }
            this.chars[this.pos++] = c;

            return this;
        }

        public IAppendable Append(ICharSequence sequence) => this.Append(sequence, 0, sequence.Count);

        public IAppendable Append(ICharSequence sequence, int start, int end)
        {
            Contract.Requires(sequence.Count >= end);

            int length = end - start;
            if (length > this.chars.Length - this.pos)
            {
                this.chars = Expand(this.chars, this.pos + length, this.pos);
            }

            var seq = sequence as AppendableCharSequence;
            if (seq != null)
            {
                // Optimize append operations via array copy
                char[] src = seq.chars;
                Array.Copy(src, start, this.chars, this.pos, length);
                this.pos += length;

                return this;
            }

            for (int i = start; i < end; i++)
            {
                this.chars[this.pos++] = sequence[i];
            }

            return this;
        }

        /**
          * Reset the {@link AppendableCharSequence}. Be aware this will only reset the current internal position and not
          * shrink the internal char array.
          */
        public void Reset() => this.pos = 0;

        public string ToString(int start)
        {
            Contract.Requires(start >= 0 && start < this.Count);

            return new string(this.chars, start, this.pos);
        }

        public override string ToString() => this.Count == 0 ? string.Empty : this.ToString(0);

        /**
          * Create a new string from the given start to end.
          * This method is considered unsafe as index values are assumed to be legitimate.
          * Only underlying array bounds checking is done.
         */
        public AppendableCharSequence SubStringUnsafe(int start, int end)
        {
            int length = end - start;
            var data = new char[length];
            Array.Copy(this.chars, start, data, 0, length);

            return new AppendableCharSequence(data);
        }

        // Create a new ascii string, this method assumes all chars has been sanitized
        //  to ascii chars when appending to the array
        public AsciiString AsciiStringUnsafe(int start, int end) => new AsciiString(this.chars, start, end - start);

        static char[] Expand(char[] array, int neededSpace, int size)
        {
            int newCapacity = array.Length;
            do
            {
                // double capacity until it is big enough
                newCapacity <<= 1;

                if (newCapacity < 0)
                {
                    throw new InvalidOperationException($"New capacity {newCapacity} must be positive");
                }

            } while (neededSpace > newCapacity);

            var newArray = new char[newCapacity];
            Array.Copy(array, 0, newArray, 0, size);

            return newArray;
        }
    }
}
