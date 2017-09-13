using d360.core;
using d360.core.entities;
using Dapper;
using gudusoft.gsqlparser;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace d360.model
{
    partial class CompanyContext: BaseContext
    {
        #region DbSets

        //public DbSet<WorkflowEventRegistration> StatisticDetails { get; set; }

        #endregion

        #region Engine Methods

        public IEnumerable<dynamic> GetReportQueryResults(int reportTileID, SystemObjects type, int id)
        {
            return Query<dynamic>(@"
declare @commandText nvarchar(max)
select @commandText = CommandText from ReportTile where ID = @id
set  @commandText = REPLACE(@commandText, '[TYPE]', @t)
set  @commandText = REPLACE(@commandText, '[ID]', @i)
exec sp_executesql @commandText", new { id = reportTileID, t = new Dapper.DbString { Value = type.ToString(), IsAnsi = true }, i = id }, 180);
        }

        public class SqlStatementValidityTest
        {
            public SqlStatementValidityTest()
            {
                IsValid = false;
                Results = new List<SqlStatementValidityTestResult>();
            }

            public bool IsValid { get; set; }

            public List<SqlStatementValidityTestResult> Results { get; set; }
        }

        public class SqlStatementValidityTestResult
        {
            public string ErrorToken { get; set; }
            public int XPosition { get; set; }
            public int YPosition { get; set; }
            public string ErrorMessage { get; set; }
        }

        public bool IsValidReportingQuery(string statement)
        {
            bool isValid = false;

            var dbv = TDbVendor.DbVMssql;
            var parser = new TGSqlParser(dbv);
            parser.SqlText.Text = statement;
            parser.Parse();
            isValid = (parser.SqlStatements[0] is TSelectSqlStatement);
            //TSelectSqlStatement selectStatement
            //TSqlStatementType.sstMssqlSelect

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
            //TSelectSqlStatement selectStatement
            //TSqlStatementType.sstMssqlSelect

            if (!isValid) throw new Exception("Non-select statement specified to function that gets columns from select statements.");

            TSelectSqlStatement select = (TSelectSqlStatement)parser.SqlStatements[0];
            var fields = select.Fields;
            foreach (var field in select.Fields)
            {
                columns.Add((field.DisplayName ?? "").Replace("[", "").Replace("]", "").Replace("'", ""));
            }
            return columns;
        }

        public List<ReportSchemaModel> GetReportingSchema()
        {
            string k = key(REPORTING_SCHEMA_KEY, CurrentCompanyID);
            if (Caching.ItemExists<List<ReportSchemaModel>>(k))
            {
                return Caching.GetItem<List<ReportSchemaModel>>(k);
            }
            else
            {
                var models = Query<ReportSchemaModel>(
@"select	distinct 
		SUBSTRING(TABLE_NAME, 0, CHARINDEX('_', TABLE_NAME)) as ID,
        NULL as ParentID,
        SUBSTRING(TABLE_NAME, 0, CHARINDEX('_', TABLE_NAME)) as Name,
        TABLE_SCHEMA as [Schema],
        0 as [Position],
        'Group' as [Type]
from	[INFORMATION_SCHEMA].[VIEWS] 
where	TABLE_SCHEMA = 'reporting'
union
select	TABLE_NAME as ID,
        SUBSTRING(TABLE_NAME, 0, CHARINDEX('_', TABLE_NAME)) as ParentID,
        TABLE_NAME as Name,
        TABLE_SCHEMA as [Schema],
        0 as [Position],
        'View' as [Type]
from	[INFORMATION_SCHEMA].[TABLES] 
where	TABLE_SCHEMA = 'reporting'
union
select	TABLE_NAME as ID,
        SUBSTRING(TABLE_NAME, 0, CHARINDEX('_', TABLE_NAME)) as ParentID,
        TABLE_NAME as Name,
        TABLE_SCHEMA as [Schema],
        0 as [Position],
        'View' as [Type]
from	[INFORMATION_SCHEMA].[VIEWS] 
where	TABLE_SCHEMA = 'reporting'
union
select	TABLE_NAME + cast(ORDINAL_POSITION as varchar(10)) as ID,
        TABLE_NAME as ParentID,
        COLUMN_NAME as Name,
        TABLE_SCHEMA as [Schema],
        ORDINAL_POSITION as [Position],
        'Column' as [Type]
from	[INFORMATION_SCHEMA].[COLUMNS]
where	TABLE_SCHEMA = 'reporting'").ToList();

                var altered = loadSchemaChildren(models, null);
                Caching.SetItem<List<ReportSchemaModel>>(k, altered, true, 5);
                return altered;
            }

        }

        List<ReportSchemaModel> loadSchemaChildren(List<ReportSchemaModel> schemaItems, string parentID)
        {
            var array = new List<ReportSchemaModel>();

            foreach (var c in schemaItems.Where(i => i.ParentID == parentID).OrderBy(i => i.Position).ThenBy(i => i.Name))
            {
                c.Items = loadSchemaChildren(schemaItems, c.ID);
                array.Add(c);
            }

            return array;
        }

        #endregion
    }
}
