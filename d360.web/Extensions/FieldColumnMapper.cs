using d360.web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace d360.web.Extensions
{
    public class FieldColumnMapper
    {
        private List<FieldColumnMapping> _fieldColumnMappings;
        private DetailReadOnlyModel _model;

        public FieldColumnMapper(List<FieldColumnMapping> columnData, DetailReadOnlyModel model)
        {
            _fieldColumnMappings = columnData != null ? columnData : new List<FieldColumnMapping>();
            _model = model;
        }

        public List<FieldColumnMapping> FieldColumnMappings
        {
            get
            {
                return _fieldColumnMappings;
            }
        }

        public void TransformRowsAndCols()
        {
            int row = 1;
            int col = 1;
            bool currentDisplayState = false;
            foreach (var item in _fieldColumnMappings)
            {
                bool displayInColumn = item.DisplayInColumn.HasValue && item.DisplayInColumn.Value == true;

                if (displayInColumn != currentDisplayState || displayInColumn == false)
                {
                    row = _fieldColumnMappings.Max(x => x.Row) + 1;
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
            }
        }

        public void ArrangeRowsAndCols(List<DetailReadOnlyRowModel> dynamicRows)
        {
            foreach (var drow in dynamicRows)
            {
                var row = _fieldColumnMappings.FirstOrDefault(x => x.Name == drow.FirstColumnFields.FirstOrDefault().FieldName).Row;
                drow.FirstColumnFields.ForEach(x => x.Row = row);
            }

            foreach (var drow in dynamicRows)
            {
                var row = _fieldColumnMappings.FirstOrDefault(x => x.Name == drow.FirstColumnFields.FirstOrDefault().FieldName).Row;

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
        public bool? DisplayInColumn { get; set; }
        public int Row { get; set; }
        public int Col { get; set; }
    }
}