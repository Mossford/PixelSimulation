﻿using System;
using Silk.NET.Windowing;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using ImGuiNET;
using Silk.NET.OpenGL.Extensions.ImGui;
using System.Drawing;
using System.IO;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using JoltPhysicsSharp;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;


//Custom Engine things
using static SpatialEngine.Rendering.MeshUtils;
using static SpatialEngine.Rendering.MainImGui;
using static SpatialEngine.Resources;
using static SpatialEngine.Globals;
using SpatialEngine.Networking;
using SpatialEngine.Rendering;
//silk net has its own shader for some reason
using Shader = SpatialEngine.Rendering.Shader;
using Texture = SpatialEngine.Rendering.Texture;
using static SpatialEngine.Debugging;
using static SpatialEngine.Input;
using SpatialGame;

namespace SpatialEngine
{

    public static class Globals
    {
        public static GL gl;
        public static GraphicsAPI glApi = GraphicsAPI.Default;
        public static IWindow window;
        public const int SCR_WIDTH = 1920;
        public const int SCR_HEIGHT = 1080;
        public static string EngVer = "PAR:0.1 | ENG:0.6.8 Stable";
        public static string OpenGlVersion = "";
        public static string Gpu = "";
        
        public static Scene scene;
        public static Physics physics;
        public static PhysicsSystem physicsSystem;
        public static BodyInterface bodyInterface;

        public static bool showWireFrame = false;
        //going to be true because my gpu squeals if vsync is off
        public static bool vsync = false;
        public static uint vertCount;
        public static uint indCount;

        public static Player player;

        public static uint DrawCallCount = 0;
        public static float totalTime = 0.0f;
        public static float deltaTime = 0.0f;

        /// <summary>
        /// In Seconds
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetTime() => totalTime;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetDeltaTime() => deltaTime;
    }

    public class Game
    {
        static ImGuiController controller;
        static Vector2 LastMousePosition;
        static Shader shader;
        static Shader depthShader;

        public static void Main(string[] args)
        {
            //init resources like resources paths
            InitResources();

            if (args.Length == 0)
            {
                glApi.Version = new APIVersion(4, 3);
                WindowOptions options = WindowOptions.Default with
                {
                    Size = new Vector2D<int>(SCR_WIDTH, SCR_HEIGHT),
                    Title = "SpatialParticles",
                    VSync = vsync,
                    PreferredDepthBufferBits = 24,
                    API = glApi,
                };
                window = Window.Create(options);
                window.Load += OnLoad;
                window.Update += OnUpdate;
                window.Render += OnRender;
                window.Run();
            }
            else if (args[0] == "server")
            {
                //handles all the initilization of scene and physics and netowrking for server
                //and starts running the update loop
                HeadlessServer.Init();
            }

            scene.SaveScene(ScenePath, "Main.txt");
            physics.CleanPhysics(ref scene);
            NetworkManager.Cleanup();
        }

        static unsafe void OnLoad() 
        {
            gl = window.CreateOpenGL();
            //gl = GL.GetApi(window);
            byte* text = gl.GetString(GLEnum.Renderer);
            int textLength = 0;
            while (text[textLength] != 0)
                textLength++;
            byte[] textArray = new byte[textLength];
            Marshal.Copy((IntPtr)text, textArray, 0, textLength);
            Gpu = System.Text.Encoding.Default.GetString(textArray);
            text = gl.GetString(GLEnum.Version);
            textLength = 0;
            while (text[textLength] != 0)
                textLength++;
            textArray = new byte[textLength];
            Marshal.Copy((IntPtr)text, textArray, 0, textLength);
            OpenGlVersion = System.Text.Encoding.Default.GetString(textArray);
            gl.Enable(GLEnum.DepthTest);
            gl.Enable(GLEnum.Texture2D);
            gl.Enable(GLEnum.CullFace);
            gl.Enable(GLEnum.DebugOutput);
            gl.DebugMessageCallback(DebugProc, null);
            gl.DebugMessageControl(GLEnum.DontCare, GLEnum.DontCare, GLEnum.DebugSeverityNotification, 0, null, false);

            //init systems
            scene = new Scene();
            physics = new Physics();
            physics.InitPhysics();

            Renderer.Init(scene);
            shader = new Shader(gl, "Default.vert", "Default.frag");
            depthShader = new Shader(gl, "Depth.vert", "Depth.frag");

            NetworkManager.Init();

            player = new Player(15.0f, new Vector3(-33, 12, -20), new Vector3(300, 15, 0));
            
            //input stuffs
            Input.Init();
            for (int i = 0; i < input.Keyboards.Count; i++)
                input.Keyboards[i].KeyDown += KeyDown;
            for (int i = 0; i < input.Mice.Count; i++)
            {
                input.Mice[i].Cursor.CursorMode = CursorMode.Normal;
                input.Mice[i].MouseMove += OnMouseMove;
            }
            //imgui control stuff
            controller = new ImGuiController(gl, window, input);
            ImGui.SetWindowSize(new Vector2(400, 600));

            //init game
            GameManager.InitGame();
        }

        static bool lockMouse = false;
        static void KeyDown(IKeyboard keyboard, Key key, int keyCode)
        {
            if(!lockMouse && key == Key.Escape)
            {
                input.Mice.FirstOrDefault().Cursor.CursorMode = CursorMode.Raw;
                lockMouse = true;
            }
            else if(lockMouse && key == Key.Escape)
            {
                input.Mice.FirstOrDefault().Cursor.CursorMode = CursorMode.Normal;
                lockMouse = false;
            }
        }

        static unsafe void OnMouseMove(IMouse mouse, Vector2 position)
        {
            if(lockMouse)
            {
                Vector2 mousePosMoved = position - LastMousePosition;
                LastMousePosition = position;
                player.Look((int)mousePosMoved.X, (int)mousePosMoved.Y, false, false);
                LastMousePosition = position;
            }
        }

        static float totalTimeUpdate = 0.0f;
        static void OnUpdate(double dt)
        {
            totalTime += (float)dt;
            deltaTime = (float)dt;
            
            Input.Update();

            for (int i = 0; i < scene.SpatialObjects.Count; i++)
            {
                scene.SpatialObjects[i].SO_mesh.SetModelMatrix();
            }
            
            GameManager.UpdateGame((float)dt);

            totalTimeUpdate += (float)dt;
            while (totalTimeUpdate >= 0.0166f)
            {
                totalTimeUpdate -= 0.0166f;
                FixedUpdate(0.0166f);
            }
        
            Input.Clear();
        }

        static void FixedUpdate(float dt)
        {
            player.Movement(dt, keysPressed.ToArray());
            player.UpdatePlayer(dt);

            GameManager.FixedUpdateGame(dt);

            if (NetworkManager.didInit)
            {
                if(NetworkManager.isServer)
                {
                    NetworkManager.server.Update(dt);
                }
                else
                {
                    if(!NetworkManager.client.IsConnected())
                    {
                        physics.UpdatePhysics(ref scene, dt);
                    }
                    NetworkManager.client.Update(dt);
                }
            }
            else
            {
                physics.UpdatePhysics(ref scene, dt);
            }
        }

        static unsafe void OnRender(double dt)
        {   
            controller.Update((float)dt);

            player.camera.SetViewMat();
            player.camera.SetProjMat(window.Size.X, window.Size.Y);
            player.camera.SetProjMatClose(window.Size.X, window.Size.Y);

            ImGuiMenu((float)dt);

            gl.ClearColor(Color.FromArgb(102, 178, 204));
            gl.Viewport(0,0, (uint)window.Size.X, (uint)window.Size.Y);

            gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            gl.DepthFunc(GLEnum.Lequal);
            gl.PolygonMode(GLEnum.FrontAndBack, GLEnum.Fill);
            if(showWireFrame)
                gl.PolygonMode(GLEnum.FrontAndBack, GLEnum.Line);

            UiRenderer.Draw();

            //Renderer.Draw(scene, ref shader, player.camera.viewMat, player.camera.projMat, player.camera.position);

            //render players
            if(NetworkManager.didInit && !NetworkManager.isServer)
            {
                for (int i = 0; i < NetworkManager.client.playerMeshes.Count; i++)
                {
                    NetworkManager.client.playerMeshes[i].SetModelMatrix();
                    NetworkManager.client.playerMeshes[i].DrawMesh(ref shader, player.camera.viewMat, player.camera.projMat, player.camera.position);
                }
            }

            SetNeededDebug(player.camera.projMat, player.camera.viewMat);
            DrawDebugItems();

            PixelColorer.Render();

            controller.Render();
        }

        

        static unsafe void DebugProc(GLEnum source, GLEnum type, int id, GLEnum severity, int length, nint msg, nint userParam)
        {
            string _source;
            string _type;
            string _severity;

            switch (source) 
            {
                case GLEnum.DebugSourceApi:
                _source = "API";
                break;

                case GLEnum.DebugSourceWindowSystem:
                _source = "WINDOW SYSTEM";
                break;

                case GLEnum.DebugSourceShaderCompiler:
                _source = "SHADER COMPILER";
                break;

                case GLEnum.DebugSourceThirdParty:
                _source = "THIRD PARTY";
                break;

                case GLEnum.DebugSourceApplication:
                _source = "APPLICATION";
                break;

                case GLEnum.DebugSourceOther:
                _source = "UNKNOWN";
                break;

                default:
                _source = "UNKNOWN";
                break;
            }

            switch (type) {
                case GLEnum.DebugTypeError:
                _type = "ERROR";
                break;

                case GLEnum.DebugTypeDeprecatedBehavior:
                _type = "DEPRECATED BEHAVIOR";
                break;

                case GLEnum.DebugTypeUndefinedBehavior:
                _type = "UDEFINED BEHAVIOR";
                break;

                case GLEnum.DebugTypePortability:
                _type = "PORTABILITY";
                break;

                case GLEnum.DebugTypePerformance:
                _type = "PERFORMANCE";
                break;

                case GLEnum.DebugTypeOther:
                _type = "OTHER";
                break;

                case GLEnum.DebugTypeMarker:
                _type = "MARKER";
                break;

                default:
                _type = "UNKNOWN";
                break;
            }

            switch (severity) {
                case GLEnum.DebugSeverityHigh:
                _severity = "HIGH";
                break;

                case GLEnum.DebugSeverityMedium:
                _severity = "MEDIUM";
                break;

                case GLEnum.DebugSeverityLow:
                _severity = "LOW";
                break;

                case GLEnum.DebugSeverityNotification:
                _severity = "NOTIFICATION";
                break;

                default:
                _severity = "UNKNOWN";
                break;
            }

            Console.WriteLine(string.Format("{0}: {1} of {2} severity, raised from {3}: {4}", id, _type, _severity, _source, msg));
        }
    }
}
