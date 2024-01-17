////////////////////////////////////////
///
///  Sandboxクラス
///  
///  機能：ゲームループ用AppDomainを格納
/// 
////////////////////////////////////////
using System;

namespace GameEngine.ScriptLoading
{
    public class Sandbox
    {
        public AppDomain m_appDomain { get; set; }

        
        /// <summary>
        /// 新しいAppDomainを作成
        /// </summary>
        public void InitSandbox()
        {
            m_appDomain = AppDomain.CreateDomain("Sandbox");
        }

    }
}
