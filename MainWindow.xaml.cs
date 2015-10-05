using Lucene.Net.QueryParsers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using F = System.Windows.Forms;

namespace CdrIndexer
{
    public partial class MainWindow : Window
    {
        private BackgroundWorker indexingWorker;
        private BackgroundWorker cleanupWorker;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void uxIndex_Click(object sender, RoutedEventArgs e)
        {
            if (this.indexingWorker == null)
            {
                StartIndexing();
            }
            else
            {
                RequestStopIndexing();
            }
        }

        private void uxPathToIndex_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                StartIndexing();
            }
        }

        private void RequestStopIndexing()
        {
            this.indexingWorker.CancelAsync();
            uxIndex.IsEnabled = false;
        }

        private void StartIndexing()
        {
            uxOutput.Text = "";

            string path = uxPathToIndex.Text;
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
                MessageBox.Show(string.Format("Path '{0}' not found.", path), "Invalid path",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Disable();
            uxIndex.IsEnabled = true;
            uxIndex.Content = "Stop";

            Log("Counting files...\r\n");
            var directory = new DirectoryInfo(path);
            var files = directory.EnumerateFiles("*.cdr", SearchOption.AllDirectories);
            Log(string.Format("{0} files to process.\r\n", files.Count()));
            uxIndexProgress.Value = 0;
            uxIndexProgress.Maximum = files.Count();
            this.indexingWorker = new BackgroundWorker();
            this.indexingWorker.DoWork += IndexFiles;
            this.indexingWorker.WorkerReportsProgress = true;
            this.indexingWorker.ProgressChanged += OnProgressChanged;
            this.indexingWorker.WorkerSupportsCancellation = true;
            this.indexingWorker.RunWorkerCompleted += OnIndexingCompleted;
            this.indexingWorker.RunWorkerAsync(files);
        }

        private void OnIndexingCompleted(object cs, RunWorkerCompletedEventArgs ce)
        {
            this.indexingWorker = null;
            uxIndex.Content = "Index";
            uxIndexProgress.Value = 0;
            Enable();
        }

        private void Disable()
        {
            foreach (var control in ControlsToDisable())
            {
                control.IsEnabled = false;
            }
        }

        private void Enable()
        {
            foreach (var control in ControlsToDisable())
            {
                control.IsEnabled = true;
            }
        }

        private Control[] ControlsToDisable()
        {
            return new Control[] { uxPathToIndex, uxIndex, uxCleanupIndex, uxSearchPhrase, uxSearch, uxResults };
        }

        private void IndexFiles(object sender, DoWorkEventArgs e)
        {
            var files = e.Argument as IEnumerable<FileInfo>;
            var worker = sender as BackgroundWorker;
            new Indexer(files,
                () => { (worker).ReportProgress(0); },
                () => { return worker.CancellationPending; },
                (message) => { Dispatcher.Invoke(() => Log(message)); }
            ).Run();
        }

        private void Log(string message)
        {
            uxOutput.Text += message;
        }

        private void OnProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            uxIndexProgress.Value++;
        }

        private void uxCleanupIndex_Click(object sender, RoutedEventArgs e)
        {
            if (this.cleanupWorker == null)
            {
                StartCleanup();
            }
            else
            {
                RequestStopCleanup();
            }
        }

        private void RequestStopCleanup()
        {
            this.cleanupWorker.CancelAsync();
            uxCleanupIndex.IsEnabled = false;
        }

        private void StartCleanup()
        {
            uxOutput.Text = "";

            Disable();
            uxCleanupIndex.IsEnabled = true;
            uxCleanupIndex.Content = "Stop";

            int totalCount = LuceneStore.Current.TotalCount();
            Log(string.Format("{0} entries to process.\r\n", totalCount));
            uxIndexProgress.Value = 0;
            uxIndexProgress.Maximum = totalCount;
            this.cleanupWorker = new BackgroundWorker();
            this.cleanupWorker.DoWork += CleanUpIndex;
            this.cleanupWorker.WorkerReportsProgress = true;
            this.cleanupWorker.ProgressChanged += OnProgressChanged;
            this.cleanupWorker.WorkerSupportsCancellation = true;
            this.cleanupWorker.RunWorkerCompleted += OnCleanupCompleted;
            this.cleanupWorker.RunWorkerAsync();
        }

        private void CleanUpIndex(object sender, DoWorkEventArgs e)
        {
            var worker = sender as BackgroundWorker;
            new IndexCleaner(
                () => { (worker).ReportProgress(0); },
                () => { return worker.CancellationPending; },
                (message) => { Dispatcher.Invoke(() => Log(message)); }
            ).Run();
        }

        private void OnCleanupCompleted(object cs, RunWorkerCompletedEventArgs ce)
        {
            this.cleanupWorker = null;
            uxCleanupIndex.Content = "Clean up index";
            uxIndexProgress.Value = 0;
            Enable();
        }

        private void uxSearchPhrase_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                PerformSearch();
            }
        }

        private void uxSearch_Click(object sender, RoutedEventArgs e)
        {
            PerformSearch();
        }

        private void PerformSearch()
        {
            uxResults.ItemsSource = new List<Entry>();

            Disable();

            var searchWorker = new BackgroundWorker();
            searchWorker.DoWork += (object sender, DoWorkEventArgs e) =>
            {
                e.Result = LuceneStore.Current.Search(e.Argument as string);
            };
            searchWorker.WorkerSupportsCancellation = true;
            searchWorker.RunWorkerCompleted += OnSearchCompleted;
            searchWorker.RunWorkerAsync(uxSearchPhrase.Text);
        }

        private void OnSearchCompleted(object cs, RunWorkerCompletedEventArgs ce)
        {
            Enable();
            if (ce.Error == null)
            {
                uxResults.ItemsSource = (List<Entry>)ce.Result;
            }
            else
            {
                if (ce.Error is ParseException)
                {
                    MessageBox.Show(ce.Error.Message, "Invalid query", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    throw ce.Error;
                }
            }
        }

        private void uxResults_RowDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DataGridRow row = sender as DataGridRow;
            Entry entry = row.Item as Entry;
            if (entry != null && !string.IsNullOrWhiteSpace(entry.Path))
            {
                Process.Start(entry.Path);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            LuceneStore.Current.Close();
        }

        private void uxBrowsePathToIndex_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new F.FolderBrowserDialog();
            if (dialog.ShowDialog() == F.DialogResult.OK)
            {
                uxPathToIndex.Text = dialog.SelectedPath;
            }
        }
    }
}
