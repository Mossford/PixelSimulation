﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SpatialEngine.SpatialMath
{
    public static class MathS
    {
        public static Vector4 ApplyMatrixVec4(Vector4 self, Matrix4x4 matrix)
        {
            return new Vector4(
                matrix.M11 * self.X + matrix.M12 * self.Y + matrix.M13 * self.Z + matrix.M14 * self.W,
                matrix.M21 * self.X + matrix.M22 * self.Y + matrix.M23 * self.Z + matrix.M24 * self.W,
                matrix.M31 * self.X + matrix.M32 * self.Y + matrix.M33 * self.Z + matrix.M34 * self.W,
                matrix.M41 * self.X + matrix.M42 * self.Y + matrix.M43 * self.Z + matrix.M44 * self.W
            );
        }

        public static Vector3 ApplyMatrixVec3(Vector3 self, Matrix4x4 matrix)
        {
            Vector3 vec;
            vec.X = matrix.M11 * self.X + matrix.M12 * self.Y + matrix.M13 * self.Z + matrix.M14;
            vec.Y = matrix.M21 * self.X + matrix.M22 * self.Y + matrix.M23 * self.Z + matrix.M24;
            vec.Z = matrix.M31 * self.X + matrix.M32 * self.Y + matrix.M33 * self.Z + matrix.M34;
            return vec;
        }

        public static float ClampValue(float value, float min, float max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }

        public static Vector3 ClampVector3(Vector3 value, float min, float max)
        {
            Vector3 temp = value;
            if (value.X < min)
                temp.X = min;
            if (value.Y < min)
                temp.Y = min;
            if (value.Z < min)
                temp.Z = min;
            if (value.X > max)
                temp.X = max;
            if (value.Y > max)
                temp.Y = max;
            if (value.Z > max)
                temp.Z = max;
            return temp;
        }
        
        public static void SwapVector2(ref Vector2 a, ref Vector2 b)
        {
            (a, b) = (b, a);
        }

        public static float Vector3Angle(Vector3 a, Vector3 b)
        {
            float denominator = MathF.Sqrt(Vector3.Dot(a, a) * Vector3.Dot(b, b));
            if (denominator < 1e-15f)
                return 0f;
            float dot = ClampValue(Vector3.Dot(a, b) / denominator, -1f, 1f);
            return (MathF.Acos(dot)) * 180f / MathF.PI;
        }

        public static Vector3 ProjectVector3ToPlane(Vector3 pos, Vector3 plane)
        {
            //zero check
            if (Vector3.Dot(plane, pos) < 1e-15f)
                return pos;
            return pos - (Vector3.Dot(pos, plane) / plane.LengthSquared()) * plane;
        }

        public static Quaternion Vec3ToQuat(Vector3 vec)
        {
            float yaw = vec.X;
            float pitch = vec.Y;
            float roll = vec.Z;
            float qx = MathF.Sin(roll / 2f) * MathF.Cos(pitch / 2f) * MathF.Cos(yaw / 2f) - MathF.Cos(roll / 2f) * MathF.Sin(pitch / 2f) * MathF.Sin(yaw / 2f);
            float qy = MathF.Cos(roll / 2f) * MathF.Sin(pitch / 2f) * MathF.Cos(yaw / 2f) + MathF.Sin(roll / 2f) * MathF.Cos(pitch / 2f) * MathF.Sin(yaw / 2f);
            float qz = MathF.Cos(roll / 2f) * MathF.Cos(pitch / 2f) * MathF.Sin(yaw / 2f) - MathF.Sin(roll / 2f) * MathF.Sin(pitch / 2f) * MathF.Cos(yaw / 2f);
            float qw = MathF.Cos(roll / 2f) * MathF.Cos(pitch / 2f) * MathF.Cos(yaw / 2f) + MathF.Sin(roll / 2f) * MathF.Sin(pitch / 2f) * MathF.Sin(yaw / 2f);
            return new Quaternion(qx, qy, qz, qw);
        }

        public static Vector3 QuatToVec3(Quaternion quat)
        {
            float x = quat.X;
            float y = quat.Y;
            float z = quat.Z;
            float w = quat.W;
            float t0 = 2f * (w * x + y * z);
            float t1 = 1f - 2f * (x * x + y * y);
            float roll = MathF.Atan2(t0, t1);
            float t2 = 2f * (w * y - z * x);
            t2 = ClampValue(t2, -1f, 1f);
            float pitch = MathF.Asin(t2);
            float t3 = 2f * (w * z + x * y);
            float t4 = 1f - 2f * (y * y + z * z);
            float yaw = MathF.Atan2(t3, t4);
            return new Vector3(yaw, pitch, roll);
        }

        public static int ThreePointOrientation(Vector2 a, Vector2 b, Vector2 c)
        {
            int val = (int)((b.Y - a.Y) * (c.X - b.X) - (b.X - a.X) * (c.Y - b.Y));
     
            //0 same direction
            //1 clockwise
            //2 counter
            if (val == 0) return 0;
            return (val > 0) ? 1 : 2;
        }
    }
}
