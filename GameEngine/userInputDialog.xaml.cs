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

        public string InputText
        {
            get { return InputBox.Text; }
            set { InputBox.Text = value; }
        }

        private void OnClick_Dialog(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void Keydown_Dialog(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Return)
                return;

            DialogResult = true;
        }
    }
}
