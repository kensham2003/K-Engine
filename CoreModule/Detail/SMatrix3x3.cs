using System;
using System.Numerics;

namespace GameEngine.Detail
{
    /// <summary>
    /// _1.X _1.Y _1.Z
    /// _2.X _2.Y _2.Z
    /// _3.X _3.Y _3.Z
    /// </summary>
    [Serializable]
    public struct SMatrix3x3
    {
        public SVector3 _1;
        public SVector3 _2;
        public SVector3 _3;


        /// <summary>
        /// <para>a.X a.Y a.Z</para>
        /// <para>b.X b.Y b.Z</para>
        /// <para>c.X c.Y c.Z</para>
        /// </summary>
        public SMatrix3x3(SVector3 a, SVector3 b, SVector3 c)
        {
            _1 = a;
            _2 = b;
            _3 = c;
        }

        /// <summary>
        /// <para>_1.X _1.Y _1.Z</para>
        /// <para>_2.X _2.Y _2.Z</para>
        /// <para>_3.X _3.Y _3.Z</para>
        /// </summary>
        public SMatrix3x3(float aX, float aY, float aZ, float bX, float bY, float bZ, float cX, float cY, float cZ)
        {
            _1 = new SVector3(aX, aY, aZ);
            _2 = new SVector3(bX, bY, bZ);
            _3 = new SVector3(cX, cY, cZ);
        }

        /// <summary>
        /// ゼロマトリックス
        /// </summary>
        /// <returns></returns>
        public static SMatrix3x3 Zero()
        {
            return new SMatrix3x3
            {
                _1 = SVector3.Zero(),
                _2 = SVector3.Zero(),
                _3 = SVector3.Zero()
            };
        }

        /// <summary>
        /// 単位マトリックス
        /// </summary>
        /// <returns></returns>
        public static SMatrix3x3 Identity()
        {
            return new SMatrix3x3
            {
                _1 = new SVector3(1.0f, 0.0f, 0.0f),
                _2 = new SVector3(0.0f, 1.0f, 0.0f),
                _3 = new SVector3(0.0f, 0.0f, 1.0f)
            };
        }

        /// <summary>
        /// マトリックス乗算
        /// </summary>
        /// <param name="a">左辺マトリックス</param>
        /// <param name="b">右辺マトリックス</param>
        /// <returns></returns>
        public static SMatrix3x3 operator *(SMatrix3x3 a, SMatrix3x3 b)
        {
            return new SMatrix3x3(
                (a._1.X * b._1.X + a._2.X * b._1.Y + a._3.X * b._1.Z), (a._1.Y * b._1.X + a._2.Y * b._1.Y + a._3.Y * b._1.Z), (a._1.Z * b._1.X + a._2.Z * b._1.Y + a._3.Z * b._1.Z),
                (a._1.X * b._2.X + a._2.X * b._2.Y + a._3.X * b._2.Z), (a._1.Y * b._2.X + a._2.Y * b._2.Y + a._3.Y * b._2.Z), (a._1.Z * b._2.X + a._2.Z * b._2.Y + a._3.Z * b._2.Z),
                (a._1.X * b._3.X + a._2.X * b._3.Y + a._3.X * b._3.Z), (a._1.Y * b._3.X + a._2.Y * b._3.Y + a._3.Y * b._3.Z), (a._1.Z * b._3.X + a._2.Z * b._3.Y + a._3.Z * b._3.Z)
                );
        }


        public static SMatrix3x3 RotationYawaPitchRow(float a, float b, float y)
        {
            float sina = MathF.Sin(a);
            float sinb = MathF.Sin(b);
            float siny = MathF.Sin(y);
            float cosa = MathF.Cos(a);
            float cosb = MathF.Cos(b);
            float cosy = MathF.Cos(y);

            SMatrix3x3 rotationMatrix = new SMatrix3x3(
                (cosb * cosy), (sina * sinb * cosy - cosa * siny), (cosa * sinb * cosy + sina * siny),
                (cosb * siny), (sina * sinb * siny + cosa * cosy), (cosa * sinb * siny - sina * cosy),
                (-1 * sinb), (sina * cosb), (cosa * cosb)
                );

            return (rotationMatrix * SMatrix3x3.Identity());
        }
    }
}
