using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CdrIndexer
{
    public class Indexer
    {
        private IEnumerable<FileInfo> files;
        private Action onFileIndexed;
        private Func<bool> cancellationRequested;
        private Action<string> log;

        public Indexer(IEnumerable<FileInfo> files,
            Action onFileIndexed,
            Func<bool> cancellationRequested,
            Action<string> log)
        {
            this.files = files;
            this.onFileIndexed = onFileIndexed;
            this.cancellationRequested = cancellationRequested;
            this.log = log;
        }

        public void Run()
        {
            int newCount = 0;
            int updatedCount = 0;
            int unchangedCount = 0;
            this.log("Loading CorelDraw...\r\n");
            using (var cdrReader = new CdrReader())
            {
                this.log("Indexing...\r\n. - file not changed\r\n* - updating file info\r\n+ - new file\r\n");
                foreach (FileInfo file in files)
                {
                    if (this.cancellationRequested())
                    {
                        this.log("\r\nTerminating\r\n");
                        return;
                    }

                    string path = file.FullName;
                    string hash = CalculateHash(path);
                    Entry entry = LuceneStore.Current.Find(path);
                    if (entry == null)
                    {
                        this.log("+");
                        newCount++;
                        entry = new Entry
                        {
                            Path = file.FullName,
                            Name = file.Name,
                            Hash = hash,
                            ModifiedOn = file.LastWriteTime,
                        };
                        ReadAndSaveText(path, entry, cdrReader);
                    }
                    else if (entry.Hash != hash)
                    {
                        this.log("*");
                        updatedCount++;
                        ReadAndSaveText(path, entry, cdrReader);
                    }
                    else
                    {
                        this.log(".");
                        unchangedCount++;
                    }
                    this.onFileIndexed();
                }
            }
            this.log(string.Format(
                "\r\nFinished: {0} not changed, {1} updated, {2} new.\r\n",
                unchangedCount, updatedCount, newCount));
        }

        private void ReadAndSaveText(string path, Entry entry, CdrReader cdrReader)
        {
            entry.Text = cdrReader.ReadText(path);
            entry.CalculateAll();
            LuceneStore.Current.Save(entry);
        }

        private string CalculateHash(string path)
        {
            byte[] hashData;
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(path))
                {
                    hashData = md5.ComputeHash(stream);
                }
            }

            var hash = new StringBuilder();
            foreach (byte b in hashData)
            {
                hash.Append(b.ToString("x2"));
            }
            return hash.ToString();
        }
    }
}
