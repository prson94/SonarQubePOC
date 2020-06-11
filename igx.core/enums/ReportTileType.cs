using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace d360.core.enums
{
    public enum ReportTileType
    {
        [Name("Table"), Description("Displays a table of data"), Icon("fa-table")]
        Table = 1,
        [Name("Pie Chart"), Description("Displays a pie chart"), Icon("fa-pie-chart")]
        Pie = 2,
        [Name("Area Chart"), Description("Displays a area chart"), Icon("fa-area-chart")]
        Area = 3,
        [Name("Bar Chart"), Description("Displays a bar chart"), Icon("fa-bar-chart")]
        Bar = 4,
        [Name("Line Chart"), Description("Displays a line chart"), Icon("fa-line-chart")]
        Line = 5,
        [Name("Matrix"), Description("Displays a matrix"), Icon("fa-th")]
        Matrix = 6
    }

    public class ReportTileTypeInfo
    {
        public ReportTileType ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
    }

    public static class ReportTileTypeExtensions
    {
        public static string GetReportTileTypeIcon(this ReportTileType type)
        {
            return type.GetType().GetMember(type.ToString()).Single().GetCustomAttribute<IconAttribute>().Icon;
        }

        public static string GetReportTileTypeDisplayName(this ReportTileType type)
        {
            return type.GetType().GetMember(type.ToString()).Single().GetCustomAttribute<NameAttribute>().Name;
        }

        public static string GetReportTileTypeDescription(this ReportTileType type)
        {
            return type.GetType().GetMember(type.ToString()).Single().GetCustomAttribute<DescriptionAttribute>().Description;
        }

        public static List<ReportTileTypeInfo> GetReportTileTypeEnumList(this ReportTileType type)
        {
            var list = new List<ReportTileTypeInfo>();

            foreach (MemberInfo tm in type.GetType().GetMembers(BindingFlags.Public | BindingFlags.Static))
            {
                list.Add(new ReportTileTypeInfo
                {
                    Name = ((NameAttribute)tm.GetCustomAttribute(typeof(NameAttribute))).Name,
                    Icon = ((IconAttribute)tm.GetCustomAttribute(typeof(IconAttribute))).Icon,
                    Description = ((DescriptionAttribute)tm.GetCustomAttribute(typeof(DescriptionAttribute))).Description,
                    ID = (ReportTileType)Enum.Parse(typeof(ReportTileType), tm.Name)
                });
            }

            return list;
        }
    }
}
