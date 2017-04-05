// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Microbench.Utilities
{
    using System;

    sealed class Blackhole
    {
        public int Tlr;
        public volatile int TlrMask;
        public volatile object Obj1;
        public volatile object[] Objs1;

        public Blackhole()
        {
            var r = new Random();
            this.Tlr = r.Next();
            this.TlrMask = 1;
            this.Obj1 = new object();
            this.Objs1 = new [] { new object() };
        }

        public void Consume(object obj)
        {
            int tlrMask = this.TlrMask; // volatile read
            int tlr = (this.Tlr = (this.Tlr * 1664525 + 1013904223));
            if ((tlr & tlrMask) == 0)
            {
                // SHOULD ALMOST NEVER HAPPEN IN MEASUREMENT
                this.Obj1 = obj;
                this.TlrMask = (tlrMask << 1) + 1;
            }
        }

        public void Consume(object[] objs)
        {
            int tlrMask = this.TlrMask; // volatile read
            int tlr = (this.Tlr = (this.Tlr * 1664525 + 1013904223));
            if ((tlr & tlrMask) == 0)
            {
                // SHOULD ALMOST NEVER HAPPEN IN MEASUREMENT
                this.Objs1 = objs;
                this.TlrMask = (tlrMask << 1) + 1;
            }
        }
    }
}
