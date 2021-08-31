using d360.web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace d360.web.Extensions
{
    public static class FieldColumnMappingExtensions
    {
        public static void TransformRowsAndCols(this List<FieldColumnMapping> fcMap)
        {
            int row = 1;
            int col = 1;
            bool currentDisplayState = false;
            foreach (var item in fcMap)
            {
                bool displayInColumn = item.DisplayInColumn.HasValue && item.DisplayInColumn.Value == true;

                if (displayInColumn != currentDisplayState || displayInColumn == false)
                {
                    row = fcMap.Max(x => x.Row) + 1;
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
    }
}