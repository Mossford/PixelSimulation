﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace SpatialGame
{
    public static class UnmoveableBehaviorDefines
    {

#if RELEASE
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static void Update(ref Particle particle)
        {
            
        }
    }
}
