using System;
using System.IO;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;

namespace PdfToolbox
{
    public class WordExporter
    {
        public static void ExportToWord(string pdfPath, string wordPath)
        {
            using (WordprocessingDocument wordDocument = WordprocessingDocument.Create(wordPath, WordprocessingDocumentType.Document))
            {
                MainDocumentPart mainPart = wordDocument.AddMainDocumentPart();
                mainPart.Document = new Document();
                Body body = mainPart.Document.AppendChild(new Body());

                using (PdfReader reader = new PdfReader(pdfPath))
                using (PdfDocument pdfDoc = new PdfDocument(reader))
                {
                    for (int i = 1; i <= pdfDoc.GetNumberOfPages(); i++)
                    {
                        var strategy = new LocationTextExtractionStrategy();
                        string text = PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(i), strategy);

                        string[] paragraphs = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

                        foreach (var para in paragraphs)
                        {
                            Paragraph p = new Paragraph();
                            Run r = new Run();
                            Text t = new Text(para);
                            r.Append(t);
                            p.Append(r);
                            body.Append(p);
                        }

                        // Add page break if not last page
                        if (i < pdfDoc.GetNumberOfPages())
                        {
                            Paragraph pBreak = new Paragraph();
                            Run rBreak = new Run();
                            Break br = new Break() { Type = BreakValues.Page };
                            rBreak.Append(br);
                            pBreak.Append(rBreak);
                            body.Append(pBreak);
                        }
                    }
                }
                
                mainPart.Document.Save();
            }
        }
    }
}
