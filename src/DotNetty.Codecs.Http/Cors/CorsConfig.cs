// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http.Cors
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Text;
    using DotNetty.Common.Concurrency;
    using DotNetty.Common.Utilities;

    public sealed class CorsConfig
    {
        public static readonly AsciiString AnyOriginValue = new AsciiString("*");

        readonly IList<ICharSequence> exposeHeaders;
        readonly ISet<HttpMethod> allowedRequestMethods;
        readonly IList<ICharSequence> allowedRequestHeaders;
        readonly IDictionary<ICharSequence, ICallable<object>> preflightHeaders;

        internal CorsConfig(CorsConfigBuilder builder)
        {
            this.Origins = new HashSet<ICharSequence>(builder.Origins);
            this.IsAnyOriginSupported = builder.AnyOrigin;
            this.IsCorsSupportEnabled = builder.Enabled;
            this.exposeHeaders = builder.ExposeHeaders;
            this.IsCredentialsAllowed = builder.AllowCredentials;
            this.MaxAge = builder.MaxAge;
            this.allowedRequestMethods = builder.RequestMethods;
            this.allowedRequestHeaders = builder.RequestHeaders;
            this.IsNullOriginAllowed = builder.AllowNullOrigin;
            this.preflightHeaders = builder.PreflightHeaders;
            this.IsShortCircuit = builder.ShortCircuit;
        }

        public bool IsCorsSupportEnabled { get; }

        public bool IsAnyOriginSupported { get; }

        public ICharSequence Origin => this.Origins.Count == 0 ? AnyOriginValue : this.Origins.First();

        public ISet<ICharSequence> Origins { get; }

        public bool IsNullOriginAllowed { get; }

        public IList<ICharSequence> ExposedHeaders => this.exposeHeaders.ToImmutableList();

        public bool IsCredentialsAllowed { get; }

        public long MaxAge { get; }

        public ISet<HttpMethod> AllowedRequestMethods => this.allowedRequestMethods.ToImmutableHashSet();

        public ISet<ICharSequence> AllowedRequestHeaders => this.allowedRequestHeaders.ToImmutableHashSet();

        public HttpHeaders PreflightResponseHeaders
        {
            get
            {
                if (this.preflightHeaders.Count == 0)
                {
                    return EmptyHttpHeaders.Default;
                }

                var headers = new DefaultHttpHeaders();
                foreach (KeyValuePair<ICharSequence, ICallable<object>> entry in this.preflightHeaders)
                {
                    object value = GetValue(entry.Value);
                    var values = value as IEnumerable<object>;
                    if (values != null)
                    {
                        headers.Add(entry.Key, values);
                    }
                    else
                    {
                        headers.Add(entry.Key, value);
                    }
                }

                return headers;
            }
        }

        public bool IsShortCircuit { get; }

        static object GetValue(ICallable<object> callable)
        {
            try
            {
                return callable.Call();
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException($"Could not generate value for callable [{callable}]", exception);
            }
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.Append($"{nameof(CorsConfig)} [")
                .Append($"enabled = {this.IsCorsSupportEnabled}");

            builder.Append(", origins=");
            if (this.Origins.Count == 0)
            {
                builder.Append("*");
            }
            else
            {
                builder.Append("(");
                foreach (ICharSequence value in this.Origins)
                {
                    builder.Append($"'{value}'");
                }
                builder.Append(")");
            }

            builder.Append(", exposedHeaders=");
            if (this.exposeHeaders.Count == 0)
            {
                builder.Append("*");
            }
            else
            {
                builder.Append("(");
                foreach (ICharSequence value in this.exposeHeaders)
                {
                    builder.Append($"'{value}'");
                }
                builder.Append(")");
            }

            builder.Append($", isCredentialsAllowed={this.IsCredentialsAllowed}");
            builder.Append($", maxAge={this.MaxAge}");

            builder.Append(", allowedRequestMethods=");
            if (this.allowedRequestMethods.Count == 0)
            {
                builder.Append("*");
            }
            else
            {
                builder.Append("(");
                foreach (HttpMethod value in this.allowedRequestMethods)
                {
                    builder.Append($"'{value}'");
                }
                builder.Append(")");
            }

            builder.Append(", allowedRequestHeaders=");
            if (this.allowedRequestHeaders.Count == 0)
            {
                builder.Append("*");
            }
            else
            {
                builder.Append("(");
                foreach(ICharSequence value in this.allowedRequestHeaders)
                {
                    builder.Append($"'{value}'");
                }
                builder.Append(")");
            }

            builder.Append(", preflightHeaders=");
            if (this.preflightHeaders.Count == 0)
            {
                builder.Append("*");
            }
            else
            {
                builder.Append("(");
                foreach (ICharSequence value in this.preflightHeaders.Keys)
                {
                    builder.Append($"'{value}'");
                }
                builder.Append(")");
            }

            builder.Append("]");

            return builder.ToString();
        }
    }
}
