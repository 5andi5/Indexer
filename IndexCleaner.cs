using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace CdrIndexer
{
    public class IndexCleaner
    {
        private Action onEntryProcessed;
        private Func<bool> cancellationRequested;
        private Action<string> log;

        public IndexCleaner(
            Action onEntryProcessed,
            Func<bool> cancellationRequested,
            Action<string> log)
        {
            this.onEntryProcessed = onEntryProcessed;
            this.cancellationRequested = cancellationRequested;
            this.log = log;
        }

        public void Run()
        {
            int deletedCount = 0;
            int presentCount = 0;
            this.log("Cleanup...\r\n. - file present\r\n- - file missing (delete)\r\n");
            foreach (string path in LuceneStore.Current.AllPaths())
            {
                if (this.cancellationRequested())
                {
                    this.log("\r\nTerminating\r\n");
                    break;
                }

                if (File.Exists(path))
                {
                    this.log(".");
                    presentCount++;
                }
                else
                {
                    this.log("-");
                    deletedCount++;
                    LuceneStore.Current.Delete(path);
                }
                this.onEntryProcessed();
            }

            LuceneStore.Current.ReopenDirectory();
            this.log(string.Format(
                "\r\nFinished: {0} files present, {1} removed from index.\r\n",
                presentCount, deletedCount));
        }
    }
}
