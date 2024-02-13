///////////////////////////////////////
///
///  ViewModelLocatorクラス
///  
///  機能：ビューモデルを取得する
/// 
///////////////////////////////////////
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using GameEngine.MVVM.ViewModel;

namespace GameEngine.Services
{
    public class ViewModelLocator
    {
        private readonly IServiceProvider _provider;
        public ViewModelLocator(IServiceProvider provider)
        {
            _provider = provider;
        }

        public MainViewModel m_mainViewModel => _provider.GetRequiredService<MainViewModel>();
        public MessageViewModel m_messageViewModel => _provider.GetRequiredService<MessageViewModel>();
    }
}
