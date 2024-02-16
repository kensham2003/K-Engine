/////////////////////////////////////////////
///
///  MainViewModelクラス
///  
///  機能：メインウインドウのビューモデル
/// 
/////////////////////////////////////////////

using System.Collections.Generic;

using GameEngine.Services;

namespace GameEngine.MVVM.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private readonly IWindowManager m_windowManager;
        private readonly ViewModelLocator m_viewModelLocator;

        public IItemsService ItemsService { get; set; }

        public RelayCommand OpenMessageWindowCommand { get; set; }



        public MainViewModel(IItemsService itemsService, IWindowManager windowManager, ViewModelLocator viewModelLocator)
        {
            m_windowManager = windowManager;
            m_viewModelLocator = viewModelLocator;
            ItemsService = itemsService;

            OpenMessageWindowCommand = new RelayCommand((object o) => { m_windowManager.ShowWindow(m_viewModelLocator.m_messageViewModel); }, (object o) => true);
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
