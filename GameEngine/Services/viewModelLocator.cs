///////////////////////////////////////
///
///  ViewModelLocatorクラス
///  
///  機能：ビューモデルを取得する
/// 
///////////////////////////////////////
using System;
using Microsoft.Extensions.DependencyInjection;
using GameEngine.MVVM.ViewModel;

namespace GameEngine.Services
{
    public class ViewModelLocator
    {
        private readonly IServiceProvider m_provider;
        public ViewModelLocator(IServiceProvider provider)
        {
            m_provider = provider;
        }

        public MainViewModel m_mainViewModel => m_provider.GetRequiredService<MainViewModel>();
        public MessageViewModel m_messageViewModel => m_provider.GetRequiredService<MessageViewModel>();
    }
}
