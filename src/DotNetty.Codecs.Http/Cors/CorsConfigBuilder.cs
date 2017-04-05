// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http.Cors
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics.Contracts;
    using DotNetty.Common.Concurrency;
    using DotNetty.Common.Utilities;

    public sealed class CorsConfigBuilder
    {
        public static CorsConfigBuilder ForAnyOrigin() => new CorsConfigBuilder();

        public static CorsConfigBuilder ForOrigin(ICharSequence origin) => CorsConfig.AnyOriginValue.Equals(origin) ? new CorsConfigBuilder() : new CorsConfigBuilder(origin);

        public static CorsConfigBuilder ForOrigins(params ICharSequence[] origins) => new CorsConfigBuilder(origins);

        readonly List<ICharSequence> exposeHeaders = new List<ICharSequence>();
        readonly HashSet<HttpMethod> requestMethods = new HashSet<HttpMethod>();
        readonly List<ICharSequence> requestHeaders = new List<ICharSequence>();
        readonly Dictionary<ICharSequence, ICallable<object>> preflightHeaders = new Dictionary<ICharSequence, ICallable<object>>();

        bool noPreflightHeaders;

        CorsConfigBuilder(params ICharSequence[] origins)
        {
            this.Origins = new HashSet<ICharSequence>(origins);
            this.AnyOrigin = false;
        }

        CorsConfigBuilder()
        {
            this.AnyOrigin = true;
            this.Origins = ImmutableHashSet<ICharSequence>.Empty;
        }

        internal bool Enabled { get; private set; } = true;

        internal ISet<ICharSequence> Origins { get; }

        internal bool AnyOrigin { get; }

        internal IList<ICharSequence> ExposeHeaders => this.exposeHeaders;

        internal bool AllowCredentials { get; private set; }

        internal long MaxAge { get; private set; }

        internal ISet<HttpMethod> RequestMethods => this.requestMethods;

        internal IList<ICharSequence> RequestHeaders => this.requestHeaders;

        internal bool AllowNullOrigin { get; private set; }

        internal IDictionary<ICharSequence, ICallable<object>> PreflightHeaders => this.preflightHeaders;

        internal bool ShortCircuit { get; private set; }

        public CorsConfigBuilder WithAllowNullOrigin()
        {
            this.AllowNullOrigin = true;
            return this;
        }

        public CorsConfigBuilder Disable()
        {
            this.Enabled = false;
            return this;
        }

        public CorsConfigBuilder WithExposeHeaders(params ICharSequence[] headers)
        {
            foreach (ICharSequence header in headers)
            {
                this.exposeHeaders.Add(header);
            }

            return this;
        }

        public CorsConfigBuilder WithAllowCredentials()
        {
            this.AllowCredentials = true;
            return this;
        }

        public CorsConfigBuilder WithMaxAge(long max)
        {
            this.MaxAge = max;
            return this;
        }

        public CorsConfigBuilder AllowedRequestMethods(params HttpMethod[] methods)
        {
            foreach (HttpMethod method in methods)
            {
                this.requestMethods.Add(method);
            }
            return this;
        }

        public CorsConfigBuilder AllowedRequestHeaders(params ICharSequence[] headers)
        {
            Contract.Requires(headers != null);

            foreach (ICharSequence header in headers)
            {
                this.requestHeaders.Add(header);
            }

            return this;
        }

        public CorsConfigBuilder PreflightResponseHeader(ICharSequence name, params object[] values)
        {
            Contract.Requires(values != null);

            if (values.Length == 1)
            {
                this.preflightHeaders.Add(name, new ConstantValueGenerator(values[0]));
            }
            else
            {
                this.PreflightResponseHeader(name, new List<object>(values));
            }
            return this;
        }

        public CorsConfigBuilder PreflightResponseHeader(ICharSequence name, IEnumerable<object> value)
        {
            this.preflightHeaders.Add(name, new ConstantValueGenerator(value));
            return this;
        }

        public CorsConfigBuilder PreflightResponseHeader(ICharSequence name, ICallable<object> valueGenerator)
        {
            this.preflightHeaders.Add(name, valueGenerator);
            return this;
        }

        public CorsConfigBuilder NoPreflightResponseHeaders()
        {
            this.noPreflightHeaders = true;
            return this;
        }

        public CorsConfigBuilder WithShortCircuit()
        {
            this.ShortCircuit = true;
            return this;
        }

        public CorsConfig Build()
        {
            if (this.preflightHeaders.Count == 0 && !this.noPreflightHeaders)
            {
                this.preflightHeaders.Add(HttpHeaderNames.Date, DateValueGenerator.Default);
                this.preflightHeaders.Add(HttpHeaderNames.ContentLength, new ConstantValueGenerator(new AsciiString("0")));
            }

            return new CorsConfig(this);
        }

        /**
          * This class is used for preflight HTTP response values that do not need to be
          * generated, but instead the value is "static" in that the same value will be returned
          * for each call.
          */
        sealed class ConstantValueGenerator : ICallable<object>
        {
            readonly object value;

            internal ConstantValueGenerator(object value)
            {
                Contract.Requires(value != null);
                this.value = value;
            }

            public object Call() => this.value;
        }

        /**
          * This callable is used for the DATE preflight HTTP response HTTP header.
          * It's value must be generated when the response is generated, hence will be
          * different for every call.
          */
        sealed class DateValueGenerator : ICallable<object>
        {
            internal static readonly DateValueGenerator Default = new DateValueGenerator();

            public object Call() => new DateTime();
        }
    }
}

