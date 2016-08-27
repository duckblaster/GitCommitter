using Hardcodet.Wpf.TaskbarNotification;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace GitCommitter
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        #region Private Fields

        private static readonly string exeDir = Directory.GetParent(Assembly.GetEntryAssembly().Location) + "\\";

        private TaskbarIcon notifyIcon;

        #endregion Private Fields

        #region Public Constructors

        public App()
        {
            List<WatchFolderInfo> tmpWatchFolderList;
            try
            {
                var settingsText = File.ReadAllText(exeDir + "watchDirs.json");
                tmpWatchFolderList = JsonConvert.DeserializeObject<List<WatchFolderInfo>>(settingsText);
            }
            catch (FileNotFoundException)
            {
                tmpWatchFolderList = new List<WatchFolderInfo>();
            }
            WatchFolders = new ObservableCollection<WatchFolderInfo>(tmpWatchFolderList);
        }

        #endregion Public Constructors

        #region Public Properties

        public ObservableCollection<WatchFolderInfo> WatchFolders { get; private set; }

        #endregion Public Properties

        #region Protected Methods

        protected override void OnExit(ExitEventArgs e)
        {
            notifyIcon.Dispose(); //the icon would clean up automatically, but this is cleaner
            var settingsText = JsonConvert.SerializeObject(WatchFolders);
            File.WriteAllText(exeDir + "watchDirs.json", settingsText);
            WatchFolders.ToList().ForEach(x => x.Committer.Quit());
            base.OnExit(e);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            //create the notifyicon (it's a resource declared in NotifyIconResources.xaml
            notifyIcon = (TaskbarIcon)FindResource("NotifyIcon");
            notifyIcon.DataContext = new NotifyIconViewModel(this);
        }

        #endregion Protected Methods
    }
}
