////////////////////////////////////////
///
///  Sandboxクラス
///  
///  機能：ゲームループ用AppDomainを格納
/// 
////////////////////////////////////////
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameEngine.ScriptLoading
{
    public class Sandbox
    {
        public AppDomain m_appDomain { get; set; }

        //新しいAppDomainを作成
        public void InitSandbox()
        {
            m_appDomain = AppDomain.CreateDomain("Sandbox");
        }

    }
}
