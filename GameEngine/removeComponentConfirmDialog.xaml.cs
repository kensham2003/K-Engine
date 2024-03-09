using System.Windows;

namespace GameEngine
{
    /// <summary>
    /// removeComponentConfirmDialog.xaml の相互作用ロジック
    /// </summary>
    public partial class removeComponentConfirmDialog : Window
    {
        public bool m_isConfirm = false;

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
