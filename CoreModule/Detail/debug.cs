////////////////////////////////////////
///
///  Debugクラス
///  
///  機能：デバッグログを管理するクラス
/// 
////////////////////////////////////////
using System;
using System.Collections.Generic;

namespace GameEngine.Detail
{
    public class Debug
    {
        public static List<string> m_log { get; } = new List<string>();


        /// <summary>
        /// デバッグログをクリア
        /// </summary>
        public static void ClearLog()
        {
            m_log.Clear();
        }


        /// <summary>
        /// メッセージをタイムスタンプを入れてログに追加
        /// </summary>
        /// <param name="message">メッセージ（型は自由）</param>
        public static void Log(object message)
        {
            m_log.Add("(" + DateTime.Now.ToString("h:mm:ss") + ")  " + message.ToString());
        }
    }
}
