﻿using SpatialEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SpatialGame
{
    public class ParticleChunk
    {
        //possibly try using a span so that its cache friendly more than an array?
        public Particle[] particles;
        public Queue<int> freeParticleSpots;
        /// <summary>
        /// the type of the particle at its position
        /// 0 is no pixel at position,
        /// 1 is movable solid at position,
        /// 2 is movable liquid at position,
        /// 3 is movable gas at position,
        /// 100 is a unmovable at position
        /// </summary>
        public byte[] positionCheck;
        /// <summary>
        /// the ids of the particles at their position
        /// </summary>
        public int[] idCheck;
        /// <summary>
        /// Queue of particles that will be deleted
        /// </summary>
        public List<int> idsToDelete;
        /// <summary>
        /// avaliable spots count
        /// </summary>
        public int particleSpotCount;
        /// <summary>
        /// Current updating particles ie. not empty
        /// </summary>
        public int particleCount;
        public Vector2 position;

        public void Init()
        {
            particleSpotCount = ParticleChunkManager.chunkSizeWidth * ParticleChunkManager.chunkSizeHeight;
            particleCount = ParticleChunkManager.chunkSizeWidth * ParticleChunkManager.chunkSizeHeight;
            particles = new Particle[particleSpotCount];

            freeParticleSpots = new Queue<int>();
            positionCheck = new byte[particleSpotCount];
            idCheck = new int[particleSpotCount];

            //tell the queue that all spots are avaliable
            for (int i = 0; i < particles.Length; i++)
            {
                freeParticleSpots.Enqueue(i);
            }

            for (int i = 0; i < particles.Length; i++)
            {
                particles[i] = new Particle();
            }
        }

        public void InitSecond()
        {
            for (int i = 0; i < particles.Length; i++)
            {
                if (particles[i].id.particleIndex == -1 || !particles[i].BoundsCheck(particles[i].position))
                    continue;

                particles[i].CheckDoubleOnPosition();
            }

            DeleteParticlesOnQueue();

            for (int i = 0; i < particles.Length; i++)
            {
                if (particles[i].id.particleIndex == -1 || !particles[i].BoundsCheck(particles[i].position))
                    continue;
                particleCount++;
            }
        }

        public void UpdateParticles()
        {
            particleCount = 0;
            for (int i = 0; i < particles.Length; i++)
            {
                //reset all lights
                if (Settings.SimulationSettings.EnableParticleLighting)
                {
                    PixelColorer.particleLights[i].index = 0;
                    PixelColorer.particleLights[i].intensity = 1;
                    PixelColorer.particleLights[i].color = new Vector4Byte(255, 255, 255, 255);
                    PixelColorer.particleLights[i].range = 2;
                }

                if (particles[i].id.particleIndex == -1 || !particles[i].BoundsCheck(particles[i].position))
                    continue;

                PixelColorer.SetColorAtPos(particles[i].position, 102, 178, 204);
                particles[i].Update();

                //check if we are still in this chunk
                if (!ChunkBounds(particles[i].position))
                {
                    //if failed
                    //then set it to not be updated again to avoid a double update
                    //and remove from own chunk and add to other chunk
                    
                    //check old position id
                    //if -1 then we have no particle there and we can then add our current particle
                    //to the next chunk that it moved into
                    //if there is an id and it does not match the id of the current particle
                    //then we have swapped particles
                    //do nothing?
                    //if do nothing then that depends if the movement code has properly set the states and shit
                }

                //reset its light color before it moves

                particles[i].UpdateGeneralFirst();
                particleCount++;
            }

            for (int i = 0; i < particles.Length; i++)
            {
                if (particles[i].id.particleIndex == -1 || !particles[i].BoundsCheck(particles[i].position))
                    continue;

                particles[i].UpdateGeneralSecond();
                if (particles[i].id.particleIndex == -1 || particles[i].BoundsCheck(particles[i].position))
                {
                    //apply transparencys to particle
                    //blend with background by the alpha
                    float alphaScale = 1f - (particles[i].state.color.w / 255f);
                    Vector3 color = Vector3.Lerp((Vector3)particles[i].state.color / 255f, new Vector3(102 / 255f, 178 / 255f, 204 / 255f), alphaScale) * 255f;
                    PixelColorer.SetColorAtPos(particles[i].position, (byte)color.X, (byte)color.Y, (byte)color.Z);

                }
            }

            DeleteParticlesOnQueue();
        }

        void DeleteParticlesOnQueue()
        {
            if (idsToDelete.Count == 0)
                return;

            for (int i = 0; i < idsToDelete.Count; i++)
            {
                int id = idsToDelete[i];
                if (particles[i].id.particleIndex == -1)
                    continue;
                if (id >= 0 && id < particles.Length && particles[id].toBeDeleted)
                {
                    particles[id].Delete();
                }
            }

            idsToDelete.Clear();
        }


#if RELEASE
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public bool ChunkBounds(Vector2 pos)
        {
            if(pos.X > position.X + ParticleChunkManager.chunkSizeWidth && pos.Y > position.Y + ParticleChunkManager.chunkSizeHeight &&
                pos.X < position.X - ParticleChunkManager.chunkSizeWidth && pos.Y < position.Y - ParticleChunkManager.chunkSizeHeight)
                return false;
            return true;
        }

#if RELEASE
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public int GetParticleIndex(Vector2 pos)
        {
            pos -= new Vector2(position.X - ParticleChunkManager.chunkSizeWidth, position.Y - ParticleChunkManager.chunkSizeHeight);
            int particleIndex = (int)(ParticleChunkManager.chunkSizeHeight * MathF.Floor(pos.X) + MathF.Floor(pos.Y));
            return particleIndex;
        }

    }
}
