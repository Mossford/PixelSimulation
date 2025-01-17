﻿
using System;
using System.Numerics;
using Silk.NET.Input;

namespace SpatialEngine
{
    public static class Mouse
    {
        public static IMouse mouse;
        public static Vector2 position;
        public static Vector2 localPosition;
        public static Vector2 lastPosition;
        public static Vector2 lastLocalPosition;
        public static bool uiWantMouse;

        public static void Init()
        {
            mouse = Input.input.Mice[0];
        }
        
        public static void Update()
        {
            lastPosition = position;
            lastLocalPosition = ((lastPosition * 2) - Window.size) / 2;
            position = mouse.Position * ((Vector2)Globals.snWindow.FramebufferSize / (Vector2)Globals.snWindow.Size);
            localPosition = ((position * 2) - Window.size) / 2;
        }
    }
}