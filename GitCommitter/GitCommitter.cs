using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GitCommitter
{
    public class GitAutoCommitter
    {
        private static readonly string exeDir = Directory.GetParent(Assembly.GetEntryAssembly().Location) + "\\";
        private static int outputNum;
        private readonly string filter;
        private readonly string name;
        private readonly string path;
        private readonly string remote;
        private readonly string remoteBranch;
        private bool changes;
        private FileSystemWatcher fileWatcher;
        private DateTime lastChange = DateTime.UtcNow;
        private object lockObject = new object();
        private bool quit;
        private bool running;
        private Thread workerThread;

        public GitAutoCommitter(string name, string dir, string filter, int delay, string branch, string remote)
        {
            this.name = name;
            path = dir;
            remoteBranch = branch;
            this.remote = remote;
            fileWatcher = new FileSystemWatcher(dir);
            this.filter = filter;
            fileWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName
                | NotifyFilters.DirectoryName | NotifyFilters.CreationTime;
            fileWatcher.IncludeSubdirectories = true;
            fileWatcher.EnableRaisingEvents = true;
            fileWatcher.Changed += FileWatcher_Changed;
            fileWatcher.Created += FileWatcher_Changed;
            fileWatcher.Deleted += FileWatcher_Changed;
            fileWatcher.Renamed += FileWatcher_Changed;
            Delay = TimeSpan.FromSeconds(delay);
            workerThread = new Thread(DoWatch);
            workerThread.Start();
        }

        public static string GitPath { get; set; }
        public TimeSpan Delay { get; set; }

        public void Quit()
        {
            quit = true;
        }

        private void DoWatch()
        {
            while (!quit)
            {
                var difference = DateTime.UtcNow - lastChange;
                if (difference < Delay)
                {
                    Thread.Sleep(Delay - difference);
                    continue;
                }
                if (!changes || running)
                {
                    Thread.Sleep(Delay);
                    continue;
                }
                changes = false;
                RunGit();
            }
        }

        private void FileWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (e.FullPath.Contains(".git/") || e.FullPath.Contains(".git\\") || e.FullPath.EndsWith(".git"))
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
            NotifyIconViewModel.Current.CurrentApp.ShowBalloonTip(name, "Commiting changes", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.None);
            var repo = new LibGit2Sharp.Repository(path);
            var branch = repo.Refs.Head.TargetIdentifier.Replace("refs/heads/", "");
            var now = DateTime.Now;
            var doPush = !string.IsNullOrEmpty(remoteBranch) && !string.IsNullOrEmpty(remote);

            Task.Run(() =>
            {
                lock (lockObject)
                {
                    running = true;
                    try
                    {
                        var result = RunProc("git", $"wip save \"{now.ToLongDateString()} {now.ToLongTimeString()}\" -u", true);
                        if (doPush)
                        {
                            var localBranch = $"refs/wip/{branch}";
                            if (result.Item2.Replace("\r", "").Contains("no changes\n"))
                            {
                                localBranch = branch;
                            }
                            RunProc("git", $"push {remote} {localBranch}:{remoteBranch} -f", true);
                        }
                    }
                    finally
                    {
                        running = false;
                    }
                }
            });
        }

        private Tuple<string, string> RunProc(string filename, string arguments, bool logOutput)
        {
            var timeout = (int)(TimeSpan.FromMinutes(10).TotalMilliseconds);
            using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
            using (AutoResetEvent errorWaitHandle = new AutoResetEvent(false))
            using (Process process = new Process())
            {
                process.StartInfo.FileName = filename;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.WorkingDirectory = path;

                StringBuilder output = new StringBuilder();
                StringBuilder error = new StringBuilder();
                DataReceivedEventHandler outputHandler = (sender, e) =>
                {
                    if (e.Data == null)
                    {
                        outputWaitHandle.Set();
                    }
                    else
                    {
                        output.AppendLine(e.Data);
                    }
                };
                DataReceivedEventHandler errorHandler = (sender, e) =>
                {
                    if (e.Data == null)
                    {
                        errorWaitHandle.Set();
                    }
                    else
                    {
                        error.AppendLine(e.Data);
                    }
                };
                process.OutputDataReceived += outputHandler;
                process.ErrorDataReceived += errorHandler;

                Console.WriteLine($"Starting: {filename} {arguments}");
                process.Start();

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                process.WaitForExit();

                process.OutputDataReceived -= outputHandler;
                process.ErrorDataReceived -= errorHandler;

                var outputText = output.ToString();
                var errorText = error.ToString();

                if (logOutput)
                {
                    var outputId = outputNum++;
                    if (outputText.Length > 0)
                    {
                        File.WriteAllText($"{exeDir}output-{outputId}.log", outputText);
                    }
                    if (errorText.Length > 0)
                    {
                        File.WriteAllText($"{exeDir}output-{outputId}-error.log", errorText);
                    }
                }
                var runTime = process.ExitTime - process.StartTime;
                Console.WriteLine($"Completed in {runTime}: {filename} {arguments}");
                return new Tuple<string, string>(outputText, errorText);
            }
        }
    }
}