﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using ClassicAssist.Data;
using ClassicAssist.UO;

namespace ClassicAssist.UI.ViewModels
{
    public class BaseViewModel : INotifyPropertyChanged
    {
        private static readonly List<BaseViewModel> _viewModels = new List<BaseViewModel>();
        protected Dispatcher _dispatcher;

        public BaseViewModel()
        {
            _dispatcher = Dispatcher.CurrentDispatcher;

            _viewModels.Add(this);

            Options.CurrentOptions.PropertyChanged += OnOptionChanged;
        }

        ~BaseViewModel()
        {
            _viewModels.Remove( this );
        }

        protected void OnOptionChanged(object sender, PropertyChangedEventArgs e)
        {
            PropertyInfo[] properties = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo property in properties)
            {
                OptionsBindingAttribute attr = property.GetCustomAttribute<OptionsBindingAttribute>();

                if (attr == null || attr.Property != e.PropertyName)
                {
                    continue;
                }

                if (!(sender is Options options))
                {
                    continue;
                }

                PropertyInfo optionsProperty = options.GetType().GetProperty(e.PropertyName);

                if (optionsProperty == null)
                {
                    continue;
                }

                object propertyValue = optionsProperty.GetValue(Options.CurrentOptions);

                property.SetValue(this, propertyValue);
            }
        }

        protected void SetOptionsNotify<T>(string propertyName, T value, T defaultValue)
        {
            PropertyInfo property = Options.CurrentOptions.GetType().GetProperty(propertyName);

            if (property == null)
                return;

            property.SetValue(Options.CurrentOptions, value == null ? defaultValue : value);
            NotifyPropertyChanged(propertyName);
        }

        public static BaseViewModel[] Instances => _viewModels.ToArray();

        public event PropertyChangedEventHandler PropertyChanged;

        public void SetProperty<T>(ref T obj, T value, [CallerMemberName] string propertyName = "")
        {
            obj = value;
            NotifyPropertyChanged(propertyName);
        }

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Func<object, bool> _canExecute;
        private readonly Action<object> _execute;

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
        }
    }

    public class RelayCommandAsync : ICommand
    {
        private readonly Func<object, Task> executedMethod;
        private readonly Func<object, bool> canExecuteMethod;

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public RelayCommandAsync(Func<object, Task> execute, Func<object, bool> canExecute)
        {
            executedMethod = execute ?? throw new ArgumentNullException(nameof(execute));
            canExecuteMethod = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return canExecuteMethod == null || canExecuteMethod(parameter);
        }

        public async void Execute(object parameter)
        {
            await executedMethod(parameter);
        }
    }
}