//////////////////////////////////////////////////////////////////////
///
///  ComponentPropInfoクラス
///  
///  機能：コンポーネントのプロパティ・フィールド情報を記録するクラス
/// 
//////////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;

namespace GameEngine.GameEntity
{
    [Serializable]
    public class ComponentPropInfo
    {
        public int PropAmount { get; set; }

        //need to convert to type during serialization/deserialization
        public List<string> PropTypes = new List<string>();

        public List<string> PropNames = new List<string>();

        public List<string> PropValues = new List<string>();
    }
}
