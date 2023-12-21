using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

using GameEngine.Detail;

namespace GameEngine.GameEntity
{
    public class Collider : Component
    {
        public bool IsTrigger { get; set; } = false;

        SVector3 Offset { get; set; } = new Vector3(0.0f, 0.0f, 0.0f);
    }
}
