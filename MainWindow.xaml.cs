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

namespace CdrIndexer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private BackgroundWorker worker;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void uxIndex_Click(object sender, RoutedEventArgs e)
        {
            if (this.worker == null)
            {
                StartIndexing();
            }
            else
            {
                RequestStopIndexing();
            }
        }

        private void StartIndexing()
        {
            Disable();
            uxIndex.Content = "Stop";

            var directory = new DirectoryInfo(uxPathToIndex.Text);
            var files = directory.EnumerateFiles("*.cdr", SearchOption.AllDirectories);
            uxIndexProgress.Value = 0;
            uxIndexProgress.Maximum = files.Count();
            this.worker = new BackgroundWorker();
            this.worker.DoWork += IndexFiles;
            this.worker.WorkerReportsProgress = true;
            this.worker.ProgressChanged += OnProgressChanged;
            this.worker.WorkerSupportsCancellation = true;
            this.worker.RunWorkerAsync(files);
            this.worker.RunWorkerCompleted += (object cs, RunWorkerCompletedEventArgs ce) =>
            {
                OnIndexingFinished();
            };
        }

        private void RequestStopIndexing()
        {
            this.worker.CancelAsync();
            uxIndex.IsEnabled = false;
        }

        private void OnIndexingFinished()
        {
            this.worker = null;
            uxIndex.Content = "Index";
            uxIndex.IsEnabled = true;
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
            return new Control[] { uxPathToIndex, uxSearchPhrase, uxSearch, uxResults };
        }

        private void IndexFiles(object sender, DoWorkEventArgs e)
        {
            var files = e.Argument as IEnumerable<FileInfo>;
            var worker = sender as BackgroundWorker;
            new Indexer(files,
                () => { (worker).ReportProgress(0); },
                () => { return worker.CancellationPending; }
            ).Run();
        }

        private void OnProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            uxIndexProgress.Value++;
        }

        private void uxSearch_Click(object sender, RoutedEventArgs e)
        {
            uxResults.ItemsSource = new List<Entry>();

            Disable();

            try
            {
                List<Entry> entries = LuceneStore.Current.Search(uxSearchPhrase.Text);
                uxResults.ItemsSource = entries;
            }
            catch (ParseException ex)
            {
                MessageBox.Show(ex.Message, "Invalid query", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            Enable();
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
    }
}
