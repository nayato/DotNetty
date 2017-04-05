// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using DotNetty.Common.Utilities;

    public sealed class HttpMethod : IComparable<HttpMethod>, IComparable
    {
        /**
         * The OPTIONS method represents a request for information about the communication options
         * available on the request/response chain identified by the Request-URI. This method allows
         * the client to determine the options and/or requirements associated with a resource, or the
         * capabilities of a server, without implying a resource action or initiating a resource
         * retrieval.
         */
        public static readonly HttpMethod Options = new HttpMethod("OPTIONS");

        /**
         * The GET method means retrieve whatever information (in the form of an entity) is identified
         * by the Request-URI.  If the Request-URI refers to a data-producing process, it is the
         * produced data which shall be returned as the entity in the response and not the source text
         * of the process, unless that text happens to be the output of the process.
         */
        public static readonly HttpMethod Get = new HttpMethod("GET");

        /**
         * The HEAD method is identical to GET except that the server MUST NOT return a message-body
         * in the response.
         */
        public static readonly HttpMethod Head = new HttpMethod("HEAD");

        /**
         * The POST method is used to request that the origin server accept the entity enclosed in the
         * request as a new subordinate of the resource identified by the Request-URI in the
         * Request-Line.
         */
        public static readonly HttpMethod Post = new HttpMethod("POST");

        /**
         * The PUT method requests that the enclosed entity be stored under the supplied Request-URI.
         */
        public static readonly HttpMethod Put = new HttpMethod("PUT");

        /**
         * The PATCH method requests that a set of changes described in the
         * request entity be applied to the resource identified by the Request-URI.
         */
        public static readonly HttpMethod Patch = new HttpMethod("PATCH");

        /**
         * The DELETE method requests that the origin server delete the resource identified by the
         * Request-URI.
         */
        public static readonly HttpMethod Delete = new HttpMethod("DELETE");

        /**
         * The TRACE method is used to invoke a remote, application-layer loop- back of the request
         * message.
         */
        public static readonly HttpMethod Trace = new HttpMethod("TRACE");

        /**
         * This specification reserves the method name CONNECT for use with a proxy that can dynamically
         * switch to being a tunnel
         */
        public static readonly HttpMethod Connect = new HttpMethod("CONNECT");

        // HashMap
        static readonly Dictionary<int, HttpMethod> MethodMap;

        static HttpMethod()
        {
            MethodMap = new Dictionary<int, HttpMethod>
            {
                { Options.GetHashCode(), Options },
                { Get.GetHashCode(), Get },
                { Head.GetHashCode(), Head },
                { Post.GetHashCode(), Post },
                { Put.GetHashCode(), Put },
                { Patch.GetHashCode(), Patch },
                { Delete.GetHashCode(), Delete },
                { Trace.GetHashCode(), Trace },
                { Connect.GetHashCode(), Connect },
            };
        }

        public static HttpMethod ValueOf(ICharSequence name)
        {
            if (name != null)
            {
                int hash = AsciiString.GetHashCode(name);
                if (MethodMap.TryGetValue(hash, out HttpMethod result))
                {
                    return result;
                }
            }

            return new HttpMethod(name?.ToString());
        }

        readonly AsciiString name;

        // Creates a new HTTP method with the specified name.  You will not need to
        // create a new method unless you are implementing a protocol derived from
        // HTTP, such as
        // http://en.wikipedia.org/wiki/Real_Time_Streaming_Protocol and
        // http://en.wikipedia.org/wiki/Internet_Content_Adaptation_Protocol
        //
        public HttpMethod(string name)
        {
            Contract.Requires(name != null);

            name = name.Trim();
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(nameof(name));
            }

            foreach (char c in name)
            {
                if (CharUtil.IsISOControl(c) || char.IsWhiteSpace(c))
                {
                    throw new ArgumentException($"Invalid character '{c}' in {nameof(name)}");
                }
            }

            this.name = new AsciiString(name);
        }

        public AsciiString AsciiName() => this.name;

        public string Name() => this.name.ToString();

        public override int GetHashCode() => this.name.GetHashCode();

        public override bool Equals(object obj) => !ReferenceEquals(obj, null) 
            && obj is HttpMethod 
            && this.name.Equals(((HttpMethod)obj).name);

        public override string ToString() => this.name.ToString();

        public int CompareTo(HttpMethod other) => this.name.CompareTo(other.name);

        public int CompareTo(object obj) => this.CompareTo(obj as HttpMethod);
    }
}
