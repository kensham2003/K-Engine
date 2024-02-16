////////////////////////////////////////
///
///  Colliderクラス
///  
///  機能：各種コライダーの親クラス
/// 
////////////////////////////////////////

using System.Numerics;
using System.Text.Json.Serialization;

using GameEngine.Detail;

namespace GameEngine.GameEntity
{
    [JsonDerivedType(typeof(BoxCollider))]
    [JsonDerivedType(typeof(SphereCollider))]
    public class Collider : Component
    {
        public bool IsTrigger { get; set; } = false;

        public SVector3 Offset { get; set; } = new Vector3(0.0f, 0.0f, 0.0f);
    }
}
