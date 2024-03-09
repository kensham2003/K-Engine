////////////////////////////////////////
///
///  SphereColliderクラス
///  
///  機能：スフィアコライダークラス
/// 
////////////////////////////////////////

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
