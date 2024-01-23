using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

using GameEngine.Detail;

namespace GameEngine.GameEntity
{
    public class BoxCollider : Collider
    {
        SVector3 Size { get; set; } = new Vector3(1.0f, 1.0f, 1.0f);
    }
}
