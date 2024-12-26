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

            UiRenderer.Init();
            ParticleResourceHandler.Init();
            SimTextHandler.Init();
            PixelColorer.Init(changeResolution);
            ParticleHeatSim.Init();
            ParticleSimulation.InitParticleSim();
            SimRenderer.Init();
            SimInput.Init();
            //RigidBodySimulation.Init();
            
            //RigidBodySimulation.bodies.Add(new SimBody(new Vector2(100, 50), 10f, 0f));

            isInitalizing = false;
        }
        
        public static void InitGame()
        {
            isInitalizing = true;
            timeSinceLastInit = GetTime();
            UiRenderer.Init();
            ParticleResourceHandler.Init();
            SimTextHandler.Init();
            PixelColorer.Init(false);
            ParticleHeatSim.Init();
            ParticleSimulation.InitParticleSim();
            SimRenderer.Init();
            SimInput.Init();
            //RigidBodySimulation.Init();
            
            //RigidBodySimulation.bodies.Add(new SimBody(new Vector2(103, 51.5f), 10f, 0f));

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

        public static void RenderGame()
        {
            //renders the pixels and simulation
            PixelColorer.Render();
            //renders some simulation ui stuff
            SimRenderer.Render();
        }

        public static void FixedUpdateGame(float dt)
        {
            SimInput.Update();
            SimInput.FixedUpdate();
            ParticleSimulation.RunParticleSim(dt);
            //RigidBodySimulation.Update(dt / 1000f);
        }
    }
}
