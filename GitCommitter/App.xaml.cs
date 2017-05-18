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
using System.Threading;
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

        private object lockObject = new object();

        private CancellationTokenSource cancelationToken = new CancellationTokenSource();

        #endregion Private Fields

        #region Public Constructors

        public App()
        {
        }

        #endregion Public Constructors

        #region Public Properties

        public ObservableCollection<WatchFolderInfo> WatchFolders { get; private set; }

        #endregion Public Properties

        #region Protected Methods

        protected override void OnExit(ExitEventArgs e)
        {
            var settingsText = JsonConvert.SerializeObject(WatchFolders, Formatting.Indented);
            File.WriteAllText(exeDir + "watchDirs.json", settingsText);
            WatchFolders.ToList().ForEach(x => x.Committer.Quit());
            notifyIcon.Dispose(); //the icon would clean up automatically, but this is cleaner
            base.OnExit(e);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            //create the notifyicon (it's a resource declared in NotifyIconResources.xaml
            notifyIcon = (TaskbarIcon)FindResource("NotifyIcon");
            notifyIcon.DataContext = new NotifyIconViewModel(this);

            foreach(var file in Directory.GetFiles(exeDir, "output-*.log", SearchOption.TopDirectoryOnly))
            {
                File.Delete(file);
            }

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

        #endregion Protected Methods


        public void ShowBalloonTip(string title, string message, BalloonIcon symbol)
        {
            ShowBalloonTip(title, message, symbol, TimeSpan.FromSeconds(10));
        }

        public void ShowBalloonTip(string title, string message, BalloonIcon symbol, TimeSpan timeout)
        {
            lock (lockObject)
            {
                notifyIcon.ShowBalloonTip(title, message, symbol);
                cancelationToken.Cancel();
                var newToken = new CancellationTokenSource();
                Task.Run(async () =>
                {
                    await Task.Delay(timeout);
                    if (newToken.IsCancellationRequested) { return; }
                    notifyIcon.HideBalloonTip();
                }, newToken.Token);
                cancelationToken = newToken;
            }
        }
    }
}
