using System.Linq;
using System.Reflection;

namespace d360.core.enums
{
    public enum BulkLoadType
    {
        [Name("P")]
        Promotion = 0,
        [Name("R")]
        Relation = 1,
        [Name("UR")]
        Unrelation = 2,
        [Name("UG")]
        UsersGroups = 3
    }

    public class BulkLoadTypeInfo
    {
        public ScoreType ID { get; set; }
        public string Name { get; set; }
    }

    public static class BulkLoadTypeClassExtensions
    {
        public static string GetDisplayName(this BulkLoadType type)
        {
            try
            {
                return type.GetType().GetMember(type.ToString()).Single().GetCustomAttribute<NameAttribute>().Name;
            }
            catch
            {
                return type.ToString();
            }
        }
    }
}