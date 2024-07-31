﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using JoltPhysicsSharp;

using SpatialEngine;
using SpatialEngine.Rendering;
using SpatialEngine.Terrain;
using static SpatialEngine.Globals;
using static SpatialEngine.Rendering.MeshUtils;
using static SpatialEngine.Resources;
using static SpatialEngine.Terrain.Terrain;

namespace SpatialGame
{
    public static class GameManager
    {
        public static void InitGame()
        {
            PixelColorer.Init();
            ElementSimulation.InitPixelSim();
        }

        public static void UpdateGame(float dt)
        {
            GameInput.Update(dt);
            ElementSimulation.RunPixelSim();

            PixelColorer.Update();
            //PixelColorer.ResetBackground();
        }

        public static void FixedUpdateGame(float dt)
        {

        }
    }
}
