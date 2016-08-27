using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;

namespace GitCommitter
{
    public class GitAutoCommitter
    {
        #region Private Fields

        private static readonly string exeDir = Directory.GetParent(Assembly.GetEntryAssembly().Location) + "\\";
        private static int outputNum;
        private readonly string path;
        private bool changes;
        private FileSystemWatcher fileWatcher;
        private DateTime lastChange = DateTime.UtcNow;
        private bool quit;
        private Thread workerThread;

        #endregion Private Fields

        #region Public Constructors

        public GitAutoCommitter(string dir, string filter)
        {
            path = dir;
            if (string.IsNullOrWhiteSpace(filter))
            {
                fileWatcher = new FileSystemWatcher(dir);
            }
            else
            {
                fileWatcher = new FileSystemWatcher(dir, filter);
            }
            fileWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName
                | NotifyFilters.DirectoryName | NotifyFilters.CreationTime;
            fileWatcher.IncludeSubdirectories = true;
            fileWatcher.Changed += FileWatcher_Changed;
            fileWatcher.Created += FileWatcher_Changed;
            fileWatcher.Deleted += FileWatcher_Changed;
            fileWatcher.Renamed += FileWatcher_Changed;
            Delay = TimeSpan.FromSeconds(1);
            workerThread = new Thread(DoWatch);
            workerThread.Start();
        }

        #endregion Public Constructors

        #region Public Properties

        public static string GitPath { get; set; }
        public TimeSpan Delay { get; set; }

        #endregion Public Properties

        #region Public Methods

        public void Quit()
        {
            quit = true;
        }

        #endregion Public Methods

        #region Private Methods

        private static void RunProc(ProcessStartInfo procInfo)
        {
            RunProc(procInfo, false);
        }

        private static void RunProc(ProcessStartInfo procInfo, bool logoutput)
        {
            if (logoutput)
            {
                procInfo.RedirectStandardOutput = true;
                procInfo.RedirectStandardError = true;
                procInfo.UseShellExecute = false;
            }
            procInfo.WindowStyle = ProcessWindowStyle.Hidden;
            var proc = new Process();
            proc.StartInfo = procInfo;
            proc.Start();
            if (logoutput)
            {
                File.WriteAllText($"{exeDir}output{outputNum++}.log", proc.StandardOutput.ReadToEnd());
                File.WriteAllText($"{exeDir}outputerror{outputNum++}.log", proc.StandardError.ReadToEnd());
            }
            proc.WaitForExit();
        }

        private void DoWatch()
        {
            RunGit();
            while (!quit)
            {
                var difference = DateTime.UtcNow - lastChange;
                if (difference < Delay)
                {
                    Thread.Sleep(Delay - difference);
                    continue;
                }
                if (!changes)
                {
                    Thread.Sleep(Delay);
                    continue;
                }
                RunGit();
            }
        }

        private void FileWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (e.FullPath.Contains(".git/"))
            {
                return;
            }
            lastChange = DateTime.UtcNow;
            changes = true;
        }

        private string GetTemporaryDirectory()
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }

        private void RunGit()
        {
            var repo = new LibGit2Sharp.Repository(path);
            var branch = repo.Refs.Head.TargetIdentifier.Replace("refs/heads/", "");
            ProcessStartInfo procInfoWipSave = new ProcessStartInfo("git", $@"wip save ""{DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString()}"" -u -e");
            ProcessStartInfo procInfoPush = new ProcessStartInfo("git", $"push wip wip/{branch}:master");

            RunProc(procInfoWipSave);
            RunProc(procInfoPush, true);
        }

        #endregion Private Methods
    }
}
