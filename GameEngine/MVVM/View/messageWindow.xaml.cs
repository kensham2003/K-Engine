using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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

namespace GameEngine.MVVM.View
{
    /// <summary>
    /// messageWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class messageWindow : Window
    {
        public messageWindow()
        {
            InitializeComponent();
        }

        private void MessageWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ((INotifyCollectionChanged)MessageLogListView.ItemsSource).CollectionChanged += 
                new NotifyCollectionChangedEventHandler(MessageLogChanged);
        }

        public void MessageLogChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (VisualTreeHelper.GetChildrenCount(MessageLogListView) > 0)
            {
                Border border = (Border)VisualTreeHelper.GetChild(MessageLogListView, 0);
                ScrollViewer scrollViewer = (ScrollViewer)VisualTreeHelper.GetChild(border, 0);
                scrollViewer.ScrollToBottom();
            }
        }


    }
}
