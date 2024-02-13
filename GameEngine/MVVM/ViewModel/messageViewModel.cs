/////////////////////////////////////////////
///
///  MessageViewModelクラス
///  
///  機能：メッセージウインドウのビューモデル
/// 
/////////////////////////////////////////////
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GameEngine.Services;

namespace GameEngine.MVVM.ViewModel
{
    public class MessageViewModel : ViewModelBase
    {
        public IItemsService ItemsService { get; set; }
        public RelayCommand AddItemCommand { get; set; }
        public string Message { get; set; }
        public MessageViewModel(IItemsService itemsService)
        {
            ItemsService = itemsService;
            AddItemCommand = new RelayCommand((object o) => { ItemsService.AddItem(Message); }, (object o) => true);
        }
    }
}
