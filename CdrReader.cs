using System;
using System.Text;
using CD = CorelDRAW;

namespace CdrIndexer
{
    public class CdrReader:IDisposable
    {
        private CD.IDrawApplication corelDraw;
        private StringBuilder text;

        public CdrReader()
        {
            this.corelDraw = new CD.Application();
        }

        public string ReadText(string path)
        {
            this.text = new StringBuilder();
            CD.Document doc = corelDraw.OpenDocument(path, 1);
            foreach (CD.Page page in doc.Pages)
            {
                ExtractText(page.Shapes.All());
            }
            (corelDraw.ActiveDocument as CD.IDrawDocument).Close();
            CleanedUpText();
            return this.text.ToString();
        }

        private void ExtractText(CD.ShapeRange shapes)
        {
            foreach (CD.Shape shape in shapes)
            {
                if (shape.Type == CD.cdrShapeType.cdrTextShape)
                {
                    this.text.AppendLine(shape.Text.Contents);
                }
                if (shape.Type == CD.cdrShapeType.cdrGroupShape)
                {
                    ExtractText(shape.Shapes.All());
                }
            }
        }

        private void CleanedUpText()
        {
            this.text.Replace("\t", " ");
            while (this.text.ToString().IndexOf("  ") > 0)
            {
                this.text = this.text.Replace("  ", " ");
            }
        }

        public void Dispose()
        {
            this.corelDraw.Quit();
        }
    }
}
