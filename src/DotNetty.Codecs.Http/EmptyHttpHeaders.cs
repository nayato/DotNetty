// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using DotNetty.Common.Utilities;

    public class EmptyHttpHeaders : HttpHeaders
    {
        static readonly IEnumerable<KeyValuePair<CharSequence, CharSequence>> EMPTY_CHARS_ITERATOR =
            Enumerable.Empty<KeyValuePair<CharSequence, CharSequence>>();

        public static readonly EmptyHttpHeaders INSTANCE = instance();

        /**
         * @deprecated Use {@link EmptyHttpHeaders#INSTANCE}
         * <p>
         * This is needed to break a cyclic static initialization loop between {@link HttpHeaders} and
         * {@link EmptyHttpHeaders}.
         * @see HttpUtil#EMPTY_HEADERS
         */

        [Obsolete]
        static EmptyHttpHeaders instance()
        {
            return HttpUtil.EMPTY_HEADERS;
        }

        protected internal EmptyHttpHeaders()
        {
        }

        // @Override
        public string get(string name)
        {
            return null;
        }

        // @Override
        public Integer getInt(CharSequence name)
        {
            return null;
        }

        // @Override
        public int getInt(CharSequence name, int defaultValue)
        {
            return defaultValue;
        }

        // @Override
        public Short getShort(CharSequence name)
        {
            return null;
        }

        // @Override
        public short getShort(CharSequence name, short defaultValue)
        {
            return defaultValue;
        }

        // @Override
        public Long getTimeMillis(CharSequence name)
        {
            return null;
        }

        // @Override
        public long getTimeMillis(CharSequence name, long defaultValue)
        {
            return defaultValue;
        }

        // @Override
        public List<string> getAll(string name)
        {
            return Collections.emptyList();
        }

        // @Override
        public List<Entry<string, string>> entries()
        {
            return Collections.emptyList();
        }

        // @Override
        public bool contains(string name)
        {
            return false;
        }

        // @Override
        public bool isEmpty()
        {
            return true;
        }

        // @Override
        public int size()
        {
            return 0;
        }

        // @Override
        public Set<string> names()
        {
            return Collections.emptySet();
        }

        // @Override
        public HttpHeaders add(string name, object value)
        {
            throw new InvalidOperationException("read only");
        }

        // @Override
        public HttpHeaders add(string name, Iterable<? > values)
        {
            throw new InvalidOperationException("read only");
        }

        // @Override
        public HttpHeaders addInt(CharSequence name, int value)
        {
            throw new InvalidOperationException("read only");
        }

        // @Override
        public HttpHeaders addShort(CharSequence name, short value)
        {
            throw new InvalidOperationException("read only");
        }

        // @Override
        public HttpHeaders set(string name, object value)
        {
            throw new InvalidOperationException("read only");
        }

        // @Override
        public HttpHeaders set(string name, Iterable<? > values)
        {
            throw new InvalidOperationException("read only");
        }

        // @Override
        public HttpHeaders setInt(CharSequence name, int value)
        {
            throw new InvalidOperationException("read only");
        }

        // @Override
        public HttpHeaders setShort(CharSequence name, short value)
        {
            throw new InvalidOperationException("read only");
        }

        // @Override
        public HttpHeaders remove(string name)
        {
            throw new InvalidOperationException("read only");
        }

        // @Override
        public HttpHeaders clear()
        {
            throw new InvalidOperationException("read only");
        }

        // @Override
        public Iterator<Entry<string, string>> iterator()
        {
            return this.entries().iterator();
        }

        // @Override
        public Iterator<Entry<CharSequence, CharSequence>> iteratorCharSequence()
        {
            return EMPTY_CHARS_ITERATOR;
        }
    }
}