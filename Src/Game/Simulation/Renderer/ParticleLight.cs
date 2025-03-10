﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpatialGame
{
    public struct ParticleLight
    {
        public int index {  get; set; } // Position of the light decoded in shader (4 bytes)
        public Vector4Byte color { get; set; } // Color of the light decoded in shader (4 bytes)
        public float intensity { get; set; } // Intensity of the light (4 bytes)
        /// <summary>
        /// cant see any difference above 3
        /// </summary>
        public int range { get; set; } // Range of the light (4 bytes)

        public ParticleLight()
        {
            index = -1;
            color = new Vector4Byte(255, 255, 255, 255);
            intensity = 1;
            range = 0;
        }

        public static int getSize()
        {
            return 16;
        }

        public override string ToString()
        {
            return "Index: " + index + "\n" + 
                   "Color: " + color + "\n" + 
                   "Intensity: " + intensity + "\n" + 
                   "Range: " + range + "\n" ;
        }
    }
}
