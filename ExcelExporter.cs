using System;
using System.IO;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;

namespace PdfToolbox
{
    public class ExcelExporter
    {
        public static void ExportToExcel(string pdfPath, string excelPath)
        {
            using (SpreadsheetDocument spreadsheetDocument = SpreadsheetDocument.Create(excelPath, SpreadsheetDocumentType.Workbook))
            {
                WorkbookPart workbookpart = spreadsheetDocument.AddWorkbookPart();
                workbookpart.Workbook = new Workbook();

                WorksheetPart worksheetPart = workbookpart.AddNewPart<WorksheetPart>();
                worksheetPart.Worksheet = new Worksheet(new SheetData());

                Sheets sheets = spreadsheetDocument.WorkbookPart.Workbook.AppendChild<Sheets>(new Sheets());
                Sheet sheet = new Sheet()
                {
                    Id = spreadsheetDocument.WorkbookPart.GetIdOfPart(worksheetPart),
                    SheetId = 1,
                    Name = "Extracted Data"
                };
                sheets.Append(sheet);

                SheetData sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();

                using (PdfReader reader = new PdfReader(pdfPath))
                using (PdfDocument pdfDoc = new PdfDocument(reader))
                {
                    for (int i = 1; i <= pdfDoc.GetNumberOfPages(); i++)
                    {
                        var strategy = new LocationTextExtractionStrategy();
                        string text = PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(i), strategy);

                        string[] lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                        
                        foreach (var line in lines)
                        {
                            Row row = new Row();
                            
                            // Divide a linha em colunas se houver 2 ou mais espaços vazios seguidos
                            string[] columns = Regex.Split(line, @"\s{2,}");
                            
                            foreach (var col in columns)
                            {
                                Cell cell = new Cell() { DataType = CellValues.String, CellValue = new CellValue(col.Trim()) };
                                row.Append(cell);
                            }
                            
                            sheetData.Append(row);
                        }
                    }
                }
                
                worksheetPart.Worksheet.Save();
                workbookpart.Workbook.Save();
            }
        }
    }
}
