////////////////////////////////////////////////////////////////
///
///  DestroyListクラス
///  
///  機能：シミュレート中仮削除するオブジェクトを管理するクラス
/// 
////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameEngine.GameEntity
{
    public static class DestroyList
    {
        public static List<GameObject> m_list { get; } = new List<GameObject>();

        /// <summary>
        /// リストをクリア
        /// </summary>
        public static void ClearList()
        {
            m_list.Clear();
        }

        /// <summary>
        /// オブジェクトを追加
        /// </summary>
        /// <param name="gameObject"></param>
        public static void Add(GameObject gameObject)
        {
            m_list.Add(gameObject);
        }
    }
}
