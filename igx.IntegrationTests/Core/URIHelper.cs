using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace igx.IntegrationTests.Core
{
    public static class URIHelper
    {
        public static string AssetClassesUri = Settings.Host + "/api/v2/assets/classes";
        public static string AssetTypesUri = Settings.Host + "/api/v2/assets/types";
        public static string AssetsUri = Settings.Host + "/api/v2/assets/";
        public static string AssetFieldsUri = Settings.Host + "/api/v2/assets/fields/";

        public static string AssetsBatchUri = Settings.Host + "/api/v2/assets/batch/";
        public static string XRefUri = Settings.Host + "/api/v2/crossreferences";
        public static string FieldsUri = Settings.Host + "/api/v2/fields";

        public static string WorkflowTypesUri = Settings.Host + "/api/v2/workflow/types";
        public static string WorkflowVersionUriWithPageSize = Settings.Host + "/api/v2/workflow/versions?_pageSize=10000";
        public static string WorkflowVersionUriWithoutPageSize = Settings.Host + "/api/v2/workflow/versions";

        public static string MetricsUri = Settings.Host + "/api/v2/metrics";

        public static string TagUri = Settings.Host + "/api/v2/tags";

        public static string ResponsibilitiesUri = Settings.Host + "/api/v2/responsibilities";


    }
}
