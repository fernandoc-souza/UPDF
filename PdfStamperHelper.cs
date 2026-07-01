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
using System.Collections.Generic;
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

                PdfFont font = PdfFontFactory.CreateRegisteredFont(fontName);

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
        public static void StampMultipleAnnotations(string sourcePdf, string targetPdf, Dictionary<int, List<AnnotationElement>> pageAnnotations)
        {
            string dirPath = System.IO.Path.GetDirectoryName(targetPdf);
            if (!System.IO.Directory.Exists(dirPath))
            {
                System.IO.Directory.CreateDirectory(dirPath);
            }

            // Registra as fontes do sistema para permitir o uso de qualquer fonte instalada no PC
            PdfFontFactory.RegisterSystemDirectories();

            using (PdfReader reader = new PdfReader(sourcePdf))
            using (PdfWriter writer = new PdfWriter(targetPdf))
            using (PdfDocument pdfDoc = new PdfDocument(reader, writer))
            {
                Document document = new Document(pdfDoc);

                foreach (var kvp in pageAnnotations)
                {
                    int pageNum = kvp.Key;
                    var elements = kvp.Value;
                    if (elements.Count == 0) continue;

                    iText.Kernel.Geom.Rectangle pageSize = pdfDoc.GetPage(pageNum).GetPageSizeWithRotation();
                    float pdfW = pageSize.GetWidth();
                    float pdfH = pageSize.GetHeight();

                    foreach (var ann in elements)
                    {
                        var fe = ann.Element;
                        double canvasW = ann.CanvasWidth;
                        double canvasH = ann.CanvasHeight;
                        
                        double elementLeft = System.Windows.Controls.Canvas.GetLeft(fe);
                        double elementTop = System.Windows.Controls.Canvas.GetTop(fe);
                        double elementWidth = fe.ActualWidth > 0 ? fe.ActualWidth : fe.Width;
                        double elementHeight = fe.ActualHeight > 0 ? fe.ActualHeight : fe.Height;

                        if (double.IsNaN(elementLeft)) elementLeft = 0;
                        if (double.IsNaN(elementTop)) elementTop = 0;

                        double bottomY = elementTop + elementHeight;

                        double ratioX = elementLeft / canvasW;
                        double ratioY = bottomY / canvasH;
                        double ratioWidth = elementWidth / canvasW;
                        double ratioHeight = elementHeight / canvasH;

                        float targetX = (float)(ratioX * pdfW);
                        float targetY = (float)(pdfH - (ratioY * pdfH));
                        float targetW = (float)(ratioWidth * pdfW);
                        float targetH = (float)(ratioHeight * pdfH);

                        if (fe is System.Windows.Controls.Grid container)
                        {
                            System.Windows.Shapes.Rectangle rect = null;
                            System.Windows.Controls.TextBox tb = null;

                            foreach (System.Windows.UIElement child in container.Children)
                            {
                                if (child is System.Windows.Shapes.Rectangle r && r.Fill != null && r.StrokeThickness == 2) rect = r;
                                if (child is System.Windows.Controls.TextBox t) tb = t;
                            }

                            if (rect != null && tb == null)
                            {
                                if (rect.Fill is System.Windows.Media.SolidColorBrush scb)
                                {
                                    var color = new iText.Kernel.Colors.DeviceRgb(scb.Color.R, scb.Color.G, scb.Color.B);
                                    float opacity = scb.Color.A / 255f;
                                    
                                    iText.Kernel.Pdf.Canvas.PdfCanvas pdfCanvas = new iText.Kernel.Pdf.Canvas.PdfCanvas(pdfDoc.GetPage(pageNum));
                                    iText.Kernel.Pdf.Extgstate.PdfExtGState gState = new iText.Kernel.Pdf.Extgstate.PdfExtGState();
                                    gState.SetFillOpacity(opacity);
                                    
                                    pdfCanvas.SaveState();
                                    pdfCanvas.SetExtGState(gState);
                                    pdfCanvas.SetFillColor(color);
                                    pdfCanvas.Rectangle(targetX, targetY, targetW, targetH);
                                    pdfCanvas.Fill();
                                    pdfCanvas.RestoreState();
                                }
                            }
                            else if (tb != null)
                            {
                                string text = tb.Text;
                                float pdfFontSize = (float)(tb.FontSize / canvasH * pdfH);
                                
                                var brush = tb.Foreground as System.Windows.Media.SolidColorBrush;
                                var color = brush != null ? new iText.Kernel.Colors.DeviceRgb(brush.Color.R, brush.Color.G, brush.Color.B) : iText.Kernel.Colors.ColorConstants.BLACK;

                                // Extrai a opacidade do texto
                                float textOpacity = brush != null ? brush.Color.A / 255f : 1f;

                                // Identifica a fonte selecionada
                                string fontName = tb.FontFamily?.Source ?? "Arial";
                                PdfFont font = null;
                                try
                                {
                                    font = PdfFontFactory.CreateRegisteredFont(fontName, iText.IO.Font.PdfEncodings.IDENTITY_H, PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED);
                                }
                                catch
                                {
                                    font = PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA);
                                }

                                if (font == null) font = PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA);

                                Paragraph p = new Paragraph(text)
                                    .SetFont(font)
                                    .SetFontSize(pdfFontSize)
                                    .SetFontColor(color);

                                // Se houver transparência, aplicamos na camada
                                if (textOpacity < 1f)
                                {
                                    iText.Kernel.Pdf.Canvas.PdfCanvas textCanvas = new iText.Kernel.Pdf.Canvas.PdfCanvas(pdfDoc.GetPage(pageNum));
                                    iText.Kernel.Pdf.Extgstate.PdfExtGState textGState = new iText.Kernel.Pdf.Extgstate.PdfExtGState();
                                    textGState.SetFillOpacity(textOpacity);
                                    textCanvas.SaveState();
                                    textCanvas.SetExtGState(textGState);
                                    
                                    new iText.Layout.Canvas(textCanvas, new iText.Kernel.Geom.Rectangle(targetX, targetY, targetW, targetH))
                                        .Add(p);
                                        
                                    textCanvas.RestoreState();
                                }
                                else
                                {
                                    document.ShowTextAligned(p, targetX, targetY + targetH, pageNum, TextAlignment.LEFT, VerticalAlignment.TOP, 0);
                                }
                            }
                        }
                    }
                }
                
                document.Close();
            }
        }
    }
}
