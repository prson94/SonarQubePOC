using DocumentFormat.OpenXml.Spreadsheet;
using SpreadsheetLight;
using System.Globalization;
using System.IO;

namespace d360.utils.excel
{
    public class SLDocumentFactory
    {
        /// <summary>
        /// This creates a full working copy of excel-document provided in stream
        /// This method was introduced because documents constructed 
        ///     by new SLDocument(Stream) constuctor were invalid after saving
        ///     
        /// Still, this method have some limitations. 
        /// 
        /// Next things aren't supported yet, but probably can be supported:
        ///  • Column/row sizing
        ///  • Mixed formatting inside of cell
        ///  • Several sheets
        ///  
        /// Next things can't be supported:
        ///  • Cell formulas (they are replaced with formula calculation result)
        ///  • Merged cells (they become unmerged)
        ///  • Any complex things like charts or etc
        /// </summary>
        /// <returns>Copy of SLDocument</returns>
        public static SLDocument CreateCopyFrom(Stream stream)
        {
            var newDocument = new SLDocument();

            var originalDocument = new SLDocument(stream);
            foreach (var rowInfo in originalDocument.GetCells())
            {
                var row = rowInfo.Key;
                var cells = rowInfo.Value;
                foreach (var cellInfo in cells)
                {
                    var column = cellInfo.Key;
                    var cell = cellInfo.Value;

                    SetCell(newDocument, row, column, originalDocument, cell);
                    newDocument.SetCellStyle(row, column, originalDocument.GetCellStyle(row, column));
                }
            }

            return newDocument;
        }

        private static void SetCell(SLDocument newDocument, int row, int column, SLDocument originalDocument, SLCell c)
        {
            if (c.CellText != null)
            {
                newDocument.SetCellValue(row, column, c.CellText);
                return;
            }

            if (c.DataType == CellValues.SharedString)
            {
                var text = originalDocument.GetSharedStrings()[(int)c.NumericValue].GetText();
                newDocument.SetCellValue(row, column, text);
                return;
            }

            newDocument.SetCellValueNumeric(row, column, c.NumericValue.ToString(CultureInfo.InvariantCulture));
        }
    }
}