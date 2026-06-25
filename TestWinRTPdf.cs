using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Data.Pdf;
using Windows.Storage;
using Windows.Storage.Streams;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Starting test...");
        try
        {
            // Create a dummy PDF with iText7
            string testPdf = "test_render.pdf";
            using (var writer = new iText.Kernel.Pdf.PdfWriter(testPdf))
            using (var pdf = new iText.Kernel.Pdf.PdfDocument(writer))
            {
                pdf.AddNewPage();
            }

            Console.WriteLine("Created test PDF.");

            string fullPath = Path.GetFullPath(testPdf);
            Console.WriteLine("Path: " + fullPath);

            StorageFile file = await StorageFile.GetFileFromPathAsync(fullPath);
            Console.WriteLine("Got StorageFile");

            PdfDocument pdfDoc = await PdfDocument.LoadFromFileAsync(file);
            Console.WriteLine("Loaded PdfDocument with " + pdfDoc.PageCount + " pages.");

            for (uint i = 0; i < pdfDoc.PageCount; i++)
            {
                using (PdfPage page = pdfDoc.GetPage(i))
                {
                    using (InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream())
                    {
                        var options = new PdfPageRenderOptions { DestinationWidth = 400 };
                        Console.WriteLine("Rendering page " + i);
                        await page.RenderToStreamAsync(stream, options);
                        Console.WriteLine("Stream size: " + stream.Size);
                        
                        Stream netStream = stream.AsStream();
                        Console.WriteLine("Net Stream Position: " + netStream.Position);
                    }
                }
            }

            Console.WriteLine("Success.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Caught Exception: " + ex.ToString());
        }
    }
}
