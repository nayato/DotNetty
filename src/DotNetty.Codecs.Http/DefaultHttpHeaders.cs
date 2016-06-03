// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http {
    using System;
    using DotNetty.Common.Internal;
    using DotNetty.Common.Utilities;

    /**
 * Default implementation of {@link HttpHeaders}.
 */
public class DefaultHttpHeaders : HttpHeaders {
    private static readonly int HIGHEST_INVALID_VALUE_CHAR_MASK = ~15;
    private static readonly ByteProcessor HEADER_NAME_VALIDATOR = new HeaderNameValidator();

    sealed class HeaderNameValidator : ByteProcessor
    {
        public override bool Process(byte value)
        {
            validateHeaderNameElement(value);
            return true;
        }
    }

    static readonly NameValidator<CharSequence> HttpNameValidator = new NameValidator<CharSequence>() {
        // @Override
        public void validateName(CharSequence name) {
            if (name == null || name.length() == 0) {
                throw new ArgumentException("empty headers are not allowed [" + name + "]");
            }
            if (name is AsciiString) {
                try {
                    ((AsciiString) name).forEachByte(HEADER_NAME_VALIDATOR);
                } catch (Exception e) {
                    PlatformDependent.throwException(e);
                }
            } else {
                // Go through each character in the name
                for (int index = 0; index < name.length(); ++index) {
                    validateHeaderNameElement(name.charAt(index));
                }
            }
        }
    };

    private readonly DefaultHeaders<CharSequence, CharSequence, ?> headers;

    public DefaultHttpHeaders() {
        this(true);
    }

    public DefaultHttpHeaders(bool validate) {
        this(validate, nameValidator(validate));
    }

    protected DefaultHttpHeaders(bool validate, NameValidator<CharSequence> nameValidator) {
        this(new DefaultHeadersImpl<CharSequence, CharSequence>(CASE_INSENSITIVE_HASHER,
                                                                valueConverter(validate),
                                                                nameValidator));
    }

    protected DefaultHttpHeaders(DefaultHeaders<CharSequence, CharSequence, ?> headers) {
        this.headers = headers;
    }

    // @Override
    public HttpHeaders add(HttpHeaders headers) {
        if (headers is DefaultHttpHeaders) {
            this.headers.add(((DefaultHttpHeaders) headers).headers);
            return this;
        } else {
            return base.add(headers);
        }
    }

    // @Override
    public HttpHeaders set(HttpHeaders headers) {
        if (headers is DefaultHttpHeaders) {
            this.headers.set(((DefaultHttpHeaders) headers).headers);
            return this;
        } else {
            return base.set(headers);
        }
    }

    // @Override
    public HttpHeaders add(string name, object value) {
        headers.addObject(name, value);
        return this;
    }

    // @Override
    public HttpHeaders add(CharSequence name, object value) {
        headers.addObject(name, value);
        return this;
    }

    // @Override
    public HttpHeaders add(string name, Iterable<?> values) {
        headers.addObject(name, values);
        return this;
    }

    // @Override
    public HttpHeaders add(CharSequence name, Iterable<?> values) {
        headers.addObject(name, values);
        return this;
    }

    // @Override
    public HttpHeaders addInt(CharSequence name, int value) {
        headers.addInt(name, value);
        return this;
    }

    // @Override
    public HttpHeaders addShort(CharSequence name, short value) {
        headers.addShort(name, value);
        return this;
    }

    // @Override
    public HttpHeaders remove(string name) {
        headers.remove(name);
        return this;
    }

    // @Override
    public HttpHeaders remove(CharSequence name) {
        headers.remove(name);
        return this;
    }

    // @Override
    public HttpHeaders set(string name, object value) {
        headers.setObject(name, value);
        return this;
    }

    // @Override
    public HttpHeaders set(CharSequence name, object value) {
        headers.setObject(name, value);
        return this;
    }

    // @Override
    public HttpHeaders set(string name, Iterable<?> values) {
        headers.setObject(name, values);
        return this;
    }

    // @Override
    public HttpHeaders set(CharSequence name, Iterable<?> values) {
        headers.setObject(name, values);
        return this;
    }

    // @Override
    public HttpHeaders setInt(CharSequence name, int value) {
        headers.setInt(name, value);
        return this;
    }

    // @Override
    public HttpHeaders setShort(CharSequence name, short value) {
        headers.setShort(name, value);
        return this;
    }

    // @Override
    public HttpHeaders clear() {
        headers.clear();
        return this;
    }

    // @Override
    public string get(string name) {
        return get((CharSequence) name);
    }

    // @Override
    public string get(CharSequence name) {
        return HeadersUtils.getAsString(headers, name);
    }

    // @Override
    public Integer getInt(CharSequence name) {
        return headers.getInt(name);
    }

    // @Override
    public int getInt(CharSequence name, int defaultValue) {
        return headers.getInt(name, defaultValue);
    }

    // @Override
    public Short getShort(CharSequence name) {
        return headers.getShort(name);
    }

    // @Override
    public short getShort(CharSequence name, short defaultValue) {
        return headers.getShort(name, defaultValue);
    }

    // @Override
    public Long getTimeMillis(CharSequence name) {
        return headers.getTimeMillis(name);
    }

    // @Override
    public long getTimeMillis(CharSequence name, long defaultValue) {
        return headers.getTimeMillis(name, defaultValue);
    }

    // @Override
    public List<string> getAll(string name) {
        return getAll((CharSequence) name);
    }

    // @Override
    public List<string> getAll(CharSequence name) {
        return HeadersUtils.getAllAsString(headers, name);
    }

    // @Override
    public List<Entry<string, string>> entries() {
        if (isEmpty()) {
            return Collections.emptyList();
        }
        List<Entry<string, string>> entriesConverted = new ArrayList<Entry<string, string>>(
                headers.size());
        for (Entry<string, string> entry : this) {
            entriesConverted.add(entry);
        }
        return entriesConverted;
    }

    [Obsolete]
    // @Override
    public Iterator<Map.Entry<string, string>> iterator() {
        return HeadersUtils.iteratorAsString(headers);
    }

    // @Override
    public Iterator<Entry<CharSequence, CharSequence>> iteratorCharSequence() {
        return headers.iterator();
    }

    // @Override
    public bool contains(string name) {
        return contains((CharSequence) name);
    }

    // @Override
    public bool contains(CharSequence name) {
        return headers.contains(name);
    }

    // @Override
    public bool isEmpty() {
        return headers.isEmpty();
    }

    // @Override
    public int size() {
        return headers.size();
    }

    // @Override
    public bool contains(string name, string value, bool ignoreCase) {
        return contains((CharSequence) name, (CharSequence) value, ignoreCase);
    }

    // @Override
    public bool contains(CharSequence name, CharSequence value, bool ignoreCase) {
        return headers.contains(name, value, ignoreCase ? CASE_INSENSITIVE_HASHER : CASE_SENSITIVE_HASHER);
    }

    // @Override
    public Set<string> names() {
        return HeadersUtils.namesAsString(headers);
    }

    // @Override
    public bool Equals(object o) {
        if (!(o is DefaultHttpHeaders)) {
            return false;
        }
        return headers.Equals(((DefaultHttpHeaders) o).headers, CASE_SENSITIVE_HASHER);
    }

    // @Override
    public int GetHashCode() {
        return headers.GetHashCode(CASE_SENSITIVE_HASHER);
    }

    private static void validateHeaderNameElement(byte value) {
        switch (value) {
        case 0x00:
        case '\t':
        case '\n':
        case 0x0b:
        case '\f':
        case '\r':
        case ' ':
        case ',':
        case ':':
        case ';':
        case '=':
            throw new ArgumentException(
               "a header name cannot contain the following prohibited characters: =,;: \\t\\r\\n\\v\\f: " +
                       value);
        default:
            // Check to see if the character is not an ASCII character, or invalid
            if (value < 0) {
                throw new ArgumentException("a header name cannot contain non-ASCII character: " +
                        value);
            }
        }
    }

    private static void validateHeaderNameElement(char value) {
        switch (value) {
        case 0x00:
        case '\t':
        case '\n':
        case 0x0b:
        case '\f':
        case '\r':
        case ' ':
        case ',':
        case ':':
        case ';':
        case '=':
            throw new ArgumentException(
               "a header name cannot contain the following prohibited characters: =,;: \\t\\r\\n\\v\\f: " +
                       value);
        default:
            // Check to see if the character is not an ASCII character, or invalid
            if (value > 127) {
                throw new ArgumentException("a header name cannot contain non-ASCII character: " +
                        value);
            }
        }
    }

    static ValueConverter<CharSequence> valueConverter(bool validate) {
        return validate ? HeaderValueConverterAndValidator.INSTANCE : HeaderValueConverter.INSTANCE;
    }

    
    static NameValidator<CharSequence> nameValidator(bool validate) {
        return validate ? HttpNameValidator : NameValidator.NOT_NULL;
    }

    private static class HeaderValueConverter : CharSequenceValueConverter {
        static readonly HeaderValueConverter INSTANCE = new HeaderValueConverter();

        // @Override
        public CharSequence convertObject(object value) {
            if (value is CharSequence) {
                return (CharSequence) value;
            }
            if (value is Date) {
                return HttpHeaderDateFormat.get().format((Date) value);
            }
            if (value is Calendar) {
                return HttpHeaderDateFormat.get().format(((Calendar) value).getTime());
            }
            return value.ToString();
        }
    }

    private static sealed class HeaderValueConverterAndValidator : HeaderValueConverter {
        static readonly HeaderValueConverterAndValidator INSTANCE = new HeaderValueConverterAndValidator();

        // @Override
        public CharSequence convertObject(object value) {
            CharSequence seq = base.convertObject(value);
            int state = 0;
            // Start looping through each of the character
            for (int index = 0; index < seq.length(); index++) {
                state = validateValueChar(seq, state, seq.charAt(index));
            }

            if (state != 0) {
                throw new ArgumentException("a header value must not end with '\\r' or '\\n':" + seq);
            }
            return seq;
        }

        private static int validateValueChar(CharSequence seq, int state, char character) {
            /*
             * State:
             * 0: Previous character was neither CR nor LF
             * 1: The previous character was CR
             * 2: The previous character was LF
             */
            if ((character & HIGHEST_INVALID_VALUE_CHAR_MASK) == 0) {
                // Check the absolutely prohibited characters.
                switch (character) {
                case 0x0: // NULL
                    throw new ArgumentException("a header value contains a prohibited character '\0': " + seq);
                case 0x0b: // Vertical tab
                    throw new ArgumentException("a header value contains a prohibited character '\\v': " + seq);
                case '\f':
                    throw new ArgumentException("a header value contains a prohibited character '\\f': " + seq);
                }
            }

            // Check the CRLF (HT | SP) pattern
            switch (state) {
                case 0:
                    switch (character) {
                        case '\r':
                            return 1;
                        case '\n':
                            return 2;
                    }
                    break;
                case 1:
                    switch (character) {
                        case '\n':
                            return 2;
                        default:
                            throw new ArgumentException("only '\\n' is allowed after '\\r': " + seq);
                    }
                case 2:
                    switch (character) {
                        case '\t':
                        case ' ':
                            return 0;
                        default:
                            throw new ArgumentException("only ' ' and '\\t' are allowed after '\\n': " + seq);
                    }
            }
            return state;
        }
    }
}
