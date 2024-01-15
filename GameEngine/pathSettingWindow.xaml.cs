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
    /// pathSettingWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class pathSettingWindow : Window
    {
        public string m_devenvPath;

        public pathSettingWindow()
        {
            InitializeComponent();
        }

        public pathSettingWindow(string devenv)
        {
            InitializeComponent();
            m_devenvPath = devenv;
            devenvPath.Text = m_devenvPath;
        }

        private void Click_ApplyButton(object sender, RoutedEventArgs e)
        {
            m_devenvPath = devenvPath.Text;
            DialogResult = true;
        }

        private void Click_CancelButton(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
