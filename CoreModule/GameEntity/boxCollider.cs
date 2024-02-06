﻿using System;
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
        public SVector3 Size { get; set; } = new Vector3(1.0f, 1.0f, 1.0f);

        public SVector3 Rotate { get; set; } = new Vector3(0.0f, 0.0f, 0.0f);

        public SVector3 GetAbsoluteSize()
        {
            return new SVector3(Size.X * Parent.Scale.X, Size.Y * Parent.Scale.Y, Size.Z * Parent.Scale.Z);
        }
    }
}
