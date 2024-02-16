using Microsoft.Extensions.DependencyInjection;
using System;
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
        private readonly IServiceProvider m_serviceProvider;

        public App()
        {
            m_services.AddSingleton<MainViewModel>();
            m_services.AddSingleton<MessageViewModel>();

            m_services.AddSingleton<ViewModelLocator>();
            m_services.AddSingleton<WindowMapper>();
            m_services.AddSingleton<IWindowManager, WindowManager>();
            m_services.AddSingleton<IItemsService, ItemsService>();

            m_serviceProvider = m_services.BuildServiceProvider();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            var windowManager = m_serviceProvider.GetRequiredService<IWindowManager>();
            windowManager.ShowWindow(m_serviceProvider.GetRequiredService<MainViewModel>());
            base.OnStartup(e);

            // UIスレッドの未処理例外で発生
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            // UIスレッド以外の未処理例外で発生
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
            // それでも処理されない例外で発生
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        }

        private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            var exception = e.Exception;
            HandleException(exception);
        }

        private void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            var exception = e.Exception.InnerException as Exception;
            HandleException(exception);
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            HandleException(exception);
        }

        private void HandleException(Exception e)
        {
            // ログを送ったり、ユーザーにお知らせしたりする
            MessageBox.Show($"エラーが発生しました\n{e?.ToString()}");
            Environment.Exit(1);
        }
    }
    
}
