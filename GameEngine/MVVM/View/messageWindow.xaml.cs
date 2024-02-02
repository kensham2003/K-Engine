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

using GameEngine.MVVM.ViewModel;

namespace GameEngine.MVVM.View
{
    /// <summary>
    /// messageWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class messageWindow : Window
    {
        MessageViewModel m_messageViewModel;
        public MainWindow m_mainWindow;

        public messageWindow()
        {
            InitializeComponent();
        }

        private void MessageWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ((INotifyCollectionChanged)MessageLogListView.ItemsSource).CollectionChanged += 
                new NotifyCollectionChangedEventHandler(MessageLogChanged);

            m_messageViewModel = (MessageViewModel)DataContext;
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

        private void OnClearButtonClick(object sender, RoutedEventArgs e)
        {
            m_messageViewModel.ItemsService.ClearItem();
            foreach (Window window in Application.Current.Windows)
            {
                if (window.GetType() == typeof(MainWindow))
                {
                    (window as MainWindow).MessageLog.Content = "";
                }
            }
        }

        private void MessageLogListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            object selected = MessageLogListView.SelectedItem;
            if(selected != null)
            {
                MessageSelected.Content = selected.ToString();
            }
            else
            {
                MessageSelected.Content = "";
            }
        }
    }
}
