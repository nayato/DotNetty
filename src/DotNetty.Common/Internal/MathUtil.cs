// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Common.Internal
{
    /// <summary>
    ///     Math utility methods.
    /// </summary>
    public static class MathUtil
    {
        /**
         * Determine if the requested {@code index} and {@code length} will fit within {@code capacity}.
         * @param index The starting index.
         * @param length The length which will be utilized (starting from {@code index}).
         * @param capacity The capacity that {@code index + length} is allowed to be within.
         * @return {@code true} if the requested {@code index} and {@code length} will fit within {@code capacity}.
         * {@code false} if this would result in an index out of bounds exception.
         */

        public static bool isOutOfBounds(int index, int length, int capacity) => (index | length | (index + length) | (capacity - (index + length))) < 0;

        /**
         * Compare to {@code long} values.
         * @param x the first {@code long} to compare.
         * @param y the second {@code long} to compare.
         * @return
         * <ul>
         * <li>0 if {@code x == y}</li>
         * <li>{@code > 0} if {@code x > y}</li>
         * <li>{@code < 0} if {@code x < y}</li>
         * </ul>
         */

        public static int Compare(long x, long y) => (x < y) ? -1 : (x > y) ? 1 : 0;
    }
}