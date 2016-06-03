// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http {















/**
 * Will add multiple values for the same header as single header with a comma separated list of values.
 * <p>
 * Please refer to section <a href="https://tools.ietf.org/html/rfc7230#section-3.2.2">RFC 7230, 3.2.2</a>.
 */
public class CombinedHttpHeaders : DefaultHttpHeaders {
    public CombinedHttpHeaders(bool validate) {
        base(new CombinedHttpHeadersImpl(CASE_INSENSITIVE_HASHER, valueConverter(validate), nameValidator(validate)));
    }

    private static sealed class CombinedHttpHeadersImpl
            : DefaultHeaders<CharSequence, CharSequence, CombinedHttpHeadersImpl> {
        /**
         * An estimate of the size of a header value.
         */
        private static readonly int VALUE_LENGTH_ESTIMATE = 10;
        private CsvValueEscaper<object> objectEscaper;
        private CsvValueEscaper<CharSequence> charSequenceEscaper;

        private CsvValueEscaper<object> objectEscaper() {
            if (objectEscaper == null) {
                objectEscaper = new CsvValueEscaper<object>() {
                    // @Override
                    public CharSequence escape(object value) {
                        return StringUtil.escapeCsv(valueConverter().convertObject(value));
                    }
                };
            }
            return objectEscaper;
        }

        private CsvValueEscaper<CharSequence> charSequenceEscaper() {
            if (charSequenceEscaper == null) {
                charSequenceEscaper = new CsvValueEscaper<CharSequence>() {
                    // @Override
                    public CharSequence escape(CharSequence value) {
                        return StringUtil.escapeCsv(value);
                    }
                };
            }
            return charSequenceEscaper;
        }

        public CombinedHttpHeadersImpl(HashingStrategy<CharSequence> nameHashingStrategy,
                ValueConverter<CharSequence> valueConverter,
                io.netty.handler.codec.DefaultHeaders.NameValidator<CharSequence> nameValidator) {
            base(nameHashingStrategy, valueConverter, nameValidator);
        }

        // @Override
        public List<CharSequence> getAll(CharSequence name) {
            List<CharSequence> values = base.getAll(name);
            if (values.isEmpty()) {
                return values;
            }
            if (values.size() != 1) {
                throw new IllegalStateException("CombinedHttpHeaders should only have one value");
            }
            return StringUtil.unescapeCsvFields(values.get(0));
        }

        // @Override
        public CombinedHttpHeadersImpl add(Headers<? : CharSequence, ? : CharSequence, ?> headers) {
            // Override the fast-copy mechanism used by DefaultHeaders
            if (headers == this) {
                throw new ArgumentException("can't add to itself.");
            }
            if (headers is CombinedHttpHeadersImpl) {
                if (isEmpty()) {
                    // Can use the fast underlying copy
                    addImpl(headers);
                } else {
                    // Values are already escaped so don't escape again
                    for (Map.Entry<? : CharSequence, ? : CharSequence> header : headers) {
                        addEscapedValue(header.getKey(), header.getValue());
                    }
                }
            } else {
                for (Map.Entry<? : CharSequence, ? : CharSequence> header : headers) {
                    add(header.getKey(), header.getValue());
                }
            }
            return this;
        }

        // @Override
        public CombinedHttpHeadersImpl set(Headers<? : CharSequence, ? : CharSequence, ?> headers) {
            if (headers == this) {
                return this;
            }
            clear();
            return add(headers);
        }

        // @Override
        public CombinedHttpHeadersImpl setAll(Headers<? : CharSequence, ? : CharSequence, ?> headers) {
            if (headers == this) {
                return this;
            }
            for (CharSequence key : headers.names()) {
                remove(key);
            }
            return add(headers);
        }

        // @Override
        public CombinedHttpHeadersImpl add(CharSequence name, CharSequence value) {
            return addEscapedValue(name, StringUtil.escapeCsv(value));
        }

        // @Override
        public CombinedHttpHeadersImpl add(CharSequence name, CharSequence... values) {
            return addEscapedValue(name, commaSeparate(charSequenceEscaper(), values));
        }

        // @Override
        public CombinedHttpHeadersImpl add(CharSequence name, Iterable<? : CharSequence> values) {
            return addEscapedValue(name, commaSeparate(charSequenceEscaper(), values));
        }

        // @Override
        public CombinedHttpHeadersImpl addObject(CharSequence name, Iterable<?> values) {
            return addEscapedValue(name, commaSeparate(objectEscaper(), values));
        }

        // @Override
        public CombinedHttpHeadersImpl addObject(CharSequence name, object... values) {
            return addEscapedValue(name, commaSeparate(objectEscaper(), values));
        }

        // @Override
        public CombinedHttpHeadersImpl set(CharSequence name, CharSequence... values) {
            base.set(name, commaSeparate(charSequenceEscaper(), values));
            return this;
        }

        // @Override
        public CombinedHttpHeadersImpl set(CharSequence name, Iterable<? : CharSequence> values) {
            base.set(name, commaSeparate(charSequenceEscaper(), values));
            return this;
        }

        // @Override
        public CombinedHttpHeadersImpl setObject(CharSequence name, object value) {
            base.set(name, commaSeparate(objectEscaper(), value));
            return this;
        }

        // @Override
        public CombinedHttpHeadersImpl setObject(CharSequence name, object... values) {
            base.set(name, commaSeparate(objectEscaper(), values));
            return this;
        }

        // @Override
        public CombinedHttpHeadersImpl setObject(CharSequence name, Iterable<?> values) {
            base.set(name, commaSeparate(objectEscaper(), values));
            return this;
        }

        private CombinedHttpHeadersImpl addEscapedValue(CharSequence name, CharSequence escapedValue) {
            CharSequence currentValue = base.get(name);
            if (currentValue == null) {
                base.add(name, escapedValue);
            } else {
                base.set(name, commaSeparateEscapedValues(currentValue, escapedValue));
            }
            return this;
        }

        private static <T> CharSequence commaSeparate(CsvValueEscaper<T> escaper, T... values) {
            StringBuilder sb = new StringBuilder(values.length * VALUE_LENGTH_ESTIMATE);
            if (values.length > 0) {
                int end = values.length - 1;
                for (int i = 0; i < end; i++) {
                    sb.append(escaper.escape(values[i])).append(COMMA);
                }
                sb.append(escaper.escape(values[end]));
            }
            return sb;
        }

        private static <T> CharSequence commaSeparate(CsvValueEscaper<T> escaper, Iterable<? : T> values) {
            
            readonly StringBuilder sb = values is Collection
                    ? new StringBuilder(((Collection) values).size() * VALUE_LENGTH_ESTIMATE) : new StringBuilder();
            Iterator<? : T> iterator = values.iterator();
            if (iterator.hasNext()) {
                T next = iterator.next();
                while (iterator.hasNext()) {
                    sb.append(escaper.escape(next)).append(COMMA);
                    next = iterator.next();
                }
                sb.append(escaper.escape(next));
            }
            return sb;
        }

        private CharSequence commaSeparateEscapedValues(CharSequence currentValue, CharSequence value) {
            return new StringBuilder(currentValue.length() + 1 + value.length())
                    .append(currentValue)
                    .append(COMMA)
                    .append(value);
        }

        /**
         * Escapes comma separated values (CSV).
         *
         * @param <T> The type that a concrete implementation handles
         */
        private interface CsvValueEscaper<T> {
            /**
             * Appends the value to the specified {@link StringBuilder}, escaping if necessary.
             *
             * @param value the value to be appended, escaped if necessary
             */
            CharSequence escape(T value);
        }
    }
}
