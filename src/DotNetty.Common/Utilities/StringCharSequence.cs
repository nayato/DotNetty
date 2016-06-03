// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Common.Utilities
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Text;

    public sealed class StringCharSequence : CharSequence
    {
        public readonly string Value;

        public static readonly StringCharSequence Empty = new StringCharSequence(string.Empty);

        public StringCharSequence(string value)
        {
            this.Value = value;
        }

        public IEnumerator<char> GetEnumerator() => this.Value.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        public int Count => this.Value.Length;

        public char this[int index] => this.Value[index];

        public int Length => this.Value.Length;

        public void CopyTo(char[] array) => this.Value.CopyTo(0, array, 0, this.Length);

        public int Encode(Encoding encoding, int startIndex, int length, byte[] destination, int destinationStartIndex)
            => encoding.GetBytes(this.Value, startIndex, length, destination, destinationStartIndex);

        public bool IsEmpty => string.IsNullOrEmpty(this.Value);

        public override string ToString() => this.Value;

        public override int GetHashCode() => this.Value.GetHashCode();

        //public static implicit operator StringCharSequence(string value) => new StringCharSequence(value);
    }
}