using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameEngine.Detail
{
    public class Debug
    {
        public static List<string> m_log { get; } = new List<string>();

        public static void ClearLog()
        {
            m_log.Clear();
        }

        public static void Log(object message)
        {
            m_log.Add("(" + DateTime.Now.ToString("h:mm:ss") + ")  " + message.ToString());
        }
    }
}
