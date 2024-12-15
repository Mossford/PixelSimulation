﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

using SpatialEngine;
using SpatialEngine.Rendering;
using static SpatialEngine.Globals;
using static SpatialEngine.Rendering.MeshUtils;
using static SpatialEngine.Resources;

namespace SpatialGame
{
    public static class GameManager
    {
        //move this?
        public static bool changeResolution;
        public static bool started;
        public static bool isInitalizing;
        public static float timeSinceLastInit;

        public static void ReInitGame()
        {
            //if (isInitalizing || (GetTime() - timeSinceLastInit) <= 1f)
            //   return;
            isInitalizing = true;
            timeSinceLastInit = GetTime();
            if (started)
            {
                PixelColorer.CleanUp();
                SimRenderer.CleanUp();
                SimInput.CleanUp();
            }

            ParticleResourceHandler.Init();
            SimTextHandler.Init();
            PixelColorer.Init(changeResolution);
            ParticleHeatSim.Init();
            ParticleSimulation.InitParticleSim();
            SimRenderer.Init();
            SimInput.Init();

            isInitalizing = false;
        }

        public static void InitGame()
        {
            isInitalizing = true;
            timeSinceLastInit = GetTime();
            ParticleResourceHandler.Init();
            SimTextHandler.Init();
            PixelColorer.Init(false);
            ParticleHeatSim.Init();
            ParticleSimulation.InitParticleSim();
            SimRenderer.Init();
            SimInput.Init();

            started = true;
            isInitalizing = false;
        }

        public static void UpdateGame(float dt)
        {
            PixelColorer.Update();
            SimRenderer.Update();
            //SimInput.Update();
            //SimRenderer.UpdateMeshes();
        }

        public static void FixedUpdateGame(float dt)
        {
            SimInput.Update();
            SimInput.FixedUpdate();
            ParticleSimulation.RunParticleSim(dt);
        }
    }
}
