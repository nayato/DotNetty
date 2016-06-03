// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Common.Internal
{
    using System;
    using DotNetty.Common.Utilities;

    public static class PlatformDependent
    {
        // constants borrowed from murmur3
        const int HASH_CODE_ASCII_SEED = -1028477387;
        const int HASH_CODE_C1 = 0x1b873593;
        const int HASH_CODE_C2 = 0x1b873593;

        public static IQueue<T> NewFixedMpscQueue<T>(int capacity) where T : class => new MpscArrayQueue<T>(capacity);

        public static IQueue<T> NewMpscQueue<T>() where T : class => new CompatibleConcurrentQueue<T>();

        internal static unsafe bool equals(byte[] bytes1, int startPos1, byte[] bytes2, int startPos2, int length)
        {
            if (length == 0)
            {
                return true;
            }

            fixed (byte* b1 = bytes1)
            fixed (byte* b2 = bytes2)
            {
                byte* bc1 = b1 + startPos1;
                byte* bc2 = b2 + startPos2;
                int remainingBytes = length & 7;
                byte* end = bc1 + remainingBytes;
                for (long* i = (long*)(bc1 + length - 8), j = (long*)(bc2 + length - 8); i >= end; i--, j--)
                {
                    if (*i != *j)
                    {
                        return false;
                    }
                }
                switch (remainingBytes)
                {
                    case 7:
                        return (*(int*)(bc1 + 3) == *(int*)(bc2 + 3)) &&
                            (*(char*)(bc1 + 1) == *(char*)(bc2 + 1)) &&
                            (*bc1 == *bc2);
                    case 6:
                        return (*(int*)(bc1 + 2) == *(int*)(bc2 + 2)) &&
                            (*(char*)bc1 == *(char*)bc2);
                    case 5:
                        return (*(int*)(bc1 + 1) == *(int*)(bc2 + 1)) &&
                            (*bc1 == *bc2);
                    case 4:
                        return *(int*)bc1 == *(int*)bc2;
                    case 3:
                        return (*(char*)(bc1 + 1) == *(char*)(bc2 + 1)) &&
                            (*bc1 == *bc2);
                    case 2:
                        return *(char*)bc1 == *(char*)bc2;
                    case 1:
                        return *bc1 == *bc2;
                    default:
                        return true;
                }
            }
        }

        internal static unsafe int hashCodeAscii(byte[] bytes, int startPos, int length)
        {
            fixed (byte* b = bytes)
            {
                byte* bc = b + startPos;

                int hash = HASH_CODE_ASCII_SEED;
                int remainingBytes = length & 7;
                if (length > 7)
                {
                    // Fast path for small sized inputs. Benchmarking shows this is beneficial.
                    byte* end = bc + remainingBytes;

                    for (var i = (long*)(bc + length - 8); i >= end; i--)
                    {
                        hash = hashCodeAsciiCompute(*i, hash);
                    }
                }
                switch (remainingBytes)
                {
                    case 7:
                        return ((hash * 31 + IntegerExtensions.RotateLeft(hashCodeAsciiSanitize(*(int*)(bc + 3)), 13))
                                * 31 + hashCodeAsciiSanitize(*(short*)(bc + 1)))
                            * 31 + hashCodeAsciiSanitize(*bc);
                    case 6:
                        return (hash * 31 + IntegerExtensions.RotateLeft(hashCodeAsciiSanitize(*(int*)(bc + 2)), 13))
                            * 31 + hashCodeAsciiSanitize(*(short*)bc);
                    case 5:
                        return (hash * 31 + IntegerExtensions.RotateLeft(hashCodeAsciiSanitize(*(int*)(bc + 1)), 13))
                            * 31 + hashCodeAsciiSanitize(*bc);
                    case 4:
                        return hash * 31 + hashCodeAsciiSanitize(*(int*)bc);
                    case 3:
                        return (hash * 31 + hashCodeAsciiSanitize(*(short*)(bc + 1)))
                            * 31 + hashCodeAsciiSanitize(*bc);
                    case 2:
                        return hash * 31 + hashCodeAsciiSanitize(*(short*)bc);
                    case 1:
                        return hash * 31 + hashCodeAsciiSanitize(*bc);
                    default:
                        return hash;
                }
            }
        }

        /**
         * Calculate a hash code of a byte array assuming ASCII character encoding.
         * The resulting hash code will be case insensitive.
         * <p>
         * This method assumes that {@code bytes} is equivalent to a {@code byte[]} but just using {@link CharSequence}
         * for storage. The upper most byte of each {@code char} from {@code bytes} is ignored.
         * @param bytes The array which contains the data to hash (assumed to be equivalent to a {@code byte[]}).
         * @return The hash code of {@code bytes} assuming ASCII character encoding.
         * The resulting hash code will be case insensitive.
         */

        public static int hashCodeAscii(CharSequence bytes)
        {
            int hash = HASH_CODE_ASCII_SEED;
            int remainingBytes = bytes.Length & 7;
            // Benchmarking shows that by just naively looping for inputs 8~31 bytes long we incur a relatively large
            // performance penalty (only achieve about 60% performance of loop which iterates over each char). So because
            // of this we take special provisions to unroll the looping for these conditions.
            switch (bytes.Length)
            {
                case 31:
                case 30:
                case 29:
                case 28:
                case 27:
                case 26:
                case 25:
                case 24:
                    hash = hashCodeAsciiCompute(bytes, bytes.Length - 24,
                        hashCodeAsciiCompute(bytes, bytes.Length - 16,
                            hashCodeAsciiCompute(bytes, bytes.Length - 8, hash)));
                    break;
                case 23:
                case 22:
                case 21:
                case 20:
                case 19:
                case 18:
                case 17:
                case 16:
                    hash = hashCodeAsciiCompute(bytes, bytes.Length - 16,
                        hashCodeAsciiCompute(bytes, bytes.Length - 8, hash));
                    break;
                case 15:
                case 14:
                case 13:
                case 12:
                case 11:
                case 10:
                case 9:
                case 8:
                    hash = hashCodeAsciiCompute(bytes, bytes.Length - 8, hash);
                    break;
                case 7:
                case 6:
                case 5:
                case 4:
                case 3:
                case 2:
                case 1:
                case 0:
                    break;
                default:
                    for (int i = bytes.Length - 8; i >= remainingBytes; i -= 8)
                    {
                        hash = hashCodeAsciiCompute(bytes, i, hash);
                    }
                    break;
            }
            switch (remainingBytes)
            {
                case 7:
                    return ((hash * HASH_CODE_C1 + hashCodeAsciiSanitizsByte(bytes[0]))
                            * HASH_CODE_C2 + hashCodeAsciiSanitizeShort(bytes, 1))
                        * HASH_CODE_C1 + hashCodeAsciiSanitizeInt(bytes, 3);
                case 6:
                    return (hash * HASH_CODE_C1 + hashCodeAsciiSanitizeShort(bytes, 0))
                        * HASH_CODE_C2 + hashCodeAsciiSanitizeInt(bytes, 2);
                case 5:
                    return (hash * HASH_CODE_C1 + hashCodeAsciiSanitizsByte(bytes[0]))
                        * HASH_CODE_C2 + hashCodeAsciiSanitizeInt(bytes, 1);
                case 4:
                    return hash * HASH_CODE_C1 + hashCodeAsciiSanitizeInt(bytes, 0);
                case 3:
                    return (hash * HASH_CODE_C1 + hashCodeAsciiSanitizsByte(bytes[0]))
                        * HASH_CODE_C2 + hashCodeAsciiSanitizeShort(bytes, 1);
                case 2:
                    return hash * HASH_CODE_C1 + hashCodeAsciiSanitizeShort(bytes, 0);
                case 1:
                    return hash * HASH_CODE_C1 + hashCodeAsciiSanitizsByte(bytes[0]);
                default:
                    return hash;
            }
        }

        static int hashCodeAsciiCompute(long value, int hash)
        {
            // masking with 0x1f reduces the number of overall bits that impact the hash code but makes the hash
            // code the same regardless of character case (upper case or lower case hash is the same).
            return hash * HASH_CODE_C1 +
                // Low order int
                hashCodeAsciiSanitize((int)value) * HASH_CODE_C2 +
                // High order int
                (int)((value & 0x1f1f1f1f00000000L).RightUShift(32));
        }

        static int hashCodeAsciiSanitize(int value) => value & 0x1f1f1f1f;

        static int hashCodeAsciiSanitize(short value) => value & 0x1f1f;

        static int hashCodeAsciiSanitize(byte value) => value & 0x1f;

        /**
                 * Identical to {@link PlatformDependent0#hashCodeAsciiCompute(long, int)} but for {@link CharSequence}.
                 */

        static int hashCodeAsciiCompute(CharSequence value, int offset, int hash)
        {
            // masking with 0x1f reduces the number of overall bits that impact the hash code but makes the hash
            // code the same regardless of character case (upper case or lower case hash is the same).
            return hash * HASH_CODE_C1 +
                // Low order int
                hashCodeAsciiSanitizeInt(value, offset) * HASH_CODE_C2 +
                // High order int
                hashCodeAsciiSanitizeInt(value, offset + 4);
        }

        /**
         * Identical to {@link PlatformDependent0#hashCodeAsciiSanitize(int)} but for {@link CharSequence}.
         */

        static int hashCodeAsciiSanitizeInt(CharSequence value, int offset)
        {
            if (!BitConverter.IsLittleEndian)
            {
                // mimic a unsafe.getInt call on a big endian machine
                return (value[offset] & 0x1f) |
                    ((value[offset + 2] & 0x1f) << 8) |
                    ((value[offset + 1] & 0x1f) << 16) |
                    ((value[offset] & 0x1f) << 24);
            }
            return ((value[offset + 3] & 0x1f) << 24) |
                ((value[offset + 2] & 0x1f) << 16) |
                ((value[offset + 1] & 0x1f) << 8) |
                (value[offset] & 0x1f);
        }

        /**
         * Identical to {@link PlatformDependent0#hashCodeAsciiSanitize(short)} but for {@link CharSequence}.
         */

        static int hashCodeAsciiSanitizeShort(CharSequence value, int offset)
        {
            if (!BitConverter.IsLittleEndian)
            {
                // mimic a unsafe.getShort call on a big endian machine
                return (value[offset + 1] & 0x1f) |
                    ((value[offset] & 0x1f) << 8);
            }
            return ((value[offset + 1] & 0x1f) << 8) |
                (value[offset] & 0x1f);
        }

        /**
         * Identical to {@link PlatformDependent0#hashCodeAsciiSanitize(byte)} but for {@link CharSequence}.
         */

        static int hashCodeAsciiSanitizsByte(char value)
        {
            return value & 0x1f;
        }
    }
}