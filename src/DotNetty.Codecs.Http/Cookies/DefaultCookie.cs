// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http.Cookies
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Text;

    public sealed class DefaultCookie : ICookie
    {
        public static readonly long DefaultMaxAge = long.MaxValue;

        string domain;
        string path;

        public DefaultCookie(string name, string value)
        {
            Contract.Requires(!string.IsNullOrEmpty(name.Trim()));
            Contract.Requires(!string.IsNullOrEmpty(value));

            this.Name = name;
            this.Value = value;
            this.MaxAge = long.MinValue;
        }

        public string Name { get; }

        public string Value { get; set; }

        public bool Wrap { get; set; }

        public string Domain
        {
            get => this.domain;
            set => this.domain = CookieUtil.ValidateAttributeValue(nameof(this.domain), value);
        }

        public string Path
        {
            get => this.path;
            set => this.path = CookieUtil.ValidateAttributeValue(nameof(this.path), value);
        }

        public long MaxAge { get; set; }

        public bool IsSecure { get; set; }

        public bool IsHttpOnly { get; set; }

        public bool Equals(ICookie other)
        {
            if (other == null)
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (string.Compare(this.Name, other.Name, StringComparison.Ordinal) != 0)
            {
                return false;
            }

            if (this.path == null)
            {
                if (other.Path != null)
                {
                    return false;
                }
            }
            else if (other.Path == null)
            {
                return false;
            }
            else if (string.Compare(this.path, other.Path, StringComparison.Ordinal) != 0)
            {
                return false;
            }

            if (this.domain == null)
            {
                if (other.Domain != null)
                {
                    return false;
                }
            }
            else
            {
                return string.Compare(this.domain, other.Domain, StringComparison.OrdinalIgnoreCase) == 0;
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            var a = obj as DefaultCookie;
            return a != null && this.Equals(a);
        }

        public override int GetHashCode() => this.Name.GetHashCode();

        public int CompareTo(ICookie other)
        {
            int v = string.Compare(this.Name, other.Name, StringComparison.Ordinal);
            if (v != 0)
            {
                return v;
            }

            if (this.path == null)
            {
                if (other.Path != null)
                {
                    return -1;
                }
            }
            else if (other.Path == null)
            {
                return 1;
            }
            else
            {
                v = string.Compare(this.path, other.Path, StringComparison.Ordinal);
                if (v != 0)
                {
                    return v;
                }
            }

            if (this.domain == null)
            {
                if (other.Domain != null)
                {
                    return -1;
                }
            }
            else if (other.Domain == null)
            {
                return 1;
            }
            else
            {
                v = string.Compare(this.domain, other.Domain, StringComparison.OrdinalIgnoreCase);
                return v;
            }

            return 0;
        }

        public int CompareTo(object obj)
        {
            if (obj == null)
            {
                return 1;
            }

            if (!(obj is DefaultCookie))
            {
                throw new ArgumentException($"{nameof(obj)} must be of {nameof(DefaultCookie)} type");
            }

            return this.CompareTo((DefaultCookie)obj);
        }

        public override string ToString()
        {
            var buf = new StringBuilder();

            buf.Append($"{nameof(this.Name)}={this.Value}");
            if (this.Domain != null)
            {
                buf.Append($", {nameof(this.Domain)}={this.domain}");
            }

            if (this.path != null)
            {
                buf.Append($", {nameof(this.Path)}={this.path}");
            }
            if (this.MaxAge >= 0)
            {
                buf.Append($", {nameof(this.MaxAge)}={this.MaxAge}");
            }
            if (this.IsSecure)
            {
                buf.Append(", secure");
            }
            if (this.IsHttpOnly)
            {
                buf.Append(", HTTPOnly");
            }

            return buf.ToString();
        }
    }
}
