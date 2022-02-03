using System;
using d360.core;

namespace d360.model.DataAccessLayer
{
    public class OrderByModel
    {
        public string ColumnName { get; set; }

        public OrderByDirectionEnum Direction { get; set; }

        public static OrderByModel Create(string columnName, string direction)
        {
            return Create(columnName, ValidateOrderByDirection(direction));
        }

        public static OrderByModel Create(string columnName, OrderByDirectionEnum direction)
        {
            Preconditions.NotEmpty(columnName, nameof(columnName));
            Preconditions.IsDefined(direction, nameof(direction));

            var result = new OrderByModel();
            result.ColumnName = columnName;
            result.Direction = direction;
            return result;
        }

        private class AllowedDirection
        {
            public string Text { get; set; }
            public OrderByDirectionEnum Value { get; set; }
        }

        private static OrderByDirectionEnum ValidateOrderByDirection(string direction, OrderByDirectionEnum defaultDirection = OrderByDirectionEnum.Descending)
        {
            var allowedDirections = new[]
            {
                new AllowedDirection { Text = "asc", Value = OrderByDirectionEnum.Ascending },
                new AllowedDirection { Text = "desc", Value = OrderByDirectionEnum.Descending },
            };

            if (string.IsNullOrEmpty(direction) == false)
            {
                return Preconditions.Exists<string, AllowedDirection>(nameof(direction), direction, allowedDirections,
                    (value, allowed) => string.Equals(allowed.Text, value, StringComparison.OrdinalIgnoreCase)).Value;
            }

            return defaultDirection;
        }
    }
}
