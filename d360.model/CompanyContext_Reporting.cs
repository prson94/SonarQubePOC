using d360.core;
using d360.core.entities;
using Dapper;
using gudusoft.gsqlparser;
using System;
using System.Collections.Generic;


namespace d360.model
{
    partial class CompanyContext: BaseContext
    {        
        public IEnumerable<dynamic> GetReportQueryResults(int reportTileID, SystemObjects type, int id)
        {
            return Query<dynamic>(@"
declare @commandText nvarchar(max)
select @commandText = CommandText from ReportTile where ID = @id
set  @commandText = REPLACE(@commandText, '[TYPE]', @t)
set  @commandText = REPLACE(@commandText, '[ID]', @i)
exec sp_executesql @commandText", new { id = reportTileID, t = new Dapper.DbString { Value = type.ToString(), IsAnsi = true }, i = id }, 180);
        }
        
        public bool IsValidReportingQuery(string statement)
        {
            bool isValid = false;

            var dbv = TDbVendor.DbVMssql;
            var parser = new TGSqlParser(dbv);
            parser.SqlText.Text = statement;
            parser.Parse();
            isValid = (parser.SqlStatements[0] is TSelectSqlStatement);
            

            return isValid;
        }

        public List<string> SelectQueryColumns(string statement)
        {
            bool isValid = false;
            List<string> columns = new List<string>();

            var dbv = TDbVendor.DbVMssql;
            var parser = new TGSqlParser(dbv);
            parser.SqlText.Text = statement;
            parser.Parse();
            isValid = (parser.SqlStatements[0] is TSelectSqlStatement);
            
            if (!isValid) throw new Exception("Non-select statement specified to function that gets columns from select statements.");

            TSelectSqlStatement select = (TSelectSqlStatement)parser.SqlStatements[0];
            
            foreach (var field in select.Fields)
            {
                columns.Add((field.DisplayName ?? "").Replace("[", "").Replace("]", "").Replace("'", ""));
            }
            return columns;
        }        
    }
}