using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GameEngine.Services;
using GameEngine.MVVM;
using GameEngine.MVVM.View;

namespace GameEngine.MVVM.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private readonly IWindowManager _windowManager;
        private readonly ViewModelLocator _viewModelLocator;

        public IItemsService ItemsService { get; set; }

        public RelayCommand OpenMessageWindowCommand { get; set; }



        public MainViewModel(IItemsService itemsService, IWindowManager windowManager, ViewModelLocator viewModelLocator)
        {
            _windowManager = windowManager;
            _viewModelLocator = viewModelLocator;
            ItemsService = itemsService;

            OpenMessageWindowCommand = new RelayCommand((object o) => { _windowManager.ShowWindow(_viewModelLocator.m_messageViewModel); }, (object o) => true);
        }

        public void AddItem(string item)
        {
            ItemsService.AddItem(item);
        }

        public void AddItem(List<string> items)
        {
            ItemsService.AddItem(items);
        }

        public string GetLastItem()
        {
            return ItemsService.LastItem;
        }

        public void ClearItem()
        {
            ItemsService.ClearItem();
        }
    }
}
