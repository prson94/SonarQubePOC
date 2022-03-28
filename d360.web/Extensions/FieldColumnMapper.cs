using System.Collections.Generic;
using System.Linq;

using d360.web.Models;

namespace d360.web.Extensions
{
    public class FieldColumnMapper
    {
        private readonly List<FieldColumnMapping> _fieldColumnMappings;
        private readonly DetailReadOnlyModel _model;

        public FieldColumnMapper(List<FieldColumnMapping> columnData, DetailReadOnlyModel model)
        {
            _fieldColumnMappings = columnData != null ? columnData : new List<FieldColumnMapping>();
            _model = model;
        }

        public List<FieldColumnMapping> FieldColumnMappings => _fieldColumnMappings;

        public void TransformRowsAndCols()
        {
            int row = 1;
            int col = 1;
            bool currentDisplayState = false;
            string lastCategory = null;

            foreach (var item in _fieldColumnMappings.OrderBy(x => x.Category))
            {
                if (lastCategory == null)
                {
                    lastCategory = item.Category ?? "";
                }
                string currentCategory = item.Category ?? "";
                bool displayInColumn = item.DisplayInColumn.HasValue && item.DisplayInColumn.Value == true;

                if (displayInColumn != currentDisplayState || displayInColumn == false || lastCategory != currentCategory)
                {
                    row = _fieldColumnMappings.Max(x => x.Row) + 1;
                    col = 1;
                }

                if (!displayInColumn)
                {
                    item.Row = row;
                    item.Col = 1;
                    col = 1;
                }
                else
                {
                    item.Row = row;
                    item.Col = col++;
                }

                currentDisplayState = displayInColumn;
                lastCategory = currentCategory;
            }
        }

        public void ArrangeRowsAndCols(List<DetailReadOnlyRowModel> dynamicRows)
        {
            foreach (var drow in dynamicRows)
            {
                var row = _fieldColumnMappings.FirstOrDefault(x => x.Name == drow.FirstColumnFields.FirstOrDefault().FieldName && x.Category == drow.Category).Row;
                drow.FirstColumnFields.ForEach(x => x.Row = row);
            }

            foreach (var drow in dynamicRows)
            {
                var row = _fieldColumnMappings.FirstOrDefault(x => x.Name == drow.FirstColumnFields.FirstOrDefault().FieldName && x.Category == drow.Category).Row;

                var refModel = _model.rows.FirstOrDefault(x => x.FirstColumnFields.FirstOrDefault().Row == row);
                
                if (refModel == null)
                {
                    _model.rows.Add(drow);
                }
                else
                {
                    refModel.FirstColumnFields.AddRange(drow.FirstColumnFields);
                }
            }
        }
    }

    public class FieldColumnMapping
    {
        public string Name { get; set; }

        public string Category { get; set; }

        public bool? DisplayInColumn { get; set; }

        public int Row { get; set; }

        public int Col { get; set; }
    }
}
