////////////////////////////////////////
///
///  Modelクラス
///  
///  機能：モデルを管理するクラス
/// 
////////////////////////////////////////
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameEngine.GameEntity
{
    class Model : Component
    {
        public string ModelName { get; set; }

        public Model(GameObject gameObject) : base(gameObject) { }
    }
}
