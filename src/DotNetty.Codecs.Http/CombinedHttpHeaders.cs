// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using DotNetty.Common.Utilities;

    public class CombinedHttpHeaders : DefaultHttpHeaders
    {
        public CombinedHttpHeaders(bool validate = true) 
            : base(new CombinedHttpHeadersImpl(AsciiString.CaseSensitiveHasher, 
                NewValueConverter(validate), NewNameValidator(validate)))
        {
        }

        public override bool ContainsValue(ICharSequence name, ICharSequence value, bool ignoreCase) => 
            base.ContainsValue(name, StringUtil.TrimOws(value), ignoreCase);

        sealed class CombinedHttpHeadersImpl : DefaultHeaders<ICharSequence, ICharSequence>
        {
            // An estimate of the size of a header value.
            const int ValueLengthEstimate = 10;

            public CombinedHttpHeadersImpl(IHashingStrategy<ICharSequence> nameHashingStrategy, IValueConverter<ICharSequence> valueConverter, INameValidator<ICharSequence> nameValidator, int arraySizeHint = 16)
                : base(nameHashingStrategy, valueConverter, nameValidator, arraySizeHint)
            {
            }

            public override IList<ICharSequence> GetAll(ICharSequence name)
            {
                IList<ICharSequence> values = base.GetAll(name);
                if (values.Count == 0)
                {
                    return values;
                }
                if (values.Count != 1)
                {
                    throw new InvalidOperationException($"{nameof(CombinedHttpHeaders)} should only have one value");
                }

                return StringUtil.UnescapeCsvFields(values[0]);
            }

            public override IHeaders<ICharSequence, ICharSequence> Add(IHeaders<ICharSequence, ICharSequence> headers)
            {
                // Override the fast-copy mechanism used by DefaultHeaders
                if (ReferenceEquals(headers, this))
                {
                    throw new ArgumentException("can't add to itself.");
                }

                if (headers is CombinedHttpHeadersImpl)
                {
                    if (this.IsEmpty)
                    {
                        // Can use the fast underlying copy
                        this.AddImpl(headers);
                    }
                    else
                    {
                        // Values are already escaped so don't escape again
                        foreach (HeaderEntry<ICharSequence, ICharSequence> header in headers)
                        {
                            this.AddEscapedValue(header.Key, header.Value);
                        }
                    }
                }
                else
                {
                    foreach (HeaderEntry<ICharSequence, ICharSequence> header in headers)
                    {
                        this.Add(header.Key, header.Value);
                    }
                }

                return this;
            }

            public override IHeaders<ICharSequence, ICharSequence> Set(IHeaders<ICharSequence, ICharSequence> headers)
            {
                if (ReferenceEquals(headers, this))
                {
                    return this;
                }
                this.Clear();

                return this.Add(headers);
            }

            public override IHeaders<ICharSequence, ICharSequence> SetAll(IHeaders<ICharSequence, ICharSequence> headers)
            {
                if (ReferenceEquals(headers, this))
                {
                    return this;
                }

                foreach (ICharSequence key in headers.Names())
                {
                    this.Remove(key);
                }

                return this.Add(headers);
            }

            public override IHeaders<ICharSequence, ICharSequence> Add(ICharSequence name, ICharSequence value) => 
                this.AddEscapedValue(name, StringUtil.EscapeCsv(value));

            public override IHeaders<ICharSequence, ICharSequence> Add(ICharSequence name, IEnumerable<ICharSequence> values) => 
                this.AddEscapedValue(name, CommaSeparate(EscapeCsv, values));

            public override IHeaders<ICharSequence, ICharSequence> AddObject(ICharSequence name, object value)
            {
                ICharSequence charSequence = this.EscapeCsvObject(value);
                this.AddEscapedValue(name, charSequence);
                return this;
            }

            public override IHeaders<ICharSequence, ICharSequence> AddObject(ICharSequence name, IEnumerable<object> values) => 
                this.AddEscapedValue(name, CommaSeparate(this.EscapeCsvObject, values));

            public override IHeaders<ICharSequence, ICharSequence> AddObject(ICharSequence name, params object[] values) => 
                this.AddEscapedValue(name, CommaSeparate(this.EscapeCsvObject, values));

            public override IHeaders<ICharSequence, ICharSequence> Set(ICharSequence name, IEnumerable<ICharSequence> values)
            {
                base.Set(name, CommaSeparate(EscapeCsv, values));
                return this;
            }

            public override IHeaders<ICharSequence, ICharSequence> SetObject(ICharSequence name, object value)
            {
                ICharSequence charSequence = this.EscapeCsvObject(value);
                base.Set(name, charSequence);
                return this;
            }

            public override IHeaders<ICharSequence, ICharSequence> SetObject(ICharSequence name, IEnumerable<object> values)
            {
                base.Set(name, CommaSeparate(this.EscapeCsvObject, values));
                return this;
            }

            CombinedHttpHeadersImpl AddEscapedValue(ICharSequence name, ICharSequence escapedValue)
            {
                ICharSequence currentValue = this.Get(name);
                if (currentValue == null)
                {
                    base.Add(name, escapedValue);
                }
                else
                {
                    this.Set(name, CommaSeparateEscapedValues(currentValue, escapedValue));
                }

                return this;
            }

            static ICharSequence CommaSeparate(Func<object, ICharSequence> escaper, IEnumerable<object> values)
            {
                StringBuilderCharSequence sb = values is ICollection
                    ? new StringBuilderCharSequence(((ICollection)values).Count * ValueLengthEstimate)
                    : new StringBuilderCharSequence();

                foreach (object value in values)
                {
                    if (sb.Count > 0)
                    {
                        sb.Append(StringUtil.Comma);
                    }

                    sb.Append(escaper(value));
                }

                return sb;
            }

            static ICharSequence CommaSeparate(Func<ICharSequence, ICharSequence> escaper, IEnumerable<ICharSequence> values)
            {
                StringBuilderCharSequence sb = values is ICollection
                    ? new StringBuilderCharSequence(((ICollection)values).Count * ValueLengthEstimate)
                    : new StringBuilderCharSequence();

                foreach (ICharSequence value in values)
                {
                    if (sb.Count > 0)
                    {
                        sb.Append(StringUtil.Comma);
                    }

                    sb.Append(escaper(value));
                }

                return sb;
            }

            static ICharSequence CommaSeparateEscapedValues(ICharSequence currentValue, ICharSequence value)
            {
                var builder = new StringBuilderCharSequence(currentValue.Count + 1 + value.Count);
                builder.Append(currentValue);
                builder.Append(StringUtil.Comma);
                builder.Append(value);

                return builder;
            }

            ICharSequence EscapeCsvObject(object value) => StringUtil.EscapeCsv(this.ValueConverter.ConvertObject(value), true);

            static ICharSequence EscapeCsv(ICharSequence value) => StringUtil.EscapeCsv(value, true);
        }
    }
}
