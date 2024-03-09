////////////////////////////////////////
///
///  BoxColliderクラス
///  
///  機能：ボックスコライダー（OBB）クラス
/// 
////////////////////////////////////////

using System.Numerics;

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
