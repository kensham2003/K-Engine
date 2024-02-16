using System;

using GameEngine.Detail;

namespace GameEngine.GameEntity
{
    public class Camera : Component
    {
        public GameObject LookAtTarget { get; set; } = null;

        public SVector3 LookAtOffset { get; set; } = SVector3.Zero();

        public GameObject FollowTarget { get; set; } = null;

        public SVector3 FollowOffset { get; set; } = SVector3.Zero();

        public override void BeginPlay()
        {
            if(FollowTarget != null)
            {
                GetParent().Position = FollowTarget.Position + FollowOffset;
            }
        }

        public override void Update(TimeSpan gameTime)
        {
            if (FollowTarget != null)
            {
                GetParent().Position = FollowTarget.Position + FollowOffset;
            }
        }
    }
}
