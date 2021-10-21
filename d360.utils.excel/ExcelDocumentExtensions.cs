using SpreadsheetLight;
using System;
using System.Linq;

namespace d360.utils.excel
{
    public static class ExcelDocumentExtensions
    {
        public static SLDocument ToSLDocument(this ExcelDocument sourceDocument)
        {
            var document = new SLDocument();
            foreach (var sourceSheet in sourceDocument.Sheets)
            {
                document.AddWorksheet(sourceSheet.Name);
                document.SelectWorksheet(sourceSheet.Name);

                FillRows(sourceSheet);
                SetColumnSettings(sourceSheet);

                if (sourceSheet.FreezeHeaderRows)
                {
                    document.FreezePanes(NumberOfTopMostRows: sourceSheet.HeaderRows.Count, NumberOfLeftMostColumns: 0);
                }
            }

            if (sourceDocument.Sheets.Any(s => s.Name != SLDocument.DefaultFirstSheetName))
            {
                document.DeleteWorksheet(SLDocument.DefaultFirstSheetName);
            }

            return document;

            void FillRows(ExcelSheet sourceSheet)
            {
                int rowIndex = 1;
                foreach (var sourceRow in sourceSheet.HeaderRows.Concat(sourceSheet.ValueRows))
                {
                    int columnIndex = 1;
                    foreach (var cell in sourceRow.Cells)
                    {
                        if (cell is ExcelCell excelCell)
                        {
                            // Operator `?? ""` is required, because dynamic binding can't happen for null-objects
                            document.SetCellValue(rowIndex, columnIndex, excelCell.Value ?? "");
                            document.SetCellStyle(rowIndex, columnIndex, excelCell.Style);
                        }
                        else
                        {
                            // Operator `?? ""` is required, because dynamic binding can't happen for null-objects
                            document.SetCellValue(rowIndex, columnIndex, (dynamic)(cell ?? ""));
                        }

                        columnIndex++;
                    }

                    rowIndex++;
                }
            }

            void SetColumnSettings(ExcelSheet sourceSheet)
            {
                var columnsCount = sourceSheet.HeaderRows.Concat(sourceSheet.ValueRows).Select(x => x.Cells.Count).Aggregate(Math.Max);
                for (var columnIndex = 1; columnIndex < columnsCount + 1; columnIndex++)
                {
                    var columnSettings = sourceSheet.ColumnSettings.TryGetValue(columnIndex, out ExcelColumnSettings value)
                        ? value
                        : ExcelColumnSettings.Default;

                    if (columnSettings.Autofit)
                    {
                        document.AutoFitColumn(columnIndex);
                    }
                }
            }
        }
    }
}
