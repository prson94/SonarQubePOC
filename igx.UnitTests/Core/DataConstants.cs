using d360.core.entities;
using d360.core.enums;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace igx.UnitTests.Core
{
    public class DataConstants
    {
        public const string ValidDataSource = "ValidDataSource";
        public const string ValidType = "ValidType";
        public const string ValidGUID = "f8bf1431-0d7b-4381-9cec-dd32c05e0158";
        public const string ValidGUID2 = "f8bf1431-0d7b-4381-9cec-dd32c05e0159";
        public const string WrongFormatGUID = "B1F828DE-BD5FB-451A-A472-77BF5916F771AAA";
        public const string InvalidGUID = "00000000-0000-0000-0000-000000000000";
        public const string FieldTypesJsonFormat = "[{\"FriendlyName\":\"Name\",\"ID\":49,\"IsListable\":false,\"IsRequired\":false,\"ColumnOrder\":1,\"SortOrder\":0,\"ObjectType\":\"ArtifactType\",\"ObjectID\":50001,\"Type\":\"Text\"}]";

        public class Tags
        {
            public const string ValidName = "valid_tag_name";
        }


        public static IEnumerable<PredicateApiViewModel> GetPredicates()
        {
            return new List<PredicateApiViewModel>(){
                new PredicateApiViewModel() { Name = "Test name", Inverse = "Inverse", IsSystem = true, Type = PredicateType.DataLineage, Uid = Guid.Parse(ValidGUID), IsInUse = true },
                new PredicateApiViewModel(){ Name ="", Inverse = "", IsInUse = true},
                new PredicateApiViewModel(){ Name ="", Inverse = "", IsInUse = true},
                new PredicateApiViewModel(){ Name ="", Inverse = "", IsInUse = false}
            };
        }

        public static List<dynamic> GetExcelModel()
        {
            dynamic data = new ExpandoObject();
            data.UID = Guid.Empty;
            data.ID = 1;
            data.Subject = "";
            data.SubjectID = 1;
            data.SubjectUid = Guid.Empty;
            data.SubjectName = "";
            data.SubjectTypeName = "";
            data.PredicateName = "";
            data.Object = "";
            data.ObjectID = 1;
            data.ObjectUid = Guid.Empty;
            data.ObjectName = "";
            data.ObjectTypeName ="";


            return new List<dynamic>() { data as dynamic };
        }

    }
}
