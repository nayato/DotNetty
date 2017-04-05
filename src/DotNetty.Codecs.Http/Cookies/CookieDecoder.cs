// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http.Cookies
{
    using DotNetty.Common.Internal.Logging;
    using DotNetty.Common.Utilities;

    public abstract class CookieDecoder
    {
        static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<CookieDecoder>();

        protected CookieDecoder(bool strict)
        {
            this.Strict = strict;
        }

        protected bool Strict { get; }

        protected DefaultCookie InitCookie(string header, int nameBegin, int nameEnd, int valueBegin, int valueEnd)
        {
            if (nameBegin == -1 
                || nameBegin == nameEnd)
            {
                Logger.Debug("Skipping cookie with null name");
                return null;
            }

            if (valueBegin == -1)
            {
                Logger.Debug("Skipping cookie with null value");
                return null;
            }

            var sequence = new StringCharSequence(header, valueBegin, valueEnd - valueBegin);
            ICharSequence unwrappedValue = CookieUtil.UnwrapValue(sequence);
            if (unwrappedValue == null)
            {
                Logger.Debug("Skipping cookie because starting quotes are not properly balanced in '{}'", sequence);
                return null;
            }

            string name = header.Substring(nameBegin, nameEnd - nameBegin);
            int invalidOctetPos;
            if (this.Strict 
                && (invalidOctetPos = CookieUtil.FirstInvalidCookieNameOctet(name)) >= 0)
            {
                if (Logger.DebugEnabled)
                {
                    Logger.Debug("Skipping cookie because name '{}' contains invalid char '{}'", 
                        name, name[invalidOctetPos]);
                }
                return null;
            }

            bool wrap = unwrappedValue.Count != valueEnd - valueBegin;

            if (this.Strict
                && (invalidOctetPos = CookieUtil.FirstInvalidCookieValueOctet(unwrappedValue)) >= 0)
            {
                if (Logger.DebugEnabled)
                {
                    Logger.Debug("Skipping cookie because value '{}' contains invalid char '{}'",
                        unwrappedValue, unwrappedValue[invalidOctetPos]);
                }

                return null;
            }

            var cookie = new DefaultCookie(name, unwrappedValue.ToString())
            {
                Wrap = wrap
            };

            return cookie;
        }
    }
}
