////////////////////////////////////////
///
///  SVector3クラス
///  
///  機能：シリアライズ可能なVector3クラス
/// 
////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Numerics;
using System.Threading.Tasks;

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

        public override string ToString()
            => $"[x, y, z]";

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
    }
}
