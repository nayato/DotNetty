// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Common.Utilities
{
    using System.Collections.Generic;
    using System.Text;

    public interface CharSequence : IReadOnlyList<char>
    {
        int Length { get; }

        void CopyTo(char[] array);

        int Encode(Encoding encoding, int startIndex, int length, byte[] destination, int destinationStartIndex);

        bool IsEmpty { get; }
    }
}