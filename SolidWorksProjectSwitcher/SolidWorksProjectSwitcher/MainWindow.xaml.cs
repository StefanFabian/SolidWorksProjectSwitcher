using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using SolidWorksProjectSwitcher.Properties;

namespace SolidWorksProjectSwitcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string NameFileName = "name.solidworksprojectswitcher.ini";

        private readonly string _newSolidWorksProjectEntry = Properties.Resources.NewSolidWorksProject;
        private readonly char[] _invalidCharacters = Path.GetInvalidPathChars().Concat(new[] { '\\', '/', '?', '!', ':', '*' }).ToArray();
        private readonly Dictionary<string, string> _paths = new Dictionary<string, string>();
        private readonly FileSystemWatcher _fileSystemWatcher;
        private readonly DispatcherTimer _solidWorksCheckTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        private bool _isSolidWorksRunning;
        private readonly string _folderPath;
        private readonly string _folderName;
        private readonly string _processName;

        public MainWindow()
        {
            InitializeComponent();

            try
            {
                _folderPath = File.ReadAllText("solidworksprojectfolder.ini").Trim();
                _folderName = Path.GetFileName(_folderPath);
            }
            catch
            {
                MessageBox.Show(Properties.Resources.ErrorSolidWorksProjectFile, Properties.Resources.Error);
                Close();
                return;
            }

            if (string.IsNullOrEmpty(_folderPath) || string.IsNullOrEmpty(_folderName))
            {
                MessageBox.Show(Properties.Resources.ErrorSolidWorksProjectPathNotSet, Properties.Resources.Error);
                Close();
                return;
            }

            try
            {
                _processName = File.ReadAllText("solidworksprocessname.ini");
            }
            catch (FileNotFoundException)
            {
                MessageBox.Show(
                    Properties.Resources.ErrorSolidWorksProcessNameIniMissing,
                    Properties.Resources.ErrorFileNotFound,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                    );
            }

            SizeChanged += MainWindow_SizeChanged;
            Activated += MainWindow_Activated;
            Deactivated += MainWindow_Deactivated;

            _solidWorksCheckTimer.Tick += (sender, e) => CheckIfSolidWorksIsRunning();
            _solidWorksCheckTimer.Start();

            FillDirectoryList();

            // ReSharper disable once AssignNullToNotNullAttribute
            _fileSystemWatcher = new FileSystemWatcher(Path.GetDirectoryName(_folderPath)) { IncludeSubdirectories = false };
            _fileSystemWatcher.Created += _fileSystemWatcher_CreatedOrDeleted;
            _fileSystemWatcher.Deleted += _fileSystemWatcher_CreatedOrDeleted;
            _fileSystemWatcher.Renamed += _fileSystemWatcher_Renamed;
            _fileSystemWatcher.Filter = $"{_folderName}*";
            _fileSystemWatcher.NotifyFilter = NotifyFilters.DirectoryName;
            _fileSystemWatcher.EnableRaisingEvents = true;

            Width = Settings.Default.Width;
            Height = Settings.Default.Height;

            StartSolidWorksCheckBox.IsChecked = Settings.Default.StartSolidWorksAfterSwitch;

            NamePrefixTextBox.Text = Settings.Default.NamePrefix;

            UpdateWorkingDirectory();

            CheckIfSolidWorksIsRunning();

        }

        #region FileSystemWatcher Callbacks
        private void _fileSystemWatcher_Renamed(object sender, RenamedEventArgs e)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                if (e.OldName == _folderName) UpdateWorkingDirectory();
                if (e.Name == _folderName) UpdateWorkingDirectory();
                else if (e.Name.StartsWith(_folderName)) UpdateNameFile(Path.Combine(e.FullPath, NameFileName), e.FullPath.Substring(_folderPath.Length));
                FillDirectoryList();
            }));
        }

        private void _fileSystemWatcher_CreatedOrDeleted(object sender, FileSystemEventArgs e)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                if (e.Name == _folderName) UpdateWorkingDirectory();
                else FillDirectoryList();
            }));
        }
        #endregion

        #region Event Callbacks
        private void CancelButton_OnClick(object sender, RoutedEventArgs e)
            => RenamePopup.IsOpen = false;

        private void DeleteButton_Click(object sender, EventArgs e)
            => DeleteDirectory((string)DirectoriesListView.SelectedItem);

        private void DirectoriesListView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
            => UpdateButtonsEnabled();

        private void DirectoriesListView_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Delete:
                    DeleteDirectory((string)DirectoriesListView.SelectedItem);
                    break;
                case Key.Enter:
                    SwitchSolidWorksProject((string)DirectoriesListView.SelectedItem);
                    break;
            }
        }

        private void MainWindow_Activated(object sender, EventArgs e)
        {
            CheckIfSolidWorksIsRunning();
            _solidWorksCheckTimer.Start();
        }

        private void MainWindow_Deactivated(object sender, EventArgs e) => _solidWorksCheckTimer.Stop();

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Settings.Default.Width = e.NewSize.Width;
            Settings.Default.Height = e.NewSize.Height;
            Settings.Default.Save();
        }

        private void NamePrefixTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            foreach (char c in _invalidCharacters.Where(c => NamePrefixTextBox.Text.Contains(c)))
            {
                NamePrefixTextBox.Text = NamePrefixTextBox.Text.Replace($"{c}", "");
            }

            Settings.Default.NamePrefix = NamePrefixTextBox.Text;
            Settings.Default.Save();
        }

        private void SwitchButton_Click(object sender, EventArgs e)
            => SwitchSolidWorksProject((string)DirectoriesListView.SelectedItem);

        private void RenameButton_OnClick(object sender, RoutedEventArgs e)
            => Rename();

        private void RenamePopup_OnKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    Rename();
                    break;
                case Key.Escape:
                    RenamePopup.IsOpen = false;
                    break;
            }
        }

        private void StartSolidWorksCheckBox_OnChecked(object sender, RoutedEventArgs e)
        {
            Settings.Default.StartSolidWorksAfterSwitch = true;
            Settings.Default.Save();
        }

        private void StartSolidWorksCheckBox_OnUnchecked(object sender, RoutedEventArgs e)
        {
            Settings.Default.StartSolidWorksAfterSwitch = false;
            Settings.Default.Save();
        }

        private void SwitchAndDeleteButton_OnClick(object sender, RoutedEventArgs e)
        {
            MessageBoxResult confirmResult = MessageBox.Show(
                Properties.Resources.WarningDeleteDirectory.Replace("%1", _folderName),
                Properties.Resources.Warning,
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
                );
            if (confirmResult != MessageBoxResult.Yes) return;

            _fileSystemWatcher.EnableRaisingEvents = false;
            try
            {
                if (Directory.Exists(_folderPath))
                    Directory.Delete(_folderPath, true);
            }
            catch (Exception)
            {
                MessageBox.Show(
                    Properties.Resources.ErrorDeletionFailedMessage.Replace("%1", _folderPath),
                    Properties.Resources.ErrorDeletionFailedTitle,
                    MessageBoxButton.OK);
                return;
            }
            finally
            {
                _fileSystemWatcher.EnableRaisingEvents = true;
            }
            SwitchSolidWorksProject((string)DirectoriesListView.SelectedItem);
        }
        #endregion

        #region Functions
        private void CheckIfSolidWorksIsRunning()
        {
            _isSolidWorksRunning = IsSolidWorksRunning();
            SolidWorksWarningBorder.Visibility = _isSolidWorksRunning ? Visibility.Visible : Visibility.Collapsed;
            UpdateButtonsEnabled();
        }

        private void DeleteDirectory(string name)
        {
            if (name == null) return;

            MessageBoxResult confirmSwitchResult = MessageBox.Show(
                Properties.Resources.WarningDeleteConfirmationMessage.Replace("%1", _paths[name]),
                Properties.Resources.Warning,
                MessageBoxButton.YesNo
                );
            if (confirmSwitchResult != MessageBoxResult.Yes) return;

            try
            {
                Directory.Delete(_paths[name], true);
            }
            catch (Exception)
            {
                MessageBox.Show(
                    Properties.Resources.ErrorDeletionFailedMessage.Replace("%1", _paths[name]),
                    Properties.Resources.ErrorDeletionFailedTitle,
                    MessageBoxButton.OK
                    );
            }
        }

        private void FillDirectoryList()
        {
            List<string> oldPaths = _paths.Values.ToList();
            _paths.Clear();
            DirectoriesListView.Items.Clear();
            DirectoriesListView.Items.Add(_newSolidWorksProjectEntry);
            foreach (string directoryPath in Directory.GetDirectories(Path.GetDirectoryName(_folderPath), $"{_folderName}*", SearchOption.TopDirectoryOnly))
            {
                if (directoryPath.Equals(_folderPath, StringComparison.OrdinalIgnoreCase)) continue;

                string name = directoryPath.Substring(_folderPath.Length);
                _paths.Add(name, directoryPath);
                DirectoriesListView.Items.Add(name);

                // Check if folder contains an up to date name file. Create or update it if not.
                string nameFilePath = Path.Combine(directoryPath, NameFileName);
                if (oldPaths.Contains(directoryPath) ||
                    (File.Exists(nameFilePath) && File.ReadAllText(nameFilePath) == name))
                    continue;

                UpdateNameFile(nameFilePath, name);
            }
        }

        private string GetCurrentSolidWorksProjectName()
        {
            if (!File.Exists($"{_folderPath}/{NameFileName}")) return null;
            return File.ReadAllText($"{_folderPath}/{NameFileName}");
        }

        private string GetDirectory(string identifier)
        {
            if (identifier == _newSolidWorksProjectEntry) return identifier;
            return _paths.ContainsKey(identifier) ? _paths[identifier] : null;
        }

        private bool IsSolidWorksRunning()
        {
            return string.IsNullOrWhiteSpace(_processName) ||
                   Process.GetProcesses()
                       .Any(proc => proc.ProcessName.Equals(_processName, StringComparison.OrdinalIgnoreCase));
        }

        private void Rename()
        {
            string newName = NewSolidWorksProjectNameTextBox.Text;
            if (newName.Length == 0) return;

            // save selection information in case of errors
            int selectionStart = NewSolidWorksProjectNameTextBox.SelectionStart;
            int selectionLength = NewSolidWorksProjectNameTextBox.SelectionLength;

            // In case of errors display message and move focus to textbox
            if (_invalidCharacters.Any(newName.Contains))
            {
                MessageBox.Show(
                    Properties.Resources.ErrorDirectoryNameContainsInvalidCharacters,
                    Properties.Resources.Error,
                    MessageBoxButton.OK
                    );
                NewSolidWorksProjectNameTextBox.Focus();
                NewSolidWorksProjectNameTextBox.SelectionStart = selectionStart;
                NewSolidWorksProjectNameTextBox.SelectionLength = selectionLength;
                return;
            }
            if (Directory.Exists(_folderPath + newName))
            {
                MessageBox.Show(
                    Properties.Resources.ErrorDirectoryExistsMessage,
                    Properties.Resources.ErrorDirectoryExistsTitle,
                    MessageBoxButton.OK
                    );
                NewSolidWorksProjectNameTextBox.Focus();
                NewSolidWorksProjectNameTextBox.SelectionStart = selectionStart;
                NewSolidWorksProjectNameTextBox.SelectionLength = selectionLength;
                return;
            }

            // Disable FileSystemWatcher events before the directory is moved and reenable them afterwards so we don't catch our own event
            _fileSystemWatcher.EnableRaisingEvents = false;
            Directory.Move(_folderPath, _folderPath + newName);
            _fileSystemWatcher.EnableRaisingEvents = true;

            RenamePopup.IsOpen = false;
            SwitchSolidWorksProject((string)DirectoriesListView.SelectedItem);
        }

        private void SwitchSolidWorksProject(string name)
        {
            if (name == null) return;

            if (IsSolidWorksRunning())
            {
                CheckIfSolidWorksIsRunning();
                return;
            }

            string newSolidWorksProject = GetDirectory(name);
            if (newSolidWorksProject == null)
            {
                MessageBox.Show(Properties.Resources.ErrorSelectedAssociatedDirectoryNotFound);
                return;
            }

            if (!Directory.Exists(newSolidWorksProject) && newSolidWorksProject != _newSolidWorksProjectEntry)
            {
                MessageBox.Show(Properties.Resources.ErrorSelectedDirectoryNotFound.Replace("%1", newSolidWorksProject));
                return;
            }

            // Open rename popup if solid works project directory exists
            if (Directory.Exists(_folderPath))
            {
                string prefix = Settings.Default.NamePrefix;
                NewSolidWorksProjectNameTextBox.Text = GetCurrentSolidWorksProjectName() ?? prefix;
                bool hasPrefix = NewSolidWorksProjectNameTextBox.Text.StartsWith(prefix);
                RenamePopup.IsOpen = true;
                NewSolidWorksProjectNameTextBox.Focus();
                NewSolidWorksProjectNameTextBox.SelectionStart = hasPrefix ? prefix.Length : 0;
                NewSolidWorksProjectNameTextBox.SelectionLength = NewSolidWorksProjectNameTextBox.Text.Length - (hasPrefix ? prefix.Length : 0);
                return;
            }

            // Disable FileSystemWatcher events before creating or moving the directory so we don't catch our own event
            _fileSystemWatcher.EnableRaisingEvents = false;
            if (name == _newSolidWorksProjectEntry)
            {
                Directory.CreateDirectory(_folderPath);
            }
            else
            {
                Directory.Move(_paths[name], _folderPath);
            }
            // Reenable it after we did our change
            _fileSystemWatcher.EnableRaisingEvents = true;

            FillDirectoryList();
            UpdateWorkingDirectory();

            if (!(StartSolidWorksCheckBox.IsChecked ?? false)) return;

            try
            {
                Process.Start(File.ReadAllText("solidworkspath.ini"));
            }
            catch (FileNotFoundException)
            {
                MessageBox.Show(
                    Properties.Resources.ErrorSolidWorksPathIniMissing,
                    Properties.Resources.ErrorFileNotFound,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                    );
            }
            catch (Exception)
            {
                MessageBox.Show(
                    Properties.Resources.ErrorCouldNotStartSolidWorksMessage,
                    Properties.Resources.ErrorCouldNotStartSolidWorksTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                    );
            }
        }

        private void UpdateButtonsEnabled()
        {
            SwitchButton.IsEnabled = !_isSolidWorksRunning && DirectoriesListView.SelectedItem != null;
            DeleteButton.IsEnabled = DirectoriesListView.SelectedItem != null &&
                                     (string)DirectoriesListView.SelectedItem != _newSolidWorksProjectEntry;
            SwitchAndDeleteButton.IsEnabled = !_isSolidWorksRunning && DirectoriesListView.SelectedItem != null;
        }

        private void UpdateNameFile(string path, string name)
        {
            if (File.Exists(path))
            {
                File.SetAttributes(path, FileAttributes.Normal);
            }
            File.WriteAllText(path, name);
            File.SetAttributes(path, FileAttributes.Hidden);
        }

        private void UpdateWorkingDirectory()
        {
            CurrentDirectoryRun.Text = GetCurrentSolidWorksProjectName() ?? Properties.Resources.Unknown;
        }
        #endregion
    }
}
