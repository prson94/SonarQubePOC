using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using Dapper;
using Newtonsoft.Json;
using System.Diagnostics;
using d360.agent.extensions;

namespace d360.extensions.fusion.customers.gmo
{
    public class GMOSynchronizationSource : BaseSchemaSynchronizationSource, IFusionSynchronizationSource
    {
        string LinkedServerPrefix = "";

        #region Models

        public class BaseDatabaseObject
        {
            public int? FusionAttributeID { get; set; }
            public int? ParentFusionAttributeID { get; set; }
        }

        public class ProviderObject : BaseDatabaseObject
        {
            public string SourceID { get; set; }
            public string BusinessTermName { get; set; }
            public string Name { get; set; }

            public static string CACHE_KEY = "GmoProvider";
            public static int FUSION_ATTRIBUTE_TYPE_ID { get { return 50003; } }
            public static string SQL
            {
                get
                {
                    return
@"declare @tbl table (SourceID varchar(10), BusinessTermName nvarchar(500), Name varchar(250))
insert into @tbl
	select	'ISITM',
			issuer_item_name,
			'[dbo].[issuer_item]'
	from	{0}[dbo].[issuer_item]
	UNION
	Select	'SECITM',
			[security_item_name],
			'[dbo].[security_item]'
	from	{0}[dbo].[security_item]
	Union
	SELECT	'CRMRT',
			[credit_market_item_name],
			'[credit].[market_item]'
	FROM	{0}[credit].[market_item]
	UNION
	SELECT	'CRRTN',
			[rating_item_name],
			'[credit].[rating_item]'
	FROM	{0}[credit].[rating_item]
	UNION
	SELECT	'CAITM',
			[corporate_action_item_name],
			'[dbo].[corporate_action_item]'
	FROM	{0}[dbo].[corporate_action_item]
	UNION
	SELECT	'IDXMKT',
			[index_market_item_name],
			'[dbo].[index_market_item]'
	FROM	{0}[dbo].[index_market_item]
	--UNION
	--SELECT 'ISSFUN' + cast([issuer_fundamental_item_id] as varchar(50))  , [issuer_fundamental_item_name] as BusinessTerm,'[dbo].[issuer_fundamental_item]' as [Tablename]
	--FROM {0}[dbo].[issuer_fundamental_item]
	UNION
	SELECT	'ISSFSG',
			[issuer_fundamental_segment_item_name],
			'[dbo].[issuer_fundamental_segment_item]'
	FROM	{0}[dbo].[issuer_fundamental_segment_item]
	UNION
	SELECT	'ISSOUT',
			[issuer_shares_outstanding_item_name],
			'[dbo].[issuer_shares_outstanding_item]'
	FROM	{0}[dbo].[issuer_shares_outstanding_item]
	UNION
	SELECT	'PRITAR',
			[price_target_aggregation_item_name],
			'[dbo].[price_target_aggregation_item]'
	FROM	{0}[dbo].[price_target_aggregation_item]
	UNION
	SELECT	'SECMAR',
			[item_desc],
			'[dbo].[security_market_item]'
	FROM	{0}[dbo].[security_market_item]
	UNION
	SELECT	'SYMBOL',
			[symbol_item_name],
			'[idc].[symbol_item]'
	FROM	{0}[idc].[symbol_item]

    SELECT * FROM @tbl";
                }
            }
        }

        public class BusinessTermTableReferenceObject : BaseDatabaseObject
        {
            public string SourceID { get; set; }
            public string BusinessTermName { get; set; }
            public string Name { get; set; }

            public static string CACHE_KEY = "GmoBtTr";
            public static int FUSION_ATTRIBUTE_TYPE_ID { get { return 50002; } } 
            public static string SQL 
            { 
                get 
                { 
                    return
@"declare @tbl table (SourceID varchar(10), BusinessTermName nvarchar(500), Name varchar(250))
insert into @tbl
	select	'ISITM',
			issuer_item_name,
			'[dbo].[issuer_item]'
	from	{0}[dbo].[issuer_item]
	UNION
	Select	'SECITM',
			[security_item_name],
			'[dbo].[security_item]'
	from	{0}[dbo].[security_item]
	Union
	SELECT	'CRMRT',
			[credit_market_item_name],
			'[credit].[market_item]'
	FROM	{0}[credit].[market_item]
	UNION
	SELECT	'CRRTN',
			[rating_item_name],
			'[credit].[rating_item]'
	FROM	{0}[credit].[rating_item]
	UNION
	SELECT	'CAITM',
			[corporate_action_item_name],
			'[dbo].[corporate_action_item]'
	FROM	{0}[dbo].[corporate_action_item]
	UNION
	SELECT	'IDXMKT',
			[index_market_item_name],
			'[dbo].[index_market_item]'
	FROM	{0}[dbo].[index_market_item]
	--UNION
	--SELECT 'ISSFUN' + cast([issuer_fundamental_item_id] as varchar(50))  , [issuer_fundamental_item_name] as BusinessTerm,'[dbo].[issuer_fundamental_item]' as [Tablename]
	--FROM {0}[dbo].[issuer_fundamental_item]
	UNION
	SELECT	'ISSFSG',
			[issuer_fundamental_segment_item_name],
			'[dbo].[issuer_fundamental_segment_item]'
	FROM	{0}[dbo].[issuer_fundamental_segment_item]
	UNION
	SELECT	'ISSOUT',
			[issuer_shares_outstanding_item_name],
			'[dbo].[issuer_shares_outstanding_item]'
	FROM	{0}[dbo].[issuer_shares_outstanding_item]
	UNION
	SELECT	'PRITAR',
			[price_target_aggregation_item_name],
			'[dbo].[price_target_aggregation_item]'
	FROM	{0}[dbo].[price_target_aggregation_item]
	UNION
	SELECT	'SECMAR',
			[item_desc],
			'[dbo].[security_market_item]'
	FROM	{0}[dbo].[security_market_item]
	UNION
	SELECT	'SYMBOL',
			[symbol_item_name],
			'[idc].[symbol_item]'
	FROM	{0}[idc].[symbol_item]

    SELECT * FROM @tbl"; 
                } 
            }
        }

        public class BusinessTermObject : BaseDatabaseObject
        {
            public string Prefix { get; set; }
            public string SourceID { get; set; }
            public string Name { get; set; }
            public string TableName { get; set; }

            public static string CACHE_KEY = "GmoBt";
            public static int FUSION_ATTRIBUTE_TYPE_ID { get { return 50001; } }
            public static string SQL 
            { 
                get 
                { 
                    return
@"declare @tbl table (Prefix varchar(10), SourceID varchar(50), Name nvarchar(500), TableName varchar(250))
insert into @tbl
	select	'ISITM',
			cast([issuer_item_id] as varchar(50)), 
			issuer_item_name,
			'[dbo].[issuer_item]'
	from	{0}[dbo].[issuer_item]
	UNION
	Select	'SECITM',
			cast([security_item_id] as varchar(50)),
			[security_item_name],
			'[dbo].[security_item]'
	from	{0}[dbo].[security_item]
	Union
	SELECT	'CRMRT',
			cast([credit_market_item_id] as varchar(50)),
			[credit_market_item_name],
			'[credit].[market_item]'
	FROM	{0}[credit].[market_item]
	UNION
	SELECT	'CRRTN',
			cast([rating_item_id] as varchar(50)),
			[rating_item_name],
			'[credit].[rating_item]'
	FROM	{0}[credit].[rating_item]
	UNION
	SELECT	'CAITM',
			cast([corporate_action_item_id] as varchar(50)),
			[corporate_action_item_name],
			'[dbo].[corporate_action_item]'
	FROM	{0}[dbo].[corporate_action_item]
	UNION
	SELECT	'IDXMKT',
			cast([index_market_item_id] as varchar(50)),
			[index_market_item_name],
			'[dbo].[index_market_item]'
	FROM	{0}[dbo].[index_market_item]
	--UNION
	--SELECT 'ISSFUN' + cast([issuer_fundamental_item_id] as varchar(50))  , [issuer_fundamental_item_name] as BusinessTerm,'[dbo].[issuer_fundamental_item]' as [Tablename]
	--FROM {0}[dbo].[issuer_fundamental_item]
	UNION
	SELECT	'ISSFSG',
			cast([issuer_fundamental_segment_item_id] as varchar(50)),
			[issuer_fundamental_segment_item_name],
			'[dbo].[issuer_fundamental_segment_item]'
	FROM	{0}[dbo].[issuer_fundamental_segment_item]
	UNION
	SELECT	'ISSOUT',
			cast([issuer_shares_outstanding_item_id] as varchar(50)),
			[issuer_shares_outstanding_item_name],
			'[dbo].[issuer_shares_outstanding_item]'
	FROM	{0}[dbo].[issuer_shares_outstanding_item]
	UNION
	SELECT	'PRITAR',
			cast([price_target_aggregation_item_id] as varchar(50)),
			[price_target_aggregation_item_name],
			'[dbo].[price_target_aggregation_item]'
	FROM	{0}[dbo].[price_target_aggregation_item]
	UNION
	SELECT	'SECMAR',
			cast([security_market_item_id] as varchar(50)),
			[item_desc],
			'[dbo].[security_market_item]'
	FROM	{0}[dbo].[security_market_item]
	UNION
	SELECT	'SYMBOL',
			cast([symbol_item_id] as varchar(50)),
			[symbol_item_name],
			'[idc].[symbol_item]'
	FROM	{0}[idc].[symbol_item]

	select	S.Prefix + '.' + S.SourceID as SourceID,
			S.Name
	from	@tbl S
			inner join	(
						select		Name,
									MIN(SourceID) as SourceID
						from		@tbl
						group by	Name 						
						) M on S.Name = M.Name and S.SourceId = M.SourceID"; 
                } 
            }
        }

        #endregion

        public void Synchronize(Dictionary<string, object> configuration)
        {
            try
            {
                Trace.TraceInformation("Starting {0}", this.GetType().Name);

                string connectionString = configuration["ConnectionString"].ToString();
                if (configuration.ContainsKey("LinkedServerPrefix"))
                {
                    LinkedServerPrefix = configuration["LinkedServerPrefix"].ToString();
                }
                if (!string.IsNullOrEmpty(connectionString))
                {
                    #region 0. Global Variables

                    CompanyID = configuration["CompanyID"].ToString();
                    FusionTypeID = int.Parse(configuration["FusionTypeID"].ToString());
                    FusionID = int.Parse(configuration["ID"].ToString());
                    connection = new XmlDb(FusionID);
                    var fusionAttributeUri = string.Format("fusion/{0}/configurations/{1}/attributes/", FusionTypeID, FusionID) + "{0}";
                    var relationshipUri = "lineage/relationships";
                    var relations = new List<RelationshipModel>();

                    var fusionItemsToSend = getNewFusionItemList();
                    var attributeWithFieldsToUpdate = new Dictionary<FusionAttribute, Dictionary<string, string>>();

                    var dbConnection = new SqlConnection(connectionString);

                    #endregion

                    StartJob();

                    #region Get from fusion source

                    Trace.TraceInformation("Getting business terms and references from data source.");

                    dbConnection.Open();

                    var businessterms = dbConnection.Query<BusinessTermObject>(string.Format(BusinessTermObject.SQL, LinkedServerPrefix)).ToList();

                    var businesstermTableRefs = dbConnection.Query<BusinessTermTableReferenceObject>(string.Format(BusinessTermTableReferenceObject.SQL, LinkedServerPrefix)).ToList();

                    dbConnection.Close();

                    Trace.TraceInformation("Finished getting business terms and references from data source.");

                    #endregion

                    try
                    {
                        businessterms.AsParallel().WithDegreeOfParallelism(10).ForAll(o =>
                        {
                            var isNew = false;
                            var jsonFields = new Dictionary<string, string>();

                            FusionAttribute attribute = null;

                            attribute = connection.Attributes.SingleOrDefault(i => i.SourceID == o.SourceID && i.Type == BusinessTermObject.CACHE_KEY);

                            #region Node is new, so create a new cached attribute

                            if (attribute == null)
                            {
                                isNew = true;
                                attribute = new FusionAttribute { FusionAttributeTableID = Guid.NewGuid(), CompanyID = CompanyID, FusionID = FusionID, Type = BusinessTermObject.CACHE_KEY, SourceID = o.SourceID };
                                lock (connection.Attributes) connection.Attributes.Add(attribute);
                            }

                            #endregion

                            #region Build fields for this node

                            jsonFields.Add("Name", o.Name);
                            jsonFields.Add("SourceID", o.SourceID);

                            #endregion

                            getOnlyDifferentOrNewFields(attribute, jsonFields, isNew);
                        });
                    }
                    catch
                    {
                    }

                    Trace.TraceInformation("Posting {0} business terms - Starting", businessterms.Count);
                    sendFusionData(BusinessTermObject.CACHE_KEY, string.Format(fusionAttributeUri, BusinessTermObject.FUSION_ATTRIBUTE_TYPE_ID));
                    Trace.TraceInformation("Posting {0} business terms - Ending", businessterms.Count);

                    Trace.TraceInformation("Caching {0} business terms - Starting", businessterms.Count);
                    connection.Save();
                    Trace.TraceInformation("Caching {0} business terms - Ending", businessterms.Count);

                    try
                    {
                        businesstermTableRefs.AsParallel().WithDegreeOfParallelism(10).ForAll(o =>
                        {
                            var isNew = false;
                            var jsonFields = new Dictionary<string, string>();

                            FusionAttribute attribute = null;

                            attribute = connection.Attributes.SingleOrDefault(i => i.SourceID == o.SourceID && i.Type == BusinessTermTableReferenceObject.CACHE_KEY);

                            #region Node is new, so create a new cached attribute

                            if (attribute == null)
                            {
                                isNew = true;
                                attribute = new FusionAttribute { FusionAttributeTableID = Guid.NewGuid(), CompanyID = CompanyID, FusionID = FusionID, Type = BusinessTermTableReferenceObject.CACHE_KEY, SourceID = o.SourceID };
                                lock (connection.Attributes) connection.Attributes.Add(attribute);
                            }

                            #endregion

                            #region Build fields for this node

                            jsonFields.Add("Name", o.Name);
                            jsonFields.Add("SourceID", o.SourceID);

                            #endregion

                            getOnlyDifferentOrNewFields(attribute, jsonFields, isNew);
                        });
                    }
                    catch
                    {
                    }

                    Trace.TraceInformation("Posting {0} table references - Starting", businesstermTableRefs.Count);
                    sendFusionData(BusinessTermTableReferenceObject.CACHE_KEY, string.Format(fusionAttributeUri, BusinessTermTableReferenceObject.FUSION_ATTRIBUTE_TYPE_ID));
                    Trace.TraceInformation("Posting {0} table references - Ending", businesstermTableRefs.Count);

                    Trace.TraceInformation("Caching {0} table references - Starting", businesstermTableRefs.Count);
                    connection.Save();
                    Trace.TraceInformation("Caching {0} table references - Ending", businesstermTableRefs.Count);

                    #region Relationships

                    try
                    {
                        Trace.TraceInformation("Processing objects to relate");
                        var rels = (
                                   from ta in connection.Attributes
                                   join tr in businesstermTableRefs on ta.SourceID equals tr.SourceID
                                   join br in businessterms on tr.BusinessTermName equals br.Name
                                   join ba in connection.Attributes on br.SourceID equals ba.SourceID
                                   where ta.Type == BusinessTermTableReferenceObject.CACHE_KEY
                                   where ta.FusionAttributeID != 0
                                   where ba.Type == BusinessTermObject.CACHE_KEY
                                   where ba.FusionAttributeID != 0
                                   select new RelationshipModel
                                   {
                                       EndID = ta.FusionAttributeID,
                                       EndType = "FusionAttribute",
                                       StartID = ba.FusionAttributeID,
                                       StartType = "FusionAttribute"
                                   }
                                   ).ToList();

                        Trace.TraceInformation("Posting {0} relationships - Starting", rels.Count);
                        while (rels.Count > 0)
                        {
                            int removeCount = rels.Count > MAX_POST_COUNT ? MAX_POST_COUNT : rels.Count;
                            var relationsToPost = rels.Skip(0).Take(removeCount);
                            getPostResponses(relationshipUri, JsonConvert.SerializeObject(relationsToPost));
                            rels.RemoveRange(0, removeCount - 1);
                        }
                        Trace.TraceInformation("Posting relationships - Ending");
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError(ex.Message, (ex.InnerException != null) ? ex.InnerException.Message : "");
                    }

                    #endregion

                    CompleteJob();

                    Trace.TraceInformation("Completing {0}", this.GetType().Name);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("{0}{1}{2}", this.GetType().Name, ex.Message, (ex.InnerException != null ? ex.InnerException.Message : ""));
            }
        }
    }
}
