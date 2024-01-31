//////////////////////////////////////////////////////////
///
///  WindowWrapperクラス
///  
///  機能：CenterMessageBoxを使うためのIWin32Windowをコンバートするクラス
///  
///　ソース：https://stackoverflow.com/questions/10296018/get-system-windows-forms-iwin32window-from-wpf-window
/// 
/////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace GameEngine
{
    public class WindowWrapper : System.Windows.Forms.IWin32Window
    {
        public WindowWrapper(IntPtr handle)
        {
            _hwnd = handle;
        }

        public WindowWrapper(Window window)
        {
            _hwnd = new WindowInteropHelper(window).Handle;
        }

        public IntPtr Handle
        {
            get { return _hwnd; }
        }

        private IntPtr _hwnd;
    }
}
