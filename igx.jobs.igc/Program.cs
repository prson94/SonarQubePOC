using ApplicationInsights.Helpers.WebJobs;
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
            var config = new JobHostConfiguration {
                DashboardConnectionString = CoreFunction.GetConfigValueByKey("WebJobsAccount"),
                StorageConnectionString = CoreFunction.GetConfigValueByKey("WebJobsAccount"),
                NameResolver = new QueueNameResolver()
            };

            if (config.IsDevelopment)
            {
                config.UseDevelopmentSettings();
            }

            config.UseApplicationInsights();
            config.UseCore();
            config.UseTimers();

            var host = new JobHost(config);
            host.RunAndBlock();
        }
    }

    public static class IgcIntegration
    {
        const string functionName = "IGC_Integration";
        //const string timerSettings = "0 */30 * * * *";
        const string timerSettings = "*/10 * * * * *";

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

        static List<MappingType> InputMappingTypes = new List<MappingType> {
            new MappingType { Id = 1, IgcType = "$ApplicationCatalog-ApplicationCatalog", GovernType = "ArtifactType", GovernTypeID = 2 },

            new MappingType { Id = 2, IgcType = "$RRP-RRPFunctionalArea", GovernType = "TaxonomyType", GovernTypeID = 3 },
            new MappingType { Id = 3, IgcType = "$RRP-RRPLevel1Service", GovernType = "TaxonomyType", GovernTypeID = 3 },
            new MappingType { Id = 4, IgcType = "$RRP-RRPLevel2Service", GovernType = "TaxonomyType", GovernTypeID = 3 },
            new MappingType { Id = 5, IgcType = "$RRP-RRPLevel3Service", GovernType = "TaxonomyType", GovernTypeID = 3 },

            new MappingType { Id = 6, IgcType = "$BUOrg-BusinessUnitLevel1", GovernType = "TaxonomyType", GovernTypeID = 7 },
            new MappingType { Id = 7, IgcType = "$BUOrg-BusinessUnitLevel2", GovernType = "TaxonomyType", GovernTypeID = 7 },
            new MappingType { Id = 8, IgcType = "$BUOrg-BusinessUnitLevel3", GovernType = "TaxonomyType", GovernTypeID = 7 },
            new MappingType { Id = 9, IgcType = "$BUOrg-BusinessUnitLevel4", GovernType = "TaxonomyType", GovernTypeID = 7 },
            new MappingType { Id = 10, IgcType = "$BUOrg-BusinessUnitLevel5", GovernType = "TaxonomyType", GovernTypeID = 7 },
            new MappingType { Id = 11, IgcType = "$BUOrg-BusinessUnitLevel6", GovernType = "TaxonomyType", GovernTypeID = 7 },
            new MappingType { Id = 12, IgcType = "$BUOrg-BusinessUnitLevel7", GovernType = "TaxonomyType", GovernTypeID = 7 },

            new MappingType { Id = 13, IgcType = "host", GovernType = "FusionAttributeType", GovernTypeID = 2 },
            new MappingType { Id = 14, IgcType = "data_file", GovernType = "FusionAttributeType", GovernTypeID = 2 },
            new MappingType { Id = 15, IgcType = "data_file_field", GovernType = "FusionAttributeType", GovernTypeID = 2 },
            new MappingType { Id = 16, IgcType = "database", GovernType = "FusionAttributeType", GovernTypeID = 2 },
            new MappingType { Id = 17, IgcType = "database_schema", GovernType = "FusionAttributeType", GovernTypeID = 2 },
            new MappingType { Id = 18, IgcType = "database_table", GovernType = "FusionAttributeType", GovernTypeID = 2 },
            new MappingType { Id = 19, IgcType = "database_view", GovernType = "FusionAttributeType", GovernTypeID = 2 },
            new MappingType { Id = 20, IgcType = "data_element", GovernType = "FusionAttributeType", GovernTypeID = 2 }
        };

        static List<MappingFieldItem> InputMappingFieldItems = new List<MappingFieldItem> {
            new MappingFieldItem { MappingTypeId = 1, IgcField = "_id", GovernField = "SourceID" },
            new MappingFieldItem { MappingTypeId = 1, IgcField = "_name", GovernField = "Name" },
            new MappingFieldItem { MappingTypeId = 1, IgcField = "short_description", GovernField = "ShortDescription" },
            new MappingFieldItem { MappingTypeId = 1, IgcField = "long_description,", GovernField = "LongDescription" },
            //new MappingFieldItem { MappingTypeId = 1, IgcField = "labels", GovernField = "" },
            //new MappingFieldItem { MappingTypeId = 1, IgcField = "stewards", GovernField = "" },
            //new MappingFieldItem { MappingTypeId = 1, IgcField = "assigned_to_terms", GovernField = "" },
            //new MappingFieldItem { MappingTypeId = 1, IgcField = "implements_rules", GovernField = "" },
            //new MappingFieldItem { MappingTypeId = 1, IgcField = "governed_by_rules", GovernField = "" },
            new MappingFieldItem { MappingTypeId = 1, IgcField = "$CMDBAppCode", GovernField = "CMDBAppCode" },
            new MappingFieldItem { MappingTypeId = 1, IgcField = "$ApplicationAlias", GovernField = "ApplicationAlias" },
            new MappingFieldItem { MappingTypeId = 1, IgcField = "$Comments", GovernField = "Comments" },
            new MappingFieldItem { MappingTypeId = 1, IgcField = "$SSID,", GovernField = "SSID" },
            new MappingFieldItem { MappingTypeId = 1, IgcField = "$KeyApplicationType", GovernField = "KeyApplicationType", IsArray = true },
            new MappingFieldItem { MappingTypeId = 1, IgcField = "$Status", GovernField = "Status" },
            new MappingFieldItem { MappingTypeId = 1, IgcField = "$DataLocation", GovernField = "DataLocation" },
            new MappingFieldItem { MappingTypeId = 1, IgcField = "$PersonalData", GovernField = "PersonalData" },
            new MappingFieldItem { MappingTypeId = 1, IgcField = "$ComponentType", GovernField = "ComponentType" },
            //new MappingFieldItem { MappingTypeId = 1, IgcField = "$ComponentCode", GovernField = "" },
            new MappingFieldItem { MappingTypeId = 1, IgcField = "$ComponentSAID", GovernField = "ComponentSAID" },
            new MappingFieldItem { MappingTypeId = 1, IgcField = "$AuthoritativeSource", GovernField = "AuthoritativeSource" },
            new MappingFieldItem { MappingTypeId = 1, IgcField = "$MaturityLevel", GovernField = "MaturityLevel" },
            new MappingFieldItem { MappingTypeId = 1, IgcField = "$BookOfRecord", GovernField = "BookOfRecord" },

            new MappingFieldItem { MappingTypeId = 2, IgcField = "_id", GovernField = "SourceID" },
            new MappingFieldItem { MappingTypeId = 2, IgcField = "_name", GovernField = "Name" },
            new MappingFieldItem { MappingTypeId = 2, IgcField = "short_description", GovernField = "ShortDescription" },
            new MappingFieldItem { MappingTypeId = 2, IgcField = "long_description,", GovernField = "LongDescription" },

            new MappingFieldItem { MappingTypeId = 3, IgcField = "_id", GovernField = "SourceID" },
            new MappingFieldItem { MappingTypeId = 3, IgcField = "_context", GovernField = "ParentSourceID", ParentContextPosition = 0 },
            new MappingFieldItem { MappingTypeId = 3, IgcField = "_name", GovernField = "Name" },
            new MappingFieldItem { MappingTypeId = 3, IgcField = "short_description", GovernField = "ShortDescription" },
            new MappingFieldItem { MappingTypeId = 3, IgcField = "long_description,", GovernField = "LongDescription" },

            new MappingFieldItem { MappingTypeId = 4, IgcField = "_id", GovernField = "SourceID" },
            new MappingFieldItem { MappingTypeId = 4, IgcField = "_context", GovernField = "ParentSourceID", ParentContextPosition = 1 },
            new MappingFieldItem { MappingTypeId = 4, IgcField = "_name", GovernField = "Name" },
            new MappingFieldItem { MappingTypeId = 4, IgcField = "short_description", GovernField = "ShortDescription" },
            new MappingFieldItem { MappingTypeId = 4, IgcField = "long_description,", GovernField = "LongDescription" },

            new MappingFieldItem { MappingTypeId = 5, IgcField = "_id", GovernField = "SourceID" },
            new MappingFieldItem { MappingTypeId = 5, IgcField = "_context", GovernField = "ParentSourceID", ParentContextPosition = 2 },
            new MappingFieldItem { MappingTypeId = 5, IgcField = "_name", GovernField = "Name" },
            new MappingFieldItem { MappingTypeId = 5, IgcField = "short_description", GovernField = "ShortDescription" },
            new MappingFieldItem { MappingTypeId = 5, IgcField = "long_description,", GovernField = "LongDescription" },

            new MappingFieldItem { MappingTypeId = 6, IgcField = "_id", GovernField = "SourceID" },
            new MappingFieldItem { MappingTypeId = 6, IgcField = "_name", GovernField = "Name" },
            new MappingFieldItem { MappingTypeId = 6, IgcField = "short_description", GovernField = "ShortDescription" },
            new MappingFieldItem { MappingTypeId = 6, IgcField = "long_description,", GovernField = "LongDescription" },
            new MappingFieldItem { MappingTypeId = 6, IgcField = "$BusinessUnitId", GovernField = "BusinessUnitID" },

            new MappingFieldItem { MappingTypeId = 7, IgcField = "_id", GovernField = "SourceID" },
            new MappingFieldItem { MappingTypeId = 7, IgcField = "_context", GovernField = "ParentSourceID", ParentContextPosition = 0 },
            new MappingFieldItem { MappingTypeId = 7, IgcField = "_name", GovernField = "Name" },
            new MappingFieldItem { MappingTypeId = 7, IgcField = "short_description", GovernField = "ShortDescription" },
            new MappingFieldItem { MappingTypeId = 7, IgcField = "long_description,", GovernField = "LongDescription" },
            new MappingFieldItem { MappingTypeId = 7, IgcField = "$BusinessUnitId", GovernField = "BusinessUnitID" },

            new MappingFieldItem { MappingTypeId = 8, IgcField = "_id", GovernField = "SourceID" },
            new MappingFieldItem { MappingTypeId = 8, IgcField = "_context", GovernField = "ParentSourceID", ParentContextPosition = 1 },
            new MappingFieldItem { MappingTypeId = 8, IgcField = "_name", GovernField = "Name" },
            new MappingFieldItem { MappingTypeId = 8, IgcField = "short_description", GovernField = "ShortDescription" },
            new MappingFieldItem { MappingTypeId = 8, IgcField = "long_description,", GovernField = "LongDescription" },
            new MappingFieldItem { MappingTypeId = 8, IgcField = "$BusinessUnitId", GovernField = "BusinessUnitID" },

            new MappingFieldItem { MappingTypeId = 9, IgcField = "_id", GovernField = "SourceID" },
            new MappingFieldItem { MappingTypeId = 9, IgcField = "_context", GovernField = "ParentSourceID", ParentContextPosition = 2 },
            new MappingFieldItem { MappingTypeId = 9, IgcField = "_name", GovernField = "Name" },
            new MappingFieldItem { MappingTypeId = 9, IgcField = "short_description", GovernField = "ShortDescription" },
            new MappingFieldItem { MappingTypeId = 9, IgcField = "long_description,", GovernField = "LongDescription" },
            new MappingFieldItem { MappingTypeId = 9, IgcField = "$BusinessUnitId", GovernField = "BusinessUnitID" },

            new MappingFieldItem { MappingTypeId = 10, IgcField = "_id", GovernField = "SourceID" },
            new MappingFieldItem { MappingTypeId = 10, IgcField = "_context", GovernField = "ParentSourceID", ParentContextPosition = 3 },
            new MappingFieldItem { MappingTypeId = 10, IgcField = "_name", GovernField = "Name" },
            new MappingFieldItem { MappingTypeId = 10, IgcField = "short_description", GovernField = "ShortDescription" },
            new MappingFieldItem { MappingTypeId = 10, IgcField = "long_description,", GovernField = "LongDescription" },
            new MappingFieldItem { MappingTypeId = 10, IgcField = "$BusinessUnitId", GovernField = "BusinessUnitID" },

            new MappingFieldItem { MappingTypeId = 11, IgcField = "_id", GovernField = "SourceID" },
            new MappingFieldItem { MappingTypeId = 11, IgcField = "_context", GovernField = "ParentSourceID", ParentContextPosition = 4 },
            new MappingFieldItem { MappingTypeId = 11, IgcField = "_name", GovernField = "Name" },
            new MappingFieldItem { MappingTypeId = 11, IgcField = "short_description", GovernField = "ShortDescription" },
            new MappingFieldItem { MappingTypeId = 11, IgcField = "long_description,", GovernField = "LongDescription" },
            new MappingFieldItem { MappingTypeId = 11, IgcField = "$BusinessUnitId", GovernField = "BusinessUnitID" },

            new MappingFieldItem { MappingTypeId = 12, IgcField = "_id", GovernField = "SourceID" },
            new MappingFieldItem { MappingTypeId = 12, IgcField = "_context", GovernField = "ParentSourceID", ParentContextPosition = 5 },
            new MappingFieldItem { MappingTypeId = 12, IgcField = "_name", GovernField = "Name" },
            new MappingFieldItem { MappingTypeId = 12, IgcField = "short_description", GovernField = "ShortDescription" },
            new MappingFieldItem { MappingTypeId = 12, IgcField = "long_description,", GovernField = "LongDescription" },
            new MappingFieldItem { MappingTypeId = 12, IgcField = "$BusinessUnitId", GovernField = "BusinessUnitID" },

            new MappingFieldItem { MappingTypeId = 13, IgcField = "_id", GovernField = "SourceID" },
            new MappingFieldItem { MappingTypeId = 13, IgcField = "_name", GovernField = "Name" },
            new MappingFieldItem { MappingTypeId = 13, IgcField = "short_description", GovernField = "ShortDescription" },
            new MappingFieldItem { MappingTypeId = 13, IgcField = "long_description,", GovernField = "LongDescription" },
            new MappingFieldItem { MappingTypeId = 13, IgcField = "location", GovernField = "Location" },
            new MappingFieldItem { MappingTypeId = 13, IgcField = "network_node", GovernField = "NetworkNode" },

            new MappingFieldItem { MappingTypeId = 14, IgcField = "_id", GovernField = "SourceID" },
            //new MappingFieldItem { MappingTypeId = 14, IgcField = "_context", GovernField = "ParentSourceID", ParentContextPosition = 0 },
            new MappingFieldItem { MappingTypeId = 14, IgcField = "_name", GovernField = "Name" },
            new MappingFieldItem { MappingTypeId = 14, IgcField = "short_description", GovernField = "ShortDescription" },
            new MappingFieldItem { MappingTypeId = 14, IgcField = "long_description,", GovernField = "LongDescription" },
            new MappingFieldItem { MappingTypeId = 14, IgcField = "custom_Catalog Status", GovernField = "CatalogStatus" },
            new MappingFieldItem { MappingTypeId = 14, IgcField = "custom_Classification", GovernField = "Classification" },
            new MappingFieldItem { MappingTypeId = 14, IgcField = "custom_Classification", GovernField = "Classification" },
            new MappingFieldItem { MappingTypeId = 14, IgcField = "custom_Comments", GovernField = "Comments" },
            new MappingFieldItem { MappingTypeId = 14, IgcField = "custom_Frequency", GovernField = "Frequency" },
            new MappingFieldItem { MappingTypeId = 14, IgcField = "custom_Information Classification", GovernField = "InformationClassification" },
            new MappingFieldItem { MappingTypeId = 14, IgcField = "custom_Output Format", GovernField = "OutputFormat" },
            new MappingFieldItem { MappingTypeId = 14, IgcField = "custom_Status", GovernField = "Status" },
            new MappingFieldItem { MappingTypeId = 14, IgcField = "alias_(business_name)", GovernField = "Alias" },
            new MappingFieldItem { MappingTypeId = 14, IgcField = "path", GovernField = "Path" },
            new MappingFieldItem { MappingTypeId = 14, IgcField = "store_type", GovernField = "StoreType" },

            new MappingFieldItem { MappingTypeId = 15, IgcField = "_id", GovernField = "SourceID" },
            //new MappingFieldItem { MappingTypeId = 15, IgcField = "_context", GovernField = "ParentSourceID", ParentContextPosition = 1 },
            new MappingFieldItem { MappingTypeId = 15, IgcField = "_name", GovernField = "Name" },
            new MappingFieldItem { MappingTypeId = 15, IgcField = "short_description", GovernField = "ShortDescription" },
            new MappingFieldItem { MappingTypeId = 15, IgcField = "long_description", GovernField = "LongDescription" },
            new MappingFieldItem { MappingTypeId = 15, IgcField = "qualityScore", GovernField = "QualityScore" },
            new MappingFieldItem { MappingTypeId = 15, IgcField = "custom_Catalog Status", GovernField = "CatalogStatus" },
            new MappingFieldItem { MappingTypeId = 15, IgcField = "custom_CDE", GovernField = "CDE" },
            new MappingFieldItem { MappingTypeId = 15, IgcField = "custom_Data Element Definition", GovernField = "DataElementDefinition" },
            new MappingFieldItem { MappingTypeId = 15, IgcField = "custom_Data Origin Type", GovernField = "DataOriginType" },
            new MappingFieldItem { MappingTypeId = 15, IgcField = "custom_Information Classification", GovernField = "InformationClassification" },
            new MappingFieldItem { MappingTypeId = 15, IgcField = "custom_Privacy Treatment", GovernField = "PrivacyTreatment" },
            new MappingFieldItem { MappingTypeId = 15, IgcField = "custom_Trusted Source", GovernField = "TrustedSource" },
            new MappingFieldItem { MappingTypeId = 15, IgcField = "data_type", GovernField = "DataType" },
            new MappingFieldItem { MappingTypeId = 15, IgcField = "selected_classification", GovernField = "SelectedClassification" },
            new MappingFieldItem { MappingTypeId = 15, IgcField = "detected_classification", GovernField = "DetectedClassification" },
            new MappingFieldItem { MappingTypeId = 15, IgcField = "odbc_type", GovernField = "OdbcType" },
            new MappingFieldItem { MappingTypeId = 15, IgcField = "length", GovernField = "Length" },
            new MappingFieldItem { MappingTypeId = 15, IgcField = "minimum_length", GovernField = "MinimumLength" },
            new MappingFieldItem { MappingTypeId = 15, IgcField = "fraction", GovernField = "Fraction" },
            new MappingFieldItem { MappingTypeId = 15, IgcField = "position", GovernField = "Position" },
            new MappingFieldItem { MappingTypeId = 15, IgcField = "level", GovernField = "Level" },
            new MappingFieldItem { MappingTypeId = 15, IgcField = "allows_null_values", GovernField = "AllowNullValues" },
            new MappingFieldItem { MappingTypeId = 15, IgcField = "unique", GovernField = "Unique" },
            new MappingFieldItem { MappingTypeId = 15, IgcField = "same_as_data_sources", GovernField = "SameAsDataSources" },

            new MappingFieldItem { MappingTypeId = 16, IgcField = "_id", GovernField = "SourceID" },
            //new MappingFieldItem { MappingTypeId = 16, IgcField = "_context", GovernField = "ParentSourceID", ParentContextPosition = 0 },
            new MappingFieldItem { MappingTypeId = 16, IgcField = "_name", GovernField = "Name" },
            new MappingFieldItem { MappingTypeId = 16, IgcField = "short_description", GovernField = "ShortDescription" },
            new MappingFieldItem { MappingTypeId = 16, IgcField = "long_description", GovernField = "LongDescription" },
            new MappingFieldItem { MappingTypeId = 16, IgcField = "custom_Catalog Status", GovernField = "CatalogStatus" },
            new MappingFieldItem { MappingTypeId = 16, IgcField = "alias_(business_name)", GovernField = "Alias" },
            new MappingFieldItem { MappingTypeId = 16, IgcField = "location", GovernField = "Location" },
            new MappingFieldItem { MappingTypeId = 16, IgcField = "dbms", GovernField = "Dbms" },
            new MappingFieldItem { MappingTypeId = 16, IgcField = "dms_server_instance", GovernField = "ServerInstance" },
            new MappingFieldItem { MappingTypeId = 16, IgcField = "dbms_vendor", GovernField = "Vendor" },
            new MappingFieldItem { MappingTypeId = 16, IgcField = "dbms_version", GovernField = "Version" },
            new MappingFieldItem { MappingTypeId = 16, IgcField = "database_type", GovernField = "DatabaseType" },
            new MappingFieldItem { MappingTypeId = 16, IgcField = "mapped_to_mdm_models", GovernField = "MappedToMdmModels" },
            new MappingFieldItem { MappingTypeId = 16, IgcField = "Notes", GovernField = "Notes" },


            new MappingFieldItem { MappingTypeId = 17, IgcField = "_id", GovernField = "SourceID" },
            //new MappingFieldItem { MappingTypeId = 17, IgcField = "_context", GovernField = "ParentSourceID", ParentContextPosition = 1 },
            new MappingFieldItem { MappingTypeId = 17, IgcField = "_name", GovernField = "Name" },
            new MappingFieldItem { MappingTypeId = 17, IgcField = "short_description", GovernField = "ShortDescription" },
            new MappingFieldItem { MappingTypeId = 17, IgcField = "long_description", GovernField = "LongDescription" },

            new MappingFieldItem { MappingTypeId = 18, IgcField = "_id", GovernField = "SourceID" },
            //new MappingFieldItem { MappingTypeId = 18, IgcField = "_context", GovernField = "ParentSourceID", ParentContextPosition = 2 },
            new MappingFieldItem { MappingTypeId = 18, IgcField = "_name", GovernField = "Name" },
            new MappingFieldItem { MappingTypeId = 18, IgcField = "short_description", GovernField = "ShortDescription" },
            new MappingFieldItem { MappingTypeId = 18, IgcField = "long_description", GovernField = "LongDescription" },
            new MappingFieldItem { MappingTypeId = 18, IgcField = "qualityScore", GovernField = "QualityScore" },
            new MappingFieldItem { MappingTypeId = 18, IgcField = "custom_Catalog Status", GovernField = "CatalogStatus" },
            new MappingFieldItem { MappingTypeId = 18, IgcField = "custom_Data Element Location", GovernField = "DataElementLocation" },
            new MappingFieldItem { MappingTypeId = 18, IgcField = "alias_(business_name)", GovernField = "Alias" },
            new MappingFieldItem { MappingTypeId = 18, IgcField = "reviewDate", GovernField = "ReviewDate" },
            new MappingFieldItem { MappingTypeId = 18, IgcField = "Notes", GovernField = "Notes" },

            new MappingFieldItem { MappingTypeId = 19, IgcField = "_id", GovernField = "SourceID" },
            //new MappingFieldItem { MappingTypeId = 19, IgcField = "_context", GovernField = "ParentSourceID", ParentContextPosition = 2 },
            new MappingFieldItem { MappingTypeId = 19, IgcField = "_name", GovernField = "Name" },
            new MappingFieldItem { MappingTypeId = 19, IgcField = "short_description", GovernField = "ShortDescription" },
            new MappingFieldItem { MappingTypeId = 19, IgcField = "long_description", GovernField = "LongDescription" },
            new MappingFieldItem { MappingTypeId = 19, IgcField = "qualityScore", GovernField = "QualityScore" },
            new MappingFieldItem { MappingTypeId = 19, IgcField = "custom_Catalog Status", GovernField = "CatalogStatus" },
            new MappingFieldItem { MappingTypeId = 19, IgcField = "custom_Data Element Location", GovernField = "DataElementLocation" },
            new MappingFieldItem { MappingTypeId = 19, IgcField = "alias_(business_name)", GovernField = "Alias" },
            new MappingFieldItem { MappingTypeId = 19, IgcField = "reviewDate", GovernField = "ReviewDate" },
            new MappingFieldItem { MappingTypeId = 19, IgcField = "fieldCount", GovernField = "FieldCount" },
            new MappingFieldItem { MappingTypeId = 19, IgcField = "notes", GovernField = "Notes" },

            new MappingFieldItem { MappingTypeId = 20, IgcField = "_id", GovernField = "SourceID" },
            //new MappingFieldItem { MappingTypeId = 20, IgcField = "_context", GovernField = "ParentSourceID", ParentContextPosition = 2 },
            new MappingFieldItem { MappingTypeId = 20, IgcField = "_name", GovernField = "Name" },
            new MappingFieldItem { MappingTypeId = 20, IgcField = "short_description", GovernField = "ShortDescription" },
            new MappingFieldItem { MappingTypeId = 20, IgcField = "long_description", GovernField = "LongDescription" },
            new MappingFieldItem { MappingTypeId = 20, IgcField = "qualityScore", GovernField = "QualityScore" },
            new MappingFieldItem { MappingTypeId = 20, IgcField = "custom_Catalog Status", GovernField = "CatalogStatus" },
            new MappingFieldItem { MappingTypeId = 20, IgcField = "custom_CDE", GovernField = "CDE" },
            new MappingFieldItem { MappingTypeId = 20, IgcField = "custom_Data Element Definition", GovernField = "DataElementDefinition" },
            new MappingFieldItem { MappingTypeId = 20, IgcField = "alias_(business_name)", GovernField = "Alias" },
            new MappingFieldItem { MappingTypeId = 20, IgcField = "reviewDate", GovernField = "ReviewDate" },
            new MappingFieldItem { MappingTypeId = 20, IgcField = "fieldCount", GovernField = "FieldCount" },
            new MappingFieldItem { MappingTypeId = 20, IgcField = "notes", GovernField = "Notes" },

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

        static List<MappingRelationItem> InputMappingRelationItems = new List<MappingRelationItem> {
            //new MappingRelationItem { MappingTypeId = 1, IgcField = "assigned_to_terms", GovernField = "" },
            //new MappingRelationItem { MappingTypeId = 1, IgcField = "implements_rules", GovernField = "" },
            //new MappingRelationItem { MappingTypeId = 1, IgcField = "governed_by_rules", GovernField = "" },
            new MappingRelationItem { MappingTypeId = 1, IgcField = "impacts_on", GovernPredicateType = 7 },

            new MappingRelationItem { MappingTypeId = 15, IgcField = "impacts_on", GovernPredicateType = 7 },
            
            new MappingRelationItem { MappingTypeId = 16, IgcField = "impacts_on", GovernPredicateType = 7 },

            new MappingRelationItem { MappingTypeId = 17, IgcField = "impacts_on", GovernPredicateType = 7 },

            new MappingRelationItem { MappingTypeId = 18, IgcField = "referenced_by_views", IsSubject = true, GovernPredicateType = 1 }
            new MappingRelationItem { MappingTypeId = 18, IgcField = "impacts_on", GovernPredicateType = 7 },
            new MappingRelationItem { MappingTypeId = 18, IgcField = "governed_by_rules", GovernPredicateType = 1 },

            new MappingRelationItem { MappingTypeId = 19, IgcField = "referenced_by_views", IsSubject = true, GovernPredicateType = 1 }
            new MappingRelationItem { MappingTypeId = 19, IgcField = "impacts_on", GovernPredicateType = 7 },
            new MappingRelationItem { MappingTypeId = 19, IgcField = "governed_by_rules", GovernPredicateType = 1 },

        };

        static List<MappingRoleItem> InputMappingRoleItems = new List<MappingRoleItem> {
            new MappingRoleItem { MappingTypeId = 1, GovernRoleName = "Business Owner", IgcIdField = "$BusinessOwnerId", IgcNameField = "$BusinessOwner" },
            new MappingRoleItem { MappingTypeId = 1, GovernRoleName = "Application Owner", IgcIdField = "$ApplicationOwnerId", IgcNameField = "$ApplicationOwner" },
            new MappingRoleItem { MappingTypeId = 1, GovernRoleName = "Data Steward", IgcIdField = "$DataStewardId", IgcNameField = "$DataSteward" },
            //new MappingRoleItem { MappingTypeId = 1, GovernRoleName = "", IgcNameField = "$DataOwner" },
            new MappingRoleItem { MappingTypeId = 1, GovernRoleName = "EDGM Steward", IgcIdField = "$EDGMStewardId" },

            new MappingRoleItem { MappingTypeId = 6, GovernRoleName = "Business Owner", IgcIdField = "$BusinessOwnerId", IgcNameField = "$BusinessOwner" },
            new MappingRoleItem { MappingTypeId = 7, GovernRoleName = "Business Owner", IgcIdField = "$BusinessOwnerId", IgcNameField = "$BusinessOwner" },
            new MappingRoleItem { MappingTypeId = 8, GovernRoleName = "Business Owner", IgcIdField = "$BusinessOwnerId", IgcNameField = "$BusinessOwner" },
            new MappingRoleItem { MappingTypeId = 9, GovernRoleName = "Business Owner", IgcIdField = "$BusinessOwnerId", IgcNameField = "$BusinessOwner" },
            new MappingRoleItem { MappingTypeId = 10, GovernRoleName = "Business Owner", IgcIdField = "$BusinessOwnerId", IgcNameField = "$BusinessOwner" },
            new MappingRoleItem { MappingTypeId = 11, GovernRoleName = "Business Owner", IgcIdField = "$BusinessOwnerId", IgcNameField = "$BusinessOwner" },
            new MappingRoleItem { MappingTypeId = 12, GovernRoleName = "Business Owner", IgcIdField = "$BusinessOwnerId", IgcNameField = "$BusinessOwner" },

            new MappingRoleItem { MappingTypeId = 14, GovernRoleName = "Data Steward", IgcIdField = "$DataStewardId", IgcNameField = "$DataSteward" },
            new MappingRoleItem { MappingTypeId = 14, GovernRoleName = "Owner", IgcIdField = "custom_Owner Id", IgcNameField = "custom_Owner" },

            new MappingRoleItem { MappingTypeId = 14, GovernRoleName = "Data Steward", IgcIdField = "custom_Data Steward Id", IgcNameField = "custom_Data Steward" },

            new MappingRoleItem { MappingTypeId = 16, GovernRoleName = "Data Steward", IgcIdField = "custom_Data Steward Id", IgcNameField = "custom_Data Steward" },
            new MappingRoleItem { MappingTypeId = 16, GovernRoleName = "Owner", IgcIdField = "custom_Owner Id", IgcNameField = "custom_Owner" },

            new MappingRoleItem { MappingTypeId = 17, GovernRoleName = "Business Owner", IgcIdField = "custom_Business Owner Id", IgcNameField = "custom_Business Owner" },
            new MappingRoleItem { MappingTypeId = 17, GovernRoleName = "Data Steward", IgcIdField = "custom_Data Steward Id", IgcNameField = "custom_Data Steward" },

            new MappingRoleItem { MappingTypeId = 18, GovernRoleName = "Data Steward", IgcIdField = "custom_Data Steward Id", IgcNameField = "custom_Data Steward" },
            new MappingRoleItem { MappingTypeId = 18, GovernRoleName = "Owner", IgcIdField = "custom_Owner Id", IgcNameField = "custom_Owner" },

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

                //LoadApplicationCatalog();

                //GetRrpFunctionalArea();
                //GetRrpLevel1();
                //GetRrpLevel2();
                //GetRrpLevel3();

                //GetBuLevel1();
                //GetBuLevel2();
                //GetBuLevel3();
                //GetBuLevel4();
                //GetBuLevel5();
                //GetBuLevel6();
                //GetBuLevel7();

                ////GetHosts();

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

        public static void LoadAssetsByMappingType(MappingType mappingType)
        {
            var fieldMappings = InputMappingFieldItems.Where(i => i.MappingTypeId == mappingType.Id).ToList();
            var relationMappings = InputMappingRelationItems.Where(i => i.MappingTypeId == mappingType.Id).ToList();
            var roleMappings = InputMappingRoleItems.Where(i => i.MappingTypeId == mappingType.Id).ToList();
            var url = $"{SourceUri}search/?pageSize=75&types={mappingType.IgcType}";

            // Add the properties we are after for this IGC type.
            url += string.Concat(fieldMappings.Select(i => $"&properties={i.IgcField}"));
            url += string.Concat(relationMappings.Select(i => $"&properties={i.IgcField}"));
            url += string.Concat(roleMappings.Where(i => !string.IsNullOrEmpty(i.IgcIdField)).Select(i => $"&properties={i.IgcIdField}"));
            url += string.Concat(roleMappings.Where(i => !string.IsNullOrEmpty(i.IgcNameField)).Select(i => $"&properties={i.IgcNameField}"));


            var igcData = new IgcDynamicArrayModels();
            var arr = new JArray();
            var relationships = new List<D3sRelationshipModel>();
            var ownershipTopModel = new D3sOwnershipItemsModel { UserIdFieldName = "UserID", Items = new List<D3sOwnershipModel>() };

            Func<JArray, JArray> parse = delegate (JArray root)
            {
                foreach (var obj in igcData.items.AsQueryable())
                {
                    var igcObjectSourceID = obj["_id"].Value<string>();

                    // Field Load Logic.
                    var d3s = new JObject();
                    fieldMappings.ForEach(f =>
                    {
                        if (f.ParentContextPosition.HasValue)
                        {
                            // There is a hierarchy here, and we need to resolve it.
                            var context = obj[f.IgcField].Cast<List<GenericIgcContextModel>>().FirstOrDefault();
                            if (context != null)
                            {
                                d3s.Add(f.GovernField, context[f.ParentContextPosition.Value]._id);
                            }
                        }
                        else
                        {
                            if (f.IsArray)
                            {
                                d3s.Add(f.GovernField, (obj[f.IgcField] != null) ? string.Join(", ", obj[f.IgcField]) : "");
                            }
                            else
                            {
                                d3s.Add(f.GovernField, obj[f.IgcField].Value<string>());
                            }
                        }

                    });
                    arr.Add(d3s);

                    // Relation Load Logic.
                    relationMappings.ForEach(r =>
                    {
                        var items = (
                                    from rm in obj[r.IgcField].AsQueryable().Cast<IgcRelationshipModel>()
                                    from i in rm.items
                                    select i
                                    ).ToList();

                        relationships.AddRange(
                            items.Select(i => new D3sRelationshipModel {
                                SubjectSourceID = r.IsSubject ? igcObjectSourceID : i.SourceID,
                                ObjectSourceID = r.IsSubject ? i.SourceID : igcObjectSourceID,
                                PredicateType = r.GovernPredicateType
                            })
                        );
                    });

                    // Role Load Logic.
                    roleMappings.ForEach(r => {
                        ownershipTopModel.Items.Add(new D3sOwnershipModel {
                            RoleName = r.GovernRoleName,
                            SourceID = igcObjectSourceID,
                            UserFullName = obj[r.IgcNameField].Value<string>(),
                            UserId = obj[r.IgcIdField].Value<string>()
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
                        $"{TargetUri}{mappingType.GovernType}/{mappingType.GovernTypeID}/bulk",
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
                    var respString = PostJsonToApi(
                        $"{TargetUri}ownership/bulk",
                        TargetAuthString,
                        JsonConvert.SerializeObject(ownershipTopModel)
                    );
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

        public static void LoadApplicationCatalog()
        {
            var properties = "short_description,long_description,labels,stewards,assigned_to_terms,implements_rules,governed_by_rules,$CMDBAppCode,$ApplicationAlias,$BusinessOwner,$BusinessOwnerId,$ApplicationOwner,$ApplicationOwnerId,$DataSteward,$DataStewardId,$DataOwner,$EDGMStewardId,$Comments,$SSID,$KeyApplicationType,$Status,$DataLocation,$PersonalData,$ComponentType,$ComponentCode,$ComponentSAID,$AuthoritativeSource,$MaturityLevel,$BookOfRecord,impacts_on".Split(',');
            var url = $"{SourceUri}search/?pageSize=75&types=$ApplicationCatalog-ApplicationCatalog";
            foreach (var p in properties)
            {
                url += $"&properties={p}";
            }

            var arr = new List<D3sApplicationCatalogModel>();
            var d3sImpactRelationships = new List<D3sRelationshipModel>();
            var ownershipTopModel = new D3sOwnershipItemsModel { UserIdFieldName = "UserID", Items = new List<D3sOwnershipModel>() };

            Func<IgcApplicationCatalogModels, IgcApplicationCatalogModels> parse = delegate (IgcApplicationCatalogModels root)
            {
                arr.AddRange(root.items.ConvertAll<D3sApplicationCatalogModel>(i => new D3sApplicationCatalogModel
                {
                    SourceID = i.SourceID,
                    Name = i.Name,
                    ShortDescription = i.ShortDescription,
                    ApplicationAlias = i.ApplicationAlias,
                    AuthoritativeSource = i.AuthoritativeSource,
                    Host = i.BookOfRecord,
                    CMDBAppCode = i.CMDBAppCode,
                    Comments = i.Comments,
                    KeyApplicationTypeText = (i.KeyApplicationType != null) ? string.Join(", ", i.KeyApplicationType) : "",
                    ComponentSAID = i.ComponentSAID,
                    ComponentType = i.ComponentType,
                    DataLocation = i.DataLocation,
                    LongDescription = i.LongDescription,
                    MaturityLevel = i.MaturityLevel,
                    PersonalData = i.PersonalData ?? "No",
                    SSID = i.SSID,
                    Status = i.Status
                }));

                ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
                {
                    SourceID = i.SourceID,
                    RoleName = "Business Owner",
                    UserId = i.BusinessOwnerId,
                    UserFullName = i.BusinessOwner
                }));

                ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
                {
                    SourceID = i.SourceID,
                    RoleName = "Application Owner",
                    UserId = i.ApplicationOwnerId,
                    UserFullName = i.ApplicationOwner
                }));

                ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
                {
                    SourceID = i.SourceID,
                    RoleName = "Data Owner",
                    UserId = string.Empty,
                    UserFullName = i.DataOwner
                }));

                ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
                {
                    SourceID = i.SourceID,
                    RoleName = "Data Steward",
                    UserId = i.DataStewardId,
                    UserFullName = i.DataSteward
                }));

                ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
                {
                    SourceID = i.SourceID,
                    RoleName = "EDGM Steward",
                    UserId = i.EDGMStewardId,
                    UserFullName = string.Empty
                }));

                foreach (var app in root.items)
                {
                    app.ImpactsOn.items.ForEach(bu =>
                    {
                        d3sImpactRelationships.Add(
                            new D3sRelationshipModel { SubjectSourceID = bu.SourceID, ObjectSourceID = app.SourceID, PredicateType = 7 }
                        );
                    });
                }

                return root;
            };

            while (!string.IsNullOrEmpty(url))
            {
                try
                {
                    var models = GetFromApi<IgcApplicationCatalogModels>(url, SourceAuthString);
                    if (models != null)
                    {
                        parse(models);
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
                        $"{TargetUri}ArtifactType/2/bulk",
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
                    var respString = PostJsonToApi(
                        $"{TargetUri}ownership/bulk",
                        TargetAuthString,
                        JsonConvert.SerializeObject(ownershipTopModel)
                    );
                }
                catch (Exception ex)
                {
                    CoreFunction.AITrackException(functionName, ex);
                }
            }

            if (d3sImpactRelationships.Count > 0)
            {
                try
                {
                    var respString = PostJsonToApi(
                        $"{TargetUri}relationships/bulk",
                        TargetAuthString,
                        JsonConvert.SerializeObject(d3sImpactRelationships)
                    );
                }
                catch (Exception ex)
                {
                    CoreFunction.AITrackException(functionName, ex);
                }
            }
        }

        #endregion

        #region Fusion

        //public static void GetHosts()
        //{
        //    var url = buildSearchUri("host", new List<string> {
        //        "short_description",
        //        "long_description",
        //        "labels",
        //        "stewards",
        //        //"assigned_to_terms",
        //        //"implements_rules",
        //        //"governed_by_rules",
        //        //"databases",
        //        //"data_files",
        //        //"idoc_types",
        //        //"transformation_projects"
        //        //"data_connections",
        //        //"amazon_s3_buckets",
        //        //"data_file_folders",
        //        "location",
        //        "network_node",
        //        //"imported_from",
        //        //"in_colleections",
        //        "notes"
        //    });

        //    var arr = new List<dynamic>();
        //    //var d3sImpactRelationships = new List<D3sBusinesUnitApplicationCatalogRelationshipModel>();
        //    //var ownershipTopModel = new D3sOwnershipItemsModel { UserIdFieldName = "UserID", Items = new List<D3sOwnershipModel>() };

        //    Func<IgcDynamicModels, IgcDynamicModels> parse = delegate (IgcDynamicModels root)
        //    {
        //        arr.AddRange(root.items.ConvertAll<dynamic>(i => new
        //        {
        //            SourceID = i._id,
        //            Name = i._name,
        //            ShortDescription = i.short_description,
        //            LongDescription = i.long_description,
        //            Location = i.location,
        //            NetworkNode = i.network_node,
        //            Notes = i.notes
        //        }));

        //        //ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
        //        //{
        //        //    SourceID = i.SourceID,
        //        //    RoleName = "Business Owner",
        //        //    UserId = i.BusinessOwnerId,
        //        //    UserFullName = i.BusinessOwner
        //        //}));

        //        //ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
        //        //{
        //        //    SourceID = i.SourceID,
        //        //    RoleName = "Application Owner",
        //        //    UserId = i.ApplicationOwnerId,
        //        //    UserFullName = i.ApplicationOwner
        //        //}));

        //        //ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
        //        //{
        //        //    SourceID = i.SourceID,
        //        //    RoleName = "Data Owner",
        //        //    UserId = string.Empty,
        //        //    UserFullName = i.DataOwner
        //        //}));

        //        //ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
        //        //{
        //        //    SourceID = i.SourceID,
        //        //    RoleName = "Data Steward",
        //        //    UserId = i.DataStewardId,
        //        //    UserFullName = i.DataSteward
        //        //}));

        //        //ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
        //        //{
        //        //    SourceID = i.SourceID,
        //        //    RoleName = "EDGM Steward",
        //        //    UserId = i.EDGMStewardId,
        //        //    UserFullName = string.Empty
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
        //            var models = GetFromApi<IgcDynamicModels>(url, SourceAuthString);
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
        //        //var respString = PostJsonToApi(
        //        //    $"{TargetUri}ArtifactType/2/bulk",
        //        //    TargetAuthString,
        //        //    JsonConvert.SerializeObject(arr)
        //        //);
        //    }

        //    //// If any owners to send to server.
        //    //if (ownershipTopModel.Items.Count > 0)
        //    //{
        //    //    var uniqueUsers = ownershipTopModel.Items
        //    //        .Where(i => !string.IsNullOrEmpty(i.UserFullName) && !string.IsNullOrEmpty(i.UserId))
        //    //        .Select(i => new { i.UserFullName, i.UserId })
        //    //        .Distinct()
        //    //        .ToList();

        //    //    // Populate the UserIDs that are missing, based can be looked up by user's full name. TODO: Confirm this logic, as it may not be correct if two or more user's have the same name.
        //    //    foreach (var item in ownershipTopModel.Items.Where(i => string.IsNullOrEmpty(i.UserId)))
        //    //    {
        //    //        var match = uniqueUsers.FirstOrDefault(i => i.UserFullName == item.UserFullName);
        //    //        if (match != null)
        //    //        {
        //    //            item.UserId = match.UserId;
        //    //        }
        //    //    }

        //    //    //Now, remove any users whose internal ID cannot be resolved.
        //    //    ownershipTopModel.Items.RemoveAll(i => string.IsNullOrEmpty(i.UserId));

        //    //    var respString = PostJsonToApi(
        //    //        $"{TargetUri}ownership/bulk",
        //    //        TargetAuthString,
        //    //        JsonConvert.SerializeObject(ownershipTopModel)
        //    //    );
        //    //}

        //    //if (d3sImpactRelationships.Count > 0)
        //    //{
        //    //    //var respString = PostJsonToApi(
        //    //    //    $"{TargetUri}relationships/bulk",
        //    //    //    TargetAuthString,
        //    //    //    JsonConvert.SerializeObject(d3sImpactRelationships)
        //    //    //);
        //    //}
        //}


        //public static void GetDataFiles()
        //{
        //    var url = buildSearchUri("data_file", new List<string> {
        //            "short_description",
        //            "long_description",
        //            "parent_folder",
        //            "host",
        //            "labels",
        //            "stewards",
        //            //"assigned_to_terms",
        //            //"implements_rules",
        //            "governed_by_rules",
        //            "data_file_records",
        //            //"implements_data_file_definition",
        //            //"implements_physical_models",
        //            "custom_Catalog Status",
        //            "custom_Classification",
        //            //"custom_Comments",
        //            //"custom_Created By",
        //            //"custom_Data Steward",
        //            //"custom_Data Steward Id",
        //            //"custom_Frequency",
        //            "custom_Classification",//"custom_Information Classification",
        //            //"custom_Modified By",
        //            "custom_Output Format",
        //            //"custom_Owner",
        //            //"custom_Owner Id",
        //            "custom_Status",
        //            "alias_(business_name)",
        //            "path",
        //            //"store_type",
        //            "imported_from",
        //            //"impacted_by",
        //            //"impacts_on",
        //            "include_for_business_lineage",
        //            "suggested_term_assignments",
        //            "notes",
        //            "amazon_s3_data_files",
        //            "implements_data_file_definition",
        //            "implements_physical_models"
        //        });

        //    var arr = new List<dynamic>();
        //    //var d3sImpactRelationships = new List<D3sBusinesUnitApplicationCatalogRelationshipModel>();
        //    var ownershipTopModel = new D3sOwnershipItemsModel { UserIdFieldName = "UserID", Items = new List<D3sOwnershipModel>() };

        //    Func<IgcDataFileModels, IgcDataFileModels> parse = delegate (IgcDataFileModels root)
        //    {
        //        arr.AddRange(root.items.ConvertAll<dynamic>(i => new
        //        {
        //            SourceID = i.SourceID,
        //            Name = i.Name,
        //            ShortDescription = i.ShortDescription,
        //            LongDescription = i.LongDescription,
        //            Classification = i.Classification,
        //            Location = i.Location,
        //            Notes = i.Notes
        //        }));

        //        //ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
        //        //{
        //        //    SourceID = i.SourceID,
        //        //    RoleName = "Business Owner",
        //        //    UserId = i.BusinessOwnerId,
        //        //    UserFullName = i.BusinessOwner
        //        //}));

        //        //ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
        //        //{
        //        //    SourceID = i.SourceID,
        //        //    RoleName = "Application Owner",
        //        //    UserId = i.ApplicationOwnerId,
        //        //    UserFullName = i.ApplicationOwner
        //        //}));

        //        //ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
        //        //{
        //        //    SourceID = i.SourceID,
        //        //    RoleName = "Data Owner",
        //        //    UserId = string.Empty,
        //        //    UserFullName = i.DataOwner
        //        //}));

        //        ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
        //        {
        //            SourceID = i.SourceID,
        //            RoleName = "Data Steward",
        //            UserId = i.DataStewardId,
        //            UserFullName = i.DataSteward
        //        }));

        //        //ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
        //        //{
        //        //    SourceID = i.SourceID,
        //        //    RoleName = "EDGM Steward",
        //        //    UserId = i.EDGMStewardId,
        //        //    UserFullName = string.Empty
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
        //            var models = GetFromApi<IgcDataFileModels>(url, SourceAuthString);
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
        //        //var respString = PostJsonToApi(
        //        //    $"{TargetUri}ArtifactType/2/bulk",
        //        //    TargetAuthString,
        //        //    JsonConvert.SerializeObject(arr)
        //        //);
        //    }

        //    //// If any owners to send to server.
        //    //if (ownershipTopModel.Items.Count > 0)
        //    //{
        //    //    var uniqueUsers = ownershipTopModel.Items
        //    //        .Where(i => !string.IsNullOrEmpty(i.UserFullName) && !string.IsNullOrEmpty(i.UserId))
        //    //        .Select(i => new { i.UserFullName, i.UserId })
        //    //        .Distinct()
        //    //        .ToList();

        //    //    // Populate the UserIDs that are missing, based can be looked up by user's full name. TODO: Confirm this logic, as it may not be correct if two or more user's have the same name.
        //    //    foreach (var item in ownershipTopModel.Items.Where(i => string.IsNullOrEmpty(i.UserId)))
        //    //    {
        //    //        var match = uniqueUsers.FirstOrDefault(i => i.UserFullName == item.UserFullName);
        //    //        if (match != null)
        //    //        {
        //    //            item.UserId = match.UserId;
        //    //        }
        //    //    }

        //    //    //Now, remove any users whose internal ID cannot be resolved.
        //    //    ownershipTopModel.Items.RemoveAll(i => string.IsNullOrEmpty(i.UserId));

        //    //    var respString = PostJsonToApi(
        //    //        $"{TargetUri}ownership/bulk",
        //    //        TargetAuthString,
        //    //        JsonConvert.SerializeObject(ownershipTopModel)
        //    //    );
        //    //}

        //    //if (d3sImpactRelationships.Count > 0)
        //    //{
        //    //    //var respString = PostJsonToApi(
        //    //    //    $"{TargetUri}relationships/bulk",
        //    //    //    TargetAuthString,
        //    //    //    JsonConvert.SerializeObject(d3sImpactRelationships)
        //    //    //);
        //    //}
        //}
        
        #endregion

        #region RRP

        public static void GetRrpFunctionalArea()
        {
            var properties = "short_description,long_description".Split(',');
            var url = $"{SourceUri}search/?pageSize=75&types=$RRP-RRPFunctionalArea";
            foreach (var p in properties)
            {
                url += $"&properties={p}";
            }

            var arr = new List<D3sRrpFunctionalAreaModel>();

            Func<IgcRrpFunctionalAreaModels, IgcRrpFunctionalAreaModels> parse = delegate (IgcRrpFunctionalAreaModels root)
            {
                arr.AddRange(root.items.ConvertAll<D3sRrpFunctionalAreaModel>(i => new D3sRrpFunctionalAreaModel
                {
                    SourceID = i.SourceID,
                    Name = i.Name,
                    ShortDescription = i.ShortDescription,
                    LongDescription = i.LongDescription
                }));

                return root;
            };

            while (!string.IsNullOrEmpty(url))
            {
                try
                {
                    var models = GetFromApi<IgcRrpFunctionalAreaModels>(url, SourceAuthString);
                    if (models != null)
                    {
                        parse(models);
                        url = models.paging.next;
                    }
                }
                catch (Exception ex)
                {
                    CoreFunction.AITrackException(functionName, ex);
                    url = null;
                }
            }

            try
            {
                var respString = PostJsonToApi(
                    $"{TargetUri}TaxonomyType/3/bulk",
                    TargetAuthString,
                    JsonConvert.SerializeObject(arr)
                );
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex);
            }
        }

        public static void GetRrpLevel1()
        {
            var properties = "short_description,long_description".Split(',');
            var url = $"{SourceUri}search/?pageSize=75&types=$RRP-RRPLevel1Service";
            foreach (var p in properties)
            {
                url += $"&properties={p}";
            }

            var arr = new List<D3sRrpLevelOneModel>();

            Func<IgcRrpLevelOneModels, IgcRrpLevelOneModels> parse = delegate (IgcRrpLevelOneModels root)
            {
                arr.AddRange(root.items.ConvertAll<D3sRrpLevelOneModel>(i => new D3sRrpLevelOneModel
                {
                    SourceID = i.SourceID,
                    ParentSourceID = i._context[0]._id,
                    Name = i.Name,
                    ShortDescription = i.ShortDescription,
                    LongDescription = i.LongDescription
                }));

                //ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
                //{
                //    SourceID = i.SourceID,
                //    RoleName = "Business Owner",
                //    UserId = i.BusinessOwnerId,
                //    UserFullName = i.BusinessOwner
                //}));

                //foreach (var app in root.items)
                //{
                //    app.ImpactsOn.items.ForEach(bu =>
                //    {
                //        d3sImpactRelationships.Add(
                //            new D3sBusinesUnitApplicationCatalogRelationshipModel { SubjectSourceID = bu.SourceID, ObjectSourceID = app.SourceID, PredicateType = 7 }
                //        );
                //    });
                //}

                return root;
            };

            while (!string.IsNullOrEmpty(url))
            {
                try
                {
                    var models = GetFromApi<IgcRrpLevelOneModels>(url, SourceAuthString);
                    if (models != null)
                    {
                        parse(models);
                        url = models.paging.next;
                    }
                }
                catch (Exception ex)
                {
                    CoreFunction.AITrackException(functionName, ex);
                    url = null;
                }
            }

            try
            {
                var respString = PostJsonToApi(
                    $"{TargetUri}TaxonomyType/3/bulk",
                    TargetAuthString,
                    JsonConvert.SerializeObject(arr)
                );
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex);
            }
        }

        public static void GetRrpLevel2()
        {
            var properties = "short_description,long_description".Split(',');
            var url = $"{SourceUri}search/?pageSize=75&types=$RRP-RRPLevel2Service";
            foreach (var p in properties)
            {
                url += $"&properties={p}";
            }

            var arr = new List<D3sRrpLevelTwoModel>();

            Func<IgcRrpLevelTwoModels, IgcRrpLevelTwoModels> parse = delegate (IgcRrpLevelTwoModels root)
            {
                arr.AddRange(root.items.ConvertAll<D3sRrpLevelTwoModel>(i => new D3sRrpLevelTwoModel
                {
                    SourceID = i.SourceID,
                    ParentSourceID = i._context[1]._id,
                    Name = i.Name,
                    ShortDescription = i.ShortDescription,
                    LongDescription = i.LongDescription
                }));

                //ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
                //{
                //    SourceID = i.SourceID,
                //    RoleName = "Business Owner",
                //    UserId = i.BusinessOwnerId,
                //    UserFullName = i.BusinessOwner
                //}));

                //foreach (var app in root.items)
                //{
                //    app.ImpactsOn.items.ForEach(bu =>
                //    {
                //        d3sImpactRelationships.Add(
                //            new D3sBusinesUnitApplicationCatalogRelationshipModel { SubjectSourceID = bu.SourceID, ObjectSourceID = app.SourceID, PredicateType = 7 }
                //        );
                //    });
                //}

                return root;
            };

            while (!string.IsNullOrEmpty(url))
            {
                try
                {
                    var models = GetFromApi<IgcRrpLevelTwoModels>(url, SourceAuthString);
                    if (models != null)
                    {
                        parse(models);
                        url = models.paging.next;
                    }
                }
                catch (Exception ex)
                {
                    CoreFunction.AITrackException(functionName, ex);
                    url = null;
                }
            }

            try
            {
                var respString = PostJsonToApi(
                    $"{TargetUri}TaxonomyType/3/bulk",
                    TargetAuthString,
                    JsonConvert.SerializeObject(arr)
                );
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex);
            }
        }

        public static void GetRrpLevel3()
        {
            var properties = "short_description,long_description".Split(',');
            var url = $"{SourceUri}search/?pageSize=75&types=$RRP-RRPLevel3Service";
            foreach (var p in properties)
            {
                url += $"&properties={p}";
            }

            var arr = new List<D3sRrpLevelThreeModel>();

            Func<IgcRrpLevelThreeModels, IgcRrpLevelThreeModels> parse = delegate (IgcRrpLevelThreeModels root)
            {
                arr.AddRange(root.items.ConvertAll<D3sRrpLevelThreeModel>(i => new D3sRrpLevelThreeModel
                {
                    SourceID = i.SourceID,
                    ParentSourceID = i._context[2]._id,
                    Name = i.Name,
                    ShortDescription = i.ShortDescription,
                    LongDescription = i.LongDescription
                }));

                //ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
                //{
                //    SourceID = i.SourceID,
                //    RoleName = "Business Owner",
                //    UserId = i.BusinessOwnerId,
                //    UserFullName = i.BusinessOwner
                //}));

                //foreach (var app in root.items)
                //{
                //    app.ImpactsOn.items.ForEach(bu =>
                //    {
                //        d3sImpactRelationships.Add(
                //            new D3sBusinesUnitApplicationCatalogRelationshipModel { SubjectSourceID = bu.SourceID, ObjectSourceID = app.SourceID, PredicateType = 7 }
                //        );
                //    });
                //}

                return root;
            };

            while (!string.IsNullOrEmpty(url))
            {
                try
                {
                    var models = GetFromApi<IgcRrpLevelThreeModels>(url, SourceAuthString);
                    if (models != null)
                    {
                        parse(models);
                        url = models.paging.next;
                    }
                }
                catch (Exception ex)
                {
                    CoreFunction.AITrackException(functionName, ex);
                    url = null;
                }
            }

            try
            {
                var respString = PostJsonToApi(
                    $"{TargetUri}TaxonomyType/3/bulk",
                    TargetAuthString,
                    JsonConvert.SerializeObject(arr)
                );
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex);
            }
        }

        #endregion

        #region Business Unit

        public static void GetBuLevel1()
        {
            var properties = "short_description,long_description,$BusinessUnitId,$BusinessOwnerId,$BusinessOwner".Split(',');
            var url = $"{SourceUri}search/?pageSize=75&types=$BUOrg-BusinessUnitLevel1";
            foreach (var p in properties)
            {
                url += $"&properties={p}";
            }

            var arr = new List<D3sBuTopModel>();

            Func<IgcBuTopModels, IgcBuTopModels> parse = delegate (IgcBuTopModels root)
            {
                arr.AddRange(root.items.ConvertAll<D3sBuTopModel>(i => new D3sBuTopModel
                {
                    SourceID = i.SourceID,
                    Name = i.Name,
                    ShortDescription = i.ShortDescription,
                    LongDescription = i.LongDescription,
                    BusinessOwner = i.BusinessOwner,
                    BusinessOwnerId = i.BusinessOwnerId,
                    BusinessUnitID = i.BusinessUnitId
                }));

                //ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
                //{
                //    SourceID = i.SourceID,
                //    RoleName = "Business Owner",
                //    UserId = i.BusinessOwnerId,
                //    UserFullName = i.BusinessOwner
                //}));

                //foreach (var app in root.items)
                //{
                //    app.ImpactsOn.items.ForEach(bu =>
                //    {
                //        d3sImpactRelationships.Add(
                //            new D3sBusinesUnitApplicationCatalogRelationshipModel { SubjectSourceID = bu.SourceID, ObjectSourceID = app.SourceID, PredicateType = 7 }
                //        );
                //    });
                //}

                return root;
            };

            while (!string.IsNullOrEmpty(url))
            {
                try
                {
                    var models = GetFromApi<IgcBuTopModels>(url, SourceAuthString);
                    if (models != null)
                    {
                        parse(models);
                        url = models.paging.next;
                    }
                }
                catch (Exception ex)
                {
                    CoreFunction.AITrackException(functionName, ex);
                    url = null;
                }
            }

            try
            {
                var respString = PostJsonToApi(
                    $"{TargetUri}TaxonomyType/7/bulk",
                    TargetAuthString,
                    JsonConvert.SerializeObject(arr)
                );
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex);
            }

        }

        public static void GetBuLevel2()
        {
            var properties = "short_description,long_description,$BusinessUnitId,$BusinessOwnerId,$BusinessOwner".Split(',');
            var url = $"{SourceUri}search/?pageSize=75&types=$BUOrg-BusinessUnitLevel2";
            foreach (var p in properties)
            {
                url += $"&properties={p}";
            }

            var arr = new List<D3sBuChildModel>();

            Func<IgcBuChildModels, IgcBuChildModels> parse = delegate (IgcBuChildModels root)
            {
                arr.AddRange(root.items.ConvertAll<D3sBuChildModel>(i => new D3sBuChildModel
                {
                    SourceID = i.SourceID,
                    ParentSourceID = i._context[0]._id,
                    Name = i.Name,
                    ShortDescription = i.ShortDescription,
                    LongDescription = i.LongDescription,
                    BusinessOwner = i.BusinessOwner,
                    BusinessOwnerId = i.BusinessOwnerId,
                    BusinessUnitID = i.BusinessUnitId
                }));

                //ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
                //{
                //    SourceID = i.SourceID,
                //    RoleName = "Business Owner",
                //    UserId = i.BusinessOwnerId,
                //    UserFullName = i.BusinessOwner
                //}));

                //foreach (var app in root.items)
                //{
                //    app.ImpactsOn.items.ForEach(bu =>
                //    {
                //        d3sImpactRelationships.Add(
                //            new D3sBusinesUnitApplicationCatalogRelationshipModel { SubjectSourceID = bu.SourceID, ObjectSourceID = app.SourceID, PredicateType = 7 }
                //        );
                //    });
                //}

                return root;
            };

            while (!string.IsNullOrEmpty(url))
            {
                try
                {
                    var models = GetFromApi<IgcBuChildModels>(url, SourceAuthString);
                    if (models != null)
                    {
                        parse(models);
                        url = models.paging.next;
                    }
                }
                catch (Exception ex)
                {
                    CoreFunction.AITrackException(functionName, ex);
                    url = null;
                }
            }

            try
            {
                var respString = PostJsonToApi(
                    $"{TargetUri}TaxonomyType/7/bulk",
                    TargetAuthString,
                    JsonConvert.SerializeObject(arr)
                );
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex);
            }
        }

        public static void GetBuLevel3()
        {
            var properties = "short_description,long_description,$BusinessUnitId,$BusinessOwnerId,$BusinessOwner".Split(',');
            var url = $"{SourceUri}search/?pageSize=75&types=$BUOrg-BusinessUnitLevel3";
            foreach (var p in properties)
            {
                url += $"&properties={p}";
            }

            var arr = new List<D3sBuChildModel>();

            Func<IgcBuChildModels, IgcBuChildModels> parse = delegate (IgcBuChildModels root)
            {
                arr.AddRange(root.items.ConvertAll<D3sBuChildModel>(i => new D3sBuChildModel
                {
                    SourceID = i.SourceID,
                    ParentSourceID = i._context[1]._id,
                    Name = i.Name,
                    ShortDescription = i.ShortDescription,
                    LongDescription = i.LongDescription,
                    BusinessOwner = i.BusinessOwner,
                    BusinessOwnerId = i.BusinessOwnerId,
                    BusinessUnitID = i.BusinessUnitId
                }));

                //ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
                //{
                //    SourceID = i.SourceID,
                //    RoleName = "Business Owner",
                //    UserId = i.BusinessOwnerId,
                //    UserFullName = i.BusinessOwner
                //}));

                //foreach (var app in root.items)
                //{
                //    app.ImpactsOn.items.ForEach(bu =>
                //    {
                //        d3sImpactRelationships.Add(
                //            new D3sBusinesUnitApplicationCatalogRelationshipModel { SubjectSourceID = bu.SourceID, ObjectSourceID = app.SourceID, PredicateType = 7 }
                //        );
                //    });
                //}

                return root;
            };

            while (!string.IsNullOrEmpty(url))
            {
                try
                {
                    var models = GetFromApi<IgcBuChildModels>(url, SourceAuthString);
                    if (models != null)
                    {
                        parse(models);
                        url = models.paging.next;
                    }
                }
                catch (Exception ex)
                {
                    CoreFunction.AITrackException(functionName, ex);
                    url = null;
                }
            }

            try
            {
            var respString = PostJsonToApi(
                $"{TargetUri}TaxonomyType/7/bulk",
                TargetAuthString,
                JsonConvert.SerializeObject(arr)
            );
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex);
            }
        }

        public static void GetBuLevel4()
        {
            var properties = "short_description,long_description,$BusinessUnitId,$BusinessOwnerId,$BusinessOwner".Split(',');
            var url = $"{SourceUri}search/?pageSize=75&types=$BUOrg-BusinessUnitLevel4";
            foreach (var p in properties)
            {
                url += $"&properties={p}";
            }

            var arr = new List<D3sBuChildModel>();

            Func<IgcBuChildModels, IgcBuChildModels> parse = delegate (IgcBuChildModels root)
            {
                arr.AddRange(root.items.ConvertAll<D3sBuChildModel>(i => new D3sBuChildModel
                {
                    SourceID = i.SourceID,
                    ParentSourceID = i._context[2]._id,
                    Name = i.Name,
                    ShortDescription = i.ShortDescription,
                    LongDescription = i.LongDescription,
                    BusinessOwner = i.BusinessOwner,
                    BusinessOwnerId = i.BusinessOwnerId,
                    BusinessUnitID = i.BusinessUnitId
                }));

                //ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
                //{
                //    SourceID = i.SourceID,
                //    RoleName = "Business Owner",
                //    UserId = i.BusinessOwnerId,
                //    UserFullName = i.BusinessOwner
                //}));

                //foreach (var app in root.items)
                //{
                //    app.ImpactsOn.items.ForEach(bu =>
                //    {
                //        d3sImpactRelationships.Add(
                //            new D3sBusinesUnitApplicationCatalogRelationshipModel { SubjectSourceID = bu.SourceID, ObjectSourceID = app.SourceID, PredicateType = 7 }
                //        );
                //    });
                //}

                return root;
            };

            while (!string.IsNullOrEmpty(url))
            {
                try
                {
                    var models = GetFromApi<IgcBuChildModels>(url, SourceAuthString);
                    if (models != null)
                    {
                        parse(models);
                        url = models.paging.next;
                    }
                }
                catch (Exception ex)
                {
                    CoreFunction.AITrackException(functionName, ex);
                    url = null;
                }
            }

            try
            {
                var respString = PostJsonToApi(
                    $"{TargetUri}TaxonomyType/7/bulk",
                    TargetAuthString,
                    JsonConvert.SerializeObject(arr)
                );
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex);
            }
        }

        public static void GetBuLevel5()
        {
            var properties = "short_description,long_description,$BusinessUnitId,$BusinessOwnerId,$BusinessOwner".Split(',');
            var url = $"{SourceUri}search/?pageSize=75&types=$BUOrg-BusinessUnitLevel5";
            foreach (var p in properties)
            {
                url += $"&properties={p}";
            }

            var arr = new List<D3sBuChildModel>();

            Func<IgcBuChildModels, IgcBuChildModels> parse = delegate (IgcBuChildModels root)
            {
                arr.AddRange(root.items.ConvertAll<D3sBuChildModel>(i => new D3sBuChildModel
                {
                    SourceID = i.SourceID,
                    ParentSourceID = i._context[3]._id,
                    Name = i.Name,
                    ShortDescription = i.ShortDescription,
                    LongDescription = i.LongDescription,
                    BusinessOwner = i.BusinessOwner,
                    BusinessOwnerId = i.BusinessOwnerId,
                    BusinessUnitID = i.BusinessUnitId
                }));

                //ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
                //{
                //    SourceID = i.SourceID,
                //    RoleName = "Business Owner",
                //    UserId = i.BusinessOwnerId,
                //    UserFullName = i.BusinessOwner
                //}));

                //foreach (var app in root.items)
                //{
                //    app.ImpactsOn.items.ForEach(bu =>
                //    {
                //        d3sImpactRelationships.Add(
                //            new D3sBusinesUnitApplicationCatalogRelationshipModel { SubjectSourceID = bu.SourceID, ObjectSourceID = app.SourceID, PredicateType = 7 }
                //        );
                //    });
                //}

                return root;
            };

            while (!string.IsNullOrEmpty(url))
            {
                try
                {
                    var models = GetFromApi<IgcBuChildModels>(url, SourceAuthString);
                    if (models != null)
                    {
                        parse(models);
                        url = models.paging.next;
                    }
                }
                catch (Exception ex)
                {
                    CoreFunction.AITrackException(functionName, ex);
                    url = null;
                }
            }

            try
            {
                var respString = PostJsonToApi(
                    $"{TargetUri}TaxonomyType/7/bulk",
                    TargetAuthString,
                    JsonConvert.SerializeObject(arr)
                );
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex);
            }
        }

        public static void GetBuLevel6()
        {
            var properties = "short_description,long_description,$BusinessUnitId,$BusinessOwnerId,$BusinessOwner".Split(',');
            var url = $"{SourceUri}search/?pageSize=75&types=$BUOrg-BusinessUnitLevel6";
            foreach (var p in properties)
            {
                url += $"&properties={p}";
            }

            var arr = new List<D3sBuChildModel>();

            Func<IgcBuChildModels, IgcBuChildModels> parse = delegate (IgcBuChildModels root)
            {
                arr.AddRange(root.items.ConvertAll<D3sBuChildModel>(i => new D3sBuChildModel
                {
                    SourceID = i.SourceID,
                    ParentSourceID = i._context[4]._id,
                    Name = i.Name,
                    ShortDescription = i.ShortDescription,
                    LongDescription = i.LongDescription,
                    BusinessOwner = i.BusinessOwner,
                    BusinessOwnerId = i.BusinessOwnerId,
                    BusinessUnitID = i.BusinessUnitId
                }));

                //ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
                //{
                //    SourceID = i.SourceID,
                //    RoleName = "Business Owner",
                //    UserId = i.BusinessOwnerId,
                //    UserFullName = i.BusinessOwner
                //}));

                //foreach (var app in root.items)
                //{
                //    app.ImpactsOn.items.ForEach(bu =>
                //    {
                //        d3sImpactRelationships.Add(
                //            new D3sBusinesUnitApplicationCatalogRelationshipModel { SubjectSourceID = bu.SourceID, ObjectSourceID = app.SourceID, PredicateType = 7 }
                //        );
                //    });
                //}

                return root;
            };

            while (!string.IsNullOrEmpty(url))
            {
                try
                {
                    var models = GetFromApi<IgcBuChildModels>(url, SourceAuthString);
                    if (models != null)
                    {
                        parse(models);
                        url = models.paging.next;
                    }
                }
                catch (Exception ex)
                {
                    CoreFunction.AITrackException(functionName, ex);
                    url = null;
                }
            }

            try
            {
                var respString = PostJsonToApi(
                    $"{TargetUri}TaxonomyType/7/bulk",
                    TargetAuthString,
                    JsonConvert.SerializeObject(arr)
                );
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex);
            }
        }

        public static void GetBuLevel7()
        {
            var properties = "short_description,long_description,$BusinessUnitId,$BusinessOwnerId,$BusinessOwner".Split(',');
            var url = $"{SourceUri}search/?pageSize=75&types=$BUOrg-BusinessUnitLevel7";
            foreach (var p in properties)
            {
                url += $"&properties={p}";
            }

            var arr = new List<D3sBuChildModel>();

            Func<IgcBuChildModels, IgcBuChildModels> parse = delegate (IgcBuChildModels root)
            {
                arr.AddRange(root.items.ConvertAll<D3sBuChildModel>(i => new D3sBuChildModel
                {
                    SourceID = i.SourceID,
                    ParentSourceID = i._context[5]._id,
                    Name = i.Name,
                    ShortDescription = i.ShortDescription,
                    LongDescription = i.LongDescription,
                    BusinessOwner = i.BusinessOwner,
                    BusinessOwnerId = i.BusinessOwnerId,
                    BusinessUnitID = i.BusinessUnitId
                }));

                //ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
                //{
                //    SourceID = i.SourceID,
                //    RoleName = "Business Owner",
                //    UserId = i.BusinessOwnerId,
                //    UserFullName = i.BusinessOwner
                //}));

                //foreach (var app in root.items)
                //{
                //    app.ImpactsOn.items.ForEach(bu =>
                //    {
                //        d3sImpactRelationships.Add(
                //            new D3sBusinesUnitApplicationCatalogRelationshipModel { SubjectSourceID = bu.SourceID, ObjectSourceID = app.SourceID, PredicateType = 7 }
                //        );
                //    });
                //}

                return root;
            };

            while (!string.IsNullOrEmpty(url))
            {
                try
                {
                    var models = GetFromApi<IgcBuChildModels>(url, SourceAuthString);
                    if (models != null)
                    {
                        parse(models);
                        url = models.paging.next;
                    }
                }
                catch (Exception ex)
                {
                    CoreFunction.AITrackException(functionName, ex);
                    url = null;
                }
            }

            try
            {
                var respString = PostJsonToApi(
                    $"{TargetUri}TaxonomyType/7/bulk",
                    TargetAuthString,
                    JsonConvert.SerializeObject(arr)
                );
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex);
            }
        }

        #endregion
    }
}
