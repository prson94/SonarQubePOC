using System;
using System.Reflection;

namespace d360.core.enums
{
    public static class EnumExtensions
    {
        public static string GetSqlCaseFilterStatement(this Enum list, string columnName)
        {
            string sql = $"(case {columnName} ";
            foreach (MemberInfo tm in list.GetType().GetMembers(BindingFlags.Public | BindingFlags.Static))
            {
                sql += $"when {(int)Enum.Parse(list.GetType(), tm.Name)} then '{tm.Name}' ";
            }
            sql += "end)";

            return sql;
        }
    }
}
