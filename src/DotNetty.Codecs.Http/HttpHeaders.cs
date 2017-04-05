// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using DotNetty.Common.Utilities;

    public abstract class HttpHeaders : IEnumerable<HeaderEntry<ICharSequence, ICharSequence>>
    {
        public abstract ICharSequence Get(ICharSequence name);

        public ICharSequence Get(ICharSequence name, ICharSequence defaultValue) => this.Get(name) ?? defaultValue;

        public abstract int? GetInt(ICharSequence name);

        public abstract int GetInt(ICharSequence name, int defaultValue);

        public abstract short? GetShort(ICharSequence name);

        public abstract short GetShort(ICharSequence name, short defaultValue);

        public abstract long? GetTimeMillis(ICharSequence name);

        public abstract long GetTimeMillis(ICharSequence name, long defaultValue);

        public abstract IList<ICharSequence> GetAll(ICharSequence name);

        public abstract bool Contains(ICharSequence name);

        public abstract bool IsEmpty { get; }

        public abstract int Size { get; }

        public abstract ISet<ICharSequence> Names();

        public abstract HttpHeaders Add(ICharSequence name, object value);

        public HttpHeaders Add(ICharSequence name, IEnumerable<object> values)
        {
            Contract.Requires(values != null);

            foreach (object value in values)
            {
                this.Add(name, value);
            }

            return this;
        }

        public virtual HttpHeaders Add(HttpHeaders headers)
        {
            Contract.Requires(headers != null);

            foreach (HeaderEntry<ICharSequence, ICharSequence> pair in headers)
            {
                this.Add(pair.Key, pair.Value);
            }

            return this;
        }

        public abstract HttpHeaders AddInt(ICharSequence name, int value);

        public abstract HttpHeaders AddShort(ICharSequence name, short value);

        public abstract HttpHeaders Set(ICharSequence name, object value);

        public abstract HttpHeaders Set(ICharSequence name, IEnumerable<object> values);

        public virtual HttpHeaders Set(HttpHeaders headers)
        {
            Contract.Requires(headers != null);

            this.Clear();

            if (headers.IsEmpty)
            {
                return this;
            }

            foreach(HeaderEntry<ICharSequence, ICharSequence> pair in headers)
            {
                this.Add(pair.Key, pair.Value);
            }

            return this;
        }

        public HttpHeaders SetAll(HttpHeaders headers)
        {
            Contract.Requires(headers != null);

            if (headers.IsEmpty)
            {
                return this;
            }

            foreach (HeaderEntry<ICharSequence, ICharSequence> pair in headers)
            {
                this.Add(pair.Key, pair.Value);
            }

            return this;
        }

        public abstract HttpHeaders SetInt(ICharSequence name, int value);

        public abstract HttpHeaders SetShort(ICharSequence name, short value);

        public abstract HttpHeaders Remove(ICharSequence name);

        public abstract HttpHeaders Clear();

        public virtual bool Contains(ICharSequence name, ICharSequence value, bool ignoreCase)
        {
            IList<ICharSequence> values = this.GetAll(name);
            if (values.Count == 0)
            {
                return false;
            }

            foreach (ICharSequence v in values)
            {
                if (v.SequenceEquals(value, ignoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public virtual bool ContainsValue(ICharSequence name, ICharSequence value, bool ignoreCase)
        {
            IList<ICharSequence> values = this.GetAll(name);
            if (values.Count == 0)
            {
                return false;
            }

            foreach (ICharSequence v in values)
            {
                if (ContainsSequence(v, value, ignoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        static bool ContainsSequence(ICharSequence value, ICharSequence expected, bool ignoreCase)
        {
            ICharSequence[] parts = CharUtil.Split(value, ',');

            if (ignoreCase)
            {
                foreach (ICharSequence s in parts)
                {
                    if (AsciiString.ContentEqualsIgnoreCase(expected, StringUtil.TrimOws(s)))
                    {
                        return true;
                    }
                }
            }
            else
            {
                foreach (ICharSequence s in parts)
                {
                    if (AsciiString.ContentEquals(expected, StringUtil.TrimOws(s)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public string GetAsString(ICharSequence name) => this.Get(name).ToString();

        public IList<string> GetAllAsString(ICharSequence name)
        {
            var values = new List<string>();
            IList<ICharSequence> list = this.GetAll(name);
            foreach (ICharSequence value in list)
            {
                values.Add(value.ToString());
            }

            return values;
        }

        public abstract IEnumerator<HeaderEntry<ICharSequence, ICharSequence>> GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}
