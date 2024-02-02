using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GameEngine.GameEntity;
using GameEngine.Detail;

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
                return OBBCheck(aC, bC);
            }

            //sphere + sphere collision

            //box + sphere collision
            return false;
        }

        private static bool OBBCheck(Collider aC, Collider bC)
        {


            //仮
            return false;
        }

        private static float GetDirectLength(GameObject a, GameObject b)
        {
            return SVector3.Distance((a.Position + a.Collider.Offset), (b.Position + b.Collider.Offset));
        }
    }
}
