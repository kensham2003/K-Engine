////////////////////////////////////////
///
///  Modelクラス
///  
///  機能：モデルを管理するクラス
/// 
////////////////////////////////////////

namespace GameEngine.GameEntity
{
    class Model : Component
    {
        public string ModelName { get; set; }

        public Model(GameObject gameObject) : base(gameObject) { }
    }
}
