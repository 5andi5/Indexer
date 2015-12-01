using Lucene.Net.Documents;
using System;
using System.Text;

namespace CdrIndexer
{
    class Entry
    {
        private static readonly int PreviewMaxLength = 100;
        private static readonly string DateFormat = "dd.MM.yyyy. H:mm:ss";

        public string Hash { get; set; }
        public string Path { get; set; }
        public string Name { get; set; }
        public string Text { get; set; }
        public string All { get; set; }
        public DateTime ModifiedOn { get; set; }
        public float Score { get; set; }
        public int PositionByScore { get; set; }

        public string TextPreview
        {
            get
            {
                if (string.IsNullOrEmpty(this.Text))
                {
                    return "";
                }
                string text = this.Text;
                if (text.Length > PreviewMaxLength)
                {
                    text = text.Substring(0, PreviewMaxLength - 3) + "...";
                }
                return text.Replace("\r", "").Replace("\n", "");
            }
        }

        public string ModifiedOnText
        {
            get
            {
                return this.ModifiedOn.ToString(DateFormat);
            }
        }

        public Entry()
        {
        }

        public Entry(Document doc, float score, int positionByScore)
        {
            this.Hash = doc.Get("Hash");
            this.Path = doc.Get("Path");
            this.Name = doc.Get("Name");
            this.Text = doc.Get("Text");
            this.All = doc.Get("All");
            this.ModifiedOn = DateTime.ParseExact(doc.Get("ModifiedOn"), DateFormat, null);
            this.Score = score;
            this.PositionByScore = positionByScore;
        }

        public Document ToDocument()
        {
            var doc = new Document();
            doc.Add(new Field("Hash",
                this.Hash,
                Field.Store.YES,
                Field.Index.NOT_ANALYZED));
            doc.Add(new Field("Path",
                this.Path,
                Field.Store.YES,
                Field.Index.NOT_ANALYZED));
            doc.Add(new Field("Name",
                this.Name,
                Field.Store.YES,
                Field.Index.ANALYZED));
            doc.Add(new Field("Text",
                this.Text,
                Field.Store.YES,
                Field.Index.ANALYZED));
            doc.Add(new Field("All",
                this.All,
                Field.Store.YES,
                Field.Index.ANALYZED));
            doc.Add(new Field("ModifiedOn",
                this.ModifiedOn.ToString("dd.MM.yyyy. h:mm:ss"),
                Field.Store.YES,
                Field.Index.NOT_ANALYZED));

            return doc;
        }

        public void CalculateAll()
        {
            var all = new StringBuilder();
            all.AppendLine(this.Path);
            string whitespacePath = SplitPath(this.Path);
            all.AppendLine(whitespacePath);
            all.AppendLine(Normalize(whitespacePath));
            all.AppendLine(SimpleNormalize(whitespacePath));
            all.AppendLine(this.Text);
            this.All = all.ToString();
        }

        private string Normalize(string value)
        {
            return value.ToLower()
                .Replace("aa", "ā").Replace("ch", "č").Replace("ee", "ē")
                .Replace("ii", "ī").Replace("sh", "š").Replace("uu", "ū");
        }

        private string SimpleNormalize(string value)
        {
            return value.ToLower()
                .Replace("aa", "a").Replace("ch", "c").Replace("ee", "e")
                .Replace("ii", "i").Replace("sh", "s").Replace("uu", "u");
        }

        private string SplitPath(string path)
        {
            return string.Join(" ", path.Split('\\', '.', '_'));
        }
    }
}
