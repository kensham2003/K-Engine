using System;
using System.Collections.Generic;
using System.Windows;
using GameEngine.MVVM.ViewModel;
using GameEngine.MVVM.View;

namespace GameEngine.Services
{
    public class WindowMapper
    {
        private readonly Dictionary<Type, Type> m_mappings = new Dictionary<Type, Type>();

        public WindowMapper()
        {
            RegisterMapping<MainViewModel, MainWindow>();
            RegisterMapping<MessageViewModel, messageWindow>();
        }

        public void RegisterMapping<TViewModel, TWindow>() where TViewModel : ViewModelBase where TWindow : Window
        {
            m_mappings[typeof(TViewModel)] = typeof(TWindow);
        }

        public Type GetWindowTypeForViewModel(Type viewModelType)
        {
            m_mappings.TryGetValue(viewModelType, out var windowType);
            return windowType;
        }
    }
}
