using System;

using GameEngine.Detail;

namespace GameEngine.GameEntity
{
    public class Camera : Component
    {
        public GameObject m_Target { get; set; } = null;

        public SVector3 m_Offset { get; set; } = SVector3.Zero();
        public override void Update(TimeSpan gameTime)
        {
            if(m_Target != null)
            {
                SVector3 dir = m_Target.Position - GetParent().Position;
            }
        }
    }
}
