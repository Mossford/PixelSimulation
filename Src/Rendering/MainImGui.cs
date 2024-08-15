﻿using ImGuiNET;
using JoltPhysicsSharp;
using Silk.NET.OpenGL;
using Silk.NET.SDL;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Riptide;

//engine stuff
using static SpatialEngine.Globals;
using static SpatialEngine.Rendering.MeshUtils;
using static SpatialEngine.Resources;
using SpatialEngine.Networking;
using System.Net;

namespace SpatialEngine.Rendering
{
    public static class MainImGui
    {
        class ScrollingBuffer
        {
            int MaxSize;
            int Offset;
            List<Vector2> Data;
            public ScrollingBuffer(int max_size = 2000)
            {
                MaxSize = max_size;
                Offset = 0;
                Data = new List<Vector2>(MaxSize);
            }
            public void AddPoint(float x, float y)
            {
                if (Data.Count < MaxSize)
                    Data.Add(new Vector2(x, y));
                else
                {
                    Data[Offset] = new Vector2(x, y);
                    Offset = (Offset + 1) % MaxSize;
                }
            }
            public void Erase()
            {
                if (Data.Count() > 0)
                {
                    Data.Clear();
                    Offset = 0;
                }
            }
        }

        static ScrollingBuffer frameTimes = new ScrollingBuffer(2000);
        static float HighestFT = 0.0f;
        static bool ShowSceneViewerMenu, ShowObjectViewerMenu, ShowConsoleViewerMenu, ShowNetworkViewerMenu;
        static int IMM_counter = 0;
        static Vector3 IMM_selposition = new Vector3();
        static Vector3 IMM_selrotation = new Vector3();
        static Vector3 IMM_selvelocity = new Vector3();
        static Vector3 IMM_selvelocityrot = new Vector3();
        static int IMM_IcoSphereSub = 0;
        static float IMM_SpikerSphereSize = 0;
        static string IMM_input = "";
        static bool IMM_static = false;
        public static void ImGuiMenu(float deltaTime)
        {
            if (deltaTime > HighestFT)
                HighestFT = (float)deltaTime;
            frameTimes.AddPoint(totalTime, deltaTime);

            ImGuiWindowFlags window_flags = 0;
            window_flags |= ImGuiWindowFlags.NoTitleBar;
            window_flags |= ImGuiWindowFlags.MenuBar;

            ImGui.Begin("SpaceTesting", window_flags);

            //needs to be io.framerate because the actal deltatime is polled too fast and the 
            //result is hard to read
            ImGui.Text("Version " + EngVer);
            ImGui.Text("OpenGl " + OpenGlVersion);
            ImGui.Text("Gpu: " + Gpu);
            ImGui.Text(String.Format("{0:N3} ms/frame ({1:N1} FPS)", 1.0f / ImGui.GetIO().Framerate * 1000.0f, ImGui.GetIO().Framerate));
            ImGui.Text(String.Format("{0} verts, {1} indices ({2} tris)", vertCount, indCount, indCount / 3));
            ImGui.Text(String.Format("DrawCall Avg: ({0:N1}) DC/frame, DrawCall Total ({1})", MathF.Round(drawCallCount / (totalTime / deltaTime)), drawCallCount));
            ImGui.Text(String.Format("Time Open {0:N1} minutes", totalTime / 60.0f));

            //float frameTimeHistory = 2.75f;
            /*ImGui.SliderFloat("FrameTimeHistory", ref frameTimeHistory, 0.1f, 10.0f);
            if (ImPlot.BeginPlot("##Scrolling", ImVec2(ImGui::GetContentRegionAvail().x,100))) 
            {
                ImPlot.SetupAxes(nullptr, nullptr, ImPlotAxisFlags_NoTickLabels, ImPlotAxisFlags_AutoFit);
                ImPlot.SetupAxisLimits(ImAxis_X1,GetTime() - frameTimeHistory, GetTime(), ImGuiCond_Always);
                ImPlot.SetupAxisLimits(ImAxis_Y1,0,HighestFT + (HighestFT * 0.25f), ImGuiCond_Always);
                ImPlot.SetNextFillStyle(ImVec4(0,0.5,0.5,1),1.0f);
                ImPlot.PlotShaded("FrameTime", &frameTimes.Data[0].x, &frameTimes.Data[0].y, frameTimes.Data.size(), -INFINITY, 0, frameTimes.Offset, 2 * sizeof(float));
                ImPlot.EndPlot();
            }*/
            if (ImGui.Checkbox("Vsync", ref vsync))
            {
                window.VSync = vsync;
            }

            if (ImGui.BeginMenuBar())
            {
                if (ImGui.BeginMenu("Menus"))
                {
                    ImGui.MenuItem("Scene Viewer", null, ref ShowSceneViewerMenu);
                    ImGui.MenuItem("Object Viewer", null, ref ShowObjectViewerMenu);
                    ImGui.MenuItem("Console Viewer", null, ref ShowConsoleViewerMenu);
                    ImGui.MenuItem("Network Viewer", null, ref ShowNetworkViewerMenu);
                    ImGui.EndMenu();
                }
                ImGui.EndMenuBar();
            }

            if (ShowSceneViewerMenu)
            {
                SceneViewer();
            }

            if (ShowObjectViewerMenu)
            {
                ObjectViewer();
            }

            if (ShowConsoleViewerMenu)
            {
                ConsoleViewer();
            }

            if(ShowNetworkViewerMenu) 
            {
                NetworkViewer();
            }
        }

        static void SceneViewer()
        {
            ImGui.SetNextWindowSize(new Vector2(600, 420), ImGuiCond.FirstUseEver);
            ImGui.Begin("Scene Viewer");
            /*if(ImGui.TreeNode("Scenes"))
            {
                std.vector<std::string> files = GetFiles(sceneLoc);
                for (unsigned int i = 0; i < files.size(); i++)
                {
                    if (ImGui::TreeNode((void*)(intptr_t)i, "Scene: %s", files[i].c_str()))
                    {
                        ImGui::SameLine();
                        if(ImGui::Button("Load"))
                        {
                            LoadScene(sceneLoc, files[i], mainScene);
                        }
                        ImGui::TreePop();
                    }
                }
                ImGui::TreePop();
            }*/

            ImGui.End();
        }

        static void ObjectViewer()
        {
            
        }

        static void ConsoleViewer()
        {
            ImGui.SetNextWindowSize(new Vector2(600, 420), ImGuiCond.FirstUseEver);
            ImGui.Begin("Console Viewer");
            ImGui.End();
        }

        static int port = 0;
        static string ip = "";
        static byte[] byteBuf = new byte[24];
        static void NetworkViewer()
        {
            ImGui.SetNextWindowSize(new Vector2(600, 420), ImGuiCond.FirstUseEver);
            ImGui.Begin("Network Viewer");
            if(!NetworkManager.didInit)
            {
                if(ImGui.Button("StartServer"))
                {
                    NetworkManager.InitServer();
                }
                if (ImGui.Button("StartClient"))
                {
                    NetworkManager.InitClient();
                }
            }
            else
            {
                if(NetworkManager.isServer)
                {
                    if(NetworkManager.server.IsRunning())
                    {
                        ImGui.Text("Running Server on: ");
                        ImGui.SameLine();
                        ImGui.TextColored(new Vector4(1, 0, 0, 1), $"{NetworkManager.server.ip}:{NetworkManager.server.port}");
                        int currentSel = 0;
                        Connection[] connections = NetworkManager.server.GetServerConnections();
                        if (ImGui.BeginListBox("##Connections", new Vector2(ImGui.GetWindowSize().X, (connections.Length + 1) * ImGui.GetTextLineHeightWithSpacing())))
                        {
                            for (int n = 0; n < connections.Length; n++)
                            {
                                bool is_selected = (currentSel == n);
                                string name = $"Client {n + 1} at {connections[n]}";
                                if (ImGui.Selectable(name, is_selected))
                                    currentSel = n;

                                if (is_selected)
                                    ImGui.SetItemDefaultFocus();
                            }
                            ImGui.EndListBox();
                        }

                        if (ImGui.Button("Stop Server"))
                        {
                            NetworkManager.server.Stop();
                        }
                    }
                    else
                    {
                        if (ImGui.Button("StartServer"))
                        {
                            NetworkManager.InitServer();
                        }
                    }
                }
                else
                {
                    if(NetworkManager.client.IsConnected())
                    {
                        ImGui.Text("Connected to Server: ");
                        ImGui.SameLine();
                        ImGui.TextColored(new Vector4(1, 0, 0, 1), $"{NetworkManager.client.connectIp}:{NetworkManager.client.connectPort}");
                        Connection clientConnect = NetworkManager.client.GetConnection();
                        ImGui.Text("Client on: ");
                        ImGui.SameLine();
                        ImGui.TextColored(new Vector4(1, 0, 0, 1), $"{clientConnect}");
                        ImGui.Text(string.Format($"Ping: {NetworkManager.client.GetPing() * 1000f:0.00}ms"));

                        if(ImGui.Button("Disconnect"))
                        {
                            NetworkManager.client.Disconnect();
                        }
                    }
                    else
                    {
                        ImGui.Text("Client not connected to server");
                        ImGui.InputText("IP Address", ref ip, 24, ImGuiInputTextFlags.CharsNoBlank);
                        ImGui.InputInt("Port", ref port);
                        port = (int)MathF.Abs(port);

                        if (ImGui.Button("Connect"))
                        {
                            if (IPAddress.TryParse(ip, out IPAddress address))
                            {
                                NetworkManager.client.Connect(address.ToString(), (ushort)port);
                            }
                        }
                    }
                }    
            }
            ImGui.End();
        }
    }
}
