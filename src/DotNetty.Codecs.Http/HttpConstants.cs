// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http
{
    public sealed class HttpConstants
    {
        /**
                 * Horizontal space
                 */
        public static readonly byte SP = 32;

        /**
         * Horizontal tab
         */
        public static readonly byte HT = 9;

        /**
         * Carriage return
         */
        public static readonly byte CR = 13;

        /**
         * Equals '='
         */
        public static readonly byte EQUALS = 61;

        /**
         * Line feed character
         */
        public static readonly byte LF = 10;

        /**
         * Colon ':'
         */
        public static readonly byte COLON = 58;

        /**
         * Semicolon ';'
         */
        public static readonly byte SEMICOLON = 59;

        /**
         * Comma ','
         */
        public static readonly byte COMMA = 44;

        /**
         * Double quote '"'
         */
        public static readonly byte DOUBLE_QUOTE = '"';

        /**
         * Default character set (UTF-8)
         */
        public static readonly Charset DEFAULT_CHARSET = CharsetUtil.UTF_8;

        /**
         * Horizontal space
         */
        public static readonly char SP_CHAR = (char)SP;

        HttpConstants()
        {
            // Unused
        }
    }
}