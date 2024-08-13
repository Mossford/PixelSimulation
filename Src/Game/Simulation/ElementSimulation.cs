﻿using Silk.NET.Input;
using SpatialEngine;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace SpatialGame
{
    public static class ElementSimulation
    {
        public static List<Element> elements = new List<Element>();
        /// <summary>
        /// the type of the element at its position
        /// 0 is no pixel at position,
        /// 1 is movable solid at position,
        /// 2 is movable liquid at position,
        /// 3 is movable gas at position,
        /// 100 is a unmovable at position
        /// </summary>
        public static byte[] positionCheck;
        /// <summary>
        /// the ids of the elements at their position
        /// </summary>
        public static int[] idCheck;
        /// <summary>
        /// Queue of elements that will be deleted
        /// </summary>
        public static List<int> idsToDelete;

        static bool mousePressed = false;
        static bool type = false;

        public static void InitPixelSim()
        {
            positionCheck = new byte[PixelColorer.width * PixelColorer.height];
            idCheck = new int[PixelColorer.width * PixelColorer.height];
            idsToDelete = new List<int>();

            for (int i = 0; i < idCheck.Length; i++)
            {
                idCheck[i] = -1;
            }

            for (int x = 0; x < PixelColorer.width; x++)
            {
                int id = elements.Count;
                elements.Add(new WallPE());
                elements[id].id = id;
                elements[id].position = new Vector2(x, 0);
                positionCheck[PixelColorer.PosToIndex(elements[id].position)] = 100;
                idCheck[PixelColorer.PosToIndex(elements[id].position)] = elements[id].id;

                id = elements.Count;
                elements.Add(new WallPE());
                elements[id].id = id;
                elements[id].position = new Vector2(x, PixelColorer.height - 1);
                positionCheck[PixelColorer.PosToIndex(elements[id].position)] = 100;
                idCheck[PixelColorer.PosToIndex(elements[id].position)] = elements[id].id;
            }

            for (int y = 0; y < PixelColorer.height; y++)
            {
                int id = elements.Count;
                elements.Add(new WallPE());
                elements[id].id = id;
                elements[id].position = new Vector2(0, y);
                positionCheck[PixelColorer.PosToIndex(elements[id].position)] = 100;
                idCheck[PixelColorer.PosToIndex(elements[id].position)] = elements[id].id;

                id = elements.Count;
                elements.Add(new WallPE());
                elements[id].id = id;
                elements[id].position = new Vector2(PixelColorer.width - 1, y);
                positionCheck[PixelColorer.PosToIndex(elements[id].position)] = 100;
                idCheck[PixelColorer.PosToIndex(elements[id].position)] = elements[id].id;
            }

            //DebugSimulation.Init();

            Input.input.Mice[0].MouseUp += MouseUp;
            Input.input.Mice[0].MouseDown += MouseDown;
        }

        public static void RunPixelSim()
        {
            for (int i = 0; i < idsToDelete.Count; i++)
            {
                int id = idsToDelete[i];
                if (id >= 0 && id < elements.Count && elements[id].toBeDeleted)
                    elements[id].Delete();
            }

            idsToDelete.Clear();

            for (int i = 0; i < elements.Count; i++)
            {
                //check if pass bounds check and delete if not
                if (elements[i].BoundsCheck(elements[i].position))
                {
                    //reset the color to background from where they were and will be overwritten if they dont move
                    PixelColorer.SetColorAtPos(elements[i].position, 102, 178, 204);
                    elements[i].Update();
                    //check if pass bounds check and delete if not
                    if (elements[i].BoundsCheck(elements[i].position))
                        PixelColorer.SetColorAtPos(elements[i].position, (byte)elements[i].color.X, (byte)elements[i].color.Y, (byte)elements[i].color.Z);
                }
            }

            for (int i = 0; i < idsToDelete.Count; i++)
            {
                int id = idsToDelete[i];
                if(id >= 0 && id < elements.Count && elements[id].toBeDeleted)
                    elements[id].Delete();
            }

            idsToDelete.Clear();
            if(mousePressed)
                CreateSphere();


            //DebugSimulation.Update();
        }

        public static void SafePositionCheckSet(byte type, Vector2 position)
        {
            int index = PixelColorer.PosToIndex(position);
            if(index == -1)
                return;
            positionCheck[index] = type;
        }

        public static void SafeIdCheckSet(int id, Vector2 position)
        {
            int index = PixelColorer.PosToIndex(position);
            if (index == -1)
                return;
            idCheck[index] = id;
        }

        public static byte SafePositionCheckGet(Vector2 position)
        {
            int index = PixelColorer.PosToIndex(position);
            if (index == -1)
                return 0;
            return positionCheck[index];
        }

        public static int SafeIdCheckGet(Vector2 position)
        {
            int index = PixelColorer.PosToIndex(position);
            if (index == -1)
                return -1;
            return idCheck[index];
        }

        public static void MouseDown(IMouse mouse, MouseButton button)
        {
            mousePressed = true;
            if (button == MouseButton.Left)
                type = true;
            if (button == MouseButton.Right)
                type = false;
        }

        public static void MouseUp(IMouse mouse, MouseButton button)
        {
            mousePressed = false;
        }

        public static void CreateSphere()
        {

            Vector2 position = new Vector2(PixelColorer.width, PixelColorer.height) * (Input.input.Mice[0].Position / (Vector2)Globals.window.Size);

            position.X = MathF.Floor(position.X);
            position.Y = MathF.Floor(position.Y);

            for (int x = (int)position.X - 10; x < position.X + 10; x++)
            {
                for (int y = (int)position.Y - 10; y < position.Y + 10; y++)
                {
                    if(x < 0 || x >= PixelColorer.width || y < 0 || y >= PixelColorer.height)
                        continue;

                    float check = (float)Math.Sqrt(((x - position.X) * (x - position.X)) + ((y - position.Y) * (y - position.Y)));
                    if (check < 10)
                    {
                        Vector2 pos = new Vector2(x, y);
                        int idToCheck = SafeIdCheckGet(pos);

                        if(idToCheck != -1)
                        {
                            elements[idToCheck].Delete();
                        }
                        int id = elements.Count;
                        if(type)
                        {
                            elements.Add(new CarbonDioxidePE());
                            elements[id].id = id;
                            elements[id].position = pos;
                            SafePositionCheckSet(ElementType.gas.ToByte(), elements[id].position);
                            SafeIdCheckSet(id, elements[id].position);
                        }
                    }
                }
            }
        }
    }
}
