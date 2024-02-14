using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using GameEngine.MVVM.ViewModel;
using GameEngine.MVVM.View;

namespace GameEngine.Services
{
    public class WindowMapper
    {
        private readonly Dictionary<Type, Type> _mappings = new Dictionary<Type, Type>();

        public WindowMapper()
        {
            RegisterMapping<MainViewModel, MainWindow>();
            RegisterMapping<MessageViewModel, messageWindow>();
        }

        public void RegisterMapping<TViewModel, TWindow>() where TViewModel : ViewModelBase where TWindow : Window
        {
            _mappings[typeof(TViewModel)] = typeof(TWindow);
        }

        public Type GetWindowTypeForViewModel(Type viewModelType)
        {
            _mappings.TryGetValue(viewModelType, out var windowType);
            return windowType;
        }
    }
}
