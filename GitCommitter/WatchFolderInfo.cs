using Newtonsoft.Json;

namespace GitCommitter
{
    public class WatchFolderInfo : NotifyPropertyChanged
    {
        #region Private Fields

        private bool active;

        private GitAutoCommitter committer;

        private string filter;
        private string name;
        private string path;

        #endregion Private Fields

        #region Public Constructors

        public WatchFolderInfo()
        {
            PropertyChanged += WatchFolderInfo_PropertyChanged;
        }

        #endregion Public Constructors

        #region Public Properties

        public bool Active
        {
            get
            {
                return active;
            }
            set
            {
                active = value;
                OnPropertyChanged();
            }
        }

        [JsonIgnore]
        public GitAutoCommitter Committer
        {
            get
            {
                return committer;
            }
            set
            {
                committer = value;
                OnPropertyChanged();
            }
        }

        public string Filter
        {
            get
            {
                return filter;
            }
            set
            {
                filter = value;
                OnPropertyChanged();
            }
        }

        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
                OnPropertyChanged();
            }
        }

        public string Path
        {
            get
            {
                return path;
            }
            set
            {
                path = value;
                OnPropertyChanged();
            }
        }

        [JsonIgnore]
        public bool Valid
        {
            get
            {
                return !(string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Path));
            }
        }

        #endregion Public Properties

        #region Private Methods

        private void WatchFolderInfo_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (committer != null)
            {
                committer.Quit();
                committer = null;
            }
            if (Valid && Active)
            {
                committer = new GitAutoCommitter(path, filter);
            }
        }

        #endregion Private Methods
    }
}
