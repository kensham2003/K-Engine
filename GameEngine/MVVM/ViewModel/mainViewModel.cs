﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GameEngine.Services;
using GameEngine.MVVM;

namespace GameEngine.MVVM.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private readonly IWindowManager _windowManager;
        private readonly ViewModelLocator _viewModelLocator;

        public IItemsService ItemsService { get; set; }

        public RelayCommand OpenMessageWindowCommand { get; set; }

        public MainViewModel(IItemsService itemsService, IWindowManager windowManager, ViewModelLocator viewModelLocator)
        {
            _windowManager = windowManager;
            _viewModelLocator = viewModelLocator;
            ItemsService = itemsService;

            OpenMessageWindowCommand = new RelayCommand((object o) => { _windowManager.ShowWindow(_viewModelLocator.m_messageViewModel); }, (object o) => true);
        }
    }
}