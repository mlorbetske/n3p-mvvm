using System;
using System.Windows.Input;

namespace N3P.MVVM
{
    public class Command : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;
        private bool _oldCanExecuteValue = true;
        private bool _isExecutingCanExecute;

        public Command(Action execute, Func<bool> canExecute)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public Command(Action execute)
            : this(execute, () => true)
        {
        }

        public Command()
            : this(() => { }, () => true)
        {

        }

        public bool CanExecute()
        {
            if (_isExecutingCanExecute)
            {
                return _oldCanExecuteValue;
            }

            _isExecutingCanExecute = true;

            var newCanExecuteValue = _canExecute();
            var changed = _oldCanExecuteValue ^ newCanExecuteValue;
            _oldCanExecuteValue = newCanExecuteValue;
            
            if (changed)
            {
                OnCanExecuteChanged();
            }

            _isExecutingCanExecute = false;
            return _oldCanExecuteValue;
        }

        bool ICommand.CanExecute(object parameter)
        {
            return CanExecute();
        }

        public void Execute()
        {
            _execute();
        }

        void ICommand.Execute(object parameter)
        {
            Execute();
        }

        private void OnCanExecuteChanged()
        {
            var handler = CanExecuteChanged;

            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }

        public event EventHandler CanExecuteChanged;
    }

    public class Command<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool> _canExecute;
        private bool _oldCanExecuteValue = true;
        private bool _isExecutingCanExecute;

        public Command(Action<T> execute, Func<T, bool> canExecute)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public Command(Action<T> execute)
            : this(execute, o => true)
        {
        }

        public Command()
            : this(o => { }, o => true)
        {

        }

        public bool CanExecute(T parameter)
        {
            if (_isExecutingCanExecute)
            {
                return _oldCanExecuteValue;
            }

            _isExecutingCanExecute = true;

            var newCanExecuteValue = _canExecute(parameter);
            var changed = _oldCanExecuteValue ^ newCanExecuteValue;
            _oldCanExecuteValue = newCanExecuteValue;

            if (changed)
            {
                OnCanExecuteChanged();
            }

            _isExecutingCanExecute = false;
            return _oldCanExecuteValue;
        }

        bool ICommand.CanExecute(object parameter)
        {
            if (!(parameter is T) && _oldCanExecuteValue)
            {
                _oldCanExecuteValue = true;
                OnCanExecuteChanged();
                return false;
            }

            return CanExecute((T) parameter);
        }

        public void Execute(T parameter)
        {
            _execute(parameter);
        }

        void ICommand.Execute(object parameter)
        {
            Execute((T) parameter);
        }

        private void OnCanExecuteChanged()
        {
            var handler = CanExecuteChanged;

            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }

        public event EventHandler CanExecuteChanged;
    }
}
