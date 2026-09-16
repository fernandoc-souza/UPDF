using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media;
using iText.Kernel.Pdf;

namespace PdfToolbox
{
    public class PageItem
    {
        public string SourcePdfPath { get; set; } = string.Empty;
        public int OriginalPageNumber { get; set; }
        public ImageSource? Thumbnail { get; set; }
        public int DisplayPageNumber { get; set; }
    }

    public static class PageOrganizer
    {
        public static void OrganizeAndSave(List<PageItem> pages, string outputPath)
        {
            // Grava num arquivo temporário primeiro: se outputPath coincidir com um dos
            // PDFs de origem (ex.: usuário reorganiza e salva por cima do próprio arquivo),
            // abrir o PdfWriter direto no outputPath truncaria a origem antes de lê-la.
            string tempPath = outputPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (PdfWriter writer = new PdfWriter(tempPath))
                using (PdfDocument destDoc = new PdfDocument(writer))
                {
                    Dictionary<string, PdfDocument> openedDocs = new Dictionary<string, PdfDocument>();
                    try
                    {
                        foreach (var page in pages)
                        {
                            if (!openedDocs.ContainsKey(page.SourcePdfPath))
                            {
                                openedDocs[page.SourcePdfPath] = new PdfDocument(new PdfReader(page.SourcePdfPath));
                            }

                            PdfDocument srcDoc = openedDocs[page.SourcePdfPath];
                            srcDoc.CopyPagesTo(page.OriginalPageNumber, page.OriginalPageNumber, destDoc);
                        }
                    }
                    finally
                    {
                        foreach (var doc in openedDocs.Values)
                        {
                            doc.Close();
                        }
                    }
                }

                File.Copy(tempPath, outputPath, overwrite: true);
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
        }
    }
}
