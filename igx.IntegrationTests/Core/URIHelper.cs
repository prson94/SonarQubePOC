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
        public static string AssetsBatchUri = Settings.Host + "/api/v2/assets/batch/";
    }
}
