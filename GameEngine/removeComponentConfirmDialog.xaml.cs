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
    /// removeComponentConfirmDialog.xaml の相互作用ロジック
    /// </summary>
    public partial class removeComponentConfirmDialog : Window
    {
        public bool IsConfirm = false;

        public removeComponentConfirmDialog()
        {
            InitializeComponent();
        }

        public removeComponentConfirmDialog(string componentName, string gameObjectName)
        {
            InitializeComponent();
            textBlock.Text = componentName + "を" + gameObjectName + "から削除しますか？";
        }

        private void YesButtonClick(object sender, RoutedEventArgs e)
        {
            IsConfirm = true;
            DialogResult = true;
        }

        private void NoButtonClick(object sender, RoutedEventArgs e)
        {
            IsConfirm = false;
            DialogResult = true;
        }
    }
}
