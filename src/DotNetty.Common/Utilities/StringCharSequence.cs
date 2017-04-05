// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Common.Utilities
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;

    public sealed class StringCharSequence : ICharSequence, IEquatable<StringCharSequence>
    {
        public static readonly StringCharSequence Empty = new StringCharSequence(string.Empty);

        readonly string value;
        readonly int offset;

        public StringCharSequence(string value)
        {
            Contract.Requires(value != null);

            this.value = value;
            this.offset = 0;
            this.Count = this.value.Length;
        }

        public StringCharSequence(string value, int offset, int count)
        {
            Contract.Requires(value != null);
            Contract.Requires(offset >= 0 && count >= 0);
            Contract.Requires(offset <= value.Length - count);

            this.value = value;
            this.offset = offset;
            this.Count = count;
        }

        public int Count { get; }

        public static explicit operator string(StringCharSequence charSequence)
        {
            Contract.Requires(charSequence != null);
            return charSequence.ToString();
        }

        public static explicit operator StringCharSequence(string value)
        {
            Contract.Requires(value != null);

            return value.Length > 0 ? new StringCharSequence(value) : Empty;
        }

        public ICharSequence SubSequence(int start) => this.SubSequence(start, this.Count);

        public ICharSequence SubSequence(int start, int end)
        {
            Contract.Requires(start >= 0 && end >= start);
            Contract.Requires(end <= this.Count);

            return end == start
                ? Empty 
                : new StringCharSequence(this.value, this.offset + start, end - start);
        }

        public char this[int index]
        {
            get
            {
                Contract.Requires(index >= 0 && index < this.Count);
                return this.value[this.offset + index];
            }
        } 

        public bool RegionMatches(bool ignoreCase, int thisStart, ICharSequence seq, int start, int length) =>
            CharUtil.RegionMatches(this, ignoreCase, thisStart, seq, start, length);

        public bool RegionMatches(int thisStart, ICharSequence seq, int start, int length) =>
            CharUtil.RegionMatches(this, thisStart, seq, start, length);

        public int IndexOf(char ch, int start = 0)
        {
            Contract.Requires(start >= 0 && start < this.Count);

            int index = this.value.IndexOf(ch, this.offset + start);
            return index < 0 ? index : index - this.offset;
        }

        public int IndexOf(string target, int start = 0) => this.value.IndexOf(target, StringComparison.Ordinal);

        public string ToString(int start)
        {
            Contract.Requires(start >= 0 && start < this.Count);

            return this.value.Substring(this.offset + start, this.Count);
        }

        public override string ToString() => this.Count == 0 ? string.Empty : this.ToString(0);

        public bool Equals(StringCharSequence other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            return ReferenceEquals(this, other) 
                || this.SequenceEquals(other, false);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, null))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            var other = obj as StringCharSequence;
            if (other != null)
            {
                return this.Equals(other);
            }

            var sequence = obj as ICharSequence;
            return sequence != null
                && this.SequenceEquals(sequence, false);
        }

        public int HashCode(bool ignoreCase) => ignoreCase 
            ? StringComparer.OrdinalIgnoreCase.GetHashCode(this.ToString()) 
            : StringComparer.Ordinal.GetHashCode(this.ToString());

        public override int GetHashCode() => this.HashCode(false);

        public bool SequenceEquals(ICharSequence other, bool ignoreCase) =>
            CharUtil.SequenceEquals(this, other, ignoreCase);

        public IEnumerator<char> GetEnumerator() => new CharSequenceEnumerator(this);

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}
