using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

using GameEngine.Detail;

namespace GameEngine.GameEntity
{
    public class SphereCollider : Collider
    {
        public float Size { get; set; } = 1.0f;

        public SVector3 GetCenterPosition()
        {
            return Parent.Position + Offset;
        }

    }
}
