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

        public Indexer(IEnumerable<FileInfo> files, Action onFileIndexed, Func<bool> cancellationRequested)
        {
            this.files = files;
            this.onFileIndexed = onFileIndexed;
            this.cancellationRequested = cancellationRequested;
        }

        public void Run()
        {
            using (var cdrReader = new CdrReader())
            {
                foreach (FileInfo file in files)
                {
                    if (this.cancellationRequested())
                    {
                        return;
                    }

                    string path = file.FullName;
                    string hash = CalculateHash(path);
                    Entry entry = LuceneStore.Current.Find(path);
                    if (entry == null)
                    {
                        entry = new Entry
                        {
                            Path = file.FullName,
                            Name = file.Name,
                            Hash = hash,
                            ModifiedOn = file.LastWriteTime,
                        };
                    }
                    else if (entry.Hash != hash)
                    {
                        entry.Text = cdrReader.ReadText(path);
                        entry.CalculateAll();
                        LuceneStore.Current.Save(entry);
                    }
                    this.onFileIndexed();
                }
            }
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
