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
    /// confirmWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class confirmWindow : Window
    {
        public bool m_isConfirm = false;

        public confirmWindow()
        {
            InitializeComponent();
        }

        public confirmWindow(string message)
        {
            InitializeComponent();
            textBlock.Text = message;
        }

        private void YesButtonClick(object sender, RoutedEventArgs e)
        {
            m_isConfirm = true;
            DialogResult = true;
        }

        private void NoButtonClick(object sender, RoutedEventArgs e)
        {
            m_isConfirm = false;
            DialogResult = true;
        }
    }
}
