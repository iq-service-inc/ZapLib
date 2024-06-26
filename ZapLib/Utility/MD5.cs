﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace ZapLib.Utility
{
    /// <summary>
    /// 作者自行實作的 MD5 雜湊功能
    /// </summary>
    public static class MD5
    {
        /*
         * Round shift values
         */
        private static int[] s = new int[64] {
            7, 12, 17, 22,  7, 12, 17, 22,  7, 12, 17, 22,  7, 12, 17, 22,
            5,  9, 14, 20,  5,  9, 14, 20,  5,  9, 14, 20,  5,  9, 14, 20,
            4, 11, 16, 23,  4, 11, 16, 23,  4, 11, 16, 23,  4, 11, 16, 23,
            6, 10, 15, 21,  6, 10, 15, 21,  6, 10, 15, 21,  6, 10, 15, 21
        };

        /*
         * Constant K Values
         */
        private static uint[] K = new uint[64] {
            0xd76aa478, 0xe8c7b756, 0x242070db, 0xc1bdceee,
            0xf57c0faf, 0x4787c62a, 0xa8304613, 0xfd469501,
            0x698098d8, 0x8b44f7af, 0xffff5bb1, 0x895cd7be,
            0x6b901122, 0xfd987193, 0xa679438e, 0x49b40821,
            0xf61e2562, 0xc040b340, 0x265e5a51, 0xe9b6c7aa,
            0xd62f105d, 0x02441453, 0xd8a1e681, 0xe7d3fbc8,
            0x21e1cde6, 0xc33707d6, 0xf4d50d87, 0x455a14ed,
            0xa9e3e905, 0xfcefa3f8, 0x676f02d9, 0x8d2a4c8a,
            0xfffa3942, 0x8771f681, 0x6d9d6122, 0xfde5380c,
            0xa4beea44, 0x4bdecfa9, 0xf6bb4b60, 0xbebfbc70,
            0x289b7ec6, 0xeaa127fa, 0xd4ef3085, 0x04881d05,
            0xd9d4d039, 0xe6db99e5, 0x1fa27cf8, 0xc4ac5665,
            0xf4292244, 0x432aff97, 0xab9423a7, 0xfc93a039,
            0x655b59c3, 0x8f0ccc92, 0xffeff47d, 0x85845dd1,
            0x6fa87e4f, 0xfe2ce6e0, 0xa3014314, 0x4e0811a1,
            0xf7537e82, 0xbd3af235, 0x2ad7d2bb, 0xeb86d391
        };

        private static uint LeftRotate(uint x, int c)
        {
            return (x << c) | (x >> (32 - c));
        }

        /// <summary>
        /// 作者自行實作的 MD5 計算
        /// </summary>
        /// <param name="input">先將字串 byte 化傳入計算</param>
        /// <returns></returns>
        public static byte[] ComputeHash(byte[] input)
        {
            uint a0 = 0x67452301;   // A
            uint b0 = 0xefcdab89;   // B
            uint c0 = 0x98badcfe;   // C
            uint d0 = 0x10325476;   // D

            var processedInputBuilder = new List<byte>(input) { 0x80 };
            while (processedInputBuilder.Count % 64 != 56) processedInputBuilder.Add(0x0);
            processedInputBuilder.AddRange(BitConverter.GetBytes((long)input.Length * 8)); // bit converter returns little-endian
            var processedInput = processedInputBuilder.ToArray();

            for (int i = 0; i < processedInput.Length / 64; ++i)
            {
                // copy the input to M
                uint[] M = new uint[16];
                for (int j = 0; j < 16; ++j)
                    M[j] = BitConverter.ToUInt32(processedInput, (i * 64) + (j * 4));

                // initialize round variables
                uint A = a0, B = b0, C = c0, D = d0, F = 0, g = 0;

                // primary loop
                for (uint k = 0; k < 64; ++k)
                {
                    if (k <= 15)
                    {
                        F = (B & C) | (~B & D);
                        g = k;
                    }
                    else if (k >= 16 && k <= 31)
                    {
                        F = (D & B) | (~D & C);
                        g = ((5 * k) + 1) % 16;
                    }
                    else if (k >= 32 && k <= 47)
                    {
                        F = B ^ C ^ D;
                        g = ((3 * k) + 5) % 16;
                    }
                    else if (k >= 48)
                    {
                        F = C ^ (B | ~D);
                        g = (7 * k) % 16;
                    }

                    var dtemp = D;
                    D = C;
                    C = B;
                    B = B + LeftRotate((A + F + K[k] + M[g]), s[k]);
                    A = dtemp;
                }

                a0 += A;
                b0 += B;
                c0 += C;
                d0 += D;
            }

            var a = BitConverter.GetBytes(a0);
            var b = BitConverter.GetBytes(b0);
            var c = BitConverter.GetBytes(c0);
            var d = BitConverter.GetBytes(d0);

            var all = new List<byte>();
            all.AddRange(a);
            all.AddRange(b);
            all.AddRange(c);
            all.AddRange(d);

            return all.ToArray();
        }

        private static string GetByteString(uint x)
        {
            return string.Join("", BitConverter.GetBytes(x).Select(y => y.ToString("x2")));
        }
    }
}
