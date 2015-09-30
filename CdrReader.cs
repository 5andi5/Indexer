using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            corelDraw.OpenDocument(path, 1);    
            foreach (CD.Page page in corelDraw.ActiveDocument.Pages)
            {
                ExtractText(page.Shapes.All());
            }
            (corelDraw.ActiveDocument as CD.IDrawDocument).Close();
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

        public void Dispose()
        {
            this.corelDraw.Quit();
        }
    }
}
