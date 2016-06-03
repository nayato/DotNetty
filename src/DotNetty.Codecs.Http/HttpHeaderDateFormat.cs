// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http {
    using DotNetty.Common;

    /**
 * This DateFormat decodes 3 formats of {@link Date}, but only encodes the one,
 * the first:
 * <ul>
 * <li>Sun, 06 Nov 1994 08:49:37 GMT: standard specification, the only one with
 * valid generation</li>
 * <li>Sunday, 06-Nov-94 08:49:37 GMT: obsolete specification</li>
 * <li>Sun Nov  6 08:49:37 1994: obsolete specification</li>
 * </ul>
 */
public sealed class HttpHeaderDateFormat : SimpleDateFormat {
    private static readonly long serialVersionUID = -925286159755905325L;

    private readonly SimpleDateFormat format1 = new HttpHeaderDateFormatObsolete1();
    private readonly SimpleDateFormat format2 = new HttpHeaderDateFormatObsolete2();

    private static readonly FastThreadLocal<HttpHeaderDateFormat> dateFormatThreadLocal =
            new FastThreadLocal<HttpHeaderDateFormat>() {
                // @Override
                protected HttpHeaderDateFormat initialValue() {
                    return new HttpHeaderDateFormat();
                }
            };

    public static HttpHeaderDateFormat get() {
        return dateFormatThreadLocal.get();
    }

    /**
     * Standard date format<p>
     * Sun, 06 Nov 1994 08:49:37 GMT -> E, d MMM yyyy HH:mm:ss z
     */
    private HttpHeaderDateFormat() {
        base("E, dd MMM yyyy HH:mm:ss z", Locale.ENGLISH);
        setTimeZone(TimeZone.getTimeZone("GMT"));
    }

    // @Override
    public Date parse(string text, ParsePosition pos) {
        Date date = base.parse(text, pos);
        if (date == null) {
            date = format1.parse(text, pos);
        }
        if (date == null) {
            date = format2.parse(text, pos);
        }
        return date;
    }

    /**
     * First obsolete format<p>
     * Sunday, 06-Nov-94 08:49:37 GMT -> E, d-MMM-y HH:mm:ss z
     */
    private static sealed class HttpHeaderDateFormatObsolete1 : SimpleDateFormat {
        private static readonly long serialVersionUID = -3178072504225114298L;

        HttpHeaderDateFormatObsolete1() {
            base("E, dd-MMM-yy HH:mm:ss z", Locale.ENGLISH);
            setTimeZone(TimeZone.getTimeZone("GMT"));
        }
    }

    /**
     * Second obsolete format
     * <p>
     * Sun Nov 6 08:49:37 1994 -> EEE, MMM d HH:mm:ss yyyy
     */
    private static sealed class HttpHeaderDateFormatObsolete2 : SimpleDateFormat {
        private static readonly long serialVersionUID = 3010674519968303714L;

        HttpHeaderDateFormatObsolete2() {
            base("E MMM d HH:mm:ss yyyy", Locale.ENGLISH);
            setTimeZone(TimeZone.getTimeZone("GMT"));
        }
    }
}
