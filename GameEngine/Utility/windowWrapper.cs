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
using System.Windows;
using System.Windows.Interop;

namespace GameEngine
{
    public class WindowWrapper : System.Windows.Forms.IWin32Window
    {
        public WindowWrapper(IntPtr handle)
        {
            m_hwnd = handle;
        }

        public WindowWrapper(Window window)
        {
            m_hwnd = new WindowInteropHelper(window).Handle;
        }

        public IntPtr Handle
        {
            get { return m_hwnd; }
        }

        private IntPtr m_hwnd;
    }
}
