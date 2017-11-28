using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace igx.functions.integration.igc
{
    public class FusionRelationshipModel
    {
        public string StartID { get; set; }
        public string EndID { get; set; }
        public string Action { get; set; }
    }

    public class BulkFusionImport
    {
        public List<Dictionary<string, string>> Models { get; set; }
        public List<IDictionary<string, string>> QueryItems { get; set; }
        public List<FusionRelationshipModel> Relationships { get; set; }
        public string Version { get; set; }
        public List<string> Errors { get; set; }
    }


    internal class DatabaseModel
    {
        public string short_description { get; set; }
        public string long_description { get; set; }
        public string _name { get; set; }
        public string _id { get; set; }
        public DatabaseSchemasModel database_schemas { get; set; }
    }
    internal class DatabaseSchemasModel
    {
        public List<DatabaseSchemaModel> items { get; set; }

    }
    internal class DatabaseSchemaModel
    {
        public DatabaseModel database { get; set; }
        public string _type { get; set; }
        public string _id { get; set; }
        public string _name { get; set; }
        public string short_description { get; set; }
        public string long_description { get; set; }
        public DatabaseTablesModel database_tables { get; set; }
    }

    internal class DatabaseTablesModel
    {
        public List<DatabaseTableModel> items { get; set; }

    }
    internal class DatabaseTableModel
    {
        public DatabaseSchemaModel database_schema { get; set; }

        public string _type { get; set; }
        public string _id { get; set; }
        public string _name { get; set; }
        public string short_description { get; set; }
        public string long_description { get; set; }
        public DatabaseColumnsModel database_columns { get; set; }
    }

    internal class DatabaseColumnsModel
    {
        public List<DatabaseColumnModel> items { get; set; }

    }
    internal class DatabaseColumnModel
    {
        public string _type { get; set; }
        public string _id { get; set; }
        public string _name { get; set; }
    }
}
