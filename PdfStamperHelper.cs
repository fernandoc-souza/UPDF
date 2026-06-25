using System;
using System.IO;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.IO.Image;
using iText.Layout.Properties;

namespace PdfToolbox
{
    public static class PdfStamperHelper
    {
        public static void StampText(string sourcePdf, string targetPdf, string text, int pageNumber, float x, float y, string fontName, float fontSize, Color color)
        {
            using (PdfReader reader = new PdfReader(sourcePdf))
            using (PdfWriter writer = new PdfWriter(targetPdf))
            using (PdfDocument pdfDoc = new PdfDocument(reader, writer))
            {
                Document document = new Document(pdfDoc);
                
                int totalPages = pdfDoc.GetNumberOfPages();
                int startPage = pageNumber <= 0 ? 1 : pageNumber;
                int endPage = pageNumber <= 0 ? totalPages : pageNumber;

                PdfFont font = PdfFontFactory.CreateFont(fontName);

                for (int i = startPage; i <= endPage; i++)
                {
                    if (i > totalPages) break;
                    
                    Paragraph p = new Paragraph(text)
                        .SetFont(font)
                        .SetFontSize(fontSize)
                        .SetFontColor(color);
                    
                    document.ShowTextAligned(p, x, y, i, TextAlignment.CENTER, VerticalAlignment.MIDDLE, 0);
                }
                document.Close();
            }
        }

        public static void StampImage(string sourcePdf, string targetPdf, string imagePath, int pageNumber, float x, float y, float targetWidth, float targetHeight)
        {
            using (iText.Kernel.Pdf.PdfReader reader = new iText.Kernel.Pdf.PdfReader(sourcePdf))
            using (iText.Kernel.Pdf.PdfWriter writer = new iText.Kernel.Pdf.PdfWriter(targetPdf))
            using (iText.Kernel.Pdf.PdfDocument document = new iText.Kernel.Pdf.PdfDocument(reader, writer))
            {
                iText.Layout.Element.Image img = new iText.Layout.Element.Image(iText.IO.Image.ImageDataFactory.Create(imagePath));
                
                // Redimensiona a imagem para a largura e altura exatas
                img.ScaleAbsolute(targetWidth, targetHeight);

                int totalPages = document.GetNumberOfPages();

                int startPage = pageNumber == 0 ? 1 : pageNumber;
                int endPage = pageNumber == 0 ? totalPages : pageNumber;

                for (int i = startPage; i <= endPage; i++)
                {
                    if (i > totalPages) break;
                    
                    // x, y devem ser as coordenadas do canto inferior esquerdo no PDF.
                    img.SetFixedPosition(i, x, y);
                    iText.Layout.Document layoutDoc = new iText.Layout.Document(document);
                    layoutDoc.Add(img);
                }
            }
        }
    }
}
