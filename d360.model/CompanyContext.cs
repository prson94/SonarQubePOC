using d360.core;
using d360.core.entities;
using d360.core.entities.Contracts;
using d360.core.entities.Views;
using d360.core.enums;
using d360.core.enums.Workflow;
using d360.core.exceptions;
using d360.core.helpers;
using d360.core.queue;
using d360.core.resources;
using d360.extensions;
using Dapper;
using Ganss.XSS;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Design.PluralizationServices;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace d360.model
{
    [DbConfigurationType(typeof(AzureConfiguration))]
    public partial class CompanyContext : BaseContext
    {
        #region Caching Methods

        internal string FUSIONATTRIBUTES_BY_FUSION_PREFIX_KEY = "AttributesByFusion_{0}_{1}";
        internal string REPORTING_SCHEMA_KEY = "ReportingSchema_{0}";
        internal string TAXONOMY_TYPES_KEY = "TaxonomyTypes_{0}";                
        internal string ARTIFACTDICTIONARY_BY_TYPE_PREFIX_KEY = "ArtifactDictionaryByType_{0}_{1}";

        internal string key(string token)
        {
            return string.Format(token, CurrentCompanyID);
        }

        internal string key(string token, int id)
        {
            return string.Format(token, CurrentCompanyID, id);
        }

        #endregion

        internal IQueueSource QueueSource;

        CommunityContext Community;

        bool IsEventingEnabled = false;

        #region Ctors

        public CompanyContext(CommunityContext community, ICachingProvider caching, IQueueSource queueSource, ISecurityContextProvider context, bool skipCacheCheck = false)
            : base(community.GetCompanyConnectionString(skipCacheCheck))
        {
            Database.SetInitializer<CompanyContext>(null); //dont create any tables if they dont exist.

            Community = community;
            Caching = caching;
            QueueSource = queueSource;

            CurrentCompanyID = context.CompanyID;
            CurrentResourceID = context.ResourceID;
            CurrentResourceIsAdmin = context.IsAdministrator;
            CurrentCompanyDomain = context.CompanyPrefix;
            
            //output queries in debug mode to console
            if (System.Diagnostics.Debugger.IsAttached)
                this.Database.Log = s => System.Diagnostics.Debug.WriteLine(s);

            var eventBusValue = (ConfigurationManager.AppSettings["EventBusTopicEnabled"] ?? "").ToUpper();

            if (eventBusValue == "TRUE") IsEventingEnabled = true;
            else IsEventingEnabled = false;
        }

        #endregion

        #region DbSets

        public DbSet<Artifact> Artifacts { get; set; }

        public DbSet<AssetDataProfile> AssetDataProfiles { get; set; }

        public DbSet<AssetTypeExportTemplate> AssetTypeExportTemplates { get; set; }


        public DbSet<AssetTypeExportTemplateStyle> AssetTypeExportTemplateStyles { get; set; }

        public DbSet<ArtifactType> ArtifactTypes { get; set; }

        public DbSet<d360.core.entities.Attribute> Attributes { get; set; }

        public DbSet<AttributeDetail> AttributeDetails { get; set; }                            /* VIEW */

        public DbSet<AttributeType> AttributeTypes { get; set; }

        public DbSet<AttributeTypeCategory> AttributeTypeCategories { get; set; }

        public DbSet<AttributeTypeRelation> AttributeTypeRelations { get; set; }

        public DbSet<AttributeTypeRelationDetail> AttributeTypeRelationDetails { get; set; }    /* VIEW */
                
        public DbSet<Comment> Comments { get; set; }

        public DbSet<CommentRelation> CommentRelations { get; set; }

        public DbSet<ContractAcceptance> ContractAcceptance { get; set; }

        public DbSet<Favorite> Favorites { get; set; }

        public DbSet<Field> Fields { get; set; }

        public DbSet<FieldValue> FieldValues { get; set; }

        public DbSet<FieldLookupValue> FieldLookupValues { get; set; }                          /* VIEW */

        public DbSet<FieldWithRelation> FieldWithRelations { get; set; }                        /* VIEW */

        public DbSet<FieldType> FieldTypes { get; set; }

        public DbSet<FieldTypeLookupValue> FieldTypeLookupValues { get; set; }                  /* VIEW */

        public DbSet<FieldTypeLookup> FieldTypeLookups { get; set; }

        public DbSet<FieldTypeFilteredLookupDefinition> FieldTypeFilteredLookupDefinitions { get; set; }

        public DbSet<FieldTypeFilteredLookupDisplayField> FieldTypeFilteredLookupDisplayFields { get; set; }

        public DbSet<FieldTypeFusionLookupDefinition> FieldTypeFusionLookupDefinitions { get; set; }

        public DbSet<FieldTypeFusionLookupDisplayField> FieldTypeFusionLookupDisplayFields { get; set; }

        public DbSet<Follow> Follows { get; set; }

        public DbSet<FollowDetail> FollowDetails { get; set; }                                  /* VIEW */

        public DbSet<FusionExecution> FusionExecutions { get; set; }

        public DbSet<FusionSchedule> FusionSchedules { get; set; }
        
        public DbSet<Fusion> FusionTypeConfigurations { get; set; }

        public DbSet<FusionAttribute> FusionAttributes { get; set; }

        public DbSet<FusionAttributeType> FusionAttributeTypes { get; set; }

        public DbSet<FusionAttributeTypeCustomQuery> FusionAttributeTypeCustomQueries { get; set; }

        public DbSet<FusionFilter> FusionFilters { get; set; }

        public DbSet<FusionQueryAttribute> FusionQueryAttributes { get; set; }

        public DbSet<FusionQueryAttributeType> FusionQueryAttributeTypes { get; set; }

        public DbSet<FusionRule> FusionRules { get; set; }

        public DbSet<FusionRuleFilter> FusionRuleFilters { get; set; }

        public DbSet<FusionRuleItem> FusionRuleItem { get; set; }
        
        public DbSet<FusionStatusLog> FusionStatusLogs { get; set; }

        public DbSet<FusionType> FusionTypes { get; set; }

        public DbSet<FusionAgentError> FusionAgentErrors { get; set; }

        public DbSet<Intersect> Intersects { get; set; }

        public DbSet<IntersectDetail> IntersectDetails { get; set; }                /* VIEW */

        public DbSet<IntersectTypeDetail> IntersectTypeDetails { get; set; }        /* VIEW */

        public DbSet<IntersectGroup> IntersectGroups { get; set; }

        public DbSet<IntersectType> IntersectTypes { get; set; }

        public DbSet<Issue> Issues { get; set; }

        public DbSet<core.entities.IssueType> IssueTypes { get; set; }

        public DbSet<IssueTypeRelation> IssueTypeRelations { get; set; }

        public DbSet<Lookup> Lookups { get; set; }

        public DbSet<LookupType> LookupTypes { get; set; }

        public DbSet<Nym> Nyms { get; set; }

        public DbSet<NymRelation> NymRelations { get; set; }

        public DbSet<ObjectStyle> ObjectStyles { get; set; }

        public DbSet<Policy> Policies { get; set; }

        public DbSet<PolicyType> PolicyTypes { get; set; }

        public DbSet<PolicyTypeLevel> PolicyTypeLevels { get; set; }

        public DbSet<Predicate> Predicates { get; set; }

        public DbSet<Question> Questions { get; set; }

        public DbSet<QuestionType> QuestionTypes { get; set; }

        public DbSet<QuestionTypeOption> QuestionTypeOptions { get; set; }

        public DbSet<ReferenceItem> ReferenceItems { get; set; }

        public DbSet<ReferenceItemType> ReferenceItemTypes { get; set; }

        public DbSet<ReportLayout> ReportLayouts { get; set; }

        public DbSet<Report> Reports { get; set; }

        public DbSet<ReportResponsibility> ReportResponsibilities { get; set; }

        public DbSet<ReportTile> ReportTiles { get; set; }

        public DbSet<d360.core.entities.Rule> Rules { get; set; }

        public DbSet<d360.core.entities.RuleDimension> RuleDimensions { get; set; }

        public DbSet<RuleImplementation> RuleImplementations { get; set; }

        public DbSet<RuleResult> RuleResults { get; set; }

        public DbSet<RuleResultFusionAttribute> RuleResultFusionAttributes { get; set; }

        public DbSet<RuleResultQualifier> RuleResultQualifiers { get; set; }

        public DbSet<RuleResultQualifierType> RuleResultQualifierTypes { get; set; }

        public DbSet<SiteNav> SiteNav { get; set; }

        public DbSet<SiteNavPermission> SiteNavPermissions { get; set; }

        public DbSet<ShoppingCartType> ShoppingCartTypes { get; set; }

        public DbSet<ShoppingCart> ShoppingCarts { get; set; }

        public DbSet<ShoppingCartItem> ShoppingCartItems { get; set; }

        public DbSet<Shortcut> Shortcuts { get; set; }

        public DbSet<Survey> Surveys { get; set; }

        public DbSet<SurveyType> SurveyTypes { get; set; }

        public DbSet<Taxonomy> Taxonomies { get; set; }

        public DbSet<TaxonomyTypeLevel> TaxonomyTypeLevels { get; set; }

        public DbSet<AssetTypeLevel> AssetTypeLevels { get; set; }

        public DbSet<TaxonomyType> TaxonomyTypes { get; set; }

        public DbSet<AuditField> AuditFields { get; set; }

        public DbSet<Audit> Audits { get; set; }

        #endregion

        #region Legacy Lineage

        public DbSet<Map> Maps { get; set; }

        public DbSet<MapGroup> MapGroups { get; set; }

        public DbSet<MapGroupItem> MapGroupItems { get; set; }

        public DbSet<MapType> MapTypes { get; set; }

        public DbSet<MapTypeOrder> MapTypeOrders { get; set; }

        public DbSet<MapTypeTemplate> MapTypeTemplates { get; set; }

        public DbSet<MapTypeTemplateItem> MapTypeTemplateItems { get; set; }

        public DbSet<MapItem> MapItems { get; set; }

        public DbSet<MapRule> MapRules { get; set; }

        public DbSet<MapRuleItem> MapRuleItems { get; set; }

        public DbSet<MapRuleItemMapItem> MapRuleItemMapItems { get; set; }

        public DbSet<MapSequence> MapSequences { get; set; }

        public DbSet<MapSequenceContext> MapSequenceContexts { get; set; }

        #endregion

        #region Repository Methods


        public void AddOrUpdateFields(List<Field> items)
        {
            if (items.Count > 0)
            {
                var oID = items[0].ObjectID;
                var oType = items[0].ObjectType;
                var existingFieldTypeIDs = Filter<Field>(i => i.ObjectID == oID && i.ObjectType == oType).Select(i => i.FieldTypeID).ToList();
                items.ForEach(item =>
                {
                    item.UpdatedBy = CurrentResourceID;
                    //UPDATE
                    if (existingFieldTypeIDs.Any(i => item.FieldTypeID == i))
                    {
                        Set<Field>().Attach(item);
                        Entry(item).State = (string.IsNullOrEmpty(item.Value)) ? EntityState.Deleted : EntityState.Modified;
                        if (item.ObjectType == "FusionAttribute")
                        {
                            item.FormattedValue = GetFormattedFieldLookupValue(item.FieldTypeID, item.Value);
                        }
                        
                    }
                    else //ADD
                    {
                        if (!string.IsNullOrEmpty(item.Value))
                        {
                            if (item.ObjectType == "FusionAttribute")
                            {
                                item.FormattedValue = GetFormattedFieldLookupValue(item.FieldTypeID, item.Value);
                            }
                            Set<Field>().Add(item);
                        }
                    }
                });
                try
                {
                    var existingFields = Filter<Field>(i => i.ObjectID == oID && i.ObjectType == oType).ToList();
                    existingFields.ForEach(item =>
                    {
                        //DELETE
                        if (items.Any(i => i.FieldTypeID == item.FieldTypeID && string.IsNullOrEmpty(i.Value)))
                        {
                            Set<Field>().Remove(item);

                            if (item.FieldType != null && item.FieldType.AllowMultipleValues)
                            {
                                var sql = @"delete from [dbo].[fieldvalue] where objectID = @objectID and objectType = @objectType and fieldtypeid = @fieldTypeID";

                                Execute(sql, new { objectID = oID, objectType = oType, fieldTypeID = item.FieldTypeID });
                            }
                        }
                    });
                }
                catch
                {
                }
                SaveChanges();

                items.ForEach(item =>
                {
                    if (item.FieldType != null && item.FieldType.AllowMultipleValues)
                    {
                        var sql = @" MERGE dbo.[FieldValue] AS T
                                    USING(
                                            SELECT
                                                f.fieldtypeid as fieldtypeid,
                                                f.objectid as objectid,
                                                f.objecttype as objecttype,
                                                V.value as id
                                            FROM field f
                                                CROSS APPLY STRING_SPLIT(f.[Value], ',') as V
                                            where
                                                objectid = @objectID and fieldtypeid = @fieldTypeID and objecttype = @objectType
                                        ) as S
                                    ON T.fieldtypeid = S.fieldtypeid
                                            and T.objectid = S.objectid
                                            and T.objecttype = S.objecttype
                                            and T.[value] = S.id
                                    WHEN NOT MATCHED BY TARGET THEN
                                            INSERT(FieldTypeID, ObjectID, ObjectType, [Value])
                                            VALUES(S.FieldTypeID, S.ObjectID, S.ObjectType, S.ID)
                                    WHEN NOT MATCHED BY SOURCE AND T.FieldTypeID = @fieldTypeID and T.ObjectID = @objectID and T.ObjectType = @objectType
                                        THEN DELETE;";
                        Query<int>(sql, new { objectID = oID, objectType = oType, fieldTypeID = item.FieldTypeID });


                    }
                });
            }
        }

        public void Enqueue(string queueName, QueueObject item)
        {
            QueueSource.CreateMessage(queueName, item);
        }

        public void Enqueue(string queueName, List<QueueObject> items)
        {
            QueueSource.CreateMessages(queueName, items);
        }

        public JObject GetPageInformation(SystemObjects o, int oid)
        {
            var jsonRows = Database.Connection.Query<string>("exec GetPageInformation @o, @oid, @rid", new { o = o.ToString(), oid, rid = CurrentResourceID });
            if (jsonRows.Count() == 0)
                return null;

            var json = string.Concat(jsonRows);
            return JObject.Parse(json);
        }

        public List<AllocationPossibility> GetTypes()
        {
            var list = Database.Connection.Query<AllocationPossibility>(@"
select	Object as ObjectType, 
		ObjectID as ObjectTypeID, 
		case Object
			when 'ArtifactType' then 'Artifacts :: '
			when 'TaxonomyType' then 'Models :: '
			when 'PolicyType' then 'Policies :: '
			when 'RuleType' then 'Rules :: '
		end + Name as Name
from	AssetType
where	Class in (1,2,6,7)
union
select	'GroupType' as ObjectType, 1 as ObjectTypeID, 'Group' as Name
union
select	'ResourceType' as ObjectType, 1 as ObjectTypeID, 'User' as Name ").ToList();

            list = list.OrderBy(i => i.Name).ToList();

            return list;
        }

        public Task<IEnumerable<IntersectTypeApiViewModel>> GetActiveIntersectTypes()
        {
            return QueryAsync<IntersectTypeApiViewModel>(@"
select	I.ID,
        I.Uid,
        coalesce(I.IsSystem, 0) as IsSystem,
		P.Name as PredicateName,
		P.Inverse as PredicateInverse,
		P.[Type] as PredicateTypeID,
		I.Subject,
		I.SubjectID,
		I.SubjectUid as SubjectUid,
		coalesce(S.Class, 0) as SubjectClassID,
		case 
			when I.Subject = 'IntersectType' then SI.SubjectName + ' ' + SI.PredicateName + ' ' + SI.ObjectName + ' relationship'
			else coalesce(SFT.Name + ' / ','') + coalesce(SP.[Path], S.Name)
		end as SubjectTypeName,
		I.Object,
		I.ObjectID,
		I.ObjectUid,
		coalesce(O.Class, 0) as ObjectClassID,
		coalesce(OFT.Name + ' / ','') + coalesce(OP.[Path], O.Name) as ObjectTypeName
from	IntersectType I
		left join [Predicate] P on P.ID = I.PredicateID

		left join AssetType S on (S.uid = I.SubjectUid OR (S.Object = I.Subject and S.ObjectID = I.SubjectID))
        left join FusionAttributeType SFAT on I.Subject = 'FusionAttributeType' and SFAT.ID = I.SubjectID 
        left join FusionType SFT on SFT.ID = SFAT.FusionTypeID 
        outer apply dbo.GetAssetTypeTextPathById(S.ID, '/') SP

		left join IntersectTypeDetail SI on I.Subject = 'IntersectType' and SI.ID = I.SubjectID
		left join AssetType O on (O.uid = I.ObjectUid OR (O.Object = I.Object and O.ObjectID = I.ObjectID))
        left join FusionAttributeType OFAT on I.Object = 'FusionAttributeType' and OFAT.ID = I.ObjectID 
        left join FusionType OFT on OFT.ID = OFAT.FusionTypeID 
        outer apply dbo.GetAssetTypeTextPathById(O.ID, '/') OP
where	I.State = 1");
        }

        public Task<IEnumerable<IntersectTypeApiViewModel>> GetActiveIntersectTypes(int id, string type)
        {
            return QueryAsync<IntersectTypeApiViewModel>(@"
select	I.ID,
        I.Uid,
        coalesce(I.IsSystem, 0) as IsSystem,
		P.Name as PredicateName,
		P.Inverse as PredicateInverse,
		P.[Type] as PredicateTypeID,
		I.Subject,
		I.SubjectID,
		I.SubjectUid as SubjectUid,
		coalesce(S.Class, 0) as SubjectClassID,
		case 
			when I.Subject = 'IntersectType' then SI.SubjectName + ' ' + SI.PredicateName + ' ' + SI.ObjectName + ' relationship'
			else coalesce(SFT.Name + ' / ','') + coalesce(SP.[Path], S.Name)
		end as SubjectTypeName,
		I.Object,
		I.ObjectID,
		I.ObjectUid,
		coalesce(O.Class, 0) as ObjectClassID,
		coalesce(OFT.Name + ' / ','') + coalesce(OP.[Path], O.Name) as ObjectTypeName
from	IntersectType I
		left join [Predicate] P on P.ID = I.PredicateID

		left join AssetType S on (S.uid = I.SubjectUid OR (S.Object = I.Subject and S.ObjectID = I.SubjectID))
        left join FusionAttributeType SFAT on I.Subject = 'FusionAttributeType' and SFAT.ID = I.SubjectID 
        left join FusionType SFT on SFT.ID = SFAT.FusionTypeID 
        outer apply dbo.GetAssetTypeTextPathById(S.ID, '/') SP

		left join IntersectTypeDetail SI on I.Subject = 'IntersectType' and SI.ID = I.SubjectID
		left join AssetType O on (O.uid = I.ObjectUid OR (O.Object = I.Object and O.ObjectID = I.ObjectID))
        left join FusionAttributeType OFAT on I.Object = 'FusionAttributeType' and OFAT.ID = I.ObjectID 
        left join FusionType OFT on OFT.ID = OFAT.FusionTypeID 
        outer apply dbo.GetAssetTypeTextPathById(O.ID, '/') OP
where	I.State = 1 and (I.SubjectID = @id and I.[Subject] = @type or I.ObjectID = @id and I.Object = @type)", new { id, type });
        }

        public List<AllocationPossibility> GetAllocationOptions()
        {
            var list = Database.Connection.Query<AllocationPossibility>(@"
select	Object as ObjectType, 
		ObjectID as ObjectTypeID, 
		case Object
			when 'ArtifactType' then 'Artifacts :: '
			when 'TaxonomyType' then 'Models :: '
			when 'PolicyType' then 'Policies :: '
			when 'RuleType' then 'Rules :: '
			when 'FusionType' then 'Fusion Types :: '
			when 'ReferenceItemType' then 'Reference Item Type :: '
		end + Name as Name
from	AssetType
where	Class in (1,2,3,6,7,9)
union
select 'IntersectType' as ObjectType, ID as ObjectTypeID, 'Relationships :: ' + IName.Name as title from intersecttypedetail itd cross apply dbo.GetIntersectTypeNames(itd.ID) IName
union
select	'FusionAttributeType' as ObjectType, ID as ObjectTypeID, 'Fusion Attributes :: ' + TextPath as Name from FusionAttributeType").ToList();

            list = list.OrderBy(i => i.Name).ToList();

            return list;
        }

        public List<AllocationPossibility> GetAvailableAllocationOptions(int attributeTypeID)
        {
            var list = Database.Connection.Query<AllocationPossibility>(@"
select A.* from (
select	Object as ObjectType, 
		ObjectID as ObjectTypeID, 
		case Object
			when 'ArtifactType' then 'Artifacts :: '
			when 'TaxonomyType' then 'Models :: '
			when 'PolicyType' then 'Policies :: '
			when 'RuleType' then 'Rules :: '
			when 'FusionType' then 'Fusion Types :: '
			when 'ReferenceItemType' then 'Reference Item Type :: '
		end + Name as Name
from	AssetType
where	Class in (1,2,3,6,7,9)
union
select	'FusionAttributeType' as ObjectType, ID as ObjectTypeID, 'Fusion Attributes :: ' + TextPath as Name from FusionAttributeType
) A left join AttributeTypeRelationDetail R on R.ObjectType = A.ObjectType and R.ObjectID = A.ObjectTypeID and R.AttributeTypeID = @id
where R.ObjectID is null", new { id = attributeTypeID }).ToList();

            list = list.OrderBy(i => i.Name).ToList();

            return list;
        }

        public async Task<IEnumerable<AllowedIntersectionType>> GetAllowedIntersectionTypes(string type, int id)
        {         
            return await Database.Connection
                .QueryAsync<AllowedIntersectionType>("GetAllowedIntersectionTypes @SourceType, @SourceTypeID", 
                new 
                { 
                    SourceType = new Dapper.DbString { Value = type.ToString(), IsAnsi = true, IsFixedLength = true, Length = 50 }, 
                    SourceTypeID = id                    
                });
        }
        
        public IQueryable<AttributeHierarchyItem> GetAttributeAndIntersectHierarchyByObject(SystemObjects type, int id)
        {
            return Query<AttributeHierarchyItem>("EXEC GetAttributeAndIntersectHierarchyByObject @type, @id", new { type = new Dapper.DbString { Value = type.ToString(), IsAnsi = true }, id = id }).AsQueryable();
        }

        public string GetFormattedFieldLookupValue(int fieldTypeID, string fieldValue)
        {
            return
                Database.Connection.Query<string>(@"
declare @type varchar(25),
        @format nvarchar(250),
        @lo varchar(25),
        @loid int

select  @type = [Type],
        @format = LookupDisplayFormat,
        @lo = LookupObjectType,
        @loid = LookupObjectID
from    FieldType 
where ID = @fieldTypeID

select utility.GetFormattedFieldLookupValue(@type, @format, @lo, @loid, @fieldValue)", new { fieldTypeID, fieldValue }).First();
        }

        public async Task<IEnumerable<FieldFilterModel>> GetFieldFiltersByType(SystemObjects type, int id)
        {
            #region SQL

            var sql = $@"
	declare @tbl table (SortOrder int, [Group] varchar(50), [Object] varchar(50), ObjectID int, Label nvarchar(500), [Type] varchar(50));

	insert into @tbl
		select	1 as SortOrder,
				'Field' as [Group],
				Name as Object,
				ID as ObjectID,
				FriendlyName as Label,
				Type
		from	FieldType
		where	Object = @SourceType
				and ObjectID = @SourceTypeID
				and Type not in ('DataTableSelect', 'FilteredLookup', 'FusionLookup', 'ComplexRelationLookup', 'RelationLookup') --these are calculated fields, and should not be selectable.
		union
		select	1 as SortOrder,
				'Field' as [Group], 'Name' as Object, 0 as ObjectID, 'Name' as Label, 'Text' as Type
		union
		select	1 as SortOrder,
				'Field' as [Group], 'Description' as Object, 0 as ObjectID, 'Description' as Label, 'Text' as Type
		union
		select	1 as SortOrder,
				'Field' as [Group], 'Status' as Object, 0 as ObjectID, 'Status' as Label, 'Lookup' as Type

	insert into @tbl
		SELECT	distinct
				2 as SortOrder,
				'Relationship' as [Group],--ID,
				Object,
				ObjectID,
				ObjectName as Label,
				'Lookup' as Type
		FROM	IntersectTypeDetail
		WHERE	Subject = @SourceType and SubjectID = @SourceTypeID

	merge	into
			@tbl T
	using	(
				SELECT	distinct
						2 as SortOrder,
						'Relationship' as [Group],--ID,
						COALESCE(I2.Object, I1.Subject) as Object,
						COALESCE(I2.ObjectID, I1.SubjectID) as ObjectID,
						COALESCE(I2.ObjectName, I1.SubjectName) as Label,
						'Lookup' as Type
				FROM	IntersectTypeDetail I1
						left join IntersectTypeDetail I2 on I1.Subject = 'MapType' and I1.Subject = I2.Subject and I1.SubjectID = I2.SubjectID and I1.ID <> I2.ID 
				WHERE	I1.Object = @SourceType and I1.ObjectID = @SourceTypeID
			) S
	on		(S.SortOrder = T.SortOrder and S.[Group] = T.[Group] and S.Object = T.Object and S.ObjectID = T.ObjectID)
	when	not matched then
	insert	(SortOrder, [Group], [Object], ObjectID, Label, [Type])
	values	(S.SortOrder, S.[Group], S.[Object], S.ObjectID, S.Label, S.[Type]);

	insert into @tbl
		select	4 as SortOrder,
				'Attribute' as [Group],
				'AttributeType' as Object,
				ID as ObjectID,
				Name as Label,
				'Lookup' as Type
		from	AttributeType T
				inner join AttributeTypeRelation R on R.AttributeTypeid = T.ID and R.ObjectType = @SourceType and R.ObjectID = @SourceTypeID

	insert into @tbl
		select	distinct
				3 as SortOrder,
				'Responsibility' as [Group],
				'ResponsibilityType' as Object,
				0 as ObjectID,
				'Owner' as Label,
				'Lookup' as Type
		from	ResponsibilityType T
				inner join ResponsibilityTypeRelation R on R.ResponsibilityTypeid = T.ID and R.ObjectType = @SourceType and R.ObjectID = @SourceTypeID

	select [Group], [Object], [ObjectID], [Label], [Type] from @tbl order by SortOrder, Label
";
            #endregion

            return await Database.Connection.QueryAsync<FieldFilterModel>(sql, new {
                SourceType = new Dapper.DbString { IsAnsi = true, IsFixedLength = true, Length = 50, Value = type.ToString() },
                SourceTypeID = id
            });
        }

        public IQueryable<FieldWithRelation> GetFieldRelationsByObject(SystemObjects type, int id)
        {
            var sType = type.ToString();
            return Filter<FieldWithRelation>(i => i.ObjectType == sType && i.ObjectID == id);
        }

        public IQueryable<FieldType> GetFieldTypesByObject(SystemObjects type, int id)
        {
            var sType = type.ToString(); 
            return Filter<FieldType>(
                i => i.Object == sType && i.ObjectID == id
                )
                .OrderBy(i => i.ColumnOrder).ThenBy(i => i.FriendlyName)
                .AsQueryable();
        }

        public Dictionary<string,object> GetRelationshipFieldItems(int fieldTypeID, string @object = null, int? objectID = null, int offset = 0, int rows = 25, string query = null, bool includeSelection = true)
        {
            var ft = GetById<FieldType>(fieldTypeID);
            var intersectType = GetById<IntersectType>(ft.LookupObjectID.Value);
            int count = 0, objID = 0;
            string sql, countSql, obj, selectedSql;

            bool isSubject = (intersectType.Subject == ft.Object && intersectType.SubjectID == ft.ObjectID);
            obj = isSubject ? intersectType.Object : intersectType.Subject;
            objID = isSubject ? intersectType.ObjectID : intersectType.SubjectID;
            var cardinality = isSubject ? intersectType.ObjectCardinality : intersectType.SubjectCardinality;

            countSql = @"select count(*) from AssetDetail with (nolock) where [Type] = @obj and TypeID = @objID order by DisplayValue";
            sql = @"select ObjectID as Value, DisplayValue as Text from AssetDetail with (nolock) where [Type] = @obj and TypeID = @objID
                    order by DisplayValue OFFSET @offset ROWS FETCH NEXT @rows ROWS ONLY";
            selectedSql = @"select 
	                case when i.Subject = @obj and i.SubjectID = @objID then i.ObjectID else i.SubjectID end as [Value],
	                P.TextPath as [Text],
	                1 as Selected
                 from [intersect] i
                 inner join Asset A with (nolock) on A.[Object] = case when i.[Subject] = @obj and i.SubjectID = @objID then i.[Object] else i.[Subject] end
				                and A.ObjectID = case when i.[Subject] = @obj and i.SubjectID = @objID then i.ObjectID else i.SubjectID end
                cross apply GetAssetTextPathById(A.ID, '.') P
                where i.intersectTypeID = @intersectTypeID and i.State = 1 and ((i.Subject = @obj and i.SubjectID = @objID) or (i.Object = @obj and i.ObjectID = @objID))";

            switch (obj)
            {
                case "ReferenceItemType":

                    if (objID == 0)
                    {
                        countSql = @"select count(*) from AssetType A with (nolock)
                        where A.[Object] = @obj  and (@query is null or A.Name like '%' + @query + '%')";
                        sql = @"select  A.ObjectID as [Value], A.[Name] as [Text], case when I.ID is not null then 1 else 0 end as Selected
                            from AssetType A with (nolock)
                            left join [Intersect] I on I.IntersectTypeID = @intersectTypeID and 
	                            ((I.[Subject] = A.[Object] and I.SubjectID = A.ObjectID and I.[Object] = @fieldObject and I.ObjectID = @fieldObjectID) or
	                            (I.[Object] = A.[Object] and I.ObjectID = A.ObjectID and I.[Subject] = @fieldObject and I.SubjectID = @fieldObjectID))
                            where A.[Object] = @obj and (@query is null or A.[Name] like '%' + @query + '%')
                            order by 3 desc, A.[Name] asc
                            OFFSET @offset ROWS FETCH NEXT @rows ROWS ONLY";
                        selectedSql = @"select 
	                        case when i.Subject = @obj and i.SubjectID = @objID then i.ObjectID else i.SubjectID end as [Value],
	                        A.[Name] as [Text],
	                        1 as Selected
                            from [intersect] i
                            inner join AssetType A with (nolock) on A.[Object] = case when i.[Subject] = @obj and i.SubjectID = @objID then i.[Object] else i.[Subject] end
				                        and A.ObjectID = case when i.[Subject] = @obj and i.SubjectID = @objID then i.ObjectID else i.SubjectID end
                        where i.intersectTypeID = @intersectTypeID and i.State = 1 and ((i.Subject = @obj and i.SubjectID = @objID) or (i.Object = @obj and i.ObjectID = @objID))";
                    }
                    else
                    {
                        countSql = @"select count(*) from Asset A with (nolock)
                        inner join AssetType T with (nolock) on T.ID = A.AssetTypeID
						cross apply dbo.GetAssetDisplayValueById(a.ID) D
                        where T.[Object] = @obj and T.ObjectID = @objID and (@query is null or D.DisplayValue like '%' + @query + '%')";

                        sql = @"select  A.ObjectID as [Value], D.DisplayValue as [Text], case when I.ID is not null then 1 else 0 end as Selected
                            from Asset A with (nolock)
							inner join AssetType T with (nolock) on T.ID = A.AssetTypeID
							cross apply dbo.GetAssetDisplayValueById(a.ID) D
                            left join [Intersect] I on I.IntersectTypeID = @intersectTypeID and 
	                            ((I.[Subject] = A.[Object] and I.SubjectID = A.ObjectID and I.[Object] = @fieldObject and I.ObjectID = @fieldObjectID) or
	                            (I.[Object] = A.[Object] and I.ObjectID = A.ObjectID and I.[Subject] = @fieldObject and I.SubjectID = @fieldObjectID))
                            where T.[Object] = @obj and T.ObjectID = @objID and (@query is null or D.DisplayValue like '%' + @query + '%')
                            order by 3 desc, D.DisplayValue asc
                            OFFSET @offset ROWS FETCH NEXT @rows ROWS ONLY";
                    }

                    break;
                case "ArtifactType":
                case "PolicyType":
                case "RuleType":
                case "TaxonomyType":
                    countSql = @"select count(*) from AssetWithType A with (nolock)
                        cross apply GetAssetTextPathById(A.ID, '.') P 
                        where A.[Type] = @obj and A.TypeID = @objID and (@query is null or P.TextPath like '%' + @query + '%')";
                    sql = @"select A.ObjectID as Value, P.TextPath as Text, case when I.ID is not null then 1 else 0 end as Selected 
                            from AssetWithType A with (nolock)
                            cross apply GetAssetTextPathById(A.ID, '.') P 
                            left join [Intersect] I on I.IntersectTypeID = @intersectTypeID and 
	                            ((I.[Subject] = A.[Object] and I.SubjectID = A.ObjectID and I.[Object] = @fieldObject and I.ObjectID = @fieldObjectID) or
	                            (I.[Object] = A.[Object] and I.ObjectID = A.ObjectID and I.[Subject] = @fieldObject and I.SubjectID = @fieldObjectID))
                            where A.[Type] = @obj and A.TypeID = @objID and (@query is null or P.TextPath like '%' + @query + '%')
                            order by 3 desc, P.TextPath asc
                            OFFSET @offset ROWS FETCH NEXT @rows ROWS ONLY";
                    break;
                case "FusionAttributeType":
                    countSql = "select count(*) from FusionAttribute F where FusionAttributeTypeID = @objID and (@query is null or F.TextPath like '%' + @query + '%')";
                    sql = @"select F.ID as Value, F.TextPath as Text, case when I.ID is not null then 1 else 0 end as Selected   
                            from FusionAttribute F with (nolock)
                            left join [Intersect] I on I.IntersectTypeID = @intersectTypeID and
                                ((I.[Subject] = 'FustionAttribute' and I.SubjectID = F.ID and I.[Object] = @fieldObject and I.ObjectID = @fieldObjectID) or
                                (I.[Object] = 'FustionAttribute' and I.ObjectID = F.ID and I.[Subject] = @fieldObject and I.SubjectID = @fieldObjectID))
                            where F.FusionAttributeTypeID = @objID and (@query is null or F.TextPath like '%' + @query + '%')
                            order by 3 desc, TextPath asc
                            OFFSET @offset ROWS FETCH NEXT @rows ROWS ONLY";
                    break;
                case "ResourceType":
                    countSql = "select count(*) from reporting.Global_Resource R where (@query is null or R.LastName + ', ' + R.FirstName like '%' + @query + '%')";
                    sql = @"select R.ResourceID as Value, R.LastName + ', ' + R.FirstName as Text, case when I.ID is not null then 1 else 0 end as Selected 
                            from reporting.[Global_Resource] R
                            left join [Intersect] I on I.IntersectTypeID = @intersectTypeID and
	                            ((I.[Subject] = 'Resource' and I.SubjectID = F.ID and I.[Object] = @fieldObject and I.ObjectID = @fieldObjectID) or
	                            (I.[Object] = 'Resource' and I.ObjectID = F.ID and I.[Subject] = @fieldObject and I.SubjectID = @fieldObjectID))
                            where (@query is null or R.LastName + ', ' + R.FirstName like '%' + @query + '%')
                            order by 3 desc, R.LastName + ', ' + R.FirstName asc  
                            OFFSET @offset ROWS FETCH NEXT @rows ROWS ONLY";
                    break;
            }

            if (offset == 0 || query != null)
                count = Query<int>(countSql, new { obj, objID, query }).FirstOrDefault();

            List<dynamic> selected = null, items = null;

            if (includeSelection)
                selected = Query<dynamic>(selectedSql, new { obj = @object, objID = objectID, intersectTypeID = intersectType.ID }).ToList();
            if (!includeSelection)
                items = Query<dynamic>(sql, new { offset, rows, query, obj, objID, fieldObject = @object ?? obj, fieldObjectID = objectID ?? objID, intersectTypeID = intersectType.ID }).ToList();

            var dict = new Dictionary<string, object>();

            if (includeSelection)
                dict.Add("Selection", selected.ToList());
            if (!includeSelection)
                dict.Add("Items", items.ToList());
            dict.Add("Count", count);

            return dict; 
        }

        #region Fusion

        public Dictionary<string, object> GetFusionAsDictionary(int id)
        {
            var item = GetById<Fusion>(id, i => i.FusionFilters);
            var sType = SystemObjects.Fusion.ToString();
            var fields = Filter<FieldWithRelation>(i => i.ObjectType == sType && i.ObjectID == item.ID && i.IsListable).ToList();

            var model = new Dictionary<string, object>();
            model.Add("ID", item.ID);
            model.Add("FusionTypeID", item.FusionTypeID);
            model.Add("Name", item.Name);
            model.Add("Enabled", item.Enabled);
            model.Add("Manual", item.Manual);
            if (item.ForceRefresh.HasValue)
            {
                if (item.ForceRefresh.Value)
                    model.Add("ForceRefresh", item.ForceRefresh.Value);
            }
            foreach (var n in fields.Where(f => f.ObjectID == item.ID).OrderBy(f => f.SortOrder))
            {
                model.Add(n.Name, n.FormattedValue);
            }

            if (item.FusionFilters.Count > 0)
            {
                model.Add("Filters", item.FusionFilters.Select(i => new { i.FusionAttributeTypeID, i.Filter }).ToDictionary(k => k.FusionAttributeTypeID, v => v.Filter));
            }

            bool hasDashboards = Filter<Report>(x => x.ObjectType == "FusionType" && x.ObjectID == item.FusionTypeID && x.ReportType == "powerbi").Any();
            model.Add("HasDashboards", hasDashboards);

            return model;
        }

        public List<FusionOwnerOption> GetFusionOwnerOptions()
        {
            return Database.Connection.Query<FusionOwnerOption>(@"
	select	ASTT.Name as [Type],
			AST.ObjectID as ID,
			ASTT.Name + ' : ' + D.DisplayValue as Name
	from	
			Asset AST
			inner join AssetType ASTT on ASTT.ID = AST.AssetTypeID and ASTT.CanOwnFusion = 1
			inner join ArtifactType T on (ASTT.ObjectID = T.ID and ASTT.[Object] = 'ArtifactType'  )			
            cross apply GetAssetDisplayValueById(AST.ID) D
	order by	ASTT.Name + ' : ' + D.DisplayValue").ToList();
        }

        public List<FusionPromotionOption> GetFusionPromotionOptions()
        {
            return Query<FusionPromotionOption>(@"
select	'ArtifactType' as PromotionObjectType, 
		ID as PromotionObjectID, 
		'Glossary: ' + Name as Name, 
		ParentID as ParentObjectTypeID
from	ArtifactType 
union
select 'TaxonomyType' as PromotionObjectType, 
		ID as PromotionObjectID, 
		'Information Model: ' + Name as Name, 
		ID as ParentObjectTypeID
from	TaxonomyType
").OrderBy(i => i.Name).ToList();
        }

        public List<FusionAttributeItem> GetAttributesByFusion(int fusionID)
        {
            string k = key(FUSIONATTRIBUTES_BY_FUSION_PREFIX_KEY, fusionID);
            if (Caching.ItemExists<List<FusionAttributeItem>>(k))
            {
                return Caching.GetItem<List<FusionAttributeItem>>(k);
            }
            else
            {

                string query = string.Format("fusion.GetAttributesByFusion {0}", fusionID);
                var list = Database.Connection.Query<FusionAttributeItem>(query).ToList();
                Caching.SetItem<List<FusionAttributeItem>>(k, list);

                return list;
            }
        }

        #endregion

        class LookupFieldValueModel
        {
            public int ID { get; set; }
            public string Name { get; set; }
            public int SortOrder { get; set; }
            public int ObjectID { get; set; }
            public string FormattedValue { get; set; }
        }

        public List<Dictionary<string, object>> GetLookupItemsAsDictionary(int typeID)
        {
            var items = new List<Dictionary<string, object>>();

            var values = Filter<Lookup>(i => i.LookupTypeID == typeID).ToList();
            
            var lookupIDs = values.Select(i => i.ID).ToList();
            var sType = SystemObjects.Lookup.ToString();
            
            // you cant use fields with relation cause this is called from setup page and cache is not updated instananiously
            var fields = new List<LookupFieldValueModel>();

            int pageSize = 2000;
            int pageNumbers = lookupIDs.Count / pageSize;

            pageNumbers += ((lookupIDs.Count % pageSize) > 0 ) ? 1 : 0;

            for(var i = 0; i< pageNumbers; i++)
            {
                var subList = pageNumbers > 1 ? lookupIDs.Skip(i * pageSize).Take(pageSize) : lookupIDs;
                fields.AddRange(Query<LookupFieldValueModel>(@"
                        select 
                            ft.ID,
                            ft.Name,
	                        ft.SortOrder,
	                        f.ObjectID,	
	                        f.FormattedValue
                        from 
	                        [FieldType] ft
	                        inner join [field] f on (f.fieldTypeID = ft.id)
                        where 
	                        f.[objecttype] = @ty and f.objectid in @ids;
                    "
                    , new { ids = subList, ty = sType }));
            }

            values.ForEach(e =>
            {
                var item = new Dictionary<string, object>();

                item.Add("ID", e.ID.ToString());
                foreach (var field in fields.Where(i => i.ObjectID == e.ID).OrderBy(i => i.SortOrder))
                {
                    var fieldName = $"Field{field.ID}";
                    if (!item.ContainsKey(fieldName)) item.Add(fieldName, field.FormattedValue);
                }

                items.Add(item);
            });

            return items;
        }

        public AssetDetail GetAssetDetail(long id)
        {
            var model = Query<AssetDetail>(@"
select	ID,
		DisplayValue,
		AssetTypeID,
		State,
		Object,
		ObjectID,
		TypeName as AssetTypeName,
		Type,
	    TypeID
from	AssetDetail
where   ID = @id", new { id }).SingleOrDefault();

            return model;
        }

        public AssetDetail GetAssetDetail(string objectType, long objectId)
        {
            var model = Query<AssetDetail>(@"
select	ID,
		DisplayValue,
		AssetTypeID,
		State,
		Object,
		ObjectID,
		TypeName as AssetTypeName,
		Type,
	    TypeID
from	AssetDetail
where   [ObjectID] = @id and [Object] = @type", new { id = objectId, type = objectType }).SingleOrDefault();

            return model;
        }

        public ObjectDetail GetObjectDetail(string type, long id)
        {
            string query = string.Format("SELECT * FROM utility.ObjectDetail('{0}', {1})", type, id);
            var model = Database.SqlQuery<ObjectDetail>(query).SingleOrDefault();
            var neutralCulture = Thread.CurrentThread.CurrentCulture.Parent.Name;

            if ((model != null) && PluralCultureHelper.IsNeutralCultureEnglish())
            {
                var pluralize = PluralizationService.CreateService(System.Globalization.CultureInfo.CurrentCulture);
                model.PluralizedName = pluralize.Pluralize(model.Name??"");
                pluralize = null;
            }
            return model;
        }

        public string GetObjectTypePath(string type, long id)
        {
            string query = string.Format("SELECT utility.GetObjectTypePath('{0}', {1}) as path", type, id);
            return Database.SqlQuery<string>(query).SingleOrDefault();            
        }

        public ObjectStyle GetObjectStyle(string type, int id)
        {
            return Filter<ObjectStyle>(i => i.ObjectType == type && i.ObjectID == id).FirstOrDefault();
        }

        public ObjectStyle GetObjectStyle(SystemObjects type, int id)
        {
            return GetObjectStyle(type.ToString(), id);
        }

        public AssetType GetParentType(int id, SystemObjects obj)
        {
            if ( id < 0)
                return default(AssetType);

            var sql = @"select a.id from IntersectType I
                               inner join [Predicate] P on P.ID = I.PredicateID
            inner join AssetType a on a.object = i.subject and a.objectid = i.subjectid
                               where P.[Type] = @type and i.[Object] = @object and i.[ObjectID] = @objectId";

            var parentId = Query<int>(sql, new { type = (int)PredicateType.InterTypeHierarchy, @object = obj.ToString(), objectId = id }).FirstOrDefault();

            if (parentId < 1)
                return default(AssetType);

            return GetById<AssetType>(parentId);
        }

        public bool UpdateObjectParentRelationship(SystemObjects type, int typeId, SystemObjects objectType, int parentID, int objectID, PredicateType predicateType = PredicateType.InterTypeHierarchy)
        {
            var intersectType = Filter<IntersectTypeDetail>(i =>
                        i.Object == type.ToString() &&
                        i.ObjectID == typeId &&
                        i.PredicateType.Value == predicateType
                    ).SingleOrDefault();

            if (intersectType != null)
            {
                var intersect = Intersects.FirstOrDefault(x => x.IntersectTypeID == intersectType.ID && x.Object == objectType.ToString() && x.ObjectID == objectID);

                if (intersect == null)
                    return AddObjectParentRelationship(type, typeId, objectType, parentID, objectID, predicateType);

                var parentExists = Any<Asset>(i =>
                    i.ObjectID == parentID &&
                    i.AssetType.Object == type.ToString() &&
                    i.AssetType.ObjectID == intersectType.SubjectID
                    );

                if (!parentExists)
                {
                    return false;
                }

                intersect.Subject = type.ToString().Replace("Type","");
                intersect.SubjectID = parentID;

                return SaveOrUpdate<Intersect>(intersect) > 0;
                
            }

            return true;
        }

        public bool AddObjectParentRelationship(SystemObjects type, int typeId, SystemObjects objectType, int parentID, int objectID, PredicateType predicateType = PredicateType.InterTypeHierarchy)
        {
            var intersectType = Filter<IntersectTypeDetail>(i =>
                        i.Object == type.ToString() &&
                        i.ObjectID == typeId &&
                        i.PredicateType.Value == predicateType
                    ).SingleOrDefault();

            if (intersectType != null)
            {
                var intersect = new Intersect
                {
                    Subject = objectType.ToString(),
                    SubjectID = parentID,
                    Object = objectType.ToString(),
                    ObjectID = objectID,
                    IntersectTypeID = intersectType.ID
                };

                var parentExists = Any<Asset>(i =>
                    i.ObjectID == intersect.SubjectID &&
                    i.AssetType.Object == type.ToString() &&
                    i.AssetType.ObjectID == intersectType.SubjectID
                    );

                if (!parentExists)
                {
                    return false;
                }

                return Add(intersect);
            }

            return true;
        }

        public IntersectType GetHierarchyIntersectType(SystemObjects objectType, int subjectId, int objectId, PredicateType predicateType = PredicateType.InterTypeHierarchy)
        {
            var @sql = @"select I.* from IntersectType I
                inner join Predicate P on P.ID = I.PredicateID
                where I.Subject = @objectType and I.SubjectID = @subjectId and I.Object = @objectType and I.ObjectID = @objectId and P.PredicateType = @type";

            var intersectType = Query<IntersectType>(sql, new { objectType, subjectId, objectId, type = (int)predicateType }).FirstOrDefault();

            return intersectType;
        }

        public IEnumerable<AssetType> GetChildTypes(int id, SystemObjects obj) 
        {
            
            var sql = @"select I.ObjectID from IntersectType I
                    inner join [Predicate] P on P.ID = I.PredicateID
                    where P.[Type] = @type and [Subject] = @object and SubjectID = @objectId";
            var childIds = Query<int>(sql, new { type = (int)PredicateType.InterTypeHierarchy, @object = obj.ToString(), objectId = id });

            if (!childIds.Any())
                return new List<AssetType>();

            return childIds.Select(t => GetById<AssetType>(t));
        }

        public bool TypeHasParent(SystemObjects type, int id)
        {

            var sql = @"select 1 from IntersectType I
                    inner join [Predicate] P on P.ID = I.PredicateID
                    where P.[Type] = @type and [Object] = @object and ObjectID = @objectId";

            return Query<dynamic>(sql, new { type = (int)PredicateType.InterTypeHierarchy, @object = type.ToString(), objectId = id }).Any();
        }

        public bool TypeHasChildren(SystemObjects type, int id)
        {

            var sql = @"select 1 from IntersectType I
                    inner join [Predicate] P on P.ID = I.PredicateID
                    where P.[Type] = @type and [Subject] = @object and SubjectID = @objectId";

            return Query<dynamic>(sql, new { type = (int)PredicateType.InterTypeHierarchy, @object = type.ToString(), objectId = id }).Any();
        }

        public bool ObjectHasParent(SystemObjects type, int id)
        {
            var sql = @"select 1 from PredicateIntersect I
                    inner join IntersectType T on T.ID = I.IntersectTypeID
                    where I.PredicateType = @type and I.[Object] = @object and I.ObjectID = @objectId";

            return Query<dynamic>(sql, new { type = (int)PredicateType.InterTypeHierarchy, @object = type.ToString(), objectId = id }).Any();
        }

        public bool ObjectHasChildren(SystemObjects type, int id)
        {
            var sql = @"select 1 from PredicateIntersect I
                    inner join IntersectType T on T.ID = I.IntersectTypeID
                    where I.PredicateType = @type and I.[Subject] = @object and I.SubjectID = @objectId";

            return Query<dynamic>(sql, new { type = (int)PredicateType.InterTypeHierarchy, @object = type.ToString(), objectId = id }).Any();
        }

        public AssetDetail GetParentObject(int id, SystemObjects obj) { 
            //string type = "";
            var predicateType = PredicateType.InterTypeHierarchy;


            switch (obj)
            {
                case SystemObjects.Policy:
                    predicateType = PredicateType.IntraTypeHierarchy;
                    break;
                case SystemObjects.Taxonomy:
                    predicateType = PredicateType.IntraTypeHierarchy;
                    break;
                default:
                    predicateType = PredicateType.InterTypeHierarchy;
                    break;

            }


            if ( id < 0)
                return default(AssetDetail);

            var sql = @"select a.Id from PredicateIntersect I
                    inner join IntersectType T on T.ID = I.IntersectTypeID
                    inner join Asset a on a.object = i.subject and a.objectid = i.subjectid
                    where I.PredicateType = @type and I.[Object] = @object and I.ObjectID = @objectId";

            var parentId = Query<int>(sql, new { type = (int)predicateType, @object = obj.ToString(), objectId = id }).FirstOrDefault();
            if (parentId < 1)
                return default(AssetDetail);

              return Filter<AssetDetail>(i => i.ID == parentId).FirstOrDefault();
        }



        public bool IsUserFollowing(SystemObjects type, int objectID, int? resourceID)
        {
            if (!resourceID.HasValue)
            {
                resourceID = CurrentResourceID;
            }
            string sType = type.ToString();

            var follow = Any<FollowDetail>(i => i.ResourceID == resourceID && i.ObjectID == objectID && i.ObjectType == sType);

            return follow;
        }

        public bool IsUserFollowingParent(SystemObjects type, int objectID, int? resourceID)
        {
            return (GetFollowingParent(type,objectID,resourceID) != null);
        }

        public Follow GetFollowingParent(SystemObjects type, int objectID, int? resourceID)
        {
            if (!resourceID.HasValue)
            {
                resourceID = CurrentResourceID;
            }
            string sType = type.ToString();

            var fd = Filter<FollowDetail>(i => i.ResourceID == resourceID && i.ObjectID == objectID && i.ObjectType == sType).FirstOrDefault();
            if (fd != null)
            {
                var followID = fd.FollowID;
                return GetById<Follow>(followID);
            }
            else
            {
                return null;
            }
        }

        public bool UpdateFollowStatus(SystemObjects type, int objectID, int? resourceID, bool includeChildren = false)
        {
            if (!resourceID.HasValue)
            {
                resourceID = CurrentResourceID;
            }

            bool value = false;

            string sType = type.ToString();
            var f = Filter<Follow>(i => i.ObjectID == objectID && i.ObjectType == sType && i.ResourceID == resourceID).FirstOrDefault();

            if (f != null)
            {
                Delete<Follow>(f);
                value = false;
            }
            else
            {
                if (IsUserFollowingParent(type, objectID, resourceID.Value) && !IsUserFollowing(type, objectID, resourceID.Value))
                {
                    //the user is following a parent of this item
                }
                else
                {
                    FollowType followType;
                    switch (type)
                    {
                        case SystemObjects.ArtifactType:
                        case SystemObjects.PolicyType:
                        case SystemObjects.ResourceType:
                        case SystemObjects.TaxonomyType:
                            followType = FollowType.Parent;
                            break;
                        case SystemObjects.Artifact:
                        case SystemObjects.Taxonomy:
                        case SystemObjects.Group:
                        case SystemObjects.Resource:
                        default:
                            followType = FollowType.Single;
                            break;
                    }

                    if (includeChildren || objectID == 0)
                        followType = FollowType.Parent;

                    var pObjectID = new SqlParameter("id", objectID);
                    var pType = new SqlParameter("type", sType);
                    var pResourceID = new SqlParameter("resourceID", resourceID);
                    var pFollowTypeID = new SqlParameter("followTypeID", followType);
                    var pIncludeChildren = new SqlParameter("includeChildren", includeChildren);

                    Database.ExecuteSqlCommand("FollowObject @id, @type, @resourceID, @followTypeID, @includeChildren", pObjectID, pType, pResourceID, pFollowTypeID, pIncludeChildren);

                    value = true;
                }
            }
            return value;
        }

        #region Relationships

        public IntersectDetail AddIntersect(int intersectTypeID, string subject, int subjectID, string @object, int objectID)
        {
            Intersect intersect = null;
            IntersectDetail dtl = null;

            var subjectDetail = GetObjectDetail(subject, subjectID);
            var objectDetail = GetObjectDetail(@object, objectID);

            if (subjectDetail == null)
                throw new NotFoundException("Subject");

            if (objectDetail == null)
                throw new NotFoundException("Object");

            if (subject == "ReferenceItemType")
                subjectDetail.TypeID = 0;

            if (@object == "ReferenceItemType")
                objectDetail.TypeID = 0;

            var intersectType = GetById<IntersectType>(intersectTypeID);

            if (intersectType == null)
                throw new NotFoundException("Intersect Type");

            if (
                (intersectType.Subject == subjectDetail.Type && intersectType.SubjectID == subjectDetail.TypeID && intersectType.Object == objectDetail.Type && intersectType.ObjectID == objectDetail.TypeID) ||
                (intersectType.Subject == objectDetail.Type && intersectType.SubjectID == objectDetail.TypeID && intersectType.Object == subjectDetail.Type && intersectType.ObjectID == subjectDetail.TypeID)
                )
            {
                dtl = Filter<IntersectDetail>(i => i.IntersectTypeID == intersectType.ID && (
                        (i.Subject == subject && i.SubjectID == subjectID && i.Object == @object && i.ObjectID == objectID) ||
                        (i.Object == subject && i.ObjectID == subjectID && i.Subject == @object && i.SubjectID == objectID)
                    )
                ).SingleOrDefault();

                if (dtl == null)
                {
                    intersect = new Intersect { IntersectTypeID = intersectType.ID };

                    if (subjectDetail.Type == intersectType.Subject && subjectDetail.TypeID == intersectType.SubjectID)
                    {
                        intersect.Subject = subject;
                        intersect.SubjectID = subjectID;
                        intersect.Object = @object;
                        intersect.ObjectID = objectID;

                        Intersects.Add(intersect);
                    }
                    else
                    {
                        intersect.Subject = @object;
                        intersect.SubjectID = objectID;
                        intersect.Object = subject;
                        intersect.ObjectID = subjectID;

                        Intersects.Add(intersect);
                    }

                    SaveChanges();

                    dtl = Filter<IntersectDetail>(i => i.ID == intersect.ID).FirstOrDefault();
                }

                return dtl;
            }
            else
            {
                throw new NotFoundException("Intersect Type");
            }
        }

        public IntersectDetail AddIntersect(int intersectTypeID, SystemObjects subject, int subjectID, SystemObjects @object, int objectID)
        {
            Intersect intersect = null;
            IntersectDetail dtl = null;

            var sSubject = subject.ToString();
            var sObject = @object.ToString();

            var subjectDetail = GetObjectDetail(subject.ToString(), subjectID);
            var objectDetail = GetObjectDetail(@object.ToString(), objectID);

            if (subjectDetail == null)
                throw new NotFoundException("Subject");

            if (objectDetail == null)
                throw new NotFoundException("Object");

            var intersectType = GetById<IntersectType>(intersectTypeID);

            if (intersectType == null)
                throw new NotFoundException("Intersect Type");

            if  (
                (intersectType.Subject == subjectDetail.Type && intersectType.SubjectID == subjectDetail.TypeID && intersectType.Object == objectDetail.Type && intersectType.ObjectID == objectDetail.TypeID) ||
                (intersectType.Subject == objectDetail.Type && intersectType.SubjectID == objectDetail.TypeID && intersectType.Object == subjectDetail.Type && intersectType.ObjectID == subjectDetail.TypeID)
                )
            {
                dtl = Filter<IntersectDetail>(i => i.IntersectTypeID == intersectType.ID && (
                        (i.Subject == sSubject && i.SubjectID == subjectID && i.Object == sObject && i.ObjectID == objectID) ||
                        (i.Object == sSubject && i.ObjectID == subjectID && i.Subject == sObject && i.SubjectID == objectID)
                    )
                ).SingleOrDefault();

                if (dtl == null)
                {
                    intersect = new Intersect { IntersectTypeID = intersectType.ID };

                    if (subjectDetail.Type == intersectType.Subject && subjectDetail.TypeID == intersectType.SubjectID)
                    {
                        intersect.Subject = sSubject;
                        intersect.SubjectID = subjectID;
                        intersect.Object = sObject;
                        intersect.ObjectID = objectID;

                        Intersects.Add(intersect);
                    }
                    else
                    {
                        intersect.Subject = sObject;
                        intersect.SubjectID = objectID;
                        intersect.Object = sSubject;
                        intersect.ObjectID = subjectID;

                        Intersects.Add(intersect);
                    }

                    SaveChanges();

                    dtl = Filter<IntersectDetail>(i => i.ID == intersect.ID).FirstOrDefault();
                }

                return dtl;
            }
            else
            {
                throw new NotFoundException("Intersect Type");
            }
        }

        public bool DeleteRelationship(int id)
        {
            var item = GetById<Intersect>(id);
            if (item == null) throw new NotFoundException("Relationship");
            var res = Database.ExecuteSqlCommand("DeleteIntersect {0}, {1}", id, CurrentResourceID) > 0;

            // add record to queue indication of delete relationship

            QueueSource.CreateTopicMessage(new core.queue.EventInfo
            {
                CompanyID = CurrentCompanyID,
                Action = core.enums.Workflow.ChangeType.Delete,
                ResourceID = CurrentResourceID,
                Object = new core.queue.EventObjectInfo
                {
                    Object = SystemObjects.Intersect,
                    ObjectID = id,
                    ObjectType = SystemObjects.IntersectType,
                    ObjectTypeID = item.IntersectTypeID
                },
                DomainPrefix = CurrentCompanyDomain
            });

            return res;
        }

        public class DetailDisplayableRelationship
        {
            public string SourceObject { get; set; }
            public int SourceObjectID { get; set; }
            public string TargetObject { get; set; }
            public int TargetObjectID { get; set; }
            public string TargetObjectName { get; set; }
            public string TargetTypeName { get; set; }
            public int Count { get; set; }
            public string TargetUrl { get; set; }
        }

        public List<DetailDisplayableRelationship> GetDetailDisplayableRelationships(SystemObjects type, int id)
        {
            return Query<DetailDisplayableRelationship>(@"
 select	SourceObject,
		SourceObjectID,
		TargetObject,
		TargetObjectID,
		FA.TextPath as TargetObjectName,
		FAT.TextPath as TargetTypeName,
		C.[Count],
		AUrl.[Url] as TargetUrl
from	cache.Relationships R
		inner join AssetDetail D on D.[Object] = R.TargetObject and D.ObjectID = R.TargetObjectID
		inner join FusionAttribute FA on FA.ID = R.TargetObjectID
		inner join FusionAttributeType FAT on FAT.ID = FA.FusionAttributeTypeID
		cross apply [dbo].[GetAssetUrlById](D.ID) AUrl
		outer apply (
					select	count(1) as [Count]
					from	FusionAttributeType
					where	ParentID = R.TargetTypeID
					) C
where	R.SourceObject = 'FusionAttribute'
		and R.TargetObject = 'FusionAttribute'
        and R.SourceObjectID = @id
        and R.TargetTypeID = 302", new { type = new Dapper.DbString { Value = type.ToString(), IsAnsi = true }, id }).ToList();
        }

        public List<IntersectTypeOption> GetIntersectTypeOptions(
            SystemObjects? subject = null, int? subjectID = null, 
            SystemObjects? @object = null, int? objectID = null,
            int? predicateID = null)
        {
            var sql = @"
    SELECT		I.ID,
				I.Name,
				I.Type
	FROM		(
                select	T.ObjectID as ID,
		                case T.Object
			                when 'ArtifactType' then 'Glossary :: '
			                when 'FusionAttributeType' then 'Fusion Attribute :: ' + FT.Name + ' / '
			                when 'FusionQueryAttributeType' then 'Fusion Query :: '
			                when 'GroupType' then 'Security :: '
			                when 'PolicyType' then 'Policy :: '
			                when 'ReferenceItemType' then 'Reference :: '
			                when 'ResourceType' then 'Security :: '
			                when 'RuleType' then 'Rule :: '
			                when 'TaxonomyType' then 'Model :: '
		                end + coalesce(P.[Path], T.Name) as Name,
		                T.Object as Type
                from	AssetType T
		                cross apply dbo.GetAssetTypeTextPathById(T.ID, '/') P
                        left join FusionAttributeType FAT on T.Object = 'FusionAttributeType' and FAT.ID = T.ObjectID 
                        left join FusionType FT on FT.ID = FAT.FusionTypeID 
                where	T.Object not in ('AttributeType', 'FusionType', 'OrganizationType')
                UNION
                SELECT	CAST(IT.ID as int) ID,
		                'Relationship :: ' + ITypeName.Name AS Name,
		                'IntersectType' AS Type
                FROM	IntersectType IT    
		                cross apply dbo.GetIntersectTypeNames(IT.ID) ITypeName				
                UNION
                SELECT	ID,
		                'Rule Implementation :: ' + DisplayValue as Name,
		                'RuleImplementationType' as Type
                FROM    [Rule]
                UNION
                SELECT	0 as ID,
		                'Reference :: List' as Name,
		                'ReferenceItemType' as Type				 				
                ) I";

            if (subject.HasValue && subjectID.HasValue)
            {
                sql += $@" left join IntersectType T on 
(T.Subject = '{subject.Value.ToString()}' and T.SubjectID = {subjectID.Value} and T.Object = I.[Type] and T.ObjectID = I.ID)";
                if ((@object.HasValue && objectID.HasValue) || predicateID.HasValue)
                {
                    if (@object.HasValue && objectID.HasValue)
                    {
                        if (predicateID.HasValue)
                        {
                            sql += $@" and ((T.Object = '{@object.Value.ToString()}' and T.ObjectID = {objectID.Value} and T.PredicateID = {predicateID.Value}) or T.ID is null)";
                        }
                        else
                        {
                            sql += $@" and ((T.Object = '{@object.Value.ToString()}' and T.ObjectID = {objectID.Value}) or T.ID is null)";
                        }
                    }
                    else
                    {
                        if (predicateID.HasValue)
                        {
                            sql += $@" and T.PredicateID = {predicateID.Value} where T.ID is null";
                        }
                    }
                }
            }

            sql += " ORDER BY I.Name";

            return Database.Connection.Query<IntersectTypeOption>(sql).ToList();
        }

        #endregion

        #region Social

        public IQueryable<CommentDetail> EditComment(Comment comment, ICollection<CommentRelation> relations)
        {            
            var now = DateTime.UtcNow;            
            if (relations == null)
                relations = new List<CommentRelation>();

            var removeRelations = Filter<CommentRelation>(t => t.CommentID == comment.ID && !(t.ObjectType == "Resource" && t.ObjectID == CurrentResourceID )).ToList();

            foreach (var r in removeRelations)
                if (!relations.ToList().Contains(r))
                Set<CommentRelation>().Remove(r);

            foreach (var r in relations)
            {

                try
                {
                    r.Date = now;
                    if (r.CommentID == 0) r.CommentID = comment.ID; //If comment ID is not 0, then a parent comment ID has already been assigned.
                    Set<CommentRelation>().Add(r);
                    SaveChanges();
                }
                catch
                {
                    Set<CommentRelation>().Remove(r);
                }
            }


            Comment c = GetById<Comment>(comment.ID);
            var hasReplies = Any<Comment>(x => x.ParentID == c.ID);
            if (((c.Body != comment.Body || removeRelations.Count() + 1 != relations.Count()) && !hasReplies) || (c.IsDeleted != comment.IsDeleted && (!hasReplies || CurrentResourceIsAdmin)))
            {
                c.IsDeleted = comment.IsDeleted;
                c.Body = comment.Body;
                c.DateEdited = comment.DateEdited;
                SaveChanges();
            }

            var coms = GetCommentDetail(comment.ID).ToList();

            return coms.AsQueryable();
            
        }

        public IQueryable<CommentDetail> AddComment(Comment comment, ICollection<CommentRelation> relations)
        {

            comment.DateCreated = DateTime.UtcNow;
            comment.CreatingResourceID = CurrentResourceID;
            SaveOrUpdate<Comment>(comment);

            foreach (var r in relations)
            {
                try
                {
                    r.Date = comment.DateCreated;
                    if (r.CommentID == 0) r.CommentID = comment.ID; //If comment ID is not 0, then a parent comment ID has already been assigned.
                    Set<CommentRelation>().Add(r);
                    SaveChanges();
                }
                catch
                {
                    Set<CommentRelation>().Remove(r);
                }
            }


            return GetCommentDetail(comment.ID);
        }

        public IQueryable<CommentDetail> GetCommentDetail(int id)
        {
            var comments = (
                    from c in Database.SqlQuery<CommentDetail>("GetCommentDetailByID @id", new SqlParameter("id", id)).ToList()
                    join r in Community.Resources on c.CreatingResourceID equals r.ID
                    select new CommentDetail
                    {
                        Body = c.Body,
                        Comments = c.Comments,
                        CommentTypeID = c.CommentTypeID,
                        CreatingResourceID = c.CreatingResourceID,
                        DateCreated = c.DateCreated,
                        ID = c.ID,
                        ObjectID = c.ObjectID,
                        ObjectName = c.ObjectName,
                        ObjectType = c.ObjectType,
                        ObjectUrl = c.ObjectUrl,
                        ParentID = c.ParentID,
                        ResourceEmail = r.Email,
                        ResourceName = r.FormatDisplayName(),
                        TagsXml = c.TagsXml,
                        VotesXml = c.VotesXml,
                        CreatorIsOwner = c.CreatorIsOwner,
                        DateEdited = c.DateEdited,
                        IsDeleted = c.IsDeleted,
                        IsEditable = (CurrentResourceID == c.CreatingResourceID
                            && (!Any<Comment>(re => re.ParentID == c.ID))
                            && DateTime.UtcNow.Subtract(c.DateCreated).Duration() < TimeSpan.FromMinutes(5)),
                        IsDeletable = (CurrentResourceIsAdmin || (CurrentResourceID == c.CreatingResourceID
                            && (!Any<Comment>(re => re.ParentID == c.ID))
                            && DateTime.UtcNow.Subtract(c.DateCreated).Duration() < TimeSpan.FromMinutes(5)))
                    }
                   );          
            
            return comments.AsQueryable();
        }

        public IQueryable<CommentDetail> GetCommentDetailsByFollower(int resourceID, int skip, int take, int daysToGet = 0, int commentType = 0, string searchPhrase = "")
        {

            DateTime dateStart;
            DateTime dateEnd = DateTime.UtcNow;
            if (daysToGet == 0)
            {
                dateStart = new DateTime(2000, 1, 1);
            }
            else
            {
                dateStart = (daysToGet < 0) ? dateEnd.AddDays(daysToGet) : dateEnd.AddDays(-daysToGet);
            }

            if (searchPhrase == null)
                searchPhrase = "";

            var comments =
                Query<CommentDetail>("GetCommentDetailsByFollower @resourceID, @skip, @take, @dateStart, @dateEnd, @commentTypeID, @searchPhrase",
                new
                {
                    resourceID = resourceID,
                    skip = skip,
                    take = take,
                    dateStart = dateStart,
                    dateEnd = dateEnd,
                    commentTypeID = commentType,
                    searchPhrase = searchPhrase.Replace("'", "''").Replace("--", "")
                });

            foreach (CommentDetail cd in comments)
            {
                cd.IsEditable = (CurrentResourceID == cd.CreatingResourceID
                        && !Any<Comment>(c => c.ParentID == cd.ID)
                        && DateTime.UtcNow.Subtract(cd.DateCreated).Duration() < TimeSpan.FromMinutes(5));
                cd.IsDeletable = (CurrentResourceIsAdmin || cd.IsEditable.Value);

            }

            return comments.AsQueryable();

        }

        public IQueryable<CommentCount> GetCommentCountByFollower(int resourceID, int daysToGet = 0, string searchPhrase = "")
        {
            DateTime dateStart;
            DateTime dateEnd = DateTime.UtcNow;
            if (daysToGet == 0)
            {
                dateStart = new DateTime(2000, 1, 1);
            }
            else
            {
                dateStart = (daysToGet < 0) ? dateEnd.AddDays(daysToGet) : dateEnd.AddDays(-daysToGet);
            }
            return Query<CommentCount>("GetCommentCountByFollower @resourceID, @dateStart, @dateEnd, @searchPhrase", new { resourceID, dateStart, dateEnd, searchPhrase}).AsQueryable();
        }

        public IQueryable<CommentCount> GetCommentCountByType(SystemObjects type,int id, int daysToGet = 0, string searchPhrase = "")
        {
            DateTime dateStart;
            DateTime dateEnd = DateTime.UtcNow;
            if (daysToGet == 0)
        {
                dateStart = new DateTime(2000, 1, 1);
        }
            else
        {
                dateStart = (daysToGet < 0) ? dateEnd.AddDays(daysToGet) : dateEnd.AddDays(-daysToGet);
            }
            return Query<CommentCount>("GetCommentCountByType @type, @id, @dateStart, @dateEnd, @searchPhrase", new { type = new Dapper.DbString { Value = type.ToString(), IsAnsi = true }, id, dateStart, dateEnd, searchPhrase }).AsQueryable();
        }

        public IQueryable<CommentVote> VoteComment(int CommentID, int ResourceID, int Vote)
        {
            return Query<CommentVote>("VoteComment @CommentID, @ResourceID, @Vote",new { CommentID, ResourceID, Vote }).AsQueryable();
        }

        public IQueryable<CommentDetail> GetCommentDetailsByType(SystemObjects type, int id, int skip, int take, int daysToGet = 0, int commentType = 0, string searchPhrase = "")
        {

            DateTime dateStart;
            DateTime dateEnd = DateTime.UtcNow;
            if (daysToGet == 0)
            {
                dateStart = new DateTime(2000, 1, 1);
            }
            else
            {
                dateStart = (daysToGet < 0) ? dateEnd.AddDays(daysToGet) : dateEnd.AddDays(-daysToGet);
            }

            if (searchPhrase == null)
                searchPhrase = "";

            var comments =
                Query<CommentDetail>("GetCommentDetailsByType @type, @id, @skip, @take, @dateStart, @dateEnd, @commentTypeID, @searchPhrase",
                new {
                    type = new Dapper.DbString { Value = type.ToString(), IsAnsi = true },
                    id = id,
                    skip = skip,
                    take = take,
                    dateStart = dateStart,
                    dateEnd = dateEnd,
                    commentTypeID = commentType,
                    searchPhrase = searchPhrase.Replace("'", "''").Replace("--", "")
                }).ToList();
            foreach (CommentDetail cd in comments)
            {
                cd.IsEditable = (CurrentResourceID == cd.CreatingResourceID
                        && !Any<Comment>(c => c.ParentID == cd.ID)
                        && DateTime.UtcNow.Subtract(cd.DateCreated).Duration() < TimeSpan.FromMinutes(5));
                cd.IsDeletable = (CurrentResourceIsAdmin || cd.IsEditable.Value);

            }

            return comments.AsQueryable();
        }

        public IQueryable<CommentDetail> GetCommentDetailsByID(int id)
        {            
            var comments =
                Query<CommentDetail>("GetCommentDetailByID @id",
                new
                {                    
                    id = id                    
                });
            foreach (CommentDetail cd in comments)
            {
                cd.IsEditable = (CurrentResourceID == cd.CreatingResourceID
                        && !Any<Comment>(c => c.ParentID == cd.ID)
                        && DateTime.UtcNow.Subtract(cd.DateCreated).Duration() < TimeSpan.FromMinutes(5));
                cd.IsDeletable = (CurrentResourceIsAdmin || cd.IsEditable.Value);

            }

            return comments.AsQueryable();
        }

        /// <summary>
        /// Get a list of those following the current object.
        /// </summary>
        public IQueryable<FollowDetail> GetFollowersByObject(SystemObjects type, int id)
        {
            var fs = type.ToString();
       
            var sql = @"select f.* from FollowDetail f
                        inner join reporting.Global_Resource r on
                        r.ResourceID = f.ResourceID
                        where r.State = @userStatus  and objectId=@objectId and objectType = @objectType";
            
            return Query<FollowDetail>(sql, new { userStatus = CompanyResourceState.Active, objectId = id, objectType = fs }).AsQueryable();
        }
                
        #endregion

        #region Token Processing Methods

        private string renderTemplate(string templateType, string action, SystemObjects type, int id)
        {
            var settings = Community.GetCompanySettings();
            
            string query = string.Format("GetRenderedTemplateBodyNg '{0}', '{1}', {2}, '{3}', '{4}', {5}", templateType, type.ToString(), id, action, settings["ArtifactType_TaxonomyTypeID"], CurrentResourceID);
            var model = Database.SqlQuery<RenderTemplateModel>(query).SingleOrDefault();
            var html = "";
            if (model != null) html = model.Body;
            return html;
        }

        public string RenderTooltip(string action, SystemObjects type, int id)
        {
            return renderTemplate("Tooltip", action, type, id);
        }

        #endregion

        #endregion

        #region Generic Methods

        public override bool Add<T>(T item)
        {
            Set<T>().Add(item);
            var returnValue = (SaveChanges() > 0);
            return returnValue;
        }

        /// <summary>
        /// Removes the item(s) from the system, as well as any dynamic fields associated with the item(s), if any.
        /// </summary>
        public override bool Delete<T>(Expression<Func<T, bool>> predicate)
        {
            var items = Filter(predicate).ToList();
            bool allDeleted = true;

            items.ForEach(i =>
            {
                if (!Delete(i))
                {
                    allDeleted = false;
                }
            });

            return allDeleted;
        }

        /// <summary>
        /// Removes the item from the system, as well as any dynamic fields associated with this item, if any.
        /// </summary>
        public override bool Delete<T>(T entity)
        {
            try
            {
                Set<T>().Remove(entity);
                return (SaveChanges() > 0);
            }
            catch (Exception ex)
            {
                throw resolveToRealException(ex);
            }
        }

        /// <summary>
        /// Removes the item from the system, as well as any dynamic fields associated with this item, if any.
        /// </summary>
        public bool Delete(SystemObjects type, int id)
        {
            try
            {
                //needs to be before delete before we say goodbye to our little friend.
                var det = GetObjectDetail(type.ToString(), id);

                Database.Connection.Execute("exec [DeleteObject] @Obj, @ObjectID, @ResourceID", new { Obj = type.ToString(), ObjectID = id, ResourceID = CurrentResourceID }, null, 120);

                // add a removed message to the service bus
                if (IsEventingEnabled)
                {
                    if (det != null)
                    {                        
                        var events = new List<EventInfo>();
                        addQE(events, ChangeType.Delete, new EventObjectInfo
                        {
                            Object = type,
                            ObjectID = id,
                            ObjectType = (SystemObjects)Enum.Parse(typeof(SystemObjects), det.Type),
                            ObjectTypeID = det.TypeID
                        });
                        QueueSource.CreateTopicMessages(events);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                throw resolveToRealException(ex);
            }
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FieldTypeFilteredLookupDisplayField>().HasRequired(t => t.FieldTypeFilteredLookupDefinition).WithMany(t => t.FieldTypeFilteredLookupDisplayFields).HasForeignKey(k => k.FieldTypeFilteredLookupDefinitionID).WillCascadeOnDelete(true);
            modelBuilder.Entity<FieldTypeFusionLookupDisplayField>().HasRequired(t => t.FieldTypeFusionLookupDefinition).WithMany(t => t.FieldTypeFusionLookupDisplayFields).HasForeignKey(k => k.FieldTypeFusionLookupDefinitionID).WillCascadeOnDelete(true);
            modelBuilder.Entity<FieldTypeLookup>().HasRequired(t => t.FieldType).WithOptional(t => t.FieldTypeLookup).WillCascadeOnDelete(true);


            modelBuilder.Entity<FieldType>().Property(x => x.MinimumLength).HasPrecision(38, 18);
            modelBuilder.Entity<FieldType>().Property(x => x.MaximumLength).HasPrecision(38, 18);
            modelBuilder.Entity<FieldType>().Property(x => x.Increment).HasPrecision(38, 18);

            modelBuilder.Entity<FieldWithRelation>().Property(x => x.MinimumLength).HasPrecision(38, 18);
            modelBuilder.Entity<FieldWithRelation>().Property(x => x.MaximumLength).HasPrecision(38, 18);

            modelBuilder.Entity<core.entities.Rule>().Property(x => x.Threshold).HasPrecision(4, 3);

            modelBuilder.Entity<Fusion>().HasMany<Asset>(i => i.FusionOwners).WithMany(i => i.OwnedFusions).Map(i => {
                i.MapLeftKey("FusionID").MapRightKey("AssetID").ToTable("FusionOwner");
            });
            modelBuilder.Entity<Question>().HasMany<QuestionTypeOption>(i => i.QuestionTypeOptions).WithMany(i => i.Questions).Map(i => {
                i.MapLeftKey("QuestionID").MapRightKey("QuestionTypeOptionID").ToTable("QuestionOption");
            });
            modelBuilder.Entity<MapRule>().HasMany<MapRuleItem>(i => i.MapRuleItems).WithMany(i => i.MapRules).Map(i => {
                i.MapLeftKey("MapRuleID").MapRightKey("MapRuleItemID").ToTable("MapRuleItemMapRule");
            });

            modelBuilder.Entity<Map>().HasMany<MapItem>(i => i.MapItems).WithMany(i => i.Maps).Map(i => {
                i.MapLeftKey("MapID").MapRightKey("MapItemID").ToTable("MapItemMap");
            });

            modelBuilder.Entity<FusionRuleStep>().HasRequired(t => t.FusionRule).WithMany(t => t.FusionRuleSteps).HasForeignKey(k => k.RuleID).WillCascadeOnDelete(true);

            base.OnModelCreating(modelBuilder);
        }

        public IEnumerable<T> Query<T>(string sql, object param = null, int timeout = 90)
        {
            return Database.Connection.Query<T>(sql, param, null, false, timeout);
        }

        public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object param = null, int timeout = 90)
        {
            return await Database.Connection.QueryAsync<T>(sql, param, null, timeout);
        }

        public override bool Update<T>(T item)
        {
            ObjectContext.ObjectStateManager.ChangeObjectState(item, EntityState.Modified);
            return (SaveChanges() > 0);
        }

        public bool SaveOrUpdate<T>(T entity, List<Field> fields, int parentId = -1, bool forceUpdate = false) where T : BaseIntObject, IFieldsObject
        {            
            var isUpdate = forceUpdate || IsPersistent(entity);
            
            var fieldsJson = JsonConvert.SerializeObject(fields.Select(f => new { ID = f.FieldTypeID, Value = f.Value }));
            var attr = entity.GetFieldsObjectInfo();
            bool exists = false;

            if (isUpdate)
                exists = Query<bool>("select dbo.CheckIfObjectExistsWithParent(@t, @tid, @oid, @f, 0) as Val", new { t = attr.Type.ToString(), tid = attr.TypeID, oid = entity.ID, f = fieldsJson }).First();
            else
                exists = Query<bool>("select dbo.CheckIfObjectExistsWithParent(@t, @tid, null, @f, @p) as Val", new { t = attr.Type.ToString(), tid = attr.TypeID, f = fieldsJson, p = parentId }).First();

            if (exists)
            {
                throw new ApplicationException($"{attr.Object} already exists.");
            }

            var returnValue = (isUpdate) ? Update<T>(entity) : Add<T>(entity);
            
            if (fields != null)
            {
                fields.ForEach(i => {
                    i.ObjectID = entity.ID;
                });
                AddOrUpdateFields(fields);
            }

            CreateOrUpdateDisplayValue(0,attr.Type.ToString(), entity.ID);

            return returnValue;
        }

        private void CreateOrUpdateDisplayValue(int assetId, string objectType, int objectId)
        {
            Database.Connection.Execute("exec GenerateAssetDisplayValue @assetID, @objType,@objId", new { assetID = assetId, objId = objectId, objType = new DbString { Value = objectType.Replace("Type",""), IsFixedLength = true, Length = 20, IsAnsi = true } }, null, 2400);
        }

        public void CreateOrUpdateTypeDisplayValuesAsync(int objectTypeId, string objectType)
        {
            Enqueue(Config.GetValue<string>("DisplayValueQueue"), new DisplayUpdateInfo { CompanyID = CurrentCompanyID, ObjectTypeID = objectTypeId, ObjectType = objectType });
        }

        public void RebuildDisplayValuesRequest()
        {
            Enqueue(Config.GetValue<string>("DisplayValueQueue"), new DisplayUpdateInfo { CompanyID = CurrentCompanyID, RebuildAll = true });
        }

        private void addQE(List<EventInfo> events, ChangeType action, EventObjectInfo item)
        {
            events.Add(new EventInfo {
                CompanyID = CurrentCompanyID,
                DomainPrefix = CurrentCompanyDomain,
                ResourceID = CurrentResourceID,
                Action = action,
                Object = item        
            });
        }

        public override int SaveChanges()
        {
            int returnValue = 0;
            var fieldsToCheckForChanges = new List<Field>();
            var changedFields = new List<Field>();

            foreach (var entry in ObjectContext.ObjectStateManager.GetObjectStateEntries(EntityState.Added))
            {
                #region Business logic : ICreatedMetadata
                if (entry.Entity is ICreatedMetadata)
                {
                    var o = entry.Entity as ICreatedMetadata;
                    o.CreatedBy = CurrentResourceID;
                    o.CreatedOn = DateTime.UtcNow;
                }
                #endregion
            }

            foreach (var entry in ObjectContext.ObjectStateManager.GetObjectStateEntries(EntityState.Added | EntityState.Modified | EntityState.Deleted))
            {                
                #region Business logic : IUpdatedMetadata
                if (entry.Entity is IUpdatedMetadata)
                {
                    var o = entry.Entity as IUpdatedMetadata;
                    o.UpdatedBy = CurrentResourceID;
                    o.UpdatedOn = DateTime.UtcNow;
                }
                #endregion

                #region Business logic : Artifact
                if (entry.Entity is Artifact)
                {
                    var o = entry.Entity as Artifact;
                    

                    switch (entry.State)
                    {                         
                        case EntityState.Deleted:
                            var any = false;
                            any = Any<Intersect>(i => (i.Subject == "Artifact" && i.SubjectID == o.ID) || (i.Object == "Artifact" && i.ObjectID == o.ID));
                            if (any) throw new ConflictException(string.Format(Messages.Error_NotRemoved_Tokenized, "Artifact"), Messages.Error_Item_RelationshipsReferences);

                            any = ObjectHasChildren(SystemObjects.Artifact, o.ID);
                            if (any) throw new ConflictException(string.Format(Messages.Error_NotRemoved_Tokenized, "Artifact"), Messages.Error_Artifact_ExistingChildren);                            
                            break;                        
                    }

                    Caching.RemoveItem(key(ARTIFACTDICTIONARY_BY_TYPE_PREFIX_KEY, o.ArtifactTypeID));
                }
                #endregion

                #region Business logic : ArtifactType
                if (entry.Entity is ArtifactType)
                {
                    var o = entry.Entity as ArtifactType;
                    
                    switch (entry.State)
                    {
                        case EntityState.Added:
                            if (Any<ArtifactType>(i => i.Name == o.Name))
                                throw new ArgumentException(Messages.Error_NameTaken);                            
                            break;
                        case EntityState.Deleted:
                            if (Any<Artifact>(i => i.ArtifactTypeID == o.ID))
                                throw new ConflictException(string.Format(Messages.Error_NotRemoved_Tokenized, o.Name), Messages.Error_ArtifactsAssignedToType);
                            var childIDs = GetChildTypes(o.ID, SystemObjects.ArtifactType).ToList();
                            if (childIDs.Count > 0)
                                throw new ConflictException(string.Format(Messages.Error_NotRemoved_Tokenized, o.Name), Messages.Error_ChildTypesAssignedToType);
                            
                            break;
                        case EntityState.Modified:
                            if (Any<ArtifactType>(i => i.Name == o.Name && i.ID != o.ID))
                                throw new ArgumentException(Messages.Error_NameTaken);
                            
                            break;
                    }
                }
                #endregion

                #region Business logic : AttributeType
                if (entry.Entity is AttributeType)
                {
                    var o = entry.Entity as AttributeType;
                    
                    switch (entry.State)
                    {
                        case EntityState.Added:
                            if (Any<AttributeType>(i => i.ParentID == o.ParentID && i.Name == o.Name))
                                throw new ArgumentException(Messages.Error_NameTaken);
                            
                            break;
                        case EntityState.Deleted:
                            if (Any<AttributeTypeRelation>(i => i.AttributeTypeID == o.ID))
                                throw new ConflictException(string.Format(Messages.Error_NotRemoved_Tokenized, o.Name), Messages.Error_AttributeType_Allocations);
                            
                            break;
                        case EntityState.Modified:
                            if (Any<AttributeType>(i => i.ParentID == o.ParentID && i.Name == o.Name && i.ID != o.ID))
                                throw new ArgumentException(Messages.Error_NameTaken);
                            
                            break;
                    }
                }
                #endregion

                #region Business logic : Field
                if (entry.Entity is Field)
                {

                    var field = (Field)entry.Entity;

                    field.UpdatedOn = DateTime.UtcNow;

                    if (field.FieldType != null && field.FieldType.Type == "Html")
                    {
                        var sanitizer = new HtmlSanitizer();
                        sanitizer.AllowedSchemes.Add("data");
                        field.Value = sanitizer.Sanitize(field.Value);
                    }

                    // Need to determine if this field value has changed if not we dont want to tell workflow
                    // added items have not changed from the previous value so ignore.
                    // please dont run a query for each modified field if there are 10 we are running 10 queries.  Do one query.
                    if(entry.State != EntityState.Added)
                    {
                        fieldsToCheckForChanges.Add(field);                        
                    }

                }
                #endregion

                #region Business logic : FieldType
                if (entry.Entity is FieldType)
                {
                    var o = entry.Entity as FieldType;
                    
                    switch (entry.State)
                    {
                        case EntityState.Added:
                            if (Any<FieldType>(i => i.Object == o.Object && i.ObjectID == o.ObjectID && i.Name == o.Name))
                                throw new ArgumentException(Messages.Error_NameTaken);
                            break;
                        case EntityState.Modified:
                            if (Any<FieldType>(i => i.Object == o.Object && i.ObjectID == o.ObjectID && i.Name == o.Name && i.ID != o.ID))
                                throw new ArgumentException(Messages.Error_NameTaken);
                            break;
                    }
                }
                #endregion

                #region Business logic : FusionAttributeType
                if (entry.Entity is FusionAttributeType)
                {
                    var o = entry.Entity as FusionAttributeType;
                    
                    switch (entry.State)
                    {
                        case EntityState.Added:
                            if (Any<FusionAttributeType>(i => i.FusionTypeID == o.FusionTypeID && i.ParentID == o.ParentID && i.Name == o.Name))
                                throw new ArgumentException(Messages.Error_NameTaken);
                            break;
                        case EntityState.Modified:
                            if (Any<FusionAttributeType>(i => i.FusionTypeID == o.FusionTypeID && i.Name == o.Name && i.ParentID == o.ParentID && i.ID != o.ID))
                                throw new ArgumentException(Messages.Error_NameTaken);
                            break;
                    }
                }
                #endregion

                #region Business logic : Fusion
                if (entry.Entity is Fusion)
                {
                    var o = entry.Entity as Fusion;
                    
                    switch (entry.State)
                    {
                        case EntityState.Added:
                            if (Any<Fusion>(i => i.Name == o.Name))
                                throw new ArgumentException(Messages.Error_NameTaken);
                            
                            break;
                        case EntityState.Modified:
                            if (Any<Fusion>(i => i.Name == o.Name && i.ID != o.ID))
                                throw new ArgumentException(Messages.Error_NameTaken);
                            
                            break;
                    }
                }
                #endregion

                #region Business logic : FusionType
                if (entry.Entity is FusionType)
                {
                    var o = entry.Entity as FusionType;
                    
                    switch (entry.State)
                    {
                        case EntityState.Added:
                            if (Any<FusionType>(i => i.Name == o.Name))
                                throw new ArgumentException(Messages.Error_NameTaken);
                            
                            break;
                        case EntityState.Modified:
                            if (Any<FusionType>(i => i.Name == o.Name && i.ID != o.ID))
                                throw new ArgumentException(Messages.Error_NameTaken);
                            
                            break;
                    }
                }
                #endregion

                #region Business logic : Group
                if (entry.Entity is Group)
                {
                    var o = entry.Entity as Group;
                    
                    switch (entry.State)
                    {
                        case EntityState.Added:
                            if (Any<Group>(i => i.Name == o.Name))
                                throw new ArgumentException(Messages.Error_NameTaken);
                            
                            break;
                        case EntityState.Modified:
                            if (Any<Group>(i => i.Name == o.Name && i.ID != o.ID))
                                throw new ArgumentException(Messages.Error_NameTaken);
                            
                            break;
                        case EntityState.Deleted:
                            if (Any<ResponsibilityTypeRelationOverrideItem>(i => i.SecurityAsset == "G" && i.SecurityAssetID == o.ID))
                                throw new ConflictException(string.Format(Messages.Error_NotRemoved_Tokenized, o.Name), Messages.Error_ResponsibilitiesAssignedToGroup);
                            
                            break;
                    }
                }
                #endregion

                #region Business logic : Intersect
                if (entry.Entity is Intersect)
                {
                    var o = entry.Entity as Intersect;
                    var id = o.ID.ToString();
                    var intersectTypeID = o.IntersectTypeID;

                    switch (entry.State)
                    {
                        case EntityState.Deleted:
                            var any = Any<Field>(f => f.FieldType.LookupObjectType == "Intersect" && f.FieldType.LookupObjectID == intersectTypeID && f.Value == id);
                            if (any) throw new ConflictException("Relationship Could not be Removed", "One or more fields reference this relationship.");
                            any = Any<core.entities.Attribute>(i => i.ObjectType == "Intersect" && i.ObjectID == o.ID);
                            if (any) throw new ConflictException("Relationship Could not be Removed", "One or more attributes reference this relationship.");
                            any = Any<Intersect>(i => (i.Subject == "Intersect" && i.SubjectID == o.ID) || (i.Object == "Intersect" && i.ObjectID == o.ID));
                            if (any) throw new ConflictException("Relationship Could not be Removed", "One or more relationships reference this relationship.");
                            break;
                    }
                }
                #endregion

                #region Business logic : IntersectType
                if (entry.Entity is IntersectType)
                {
                    var o = entry.Entity as IntersectType;
                    var id = o.ID;

                    var sql = $@"
declare	@id int = {id},
        @s varchar(50) = '{o.Subject}',
		@sid int = {o.SubjectID},
		@sc int = {(int)o.SubjectCardinality},
		@o varchar(50) = '{o.Object}',
		@oid int = {o.ObjectID},
		@oc int = {(int)o.ObjectCardinality},
		@p int = {o.PredicateID},
		@err nvarchar(500) = ''

if exists(select 1 from IntersectType where Subject = @s and SubjectID = @sid and Object = @o and ObjectID = @oid and PredicateID = @p and ( (@id is not null and ID <> @id) OR (@id is null) ) )
begin
	set @err = 'Another relationship already exists with this configuration.'
end

if @err = '' and (@sc = 0 OR @sc = 1) --ONLY ONE, check if multiple already
begin
	if exists(select Object, ObjectID, count(1) as [Count] from [Intersect] where IntersectTypeID = @id group by Object, ObjectID having count(1) > 1)
	begin
		set @err = 'There are objects that are related to more than one subject.'
	end
end

if @err = '' and (@oc = 0 OR @oc = 1) --ONLY ONE, check if multiple already
begin
	if exists(select Subject, SubjectID, count(1) as [Count] from [Intersect] where IntersectTypeID = @id group by Subject, SubjectID having count(1) > 1)
	begin
		set @err = 'There are subjects that are related to more than one object.'
	end
end

select @err";

                    switch (entry.State)
                    {
                        case EntityState.Added:
                            o.uid = Guid.NewGuid();
                            var addCheck = Query<string>(sql).SingleOrDefault();
                            if (!string.IsNullOrEmpty(addCheck))
                                throw new ConflictException("Relationship Type Cannot Be Created", addCheck);
                            break;
                        case EntityState.Modified:
                            var updateCheck = Query<string>(sql).SingleOrDefault();
                            if (!string.IsNullOrEmpty(updateCheck))
                                throw new ConflictException("Relationship Type Cannot Be Updated", updateCheck);
                            break;
                    }
                }
                #endregion

                #region Business logic : Lookup
                if (entry.Entity is Lookup)
                {
                    var o = entry.Entity as Lookup;
                    var id = o.ID.ToString();
                    var lookupTypeID = o.LookupTypeID;

                    switch (entry.State)
                    {
                        case EntityState.Deleted:
                            var any = Any<Field>(f => f.FieldType.LookupObjectType == "Lookup" && f.FieldType.LookupObjectID == lookupTypeID && f.Value == id);
                            if (any) throw new ConflictException("Lookup Could not be Removed", "One or more fields reference this lookup.");
                            break;
                    }
                }
                #endregion

                #region Business logic : LookupType
                if (entry.Entity is LookupType)
                {
                    var o = entry.Entity as LookupType;

                    switch (entry.State)
                    {
                        case EntityState.Added:
                            if (Any<LookupType>(i => i.Name == o.Name))
                                throw new ArgumentException(Messages.Error_NameTaken);
                            break;
                        case EntityState.Modified:
                            if (Any<LookupType>(i => i.Name == o.Name && i.ID != o.ID))
                                throw new ArgumentException(Messages.Error_NameTaken);
                            break;
                    }
                }
                #endregion

                #region Business logic : PolicyType
                if (entry.Entity is PolicyType)
                {
                    var o = entry.Entity as PolicyType;                    
                    if (string.IsNullOrEmpty(o.Name.Trim()))   throw new ArgumentException(Messages.Error_Name_Required);



                    switch (entry.State)
                    {
                        case EntityState.Added:
                            if (Any<PolicyType>(i => i.Name == o.Name))
                                throw new ArgumentException(Messages.Error_NameTaken);
                            break;
                        case EntityState.Modified:
                            if (Any<PolicyType>(i => i.Name == o.Name && i.ID != o.ID))
                                throw new ArgumentException(Messages.Error_NameTaken);
                            break;
                    }
                }
                #endregion

                #region Business logic : QuestionType
                if (entry.Entity is QuestionType)
                {
                    var o = entry.Entity as QuestionType;
                    
                    switch (entry.State)
                    {
                        case EntityState.Added:
                            if (Any<QuestionType>(i => i.SurveyTypeID == o.SurveyTypeID && i.Name == o.Name))
                                throw new ArgumentException(Messages.Error_NameTaken);
                            break;
                        case EntityState.Modified:
                            if (Any<QuestionType>(i => i.SurveyTypeID == o.SurveyTypeID && i.Name == o.Name && i.ID != o.ID))
                                throw new ArgumentException(Messages.Error_NameTaken);
                            break;
                    }
                }
                #endregion

                #region Business logic : Reference Item

                if(entry.Entity is ReferenceItem)
                {
                    var o = entry.Entity as ReferenceItem;
                    
                    switch (entry.State)
                    {                        
                        case EntityState.Deleted:                                                    
                            if(Any<Intersect>(i => (i.Subject == "ReferenceItem" && i.SubjectID == o.ID) || (i.Object == "ReferenceItem" && i.ObjectID == o.ID)))
                                throw new ConflictException(string.Format(Messages.Error_NotRemoved_Tokenized, "ReferenceItem"), Messages.Error_Item_RelationshipsReferences);
                            break;                     
                    }
                }

                #endregion

                #region Business logic : ReferenceItemTyoe

                if (entry.Entity is ReferenceItemType)
                {
                    var o = entry.Entity as ReferenceItemType;

                    switch (entry.State)
                    {
                        case EntityState.Added:
                            if (Any<ReferenceItemType>(i => i.Name == o.Name))
                                throw new ArgumentException(Messages.Error_NameTaken);
                            break;
                        case EntityState.Modified:
                            if (Any<ReferenceItemType>(i => i.Name == o.Name && i.ID != o.ID))
                                throw new ArgumentException(Messages.Error_NameTaken);
                            break;
                    }
                }

                #endregion

                #region Business logic : Report
                if (entry.Entity is Report)
                {
                    var o = entry.Entity as Report;
                    
                    switch (entry.State)
                    {
                        case EntityState.Added:
                            if (Any<Report>(i => i.Name == o.Name)) throw new ArgumentException(Messages.Error_NameTaken);
                            break;
                        case EntityState.Deleted:
                            var any = Any<ReportTile>(i => i.ReportID == o.ID);
                            if (any) throw new ConflictException(string.Format(Messages.Error_NotRemoved_Tokenized, "Report"), Messages.Error_List_FieldReferences);
                            break;
                        case EntityState.Modified:
                            if (Any<Report>(i => i.Name == o.Name && i.ID != o.ID)) throw new ArgumentException(Messages.Error_NameTaken);
                            break;
                    }
                }
                #endregion

                #region Business logic : ReportTile
                if (entry.Entity is ReportTile)
                {
                    var o = entry.Entity as ReportTile;
                    
                    switch (entry.State)
                    {
                        case EntityState.Added:
                            if (Any<ReportTile>(i => i.Name == o.Name && i.ReportID == o.ReportID)) throw new ArgumentException(Messages.Error_NameTaken);
                            break;
                        case EntityState.Modified:
                            if (Any<ReportTile>(i => i.Name == o.Name && i.ReportID == o.ReportID && i.ID != o.ID)) throw new ArgumentException(Messages.Error_NameTaken);
                            break;
                    }
                }
                #endregion

                #region Business logic : ResponsibilityType
                if (entry.Entity is ResponsibilityType)
                {
                    var o = entry.Entity as ResponsibilityType;
                    
                    switch (entry.State)
                    {
                        case EntityState.Added:
                            if (Any<ResponsibilityType>(i =>
                                i.Name == o.Name
                                )) throw new ArgumentException(Messages.Error_NameTaken);
                            break;
                        case EntityState.Deleted:
                            if (Any<ResponsibilityDetail>(i =>
                                i.ResponsibilityTypeID == o.ID
                                )) throw new ArgumentException(Messages.Error_ResponsibilityType_ExistingResponsibilities);
                            break;
                        case EntityState.Modified:
                            if (Any<ResponsibilityType>(i =>
                                i.Name == o.Name &&
                                i.ID != o.ID
                                )) throw new ArgumentException(Messages.Error_NameTaken);
                            break;
                    }
                }
                #endregion

                #region Business logic : RuleType

                if (entry.Entity is RuleType)
                {
                    var o = entry.Entity as RuleType;

                    switch (entry.State)
                    {
                        case EntityState.Added:
                            if (Any<RuleType>(i => i.Name == o.Name))
                                throw new ArgumentException(Messages.Error_NameTaken);
                            break;
                        case EntityState.Modified:
                            if (Any<RuleType>(i => i.Name == o.Name && i.ID != o.ID))
                                throw new ArgumentException(Messages.Error_NameTaken);
                            break;
                    }
                }

                #endregion

                #region Business logic : SurveyType
                if (entry.Entity is SurveyType)
                {
                    var o = entry.Entity as SurveyType;
                    
                    switch (entry.State)
                    {
                        case EntityState.Added:
                            if (Any<SurveyType>(i => i.Name == o.Name))
                                throw new ArgumentException(Messages.Error_NameTaken);
                            
                            break;
                        case EntityState.Modified:
                            if (Any<SurveyType>(i => i.Name == o.Name && i.ID != o.ID))
                                throw new ArgumentException(Messages.Error_NameTaken);
                            
                            break;
                    }
                }
                #endregion

                #region Business logic : Taxonomy
                if (entry.Entity is Taxonomy)
                {
                    var o = entry.Entity as Taxonomy;
                    var id = o.ID.ToString();
                    var taxonomyTypeID = o.TaxonomyTypeID;

                    switch (entry.State)
                    {
                        
                        case EntityState.Deleted:
                            var any = Any<Field>(f => f.FieldType.LookupObjectType == "Taxonomy" && f.FieldType.LookupObjectID == taxonomyTypeID && f.Value == id);
                            if (any) 
                                throw new ConflictException(Messages.Error_Taxonomy_RemoveTitle, Messages.Error_Taxonomy_FieldReference);
                            if (Any<core.entities.Attribute>(i => i.ObjectType == "Taxonomy" && i.ObjectID == o.ID))
                                throw new ConflictException(Messages.Error_Taxonomy_RemoveTitle, Messages.Error_Taxonomy_AttributeReference);
                            if (Any<Intersect>(i => (i.Subject == "Taxonomy" && i.SubjectID == o.ID) || (i.Object == "Taxonomy" && i.ObjectID == o.ID)))
                                throw new ConflictException(Messages.Error_Taxonomy_RemoveTitle, Messages.Error_Taxonomy_RelationshipReference);
                            if (ObjectHasChildren(SystemObjects.Taxonomy, o.ID)) 
                                throw new ConflictException(Messages.Error_Taxonomy_RemoveTitle, Messages.Error_Taxonomy_ChildModelsExist);
                            
                            break;
                        
                    }

                }
                #endregion

                #region Business logic : TaxonomyType
                if (entry.Entity is TaxonomyType)
                {
                    var o = entry.Entity as TaxonomyType;                    

                    switch (entry.State)
                    {
                        case EntityState.Added:
                            if (Any<TaxonomyType>(i => i.Name == o.Name))
                                throw new ArgumentException(Messages.Error_NameTaken);
                            
                            break;
                        case EntityState.Modified:
                            if (Any<TaxonomyType>(i => i.Name == o.Name && i.ID != o.ID))
                                throw new ArgumentException(Messages.Error_NameTaken);
                            
                            break;
                    }

                    Caching.RemoveItem(key(TAXONOMY_TYPES_KEY));
                }
                #endregion

            }

            #region Get objects that need event tracking.

            var modifiedEventEntities = ChangeTracker.Entries<IEventTrackedEntity>()
               .Where(p => p.State == EntityState.Modified)
               .Select(p => p.Entity).ToList();
            
            var addedEventEntities = ChangeTracker.Entries<IEventTrackedEntity>()
                .Where(p => p.State == EntityState.Added)
                .Select(p => p.Entity).ToList();

            var deletedEventEntities = ChangeTracker.Entries<IEventTrackedEntity>()
                .Where(p => p.State == EntityState.Deleted)
                .Select(p => p.Entity).ToList();

            #endregion

            //check for changed field values before the new values are written tothe db
            if (fieldsToCheckForChanges.Any())
            {                
                var fieldSql = "";

                foreach (var item in fieldsToCheckForChanges)
                {

                    if (item.ObjectID > 0 && item.FieldTypeID > 0 && !string.IsNullOrEmpty(item.ObjectType))
                    {
                        if (!string.IsNullOrEmpty(fieldSql)) fieldSql += " or ";
                        fieldSql += $"(f.ObjectID = {item.ObjectID} and f.[ObjectType] = '{item.ObjectType}' and f.FieldTypeID = {item.FieldTypeID})";
                    }
                }

                if (!string.IsNullOrEmpty(fieldSql))
                {
                    var sql = $"select f.ObjectID, f.ObjectType, f.Value, f.FieldTypeID from field f where {fieldSql}";

                    var vals = Query<dynamic>(sql);

                    foreach (var item in fieldsToCheckForChanges)
                    {
                        var value = vals.FirstOrDefault(x => x.ObjectID == item.ObjectID && x.ObjectType == item.ObjectType && x.FieldTypeID == item.FieldTypeID);

                        if ((value != null) && (item.Value != (string)value.Value))
                        {
                            changedFields.Add(item);
                        }
                    }
                }
            }

            try
            {                
                returnValue = base.SaveChanges();
            }
            catch (OptimisticConcurrencyException)
            {
            }
            
            // create events for the objects this needs to be done after save changes so we have new objects id's
            if(IsEventingEnabled) CreateEventsForObjectsRequiringTracking(modifiedEventEntities, addedEventEntities, deletedEventEntities, changedFields);

            return returnValue;
        }


        private void CreateEventsForObjectsRequiringTracking(IEnumerable<IEventTrackedEntity> modifiedEntities, IEnumerable<IEventTrackedEntity> addedEntities, IEnumerable<IEventTrackedEntity> deletedEntities, List<Field> changedFields)
        {
            //get any objects that implement EventTrackedEntity so we can add messages for them
            var events = new List<EventInfo>();
            var fieldEvents = new List<EventObjectInfo>();

            //we need to create event objects for field changes. Add them here
            foreach (var field in changedFields)
            {
                var fieldType = FieldTypes.AsNoTracking().FirstOrDefault(f => f.ID == field.FieldTypeID);
                var eventInfo = fieldEvents.FirstOrDefault(f => f.Object.ToString() == field.ObjectType && f.ObjectID == field.ObjectID);
                if (eventInfo != null)
                {
                    eventInfo.ChangedFieldIds.Add(field.FieldTypeID);
                }
                else
                {
                    eventInfo = new EventObjectInfo();
                    eventInfo.Object = (SystemObjects)Enum.Parse(typeof(SystemObjects), field.ObjectType);
                    eventInfo.ObjectID = field.ObjectID;
                    eventInfo.ObjectType = (SystemObjects)Enum.Parse(typeof(SystemObjects), fieldType.Object);
                    eventInfo.ObjectTypeID = fieldType.ObjectID;
                    eventInfo.ChangedFieldIds.Add(field.FieldTypeID);
                    fieldEvents.Add(eventInfo);
                }

            }

            foreach (var fieldEvent in fieldEvents)
            {
                addQE(events, ChangeType.Update, fieldEvent);
            }


            foreach (var modified in modifiedEntities)
            {
                addQE(events, ChangeType.Update, modified.GetEventObjectInfo());
            }
                        
            foreach (var added in addedEntities)
            {
                addQE(events, ChangeType.Add, added.GetEventObjectInfo());
            }
            
            foreach (var deleted in deletedEntities)
            {
                addQE(events, ChangeType.Delete, deleted.GetEventObjectInfo());
            }

            if (events.Any())
            {
                QueueSource.CreateTopicMessages(events);
            }
        }
        
        public string GetUserHomePage()
        {
            var homePage = Favorites.FirstOrDefault(f => f.ResourceID == CurrentResourceID && f.IsHomePage);

            return homePage?.Route ?? "";
        }

        #endregion
    }
}
