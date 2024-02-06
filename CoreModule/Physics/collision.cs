using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GameEngine.GameEntity;
using GameEngine.Detail;
using System.Numerics;

namespace GameEngine.Physics
{
    public static class Collision
    {
        public static bool CheckCollision(GameObject a, GameObject b)
        {
            //どれかがコライダーを持っていない場合はすぐfalseを返す
            if(!a.HasCollider || !b.HasCollider) { return false; }

            Collider aC = a.Collider;

            Collider bC = b.Collider;

            Type aT = aC.GetType();
            Type bT = bC.GetType();

            if(aT == typeof(BoxCollider) && bT == typeof(BoxCollider))
            {
                return OBBCheck(aC as BoxCollider, bC as BoxCollider);
            }

            //sphere + sphere collision

            //box + sphere collision
            return false;
        }

        private static bool OBBCheck(BoxCollider aC, BoxCollider bC)
        {
            SVector3 aR = aC.GetParent().Rotation + aC.Rotate;
            SVector3 bR = bC.GetParent().Rotation + bC.Rotate;
            SVector3 aS = aC.GetAbsoluteSize();
            SVector3 bS = bC.GetAbsoluteSize();

            //正規化された方向ベクトル
            SVector3 aForwardN = SVector3.GetForward(aR);
            SVector3 aRightN = SVector3.GetRight(aR);
            SVector3 aTopN = SVector3.GetTop(aR);
            SVector3 bForwardN = SVector3.GetForward(bR);
            SVector3 bRightN = SVector3.GetRight(bR);
            SVector3 bTopN = SVector3.GetTop(bR);

            //長さ付き方向ベクトル
            SVector3 aForward = aForwardN * aS.Z;
            SVector3 aRight = aRightN * aS.X;
            SVector3 aTop = aTopN * aS.Y;
            SVector3 bForward = bForwardN * bS.Z;
            SVector3 bRight = bRightN * bS.X;
            SVector3 bTop = bTopN * bS.Y;

            //距離ベクトル
            SVector3 interval = GetDirectLength(aC, bC);

            float rA, rB, L;
            SVector3 Cross;

            //分離軸：aForwardN
            rA = SVector3.Length(aForward);
            rB = GetLengthOnSeperatingAxis(aForwardN, bForward, bRight, bTop);
            L = MathF.Abs(SVector3.Dot(interval, aForwardN));
            if (L > rA + rB)
                return false;

            //分離軸：aRightN
            rA = SVector3.Length(aRight);
            rB = GetLengthOnSeperatingAxis(aRightN, bForward, bRight, bTop);
            L = MathF.Abs(SVector3.Dot(interval, aRightN));
            if (L > rA + rB)
                return false;

            //分離軸：aTopN
            rA = SVector3.Length(aTop);
            rB = GetLengthOnSeperatingAxis(aTopN, bForward, bRight, bTop);
            L = MathF.Abs(SVector3.Dot(interval, aTopN));
            if (L > rA + rB)
                return false;

            //-------------------------------------
            //分離軸：bForwardN
            rA = SVector3.Length(bForward);
            rB = GetLengthOnSeperatingAxis(bForwardN, aForward, aRight, aTop);
            L = MathF.Abs(SVector3.Dot(interval, bForwardN));
            if (L > rA + rB)
                return false;

            //分離軸：bRightN
            rA = SVector3.Length(bRight);
            rB = GetLengthOnSeperatingAxis(bRightN, aForward, aRight, aTop);
            L = MathF.Abs(SVector3.Dot(interval, bRightN));
            if (L > rA + rB)
                return false;

            //分離軸：bTopN
            rA = SVector3.Length(bTop);
            rB = GetLengthOnSeperatingAxis(bTopN, aForward, aRight, aTop);
            L = MathF.Abs(SVector3.Dot(interval, bTopN));
            if (L > rA + rB)
                return false;

            //-------------------------------------
            //分離軸：aForwardN x bForwardN
            Cross = SVector3.Cross(aForwardN, bForwardN);
            rA = GetLengthOnSeperatingAxis(Cross, aRight, aTop);
            rB = GetLengthOnSeperatingAxis(Cross, bRight, bTop);
            L = MathF.Abs(SVector3.Dot(interval, Cross));
            if (L > rA + rB)
                return false;

            //分離軸：aForwardN x bRightN
            Cross = SVector3.Cross(aForwardN, bRightN);
            rA = GetLengthOnSeperatingAxis(Cross, aRight, aTop);
            rB = GetLengthOnSeperatingAxis(Cross, bForward, bTop);
            L = MathF.Abs(SVector3.Dot(interval, Cross));
            if (L > rA + rB)
                return false;

            //分離軸：aForwardN x bTopN
            Cross = SVector3.Cross(aForwardN, bTopN);
            rA = GetLengthOnSeperatingAxis(Cross, aRight, aTop);
            rB = GetLengthOnSeperatingAxis(Cross, bRight, bForward);
            L = MathF.Abs(SVector3.Dot(interval, Cross));
            if (L > rA + rB)
                return false;

            //-------------------------------------
            //分離軸：aRightN x bForwardN
            Cross = SVector3.Cross(aRightN, bForwardN);
            rA = GetLengthOnSeperatingAxis(Cross, aForward, aTop);
            rB = GetLengthOnSeperatingAxis(Cross, bRight, bTop);
            L = MathF.Abs(SVector3.Dot(interval, Cross));
            if (L > rA + rB)
                return false;

            //分離軸：aRightN x bRightN
            Cross = SVector3.Cross(aRightN, bRightN);
            rA = GetLengthOnSeperatingAxis(Cross, aForward, aTop);
            rB = GetLengthOnSeperatingAxis(Cross, bForward, bTop);
            L = MathF.Abs(SVector3.Dot(interval, Cross));
            if (L > rA + rB)
                return false;

            //分離軸：aRightN x bTopN
            Cross = SVector3.Cross(aRightN, bTopN);
            rA = GetLengthOnSeperatingAxis(Cross, aForward, aTop);
            rB = GetLengthOnSeperatingAxis(Cross, bRight, bForward);
            L = MathF.Abs(SVector3.Dot(interval, Cross));
            if (L > rA + rB)
                return false;

            //-------------------------------------
            //分離軸：aTopN x bForwardN
            Cross = SVector3.Cross(aTopN, bForwardN);
            rA = GetLengthOnSeperatingAxis(Cross, aForward, aRight);
            rB = GetLengthOnSeperatingAxis(Cross, bRight, bTop);
            L = MathF.Abs(SVector3.Dot(interval, Cross));
            if (L > rA + rB)
                return false;

            //分離軸：aTopN x bRightN
            Cross = SVector3.Cross(aTopN, bRightN);
            rA = GetLengthOnSeperatingAxis(Cross, aForward, aRight);
            rB = GetLengthOnSeperatingAxis(Cross, bForward, bTop);
            L = MathF.Abs(SVector3.Dot(interval, Cross));
            if (L > rA + rB)
                return false;

            //分離軸：aTopN x bTopN
            Cross = SVector3.Cross(aTopN, bTopN);
            rA = GetLengthOnSeperatingAxis(Cross, aForward, aRight);
            rB = GetLengthOnSeperatingAxis(Cross, bRight, bForward);
            L = MathF.Abs(SVector3.Dot(interval, Cross));
            if (L > rA + rB)
                return false;

            //分離平面が存在しないので当たっている
            return true;
        }

        private static SVector3 GetDirectLength(Collider a, Collider b)
        {
            SVector3 aPPos = a.GetParent().Position;
            SVector3 bPPos = b.GetParent().Position;
            SVector3 aPos = a.GetParent().Position + a.Offset;
            SVector3 bPos = b.GetParent().Position + b.Offset;
            return bPos - aPos;
        }


        /// <summary>
        /// 3つの軸が分離軸に投影した線分の長さの和
        /// </summary>
        /// <param name="sep">分離軸</param>
        /// <param name="axis1">軸１</param>
        /// <param name="axis2">軸２</param>
        /// <param name="axis3">軸３（必須ではない）</param>
        /// <returns></returns>
        private static float GetLengthOnSeperatingAxis(SVector3 sep, SVector3 axis1, SVector3 axis2, SVector3 axis3 = new SVector3())
        {
            float r1 = MathF.Abs(SVector3.Dot(sep, axis1));
            float r2 = MathF.Abs(SVector3.Dot(sep, axis2));
            float r3 = axis3 == SVector3.Zero() ? MathF.Abs(SVector3.Dot(sep, axis3)) : 0;

            return r1 + r2 + r3;
        }
    }
}
