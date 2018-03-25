using ApplicationInsights.Helpers.WebJobs;
using d360.core.entities;
using igx.jobs.igc;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace igx.jobs
{
    class Program
    {
        static void Main()
        {
            var config = CoreFunction.GetJobHostConfiguration();
            config.UseTimers();

            var host = new JobHost(config);
            host.RunAndBlock();
        }
    }

    public static class IgcIntegration
    {
        const string functionName = "IGC_Integration";
        //const string timerSettings = "0 */30 * * * *";
        const string timerSettings = "*/5 * * * * *";

        #region State street Settings - NEED to Externalize

        const string TargetUri = "https://ssb-igx.uat.data3sixty.com/services/assets/";
        //const string TargetUri = "http://ssb-igx.dev.data3sixty.local/services/assets/";
        const string TargetAuthString = "w7gt581AOMXhXeW9mh0jWCPMe;3=f+7afAQUq9wUZgyibXq9kGa2iLGS3M0r-Ex-ZxJ6O9TAu+-7";

        //const string SourceUri = "https://192.168.99.100:9443/ibm/iis/igc-rest/v1/";
        //const string SourceAuthString = "Basic aXNhZG1pbjppc2FkbWlu";   //Local
        const string SourceUri = "https://edgm-catalog-uat.statestreet.com/ibm/iis/igc-rest/v1/";
        const string SourceAuthString = "Basic dGVzdDM2MDpkYXRhMzYw";   //State Street UAT
        //const string SourceUri = "https://edgm-catalog.statestreet.com/ibm/iis/igc-rest/v1/";
        //const string SourceAuthString = "Basic c3BsRFRTV0VCMjg2MjM6cChMWlsxfF1bYkl1";   //State Street PROD //UID: splDTSWEB28623    PWD: p(LZ[1|][bIu

        #region SSB Mappings

        static List<IntegrationAssetType> InputMappingTypes = new List<IntegrationAssetType> {
            new IntegrationAssetType { ID = 1, SourceAssetTypeName = "$ApplicationCatalog-ApplicationCatalog", Object = "ArtifactType", ObjectID = 2, Active = true },

            new IntegrationAssetType { ID = 2, SourceAssetTypeName = "$RRP-RRPFunctionalArea", Object = "TaxonomyType", ObjectID = 3 },
            new IntegrationAssetType { ID = 3, SourceAssetTypeName = "$RRP-RRPLevel1Service", Object = "TaxonomyType", ObjectID = 3 },
            new IntegrationAssetType { ID = 4, SourceAssetTypeName = "$RRP-RRPLevel2Service", Object = "TaxonomyType", ObjectID = 3 },
            new IntegrationAssetType { ID = 5, SourceAssetTypeName = "$RRP-RRPLevel3Service", Object = "TaxonomyType", ObjectID = 3 },

            new IntegrationAssetType { ID = 6, SourceAssetTypeName = "$BUOrg-BusinessUnitLevel1", Object = "TaxonomyType", ObjectID = 7 },
            new IntegrationAssetType { ID = 7, SourceAssetTypeName = "$BUOrg-BusinessUnitLevel2", Object = "TaxonomyType", ObjectID = 7 },
            new IntegrationAssetType { ID = 8, SourceAssetTypeName = "$BUOrg-BusinessUnitLevel3", Object = "TaxonomyType", ObjectID = 7 },
            new IntegrationAssetType { ID = 9, SourceAssetTypeName = "$BUOrg-BusinessUnitLevel4", Object = "TaxonomyType", ObjectID = 7 },
            new IntegrationAssetType { ID = 10, SourceAssetTypeName = "$BUOrg-BusinessUnitLevel5", Object = "TaxonomyType", ObjectID = 7 },
            new IntegrationAssetType { ID = 11, SourceAssetTypeName = "$BUOrg-BusinessUnitLevel6", Object = "TaxonomyType", ObjectID = 7 },
            new IntegrationAssetType { ID = 12, SourceAssetTypeName = "$BUOrg-BusinessUnitLevel7", Object = "TaxonomyType", ObjectID = 7 },

            new IntegrationAssetType { ID = 13, SourceAssetTypeName = "host", Object = "FusionAttributeType", ObjectID = 2 },
            new IntegrationAssetType { ID = 14, SourceAssetTypeName = "data_file", Object = "FusionAttributeType", ObjectID = 2 },
            new IntegrationAssetType { ID = 15, SourceAssetTypeName = "data_file_field", Object = "FusionAttributeType", ObjectID = 2 },
            new IntegrationAssetType { ID = 16, SourceAssetTypeName = "database", Object = "FusionAttributeType", ObjectID = 2 },
            new IntegrationAssetType { ID = 17, SourceAssetTypeName = "database_schema", Object = "FusionAttributeType", ObjectID = 2 },
            new IntegrationAssetType { ID = 18, SourceAssetTypeName = "database_table", Object = "FusionAttributeType", ObjectID = 2 },
            new IntegrationAssetType { ID = 19, SourceAssetTypeName = "database_view", Object = "FusionAttributeType", ObjectID = 2 },
            new IntegrationAssetType { ID = 20, SourceAssetTypeName = "data_element", Object = "FusionAttributeType", ObjectID = 2 }
        };

        static List<IntegrationAssetTypeFieldItem> InputMappingFieldItems = new List<IntegrationAssetTypeFieldItem> {
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 1, SourceField = "_id", TargetField = "SourceID", IncludeInPropertyRequest = false },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 1, SourceField = "_name", TargetField = "Name", IncludeInPropertyRequest = false },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 1, SourceField = "short_description", TargetField = "ShortDescription" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 1, SourceField = "long_description", TargetField = "LongDescription" },
            //new AssetTypeIntegrationFieldItem { MappingTypeId = 1, IgcField = "labels", GovernField = "" },
            //new AssetTypeIntegrationFieldItem { MappingTypeId = 1, IgcField = "stewards", GovernField = "" },
            //new AssetTypeIntegrationFieldItem { MappingTypeId = 1, IgcField = "assigned_to_terms", GovernField = "" },
            //new AssetTypeIntegrationFieldItem { MappingTypeId = 1, IgcField = "implements_rules", GovernField = "" },
            //new AssetTypeIntegrationFieldItem { MappingTypeId = 1, IgcField = "governed_by_rules", GovernField = "" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 1, SourceField = "$CMDBAppCode", TargetField = "CMDBAppCode" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 1, SourceField = "$ApplicationAlias", TargetField = "ApplicationAlias" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 1, SourceField = "$Comments", TargetField = "Comments" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 1, SourceField = "$SSID", TargetField = "SSID" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 1, SourceField = "$KeyApplicationType", TargetField = "KeyApplicationType", IsArray = true },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 1, SourceField = "$Status", TargetField = "Status" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 1, SourceField = "$DataLocation", TargetField = "DataLocation" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 1, SourceField = "$PersonalData", TargetField = "PersonalData" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 1, SourceField = "$ComponentType", TargetField = "ComponentType" },
            //new AssetTypeIntegrationFieldItem { MappingTypeId = 1, IgcField = "$ComponentCode", GovernField = "" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 1, SourceField = "$ComponentSAID", TargetField = "ComponentSAID" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 1, SourceField = "$AuthoritativeSource", TargetField = "AuthoritativeSource" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 1, SourceField = "$MaturityLevel", TargetField = "MaturityLevel" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 1, SourceField = "$BookOfRecord", TargetField = "BookOfRecord" },

            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 2, SourceField = "_id", TargetField = "SourceID", IncludeInPropertyRequest = false },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 2, SourceField = "_name", TargetField = "Name", IncludeInPropertyRequest = false },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 2, SourceField = "short_description", TargetField = "ShortDescription" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 2, SourceField = "long_description", TargetField = "LongDescription" },

            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 3, SourceField = "_id", TargetField = "SourceID", IncludeInPropertyRequest = false },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 3, SourceField = "_context", TargetField = "ParentSourceID", ParentContextPosition = 0 },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 3, SourceField = "_name", TargetField = "Name", IncludeInPropertyRequest = false },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 3, SourceField = "short_description", TargetField = "ShortDescription" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 3, SourceField = "long_description", TargetField = "LongDescription" },

            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 4, SourceField = "_id", TargetField = "SourceID", IncludeInPropertyRequest = false },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 4, SourceField = "_context", TargetField = "ParentSourceID", ParentContextPosition = 1 },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 4, SourceField = "_name", TargetField = "Name", IncludeInPropertyRequest = false },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 4, SourceField = "short_description", TargetField = "ShortDescription" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 4, SourceField = "long_description", TargetField = "LongDescription" },

            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 5, SourceField = "_id", TargetField = "SourceID", IncludeInPropertyRequest = false },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 5, SourceField = "_context", TargetField = "ParentSourceID", ParentContextPosition = 2 },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 5, SourceField = "_name", TargetField = "Name", IncludeInPropertyRequest = false },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 5, SourceField = "short_description", TargetField = "ShortDescription" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 5, SourceField = "long_description", TargetField = "LongDescription" },

            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 6, SourceField = "_id", TargetField = "SourceID", IncludeInPropertyRequest = false },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 6, SourceField = "_name", TargetField = "Name", IncludeInPropertyRequest = false },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 6, SourceField = "short_description", TargetField = "ShortDescription" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 6, SourceField = "long_description", TargetField = "LongDescription" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 6, SourceField = "$BusinessUnitId", TargetField = "BusinessUnitID" },

            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 7, SourceField = "_id", TargetField = "SourceID", IncludeInPropertyRequest = false },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 7, SourceField = "_context", TargetField = "ParentSourceID", ParentContextPosition = 0 },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 7, SourceField = "_name", TargetField = "Name", IncludeInPropertyRequest = false },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 7, SourceField = "short_description", TargetField = "ShortDescription" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 7, SourceField = "long_description", TargetField = "LongDescription" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 7, SourceField = "$BusinessUnitId", TargetField = "BusinessUnitID" },

            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 8, SourceField = "_id", TargetField = "SourceID", IncludeInPropertyRequest = false },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 8, SourceField = "_context", TargetField = "ParentSourceID", ParentContextPosition = 1 },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 8, SourceField = "_name", TargetField = "Name", IncludeInPropertyRequest = false },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 8, SourceField = "short_description", TargetField = "ShortDescription" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 8, SourceField = "long_description", TargetField = "LongDescription" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 8, SourceField = "$BusinessUnitId", TargetField = "BusinessUnitID" },

            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 9, SourceField = "_id", TargetField = "SourceID", IncludeInPropertyRequest = false },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 9, SourceField = "_context", TargetField = "ParentSourceID", ParentContextPosition = 2 },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 9, SourceField = "_name", TargetField = "Name", IncludeInPropertyRequest = false },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 9, SourceField = "short_description", TargetField = "ShortDescription" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 9, SourceField = "long_description", TargetField = "LongDescription" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 9, SourceField = "$BusinessUnitId", TargetField = "BusinessUnitID" },

            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 10, SourceField = "_id", TargetField = "SourceID", IncludeInPropertyRequest = false },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 10, SourceField = "_context", TargetField = "ParentSourceID", ParentContextPosition = 3 },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 10, SourceField = "_name", TargetField = "Name", IncludeInPropertyRequest = false },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 10, SourceField = "short_description", TargetField = "ShortDescription" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 10, SourceField = "long_description", TargetField = "LongDescription" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 10, SourceField = "$BusinessUnitId", TargetField = "BusinessUnitID" },

            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 11, SourceField = "_id", TargetField = "SourceID", IncludeInPropertyRequest = false },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 11, SourceField = "_context", TargetField = "ParentSourceID", ParentContextPosition = 4 },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 11, SourceField = "_name", TargetField = "Name", IncludeInPropertyRequest = false },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 11, SourceField = "short_description", TargetField = "ShortDescription" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 11, SourceField = "long_description", TargetField = "LongDescription" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 11, SourceField = "$BusinessUnitId", TargetField = "BusinessUnitID" },

            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 12, SourceField = "_id", TargetField = "SourceID", IncludeInPropertyRequest = false },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 12, SourceField = "_context", TargetField = "ParentSourceID", ParentContextPosition = 5 },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 12, SourceField = "_name", TargetField = "Name", IncludeInPropertyRequest = false },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 12, SourceField = "short_description", TargetField = "ShortDescription" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 12, SourceField = "long_description", TargetField = "LongDescription" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 12, SourceField = "$BusinessUnitId", TargetField = "BusinessUnitID" },

            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 13, SourceField = "_id", TargetField = "SourceID", IncludeInPropertyRequest = false },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 13, SourceField = "_name", TargetField = "Name", IncludeInPropertyRequest = false },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 13, SourceField = "short_description", TargetField = "ShortDescription" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 13, SourceField = "long_description", TargetField = "LongDescription" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 13, SourceField = "location", TargetField = "Location" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 13, SourceField = "network_node", TargetField = "NetworkNode" },

            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 14, SourceField = "_id", TargetField = "SourceID", IncludeInPropertyRequest = false },
            //new AssetTypeIntegrationFieldItem { MappingTypeId = 14, IgcField = "_context", GovernField = "ParentSourceID", ParentContextPosition = 0 },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 14, SourceField = "_name", TargetField = "Name", IncludeInPropertyRequest = false },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 14, SourceField = "short_description", TargetField = "ShortDescription" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 14, SourceField = "long_description", TargetField = "LongDescription" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 14, SourceField = "custom_Catalog Status", TargetField = "CatalogStatus" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 14, SourceField = "custom_Classification", TargetField = "Classification" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 14, SourceField = "custom_Comments", TargetField = "Comments" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 14, SourceField = "custom_Frequency", TargetField = "Frequency" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 14, SourceField = "custom_Information Classification", TargetField = "InformationClassification" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 14, SourceField = "custom_Output Format", TargetField = "OutputFormat" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 14, SourceField = "custom_Status", TargetField = "Status" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 14, SourceField = "alias_(business_name)", TargetField = "Alias" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 14, SourceField = "path", TargetField = "Path" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 14, SourceField = "store_type", TargetField = "StoreType" },

            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 15, SourceField = "_id", TargetField = "SourceID", IncludeInPropertyRequest = false },
            //new AssetTypeIntegrationFieldItem { MappingTypeId = 15, IgcField = "_context", GovernField = "ParentSourceID", ParentContextPosition = 1 },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 15, SourceField = "_name", TargetField = "Name", IncludeInPropertyRequest = false },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 15, SourceField = "short_description", TargetField = "ShortDescription" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 15, SourceField = "long_description", TargetField = "LongDescription" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 15, SourceField = "qualityScore", TargetField = "QualityScore" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 15, SourceField = "custom_Catalog Status", TargetField = "CatalogStatus" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 15, SourceField = "custom_CDE", TargetField = "CDE" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 15, SourceField = "custom_Data Element Definition", TargetField = "DataElementDefinition" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 15, SourceField = "custom_Data Origin Type", TargetField = "DataOriginType" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 15, SourceField = "custom_Information Classification", TargetField = "InformationClassification" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 15, SourceField = "custom_Privacy Treatment", TargetField = "PrivacyTreatment" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 15, SourceField = "custom_Trusted Source", TargetField = "TrustedSource" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 15, SourceField = "data_type", TargetField = "DataType" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 15, SourceField = "selected_classification", TargetField = "SelectedClassification" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 15, SourceField = "detected_classification", TargetField = "DetectedClassification" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 15, SourceField = "odbc_type", TargetField = "OdbcType" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 15, SourceField = "length", TargetField = "Length" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 15, SourceField = "minimum_length", TargetField = "MinimumLength" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 15, SourceField = "fraction", TargetField = "Fraction" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 15, SourceField = "position", TargetField = "Position" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 15, SourceField = "level", TargetField = "Level" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 15, SourceField = "allows_null_values", TargetField = "AllowNullValues" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 15, SourceField = "unique", TargetField = "Unique" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 15, SourceField = "same_as_data_sources", TargetField = "SameAsDataSources" },

            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 16, SourceField = "_id", TargetField = "SourceID", IncludeInPropertyRequest = false },
            //new AssetTypeIntegrationFieldItem { MappingTypeId = 16, IgcField = "_context", GovernField = "ParentSourceID", ParentContextPosition = 0 },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 16, SourceField = "_name", TargetField = "Name", IncludeInPropertyRequest = false },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 16, SourceField = "short_description", TargetField = "ShortDescription" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 16, SourceField = "long_description", TargetField = "LongDescription" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 16, SourceField = "custom_Catalog Status", TargetField = "CatalogStatus" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 16, SourceField = "alias_(business_name)", TargetField = "Alias" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 16, SourceField = "location", TargetField = "Location" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 16, SourceField = "dbms", TargetField = "Dbms" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 16, SourceField = "dms_server_instance", TargetField = "ServerInstance" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 16, SourceField = "dbms_vendor", TargetField = "Vendor" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 16, SourceField = "dbms_version", TargetField = "Version" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 16, SourceField = "database_type", TargetField = "DatabaseType" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 16, SourceField = "mapped_to_mdm_models", TargetField = "MappedToMdmModels" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 16, SourceField = "Notes", TargetField = "Notes" },


            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 17, SourceField = "_id", TargetField = "SourceID", IncludeInPropertyRequest = false },
            //new AssetTypeIntegrationFieldItem { MappingTypeId = 17, IgcField = "_context", GovernField = "ParentSourceID", ParentContextPosition = 1 },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 17, SourceField = "_name", TargetField = "Name", IncludeInPropertyRequest = false },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 17, SourceField = "short_description", TargetField = "ShortDescription" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 17, SourceField = "long_description", TargetField = "LongDescription" },

            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 18, SourceField = "_id", TargetField = "SourceID", IncludeInPropertyRequest = false },
            //new AssetTypeIntegrationFieldItem { MappingTypeId = 18, IgcField = "_context", GovernField = "ParentSourceID", ParentContextPosition = 2 },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 18, SourceField = "_name", TargetField = "Name", IncludeInPropertyRequest = false },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 18, SourceField = "short_description", TargetField = "ShortDescription" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 18, SourceField = "long_description", TargetField = "LongDescription" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 18, SourceField = "qualityScore", TargetField = "QualityScore" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 18, SourceField = "custom_Catalog Status", TargetField = "CatalogStatus" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 18, SourceField = "custom_Data Element Location", TargetField = "DataElementLocation" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 18, SourceField = "alias_(business_name)", TargetField = "Alias" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 18, SourceField = "reviewDate", TargetField = "ReviewDate" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 18, SourceField = "Notes", TargetField = "Notes" },

            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 19, SourceField = "_id", TargetField = "SourceID", IncludeInPropertyRequest = false },
            //new AssetTypeIntegrationFieldItem { MappingTypeId = 19, IgcField = "_context", GovernField = "ParentSourceID", ParentContextPosition = 2 },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 19, SourceField = "_name", TargetField = "Name", IncludeInPropertyRequest = false },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 19, SourceField = "short_description", TargetField = "ShortDescription" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 19, SourceField = "long_description", TargetField = "LongDescription" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 19, SourceField = "qualityScore", TargetField = "QualityScore" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 19, SourceField = "custom_Catalog Status", TargetField = "CatalogStatus" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 19, SourceField = "custom_Data Element Location", TargetField = "DataElementLocation" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 19, SourceField = "alias_(business_name)", TargetField = "Alias" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 19, SourceField = "reviewDate", TargetField = "ReviewDate" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 19, SourceField = "fieldCount", TargetField = "FieldCount" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 19, SourceField = "notes", TargetField = "Notes" },

            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 20, SourceField = "_id", TargetField = "SourceID", IncludeInPropertyRequest = false },
            //new AssetTypeIntegrationFieldItem { MappingTypeId = 20, IgcField = "_context", GovernField = "ParentSourceID", ParentContextPosition = 2 },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 20, SourceField = "_name", TargetField = "Name", IncludeInPropertyRequest = false },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 20, SourceField = "short_description", TargetField = "ShortDescription" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 20, SourceField = "long_description", TargetField = "LongDescription" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 20, SourceField = "qualityScore", TargetField = "QualityScore" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 20, SourceField = "custom_Catalog Status", TargetField = "CatalogStatus" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 20, SourceField = "custom_CDE", TargetField = "CDE" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 20, SourceField = "custom_Data Element Definition", TargetField = "DataElementDefinition" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 20, SourceField = "alias_(business_name)", TargetField = "Alias" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 20, SourceField = "reviewDate", TargetField = "ReviewDate" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 20, SourceField = "fieldCount", TargetField = "FieldCount" },
            new IntegrationAssetTypeFieldItem { SynchedAssetTypeID = 20, SourceField = "notes", TargetField = "Notes" },

/*
custom_Data Origin Type
custom_Data Steward
custom_Data Steward Id
custom_Information Classification
custom_Privacy Treatment
custom_Trusted Source
type
odbc_type
data_type
database_domains
referenced_by_database_columns
selected_classification
detected_classifications
length
minimum_length
fraction
position
level
occurs
start_end_columns
allows_null_values
unique
same_as_data_sources
references_database_columns
defined_primary_key
selected_primary_key
selected_natural_key
defined_foreign_key
defined_foreign_key_references
defined_foreign_key_referenced
selected_foreign_key
selected_foreign_key_references
selected_foreign_key_referenced
database_indexes
validity_tables
uniqueFlag
nullabilityFlag
constantFlag
domainType
numberCompleteValues
numberValidValues
numberEmptyValues
numberNullValues
numberDistinctValues
numberFormats
numberZeroValues
inferredDataType
inferredLength
inferredFormat
inferredScale
inferredPrecision
averageValue
isInferredForeignKey
isInferredPrimaryKey
nbRecordsTested
column_definitions
mapped_to_physical_object_attributes
impacts_on
notes

*/
        };

/*
new MappingType { Id = 20, IgcType = "data_element", GovernType = "FusionAttributeType", GovernTypeID = 2 }         
*/

        static List<IntegrationAssetTypeRelationItem> InputMappingRelationItems = new List<IntegrationAssetTypeRelationItem> {
            //new MappingRelationItem { MappingTypeId = 1, IgcField = "assigned_to_terms", GovernField = "" },
            //new MappingRelationItem { MappingTypeId = 1, IgcField = "implements_rules", GovernField = "" },
            //new MappingRelationItem { MappingTypeId = 1, IgcField = "governed_by_rules", GovernField = "" },
            new IntegrationAssetTypeRelationItem { SynchedAssetTypeID = 1, SourceField = "impacts_on", PredicateType = 7 },

            new IntegrationAssetTypeRelationItem { SynchedAssetTypeID = 15, SourceField = "impacts_on", PredicateType = 7 },
            
            new IntegrationAssetTypeRelationItem { SynchedAssetTypeID = 16, SourceField = "impacts_on", PredicateType = 7 },

            new IntegrationAssetTypeRelationItem { SynchedAssetTypeID = 17, SourceField = "impacts_on", PredicateType = 7 },

            new IntegrationAssetTypeRelationItem { SynchedAssetTypeID = 18, SourceField = "referenced_by_views", IsSubject = true, PredicateType = 1 },
            new IntegrationAssetTypeRelationItem { SynchedAssetTypeID = 18, SourceField = "impacts_on", PredicateType = 7 },
            new IntegrationAssetTypeRelationItem { SynchedAssetTypeID = 18, SourceField = "governed_by_rules", PredicateType = 1 },

            new IntegrationAssetTypeRelationItem { SynchedAssetTypeID = 19, SourceField = "referenced_by_views", IsSubject = true, PredicateType = 1 },
            new IntegrationAssetTypeRelationItem { SynchedAssetTypeID = 19, SourceField = "impacts_on", PredicateType = 7 },
            new IntegrationAssetTypeRelationItem { SynchedAssetTypeID = 19, SourceField = "governed_by_rules", PredicateType = 1 }

        };

        static List<IntegrationAssetTypeRoleItem> InputMappingRoleItems = new List<IntegrationAssetTypeRoleItem> {
            new IntegrationAssetTypeRoleItem { SynchedAssetTypeID = 1, RoleName = "Business Owner", SourceIdField = "$BusinessOwnerId", SourceNameField = "$BusinessOwner" },
            new IntegrationAssetTypeRoleItem { SynchedAssetTypeID = 1, RoleName = "Application Owner", SourceIdField = "$ApplicationOwnerId", SourceNameField = "$ApplicationOwner" },
            new IntegrationAssetTypeRoleItem { SynchedAssetTypeID = 1, RoleName = "Data Steward", SourceIdField = "$DataStewardId", SourceNameField = "$DataSteward" },
            //new MappingRoleItem { MappingTypeId = 1, GovernRoleName = "", IgcNameField = "$DataOwner" },
            new IntegrationAssetTypeRoleItem { SynchedAssetTypeID = 1, RoleName = "EDGM Steward", SourceIdField = "$EDGMStewardId" },

            new IntegrationAssetTypeRoleItem { SynchedAssetTypeID = 6, RoleName = "Business Owner", SourceIdField = "$BusinessOwnerId", SourceNameField = "$BusinessOwner" },
            new IntegrationAssetTypeRoleItem { SynchedAssetTypeID = 7, RoleName = "Business Owner", SourceIdField = "$BusinessOwnerId", SourceNameField = "$BusinessOwner" },
            new IntegrationAssetTypeRoleItem { SynchedAssetTypeID = 8, RoleName = "Business Owner", SourceIdField = "$BusinessOwnerId", SourceNameField = "$BusinessOwner" },
            new IntegrationAssetTypeRoleItem { SynchedAssetTypeID = 9, RoleName = "Business Owner", SourceIdField = "$BusinessOwnerId", SourceNameField = "$BusinessOwner" },
            new IntegrationAssetTypeRoleItem { SynchedAssetTypeID = 10, RoleName = "Business Owner", SourceIdField = "$BusinessOwnerId", SourceNameField = "$BusinessOwner" },
            new IntegrationAssetTypeRoleItem { SynchedAssetTypeID = 11, RoleName = "Business Owner", SourceIdField = "$BusinessOwnerId", SourceNameField = "$BusinessOwner" },
            new IntegrationAssetTypeRoleItem { SynchedAssetTypeID = 12, RoleName = "Business Owner", SourceIdField = "$BusinessOwnerId", SourceNameField = "$BusinessOwner" },

            new IntegrationAssetTypeRoleItem { SynchedAssetTypeID = 14, RoleName = "Data Steward", SourceIdField = "$DataStewardId", SourceNameField = "$DataSteward" },
            new IntegrationAssetTypeRoleItem { SynchedAssetTypeID = 14, RoleName = "Owner", SourceIdField = "custom_Owner Id", SourceNameField = "custom_Owner" },

            new IntegrationAssetTypeRoleItem { SynchedAssetTypeID = 14, RoleName = "Data Steward", SourceIdField = "custom_Data Steward Id", SourceNameField = "custom_Data Steward" },

            new IntegrationAssetTypeRoleItem { SynchedAssetTypeID = 16, RoleName = "Data Steward", SourceIdField = "custom_Data Steward Id", SourceNameField = "custom_Data Steward" },
            new IntegrationAssetTypeRoleItem { SynchedAssetTypeID = 16, RoleName = "Owner", SourceIdField = "custom_Owner Id", SourceNameField = "custom_Owner" },

            new IntegrationAssetTypeRoleItem { SynchedAssetTypeID = 17, RoleName = "Business Owner", SourceIdField = "custom_Business Owner Id", SourceNameField = "custom_Business Owner" },
            new IntegrationAssetTypeRoleItem { SynchedAssetTypeID = 17, RoleName = "Data Steward", SourceIdField = "custom_Data Steward Id", SourceNameField = "custom_Data Steward" },

            new IntegrationAssetTypeRoleItem { SynchedAssetTypeID = 18, RoleName = "Data Steward", SourceIdField = "custom_Data Steward Id", SourceNameField = "custom_Data Steward" },
            new IntegrationAssetTypeRoleItem { SynchedAssetTypeID = 18, RoleName = "Owner", SourceIdField = "custom_Owner Id", SourceNameField = "custom_Owner" },

        };

        #endregion

        #endregion

        public static void Run([TimerTrigger(timerSettings)]TimerInfo myTimer, TextWriter log) //   
        {
            try
            {
                CoreFunction.AITrackJobStart(functionName);

                foreach (var item in InputMappingTypes.Where(i => i.Active && i.ToGovern))
                {
                    LoadAssetsByMappingType(item);
                }

                //var companies = CoreFunction.GetCompaniesByCurrentSlot();
                //companies.ForEach(c =>
                //{
                //  try
                //  {
                //      var company = CompanyConnectionUtils.GetCompanyConnection(c.CompanyID, c.Server, c.Username, c.Password);
                //      company.OpenWithRetry(RetryPolicy.DefaultFixed);
                //  }
                //  catch (Exception ex)
                //  {
                //      CoreFunction.AITrackException(functionName, ex, c.CompanyID);
                //      //log.Error($"Company [{c.CompanyID}]: [{ex.GetFullExceptionData()}]");
                //  }
                //});

                CoreFunction.AITrackJobCompletedNoErrors(functionName);
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex);
                //log.Error($"General Exception: {ex.GetFullExceptionData()}");
            }

            CoreFunction.AIFlush();
        }


        #region Generic

        internal static T GetFromApi<T>(string uri, string authorization)
        {
            var cleanUri = new Uri(uri);
            if (cleanUri.Port != 80 && cleanUri.Port != 443)
            {
                uri = uri.Replace($":{cleanUri.Port}", "");
            }

            var req = HttpWebRequest.CreateHttp(uri);
            req.Accept = "application/json";
            req.Headers.Set(HttpRequestHeader.Authorization, authorization);
            req.ServerCertificateValidationCallback = delegate { return true; };

            var jsonRaw = "";

            var response = req.GetResponse();
            using (var responseStream = response.GetResponseStream())
            {
                using (var rdr = new StreamReader(responseStream))
                {
                    jsonRaw = rdr.ReadToEnd();
                }
            }

            return JsonConvert.DeserializeObject<T>(jsonRaw, new JsonSerializerSettings { MetadataPropertyHandling = MetadataPropertyHandling.Ignore });
        }

        static string PostJsonToApi(string uri, string authorization, string requestBody)
        {
            var jsonToReturn = "";

            using (var client = new WebClient())
            {
                client.Headers.Set(HttpRequestHeader.Accept, "application/json");
                client.Headers.Set(HttpRequestHeader.ContentType, "application/json");
                client.Headers.Set(HttpRequestHeader.Authorization, authorization);
                jsonToReturn = client.UploadString(uri, requestBody);
            }

            return jsonToReturn;
        }

        static string buildSearchUri(string type, List<string> properties)
        {
            var url = $"{SourceUri}search/?pageSize=75&types={type}";
            foreach (var p in properties)
            {
                url += $"&properties={p}";
            }
            return url;
        }

        #endregion

        #region NEW WAY - GENERIC

        public static void LoadAssetsByMappingType(IntegrationAssetType mappingType)
        {
            var fieldMappings = InputMappingFieldItems.Where(i => i.SynchedAssetTypeID == mappingType.ID).ToList();
            var relationMappings = InputMappingRelationItems.Where(i => i.SynchedAssetTypeID == mappingType.ID).ToList();
            var roleMappings = InputMappingRoleItems.Where(i => i.SynchedAssetTypeID == mappingType.ID).ToList();
            var url = $"{SourceUri}search/?pageSize=75&types={mappingType.SourceAssetTypeName}";

            // Add the properties we are after for this IGC type.
            url += string.Concat(fieldMappings.Where(i => i.IncludeInPropertyRequest).Select(i => $"&properties={i.SourceField}"));
            url += string.Concat(relationMappings.Where(i => i.IncludeInPropertyRequest).Select(i => $"&properties={i.SourceField}"));
            url += string.Concat(roleMappings.Where(i => i.IncludeInPropertyRequest).Where(i => !string.IsNullOrEmpty(i.SourceIdField)).Select(i => $"&properties={i.SourceIdField}"));
            url += string.Concat(roleMappings.Where(i => i.IncludeInPropertyRequest).Where(i => !string.IsNullOrEmpty(i.SourceNameField)).Select(i => $"&properties={i.SourceNameField}"));


            var igcData = new IgcDynamicArrayModels();
            var arr = new JArray();
            var relationships = new List<D3sRelationshipModel>();
            var ownershipTopModel = new D3sOwnershipItemsModel { UserIdFieldName = "UserID", Items = new List<D3sOwnershipModel>() };

            Func<JArray, JArray> parse = delegate (JArray root)
            {
                foreach (var obj in root.Children())
                {
                    var igcObjectSourceID = obj["_id"].Value<string>();

                    // Field Load Logic.
                    var d3s = new JObject();
                    fieldMappings.ForEach(f =>
                    {
                        if (f.ParentContextPosition.HasValue)
                        {
                            // There is a hierarchy here, and we need to resolve it.
                            var context = obj[f.SourceField].Cast<List<GenericIgcContextModel>>().FirstOrDefault();
                            if (context != null)
                            {
                                d3s.Add(f.TargetField, context[f.ParentContextPosition.Value]._id);
                            }
                        }
                        else
                        {
                            if (f.IsArray)
                            {
                                d3s.Add(f.TargetField, (obj[f.SourceField] != null) ? string.Join(", ", obj[f.SourceField]) : "");
                            }
                            else
                            {
                                d3s.Add(f.TargetField, obj[f.SourceField].Value<string>());
                            }
                        }

                    });
                    arr.Add(d3s);

                    // Relation Load Logic.
                    relationMappings.ForEach(r =>
                    {
                        try
                        {
                            var rm = obj[r.SourceField].ToObject<IgcRelationshipModel>();
                            var items = (
                                        from i in rm.items
                                        select i
                                        ).ToList();

                            relationships.AddRange(
                                items.Select(i => new D3sRelationshipModel
                                {
                                    SubjectSourceID = r.IsSubject ? igcObjectSourceID : i.SourceID,
                                    ObjectSourceID = r.IsSubject ? i.SourceID : igcObjectSourceID,
                                    PredicateType = r.PredicateType
                                })
                            );
                        }
                        catch (Exception)
                        {
                        }
                    });

                    // Role Load Logic.
                    roleMappings.ForEach(r => {
                        var userFullName = "";
                        var userId = "";
                        if (!string.IsNullOrEmpty(r.SourceNameField))
                        {
                            if (obj[r.SourceNameField] != null)
                            {
                                userFullName = obj[r.SourceNameField].Value<string>();
                            }
                        }
                        if (!string.IsNullOrEmpty(r.SourceIdField))
                        {
                            if (obj[r.SourceIdField] != null)
                            {
                                userId = obj[r.SourceIdField].Value<string>();
                            }
                        }
                        ownershipTopModel.Items.Add(new D3sOwnershipModel {
                            RoleName = r.RoleName,
                            SourceID = igcObjectSourceID,
                            UserFullName = userFullName,
                            UserId = userId
                        });
                    });
                }

                return root;
            };

            while (!string.IsNullOrEmpty(url))
            {
                try
                {
                    var models = GetFromApi<IgcDynamicArrayModels>(url, SourceAuthString);
                    if (models != null)
                    {
                        parse(models.items);
                        url = models.paging.next;
                    }
                }
                catch (Exception ex)
                {
                    CoreFunction.AITrackException(functionName, ex);
                    url = null;
                }
            }

            // If any items to send to server.
            if (arr.Count > 0)
            {
                try
                {
                    var respString = PostJsonToApi(
                        $"{TargetUri}{mappingType.Object}/{mappingType.ObjectID}/bulk",
                        TargetAuthString,
                        JsonConvert.SerializeObject(arr)
                    );
                }
                catch (Exception ex)
                {
                    CoreFunction.AITrackException(functionName, ex);
                }
            }

            // If any owners to send to server.
            if (ownershipTopModel.Items.Count > 0)
            {
                var uniqueUsers = ownershipTopModel.Items
                    .Where(i => !string.IsNullOrEmpty(i.UserFullName) && !string.IsNullOrEmpty(i.UserId))
                    .Select(i => new { i.UserFullName, i.UserId })
                    .Distinct()
                    .ToList();

                // Populate the UserIDs that are missing, based can be looked up by user's full name. TODO: Confirm this logic, as it may not be correct if two or more user's have the same name.
                foreach (var item in ownershipTopModel.Items.Where(i => string.IsNullOrEmpty(i.UserId)))
                {
                    var match = uniqueUsers.FirstOrDefault(i => i.UserFullName == item.UserFullName);
                    if (match != null)
                    {
                        item.UserId = match.UserId;
                    }
                }

                //Now, remove any users whose internal ID cannot be resolved.
                ownershipTopModel.Items.RemoveAll(i => string.IsNullOrEmpty(i.UserId));

                try
                {
                    if (ownershipTopModel.Items.Count > 0)
                    {
                        var respString = PostJsonToApi(
                            $"{TargetUri}ownership/bulk",
                            TargetAuthString,
                            JsonConvert.SerializeObject(ownershipTopModel)
                        );
                    }
                }
                catch (Exception ex)
                {
                    CoreFunction.AITrackException(functionName, ex);
                }
            }

            if (relationships.Count > 0)
            {
                try
                {
                    var respString = PostJsonToApi(
                        $"{TargetUri}relationships/bulk",
                        TargetAuthString,
                        JsonConvert.SerializeObject(relationships)
                    );
                }
                catch (Exception ex)
                {
                    CoreFunction.AITrackException(functionName, ex);
                }
            }
        }

        #endregion








        public static void GetTypes()
        {
            var url = $"{SourceUri}types";

            var models = GetFromApi<dynamic>(url, SourceAuthString);
            //if (models != null)
            //{
            //    url = models.paging.next;
            //}

        }


        #region Application

        //public static void LoadApplicationCatalog()
        //{
        //    var properties = "short_description,long_description,labels,stewards,assigned_to_terms,implements_rules,governed_by_rules,$CMDBAppCode,$ApplicationAlias,$BusinessOwner,$BusinessOwnerId,$ApplicationOwner,$ApplicationOwnerId,$DataSteward,$DataStewardId,$DataOwner,$EDGMStewardId,$Comments,$SSID,$KeyApplicationType,$Status,$DataLocation,$PersonalData,$ComponentType,$ComponentCode,$ComponentSAID,$AuthoritativeSource,$MaturityLevel,$BookOfRecord,impacts_on".Split(',');
        //    var url = $"{SourceUri}search/?pageSize=75&types=$ApplicationCatalog-ApplicationCatalog";
        //    foreach (var p in properties)
        //    {
        //        url += $"&properties={p}";
        //    }

        //    var arr = new List<D3sApplicationCatalogModel>();
        //    var d3sImpactRelationships = new List<D3sRelationshipModel>();
        //    var ownershipTopModel = new D3sOwnershipItemsModel { UserIdFieldName = "UserID", Items = new List<D3sOwnershipModel>() };

        //    Func<IgcApplicationCatalogModels, IgcApplicationCatalogModels> parse = delegate (IgcApplicationCatalogModels root)
        //    {
        //        arr.AddRange(root.items.ConvertAll<D3sApplicationCatalogModel>(i => new D3sApplicationCatalogModel
        //        {
        //            SourceID = i.SourceID,
        //            Name = i.Name,
        //            ShortDescription = i.ShortDescription,
        //            ApplicationAlias = i.ApplicationAlias,
        //            AuthoritativeSource = i.AuthoritativeSource,
        //            Host = i.BookOfRecord,
        //            CMDBAppCode = i.CMDBAppCode,
        //            Comments = i.Comments,
        //            KeyApplicationTypeText = (i.KeyApplicationType != null) ? string.Join(", ", i.KeyApplicationType) : "",
        //            ComponentSAID = i.ComponentSAID,
        //            ComponentType = i.ComponentType,
        //            DataLocation = i.DataLocation,
        //            LongDescription = i.LongDescription,
        //            MaturityLevel = i.MaturityLevel,
        //            PersonalData = i.PersonalData ?? "No",
        //            SSID = i.SSID,
        //            Status = i.Status
        //        }));

        //        ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
        //        {
        //            SourceID = i.SourceID,
        //            RoleName = "Business Owner",
        //            UserId = i.BusinessOwnerId,
        //            UserFullName = i.BusinessOwner
        //        }));

        //        ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
        //        {
        //            SourceID = i.SourceID,
        //            RoleName = "Application Owner",
        //            UserId = i.ApplicationOwnerId,
        //            UserFullName = i.ApplicationOwner
        //        }));

        //        ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
        //        {
        //            SourceID = i.SourceID,
        //            RoleName = "Data Owner",
        //            UserId = string.Empty,
        //            UserFullName = i.DataOwner
        //        }));

        //        ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
        //        {
        //            SourceID = i.SourceID,
        //            RoleName = "Data Steward",
        //            UserId = i.DataStewardId,
        //            UserFullName = i.DataSteward
        //        }));

        //        ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
        //        {
        //            SourceID = i.SourceID,
        //            RoleName = "EDGM Steward",
        //            UserId = i.EDGMStewardId,
        //            UserFullName = string.Empty
        //        }));

        //        foreach (var app in root.items)
        //        {
        //            app.ImpactsOn.items.ForEach(bu =>
        //            {
        //                d3sImpactRelationships.Add(
        //                    new D3sRelationshipModel { SubjectSourceID = bu.SourceID, ObjectSourceID = app.SourceID, PredicateType = 7 }
        //                );
        //            });
        //        }

        //        return root;
        //    };

        //    while (!string.IsNullOrEmpty(url))
        //    {
        //        try
        //        {
        //            var models = GetFromApi<IgcApplicationCatalogModels>(url, SourceAuthString);
        //            if (models != null)
        //            {
        //                parse(models);
        //                url = models.paging.next;
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            CoreFunction.AITrackException(functionName, ex);
        //            url = null;
        //        }
        //    }

        //    // If any items to send to server.
        //    if (arr.Count > 0)
        //    {
        //        try
        //        {
        //            var respString = PostJsonToApi(
        //                $"{TargetUri}ArtifactType/2/bulk",
        //                TargetAuthString,
        //                JsonConvert.SerializeObject(arr)
        //            );
        //        }
        //        catch (Exception ex)
        //        {
        //            CoreFunction.AITrackException(functionName, ex);
        //        }
        //    }

        //    // If any owners to send to server.
        //    if (ownershipTopModel.Items.Count > 0)
        //    {
        //        var uniqueUsers = ownershipTopModel.Items
        //            .Where(i => !string.IsNullOrEmpty(i.UserFullName) && !string.IsNullOrEmpty(i.UserId))
        //            .Select(i => new { i.UserFullName, i.UserId })
        //            .Distinct()
        //            .ToList();

        //        // Populate the UserIDs that are missing, based can be looked up by user's full name. TODO: Confirm this logic, as it may not be correct if two or more user's have the same name.
        //        foreach (var item in ownershipTopModel.Items.Where(i => string.IsNullOrEmpty(i.UserId)))
        //        {
        //            var match = uniqueUsers.FirstOrDefault(i => i.UserFullName == item.UserFullName);
        //            if (match != null)
        //            {
        //                item.UserId = match.UserId;
        //            }
        //        }

        //        //Now, remove any users whose internal ID cannot be resolved.
        //        ownershipTopModel.Items.RemoveAll(i => string.IsNullOrEmpty(i.UserId));

        //        try
        //        {
        //            var respString = PostJsonToApi(
        //                $"{TargetUri}ownership/bulk",
        //                TargetAuthString,
        //                JsonConvert.SerializeObject(ownershipTopModel)
        //            );
        //        }
        //        catch (Exception ex)
        //        {
        //            CoreFunction.AITrackException(functionName, ex);
        //        }
        //    }

        //    if (d3sImpactRelationships.Count > 0)
        //    {
        //        try
        //        {
        //            var respString = PostJsonToApi(
        //                $"{TargetUri}relationships/bulk",
        //                TargetAuthString,
        //                JsonConvert.SerializeObject(d3sImpactRelationships)
        //            );
        //        }
        //        catch (Exception ex)
        //        {
        //            CoreFunction.AITrackException(functionName, ex);
        //        }
        //    }
        //}

        #endregion

        #region Fusion

        ////public static void GetHosts()
        ////{
        ////    var url = buildSearchUri("host", new List<string> {
        ////        "short_description",
        ////        "long_description",
        ////        "labels",
        ////        "stewards",
        ////        //"assigned_to_terms",
        ////        //"implements_rules",
        ////        //"governed_by_rules",
        ////        //"databases",
        ////        //"data_files",
        ////        //"idoc_types",
        ////        //"transformation_projects"
        ////        //"data_connections",
        ////        //"amazon_s3_buckets",
        ////        //"data_file_folders",
        ////        "location",
        ////        "network_node",
        ////        //"imported_from",
        ////        //"in_colleections",
        ////        "notes"
        ////    });

        ////    var arr = new List<dynamic>();
        ////    //var d3sImpactRelationships = new List<D3sBusinesUnitApplicationCatalogRelationshipModel>();
        ////    //var ownershipTopModel = new D3sOwnershipItemsModel { UserIdFieldName = "UserID", Items = new List<D3sOwnershipModel>() };

        ////    Func<IgcDynamicModels, IgcDynamicModels> parse = delegate (IgcDynamicModels root)
        ////    {
        ////        arr.AddRange(root.items.ConvertAll<dynamic>(i => new
        ////        {
        ////            SourceID = i._id,
        ////            Name = i._name,
        ////            ShortDescription = i.short_description,
        ////            LongDescription = i.long_description,
        ////            Location = i.location,
        ////            NetworkNode = i.network_node,
        ////            Notes = i.notes
        ////        }));

        ////        //ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
        ////        //{
        ////        //    SourceID = i.SourceID,
        ////        //    RoleName = "Business Owner",
        ////        //    UserId = i.BusinessOwnerId,
        ////        //    UserFullName = i.BusinessOwner
        ////        //}));

        ////        //ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
        ////        //{
        ////        //    SourceID = i.SourceID,
        ////        //    RoleName = "Application Owner",
        ////        //    UserId = i.ApplicationOwnerId,
        ////        //    UserFullName = i.ApplicationOwner
        ////        //}));

        ////        //ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
        ////        //{
        ////        //    SourceID = i.SourceID,
        ////        //    RoleName = "Data Owner",
        ////        //    UserId = string.Empty,
        ////        //    UserFullName = i.DataOwner
        ////        //}));

        ////        //ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
        ////        //{
        ////        //    SourceID = i.SourceID,
        ////        //    RoleName = "Data Steward",
        ////        //    UserId = i.DataStewardId,
        ////        //    UserFullName = i.DataSteward
        ////        //}));

        ////        //ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
        ////        //{
        ////        //    SourceID = i.SourceID,
        ////        //    RoleName = "EDGM Steward",
        ////        //    UserId = i.EDGMStewardId,
        ////        //    UserFullName = string.Empty
        ////        //}));

        ////        //foreach (var app in root.items)
        ////        //{
        ////        //    app.ImpactsOn.items.ForEach(bu =>
        ////        //    {
        ////        //        d3sImpactRelationships.Add(
        ////        //            new D3sBusinesUnitApplicationCatalogRelationshipModel { SubjectSourceID = bu.SourceID, ObjectSourceID = app.SourceID, PredicateType = 7 }
        ////        //        );
        ////        //    });
        ////        //}

        ////        return root;
        ////    };

        ////    while (!string.IsNullOrEmpty(url))
        ////    {
        ////        try
        ////        {
        ////            var models = GetFromApi<IgcDynamicModels>(url, SourceAuthString);
        ////            if (models != null)
        ////            {
        ////                parse(models);
        ////                url = models.paging.next;
        ////            }
        ////        }
        ////        catch (Exception ex)
        ////        {
        ////            CoreFunction.AITrackException(functionName, ex);
        ////            url = null;
        ////        }
        ////    }

        ////    // If any items to send to server.
        ////    if (arr.Count > 0)
        ////    {
        ////        //var respString = PostJsonToApi(
        ////        //    $"{TargetUri}ArtifactType/2/bulk",
        ////        //    TargetAuthString,
        ////        //    JsonConvert.SerializeObject(arr)
        ////        //);
        ////    }

        ////    //// If any owners to send to server.
        ////    //if (ownershipTopModel.Items.Count > 0)
        ////    //{
        ////    //    var uniqueUsers = ownershipTopModel.Items
        ////    //        .Where(i => !string.IsNullOrEmpty(i.UserFullName) && !string.IsNullOrEmpty(i.UserId))
        ////    //        .Select(i => new { i.UserFullName, i.UserId })
        ////    //        .Distinct()
        ////    //        .ToList();

        ////    //    // Populate the UserIDs that are missing, based can be looked up by user's full name. TODO: Confirm this logic, as it may not be correct if two or more user's have the same name.
        ////    //    foreach (var item in ownershipTopModel.Items.Where(i => string.IsNullOrEmpty(i.UserId)))
        ////    //    {
        ////    //        var match = uniqueUsers.FirstOrDefault(i => i.UserFullName == item.UserFullName);
        ////    //        if (match != null)
        ////    //        {
        ////    //            item.UserId = match.UserId;
        ////    //        }
        ////    //    }

        ////    //    //Now, remove any users whose internal ID cannot be resolved.
        ////    //    ownershipTopModel.Items.RemoveAll(i => string.IsNullOrEmpty(i.UserId));

        ////    //    var respString = PostJsonToApi(
        ////    //        $"{TargetUri}ownership/bulk",
        ////    //        TargetAuthString,
        ////    //        JsonConvert.SerializeObject(ownershipTopModel)
        ////    //    );
        ////    //}

        ////    //if (d3sImpactRelationships.Count > 0)
        ////    //{
        ////    //    //var respString = PostJsonToApi(
        ////    //    //    $"{TargetUri}relationships/bulk",
        ////    //    //    TargetAuthString,
        ////    //    //    JsonConvert.SerializeObject(d3sImpactRelationships)
        ////    //    //);
        ////    //}
        ////}


        ////public static void GetDataFiles()
        ////{
        ////    var url = buildSearchUri("data_file", new List<string> {
        ////            "short_description",
        ////            "long_description",
        ////            "parent_folder",
        ////            "host",
        ////            "labels",
        ////            "stewards",
        ////            //"assigned_to_terms",
        ////            //"implements_rules",
        ////            "governed_by_rules",
        ////            "data_file_records",
        ////            //"implements_data_file_definition",
        ////            //"implements_physical_models",
        ////            "custom_Catalog Status",
        ////            "custom_Classification",
        ////            //"custom_Comments",
        ////            //"custom_Created By",
        ////            //"custom_Data Steward",
        ////            //"custom_Data Steward Id",
        ////            //"custom_Frequency",
        ////            "custom_Classification",//"custom_Information Classification",
        ////            //"custom_Modified By",
        ////            "custom_Output Format",
        ////            //"custom_Owner",
        ////            //"custom_Owner Id",
        ////            "custom_Status",
        ////            "alias_(business_name)",
        ////            "path",
        ////            //"store_type",
        ////            "imported_from",
        ////            //"impacted_by",
        ////            //"impacts_on",
        ////            "include_for_business_lineage",
        ////            "suggested_term_assignments",
        ////            "notes",
        ////            "amazon_s3_data_files",
        ////            "implements_data_file_definition",
        ////            "implements_physical_models"
        ////        });

        ////    var arr = new List<dynamic>();
        ////    //var d3sImpactRelationships = new List<D3sBusinesUnitApplicationCatalogRelationshipModel>();
        ////    var ownershipTopModel = new D3sOwnershipItemsModel { UserIdFieldName = "UserID", Items = new List<D3sOwnershipModel>() };

        ////    Func<IgcDataFileModels, IgcDataFileModels> parse = delegate (IgcDataFileModels root)
        ////    {
        ////        arr.AddRange(root.items.ConvertAll<dynamic>(i => new
        ////        {
        ////            SourceID = i.SourceID,
        ////            Name = i.Name,
        ////            ShortDescription = i.ShortDescription,
        ////            LongDescription = i.LongDescription,
        ////            Classification = i.Classification,
        ////            Location = i.Location,
        ////            Notes = i.Notes
        ////        }));

        ////        //ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
        ////        //{
        ////        //    SourceID = i.SourceID,
        ////        //    RoleName = "Business Owner",
        ////        //    UserId = i.BusinessOwnerId,
        ////        //    UserFullName = i.BusinessOwner
        ////        //}));

        ////        //ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
        ////        //{
        ////        //    SourceID = i.SourceID,
        ////        //    RoleName = "Application Owner",
        ////        //    UserId = i.ApplicationOwnerId,
        ////        //    UserFullName = i.ApplicationOwner
        ////        //}));

        ////        //ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
        ////        //{
        ////        //    SourceID = i.SourceID,
        ////        //    RoleName = "Data Owner",
        ////        //    UserId = string.Empty,
        ////        //    UserFullName = i.DataOwner
        ////        //}));

        ////        ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
        ////        {
        ////            SourceID = i.SourceID,
        ////            RoleName = "Data Steward",
        ////            UserId = i.DataStewardId,
        ////            UserFullName = i.DataSteward
        ////        }));

        ////        //ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
        ////        //{
        ////        //    SourceID = i.SourceID,
        ////        //    RoleName = "EDGM Steward",
        ////        //    UserId = i.EDGMStewardId,
        ////        //    UserFullName = string.Empty
        ////        //}));

        ////        //foreach (var app in root.items)
        ////        //{
        ////        //    app.ImpactsOn.items.ForEach(bu =>
        ////        //    {
        ////        //        d3sImpactRelationships.Add(
        ////        //            new D3sBusinesUnitApplicationCatalogRelationshipModel { SubjectSourceID = bu.SourceID, ObjectSourceID = app.SourceID, PredicateType = 7 }
        ////        //        );
        ////        //    });
        ////        //}

        ////        return root;
        ////    };

        ////    while (!string.IsNullOrEmpty(url))
        ////    {
        ////        try
        ////        {
        ////            var models = GetFromApi<IgcDataFileModels>(url, SourceAuthString);
        ////            if (models != null)
        ////            {
        ////                parse(models);
        ////                url = models.paging.next;
        ////            }
        ////        }
        ////        catch (Exception ex)
        ////        {
        ////            CoreFunction.AITrackException(functionName, ex);
        ////            url = null;
        ////        }
        ////    }

        ////    // If any items to send to server.
        ////    if (arr.Count > 0)
        ////    {
        ////        //var respString = PostJsonToApi(
        ////        //    $"{TargetUri}ArtifactType/2/bulk",
        ////        //    TargetAuthString,
        ////        //    JsonConvert.SerializeObject(arr)
        ////        //);
        ////    }

        ////    //// If any owners to send to server.
        ////    //if (ownershipTopModel.Items.Count > 0)
        ////    //{
        ////    //    var uniqueUsers = ownershipTopModel.Items
        ////    //        .Where(i => !string.IsNullOrEmpty(i.UserFullName) && !string.IsNullOrEmpty(i.UserId))
        ////    //        .Select(i => new { i.UserFullName, i.UserId })
        ////    //        .Distinct()
        ////    //        .ToList();

        ////    //    // Populate the UserIDs that are missing, based can be looked up by user's full name. TODO: Confirm this logic, as it may not be correct if two or more user's have the same name.
        ////    //    foreach (var item in ownershipTopModel.Items.Where(i => string.IsNullOrEmpty(i.UserId)))
        ////    //    {
        ////    //        var match = uniqueUsers.FirstOrDefault(i => i.UserFullName == item.UserFullName);
        ////    //        if (match != null)
        ////    //        {
        ////    //            item.UserId = match.UserId;
        ////    //        }
        ////    //    }

        ////    //    //Now, remove any users whose internal ID cannot be resolved.
        ////    //    ownershipTopModel.Items.RemoveAll(i => string.IsNullOrEmpty(i.UserId));

        ////    //    var respString = PostJsonToApi(
        ////    //        $"{TargetUri}ownership/bulk",
        ////    //        TargetAuthString,
        ////    //        JsonConvert.SerializeObject(ownershipTopModel)
        ////    //    );
        ////    //}

        ////    //if (d3sImpactRelationships.Count > 0)
        ////    //{
        ////    //    //var respString = PostJsonToApi(
        ////    //    //    $"{TargetUri}relationships/bulk",
        ////    //    //    TargetAuthString,
        ////    //    //    JsonConvert.SerializeObject(d3sImpactRelationships)
        ////    //    //);
        ////    //}
        ////}
        
        #endregion

        #region RRP

        //public static void GetRrpFunctionalArea()
        //{
        //    var properties = "short_description,long_description".Split(',');
        //    var url = $"{SourceUri}search/?pageSize=75&types=$RRP-RRPFunctionalArea";
        //    foreach (var p in properties)
        //    {
        //        url += $"&properties={p}";
        //    }

        //    var arr = new List<D3sRrpFunctionalAreaModel>();

        //    Func<IgcRrpFunctionalAreaModels, IgcRrpFunctionalAreaModels> parse = delegate (IgcRrpFunctionalAreaModels root)
        //    {
        //        arr.AddRange(root.items.ConvertAll<D3sRrpFunctionalAreaModel>(i => new D3sRrpFunctionalAreaModel
        //        {
        //            SourceID = i.SourceID,
        //            Name = i.Name,
        //            ShortDescription = i.ShortDescription,
        //            LongDescription = i.LongDescription
        //        }));

        //        return root;
        //    };

        //    while (!string.IsNullOrEmpty(url))
        //    {
        //        try
        //        {
        //            var models = GetFromApi<IgcRrpFunctionalAreaModels>(url, SourceAuthString);
        //            if (models != null)
        //            {
        //                parse(models);
        //                url = models.paging.next;
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            CoreFunction.AITrackException(functionName, ex);
        //            url = null;
        //        }
        //    }

        //    try
        //    {
        //        var respString = PostJsonToApi(
        //            $"{TargetUri}TaxonomyType/3/bulk",
        //            TargetAuthString,
        //            JsonConvert.SerializeObject(arr)
        //        );
        //    }
        //    catch (Exception ex)
        //    {
        //        CoreFunction.AITrackException(functionName, ex);
        //    }
        //}

        //public static void GetRrpLevel1()
        //{
        //    var properties = "short_description,long_description".Split(',');
        //    var url = $"{SourceUri}search/?pageSize=75&types=$RRP-RRPLevel1Service";
        //    foreach (var p in properties)
        //    {
        //        url += $"&properties={p}";
        //    }

        //    var arr = new List<D3sRrpLevelOneModel>();

        //    Func<IgcRrpLevelOneModels, IgcRrpLevelOneModels> parse = delegate (IgcRrpLevelOneModels root)
        //    {
        //        arr.AddRange(root.items.ConvertAll<D3sRrpLevelOneModel>(i => new D3sRrpLevelOneModel
        //        {
        //            SourceID = i.SourceID,
        //            ParentSourceID = i._context[0]._id,
        //            Name = i.Name,
        //            ShortDescription = i.ShortDescription,
        //            LongDescription = i.LongDescription
        //        }));

        //        //ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
        //        //{
        //        //    SourceID = i.SourceID,
        //        //    RoleName = "Business Owner",
        //        //    UserId = i.BusinessOwnerId,
        //        //    UserFullName = i.BusinessOwner
        //        //}));

        //        //foreach (var app in root.items)
        //        //{
        //        //    app.ImpactsOn.items.ForEach(bu =>
        //        //    {
        //        //        d3sImpactRelationships.Add(
        //        //            new D3sBusinesUnitApplicationCatalogRelationshipModel { SubjectSourceID = bu.SourceID, ObjectSourceID = app.SourceID, PredicateType = 7 }
        //        //        );
        //        //    });
        //        //}

        //        return root;
        //    };

        //    while (!string.IsNullOrEmpty(url))
        //    {
        //        try
        //        {
        //            var models = GetFromApi<IgcRrpLevelOneModels>(url, SourceAuthString);
        //            if (models != null)
        //            {
        //                parse(models);
        //                url = models.paging.next;
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            CoreFunction.AITrackException(functionName, ex);
        //            url = null;
        //        }
        //    }

        //    try
        //    {
        //        var respString = PostJsonToApi(
        //            $"{TargetUri}TaxonomyType/3/bulk",
        //            TargetAuthString,
        //            JsonConvert.SerializeObject(arr)
        //        );
        //    }
        //    catch (Exception ex)
        //    {
        //        CoreFunction.AITrackException(functionName, ex);
        //    }
        //}

        //public static void GetRrpLevel2()
        //{
        //    var properties = "short_description,long_description".Split(',');
        //    var url = $"{SourceUri}search/?pageSize=75&types=$RRP-RRPLevel2Service";
        //    foreach (var p in properties)
        //    {
        //        url += $"&properties={p}";
        //    }

        //    var arr = new List<D3sRrpLevelTwoModel>();

        //    Func<IgcRrpLevelTwoModels, IgcRrpLevelTwoModels> parse = delegate (IgcRrpLevelTwoModels root)
        //    {
        //        arr.AddRange(root.items.ConvertAll<D3sRrpLevelTwoModel>(i => new D3sRrpLevelTwoModel
        //        {
        //            SourceID = i.SourceID,
        //            ParentSourceID = i._context[1]._id,
        //            Name = i.Name,
        //            ShortDescription = i.ShortDescription,
        //            LongDescription = i.LongDescription
        //        }));

        //        //ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
        //        //{
        //        //    SourceID = i.SourceID,
        //        //    RoleName = "Business Owner",
        //        //    UserId = i.BusinessOwnerId,
        //        //    UserFullName = i.BusinessOwner
        //        //}));

        //        //foreach (var app in root.items)
        //        //{
        //        //    app.ImpactsOn.items.ForEach(bu =>
        //        //    {
        //        //        d3sImpactRelationships.Add(
        //        //            new D3sBusinesUnitApplicationCatalogRelationshipModel { SubjectSourceID = bu.SourceID, ObjectSourceID = app.SourceID, PredicateType = 7 }
        //        //        );
        //        //    });
        //        //}

        //        return root;
        //    };

        //    while (!string.IsNullOrEmpty(url))
        //    {
        //        try
        //        {
        //            var models = GetFromApi<IgcRrpLevelTwoModels>(url, SourceAuthString);
        //            if (models != null)
        //            {
        //                parse(models);
        //                url = models.paging.next;
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            CoreFunction.AITrackException(functionName, ex);
        //            url = null;
        //        }
        //    }

        //    try
        //    {
        //        var respString = PostJsonToApi(
        //            $"{TargetUri}TaxonomyType/3/bulk",
        //            TargetAuthString,
        //            JsonConvert.SerializeObject(arr)
        //        );
        //    }
        //    catch (Exception ex)
        //    {
        //        CoreFunction.AITrackException(functionName, ex);
        //    }
        //}

        //public static void GetRrpLevel3()
        //{
        //    var properties = "short_description,long_description".Split(',');
        //    var url = $"{SourceUri}search/?pageSize=75&types=$RRP-RRPLevel3Service";
        //    foreach (var p in properties)
        //    {
        //        url += $"&properties={p}";
        //    }

        //    var arr = new List<D3sRrpLevelThreeModel>();

        //    Func<IgcRrpLevelThreeModels, IgcRrpLevelThreeModels> parse = delegate (IgcRrpLevelThreeModels root)
        //    {
        //        arr.AddRange(root.items.ConvertAll<D3sRrpLevelThreeModel>(i => new D3sRrpLevelThreeModel
        //        {
        //            SourceID = i.SourceID,
        //            ParentSourceID = i._context[2]._id,
        //            Name = i.Name,
        //            ShortDescription = i.ShortDescription,
        //            LongDescription = i.LongDescription
        //        }));

        //        //ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
        //        //{
        //        //    SourceID = i.SourceID,
        //        //    RoleName = "Business Owner",
        //        //    UserId = i.BusinessOwnerId,
        //        //    UserFullName = i.BusinessOwner
        //        //}));

        //        //foreach (var app in root.items)
        //        //{
        //        //    app.ImpactsOn.items.ForEach(bu =>
        //        //    {
        //        //        d3sImpactRelationships.Add(
        //        //            new D3sBusinesUnitApplicationCatalogRelationshipModel { SubjectSourceID = bu.SourceID, ObjectSourceID = app.SourceID, PredicateType = 7 }
        //        //        );
        //        //    });
        //        //}

        //        return root;
        //    };

        //    while (!string.IsNullOrEmpty(url))
        //    {
        //        try
        //        {
        //            var models = GetFromApi<IgcRrpLevelThreeModels>(url, SourceAuthString);
        //            if (models != null)
        //            {
        //                parse(models);
        //                url = models.paging.next;
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            CoreFunction.AITrackException(functionName, ex);
        //            url = null;
        //        }
        //    }

        //    try
        //    {
        //        var respString = PostJsonToApi(
        //            $"{TargetUri}TaxonomyType/3/bulk",
        //            TargetAuthString,
        //            JsonConvert.SerializeObject(arr)
        //        );
        //    }
        //    catch (Exception ex)
        //    {
        //        CoreFunction.AITrackException(functionName, ex);
        //    }
        //}

        #endregion

        #region Business Unit

        //public static void GetBuLevel1()
        //{
        //    var properties = "short_description,long_description,$BusinessUnitId,$BusinessOwnerId,$BusinessOwner".Split(',');
        //    var url = $"{SourceUri}search/?pageSize=75&types=$BUOrg-BusinessUnitLevel1";
        //    foreach (var p in properties)
        //    {
        //        url += $"&properties={p}";
        //    }

        //    var arr = new List<D3sBuTopModel>();

        //    Func<IgcBuTopModels, IgcBuTopModels> parse = delegate (IgcBuTopModels root)
        //    {
        //        arr.AddRange(root.items.ConvertAll<D3sBuTopModel>(i => new D3sBuTopModel
        //        {
        //            SourceID = i.SourceID,
        //            Name = i.Name,
        //            ShortDescription = i.ShortDescription,
        //            LongDescription = i.LongDescription,
        //            BusinessOwner = i.BusinessOwner,
        //            BusinessOwnerId = i.BusinessOwnerId,
        //            BusinessUnitID = i.BusinessUnitId
        //        }));

        //        //ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
        //        //{
        //        //    SourceID = i.SourceID,
        //        //    RoleName = "Business Owner",
        //        //    UserId = i.BusinessOwnerId,
        //        //    UserFullName = i.BusinessOwner
        //        //}));

        //        //foreach (var app in root.items)
        //        //{
        //        //    app.ImpactsOn.items.ForEach(bu =>
        //        //    {
        //        //        d3sImpactRelationships.Add(
        //        //            new D3sBusinesUnitApplicationCatalogRelationshipModel { SubjectSourceID = bu.SourceID, ObjectSourceID = app.SourceID, PredicateType = 7 }
        //        //        );
        //        //    });
        //        //}

        //        return root;
        //    };

        //    while (!string.IsNullOrEmpty(url))
        //    {
        //        try
        //        {
        //            var models = GetFromApi<IgcBuTopModels>(url, SourceAuthString);
        //            if (models != null)
        //            {
        //                parse(models);
        //                url = models.paging.next;
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            CoreFunction.AITrackException(functionName, ex);
        //            url = null;
        //        }
        //    }

        //    try
        //    {
        //        var respString = PostJsonToApi(
        //            $"{TargetUri}TaxonomyType/7/bulk",
        //            TargetAuthString,
        //            JsonConvert.SerializeObject(arr)
        //        );
        //    }
        //    catch (Exception ex)
        //    {
        //        CoreFunction.AITrackException(functionName, ex);
        //    }

        //}

        //public static void GetBuLevel2()
        //{
        //    var properties = "short_description,long_description,$BusinessUnitId,$BusinessOwnerId,$BusinessOwner".Split(',');
        //    var url = $"{SourceUri}search/?pageSize=75&types=$BUOrg-BusinessUnitLevel2";
        //    foreach (var p in properties)
        //    {
        //        url += $"&properties={p}";
        //    }

        //    var arr = new List<D3sBuChildModel>();

        //    Func<IgcBuChildModels, IgcBuChildModels> parse = delegate (IgcBuChildModels root)
        //    {
        //        arr.AddRange(root.items.ConvertAll<D3sBuChildModel>(i => new D3sBuChildModel
        //        {
        //            SourceID = i.SourceID,
        //            ParentSourceID = i._context[0]._id,
        //            Name = i.Name,
        //            ShortDescription = i.ShortDescription,
        //            LongDescription = i.LongDescription,
        //            BusinessOwner = i.BusinessOwner,
        //            BusinessOwnerId = i.BusinessOwnerId,
        //            BusinessUnitID = i.BusinessUnitId
        //        }));

        //        //ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
        //        //{
        //        //    SourceID = i.SourceID,
        //        //    RoleName = "Business Owner",
        //        //    UserId = i.BusinessOwnerId,
        //        //    UserFullName = i.BusinessOwner
        //        //}));

        //        //foreach (var app in root.items)
        //        //{
        //        //    app.ImpactsOn.items.ForEach(bu =>
        //        //    {
        //        //        d3sImpactRelationships.Add(
        //        //            new D3sBusinesUnitApplicationCatalogRelationshipModel { SubjectSourceID = bu.SourceID, ObjectSourceID = app.SourceID, PredicateType = 7 }
        //        //        );
        //        //    });
        //        //}

        //        return root;
        //    };

        //    while (!string.IsNullOrEmpty(url))
        //    {
        //        try
        //        {
        //            var models = GetFromApi<IgcBuChildModels>(url, SourceAuthString);
        //            if (models != null)
        //            {
        //                parse(models);
        //                url = models.paging.next;
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            CoreFunction.AITrackException(functionName, ex);
        //            url = null;
        //        }
        //    }

        //    try
        //    {
        //        var respString = PostJsonToApi(
        //            $"{TargetUri}TaxonomyType/7/bulk",
        //            TargetAuthString,
        //            JsonConvert.SerializeObject(arr)
        //        );
        //    }
        //    catch (Exception ex)
        //    {
        //        CoreFunction.AITrackException(functionName, ex);
        //    }
        //}

        //public static void GetBuLevel3()
        //{
        //    var properties = "short_description,long_description,$BusinessUnitId,$BusinessOwnerId,$BusinessOwner".Split(',');
        //    var url = $"{SourceUri}search/?pageSize=75&types=$BUOrg-BusinessUnitLevel3";
        //    foreach (var p in properties)
        //    {
        //        url += $"&properties={p}";
        //    }

        //    var arr = new List<D3sBuChildModel>();

        //    Func<IgcBuChildModels, IgcBuChildModels> parse = delegate (IgcBuChildModels root)
        //    {
        //        arr.AddRange(root.items.ConvertAll<D3sBuChildModel>(i => new D3sBuChildModel
        //        {
        //            SourceID = i.SourceID,
        //            ParentSourceID = i._context[1]._id,
        //            Name = i.Name,
        //            ShortDescription = i.ShortDescription,
        //            LongDescription = i.LongDescription,
        //            BusinessOwner = i.BusinessOwner,
        //            BusinessOwnerId = i.BusinessOwnerId,
        //            BusinessUnitID = i.BusinessUnitId
        //        }));

        //        //ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
        //        //{
        //        //    SourceID = i.SourceID,
        //        //    RoleName = "Business Owner",
        //        //    UserId = i.BusinessOwnerId,
        //        //    UserFullName = i.BusinessOwner
        //        //}));

        //        //foreach (var app in root.items)
        //        //{
        //        //    app.ImpactsOn.items.ForEach(bu =>
        //        //    {
        //        //        d3sImpactRelationships.Add(
        //        //            new D3sBusinesUnitApplicationCatalogRelationshipModel { SubjectSourceID = bu.SourceID, ObjectSourceID = app.SourceID, PredicateType = 7 }
        //        //        );
        //        //    });
        //        //}

        //        return root;
        //    };

        //    while (!string.IsNullOrEmpty(url))
        //    {
        //        try
        //        {
        //            var models = GetFromApi<IgcBuChildModels>(url, SourceAuthString);
        //            if (models != null)
        //            {
        //                parse(models);
        //                url = models.paging.next;
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            CoreFunction.AITrackException(functionName, ex);
        //            url = null;
        //        }
        //    }

        //    try
        //    {
        //    var respString = PostJsonToApi(
        //        $"{TargetUri}TaxonomyType/7/bulk",
        //        TargetAuthString,
        //        JsonConvert.SerializeObject(arr)
        //    );
        //    }
        //    catch (Exception ex)
        //    {
        //        CoreFunction.AITrackException(functionName, ex);
        //    }
        //}

        //public static void GetBuLevel4()
        //{
        //    var properties = "short_description,long_description,$BusinessUnitId,$BusinessOwnerId,$BusinessOwner".Split(',');
        //    var url = $"{SourceUri}search/?pageSize=75&types=$BUOrg-BusinessUnitLevel4";
        //    foreach (var p in properties)
        //    {
        //        url += $"&properties={p}";
        //    }

        //    var arr = new List<D3sBuChildModel>();

        //    Func<IgcBuChildModels, IgcBuChildModels> parse = delegate (IgcBuChildModels root)
        //    {
        //        arr.AddRange(root.items.ConvertAll<D3sBuChildModel>(i => new D3sBuChildModel
        //        {
        //            SourceID = i.SourceID,
        //            ParentSourceID = i._context[2]._id,
        //            Name = i.Name,
        //            ShortDescription = i.ShortDescription,
        //            LongDescription = i.LongDescription,
        //            BusinessOwner = i.BusinessOwner,
        //            BusinessOwnerId = i.BusinessOwnerId,
        //            BusinessUnitID = i.BusinessUnitId
        //        }));

        //        //ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
        //        //{
        //        //    SourceID = i.SourceID,
        //        //    RoleName = "Business Owner",
        //        //    UserId = i.BusinessOwnerId,
        //        //    UserFullName = i.BusinessOwner
        //        //}));

        //        //foreach (var app in root.items)
        //        //{
        //        //    app.ImpactsOn.items.ForEach(bu =>
        //        //    {
        //        //        d3sImpactRelationships.Add(
        //        //            new D3sBusinesUnitApplicationCatalogRelationshipModel { SubjectSourceID = bu.SourceID, ObjectSourceID = app.SourceID, PredicateType = 7 }
        //        //        );
        //        //    });
        //        //}

        //        return root;
        //    };

        //    while (!string.IsNullOrEmpty(url))
        //    {
        //        try
        //        {
        //            var models = GetFromApi<IgcBuChildModels>(url, SourceAuthString);
        //            if (models != null)
        //            {
        //                parse(models);
        //                url = models.paging.next;
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            CoreFunction.AITrackException(functionName, ex);
        //            url = null;
        //        }
        //    }

        //    try
        //    {
        //        var respString = PostJsonToApi(
        //            $"{TargetUri}TaxonomyType/7/bulk",
        //            TargetAuthString,
        //            JsonConvert.SerializeObject(arr)
        //        );
        //    }
        //    catch (Exception ex)
        //    {
        //        CoreFunction.AITrackException(functionName, ex);
        //    }
        //}

        //public static void GetBuLevel5()
        //{
        //    var properties = "short_description,long_description,$BusinessUnitId,$BusinessOwnerId,$BusinessOwner".Split(',');
        //    var url = $"{SourceUri}search/?pageSize=75&types=$BUOrg-BusinessUnitLevel5";
        //    foreach (var p in properties)
        //    {
        //        url += $"&properties={p}";
        //    }

        //    var arr = new List<D3sBuChildModel>();

        //    Func<IgcBuChildModels, IgcBuChildModels> parse = delegate (IgcBuChildModels root)
        //    {
        //        arr.AddRange(root.items.ConvertAll<D3sBuChildModel>(i => new D3sBuChildModel
        //        {
        //            SourceID = i.SourceID,
        //            ParentSourceID = i._context[3]._id,
        //            Name = i.Name,
        //            ShortDescription = i.ShortDescription,
        //            LongDescription = i.LongDescription,
        //            BusinessOwner = i.BusinessOwner,
        //            BusinessOwnerId = i.BusinessOwnerId,
        //            BusinessUnitID = i.BusinessUnitId
        //        }));

        //        //ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
        //        //{
        //        //    SourceID = i.SourceID,
        //        //    RoleName = "Business Owner",
        //        //    UserId = i.BusinessOwnerId,
        //        //    UserFullName = i.BusinessOwner
        //        //}));

        //        //foreach (var app in root.items)
        //        //{
        //        //    app.ImpactsOn.items.ForEach(bu =>
        //        //    {
        //        //        d3sImpactRelationships.Add(
        //        //            new D3sBusinesUnitApplicationCatalogRelationshipModel { SubjectSourceID = bu.SourceID, ObjectSourceID = app.SourceID, PredicateType = 7 }
        //        //        );
        //        //    });
        //        //}

        //        return root;
        //    };

        //    while (!string.IsNullOrEmpty(url))
        //    {
        //        try
        //        {
        //            var models = GetFromApi<IgcBuChildModels>(url, SourceAuthString);
        //            if (models != null)
        //            {
        //                parse(models);
        //                url = models.paging.next;
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            CoreFunction.AITrackException(functionName, ex);
        //            url = null;
        //        }
        //    }

        //    try
        //    {
        //        var respString = PostJsonToApi(
        //            $"{TargetUri}TaxonomyType/7/bulk",
        //            TargetAuthString,
        //            JsonConvert.SerializeObject(arr)
        //        );
        //    }
        //    catch (Exception ex)
        //    {
        //        CoreFunction.AITrackException(functionName, ex);
        //    }
        //}

        //public static void GetBuLevel6()
        //{
        //    var properties = "short_description,long_description,$BusinessUnitId,$BusinessOwnerId,$BusinessOwner".Split(',');
        //    var url = $"{SourceUri}search/?pageSize=75&types=$BUOrg-BusinessUnitLevel6";
        //    foreach (var p in properties)
        //    {
        //        url += $"&properties={p}";
        //    }

        //    var arr = new List<D3sBuChildModel>();

        //    Func<IgcBuChildModels, IgcBuChildModels> parse = delegate (IgcBuChildModels root)
        //    {
        //        arr.AddRange(root.items.ConvertAll<D3sBuChildModel>(i => new D3sBuChildModel
        //        {
        //            SourceID = i.SourceID,
        //            ParentSourceID = i._context[4]._id,
        //            Name = i.Name,
        //            ShortDescription = i.ShortDescription,
        //            LongDescription = i.LongDescription,
        //            BusinessOwner = i.BusinessOwner,
        //            BusinessOwnerId = i.BusinessOwnerId,
        //            BusinessUnitID = i.BusinessUnitId
        //        }));

        //        //ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
        //        //{
        //        //    SourceID = i.SourceID,
        //        //    RoleName = "Business Owner",
        //        //    UserId = i.BusinessOwnerId,
        //        //    UserFullName = i.BusinessOwner
        //        //}));

        //        //foreach (var app in root.items)
        //        //{
        //        //    app.ImpactsOn.items.ForEach(bu =>
        //        //    {
        //        //        d3sImpactRelationships.Add(
        //        //            new D3sBusinesUnitApplicationCatalogRelationshipModel { SubjectSourceID = bu.SourceID, ObjectSourceID = app.SourceID, PredicateType = 7 }
        //        //        );
        //        //    });
        //        //}

        //        return root;
        //    };

        //    while (!string.IsNullOrEmpty(url))
        //    {
        //        try
        //        {
        //            var models = GetFromApi<IgcBuChildModels>(url, SourceAuthString);
        //            if (models != null)
        //            {
        //                parse(models);
        //                url = models.paging.next;
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            CoreFunction.AITrackException(functionName, ex);
        //            url = null;
        //        }
        //    }

        //    try
        //    {
        //        var respString = PostJsonToApi(
        //            $"{TargetUri}TaxonomyType/7/bulk",
        //            TargetAuthString,
        //            JsonConvert.SerializeObject(arr)
        //        );
        //    }
        //    catch (Exception ex)
        //    {
        //        CoreFunction.AITrackException(functionName, ex);
        //    }
        //}

        //public static void GetBuLevel7()
        //{
        //    var properties = "short_description,long_description,$BusinessUnitId,$BusinessOwnerId,$BusinessOwner".Split(',');
        //    var url = $"{SourceUri}search/?pageSize=75&types=$BUOrg-BusinessUnitLevel7";
        //    foreach (var p in properties)
        //    {
        //        url += $"&properties={p}";
        //    }

        //    var arr = new List<D3sBuChildModel>();

        //    Func<IgcBuChildModels, IgcBuChildModels> parse = delegate (IgcBuChildModels root)
        //    {
        //        arr.AddRange(root.items.ConvertAll<D3sBuChildModel>(i => new D3sBuChildModel
        //        {
        //            SourceID = i.SourceID,
        //            ParentSourceID = i._context[5]._id,
        //            Name = i.Name,
        //            ShortDescription = i.ShortDescription,
        //            LongDescription = i.LongDescription,
        //            BusinessOwner = i.BusinessOwner,
        //            BusinessOwnerId = i.BusinessOwnerId,
        //            BusinessUnitID = i.BusinessUnitId
        //        }));

        //        //ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
        //        //{
        //        //    SourceID = i.SourceID,
        //        //    RoleName = "Business Owner",
        //        //    UserId = i.BusinessOwnerId,
        //        //    UserFullName = i.BusinessOwner
        //        //}));

        //        //foreach (var app in root.items)
        //        //{
        //        //    app.ImpactsOn.items.ForEach(bu =>
        //        //    {
        //        //        d3sImpactRelationships.Add(
        //        //            new D3sBusinesUnitApplicationCatalogRelationshipModel { SubjectSourceID = bu.SourceID, ObjectSourceID = app.SourceID, PredicateType = 7 }
        //        //        );
        //        //    });
        //        //}

        //        return root;
        //    };

        //    while (!string.IsNullOrEmpty(url))
        //    {
        //        try
        //        {
        //            var models = GetFromApi<IgcBuChildModels>(url, SourceAuthString);
        //            if (models != null)
        //            {
        //                parse(models);
        //                url = models.paging.next;
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            CoreFunction.AITrackException(functionName, ex);
        //            url = null;
        //        }
        //    }

        //    try
        //    {
        //        var respString = PostJsonToApi(
        //            $"{TargetUri}TaxonomyType/7/bulk",
        //            TargetAuthString,
        //            JsonConvert.SerializeObject(arr)
        //        );
        //    }
        //    catch (Exception ex)
        //    {
        //        CoreFunction.AITrackException(functionName, ex);
        //    }
        //}

        #endregion
    }
}
