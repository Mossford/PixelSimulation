﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SpatialGame
{
    public static class ParticleMovementDefines
    {
#if RELEASE
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static void Update(ref Particle particle)
        {
            int num = ParticleSimulation.GetRandomNumberOnPos(particle.position); // choose random size to pick to favor instead of always left
            int posCheckBelow = ParticleSimulation.SafePositionCheckGet(new Vector2(particle.position.X, particle.position.Y + 1));
            bool displaceLiq = posCheckBelow == ParticleBehaviorType.liquid.ToByte() || posCheckBelow == ParticleBehaviorType.gas.ToByte();
            //swapping down with a liquid
            if (displaceLiq)
            {
                particle.SwapParticle(new Vector2(particle.position.X, particle.position.Y + 1), (ParticleBehaviorType)posCheckBelow);
                return;
            }
            int posCheckLU = ParticleSimulation.SafePositionCheckGet(new Vector2(particle.position.X - 1, particle.position.Y + 1));
            bool displaceLiqLU = posCheckLU == ParticleBehaviorType.liquid.ToByte() || posCheckLU == ParticleBehaviorType.gas.ToByte() && posCheckBelow == ParticleBehaviorType.empty.ToByte();
            if (displaceLiqLU && num == 0)
            {
                particle.SwapParticle(new Vector2(particle.position.X - 1, particle.position.Y + 1), (ParticleBehaviorType)posCheckLU);
                return;
            }
            int posCheckRU = ParticleSimulation.SafePositionCheckGet(new Vector2(particle.position.X + 1, particle.position.Y + 1));
            bool displaceLiqRU = posCheckRU == ParticleBehaviorType.liquid.ToByte() || posCheckRU == ParticleBehaviorType.gas.ToByte() && posCheckBelow == ParticleBehaviorType.empty.ToByte();
            if (displaceLiqRU && num == 1)
            {
                particle.SwapParticle(new Vector2(particle.position.X + 1, particle.position.Y + 1), (ParticleBehaviorType)posCheckRU);
                return;
            }
            
            //check around particle for collision
            
            //if there is air under the solid
            bool inAir = posCheckBelow == ParticleBehaviorType.empty.ToByte();
            float velocityMag = particle.pastVelocity.Length();
            
            if (inAir == false)
            {
                particle.velocity = new Vector2(0, -velocityMag * particle.state.yBounce);
                if (particle.velocity.Length() < 0.01f)
                {
                    bool LUnder = posCheckLU == ParticleBehaviorType.empty.ToByte();
                    bool RUnder = posCheckRU == ParticleBehaviorType.empty.ToByte();
                    
                    if (LUnder && num == 0)
                    {
                        particle.MoveParticleOne(new Vector2(-1, 1));
                    }
                    else if (RUnder && num == 1)
                    {
                        particle.MoveParticleOne(new Vector2(1, 1));
                    }
                }
            }
            
        }
    }
}
