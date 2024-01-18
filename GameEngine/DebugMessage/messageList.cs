using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace GameEngine
{
    public class MessageList : INotifyPropertyChanged
    {
        private List<string> _messageList = new List<string>();

        public List<string> m_messageList
        {
            get
            {
                return _messageList;
            }
            set
            {
                if (_messageList != value)
                {
                    _messageList = value;
                    RaisePropertyChanged("messageList");
                }
            }
        }

        public void Clear()
        {
            _messageList.Clear();
            RaisePropertyChanged("messageList");
        }

        public void Add(string message)
        {
            _messageList.Add(message);
            RaisePropertyChanged("messageList");
        }

        public void AddRange(List<string> messages)
        {
            _messageList.AddRange(messages);
            RaisePropertyChanged("messageList");
        }

        public string Last()
        {
            if(_messageList.Count() > 0)
            {
                return _messageList.Last();
            }
            return "";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string propertyName)
        {
            var d = PropertyChanged;
            if (d != null)
                d(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
