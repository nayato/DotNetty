// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics.Contracts;
    using System.Text;
    using DotNetty.Common.Utilities;

    public class DefaultHeaders<TKey, TValue> : IHeaders<TKey, TValue>
    {
        const int HashCodeSeed = unchecked((int)0xc2b2ae35);
        static readonly DefaultHashingStrategy<TValue> DefaultValueHashingStrategy = new DefaultHashingStrategy<TValue>();
        static readonly DefaultHashingStrategy<TKey> DefaultKeyHashingStragety = new DefaultHashingStrategy<TKey>();
        static readonly NullNameValidator<TKey> DefaultKeyNameValidator = new NullNameValidator<TKey>();

        readonly HeaderEntry<TKey, TValue>[] entries;
        readonly HeaderEntry<TKey, TValue> head;

        readonly byte hashMask;
        protected readonly IValueConverter<TValue> ValueConverter;
        readonly INameValidator<TKey> nameValidator;
        readonly IHashingStrategy<TKey> hashingStrategy;

        public DefaultHeaders(IValueConverter<TValue> valueConverter)
            : this(DefaultKeyHashingStragety, valueConverter, DefaultKeyNameValidator, 16)
        {
        }

        public DefaultHeaders(IValueConverter<TValue> valueConverter, INameValidator<TKey> nameValidator)
            : this(DefaultKeyHashingStragety, valueConverter, nameValidator, 16)
        {
        }

        public DefaultHeaders(IHashingStrategy<TKey> nameHashingStrategy, IValueConverter<TValue> valueConverter, INameValidator<TKey> nameValidator) 
            : this(nameHashingStrategy, valueConverter, nameValidator, 16)
        {
        }

        public DefaultHeaders(IHashingStrategy<TKey> nameHashingStrategy,
            IValueConverter<TValue> valueConverter, INameValidator<TKey> nameValidator, int arraySizeHint)
        {
            Contract.Requires(nameHashingStrategy != null);
            Contract.Requires(valueConverter != null);
            Contract.Requires(nameValidator != null);

            this.hashingStrategy = nameHashingStrategy;
            this.ValueConverter = valueConverter;
            this.nameValidator = nameValidator;

            // Enforce a bound of [2, 128] because hashMask is a byte. The max possible value of hashMask is one less
            // than the length of this array, and we want the mask to be > 0.
            this.entries = new HeaderEntry<TKey, TValue>[
                MathUtil.FindNextPositivePowerOfTwo(Math.Max(2, Math.Min(arraySizeHint, 128)))];
            this.hashMask = (byte)(this.entries.Length - 1);
            this.head = new HeaderEntry<TKey, TValue>();
        }

        public TValue Get(TKey name)
        {
            Contract.Requires(name != null);

            int h = this.hashingStrategy.GetHashCode(name);
            int i = this.IndexOf(h);
            HeaderEntry<TKey, TValue> e = this.entries[i];
            TValue value = default(TValue);

            // loop until the first header was found
            while (e != null)
            {
                if (e.GetHashCode() == h
                    && this.hashingStrategy.Equals(name, e.Key))
                {
                    value = e.Value;
                }

                e = e.Next;
            }

            return value;
        }

        public TValue Get(TKey name, TValue defaultValue)
        {
            TValue value = this.Get(name);
            return ReferenceEquals(value, default(TValue)) ? defaultValue : value;
        }

        public TValue GetAndRemove(TKey name)
        {
            Contract.Requires(name != null);

            int h = this.hashingStrategy.GetHashCode(name);
            return this.Remove0(h, this.IndexOf(h), name);
        }

        public TValue GetAndRemove(TKey name, TValue defaultValue)
        {
            TValue value = this.GetAndRemove(name);
            return ReferenceEquals(value, default(TValue)) ? defaultValue : value;
        }

        public virtual IList<TValue> GetAll(TKey name)
        {
            Contract.Requires(name != null);

            var values = new List<TValue>();
            int h = this.hashingStrategy.GetHashCode(name);
            int i = this.IndexOf(h);

            HeaderEntry<TKey, TValue> e = this.entries[i];
            while (e != null)
            {
                if (e.GetHashCode() == h
                    && this.hashingStrategy.Equals(name, e.Key))
                {
                    values.Insert(0, e.Value);
                }

                e = e.Next;
            }

            return values;
        }

        public IList<TValue> GetAllAndRemove(TKey name)
        {
            IList<TValue> all = this.GetAll(name);
            this.Remove(name);

            return all;
        }
        public bool Contains(TKey name) => this.Get(name) != null;

        public bool ContainsObject(TKey name, object value)
        {
            Contract.Requires(value != null);
            TValue v = this.ValueConverter.ConvertObject(value);
            if (v == null)
            {
                throw new ArgumentException("Converted value cannot be null", nameof(value));
            }

            return this.Contains(name, v);
        }

        public bool ContainsBoolean(TKey name, bool value) => 
            this.Contains(name, this.ValueConverter.ConvertBoolean(value));

        public bool ContainsByte(TKey name, byte value) =>
            this.Contains(name, this.ValueConverter.ConvertByte(value));

        public bool ContainsChar(TKey name, char value) =>
            this.Contains(name, this.ValueConverter.ConvertChar(value));

        public bool ContainsShort(TKey name, short value) =>
            this.Contains(name, this.ValueConverter.ConvertShort(value));

        public bool ContainsInt(TKey name, int value) =>
            this.Contains(name, this.ValueConverter.ConvertInt(value));

        public bool ContainsLong(TKey name, long value) =>
            this.Contains(name, this.ValueConverter.ConvertLong(value));

        public bool ContainsFloat(TKey name, float value) =>
            this.Contains(name, this.ValueConverter.ConvertFloat(value));

        public bool ContainsDouble(TKey name, double value) =>
            this.Contains(name, this.ValueConverter.ConvertDouble(value));

        public bool ContainsTimeMillis(TKey name, long value) =>
            this.Contains(name, this.ValueConverter.ConvertTimeMillis(value));

        public bool Contains(TKey name, TValue value) =>
            this.Contains(name, value, DefaultValueHashingStrategy);

        public bool Contains(TKey name, TValue value, IHashingStrategy<TValue> valueHashingStrategy)
        {
            Contract.Requires(name != null);

            int h = this.hashingStrategy.GetHashCode(name);
            int i = this.IndexOf(h);
            HeaderEntry<TKey, TValue> e = this.entries[i];
            while (e != null)
            {
                if (e.GetHashCode() == h
                    && this.hashingStrategy.Equals(name, e.Key)
                    && valueHashingStrategy.Equals(value, e.Value))
                {
                    return true;
                }

                e = e.Next;
            }

            return false;
        }

        public int Size { get; private set; }

        public bool IsEmpty => this.head == this.head.After;

        public ISet<TKey> Names()
        {
            if (this.IsEmpty)
            {
                return ImmutableHashSet<TKey>.Empty;
            }

            var keys = new HashSet<TKey>(this.hashingStrategy);
            HeaderEntry<TKey, TValue> e = this.head.After;
            while (e != this.head)
            {
                keys.Add(e.Key);
                e = e.After;
            }

            return keys;
        }

        public virtual IHeaders<TKey, TValue> Add(TKey name, TValue value)
        {
            Contract.Requires(value != null);

            this.nameValidator.ValidateName(name);
            int h = this.hashingStrategy.GetHashCode(name);
            int i = this.IndexOf(h);
            this.Add0(h, i, name, value);

            return this;
        }

        public virtual IHeaders<TKey, TValue> Add(TKey name, IEnumerable<TValue> values)
        {
            this.nameValidator.ValidateName(name);
            int h = this.hashingStrategy.GetHashCode(name);
            int i = this.IndexOf(h);
            foreach (TValue v in values)
            {
                this.Add0(h, i, name, v);
            }

            return this;
        }

        public virtual IHeaders<TKey, TValue> AddObject(TKey name, object value)
        {
            Contract.Requires(value != null);
            TValue v = this.ValueConverter.ConvertObject(value);
            if (v == null)
            {
                throw new ArgumentException("Converted value cannot be null", nameof(value));
            }

            return this.Add(name, v);
        }

        public virtual IHeaders<TKey, TValue> AddObject(TKey name, IEnumerable<object> values)
        {
            foreach (object value in values)
            {
                this.AddObject(name, value);
            }

            return this;
        }

        public virtual IHeaders<TKey, TValue> AddObject(TKey name, params object[] values)
        {
            // ReSharper disable once ForCanBeConvertedToForeach
            // Avoid enumerator allocations
            for (int i = 0; i < values.Length; i++)
            {
                this.AddObject(name, values[i]);
            }

            return this;
        }

        public IHeaders<TKey, TValue> AddBoolean(TKey name, bool value) =>
            this.Add(name, this.ValueConverter.ConvertBoolean(value));

        public IHeaders<TKey, TValue> AddByte(TKey name, byte value) =>
            this.Add(name, this.ValueConverter.ConvertByte(value));

        public IHeaders<TKey, TValue> AddChar(TKey name, char value) =>
            this.Add(name, this.ValueConverter.ConvertChar(value));

        public IHeaders<TKey, TValue> AddShort(TKey name, short value) =>
            this.Add(name, this.ValueConverter.ConvertShort(value));

        public IHeaders<TKey, TValue> AddInt(TKey name, int value) =>
            this.Add(name, this.ValueConverter.ConvertInt(value));

        public IHeaders<TKey, TValue> AddLong(TKey name, long value) =>
            this.Add(name, this.ValueConverter.ConvertLong(value));

        public IHeaders<TKey, TValue> AddFloat(TKey name, float value) =>
            this.Add(name, this.ValueConverter.ConvertFloat(value));

        public IHeaders<TKey, TValue> AddDouble(TKey name, double value) =>
            this.Add(name, this.ValueConverter.ConvertDouble(value));

        public IHeaders<TKey, TValue> AddTimeMillis(TKey name, long value) =>
            this.Add(name, this.ValueConverter.ConvertTimeMillis(value));

        public virtual IHeaders<TKey, TValue> Add(IHeaders<TKey, TValue> headers)
        {
            if (ReferenceEquals(headers, this))
            {
                throw new ArgumentException("can't add to itself.");
            }

            this.AddImpl(headers);
            return this;
        }

        protected void AddImpl(IHeaders<TKey, TValue> headers)
        {
            var defaultHeaders = headers as DefaultHeaders<TKey, TValue>;
            if (defaultHeaders != null)
            {
                HeaderEntry<TKey, TValue> e = defaultHeaders.head.After;

                if (defaultHeaders.hashingStrategy == this.hashingStrategy
                    && defaultHeaders.nameValidator == this.nameValidator)
                {
                    // Fastest copy
                    while (e != defaultHeaders.head)
                    {
                        int hash = e.GetHashCode();
                        this.Add0(hash, this.IndexOf(hash), e.Key, e.Value);
                        e = e.After;
                    }
                }
                else
                {
                    // Fast copy
                    while (e != defaultHeaders.head)
                    {
                        this.Add(e.Key, e.Value);
                        e = e.After;
                    }
                }
            }
            else
            {
                // Slow copy
                foreach (HeaderEntry<TKey, TValue> header in headers)
                {
                    this.Add(header.Key, header.Value);
                }
            }
        }

        public IHeaders<TKey, TValue> Set(TKey name, TValue value)
        {
            Contract.Requires(value != null);

            this.nameValidator.ValidateName(name);
            int h = this.hashingStrategy.GetHashCode(name);
            int i = this.IndexOf(h);
            this.Remove0(h, i, name);
            this.Add0(h, i, name, value);

            return this;
        }

        public virtual IHeaders<TKey, TValue> Set(TKey name, IEnumerable<TValue> values)
        {
            Contract.Requires(values != null);

            this.nameValidator.ValidateName(name);
            int h = this.hashingStrategy.GetHashCode(name);
            int i = this.IndexOf(h);

            this.Remove0(h, i, name);
            foreach (TValue v in values)
            {
                if (ReferenceEquals(v, null))
                {
                    break;
                }

                this.Add0(h, i, name, v);
            }

            return this;
        }

        public virtual IHeaders<TKey, TValue> SetObject(TKey name, object value)
        {
            Contract.Requires(value != null);

            TValue convertedValue = this.ValueConverter.ConvertObject(value);
            if (convertedValue == null)
            {
                throw new ArgumentException("Converted value cannot be null", nameof(value));
            }

            return this.Set(name, convertedValue);
        }

        public virtual IHeaders<TKey, TValue> SetObject(TKey name, IEnumerable<object> values)
        {
            Contract.Requires(values != null);

            this.nameValidator.ValidateName(name);
            int h = this.hashingStrategy.GetHashCode(name);
            int i = this.IndexOf(h);

            this.Remove0(h, i, name);
            foreach (object v in values)
            {
                if (ReferenceEquals(v, null))
                {
                    break;
                }
                this.Add0(h, i, name, this.ValueConverter.ConvertObject(v));
            }

            return this;
        }

        public IHeaders<TKey, TValue> SetBoolean(TKey name, bool value) =>
            this.Set(name, this.ValueConverter.ConvertBoolean(value));

        public IHeaders<TKey, TValue> SetByte(TKey name, byte value) =>
            this.Set(name, this.ValueConverter.ConvertByte(value));

        public IHeaders<TKey, TValue> SetChar(TKey name, char value) =>
            this.Set(name, this.ValueConverter.ConvertChar(value));

        public IHeaders<TKey, TValue> SetShort(TKey name, short value) =>
            this.Set(name, this.ValueConverter.ConvertShort(value));

        public IHeaders<TKey, TValue> SetInt(TKey name, int value) =>
            this.Set(name, this.ValueConverter.ConvertInt(value));

        public IHeaders<TKey, TValue> SetLong(TKey name, long value) =>
            this.Set(name, this.ValueConverter.ConvertLong(value));

        public IHeaders<TKey, TValue> SetFloat(TKey name, float value) =>
            this.Set(name, this.ValueConverter.ConvertFloat(value));

        public IHeaders<TKey, TValue> SetDouble(TKey name, double value) =>
            this.Set(name, this.ValueConverter.ConvertDouble(value));

        public IHeaders<TKey, TValue> SetTimeMillis(TKey name, long value) =>
            this.Set(name, this.ValueConverter.ConvertTimeMillis(value));

        public virtual IHeaders<TKey, TValue> Set(IHeaders<TKey, TValue> headers)
        {
            if (!ReferenceEquals(headers, this))
            {
                this.Clear();
                this.AddImpl(headers);
            }

            return this;
        }

        public virtual IHeaders<TKey, TValue> SetAll(IHeaders<TKey, TValue> headers)
        {
            if (!ReferenceEquals(headers, this))
            {
                foreach (TKey key in headers.Names())
                {
                    this.Remove(key);
                }

                this.AddImpl(headers);
            }

            return this;
        }

        public bool Remove(TKey name) => this.GetAndRemove(name) != null;

        public IHeaders<TKey, TValue> Clear()
        {
            this.entries.Fill(null);
            this.head.Before = this.head.After = this.head;
            this.Size = 0;

            return this;
        }

        public IEnumerator<HeaderEntry<TKey, TValue>> GetEnumerator()
        {
            HeaderEntry<TKey, TValue> current = this.head.After;
            while (current != this.head)
            {
                yield return current;
                current = current.After;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        public bool? GetBoolean(TKey name)
        {
            TValue v = this.Get(name);
            return v != null ? this.ValueConverter.ConvertToBoolean(v) : default(bool?);
        }

        public bool GetBoolean(TKey name, bool defaultValue) => this.GetBoolean(name) ?? defaultValue;

        public byte? GetByte(TKey name)
        {
            TValue v = this.Get(name);
            return v != null ? this.ValueConverter.ConvertToByte(v) : default(byte?);
        }

        public byte GetByte(TKey name, byte defaultValue) => this.GetByte(name) ?? defaultValue;

        public char? GetChar(TKey name)
        {
            TValue v = this.Get(name);
            return v != null ? this.ValueConverter.ConvertToChar(v) : default(char?);
        }

        public char GetChar(TKey name, char defaultValue) => this.GetChar(name) ?? defaultValue;

        public short? GetShort(TKey name)
        {
            TValue v = this.Get(name);
            return v != null ? this.ValueConverter.ConvertToShort(v) : default(short?);
        }

        public short GetShort(TKey name, short defaultValue) => this.GetShort(name) ?? defaultValue;

        public int? GetInt(TKey name)
        {
            TValue v = this.Get(name);
            return v != null ? this.ValueConverter.ConvertToInt(v) : default(int?);
        }

        public int GetInt(TKey name, int defaultValue) => this.GetInt(name) ?? defaultValue;

        public long? GetLong(TKey name)
        {
            TValue v = this.Get(name);
            return v != null ? this.ValueConverter.ConvertToLong(v) : default(long?);
        }

        public long GetLong(TKey name, long defaultValue) => this.GetLong(name) ?? defaultValue;

        public float? GetFloat(TKey name)
        {
            TValue v = this.Get(name);
            return v != null ? this.ValueConverter.ConvertToFloat(v) : default(float?);
        }

        public float GetFloat(TKey name, float defaultValue) => this.GetFloat(name) ?? defaultValue;

        public double? GetDouble(TKey name)
        {
            TValue v = this.Get(name);
            return v != null ? this.ValueConverter.ConvertToDouble(v) : default(double?);
        }

        public double GetDouble(TKey name, double defaultValue) => this.GetDouble(name) ?? defaultValue;

        public long? GetTimeMillis(TKey name)
        {
            TValue v = this.Get(name);
            return v != null ? this.ValueConverter.ConvertToTimeMillis(v) : default(long?);
        }

        public long GetTimeMillis(TKey name, long defaultValue) => this.GetTimeMillis(name) ?? defaultValue;

        public bool? GetBooleanAndRemove(TKey name)
        {
            TValue v = this.GetAndRemove(name);
            return v != null ? this.ValueConverter.ConvertToBoolean(v) : default(bool?);
        }

        public bool GetBooleanAndRemove(TKey name, bool defaultValue) => this.GetBooleanAndRemove(name) ?? defaultValue;

        public byte? GetByteAndRemove(TKey name)
        {
            TValue v = this.GetAndRemove(name);
            return v != null ? this.ValueConverter.ConvertToByte(v) : default(byte?);
        }

        public byte GetByteAndRemove(TKey name, byte defaultValue) => this.GetByteAndRemove(name) ?? defaultValue;

        public char? GetCharAndRemove(TKey name)
        {
            TValue v = this.GetAndRemove(name);
            return v != null ? this.ValueConverter.ConvertToChar(v) : default(char?);
        }

        public char GetCharAndRemove(TKey name, char defaultValue) => this.GetCharAndRemove(name) ?? defaultValue;

        public short? GetShortAndRemove(TKey name)
        {
            TValue v = this.GetAndRemove(name);
            return v != null ? this.ValueConverter.ConvertToShort(v) : default(short?);
        }

        public short GetShortAndRemove(TKey name, short defaultValue) => this.GetShortAndRemove(name) ?? defaultValue;

        public int? GetIntAndRemove(TKey name)
        {
            TValue v = this.GetAndRemove(name);
            return v != null ? this.ValueConverter.ConvertToInt(v) : default(int?);
        }

        public int GetIntAndRemove(TKey name, int defaultValue) => this.GetIntAndRemove(name) ?? defaultValue;

        public long? GetLongAndRemove(TKey name)
        {
            TValue v = this.GetAndRemove(name);
            return v != null ? this.ValueConverter.ConvertToLong(v) : default(long?);
        }

        public long GetLongAndRemove(TKey name, long defaultValue) => this.GetLongAndRemove(name) ?? defaultValue;

        public float? GetFloatAndRemove(TKey name)
        {
            TValue v = this.GetAndRemove(name);
            return v != null ? this.ValueConverter.ConvertToFloat(v) : default(float?);
        }

        public float GetFloatAndRemove(TKey name, float defaultValue) => this.GetFloatAndRemove(name) ?? defaultValue;

        public double? GetDoubleAndRemove(TKey name)
        {
            TValue v = this.GetAndRemove(name);
            return v != null ? this.ValueConverter.ConvertToDouble(v) : default(double?);
        }

        public double GetDoubleAndRemove(TKey name, double defaultValue) => this.GetDoubleAndRemove(name) ?? defaultValue;

        public long? GetTimeMillisAndRemove(TKey name)
        {
            TValue v = this.GetAndRemove(name);
            return v != null ? this.ValueConverter.ConvertToTimeMillis(v) : default(long?);
        }

        public long GetTimeMillisAndRemove(TKey name, long defaultValue) => this.GetTimeMillisAndRemove(name) ?? defaultValue;

        public override bool Equals(object obj)
        {
            var headers = obj as IHeaders<TKey, TValue>;

            return headers != null 
                && this.Equals(headers, DefaultValueHashingStrategy);
        }

        public override int GetHashCode() => this.HashCode(DefaultValueHashingStrategy);

        public bool Equals(IHeaders<TKey, TValue> h2, IHashingStrategy<TValue> valueHashingStrategy)
        {
            if (h2.Size != this.Size)
            {
                return false;
            }

            if (ReferenceEquals(this, h2))
            {
                return true;
            }

            foreach (TKey name in this.Names())
            {
                IList<TValue> otherValues = h2.GetAll(name);
                IList<TValue> values = this.GetAll(name);
                if (otherValues.Count != values.Count)
                {
                    return false;
                }
                for (int i = 0; i < otherValues.Count; i++)
                {
                    if (!valueHashingStrategy.Equals(otherValues[i], values[i]))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public int HashCode(IHashingStrategy<TValue> valueHashingStrategy)
        {
            int result = HashCodeSeed;
            foreach (TKey name in this.Names())
            {
                result = 31 * result + this.hashingStrategy.GetHashCode(name);
                IList<TValue> values = this.GetAll(name);
                // ReSharper disable once ForCanBeConvertedToForeach
                // Avoid enumerator allocation
                for (int i = 0; i < values.Count; ++i)
                {
                    result = 31 * result + valueHashingStrategy.GetHashCode(values[i]);
                }
            }

            return result;
        }

        public override string ToString()
        {
            var builder = new StringBuilder(StringUtil.SimpleClassName(this.GetType()));

            builder.Append('[');
            string separator = "";
            foreach (TKey name in this.Names())
            {
                IList<TValue> values = this.GetAll(name);
                foreach (TValue t in values)
                {
                    builder.Append(separator);
                    builder.Append(name).Append(": ").Append(t);
                    separator = ", ";
                }
            }
            builder.Append(']');

            return builder.ToString();
        }

        protected HeaderEntry<TKey, TValue> NewHeaderEntry(int h, TKey name, TValue value, HeaderEntry<TKey, TValue> next) =>
            new HeaderEntry<TKey, TValue>(h, name, value, next, this.head);

        int IndexOf(int hash) => hash & this.hashMask;

        void Add0(int h, int i, TKey name, TValue value)
        {
            // Update the hash table.
            this.entries[i] = this.NewHeaderEntry(h, name, value, this.entries[i]);
            ++this.Size;
        }

        TValue Remove0(int h, int i, TKey name)
        {
            HeaderEntry<TKey, TValue> e = this.entries[i];
            if (e == null)
            {
                return default(TValue);
            }

            TValue value = default(TValue);
            HeaderEntry<TKey, TValue> next = e.Next;
            while (next != null)
            {
                if (next.GetHashCode() == h
                    && this.hashingStrategy.Equals(name, next.Key))
                {
                    value = next.Value;
                    e.Next = next.Next;
                    next.Remove();

                    --this.Size;
                }
                else
                {
                    e = next;
                }

                next = e.Next;
            }

            e = this.entries[i];
            if (e.GetHashCode() == h
                && this.hashingStrategy.Equals(name, e.Key))
            {
                if (value == null)
                {
                    value = e.Value;
                }

                this.entries[i] = e.Next;
                e.Remove();

                --this.Size;
            }

            return value;
        }
    }
}
