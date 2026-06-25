using System;
using System.Collections.Generic;
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
            using (PdfWriter writer = new PdfWriter(outputPath))
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
        }
    }
}
