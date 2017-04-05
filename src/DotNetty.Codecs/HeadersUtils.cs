// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs
{
    using System.Collections.Generic;
    using DotNetty.Common.Utilities;

    public static class HeadersUtils
    {
        public static List<string> GetAllAsString<TKey, TValue>(IHeaders<TKey, TValue> headers, TKey name)
        {
            IList<TValue> allNames = headers.GetAll(name);
            var values = new List<string>();

            // ReSharper disable once ForCanBeConvertedToForeach
            // Avoid enumerator allocation
            for (int i = 0; i < allNames.Count; i++)
            {
                TValue value = allNames[i];
                values.Add(value?.ToString());
            }

            return values;
        }

        public static string GetAsString<TKey, TValue>(IHeaders<TKey, TValue> headers, TKey name)
        {
            TValue orig = headers.Get(name);
            return orig?.ToString();
        }

        public static IList<string> NamesAsString(IHeaders<ICharSequence, ICharSequence> headers)
        {
            ISet<ICharSequence> allNames = headers.Names();

            var names = new List<string>();

            foreach (ICharSequence name in allNames)
            {
                names.Add(name.ToString());
            }

            return names;
        }
    }
}
