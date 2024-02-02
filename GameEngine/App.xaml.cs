using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

using GameEngine.MVVM.ViewModel;
using GameEngine.Services;

namespace GameEngine
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        private readonly IServiceCollection m_services = new ServiceCollection();
        private readonly IServiceProvider _serviceProvider;

        public App()
        {
            m_services.AddSingleton<MainViewModel>();
            m_services.AddSingleton<MessageViewModel>();

            m_services.AddSingleton<ViewModelLocator>();
            m_services.AddSingleton<WindowMapper>();
            m_services.AddSingleton<IWindowManager, WindowManager>();
            m_services.AddSingleton<IItemsService, ItemsService>();

            _serviceProvider = m_services.BuildServiceProvider();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            var windowManager = _serviceProvider.GetRequiredService<IWindowManager>();
            windowManager.ShowWindow(_serviceProvider.GetRequiredService<MainViewModel>());
            base.OnStartup(e);
        }
    }
    
}
