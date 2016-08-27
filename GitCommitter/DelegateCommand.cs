using System;
using System.Windows.Input;

namespace GitCommitter
{
    /// <summary>
    /// Simplistic delegate command for the demo.
    /// </summary>
    public class DelegateCommand : ICommand
    {
        #region Public Events

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        #endregion Public Events

        #region Public Properties

        public Func<bool> CanExecuteFunc { get; set; }
        public Action CommandAction { get; set; }

        #endregion Public Properties

        #region Public Methods

        public bool CanExecute(object parameter)
        {
            return CanExecuteFunc == null || CanExecuteFunc();
        }

        public void Execute(object parameter)
        {
            CommandAction();
        }

        #endregion Public Methods
    }

    /// <summary>
    /// Simplistic delegate command for the demo.
    /// </summary>
    public class DelegateCommandWithParam : ICommand
    {
        #region Public Events

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        #endregion Public Events

        #region Public Properties

        public Func<object, bool> CanExecuteFunc { get; set; }
        public Action<object> CommandAction { get; set; }

        #endregion Public Properties

        #region Public Methods

        public bool CanExecute(object parameter)
        {
            return CanExecuteFunc == null || CanExecuteFunc(parameter);
        }

        public void Execute(object parameter)
        {
            CommandAction(parameter);
        }

        #endregion Public Methods
    }
}
