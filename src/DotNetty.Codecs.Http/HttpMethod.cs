// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using DotNetty.Common.Utilities;

    /**
 * The request method of HTTP or its derived protocols, such as
 * <a href="http://en.wikipedia.org/wiki/Real_Time_Streaming_Protocol">RTSP</a> and
 * <a href="http://en.wikipedia.org/wiki/Internet_Content_Adaptation_Protocol">ICAP</a>.
 */

    public class HttpMethod : IComparable<HttpMethod>
    {
        /**
         * The OPTIONS method represents a request for information about the communication options
         * available on the request/response chain identified by the Request-URI. This method allows
         * the client to determine the options and/or requirements associated with a resource, or the
         * capabilities of a server, without implying a resource action or initiating a resource
         * retrieval.
         */
        public static readonly HttpMethod OPTIONS = new HttpMethod("OPTIONS");

        /**
         * The GET method means retrieve whatever information (in the form of an entity) is identified
         * by the Request-URI.  If the Request-URI refers to a data-producing process, it is the
         * produced data which shall be returned as the entity in the response and not the source text
         * of the process, unless that text happens to be the output of the process.
         */
        public static readonly HttpMethod GET = new HttpMethod("GET");

        /**
         * The HEAD method is identical to GET except that the server MUST NOT return a message-body
         * in the response.
         */
        public static readonly HttpMethod HEAD = new HttpMethod("HEAD");

        /**
         * The POST method is used to request that the origin server accept the entity enclosed in the
         * request as a new subordinate of the resource identified by the Request-URI in the
         * Request-Line.
         */
        public static readonly HttpMethod POST = new HttpMethod("POST");

        /**
         * The PUT method requests that the enclosed entity be stored under the supplied Request-URI.
         */
        public static readonly HttpMethod PUT = new HttpMethod("PUT");

        /**
         * The PATCH method requests that a set of changes described in the
         * request entity be applied to the resource identified by the Request-URI.
         */
        public static readonly HttpMethod PATCH = new HttpMethod("PATCH");

        /**
         * The DELETE method requests that the origin server delete the resource identified by the
         * Request-URI.
         */
        public static readonly HttpMethod DELETE = new HttpMethod("DELETE");

        /**
         * The TRACE method is used to invoke a remote, application-layer loop- back of the request
         * message.
         */
        public static readonly HttpMethod TRACE = new HttpMethod("TRACE");

        /**
         * This specification reserves the method name CONNECT for use with a proxy that can dynamically
         * switch to being a tunnel
         */
        public static readonly HttpMethod CONNECT = new HttpMethod("CONNECT");

        static readonly Dictionary<string, HttpMethod> methodMap = new Dictionary<string, HttpMethod>();

        static HttpMethod()
        {
            methodMap.Add(OPTIONS.ToString(), OPTIONS);
            methodMap.Add(GET.ToString(), GET);
            methodMap.Add(HEAD.ToString(), HEAD);
            methodMap.Add(POST.ToString(), POST);
            methodMap.Add(PUT.ToString(), PUT);
            methodMap.Add(PATCH.ToString(), PATCH);
            methodMap.Add(DELETE.ToString(), DELETE);
            methodMap.Add(TRACE.ToString(), TRACE);
            methodMap.Add(CONNECT.ToString(), CONNECT);
        }

        /**
         * Returns the {@link HttpMethod} represented by the specified name.
         * If the specified name is a standard HTTP method name, a cached instance
         * will be returned.  Otherwise, a new instance will be returned.
         */

        public static HttpMethod valueOf(string name)
        {
            HttpMethod value;
            return methodMap.TryGetValue(name, out value) ? value : new HttpMethod(name);
        }

        readonly AsciiString name;

        /**
         * Creates a new HTTP method with the specified name.  You will not need to
         * create a new method unless you are implementing a protocol derived from
         * HTTP, such as
         * <a href="http://en.wikipedia.org/wiki/Real_Time_Streaming_Protocol">RTSP</a> and
         * <a href="http://en.wikipedia.org/wiki/Internet_Content_Adaptation_Protocol">ICAP</a>
         */

        public HttpMethod(string name)
        {
            Contract.Requires(!string.IsNullOrEmpty(name));

            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if (char.IsControl(c) || char.IsWhiteSpace(c))
                {
                    throw new ArgumentException("invalid character in name");
                }
            }

            this.name = new AsciiString(name);
        }

        /**
         * Returns the name of this method.
         */

        public string Name => this.name.ToString();

        /**
         * Returns the name of this method.
         */

        public AsciiString asciiName() => this.name;

        // @Override
        public override int GetHashCode() => this.Name.GetHashCode();

        // @Override
        public override bool Equals(object o)
        {
            if (!(o is HttpMethod))
            {
                return false;
            }

            var that = (HttpMethod)o;
            return this.Name.Equals(that.Name);
        }

        // @Override
        public override string ToString() => this.name.ToString();

        // @Override
        public int CompareTo(HttpMethod o) => this.Name.CompareTo(o.Name);
    }
}