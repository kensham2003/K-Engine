using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Forms;

namespace GameEngine
{
    /// <summary>
    /// userInputDialog.xaml の相互作用ロジック
    /// </summary>
    public partial class userInputDialog : Window
    {

        public userInputDialog()
        {
            InitializeComponent();
        }

        public userInputDialog(List<string> componentList)
        {
            InitializeComponent();
            ComponentListbox.ItemsSource = componentList;
        }

        public string InputText
        {
            get { return InputBox.Text; }
            set { InputBox.Text = value; }
        }

        private void OnClick_Dialog(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void Keydown_Dialog(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != Key.Return)
                return;

            DialogResult = true;
        }

        private void MouseDoubleClick_Dialog(object sender, MouseButtonEventArgs e)
        {
            //int index = this.ComponentListbox.IndexFromPoint()
            string item = ComponentListbox.SelectedItem.ToString();
            InputBox.Text = item;
            DialogResult = true;
        }

        private void MouseLeftButtonDown_Dialog(object sender, MouseButtonEventArgs e)
        {
            ComponentListbox.SelectedItem = null;
        }

        private void SelectionChanged_Dialog(object sender, SelectionChangedEventArgs e)
        {
            var item = ComponentListbox.SelectedItem;

            if (item != null)
            {
                string itemName = item.ToString();
                InputBox.Text = itemName;
            }
        }
    }
}
