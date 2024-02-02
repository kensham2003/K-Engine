////////////////////////////////////////
///
///  SVector3クラス
///  
///  機能：シリアライズ可能なVector3クラス
/// 
////////////////////////////////////////
using System;
using System.Numerics;

namespace GameEngine.Detail
{
    [Serializable]
    public struct SVector3
    {
        public float X;
        public float Y;
        public float Z;

        public SVector3(float x, float y, float z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public SVector3(string s)
        {
            string[] xyz = s.Split(new string[] { ", " }, StringSplitOptions.None);
            if (!float.TryParse(xyz[0], out X))
            {
                X = 0;
            }
            if (!float.TryParse(xyz[1], out Y))
            {
                Y = 0;
            }
            if (!float.TryParse(xyz[2], out Z))
            {
                Z = 0;
            }
        }

        public SVector3(string x, string y, string z)
        {
            if (!float.TryParse(x, out X))
            {
                X = 0;
            }
            if (!float.TryParse(y, out Y))
            {
                Y = 0;
            }
            if (!float.TryParse(z, out Z))
            {
                Z = 0;
            }
        }

        //public override string ToString()
        //    => $"[x, y, z]";
        public override string ToString()
            => X.ToString() + ", " + Y.ToString() + ", " + Z.ToString();

        public static implicit operator Vector3(SVector3 s)
            => new Vector3(s.X, s.Y, s.Z);

        public static implicit operator SVector3(Vector3 v)
            => new SVector3(v.X, v.Y, v.Z);


        public static SVector3 operator +(SVector3 a, SVector3 b)
            => new SVector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

        public static SVector3 operator -(SVector3 a, SVector3 b)
            => new SVector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

        public static SVector3 operator -(SVector3 a)
            => new SVector3(-a.X, -a.Y, -a.Z);

        public static SVector3 operator *(SVector3 a, float m)
            => new SVector3(a.X * m, a.Y * m, a.Z * m);

        public static SVector3 operator *(float m, SVector3 a)
            => new SVector3(a.X * m, a.Y * m, a.Z * m);

        public static SVector3 operator /(SVector3 a, float d)
            => new SVector3(a.X / d, a.Y / d, a.Z / d);

        /// <summary>
        /// ゼロベクトル
        /// </summary>
        /// <returns></returns>
        public static SVector3 Zero() { return new SVector3(0.0f, 0.0f, 0.0f); }

        /// <summary>
        /// SVector3の長さ
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static float Length(SVector3 v)
        {
            return MathF.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
        }

        /// <summary>
        /// 二つのSVector3の間の距離
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        public static float Distance(SVector3 v1, SVector3 v2)
        {
            return Length(v1 - v2);
        }

        public static SVector3 GetRight(SVector3 rot)
        {
            Matrix4x4 rotMat = Matrix4x4.CreateFromYawPitchRoll(rot.Y, rot.X, rot.Z);

            return new SVector3(rotMat.M11, rotMat.M12, rotMat.M13);
        }

        public static SVector3 GetTop(SVector3 rot)
        {
            Matrix4x4 rotMat = Matrix4x4.CreateFromYawPitchRoll(rot.Y, rot.X, rot.Z);

            return new SVector3(rotMat.M21, rotMat.M22, rotMat.M23);
        }

        public static SVector3 GetForward(SVector3 rot)
        {
            Matrix4x4 rotMat = Matrix4x4.CreateFromYawPitchRoll(rot.Y, rot.X, rot.Z);

            return new SVector3(rotMat.M31, rotMat.M32, rotMat.M33);
        }
    }
}
