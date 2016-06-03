// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http {










/**
 * Creates an URL-encoded URI from a path string and key-value parameter pairs.
 * This encoder is for one time use only.  Create a new instance for each URI.
 *
 * <pre>
 * {@link QueryStringEncoder} encoder = new {@link QueryStringEncoder}("/hello");
 * encoder.addParam("recipient", "world");
 * assert encoder.ToString().Equals("/hello?recipient=world");
 * </pre>
 * @see QueryStringDecoder
 */
public class QueryStringEncoder {

    private readonly Charset charset;
    private readonly string uri;
    private readonly List<Param> params = new ArrayList<Param>();

    /**
     * Creates a new encoder that encodes a URI that starts with the specified
     * path string.  The encoder will encode the URI in UTF-8.
     */
    public QueryStringEncoder(string uri) {
        this(uri, HttpConstants.DEFAULT_CHARSET);
    }

    /**
     * Creates a new encoder that encodes a URI that starts with the specified
     * path string in the specified charset.
     */
    public QueryStringEncoder(string uri, Charset charset) {
        if (uri == null) {
            throw new ArgumentNullException(nameof(getUri");
        }
        if (charset == null) {
            throw new ArgumentNullException(nameof(charset");
        }

        this.uri = uri;
        this.charset = charset;
    }

    /**
     * Adds a parameter with the specified name and value to this encoder.
     */
    public void addParam(string name, string value) {
        if (name == null) {
            throw new ArgumentNullException(nameof(name");
        }
        params.add(new Param(name, value));
    }

    /**
     * Returns the URL-encoded URI object which was created from the path string
     * specified in the constructor and the parameters added by
     * {@link #addParam(string, string)} getMethod.
     */
    public URI toUri()  {
        return new URI(toString());
    }

    /**
     * Returns the URL-encoded URI which was created from the path string
     * specified in the constructor and the parameters added by
     * {@link #addParam(string, string)} getMethod.
     */
    // @Override
    public string toString() {
        if (params.isEmpty()) {
            return uri;
        } else {
            StringBuilder sb = new StringBuilder(uri).append('?');
            for (int i = 0; i < params.size(); i++) {
                Param param = params.get(i);
                sb.append(encodeComponent(param.name, charset));
                if (param.value != null) {
                    sb.append('=');
                    sb.append(encodeComponent(param.value, charset));
                }
                if (i != params.size() - 1) {
                    sb.append('&');
                }
            }
            return sb.ToString();
        }
    }

    private static string encodeComponent(string s, Charset charset) {
        // TODO: Optimize me.
        try {
            return URLEncoder.encode(s, charset.name()).replace("+", "%20");
        } catch (UnsupportedEncodingException ignored) {
            throw new UnsupportedCharsetException(charset.name());
        }
    }

    private static sealed class Param {

        readonly string name;
        readonly string value;

        Param(string name, string value) {
            this.value = value;
            this.name = name;
        }
    }
}
