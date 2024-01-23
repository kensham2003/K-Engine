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
        void AddItem();
    }

    public class ItemsService : IItemsService
    {
        public ObservableCollection<string> Items { get; set; } = new ObservableCollection<string>();

        public void AddItem()
        {
            Items.Add("item");
        }
        public void AddItem(string item)
        {
            Items.Add(item);
        }
    }
}
