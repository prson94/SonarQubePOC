using SpreadsheetLight;
using System;
using System.Collections;
using System.Collections.Generic;

namespace d360.utils.excel
{
    public class ExcelDocument : IEnumerable<ExcelSheet>
    {
        public string Name { get; set; }

        public List<ExcelSheet> Sheets { get; set; }
            = new List<ExcelSheet>();

        public ExcelDocument(string name)
        {
            this.Name = name;
        }

        public void Add(ExcelSheet sheet)
        {
            this.Sheets.Add(sheet);
        }

        public IEnumerator<ExcelSheet> GetEnumerator() => Sheets.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => Sheets.GetEnumerator();
    }

    public class ExcelSheet
    {
        public string Name { get; set; }

        public List<ExcelRow> HeaderRows { get; set; }
            = new List<ExcelRow>();

        public List<ExcelRow> ValueRows { get; set; }
            = new List<ExcelRow>();

        public Dictionary<int, ExcelColumnSettings> ColumnSettings { get; set; }
            = new Dictionary<int, ExcelColumnSettings>();

        public bool FreezeHeaderRows { get; set; } = true;

        public ExcelSheet(string name)
        {
            this.Name = name;
        }
    }

    /// <summary>
    /// This stores ExcelCell | object
    /// When ExcelCell is stored, then we get value from ExcelCell
    /// When object is stored, then we consider object as value
    /// </summary>
    public class ExcelRow : IEnumerable<object>
    {
        public List<object> Cells { get; set; } = new List<object>();

        public void Add(object cell)
        {
            this.Cells.Add(cell);
        }

        public IEnumerator<object> GetEnumerator() => Cells.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => Cells.GetEnumerator();
    }

    public class ExcelCell
    {
        public dynamic Value { get; set; }

        public SLStyle Style { get; set; }

        public ExcelCell(dynamic value, SLStyle style)
        {
            this.Value = value;
            this.Style = style;
        }

        public static SLStyle MakeDefaultStyle() => new SLDocument().CreateStyle();

        public static SLStyle MakeStyle(Action<SLStyle> action)
        {
            var style = MakeDefaultStyle();
            action(style);
            return style;
        }
    }

    public class ExcelColumnSettings
    {
        public bool Autofit { get; set; }

        public static ExcelColumnSettings Default => new ExcelColumnSettings
        {
            Autofit = true
        };
    }
}
