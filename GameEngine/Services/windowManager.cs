///////////////////////////////////////////////
///
///  WindowManagerクラス
///  
///  機能：現在開いているウインドウを管理する
/// 
///////////////////////////////////////////////
using System;
using System.Collections.Generic;
using System.Windows;
using GameEngine.MVVM.ViewModel;

namespace GameEngine.Services
{
    public interface IWindowManager
    {
        void ShowWindow(ViewModelBase viewModel);
        void CloseWindow(object sender);
    }

    public class WindowManager : IWindowManager
    {
        private readonly WindowMapper m_windowMapper;
        private Dictionary<Type, int> m_windowCounter = new Dictionary<Type, int>();

        public WindowManager(WindowMapper windowMapper)
        {
            m_windowMapper = windowMapper;
        }

        public void ShowWindow(ViewModelBase viewModel)
        {
            var windowType = m_windowMapper.GetWindowTypeForViewModel(viewModel.GetType());
            if(windowType != null)
            {
                //対象ウインドウが存在している場合は生成しない
                //ウインドウは一種類につき1個しか存在できない
                if (m_windowCounter.ContainsKey(windowType))
                {
                    if(m_windowCounter[windowType] > 0)
                    {
                        return;
                    }
                }
                var window = Activator.CreateInstance(windowType) as Window;
                window.DataContext = viewModel;
                window.Show();
                window.Closed += (sender, args) => CloseWindow(sender);
                m_windowCounter[windowType] = 1;
            }
        }

        public void CloseWindow(object sender)
        {
            m_windowCounter[sender.GetType()] = 0;
        }
    }
}
