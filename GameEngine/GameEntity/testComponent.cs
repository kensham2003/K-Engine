﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Numerics;
using System.Threading.Tasks;

namespace GameEngine.GameEntity
{
    class testComponent : GameScript
    {
        public testComponent() { }
        public testComponent(GameObject gameObject) : base(gameObject) { }

        public override void BeginPlay()
        {
        }

        public override void Update(TimeSpan gameTime)
        {
            Vector3 rot = Parent.Rotation;
            rot.X += 0.1f;
            Parent.Rotation = rot;
        }
    }
}
