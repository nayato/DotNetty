// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using DotNetty.Common.Utilities;

    public class EmptyHttpHeaders : HttpHeaders
    {
        public static readonly EmptyHttpHeaders Default = new EmptyHttpHeaders();

        protected EmptyHttpHeaders()
        {
        }

        public override ICharSequence Get(ICharSequence name) => null;

        public override int? GetInt(ICharSequence name) => null;

        public override int GetInt(ICharSequence name, int defaultValue) => defaultValue;

        public override short? GetShort(ICharSequence name) => null;

        public override short GetShort(ICharSequence name, short defaultValue) => defaultValue;

        public override long? GetTimeMillis(ICharSequence name) => null;

        public override long GetTimeMillis(ICharSequence name, long defaultValue) => defaultValue;

        public override IList<ICharSequence> GetAll(ICharSequence name) => ImmutableList<ICharSequence>.Empty;

        public override bool Contains(ICharSequence name) => false;

        public override bool IsEmpty => true;

        public override int Size => 0;

        public override ISet<ICharSequence> Names() => ImmutableHashSet<ICharSequence>.Empty;

        public override HttpHeaders AddInt(ICharSequence name, int value)
        {
            throw new NotSupportedException();
        }

        public override HttpHeaders AddShort(ICharSequence name, short value)
        {
            throw new NotSupportedException();
        }

        public override HttpHeaders Set(ICharSequence name, object value)
        {
            throw new NotSupportedException();
        }

        public override HttpHeaders Set(ICharSequence name, IEnumerable<object> values)
        {
            throw new NotSupportedException();
        }

        public override HttpHeaders SetInt(ICharSequence name, int value)
        {
            throw new NotSupportedException();
        }

        public override HttpHeaders SetShort(ICharSequence name, short value)
        {
            throw new NotSupportedException();
        }

        public override HttpHeaders Remove(ICharSequence name)
        {
            throw new NotSupportedException();
        }

        public override HttpHeaders Clear()
        {
            throw new NotSupportedException();
        }

        public override HttpHeaders Add(ICharSequence name, object value)
        {
            throw new NotSupportedException();
        }

        public override IEnumerator<HeaderEntry<ICharSequence, ICharSequence>> GetEnumerator() => 
            Enumerable.Empty<HeaderEntry<ICharSequence, ICharSequence>>().GetEnumerator();
    }
}
