////////////////////////////////////////////////////
///
///  ItemsServiceクラス
///  
///  機能：ウインドウの間に共有できるアイテムを提供
/// 
////////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameEngine.Services
{
    public interface IItemsService
    {
        ObservableCollection<string> Items { get; set; }
        string LastItem { get; }
        void AddItem();
        void AddItem(string s);
        void AddItem(List<string> ss);
        void ClearItem();
    }

    public class ItemsService : IItemsService
    {
        public ObservableCollection<string> Items { get; set; } = new ObservableCollection<string>();


        public string LastItem
        {
            get
            {
                if (Items.Count() > 0)
                {
                    return Items.Last();
                }
                return "";
            }
        }


        public void AddItem()
        {
            Items.Add("item");
        }

        public void AddItem(string item)
        {
            Items.Add(item);
        }

        public void AddItem(List<string> items)
        {
            foreach(string item in items)
            {
                Items.Add(item);
            }
        }

        public void ClearItem()
        {
            Items.Clear();
        }
    }
}
