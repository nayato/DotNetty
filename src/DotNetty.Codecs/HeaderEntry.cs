// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs
{
    using System.Diagnostics.Contracts;

    public sealed class HeaderEntry<TKey, TValue>
    {
        readonly int hash;

        public HeaderEntry(int hash, TKey key)
        {
            this.hash = hash;
            this.Key = key;
        }

        internal HeaderEntry()
        {
            this.hash = -1;
            this.Key = default(TKey);
            this.Before = this;
            this.After = this;
        }

        internal HeaderEntry(int hash, TKey key, TValue value,
            HeaderEntry<TKey, TValue> next, HeaderEntry<TKey, TValue> head)
        {
            this.hash = hash;
            this.Key = key;
            this.Value = value;
            this.Next = next;

            this.After = head;
            this.Before = head.Before;
            this.PointNeighborsToThis();
        }

        void PointNeighborsToThis()
        {
            this.Before.After = this;
            this.After.Before = this;
        }

        internal void Remove()
        {
            this.Before.After = this.After;
            this.After.Before = this.Before;
        }

        internal HeaderEntry<TKey, TValue> Before { get; set; }

        internal HeaderEntry<TKey, TValue> After { get; set; }

        internal HeaderEntry<TKey, TValue> Next { get; set; }

        public override int GetHashCode() => this.hash;

        public TKey Key { get; }

        public TValue Value { get; private set; }

        public TValue SetValue(TValue value)
        {
            Contract.Requires(value != null);

            TValue oldValue = this.Value;
            this.Value = value;

            return oldValue;
        }

        public override string ToString() => this.hash == -1 ? "Empty" : $"{this.Key}={this.Value}";
    }
}
