﻿using Silk.NET.GLFW;
using SpatialEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SpatialGame
{
    public enum ElementType : byte
    {
        empty = 0,
        solid = 1, //moveable
        liquid = 2,
        gas = 3,
        wall = 100,
    }

    public static class ElementTypeConversion
    {
        //the this keyword allows it cool as hell
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ToByte(this ElementType elementType)
        {
            return (byte)elementType;
        }
    }
    

    public abstract class Element
    {
        public Vector2 position { get; set; }
        public Vector2 velocity { get; set; }
        public Vector3 color { get; set; }
        public bool canMove { get; set; }
        public int id { get; set; }
        public Vector2 oldpos { get; set; }
        public bool toBeDeleted {get; set;}
        public int deleteIndex {get; set;}

        /// <summary>
        /// Position check must be updated when pixel pos changed
        /// </summary>
        public abstract void Update();
        public abstract ElementType GetElementType();
        public bool BoundsCheck(Vector2 position)
        {
            if (position.X < 0 || position.X >= PixelColorer.width || position.Y < 0 || position.Y >= PixelColorer.height)
                return false;
            return true;
        }

        public void SwapElement(Vector2 newPos, ElementType type)
        {
            ElementSimulation.SafePositionCheckSet(type.ToByte(), position);
            //get the id of the element below this current element
            int swapid = ElementSimulation.SafeIdCheckGet(newPos);

            if (swapid == -1)
                return;

            //check position of swap element beacuse it has a possibility to be out of bounds somehow
            if (!BoundsCheck(ElementSimulation.elements[swapid].position))
                return;

            //set the element below the current element to the same position
            ElementSimulation.elements[swapid].position = position;
            //set the id at the current position to the id from the element below
            ElementSimulation.SafeIdCheckSet(swapid, position);

            //set the type to the new position to our current element
            ElementSimulation.SafePositionCheckSet(GetElementType().ToByte(), position);
            //set the id of our element to the new position
            ElementSimulation.SafeIdCheckSet(id, newPos);
            //set the new position of the current element
            ElementSimulation.elements[id].position = newPos;
        }

        public void MoveElement(ElementType type, float deltaTime)
        {
            //find new position
            Vector2 posMove = position + (velocity * deltaTime);
            //do line algorithim

            Vector2 dir = Vector2.Normalize(posMove - position);

            int matrixX1 = (int)position.X;
            int matrixY1 = (int)position.Y;
            int matrixX2 = (int)posMove.X;
            int matrixY2 = (int)posMove.Y;

            int xDir = matrixX1 - matrixX2;
            int yDir = matrixY1 + matrixY2;
            bool xDirIsLarger = Math.Abs(xDir) > Math.Abs(yDir);

            int xModifier = xDir < 0 ? 1 : -1;
            int yModifier = yDir < 0 ? -1 : 1;

            int longerSideLength = Math.Max(Math.Abs(xDir), Math.Abs(yDir));
            int shorterSideLength = Math.Min(Math.Abs(xDir), Math.Abs(yDir));
            float slope = (shorterSideLength == 0 || longerSideLength == 0) ? 0 : ((float)(shorterSideLength) / (longerSideLength));

            int shorterSideIncrease;
            for (int i = 1; i <= longerSideLength; i++)
            {
                shorterSideIncrease = (int)Math.Round(i * slope);
                int yIncrease, xIncrease;
                if (xDirIsLarger)
                {
                    xIncrease = i;
                    yIncrease = shorterSideIncrease;
                }
                else
                {
                    yIncrease = i;
                    xIncrease = shorterSideIncrease;
                }
                int currentY = matrixY1 + (yIncrease * yModifier);
                int currentX = matrixX1 + (xIncrease * xModifier);

                Vector2 pos = new Vector2(currentX, currentY);

                int idAfter = ElementSimulation.idCheck[PixelColorer.PosToIndex(pos)];
                if (idAfter == 0)
                {
                    ElementSimulation.positionCheck[PixelColorer.PosToIndex(position)] = ElementType.empty.ToByte();
                    ElementSimulation.idCheck[PixelColorer.PosToIndex(position)] = 0;
                    position = pos;
                    ElementSimulation.positionCheck[PixelColorer.PosToIndex(position)] = type.ToByte();
                    ElementSimulation.idCheck[PixelColorer.PosToIndex(position)] = id;
                }
                else
                    return;
            }
        }

        public void MoveElementOne(Vector2 dir)
        {
            //push to simulation to be deleted
            if(!BoundsCheck(position + dir))
            {
                velocity = dir;
                QueueDelete();
                return;
            }

            ElementSimulation.SafePositionCheckSet(ElementType.empty.ToByte(), position);
            ElementSimulation.SafeIdCheckSet(-1, position);
            position += dir;
            velocity = dir;
            ElementSimulation.SafePositionCheckSet(GetElementType().ToByte(), position);
            ElementSimulation.SafeIdCheckSet(id, position);
        }

        /// <summary>
        /// Should be used when deletion needs to happen during a particles update loop
        /// </summary>
        public void QueueDelete()
        {
            ElementSimulation.idsToDelete.Add(id);
            toBeDeleted = true;
            deleteIndex = ElementSimulation.idsToDelete.Count - 1;
            //ElementSimulation.SafePositionCheckSet(ElementType.empty.ToByte(), position);
            //ElementSimulation.SafeIdCheckSet(-1, position);
        }

        /// <summary>
        /// Should be used when a deletion is needed right away and outside of a particles update loop
        /// </summary>
        public void Delete()
        {
            //set its position to nothing
            ElementSimulation.SafePositionCheckSet(ElementType.empty.ToByte(), position);
            //set its id at its position to nothing
            ElementSimulation.SafeIdCheckSet(-1, position);
            //set the color to empty
            PixelColorer.SetColorAtPos(position, 102, 178, 204);

            //add our index to the dictionary and everything above it will be subtract 1
            if (ElementSimulation.indexCountDelete.TryAdd(id, 1))
            {

            }
            //if we already have that key in the dictionary then add one
            //and subtract that amount from everything above it
            else if(ElementSimulation.indexCountDelete.ContainsKey(id))
            {
                ElementSimulation.indexCountDelete[id]++;
            }

            //swap this to a variable and subtract and change so performance is not degraded
            //-------------------------------------------------------------
            /*Parallel.For(id + 1, ElementSimulation.elements.Count, i =>
            {
                ElementSimulation.elements[i].id--;
                ElementSimulation.SafeIdCheckSet(ElementSimulation.elements[i].id, ElementSimulation.elements[i].position);
            });*/

            //subtract from ids so that they dont go out of bounds
            /*Parallel.For(deleteIndex, ElementSimulation.idsToDelete.Count, i =>
            {
                ElementSimulation.idsToDelete[i]--;
            });*/

            //Find amount that was deleted before the current element and subtract that from the id used
            int adder = 0;
            int[] keys = ElementSimulation.indexCountDelete.Keys.ToArray();
            for (int i = 0; i < ElementSimulation.indexCountDelete.Count; i++)
            {
                if(id > keys[i])
                    adder++;
                else
                    break;
            }
            //delete it from the array
            ElementSimulation.elements.RemoveAt(id - adder);
        }
    }


    //movement defines

    public abstract class Unmoveable : Element
    {
        public override void Update()
        {
            ElementSimulation.SafePositionCheckSet(ElementType.wall.ToByte(), position);
            ElementSimulation.SafeIdCheckSet(id, position);
        }

        public override ElementType GetElementType()
        {
            return ElementType.wall;
        }
    }

    /// <summary>
    /// Swaps places with gas and liquid movable
    /// </summary>
    public abstract class Solid : Element
    {
        public override void Update()
        {
            int num = ElementSimulation.random.Next(0, 2); // choose random size to pick to favor instead of always left
            bool displaceLiq = ElementSimulation.SafePositionCheckGet(new Vector2(position.X, position.Y + 1)) == ElementType.liquid.ToByte();
            //swapping down with a liquid
            if (displaceLiq)
            {
                SwapElement(new Vector2(position.X, position.Y + 1), ElementType.liquid);
                return;
            }
            bool displaceLiqLU = ElementSimulation.SafePositionCheckGet(new Vector2(position.X - 1, position.Y + 1)) == ElementType.liquid.ToByte();
            if (displaceLiqLU && num == 0)
            {
                SwapElement(new Vector2(position.X - 1, position.Y + 1), ElementType.liquid);
                return;
            }
            bool displaceLiqRU = ElementSimulation.SafePositionCheckGet(new Vector2(position.X + 1, position.Y + 1)) == ElementType.liquid.ToByte();
            if (displaceLiqRU && num == 1)
            {
                SwapElement(new Vector2(position.X + 1, position.Y + 1), ElementType.liquid);
                return;
            }

            //if there is air under the solid

            bool grounded = ElementSimulation.SafePositionCheckGet(new Vector2(position.X, position.Y + 1)) == ElementType.empty.ToByte();
            if (grounded)
            {
                MoveElementOne(new Vector2(0, 1));
                return;
            }
            bool LUnder = ElementSimulation.SafePositionCheckGet(new Vector2(position.X - 1, position.Y + 1)) == ElementType.empty.ToByte();
            if (LUnder && num == 0)
            {
                MoveElementOne(new Vector2(-1, 1));
                return;
            }
            bool RUnder = ElementSimulation.SafePositionCheckGet(new Vector2(position.X + 1, position.Y + 1)) == ElementType.empty.ToByte();
            if (RUnder && num == 1)
            {
                MoveElementOne(new Vector2(1, 1));
                return;
            }
        }

        public override ElementType GetElementType()
        {
            return ElementType.solid;
        }
    }

    /// <summary>
    /// Swaps places with solid and gas
    /// </summary>
    public abstract class Liquid : Element
    {

        public int disp { get; set; } // viscosity
        public int level { get; set; } // bouyency

        public override void Update()
        {
            int num = ElementSimulation.random.Next(0, 2); // choose random size to pick to favor instead of always left

            oldpos = position;
            //displacement

            //gravity stuff
            bool ground = ElementSimulation.SafePositionCheckGet(new Vector2(position.X, position.Y + 1)) == ElementType.empty.ToByte();
            if (ground)
            {
                MoveElementOne(new Vector2(0, 1));
                return;
            }
            bool LUnder = ElementSimulation.SafePositionCheckGet(new Vector2(position.X - 1, position.Y + 1)) == ElementType.empty.ToByte()
                && ElementSimulation.SafePositionCheckGet(new Vector2(position.X - 1, position.Y)) == ElementType.empty.ToByte();
            if (LUnder && num == 0)
            {
                MoveElementOne(new Vector2(-1, 1));
                return;
            }
            bool RUnder = ElementSimulation.SafePositionCheckGet(new Vector2(position.X + 1, position.Y + 1)) == ElementType.empty.ToByte()
                && ElementSimulation.SafePositionCheckGet(new Vector2(position.X + 1, position.Y)) == ElementType.empty.ToByte();
            if (RUnder && num == 1)
            {
                MoveElementOne(new Vector2(1, 1));
                return;
            }
            bool left = ElementSimulation.SafePositionCheckGet(new Vector2(position.X, position.Y + 1)) != ElementType.empty.ToByte()
                && ElementSimulation.SafePositionCheckGet(new Vector2(position.X - 1, position.Y + 1)) != ElementType.empty.ToByte();
            if (!ground && left && num == 0)
            {
                int moveDisp = ElementSimulation.random.Next(0, disp);
                for (int i = 0; i < moveDisp; i++)
                {
                    if(i < 5)
                    {
                        Vector2 checkPos = new Vector2(position.X - 1, position.Y);
                        if (!BoundsCheck(checkPos))
                            return;

                        if (ElementSimulation.SafePositionCheckGet(checkPos) == ElementType.empty.ToByte())
                        {
                            MoveElementOne(new Vector2(-1, 0));
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        Vector2 checkPos = new Vector2(position.X - 1, position.Y);
                        if (!BoundsCheck(checkPos))
                            return;

                        if (ElementSimulation.SafePositionCheckGet(checkPos) == ElementType.empty.ToByte()
                            && ElementSimulation.SafePositionCheckGet(new Vector2(position.X - 1, position.Y + 1)) != ElementType.empty.ToByte())
                        {
                            MoveElementOne(new Vector2(-1, 0));
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                return;
            }
            bool right = ElementSimulation.SafePositionCheckGet(new Vector2(position.X, position.Y + 1)) != ElementType.empty.ToByte()
                && ElementSimulation.SafePositionCheckGet(new Vector2(position.X + 1, position.Y + 1)) != ElementType.empty.ToByte();
            if (!ground && right && num == 1)
            {
                int moveDisp = ElementSimulation.random.Next(0, disp);
                for (int i = 0; i < moveDisp; i++)
                {
                    if (i < 5)
                    {
                        Vector2 checkPos = new Vector2(position.X + 1, position.Y);
                        if (!BoundsCheck(checkPos))
                            return;

                        if (ElementSimulation.SafePositionCheckGet(checkPos) == ElementType.empty.ToByte())
                        {
                            MoveElementOne(new Vector2(1, 0));
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        Vector2 checkPos = new Vector2(position.X + 1, position.Y);
                        if (!BoundsCheck(checkPos))
                            return;

                        if (ElementSimulation.SafePositionCheckGet(checkPos) == ElementType.empty.ToByte()
                            && ElementSimulation.SafePositionCheckGet(new Vector2(position.X + 1, position.Y + 1)) != ElementType.empty.ToByte())
                        {
                            MoveElementOne(new Vector2(1, 0));
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                return;
            }
        }

        public override ElementType GetElementType()
        {
            return ElementType.liquid;
        }
    }

    /// <summary>
    /// Swaps places with solid and gas
    /// </summary>
    public abstract class Gas : Element
    {

        public int disp { get; set; } // viscosity
        public int level { get; set; } // bouyency

        public override void Update()
        {
            int num = ElementSimulation.random.Next(0, 3); // choose random size to pick to favor instead of always left

            oldpos = position;
            //displacement

            //gravity stuff
            bool ground = ElementSimulation.SafePositionCheckGet(new Vector2(position.X, position.Y - 1)) == ElementType.empty.ToByte();
            if (ground && num == 2)
            {
                MoveElementOne(new Vector2(0, -1));
                return;
            }
            bool LUnder = ElementSimulation.SafePositionCheckGet(new Vector2(position.X - 1, position.Y - 1)) == ElementType.empty.ToByte()
                && ElementSimulation.SafePositionCheckGet(new Vector2(position.X - 1, position.Y)) == ElementType.empty.ToByte();
            if (LUnder && num == 0)
            {
                MoveElementOne(new Vector2(-1, -1));
                return;
            }
            bool RUnder = ElementSimulation.SafePositionCheckGet(new Vector2(position.X + 1, position.Y - 1)) == ElementType.empty.ToByte()
                && ElementSimulation.SafePositionCheckGet(new Vector2(position.X + 1, position.Y)) == ElementType.empty.ToByte();
            if (RUnder && num == 1)
            {
                MoveElementOne(new Vector2(1, -1));
                return;
            }
            bool Left = ElementSimulation.SafePositionCheckGet(new Vector2(position.X - 1, position.Y)) == ElementType.empty.ToByte();
            if (!ground && Left && num == 0)
            {
                for (int i = 0; i < disp; i++)
                {
                    if (!BoundsCheck(new Vector2(position.X - (i + 1), position.Y)))
                        return;

                    if (ElementSimulation.SafePositionCheckGet(new Vector2(position.X - (i + 1), position.Y)) == ElementType.empty.ToByte())
                    {
                        MoveElementOne(new Vector2(-1, 0));
                    }
                    else
                    {
                        break;
                    }
                }

                return;
            }
            bool Right = ElementSimulation.SafePositionCheckGet(new Vector2(position.X + 1, position.Y)) == ElementType.empty.ToByte();
            if (!ground && Right && num == 1)
            {
                for (int i = 0; i < disp; i++)
                {
                    if (!BoundsCheck(new Vector2(position.X + (i + 1), position.Y)))
                        return;

                    if (ElementSimulation.SafePositionCheckGet(new Vector2(position.X + (i + 1), position.Y)) == ElementType.empty.ToByte())
                    {
                        MoveElementOne(new Vector2(1, 0));
                    }
                    else
                    {
                        break;
                    }
                }

                return;
            }
        }

        public override ElementType GetElementType()
        {
            return ElementType.gas;
        }
    }

    //----Elements----
    //Can only run things using stuff from element
    //----------------


    //more specific implements

    /// <summary>
    /// Reacts to gravity
    /// </summary>
    public class SandPE : Solid
    {
        public SandPE()
        {
            color = new Vector3(255, 255, 180);
            canMove = true;
        }
    }

    public class StonePE : Solid
    {
        public StonePE()
        {
            color = new Vector3(180, 180, 180);
            canMove = true;
        }
    }

    /// <summary>
    /// Unmoveable pixel
    /// </summary>
    public class WallPE : Unmoveable
    {
        public WallPE()
        {
            canMove = false;
            color = new Vector3(40, 40, 40);
        }
    }

    /// <summary>
    /// Reacts to gravity and solids
    /// </summary>
    public class WaterPE : Liquid
    {

        public WaterPE()
        {
            disp = 50;
            canMove = true;
            color = new Vector3(40, 0, 255);
        }

    }

    /// <summary>
    /// Reacts to gravity and solids
    /// </summary>
    public class CarbonDioxidePE : Gas
    {
        public CarbonDioxidePE()
        {
            disp = 10;
            canMove = true;
            color = new Vector3(60, 60, 60);
        }

    }
}
