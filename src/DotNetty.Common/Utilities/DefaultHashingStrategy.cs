// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Common.Utilities
{
    public sealed class DefaultHashingStrategy<T> : IHashingStrategy<T>
    {
        public bool Equals(T a, T b) => ReferenceEquals(a, b) || (a != null && a.Equals(b));

        public int GetHashCode(T obj) => obj?.GetHashCode() ?? 0;
    }
}
