using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace GitCommitter
{
    /// <summary>
    /// Provides bindable properties and commands for the NotifyIcon. In this sample, the
    /// view model is assigned to the NotifyIcon in XAML. Alternatively, the startup routing
    /// in App.xaml.cs could have created this view model, and assigned it to the NotifyIcon.
    /// </summary>
    public class NotifyIconViewModel
    {
        #region Public Constructors

        public NotifyIconViewModel(App currentApp)
        {
            CurrentApp = currentApp;
            Current = this;
        }

        public NotifyIconViewModel() : this(Application.Current as App)
        {
        }

        #endregion Public Constructors

        #region Public Properties

        public static NotifyIconViewModel Current { get; private set; }

        public ICommand AddNewCommand
        {
            get
            {
                return new DelegateCommand
                {
                    CommandAction = () =>
                    {
                        var watch = new WatchFolderInfo();
                        WatchFolders.Add(watch);
                    }
                };
            }
        }

        public ICommand BrowseCommand
        {
            get
            {
                return new DelegateCommandWithParam
                {
                    CommandAction = (x) =>
                    {
                        var watch = x as WatchFolderInfo;
                        if (watch == null)
                        {
                            watch = new WatchFolderInfo();
                            WatchFolders.Add(watch);
                        }
                        var dirPicker = new CommonOpenFileDialog();
                        dirPicker.IsFolderPicker = true;
                        dirPicker.EnsurePathExists = true;
                        var result = dirPicker.ShowDialog(CurrentApp.MainWindow);
                        if (result != CommonFileDialogResult.Ok)
                        {
                            return;
                        }
                        watch.Path = dirPicker.FileName;
                    }
                };
            }
        }

        public App CurrentApp { get; private set; }

        public ICommand DeleteCommand
        {
            get
            {
                return new DelegateCommandWithParam
                {
                    CanExecuteFunc = (x) => x as WatchFolderInfo != null,
                    CommandAction = (x) =>
                    {
                        var watch = x as WatchFolderInfo;
                        if (watch == null)
                        {
                            return;
                        }
                        if (string.IsNullOrWhiteSpace(watch.Name) && string.IsNullOrWhiteSpace(watch.Path))
                        {
                            WatchFolders.Remove(watch);
                            return;
                        }
                        var msgBoxResult = MessageBox.Show(CurrentApp.MainWindow,
                            $"Are you sure you want to delete the watch \"{watch.Name}\" for \"{watch.Path}\"?",
                            "Are you sure?", MessageBoxButton.YesNo);
                        if (msgBoxResult != MessageBoxResult.Yes)
                        {
                            return;
                        }
                        WatchFolders.Remove(watch);
                    }
                };
            }
        }

        /// <summary>
        /// Shuts down the application.
        /// </summary>
        public ICommand ExitApplicationCommand
        {
            get
            {
                return new DelegateCommand { CommandAction = () => CurrentApp.Shutdown() };
            }
        }

        public bool MainWindowShowing => CurrentApp.MainWindow != null;

        public ICommand ShowWindowCommand
        {
            get
            {
                return new DelegateCommand
                {
                    CommandAction = () =>
                    {
                        if (MainWindowShowing)
                        {
                            CurrentApp.MainWindow.WindowState = WindowState.Normal;
                            CurrentApp.MainWindow.Focus();
                            return;
                        }
                        CurrentApp.MainWindow = new MainWindow();
                        CurrentApp.MainWindow.DataContext = this;
                        CurrentApp.MainWindow.Show();
                        //CurrentApp.MainWindow.WindowState = WindowState.Normal;
                        //CurrentApp.MainWindow.Focus();
                    }
                };
            }
        }

        public ICommand ToggleWindowCommand
        {
            get
            {
                return new DelegateCommand
                {
                    CommandAction = () =>
                    {
                        if (MainWindowShowing)
                        {
                            CurrentApp.MainWindow.Close();
                        }
                        else
                        {
                            ShowWindowCommand.Execute(null);
                        }
                    }
                };
            }
        }

        public ObservableCollection<WatchFolderInfo> WatchFolders => CurrentApp?.WatchFolders ?? new ObservableCollection<WatchFolderInfo>();

        #endregion Public Properties
    }
}
