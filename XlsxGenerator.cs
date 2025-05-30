using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using WordParserLibrary.Model;
using WordParserLibrary.Helpers;

namespace WordParserLibrary
{
    public class XlsxGenerator
    {
        private readonly LegalAct legalAct;

        public XlsxGenerator(LegalAct legalAct)
        {
            this.legalAct = legalAct;
        }

        public MemoryStream GenerateXlsx()
        {
            var stream = new MemoryStream();

            using (var spreadsheetDocument = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
            {
                var workbookPart = spreadsheetDocument.AddWorkbookPart();
                workbookPart.Workbook = new Workbook();

                var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                var sheetData = new SheetData();
                worksheetPart.Worksheet = new Worksheet(sheetData);

                // Set column widths to auto
                var columns = new Columns();
                columns.Append(new Column { Min = 1, Max = 1, Width = 60, CustomWidth = true, BestFit = true });  // A
                columns.Append(new Column { Min = 2, Max = 2, Width = 8, CustomWidth = true, BestFit = true });  // B
                columns.Append(new Column { Min = 3, Max = 3, Width = 30, CustomWidth = true, BestFit = true });  // C
                columns.Append(new Column { Min = 4, Max = 4, Width = 20, CustomWidth = true, BestFit = true });  // D
                columns.Append(new Column { Min = 5, Max = 5, Width = 25, CustomWidth = true, BestFit = true });  // E
                worksheetPart.Worksheet.InsertAt(columns, 0);

                // Add header row
                var headerRow = new Row { RowIndex = 1 };
                headerRow.Append(
                    SpreadsheetHelper.CreateTextCell("A", 1, "Content"),
                    SpreadsheetHelper.CreateTextCell("B", 1, "Typ"),
                    SpreadsheetHelper.CreateTextCell("C", 1, "ID"),
                    SpreadsheetHelper.CreateTextCell("D", 1, "Journal"),
                    SpreadsheetHelper.CreateTextCell("E", 1, "Data wejścia w życie")
                );
                sheetData.Append(headerRow);


                // Add articles and their children
                uint currentRow = 2;
                foreach (var article in legalAct.Articles)
                {
                    sheetData.Append(ToXlsRow(article, currentRow));
                    currentRow++;
                    foreach (var subsection in article.Subsections)
                    {
                        sheetData.Append(ToXlsRow(subsection, currentRow));
                        currentRow++;
                        if (subsection.Amendments.Any())
                        {
                            foreach (var amendment in subsection.Amendments)
                            {
                                sheetData.Append(ToXlsRow(amendment, currentRow));
                                currentRow++;
                            }
                        }
                        foreach (var point in subsection.Points)
                        {
                            sheetData.Append(ToXlsRow(point, currentRow));
                            currentRow++;
                            if (point.Amendments.Any())
                            {
                                foreach (var amendment in point.Amendments)
                                {
                                    sheetData.Append(ToXlsRow(amendment, currentRow));
                                    currentRow++;
                                }
                            }
                            foreach (var letter in point.Letters)
                            {
                                sheetData.Append(ToXlsRow(letter, currentRow));
                                currentRow++;
                                if (letter.Amendments.Any())
                                {
                                    foreach (var amendment in letter.Amendments)
                                    {
                                        sheetData.Append(ToXlsRow(amendment, currentRow));
                                        currentRow++;
                                    }
                                }
                                foreach (var tiret in letter.Tirets)
                                {
                                    sheetData.Append(ToXlsRow(tiret, currentRow));
                                    currentRow++;
                                    if (tiret.Amendments.Any())
                                    {
                                        foreach (var amendment in tiret.Amendments)
                                        {
                                            sheetData.Append(ToXlsRow(amendment, currentRow));
                                            currentRow++;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // Add Sheets to the Workbook
                var sheets = spreadsheetDocument.WorkbookPart.Workbook.AppendChild(new Sheets());
                var sheet = new Sheet()
                {
                    Id = spreadsheetDocument.WorkbookPart.GetIdOfPart(worksheetPart),
                    SheetId = 1,
                    Name = "Articles"
                };
                sheets.Append(sheet);

                workbookPart.Workbook.Save();
            }

            stream.Position = 0;
            return stream;
        }

        public Row ToXlsRow(BaseEntity entity, uint rowIndex = 0)
        {
            var row = new Row { RowIndex = rowIndex };
            row.Append(
                SpreadsheetHelper.CreateTextCell("A", rowIndex, entity.ContentText),
                SpreadsheetHelper.CreateTextCell("B", rowIndex, entity.EntityType),
                SpreadsheetHelper.CreateTextCell("C", rowIndex, entity.Id),
                SpreadsheetHelper.CreateTextCell("D", rowIndex, entity.Article?.Journals.FirstOrDefault()?.ToString() ?? string.Empty),
                SpreadsheetHelper.CreateTextCell("E", rowIndex, entity.EffectiveDate.ToString("yyyy-MM-dd"))
            );
            return row;
        }
    }
}