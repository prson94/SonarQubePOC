using System.Linq;
using System.Collections.Generic;
using System.Data.Entity;
using d360.core.entities;
using d360.core.entities.Views;
using d360.extensions;
using d360.core;
using System;
using System.Xml.Linq;
using System.Data.Entity.Infrastructure;
using d360.core.entities.Contracts;
using System.Linq.Expressions;
using System.Data;
using d360.core.exceptions;
using System.Data.SqlClient;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Core;
using d360.core.resources;
using d360.core.enums;
using Dapper;
using d360.core.entities.Transitive;
using System.Data.Entity.Validation;
using System.Data.Entity.Design.PluralizationServices;
using gudusoft.gsqlparser;
using d360.workflow;
using d360.workflow.entities;
using d360.workflow.models;
using System.Threading.Tasks;
using d360.core.entities.Queues;
using d360.core.queue;

namespace d360.model
{
    [DbConfigurationType(typeof(AzureConfiguration))]
    public class CompanyContext : BaseContext
    {
        #region Caching Methods

        internal string FUSIONATTRIBUTES_BY_FUSION_PREFIX_KEY = "AttributesByFusion_{0}_{1}";
        internal string REPORTING_SCHEMA_KEY = "ReportingSchema_{0}";
        internal string TAXONOMY_TYPES_KEY = "TaxonomyTypes_{0}";
        internal string TAXONOMY_BY_TYPE_PREFIX_KEY = "TaxonomyByType_{0}_{1}";
        internal string TAXONOMYDETAIL_BY_TYPE_PREFIX_KEY = "TaxonomyDetailByType_{0}_{1}";
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

        #region Ctors

        public CompanyContext(CommunityContext community, ICachingProvider caching, IQueueSource queueSource, ISecurityContextProvider context, bool skipCacheCheck = false)
            : base(community.GetCompanyConnectionString(skipCacheCheck))
        {
            Community = community;
            Caching = caching;
            QueueSource = queueSource;

            CurrentCompanyID = context.CompanyID;
            CurrentResourceID = context.ResourceID;
            CurrentResourceIsAdmin = context.IsAdministrator;
            CurrentCompanyDomain = context.CompanyPrefix;
        }

        #endregion

        #region DbSets

        public DbSet<Artifact> Artifacts { get; set; }

        public DbSet<ArtifactType> ArtifactTypes { get; set; }

        public DbSet<d360.core.entities.Attribute> Attributes { get; set; }

        public DbSet<AttributeDetail> AttributeDetails { get; set; }                            /* VIEW */

        public DbSet<AttributeType> AttributeTypes { get; set; }

        public DbSet<AttributeTypeCategory> AttributeTypeCategories { get; set; }

        public DbSet<AttributeTypeRelation> AttributeTypeRelations { get; set; }

        public DbSet<AttributeTypeRelationDetail> AttributeTypeRelationDetails { get; set; }    /* VIEW */

        public DbSet<BulkLoadQueue> BulkLoadQueues { get; set; }

        public DbSet<CacheObject> CacheObjects { get; set; }

        public DbSet<CacheRelationship> CacheRelationships { get; set; }

        public DbSet<Comment> Comments { get; set; }

        public DbSet<CommentRelation> CommentRelations { get; set; }

        public DbSet<DomainAllocationDetail> DomainAllocationDetails { get; set; }      /* VIEW */

        public DbSet<Domain> Domains { get; set; }

        public DbSet<DomainClassification> DomainClassifications { get; set; }

        public DbSet<DomainGroup> DomainGroups { get; set; }

        public DbSet<DomainItem> DomainItems { get; set; }

        public DbSet<DomainItemXref> DomainItemXrefs { get; set; }

        public DbSet<DomainSourceType> DomainSourceTypes { get; set; }

        public DbSet<DomainType> DomainTypes { get; set; }

        public DbSet<EmailTemplate> EmailTemplates { get; set; }

        public DbSet<Event> Events { get; set; }

        public DbSet<EventGroup> EventGroups { get; set; }

        public DbSet<Field> Fields { get; set; }

        public DbSet<FieldLookupValue> FieldLookupValues { get; set; }                          /* VIEW */

        public DbSet<FieldWithRelation> FieldWithRelations { get; set; }                        /* VIEW */

        public DbSet<FieldType> FieldTypes { get; set; }

        public DbSet<FieldTypeLookupValue> FieldTypeLookupValues { get; set; }                  /* VIEW */

        public DbSet<FieldTypeWithRelation> FieldTypeWithRelations { get; set; }                /* VIEW */

        public DbSet<FieldTypeFilteredLookupDefinition> FieldTypeFilteredLookupDefinitions { get; set; }

        public DbSet<FieldTypeFilteredLookupDisplayField> FieldTypeFilteredLookupDisplayFields { get; set; }

        public DbSet<FieldTypeFusionLookupDefinition> FieldTypeFusionLookupDefinitions { get; set; }

        public DbSet<FieldTypeFusionLookupDisplayField> FieldTypeFusionLookupDisplayFields { get; set; }

        public DbSet<FieldTypeRelationLookupDefinition> FieldTypeRelationLookupDefinitions { get; set; }

        public DbSet<FieldTypeRelationLookupDisplayField> FieldTypeRelationLookupDisplayFields { get; set; }

        public DbSet<Follow> Follows { get; set; }

        public DbSet<FollowChild> FollowChildren { get; set; }

        public DbSet<FollowDetail> FollowDetails { get; set; }                                  /* VIEW */

        public DbSet<FusionExecution> FusionExecutions { get; set; }

        public DbSet<FusionExecutionResultDetail> FusionExecutionResultDetails { get; set; }    /* VIEW */

        public DbSet<Fusion> FusionTypeConfigurations { get; set; }

        public DbSet<FusionAttributeOwnerDetail> FusionAttributeOwnerDetails { get; set; }          /* VIEW */

        public DbSet<FusionAttributePromotionDetail> FusionAttributePromotionDetails { get; set; }  /* VIEW */

        public DbSet<FusionAttributeOwnerRule> FusionAttributeOwnerRules { get; set; }

        public DbSet<FusionAttributeOwnerRuleItem> FusionAttributeOwnerRuleItems { get; set; }

        public DbSet<FusionAttribute> FusionAttributes { get; set; }

        public DbSet<FusionAttributePromotionRule> FusionAttributePromotionRules { get; set; }

        public DbSet<FusionAttributePromotionRuleItem> FusionAttributePromotionRuleItems { get; set; }

        public DbSet<FusionAttributePromotionRuleMapping> FusionAttributePromotionRuleMappings { get; set; }

        public DbSet<FusionAttributePromotion> FusionAttributePromotions { get; set; }

        public DbSet<FusionAttributeType> FusionAttributeTypes { get; set; }

        public DbSet<FusionAttributeTypePromotion> FusionAttributeTypePromotions { get; set; }

        public DbSet<FusionFilter> FusionFilters { get; set; }

        public DbSet<FusionJobHistory> FusionJobHistories { get; set; }

        public DbSet<FusionRule> FusionRules { get; set; }

        public DbSet<FusionRuleItem> FusionRuleItem { get; set; }
        
        public DbSet<FusionJobSchedule> FusionJobSchedules { get; set; }

        public DbSet<FusionStatusLog> FusionStatusLogs { get; set; }

        public DbSet<FusionType> FusionTypes { get; set; }

        public DbSet<FusionAgentError> FusionAgentErrors { get; set; }

        public DbSet<Group> Groups { get; set; }

        public DbSet<IntersectMap> IntersectMaps { get; set; }

        public DbSet<IntersectMapGroup> IntersectMapGroups { get; set; }

        public DbSet<IntersectMapSourceRule> IntersectMapSourceRules { get; set; }

        public DbSet<IntersectMapSourceTargetRule> IntersectMapSourceTargetRules { get; set; }

        public DbSet<IntersectMapSourceRuleContext> IntersectMapSourceRuleContexts { get; set; }

        public DbSet<Intersect> Intersects { get; set; }

        public DbSet<IntersectDetail> IntersectDetails { get; set; }                /* VIEW */

        public DbSet<IntersectTypeDetail> IntersectTypeDetails { get; set; }        /* VIEW */

        public DbSet<IntersectGroup> IntersectGroups { get; set; }

        public DbSet<IntersectNode> IntersectNodes { get; set; }

        public DbSet<IntersectRole> IntersectRoles { get; set; }

        public DbSet<IntersectType> IntersectTypes { get; set; }

        public DbSet<IntersectTypeNode> IntersectTypeNodes { get; set; }

        public DbSet<IntersectTypePredicate> IntersectTypePredicates { get; set; }

        public DbSet<Language> Languages { get; set; }

        public DbSet<Load> Loads { get; set; }

        public DbSet<LoadItem> LoadItems { get; set; }

        public DbSet<LoadItemColumn> LoadItemColumns { get; set; }

        public DbSet<LoadColumn> LoadColumns { get; set; }

        public DbSet<Lookup> Lookups { get; set; }

        public DbSet<LookupType> LookupTypes { get; set; }

        public DbSet<Map> Maps { get; set; }

        public DbSet<MapItem> MapItems { get; set; }

        public DbSet<MapRule> MapRules { get; set; }

        public DbSet<MapRuleItem> MapRuleItems { get; set; }

        public DbSet<MapRuleItemDetail> MapRuleItemDetails { get; set; }        /* VIEW */

        public DbSet<MapRuleItemMapItem> MapRuleItemMapItems { get; set; }

        public DbSet<MapSequence> MapSequences { get; set; }

        public DbSet<MapSequenceContext> MapSequenceContexts { get; set; }

        public DbSet<ObjectSecurity> ObjectSecurities { get; set; }                                         /* CACHED TABLE LOADED BY JOB */

        public DbSet<ObjectStyle> ObjectStyles { get; set; }

        public DbSet<Policy> Policies { get; set; }

        public DbSet<PolicyType> PolicyTypes { get; set; }

        public DbSet<PolicyTypeClass> PolicyTypeClasses { get; set; }

        public DbSet<PolicyTypeLevel> PolicyTypeLevels { get; set; }

        public DbSet<Predicate> Predicates { get; set; }

        public DbSet<Question> Questions { get; set; }

        public DbSet<QuestionType> QuestionTypes { get; set; }

        public DbSet<QuestionTypeOption> QuestionTypeOptions { get; set; }

        public DbSet<Relationship> Relationships { get; set; }                                              /* VIEW */


        public DbSet<Relation> Relations { get; set; }

        public DbSet<RelationType> RelationTypes { get; set; }

        public DbSet<ReportLayout> ReportLayouts { get; set; }

        public DbSet<Report> Reports { get; set; }

        public DbSet<ReportTile> ReportTiles { get; set; }

        public DbSet<Resolution> Resolutions { get; set; }

        public DbSet<ResolutionRelation> ResolutionRelations { get; set; }

        public DbSet<ResourceGroup> ResourceGroups { get; set; }

        public DbSet<Responsibility> Responsibilities { get; set; }

        public DbSet<ResponsibilityContextItem> ResponsibilityContextItems { get; set; }

        public DbSet<ResponsibilityDetailForResource> ResponsibilityDetailForResources { get; set; }        /* VIEW */

        public DbSet<ResponsibilityDetail> ResponsibilityDetails { get; set; }                              /* VIEW */

        public DbSet<ResponsibilitySummaryDetail> ResponsibilitySummaryDetails { get; set; }                /* VIEW */

        public DbSet<ResponsibilityType> ResponsibilityTypes { get; set; }

        public DbSet<ResponsibilityTypeClaim> ResponsibilityTypeClaims { get; set; }

        public DbSet<ResponsibilityTypeObjectClaim> ResponsibilityTypeObjectClaims { get; set; }

        public DbSet<ResponsibilityTypeRelation> ResponsibilityTypeRelations { get; set; }

        public DbSet<GlobalReportingResource> GlobalReportingResources { get; set; }

        public DbSet<ResponsibilityTypeObjectClaimDetail> ResponsibilityTypeObjectClaimDetail { get; set; } /* VIEW */

        public DbSet<d360.core.entities.Rule> Rules { get; set; }

        public DbSet<d360.core.entities.RuleDimension> RuleDimensions { get; set; }

        public DbSet<SecurityDetail> SecurityDetails { get; set; }                                          /* VIEW */

        public DbSet<SourceRule> SourceRules { get; set; }

        public DbSet<SourceRuleContext> SourceRuleContexts { get; set; }

        public DbSet<SourceTargetRule> SourceTargetRules { get; set; }

        public DbSet<Statistic> Statistics { get; set; }

        public DbSet<StatisticType> StatisticTypes { get; set; }

        public DbSet<StatisticTypeCheckOption> StatisticTypeCheckOptions { get; set; }

        public DbSet<Survey> Surveys { get; set; }

        public DbSet<SurveyType> SurveyTypes { get; set; }

        public DbSet<Taxonomy> Taxonomies { get; set; }

        public DbSet<TaxonomyTypeLevel> TaxonomyTypeLevels { get; set; }

        public DbSet<TaxonomyTypeClass> TaxonomyTypeClasses { get; set; }

        public DbSet<TaxonomyType> TaxonomyTypes { get; set; }

        public DbSet<TooltipTemplate> TooltipTemplates { get; set; }

        public DbSet<d360.workflow.entities.Workflow> Workflows { get; set; }

        public DbSet<d360.workflow.entities.WorkflowChallenge> WorkflowChallenges { get; set; }
        public DbSet<d360.workflow.entities.WorkflowIssue> WorkflowIssues { get; set; }
        public DbSet<d360.workflow.entities.WorkflowResource> WorkflowResources { get; set; }
        public DbSet<d360.workflow.entities.WorkflowStatus> WorkflowStatuses { get; set; }
        public DbSet<d360.workflow.entities.WorkflowTypeRelation> WorkflowTypeRelations { get; set; }

        public DbSet<AuditField> AuditFields { get; set; }
        public DbSet<Audit> Audits { get; set; }

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
                    if (existingFieldTypeIDs.Any(i => item.FieldTypeID == i))
                    {
                        Set<Field>().Attach(item);
                        Entry(item).State = (string.IsNullOrEmpty(item.Value)) ? EntityState.Deleted : EntityState.Modified;
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(item.Value))
                        {
                            Set<Field>().Add(item);
                        }
                    }
                });
                try
                {
                    var existingFields = Filter<Field>(i => i.ObjectID == oID && i.ObjectType == oType).ToList();
                    existingFields.ForEach(item =>
                    {
                        if (!items.Any(i => i.FieldTypeID == item.FieldTypeID))
                        {
                            Set<Field>().Remove(item);
                        }
                    });
                }
                catch
                {
                }
                SaveChanges();
            }
        }

        public void AddRelatedArtifact(int artifact, int artifactToRelate)
        {
            try
            {
                // See if there items are already related.
                Database.Connection.Execute(@"
declare @s int, @t int, @sG int, @tG int

set @s = @ss
set @t = @tt
set @sG = 0
set @tG = 0

if exists(select GroupID from RelatedArtifact where ArtifactID = @s)
begin
	select @sG = GroupID from RelatedArtifact where ArtifactID = @s
end
if exists(select GroupID from RelatedArtifact where ArtifactID = @t)
begin
	select @tG = GroupID from RelatedArtifact where ArtifactID = @t
end

if @sG = @tG and @sG = 0 and @tG = 0
begin
 select @sG = coalesce(max(GroupID), 0) + 1 from RelatedArtifact
 begin try
  insert into RelatedArtifact (GroupID, ArtifactID) values (@sG, @s)
  insert into RelatedArtifact (GroupID, ArtifactID) values (@sG, @t)
 end try
 begin catch
  select ''
 end catch
end

if @sG <> @tG and @sG <> 0 and @tG = 0
begin
 insert into RelatedArtifact (GroupID, ArtifactID) values (@sG, @t)
end

if @sG <> @tG and @sG = 0 and @tG <> 0
begin
 insert into RelatedArtifact (GroupID, ArtifactID) values (@tG, @s)
end
", new { ss = artifact, tt = artifactToRelate });
            }
            catch
            {
            }
        }

        public void DeleteRelatedArtifact(int source, int target)
        {
            // See if there items are already related.
            Database.Connection.Execute(@"
declare @s int, @t int, @sG int, @tG int, @count int

set @s = @ss
set @t = @tt
set @sG = 0
set @tG = 0

if exists(select GroupID from RelatedArtifact where ArtifactID = @s)
begin
	select @sG = GroupID from RelatedArtifact where ArtifactID = @s
end
if exists(select GroupID from RelatedArtifact where ArtifactID = @t)
begin
	select @tG = GroupID from RelatedArtifact where ArtifactID = @t
end

if @sG = @tG
begin
 delete RelatedArtifact where GroupID = @tG and ArtifactID = @t
 select @count = count(1) from RelatedArtifact where GroupID = @tG
 if @count = 1
 begin
  delete RelatedArtifact where GroupID = @tG
 end
end", new { ss = source, tt = target });
        }

        public void Enqueue(QueueType type, QueueObject obj)
        {
            QueueSource.CreateMessage(type, obj);
        }

        public List<AllocationPossibility> GetTypes()
        {
            var list = Database.Connection.Query<AllocationPossibility>(@"
			select	'ArtifactType' as ObjectType, ID as ObjectTypeID, 'Artifacts :: ' + Name as Name from ArtifactType
			union
			select	'DomainType' as ObjectType, ID as ObjectTypeID, 'Reference :: ' + Name as Name from DomainType
			union
			select	'TaxonomyType' as ObjectType, T.ID as ObjectTypeID, 'Models :: ' + C.Name  +' :: ' + T.Name as Name from TaxonomyType T inner join TaxonomyTypeClass C on C.ID = T.TaxonomyTypeClassID
			union
			select	'PolicyType' as ObjectType, ID as ObjectTypeID, 'Policies :: ' + Name as Name from PolicyType
			union
			select	'ResourceType' as ObjectType, 1 as ObjectTypeID, 'User' as Name 
			union
			select	'GroupType' as ObjectType, 1 as ObjectTypeID, 'Group' as Name").ToList();
            RuleType ruleType = RuleType.Informational;
            foreach (var rt in ruleType.GetRuleTypeEnumList())
            {
                list.Add(new AllocationPossibility { ObjectType = "RuleType", Name = string.Format("Rules :: {0}", rt.Name), ObjectTypeID = (int)rt.ID });
            }

            list = list.OrderBy(i => i.Name).ToList();

            return list;
        }

        public List<AllocationPossibility> GetAllocationOptions()
        {
            var list = Database.Connection.Query<AllocationPossibility>(@"
			select	'ArtifactType' as ObjectType, ID as ObjectTypeID, 'Artifacts :: ' + Name as Name from ArtifactType
			union
			select	'DomainType' as ObjectType, ID as ObjectTypeID, 'Reference :: ' + Name as Name from DomainType
			union
			select	'TaxonomyType' as ObjectType, ID as ObjectTypeID, 'Models :: ' + Name as Name from TaxonomyType
			union
			select	'PolicyType' as ObjectType, ID as ObjectTypeID, 'Policies :: ' + Name as Name from PolicyType
			union
            select	'IntersectType' as ObjectType, ID as ObjectTypeID, 'Relationships :: ' + Name as Name from IntersectType
			union
			select	'FusionType' as ObjectType, ID as ObjectTypeID, 'Fusion Types :: ' + Name as Name from FusionType
			union
			select	'FusionAttributeType' as ObjectType, ID as ObjectTypeID, 'Fusion Attributes :: ' + TextPath as Name from FusionAttributeType
").ToList();
            RuleType ruleType = RuleType.Informational;
            foreach (var rt in ruleType.GetRuleTypeEnumList())
            {
                list.Add(new AllocationPossibility { ObjectType = "RuleType", Name = string.Format("Rules :: {0}", rt.Name), ObjectTypeID = (int)rt.ID });
            }

            list = list.OrderBy(i => i.Name).ToList();

            return list;
        }

        public List<AllocationPossibility> GetAvailableAllocationOptions(int attributeTypeID)
        {
            var list = Database.Connection.Query<AllocationPossibility>(@"
select A.* from (
			select	'ArtifactType' as ObjectType, ID as ObjectTypeID, 'Artifacts :: ' + Name as Name from ArtifactType
			union
			select	'DomainType' as ObjectType, ID as ObjectTypeID, 'Reference :: ' + Name as Name from DomainType
			union
			select	'TaxonomyType' as ObjectType, ID as ObjectTypeID, 'Models :: ' + Name as Name from TaxonomyType
			union
			select	'IntersectType' as ObjectType, ID as ObjectTypeID, 'Relationships :: ' + Name as Name from IntersectType
			union
			select	'FusionType' as ObjectType, ID as ObjectTypeID, 'Fusion Types :: ' + Name as Name from FusionType
			union
			select	'FusionAttributeType' as ObjectType, ID as ObjectTypeID, 'Fusion Attributes :: ' + TextPath as Name from FusionAttributeType
			union
			select	'PolicyType' as ObjectType, ID as ObjectTypeID, 'Policies :: ' + Name as Name from PolicyType
) A left join AttributeTypeRelationDetail R on R.ObjectType = A.ObjectType and R.ObjectID = A.ObjectTypeID and R.AttributeTypeID = @id
where R.ObjectID is null", new { id = attributeTypeID }).ToList();

            RuleType ruleType = RuleType.Informational;
            foreach (var rt in ruleType.GetRuleTypeEnumList())
            {
                list.Add(new AllocationPossibility { ObjectType = "RuleType", Name = string.Format("Rules :: {0}", rt.Name), ObjectTypeID = (int)rt.ID });
            }

            list = list.OrderBy(i => i.Name).ToList();

            return list;
        }

        public List<AllocationPossibility> GetAvailableAllocationPossibilities()
        {
            return GetAllocationOptions();
        }

        public List<AllowedIntersectionType> GetAllowedIntersectionTypes(string type, int id, int intersectID = 0)
        {
            return Database.Connection
                .Query<AllowedIntersectionType>("GetAllowedIntersectionTypes @SourceType, @SourceTypeID, @IntersectID", 
                new 
                { 
                    SourceType = new Dapper.DbString { Value = type.ToString(), IsAnsi = true }, 
                    SourceTypeID = id, 
                    IntersectID = intersectID 
                }).ToList();
        }

        public IQueryable<ResponsibilityType> GetAllowedResponsibilityTypesByObject(SystemObjects type, int id)
        {
            try
            {
                return Database.Connection.Query<ResponsibilityType>("EXEC GetAllowedResponsibilityTypesByObject @type, @id", new
                {
                    type = new Dapper.DbString { Value = type.ToString(), IsAnsi = true },
                    id = id
                }).AsQueryable();
            }
            catch (SqlException ex)
            {
                throw CheckAndTranslateSqlException(ex, "Responsibility Type");
            }
            catch
            {
                throw;
            }
        }
        
        public IQueryable<AttributeHierarchyItem> GetAttributeAndIntersectHierarchyByObject(SystemObjects type, int id)
        {
            return Query<AttributeHierarchyItem>("EXEC GetAttributeAndIntersectHierarchyByObject @type, @id", new { type = new Dapper.DbString { Value = type.ToString(), IsAnsi = true }, id = id }).AsQueryable();
        }

        public List<KeyValuePair<int, string>> GetClassifications()
        {
            var array = (IntersectClassification[])(Enum.GetValues(typeof(IntersectClassification)).Cast<IntersectClassification>());
            return array
                .Select(a => new KeyValuePair<int, string>(Convert.ToInt32(a), a.ToString()))
                .ToList();
        }

        public IQueryable<FieldWithRelation> GetFieldRelationsByObject(SystemObjects type, int id)
        {
            string query = string.Format("EXEC GetFieldsWithRelationsByObject '{0}', {1}", type.ToString(), id);
            return Database.SqlQuery<FieldWithRelation>(query).OrderBy(i => i.SortOrder).AsQueryable();
        }

        public IQueryable<FieldTypeWithRelation> GetFieldTypeRelationsByObject(SystemObjects type, int id)
        {
            var sType = type.ToString();
            return Filter<FieldTypeWithRelation>(
                i => i.Object == sType && i.ObjectID == id
                )
                .OrderBy(i => i.SortOrder).ThenBy(i => i.FriendlyName)
                .AsQueryable();
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
            
            return model;
        }

        public List<FusionOwnerOption> GetFusionOwnerOptions()
        {
            return Database.Connection.Query<FusionOwnerOption>("EXEC fusion.GetFusionOwnerOptions").ToList();
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
union
select	'DomainType' as PromotionObjectType, 
		ID as PromotionObjectID, 
		'Reference: ' + Name + ' List' as Name, 
		NULL as ParentObjectTypeID 
from	DomainType 
union
select	'DomainType' as PromotionObjectType, 
		ID as PromotionObjectID, 
		'Reference:' + Name + ' List Item' as Name, 
		ID as ParentObjectTypeID 
from	DomainType
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

        #region Load

        string LoadDetailBaseSql = @"select	L.ID,
		L.[Object],
		L.ObjectID,
		coalesce(D.TextPath, 'Default') as ObjectName,
		L.Notes,
		'MyFile.' + L.Extension as FilePath,
		L.DateStarted,
		L.DateCompleted,
		case L.[Action]
			when 'P' then 'Promotion'
			when 'R' then 'Relation'
			when 'U' then 'Unrelation'
            when 'L' then 'Lineage'
            when 'N' then 'Lineage : Business'
            when 'O' then 'Responsibilities'
            when 'T' then 'Lineage : Technical'
            when 'S' then 'Synonyms'
			when 'W' then 'Promotion (via Propose Workflow)'
		end as [Action],
        S.C as Success,
        E.C as Error,
        I.C as Incomplete,
		T.C as Total,
        R.FirstName + ' ' + R.LastName as Requestor
from	[Load] L
		left join cache.ObjectDetails D on D.[Object] = L.[Object] and D.ObjectID = L.ObjectID
		left join reporting.Global_Resource R on R.ResourceID = L.UpdatedBy       
        cross apply (select count(1) as C from LoadItem where LoadID = L.ID and Status = 1) S
        cross apply (select count(1) as C from LoadItem where LoadID = L.ID and Status = 0) E
        cross apply (select count(1) as C from LoadItem where LoadID = L.ID and Status is null) I
        cross apply (select count(1) as C from LoadItem where LoadID = L.ID) T 
";
       
        public IEnumerable<LoadDetail> GetLoadDetails()
        {
            return Query<LoadDetail>(LoadDetailBaseSql + " order by L.ID desc");
        }

        public LoadDetail GetLoadDetail(int id)
        {
            return Query<LoadDetail>(LoadDetailBaseSql + " where ID = " + id).SingleOrDefault();
        }

        public IEnumerable<dynamic> GetLoadColumnDetails(int id)
        {
            return Query<dynamic>(@"
select		'Column' + cast(ColumnIndex as varchar) as datafield,
			Name as text
from		LoadColumn
where		LoadID = @id
order by	ColumnIndex", new { id });
        }

        public IEnumerable<dynamic> GetLoadItemDetails(int id)
        {
            var columns = Filter<LoadColumn>(i => i.LoadID == id).OrderBy(i => i.ColumnIndex).ToList();
            var sql = "";
            var sqlColumns = "select I.LoadID, I.RowIndex";
            var sqlTables = "from LoadItem I";
            columns.ForEach(c =>
            {
                sqlColumns += string.Format(", C{0}.Value as Column{0}", c.ColumnIndex);
                sqlTables += string.Format(" left join LoadItemColumn C{0} on C{0}.LoadID = I.LoadID and C{0}.RowIndex = I.RowIndex and C{0}.ColumnIndex = {0}", c.ColumnIndex);
            });
            sqlColumns += ", case I.[Status] when 1 then 'Complete' when 0 then 'Failed' else 'Queued' end as [Status], I.StatusMessage"; 
            sql += sqlColumns + " " + sqlTables + " where I.LoadID = @id order by I.RowIndex";
            return Query<dynamic>(sql, new { id });
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
        

        public ObjectDetail GetObjectDetail(SystemObjects type, long id)
        {
            var model = GetObjectDetail(type.ToString(), id);
            return model;
        }

        public ObjectDetail GetObjectDetail(string type, long id)
        {
            string query = string.Format("SELECT * FROM utility.ObjectDetail('{0}', {1})", type, id);
            var model = Database.SqlQuery<ObjectDetail>(query).SingleOrDefault();
            if (model != null)
            {
                var pluralize = PluralizationService.CreateService(System.Globalization.CultureInfo.CurrentCulture);
                model.PluralizedName = pluralize.Pluralize(model.Name);
                pluralize = null;
            }
            return model;
        }

        public ObjectStyle GetObjectStyle(SystemObjects type, int id)
        {
            var sType = type.ToString();
            return Filter<ObjectStyle>(i => i.ObjectType == sType && i.ObjectID == id).FirstOrDefault();
        }
        
        public XElement GetRandomSurveyQuestionForUser(SystemObjects type, int id)
        {
            string query = string.Format("GetRandomSurveyQuestionForUser {0}, '{1}', {2}", CurrentResourceID, type.ToString(), id);
            var xmlString = Database.SqlQuery<string>(query).First();
            return XElement.Parse(xmlString);
        }

        public IQueryable<ResponsibilityDetail> GetResponsibilitiesByObject(SystemObjects type, int id, bool showHidden = true)
        {
            try
            {
                var sql = @"select * from ResponsibilityDetail where ObjectType = @type and ObjectID = @id" + (showHidden ? "" : " and Visible = 1") + " order by [Role], [ResponsibleObjectName]";
                return Query<ResponsibilityDetail>(sql, new { type = new Dapper.DbString { Value = type.ToString(), IsAnsi = true }, id = id }).AsQueryable();
            }
            catch (SqlException ex)
            {
                throw CheckAndTranslateSqlException(ex, "Responsibility");
            }
            catch
            {
                throw;
            }
        }

        public IQueryable<ResponsibilityDetail> GetResponsibilitiesByResource(SystemObjects type, int id)
        {
            try
            {
                var sType = type.ToString();
                return Filter<ResponsibilityDetail>(i => i.ResponsibleObjectType == sType && i.ResponsibleObjectID == id);
            }
            catch (SqlException ex)
            {
                throw CheckAndTranslateSqlException(ex, "Responsibility");
            }
            catch
            {
                throw;
            }
        }

        public IQueryable<ResponsibilitySummaryDetail> GetResponsibilitiesByType(int id)
        {
            try
            {
                return Filter<ResponsibilitySummaryDetail>(i => i.ResponsibilityTypeID == id);
            }
            catch (SqlException ex)
            {
                throw CheckAndTranslateSqlException(ex, "Responsibility");
            }
            catch
            {
                throw;
            }
        }

        public List<StatisticDetail> GetStatisticDetailsByType(SystemObjects type, int id)
        {
            return Query<StatisticDetail>($"GetStatisticDetails '{type.ToString()}', {id}").ToList();
        }

        public IEnumerable<dynamic> GetStatisticTypeRollupCheckOptions()
        {
            var sql = @"select * from (
			select	'ArtifactType|' + cast(ID as varchar(15)) as ID, 'Artifacts :: ' + Name as Name from ArtifactType
			) O order by Name";
            return Query<dynamic>(sql).ToList();
        }

        public bool HasClaimInCurrentPermissionList(List<SecurityDetail> list, Claim claim, ClaimObject claimObject = ClaimObject.Root)
        {
            var has = CurrentResourceIsAdmin;
            if (!has) has = list.Any(i => i.Claim == claim && i.ClaimObject == claimObject);
            return has;
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


        public IEnumerable<T> Query<T>(string sql, object param = null, int timeout = 90)
        {
            return Database.Connection.Query<T>(sql, param, null, true, timeout);
        }

        public SqlMapper.GridReader QueryMultiple(string sql, object param = null)
        {
            return Database.Connection.QueryMultiple(sql, param, null);
        }

        public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object param = null, int timeout = 90)
        {
            return await Database.Connection.QueryAsync<T>(sql, param, null, timeout);
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
                        case SystemObjects.DomainGroup:
                        case SystemObjects.DomainType:
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

        public void ValidateIntersectType(int id, List<IntersectTypeNode> nodes)
        {
            var dt = new DataTable();
            dt.Columns.Add("ObjectType");
            dt.Columns.Add("ObjectID");

            nodes.ForEach(n =>
            {
                dt.Rows.Add(n.ObjectType, n.ObjectID);
            });

            var pId = new SqlParameter("ID", SqlDbType.Int);
            pId.Value = id;

            var pTbl = new SqlParameter("Nodes", SqlDbType.Structured);
            pTbl.Value = dt;
            pTbl.TypeName = "dbo.IntersectionNodeType";

            Database.ExecuteSqlCommand("validate.IntersectType @ID, @Nodes", pId, pTbl);
        }

        #region Events

        public IQueryable<OverlayEventHeader> GetEventHeadersByObject(SystemObjects type, int id)
        {
            return Database.Connection.Query<OverlayEventHeader>(@"
select		R.Name as [Rule],
			G.ID,
			G.Name,
			case coalesce(E.Status, 'Closed')
				when 'Closed' then 'Closed'
				else 'Active'
			end as Status,
			max(Date) as Date,
			coalesce(count(E.ID), 0) as [Count]
from		EventGroup G
			inner join [Rule] R on R.ID = G.RuleID
			inner join cache.Relationships CR on CR.SourceObject = @t and CR.SourceObjectID = @i and CR.TargetObject = 'Rule' and CR.TargetObjectID = R.ID
			left join [Event] E on E.EventGroupID = G.ID
group by	R.Name,
			G.ID,
			G.Name,
			case coalesce(E.Status, 'Closed')
				when 'Closed' then 'Closed'
				else 'Active'
			end
order by	Date desc",
                new { t = new Dapper.DbString { Value = type.ToString(), IsAnsi = true }, i = id }
            ).AsQueryable();
        }

        #endregion

        public ObjectStatisticTileModel GetObjectStatistics(SystemObjects type, int id)
        {
            var model = new ObjectStatisticTileModel { Items = new List<ObjectStatisticTileItemModel>() };

            var list = Database.Connection.Query<RawObjectStatistic>("[tile].[GetObjectStatistics] @type, @id", new { type = new Dapper.DbString { Value = type.ToString(), IsAnsi = true }, id = id }).ToList();
            
            var pluralize = PluralizationService.CreateService(System.Globalization.CultureInfo.CurrentCulture);

            list.ForEach(i =>
            {
                switch (i.Group)
                {
                    case "Events":
                        model.EventCount = i.Value;
                        model.EventUrl = i.Url;
                        break;
                    case "Comments":
                        model.CommentCount = i.Value;
                        model.CommentUrl = i.Url;
                        break;
                    case "Followers":
                        model.FollowerCount = i.Value;
                        model.FollowerUrl = i.Url;
                        break;
                    case "Score":
                        model.Score = i.Value;
                        model.ScoreUrl = i.Url;
                        break;
                    case "Issues":
                        model.IssueCount = i.Value;
                        model.IssueUrl = i.Url;
                        break;
                    default:
                        model.Items.Add(new ObjectStatisticTileItemModel { Count = i.Value, Name = pluralize.Pluralize(i.Name ?? ""), Url = i.Url });
                        break;
                }
            });

            return model;
        }

        public IEnumerable<FieldTypeLookupValue> GetFieldTypeLookupOptions()
        {
            return Query<FieldTypeLookupValue>(
@"
select	*
from	(
		SELECT	'Artifact' as LookupObjectType,
				ID as LookupObjectID,
				'Artifact : ' + Name as Name
		FROM	ArtifactType
		UNION
		SELECT	'Domain' as LookupObjectType,
				ID as LookupObjectID,
				'Reference : ' + Name as Name
		FROM	DomainType
		UNION
		SELECT	'DomainItem' as LookupObjectType,
				O.ID as LookupObjectID,
				'Reference Items : ' + T.Name + ' : ' + O.Name as Name
		FROM	Domain O
                inner join DomainType T on T.ID = O.DomainTypeID
        UNION
		SELECT	'Resource' as LookupObjectType,
				1 as LookupObjectID,
				'Resource : User' as Name
		UNION
		SELECT	'Taxonomy' as LookupObjectType,
				ID as LookupObjectID,
				'Information Model : ' + Name as Name
		FROM	TaxonomyType
		UNION
		SELECT	'Lookup' as LookupObjectType,
				ID as LookupObjectID,
				'Lookup : ' + Name as Name
		FROM	LookupType
		) O
order by Name");
        }

        #region Permission

        public IQueryable<SecurityDetail> GetPermissions(SystemObjects type, int id)
        {
            var sType = type.ToString();
            return Filter<SecurityDetail>(i => i.ObjectType == sType && i.ObjectID == id && i.ResponsibleObjectID == CurrentResourceID);
        }

        public IQueryable<SecurityDetail> GetPermissions(SystemObjects type, int[] id)
        {
            var sType = type.ToString();
            return Filter<SecurityDetail>(i => i.ObjectType == sType && id.Contains(i.ObjectID) && i.ResponsibleObjectID == CurrentResourceID);
        }

        public bool HasPermission(SystemObjects type, int id, Claim claim, ClaimObject claimObject = ClaimObject.Root)
        {
            bool hasPermission = CurrentResourceIsAdmin;
            if (!hasPermission)
            {
                var sType = type.ToString();
                hasPermission = Any<SecurityDetail>(i => i.ObjectType == sType && i.ObjectID == id && i.ResponsibleObjectID == CurrentResourceID && i.Claim == claim && i.ClaimObject == claimObject);
            }

            return hasPermission;
        }

        #endregion

        #region Relationships

        public Intersect AddIntersect(SystemObjects subject, int subjectID, SystemObjects @object, int objectID, IntersectClassification classification, int? predicateID, string description)
        {
            return AddIntersect(subject.ToString(), subjectID, @object.ToString(), objectID, classification, predicateID, description);
        }

        public Intersect AddIntersect(string subject, int subjectID, string @object, int objectID, IntersectClassification classification, int? predicateID, string description)
        {
            Intersect intersect = null;

            var subjectDetail = GetObjectDetail(subject, subjectID);
            var objectDetail = GetObjectDetail(@object, objectID);

            if (subjectDetail == null)
                throw new NotFoundException("Subject");

            if (objectDetail == null)
                throw new NotFoundException("Object");

            var intersectType = Filter<IntersectType>(i => (
                    (i.Subject == subjectDetail.Type && i.SubjectID == subjectDetail.TypeID && i.Object == objectDetail.Type && i.ObjectID == objectDetail.TypeID) ||
                    (i.Object == subjectDetail.Type && i.ObjectID == subjectDetail.TypeID && i.Subject == objectDetail.Type && i.SubjectID == objectDetail.TypeID)
                ) //&&
                //(predicateID.HasValue && i.IntersectTypePredicates.Any(p => p.PredicateType == PredicateType.Synonym), 
                , i => i.Nodes
            ).FirstOrDefault();

            if (intersectType == null)
                throw new NotFoundException($"Relation type [{subjectDetail.Name} to {objectDetail.Name}]");

            intersect = Filter<Intersect>(i => i.IntersectTypeID == intersectType.ID && (
                    (i.Subject == subject && i.SubjectID == subjectID && i.Object == @object && i.ObjectID == objectID) ||
                    (i.Object == subject && i.ObjectID == subjectID && i.Subject == @object && i.SubjectID == objectID)
                ), i => i.Nodes
            ).SingleOrDefault();

            if (intersect == null)
            {
                var nodes = intersectType.Nodes.OrderBy(i => i.Order).ToList();
                intersect = new Intersect { IntersectTypeID = intersectType.ID, Classification = classification, Description = description };
                intersect.Nodes = new List<IntersectNode>();

                if (subjectDetail.Type == intersectType.Subject && subjectDetail.TypeID == intersectType.SubjectID)
                {
                    intersect.Subject = subject;
                    intersect.SubjectID = subjectID;
                    intersect.Object = @object;
                    intersect.ObjectID = objectID;

                    intersect.Nodes.Add(new IntersectNode { IntersectTypeNodeID = nodes.First().ID, ObjectType = subject, ObjectID = subjectID });
                    intersect.Nodes.Add(new IntersectNode { IntersectTypeNodeID = nodes.Last().ID, ObjectType = @object, ObjectID = objectID });
                    Intersects.Add(intersect);
                }
                else
                {
                    intersect.Subject = @object;
                    intersect.SubjectID = objectID;
                    intersect.Object = subject;
                    intersect.ObjectID = subjectID;

                    intersect.Nodes.Add(new IntersectNode { IntersectTypeNodeID = nodes.First().ID, ObjectType = @object, ObjectID = objectID });
                    intersect.Nodes.Add(new IntersectNode { IntersectTypeNodeID = nodes.Last().ID, ObjectType = subject, ObjectID = subjectID });
                    Intersects.Add(intersect);
                }

                SaveChanges();
            }
            else
            {
                intersect.Classification = classification;
                intersect.Description = description;
                SaveChanges();
            }

            return intersect;
        }

        public IntersectDetail AddIntersect(int intersectTypeID, SystemObjects subject, int subjectID, SystemObjects @object, int objectID)
        {
            Intersect intersect = null;
            IntersectDetail dtl = null;

            var sSubject = subject.ToString();
            var sObject = @object.ToString();

            var subjectDetail = GetObjectDetail(subject, subjectID);
            var objectDetail = GetObjectDetail(@object, objectID);

            if (subjectDetail == null)
                throw new NotFoundException("Subject");

            if (objectDetail == null)
                throw new NotFoundException("Object");

            var intersectType = GetById<IntersectType>(intersectTypeID, i => i.Nodes);

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
                    var nodes = intersectType.Nodes.OrderBy(i => i.Order).ToList();
                    intersect = new Intersect { IntersectTypeID = intersectType.ID };
                    intersect.Nodes = new List<IntersectNode>();

                    if (subjectDetail.Type == intersectType.Subject && subjectDetail.TypeID == intersectType.SubjectID)
                    {
                        intersect.Subject = sSubject;
                        intersect.SubjectID = subjectID;
                        intersect.Object = sObject;
                        intersect.ObjectID = objectID;

                        intersect.Nodes.Add(new IntersectNode { IntersectTypeNodeID = nodes.First().ID, ObjectType = sSubject, ObjectID = subjectID });
                        intersect.Nodes.Add(new IntersectNode { IntersectTypeNodeID = nodes.Last().ID, ObjectType = sObject, ObjectID = objectID });
                        Intersects.Add(intersect);
                    }
                    else
                    {
                        intersect.Subject = sObject;
                        intersect.SubjectID = objectID;
                        intersect.Object = sSubject;
                        intersect.ObjectID = subjectID;

                        intersect.Nodes.Add(new IntersectNode { IntersectTypeNodeID = nodes.First().ID, ObjectType = sObject, ObjectID = objectID });
                        intersect.Nodes.Add(new IntersectNode { IntersectTypeNodeID = nodes.Last().ID, ObjectType = sSubject, ObjectID = subjectID });
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

        //public void AddRelationship(SystemObjects type, int id, SystemObjects targetType, int targetID, IntersectClassification classification, int? roleID, string description)
        //{
        //    AddRelationship(type.ToString(), id, targetType.ToString(), targetID, classification, roleID, description);
        //}

        //public void AddRelationship(string type, int id, string targetType, int targetID, IntersectClassification classification, int? roleID, string description)
        //{
        //    if (!roleID.HasValue) roleID = 0;

        //    Database.Connection.Execute(
        //        "AddRelationship @ResourceID, @Date, @Type, @ID, @Classification, @IntersectRole, @Description, @TargetType, @TargetID",
        //        new
        //        {
        //            ResourceID = CurrentResourceID,
        //            Date = DateTime.UtcNow,
        //            Type = type,
        //            ID = id,
        //            Classification = (int)classification,
        //            IntersectRole = roleID,
        //            Description = description,
        //            TargetType = targetType,
        //            TargetID = targetID
        //        });
        //}


        //public void AddRelationships(SystemObjects type, int id, IntersectClassification classification, int? roleID, string description, List<ObjectModel> objects)
        //{
        //    #region Load Objects Parameter

        //    var tObjects = new DataTable();
        //    tObjects.Columns.Add("ObjectType");
        //    tObjects.Columns.Add("ObjectID");

        //    objects.ForEach(o =>
        //    {
        //        tObjects.Rows.Add(o.ObjectType, o.ObjectID);
        //    });

        //    #endregion

        //    if (!roleID.HasValue) roleID = 0;

        //    ExecuteNonQueryCommand(
        //        "EXEC AddRelationships @ResourceID, @Date, @Type, @ID, @Classification, @IntersectRole, @Description, @Objects",
        //        new List<SqlParameter>() {
        //            new SqlParameter("ResourceID", CurrentResourceID),
        //            new SqlParameter("Date", DateTime.UtcNow) { SqlDbType = SqlDbType.DateTime },
        //            new SqlParameter("Type", type.ToString()),
        //            new SqlParameter("ID", id),
        //            new SqlParameter("Classification", (int)classification),
        //            new SqlParameter("IntersectRole", roleID),
        //            new SqlParameter("Description", description + ""),
        //            new SqlParameter("Objects", tObjects) { SqlDbType = SqlDbType.Structured, TypeName = "dbo.ObjectsTable" }
        //        }
        //    );
        //}

        public bool DeleteRelationship(int id)
        {

            var item = GetById<Intersect>(id, i => i.Nodes);
            if (item == null) throw new NotFoundException("Relationship");
            return Database.ExecuteSqlCommand("DeleteIntersect {0}, {1}", id, CurrentResourceID) > 0;
        }

        //public void EditRelationship(int id, int? roleID, IntersectClassification classification, string description)
        //{
        //    if (!roleID.HasValue) roleID = 0;

        //    ExecuteNonQueryCommand(
        //        "EditRelationship @ResourceID, @Date, @ID, @Classification, @IntersectRole, @Description",
        //        new List<SqlParameter>() {
        //            new SqlParameter("ResourceID", CurrentResourceID),
        //            new SqlParameter("Date", DateTime.UtcNow) { SqlDbType = SqlDbType.DateTime },
        //            new SqlParameter("ID", id),
        //            new SqlParameter("Classification", (int)classification),
        //            new SqlParameter("IntersectRole", roleID),
        //            new SqlParameter("Description", description + "")
        //        }
        //    );
        //}

        public IQueryable<CriticalRelationshipsByObject> GetCriticalRelationshipsByObject(SystemObjects type, int id)
        {
            return Database.Connection.Query<CriticalRelationshipsByObject>(
@"select		R.IntersectID,
				S.IconBackColor,
				S.IconForeColor,
				S.IconText,
				dbo.GenerateObjectUrl(R.TargetObject, R.TargetTypeID, R.TargetObjectID) as Url,
				R.TargetObjectID as ID,
				R.TargetObject as ObjectType,
				R.TargetTypeName as TypeName,
				R.TargetObjectName as Name,
				Description
	from		cache.Relationships R
				left join ObjectStyle R on R.ObjectType = R.TargetType and S.ObjectID = R.TargetTypeID 
	where		R.SourceObject = @type 
				and R.SourceObjectID = @id
				and R.Classification = 1
	order by	R.TargetTypeName,
				R.TargetObjectName", new { type = new Dapper.DbString { Value = type.ToString(), IsAnsi = true }, id = id }).AsQueryable();
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
		TargetObjectName,
		TargetTypeName,
		C.[Count],
		D.Url as TargetUrl
from	cache.Relationships R
        inner join cache.ObjectDetails D on D.[Object] = R.TargetObject and D.ObjectID = R.TargetObjectID
		outer apply (
					select	count(1) as [Count]
					from	FusionAttributeType
					where	ParentID = R.TargetTypeID
					) C
where	R.SourceObject = 'FusionAttribute'
		and R.TargetObject = 'FusionAttribute'
		--and R.SourceTypeID = 301
        --and R.SourceObject =  @type
        and R.SourceObjectID = @id
        and R.TargetTypeID = 302", new { type = new Dapper.DbString { Value = type.ToString(), IsAnsi = true }, id }).ToList();
        }

        public List<IntersectTypeOption> GetIntersectTypeOptions(SystemObjects? startType = null, int? startID = null, SystemObjects? endType = null, int? endID = null)
        {
            var sql = @"
	SELECT		I.ID,
				I.Name,
				I.Type
	FROM		(
				SELECT	ID,
						'Artifacts :: ' + Name AS Name,
						'ArtifactType' AS Type
				FROM	ArtifactType
				UNION
				SELECT	ID,
						'Reference :: ' + Name AS Name,
						'DomainType' AS Type
				FROM	DomainType
				UNION
				SELECT	A.ID,
						'Fusion Attributes :: ' + A.TextPath AS Name,
						'FusionAttributeType' AS Type
				FROM	FusionAttributeType A
						INNER JOIN FusionType T ON A.FusionTypeID = T.ID
				UNION
				SELECT	1 as ID,
						'Group' as Name,
						'GroupType' as Type
				UNION
				SELECT	ID,
						'Models :: ' + Name AS Name,
						'TaxonomyType' AS Type
				FROM	TaxonomyType
				UNION
				SELECT	ID,
						'Policies :: ' + Name AS Name,
						'PolicyType' AS Type
				FROM	PolicyType
				UNION
				SELECT	CAST(ID as int) ID,
						'Relationships :: ' + Name AS Name,
						'IntersectType' AS Type
				FROM	IntersectType
				UNION
				SELECT	1 as ID,
						'Resource' as Name,
						'ResourceType' as Type
				UNION
				SELECT	1 as ID,
						'Rules :: Informational' as Name,
						'RuleType' as Type
				UNION
				SELECT	2 as ID,
						'Rules :: Quality Check' as Name,
						'RuleType' as Type
				UNION
				SELECT	3 as ID,
						'Rules :: Metric' as Name,
						'RuleType' as Type
				UNION
				SELECT	4 as ID,
						'Rules :: Profile' as Name,
						'RuleType' as Type
) I";

            if (startType.HasValue && startID.HasValue)
            {
                sql += string.Format(@" left join [utility].[RelationshipTypes] T on T.SourceObjectType = '{0}' and T.SourceObjectID = {1} and T.TargetObjectType = I.[Type] and T.TargetObjectID = I.ID", startType.Value.ToString(), startID.Value);

                if (endType.HasValue && endID.HasValue)
                {
                    sql += string.Format(@" where (T.IntersectTypeID is null OR (T.TargetObjectType = '{0}' and T.TargetObjectID = {1}) )", endType.Value.ToString(), endID.Value);
                }
                else
                {
                    sql += " where T.IntersectTypeID is null";
                }
            }

            sql += " ORDER BY I.Name";

            return Database.Connection.Query<IntersectTypeOption>(sql).ToList();
        }

//        public List<IntersectTypeOption> GetRelationTypeOptions(SystemObjects? sub = null, int? subID = null, SystemObjects? obj = null, int? objID = null)
//        {
//            var sql = @"
//	SELECT		I.ID,
//				I.Name,
//				I.Type
//	FROM		(
//				SELECT	ID,
//						'Artifacts :: ' + Name AS Name,
//						'ArtifactType' AS Type
//				FROM	ArtifactType
//				UNION
//				SELECT	ID,
//						'Reference :: ' + Name AS Name,
//						'DomainType' AS Type
//				FROM	DomainType
//				UNION
//				SELECT	A.ID,
//						'Fusion Attributes :: ' + A.TextPath AS Name,
//						'FusionAttributeType' AS Type
//				FROM	FusionAttributeType A
//						INNER JOIN FusionType T ON A.FusionTypeID = T.ID
//				UNION
//				SELECT	1 as ID,
//						'Group' as Name,
//						'GroupType' as Type
//				UNION
//				SELECT	ID,
//						'Models :: ' + Name AS Name,
//						'TaxonomyType' AS Type
//				FROM	TaxonomyType
//				UNION
//				SELECT	ID,
//						'Policies :: ' + Name AS Name,
//						'PolicyType' AS Type
//				FROM	PolicyType
//				UNION
//				SELECT	CAST(ID as int) ID,
//						'Relationships :: ' + Name AS Name,
//						'IntersectType' AS Type
//				FROM	IntersectType
//				UNION
//				SELECT	1 as ID,
//						'Resource' as Name,
//						'ResourceType' as Type
//				UNION
//				SELECT	1 as ID,
//						'Rules :: Informational' as Name,
//						'RuleType' as Type
//				UNION
//				SELECT	2 as ID,
//						'Rules :: Quality Check' as Name,
//						'RuleType' as Type
//				UNION
//				SELECT	3 as ID,
//						'Rules :: Metric' as Name,
//						'RuleType' as Type
//				UNION
//				SELECT	4 as ID,
//						'Rules :: Profile' as Name,
//						'RuleType' as Type
//) I";

//            if (sub.HasValue && subID.HasValue)
//            {
//                sql += @"
// left join [RelationType] T on	(T.[Subject] = @sub and T.SubjectID = @subID and T.[Object] = I.[Type] and T.ObjectID = I.ID) 
//                                or (T.[Object] = @sub and T.ObjectID = @subID and T.[Subject] = I.[Type] and T.SubjectID = I.ID)
// left join Predicate P on P.ID = T.PredicateID and P.Type not in (5)";

//                if (obj.HasValue && objID.HasValue)
//                {
//                    sql += @" where P.ID is null OR (
//(T.[Subject] = @sub and T.SubjectID = @subID and T.[Object] = @obj and T.ObjectID = @objID) 
//or (T.[Object] = @sub and T.ObjectID = @subID and T.[Subject] = @obj and T.SubjectID = @objID)
//)";
//                }
//                else
//                {
//                    sql += " where P.ID is null";
//                }
//            }
//            sql += " GROUP BY I.ID, I.Name, I.Type ORDER BY I.Name";

//            if (obj.HasValue && objID.HasValue)
//            {
//                return Database.Connection.Query<IntersectTypeOption>(sql, new { sub = new Dapper.DbString { Value = sub.ToString(), IsAnsi = true }, subID, obj = new Dapper.DbString { Value = obj.ToString(), IsAnsi = true }, objID }).ToList();
//            }
//            else
//            {
//                return Database.Connection.Query<IntersectTypeOption>(sql, new { sub = new Dapper.DbString { Value = sub.ToString(), IsAnsi = true }, subID }).ToList();
//            }
//        }

        internal class RelationModel
        {
            public int ID { get; set; }
            public int IntersectTypeID { get; set; }
            public string Object { get; set; }
            public int ObjectID { get; set; }
            public string Name { get; set; }
            public string Type { get; set; }
            public int TypeID { get; set; }
            public string TypeName { get; set; }
            public string IconBackColor { get; set; }
            public string IconForeColor { get; set; }
            public string IconText { get; set; }
            public IntersectClassification Classification { get; set; }
        }

        /// <summary>
        /// Gets a list of relationship counts for a given object, broken up by All Glossary Items, Critical Glossary ITems, and All Models.
        /// </summary>
        /// <param name="type">The type of object</param>
        /// <param name="id">The ID of the object</param>
        /// <returns>A list of aggregate relationship data. <seealso cref="RelationshipAggregate"/></returns>
        public List<RelationshipAggregate> GetAggregateRelationshipBreakdownsByObject(SystemObjects type, int id)
        {
            var list = new List<RelationshipAggregate>();
            var models = Query<RelationModel>(QueryConstants.ObjectRelationships, new { type = new Dapper.DbString { Value = type.ToString(), IsAnsi = true }, id }).ToList();
            list.AddRange(
                models.Where(i => i.Object != "Taxonomy")
                    .GroupBy(i => new { i.IntersectTypeID, i.Type, i.TypeID, i.TypeName, i.IconBackColor, i.IconForeColor, i.IconText } )
                    .Select(i => new RelationshipAggregate {
                        Group = "1",
                        GroupName = "All Glossary Items",
                        Count = i.Count(),
                        IconBackColor = i.Key.IconBackColor,
                        IconForeColor = i.Key.IconForeColor,
                        IconText = i.Key.IconText,
                        IntersectTypeID = i.Key.IntersectTypeID,
                        Type = i.Key.Type,
                        TypeID = i.Key.TypeID,
                        TypeName = i.Key.TypeName
                    }).OrderBy(i => i.TypeName)
                );
            list.AddRange(
                models.Where(i => i.Object != "Taxonomy" && i.Classification == IntersectClassification.Critical)
                    .GroupBy(i => new { i.IntersectTypeID, i.Type, i.TypeID, i.TypeName, i.IconBackColor, i.IconForeColor, i.IconText })
                    .Select(i => new RelationshipAggregate
                    {
                        Group = "2",
                        GroupName = "Critical Glossary Items",
                        Count = i.Count(),
                        IconBackColor = i.Key.IconBackColor,
                        IconForeColor = i.Key.IconForeColor,
                        IconText = i.Key.IconText,
                        IntersectTypeID = i.Key.IntersectTypeID,
                        Type = i.Key.Type,
                        TypeID = i.Key.TypeID,
                        TypeName = i.Key.TypeName
                    }).OrderBy(i => i.TypeName)
                );
            list.AddRange(
                models.Where(i => i.Object == "Taxonomy")
                    .GroupBy(i => new { i.IntersectTypeID, i.Type, i.TypeID, i.TypeName, i.IconBackColor, i.IconForeColor, i.IconText })
                    .Select(i => new RelationshipAggregate
                    {
                        Group = "3",
                        GroupName = "All Models",
                        Count = i.Count(),
                        IconBackColor = i.Key.IconBackColor,
                        IconForeColor = i.Key.IconForeColor,
                        IconText = i.Key.IconText,
                        IntersectTypeID = i.Key.IntersectTypeID,
                        Type = i.Key.Type,
                        TypeID = i.Key.TypeID,
                        TypeName = i.Key.TypeName
                    }).OrderBy(i => i.TypeName)
                );
            return list;
        }

        #endregion

        #region Reporting Engine

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

        #region Social

        public IQueryable<CommentDetail> EditComment(Comment comment, ICollection<CommentRelation> relations)
        {
            //comment.DateCreated = DateTime.UtcNow;
            //comment.CreatingResourceID = CurrentResourceID;
            var now = DateTime.UtcNow;
            //SaveOrUpdate<Comment>(comment);
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
            return Filter<FollowDetail>(i => i.ObjectType == fs && i.ObjectID == id);
        }

        public IQueryable<MostActiveUserReportModel> GetMostActiveUsersReport()
        {
            return Database.SqlQuery<MostActiveUserReportModel>("report.GetMostActiveUsers").AsQueryable();
        }

        //public SocialStatisticsByObject GetSocialStatisticsByObject(SystemObjects type, int id)
        //{
        //    return
        //    ExecuteQuery<SocialStatisticsByObject>("tile.GetSocialStatisticsByObject @type, @id",
        //        new List<SqlParameter>() {
        //            new SqlParameter("type", type.ToString()),
        //            new SqlParameter("id", id)
        //        }
        //    ).FirstOrDefault();
        //}

        public dynamic GetSocialDataForCurrentResource()
        {
            return Query<dynamic>(@"
select	* 
from	(
		select		count(1) as FollowerCount from Follow where ObjectType = 'Resource' and ObjectID = @id
		) FC
		full join	(
					select count(1) as GroupCount from ResourceGroup where ResourceID = @id
					) G on 1=1
		full join	(
					select dbo.[GetObjectStatisticScore]('Resource', @id) * 100 as Score
					) S on 1=1", new { id = CurrentResourceID }).SingleOrDefault();
        }

        public dynamic GetSocialDataForGroup(int id)
        {
            return Query<dynamic>(@"select	* from 
(select	count(1) as FollowerCount from Follow where ObjectType = 'Group' and ObjectID = @id) FC
full join (select count(1) as MemberCount from ResourceGroup where GroupID = @id) G on 1=1", new { id = id }).SingleOrDefault();
        }

        public dynamic GetSocialDataForResource(int id)
        {
            return Query<dynamic>(@"select	* from 
(select	count(1) as FollowerCount from Follow where ObjectType = 'Resource' and ObjectID = @id) FC
full join (select count(1) as FollowingCount from Follow where ResourceID = @id) FO on 1=1
full join (select count(1) as GroupCount from ResourceGroup where ResourceID = @id) G on 1=1", new { id = id }).SingleOrDefault();
        }

        #endregion

        #region Token Processing Methods

        private string renderTemplate(string templateType, string action, SystemObjects type, int id)
        {
            string query = string.Format("GetRenderedTemplateBody '{0}', '{1}', {2}, '{3}'", templateType, type.ToString(), id, action);
            var model = Database.SqlQuery<RenderTemplateModel>(query).SingleOrDefault();
            var html = "";
            if (model != null) html = model.Body;
            return html;
        }

        public string RenderEmail(string action, SystemObjects type, int id)
        {
            return renderTemplate("Email", action, type, id);
        }

        public string RenderTooltip(string action, SystemObjects type, int id)
        {
            return renderTemplate("Tooltip", action, type, id);
        }

        #endregion

        #region Workflow

        public IEnumerable<Resource> GetResponsibleResourcesByArtifactAndWorkflowType(WorkflowType workflowType, int id)
        {
            return Query<Resource>(
@"
select	R.ResourceID, R.FirstName, R.LastName, R.Email, R.Email, R.DateLastLoggedIn, 1 as ResourceTypeID, R.Status
from	ResponsibilityDetail RD 
		inner join Artifact A on RD.ObjectType = 'Artifact' and RD.ObjectID = A.ID and A.ID = @id
		inner join WorkflowTypeRelation WTR on WTR.[Object] = 'ArtifactType' and WTR.ObjectID = A.ArtifactTypeID and WTR.WorkflowType = @wt and WTR.ResponsibilityTypeID = RD.ResponsibilityTypeID
		inner join reporting.Global_Resource R 
			on	(
					(RD.ResponsibleObjectType = 'Group' and R.ResourceID = RD.PrimaryOwnerResourceID) or 
					(RD.ResponsibleObjectType = 'Resource' and R.ResourceID = RD.ResponsibleObjectID)
				)", new { wt = (int)workflowType, id = id });
        }

        public Workflow GetMostRecentCertificationWorkflowByArtifact(int id)
        {
            return Query<Workflow>(
@"select    top 1
			*
from		Workflow
where		WorkflowType = 2
			and Data.exist('/fields/ArtifactID[text() = sql:variable(""@id"")]') = 1
order by	DateStarted desc", new { id = id }).FirstOrDefault();
        }

        public List<WorkflowRelationResponsibilityModel> GetWorkflowRelations()
        {
            return Query<WorkflowRelationResponsibilityModel>(@"select	W.ID,
		W.WorkflowType,
		W.[Object],
		W.ObjectID, 
		OD.Name as ObjectName,
		W.Parent,
		W.ParentID,
		PD.Name as ParentName,
		W.Fields,
        W.[Enabled],
		W.ResponsibilityTypeID,
		RT.Name as ResponsibilityType
from	WorkflowTypeRelation W
        inner join cache.ObjectDetails OD on OD.[Object] = W.[Object] and OD.ObjectID = W.ObjectID
		left join cache.ObjectDetails PD on PD.[Object] = W.[Parent] and PD.ObjectID = W.ParentID
		inner join ResponsibilityType RT on RT.ID = W.ResponsibilityTypeID").ToList();
        }

        public IEnumerable<FieldTypeLookupValue> GetWorkflowObjectTypeOptions()
        {
            return Query<FieldTypeLookupValue>(
@"
select	*
from	(
		SELECT	'ArtifactType' as LookupObjectType,
				ID as LookupObjectID,
				'Artifact : ' + Name as Name
		FROM	ArtifactType
) O
order by Name");
        }

        public IEnumerable<FieldTypeLookupValue> GetWorkflowParentTypeOptions(int workflowType, string type, int id, bool includeAlreadyAssignedItem = false)
        {
            if (includeAlreadyAssignedItem)
            {
                return Query<FieldTypeLookupValue>(
    @"
select	*
from	(
		SELECT	'TaxonomyType' as LookupObjectType,
				ID as LookupObjectID,
				'Model : ' + Name as Name
		FROM	TaxonomyType
) O
order by Name", new { workflowType, type, id });  
            }
            else 
            {
                return Query<FieldTypeLookupValue>(
    @"
select	*
from	(
		SELECT	'TaxonomyType' as LookupObjectType,
				ID as LookupObjectID,
				'Model : ' + Name as Name
		FROM	TaxonomyType
        WHERE   ID not in   (
                            SELECT  ParentID 
                            FROM    WorkflowTypeRelation
                            WHERE   Parent = 'TaxonomyType'
                                    AND WorkflowType = @workflowType
                                    AND [Object] = @type 
                                    AND ObjectID = @id
                            )
) O
order by Name", new { workflowType, type, id });          
            }
        }

        public List<ResponsibilityType> GetWorkflowResponsibilityTypeOptions(string type, int id)
        {
            return Filter<ResponsibilityTypeRelation>(i => i.ObjectType == type && i.ObjectID == id, i => i.ResponsibilityType)
                    .OrderBy(i => i.ResponsibilityType.Name)
                    .ToList().Where(i => i.ResponsibilityType.ResponsibilityTypeGroup == ResponsibilityTypeGroup.People)
                    .Select(i => i.ResponsibilityType)
                    .ToList();
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
        public bool Delete(string type, int id)
        {
            try
            {
                Database.Connection.Execute("DeleteObject @Obj, @ObjectID, @ResourceID", new { Obj = type, ObjectID = id, ResourceID = CurrentResourceID });
                return true;
            }
            catch (Exception ex)
            {
                throw resolveToRealException(ex);
            }
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FusionAttributeOwnerRuleItem>().HasRequired(t => t.FusionAttributeOwnerRule).WithMany(t => t.FusionAttributeOwnerRuleItems).HasForeignKey(k => k.FusionAttributeOwnerRuleID).WillCascadeOnDelete(true);
            modelBuilder.Entity<FieldTypeFilteredLookupDisplayField>().HasRequired(t => t.FieldTypeFilteredLookupDefinition).WithMany(t => t.FieldTypeFilteredLookupDisplayFields).HasForeignKey(k => k.FieldTypeFilteredLookupDefinitionID).WillCascadeOnDelete(true);
            modelBuilder.Entity<FieldTypeFusionLookupDisplayField>().HasRequired(t => t.FieldTypeFusionLookupDefinition).WithMany(t => t.FieldTypeFusionLookupDisplayFields).HasForeignKey(k => k.FieldTypeFusionLookupDefinitionID).WillCascadeOnDelete(true);
            modelBuilder.Entity<FieldTypeRelationLookupDisplayField>().HasRequired(t => t.FieldTypeRelationLookupDefinition).WithMany(t => t.FieldTypeRelationLookupDisplayFields).HasForeignKey(k => k.FieldTypeRelationLookupDefinitionID).WillCascadeOnDelete(true);
            modelBuilder.Entity<IntersectTypeNode>().HasRequired(t => t.IntersectType).WithMany(t => t.Nodes).HasForeignKey(k => k.IntersectTypeID).WillCascadeOnDelete(true);
            modelBuilder.Entity<IntersectTypePredicate>().HasRequired(t => t.IntersectType).WithMany(t => t.IntersectTypePredicates).HasForeignKey(k => k.IntersectTypeID).WillCascadeOnDelete(true);

            modelBuilder.Entity<FusionAttributeOwnerRuleItem>().HasRequired(t => t.FusionAttributeOwnerRule).WithMany(t => t.FusionAttributeOwnerRuleItems).HasForeignKey(k => k.FusionAttributeOwnerRuleID).WillCascadeOnDelete(true);
            modelBuilder.Entity<FusionAttributePromotionRuleItem>().HasRequired(t => t.FusionAttributePromotionRule).WithMany(t => t.FusionAttributePromotionRuleItems).HasForeignKey(k => k.FusionAttributePromotionRuleID).WillCascadeOnDelete(true);
            modelBuilder.Entity<FusionAttributePromotionRuleMapping>().HasRequired(t => t.FusionAttributePromotionRule).WithMany(t => t.FusionAttributePromotionRuleMappings).HasForeignKey(k => k.FusionAttributePromotionRuleID).WillCascadeOnDelete(true);
            modelBuilder.Entity<IntersectMapSourceRule>().HasRequired(t => t.IntersectMap).WithMany(t => t.IntersectMapSourceRules).HasForeignKey(k => k.IntersectMapID).WillCascadeOnDelete(true);
            modelBuilder.Entity<IntersectMapSourceRuleContext>().HasRequired(t => t.IntersectMapSourceRule).WithMany(t => t.Contexts).HasForeignKey(k => k.IntersectMapSourceRuleID).WillCascadeOnDelete(true);
            modelBuilder.Entity<IntersectMapSourceRule>().HasRequired(t => t.SourceRule).WithMany(t => t.Items).HasForeignKey(k => k.SourceRuleID).WillCascadeOnDelete(true);
            //modelBuilder.Entity<MapRuleItem>().HasMany<MapItem>(i => i.MapItems).WithMany(i => i.MapRuleItems).Map(i =>
            //{
            //    i.MapLeftKey("MapRuleItemID").MapRightKey("MapItemID").ToTable("MapRuleItemMapItem");
            //});

            modelBuilder.Entity<Fusion>().HasMany<Artifact>(i => i.FusionOwners).WithMany(i => i.OwnedFusions).Map(i => {
                i.MapLeftKey("FusionID").MapRightKey("ArtifactID").ToTable("FusionOwner");
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

        public override bool Update<T>(T item)
        {
            ObjectContext.ObjectStateManager.ChangeObjectState(item, EntityState.Modified);
            return (SaveChanges() > 0);
        }

        public bool SaveOrUpdate<T>(T entity, List<Field> fields) where T : BaseIntObject
        {
            var returnValue = false;

            if (IsPersistent(entity))
            {
                returnValue = Update<T>(entity);
            }
            else
            {
                returnValue = Add<T>(entity);
            }

            if (fields != null)
            {
                fields.ForEach(i => {
                    i.ObjectID = entity.ID;
                });
                AddOrUpdateFields(fields);
            }

            return returnValue;
        }

        public override int SaveChanges()
        {
            int returnValue = 0;

            foreach (var entry in ObjectContext.ObjectStateManager.GetObjectStateEntries(System.Data.Entity.EntityState.Added))
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

            foreach (var entry in ObjectContext.ObjectStateManager.GetObjectStateEntries(System.Data.Entity.EntityState.Added | System.Data.Entity.EntityState.Unchanged | System.Data.Entity.EntityState.Modified | System.Data.Entity.EntityState.Deleted))
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
                    var id = o.ID.ToString();

                    switch (entry.State)
                    { 
                        case EntityState.Added:
                            if (Any<Artifact>(i => i.Name == o.Name && i.ArtifactTypeID == o.ArtifactTypeID && i.TaxonomyTypeID == o.TaxonomyTypeID && i.ParentID == o.ParentID)) throw new ArgumentException(Messages.Error_NameTaken);
                            break;
                        case EntityState.Deleted:
                            var any = false;
                            any = Any<IntersectNode>(i => i.ObjectType == "Artifact" && i.ObjectID == o.ID);
                            if (any) throw new ConflictException(string.Format(Messages.Error_NotRemoved_Tokenized, "Artifact"), Messages.Error_Item_RelationshipsReferences);

                            any = Any<Artifact>(i => i.ParentID == o.ID);
                            if (any) throw new ConflictException(string.Format(Messages.Error_NotRemoved_Tokenized, "Artifact"), Messages.Error_Artifact_ExistingChildren);
                            break;
                        case EntityState.Modified:
                            if (Any<Artifact>(i => i.Name == o.Name && i.ArtifactTypeID == o.ArtifactTypeID && i.TaxonomyTypeID == o.TaxonomyTypeID & i.ParentID == o.ParentID && i.ID != o.ID)) throw new ArgumentException(Messages.Error_NameTaken);
                            break;
                    }

                    Caching.RemoveItem(key(ARTIFACTDICTIONARY_BY_TYPE_PREFIX_KEY, o.ArtifactTypeID));
                }
                #endregion

                #region Business logic : ArtifactType
                if (entry.Entity is ArtifactType)
                {
                    var o = entry.Entity as ArtifactType;
                    var id = o.ID.ToString();

                    switch (entry.State)
                    {
                        case EntityState.Added:
                            if (Any<ArtifactType>(i => i.Name == o.Name))
                                throw new ArgumentException(Messages.Error_NameTaken);
                            break;
                        case EntityState.Deleted:
                            if (Any<Artifact>(i => i.ArtifactTypeID == o.ID))
                                throw new ConflictException(string.Format(Messages.Error_NotRemoved_Tokenized, o.Name), Messages.Error_ArtifactsAssignedToType);
                            var childIDs = Filter<ArtifactType>(i => i.ParentID == o.ID).Select(i => i.ID).ToList();
                            if (childIDs.Count > 0)
                                throw new ConflictException(string.Format(Messages.Error_NotRemoved_Tokenized, o.Name), Messages.Error_ChildTypesAssignedToType);
                            //if (Artifacts.Any(i => childIDs.Contains(i.ArtifactTypeID)))
                            //    throw new ConflictException(string.Format(Messages.Error_NotRemoved_Tokenized, o.Name), Messages.Error_ArtifactsAssignedToType);
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
                    var id = o.ID.ToString();

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

                #region Business logic : Domain
                if (entry.Entity is Domain)
                {
                    var o = entry.Entity as Domain;
                    var id = o.ID.ToString();
                    var domainTypeID = o.DomainTypeID;
                    switch (entry.State)
                    {
                        case EntityState.Added:
                            if (Any<Domain>(i => i.Name == o.Name && i.DomainTypeID == o.DomainTypeID)) throw new ArgumentException(Messages.Error_NameTaken);
                            break;
                        case EntityState.Deleted:
                            var any = Any<Field>(f => f.FieldType.LookupObjectType == "Domain" && f.FieldType.LookupObjectID == domainTypeID && f.Value == id);
                            if (any) throw new ConflictException(string.Format(Messages.Error_NotRemoved_Tokenized, "Domain list"), Messages.Error_List_FieldReferences);
                            any = Any<IntersectNode>(i => i.ObjectType == "Domain" && i.ObjectID == o.ID);
                            if (any) throw new ConflictException(string.Format(Messages.Error_NotRemoved_Tokenized, "Domain list"), Messages.Error_List_FieldReferences);
                            break;
                        case EntityState.Modified:
                            if (Any<Domain>(i => i.Name == o.Name && i.DomainTypeID == o.DomainTypeID && i.ID != o.ID)) throw new ArgumentException(Messages.Error_NameTaken);
                            break;
                    }
                }
                #endregion

                #region Business logic : DomainGroup
                if (entry.Entity is DomainGroup)
                {
                    var o = entry.Entity as DomainGroup;
                    var id = o.ID.ToString();

                    switch (entry.State)
                    {
                        case EntityState.Added:
                            if (Any<DomainGroup>(i => i.DomainTypeID == o.DomainTypeID && i.Name == o.Name))
                                throw new ArgumentException(Messages.Error_NameTaken);
                            break;
                        case EntityState.Modified:
                            if (Any<DomainGroup>(i => i.DomainTypeID == o.DomainTypeID && i.Name == o.Name && i.ID != o.ID))
                                throw new ArgumentException(Messages.Error_NameTaken);
                            break;
                    }
                }
                #endregion

                #region Business logic : DomainItem
                if (entry.Entity is DomainItem)
                {
                    var o = entry.Entity as DomainItem;
                    var id = o.ID.ToString();

                    switch (entry.State)
                    {
                        case EntityState.Added:
                            if (Any<DomainItem>(i => ((i.Code == o.Code) || (i.Name == o.Name)) && i.DomainID == o.DomainID)) throw new ArgumentException(Messages.Error_NameTaken);
                            break;
                        //case EntityState.Unchanged:
                        case EntityState.Modified:
                            if (Any<DomainItem>(i => ((i.Code == o.Code) || (i.Name == o.Name)) && i.DomainID == o.DomainID && i.ID != o.ID)) throw new ArgumentException(Messages.Error_NameTaken);
                            break;
                    }
                }
                #endregion

                #region Business logic : DomainType
                if (entry.Entity is DomainType)
                {
                    var o = entry.Entity as DomainType;
                    var id = o.ID.ToString();

                    switch (entry.State)
                    {
                        case EntityState.Added:
                            if (Any<DomainType>(i => i.Name == o.Name))
                                throw new ArgumentException(Messages.Error_NameTaken);
                            break;
                        case EntityState.Modified:
                            if (Any<DomainType>(i => i.Name == o.Name && i.ID != o.ID))
                                throw new ArgumentException(Messages.Error_NameTaken);
                            break;
                    }
                }
                #endregion

                #region Business logic : EmailTemplate
                if (entry.Entity is EmailTemplate)
                {
                    var o = entry.Entity as EmailTemplate;
                    var id = o.ID.ToString();

                    switch (entry.State)
                    {
                        case EntityState.Added:
                            if (Any<EmailTemplate>(i => i.Name == o.Name && i.Action == o.Action))
                                throw new ArgumentException(Messages.Error_NameTaken);
                            break;
                        case EntityState.Modified:
                            if (Any<EmailTemplate>(i => i.Name == o.Name && i.Action == o.Action && i.ID != o.ID))
                                throw new ArgumentException(Messages.Error_NameTaken);
                            break;
                    }
                }
                #endregion

                #region Business logic : FieldType
                if (entry.Entity is FieldType)
                {
                    var o = entry.Entity as FieldType;
                    var id = o.ID.ToString();

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
                    var id = o.ID.ToString();

                    switch (entry.State)
                    {
                        case EntityState.Added:
                            if (Any<FusionAttributeType>(i => i.FusionTypeID == o.FusionTypeID && i.Name == o.Name))
                                throw new ArgumentException(Messages.Error_NameTaken);
                            break;
                        case EntityState.Modified:
                            if (Any<FusionAttributeType>(i => i.FusionTypeID == o.FusionTypeID && i.Name == o.Name && i.ID != o.ID))
                                throw new ArgumentException(Messages.Error_NameTaken);
                            break;
                    }
                }
                #endregion

                #region Business logic : Fusion
                if (entry.Entity is Fusion)
                {
                    var o = entry.Entity as Fusion;
                    var id = o.ID.ToString();

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
                    var id = o.ID.ToString();

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
                    var id = o.ID.ToString();

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
                            if (Any<Responsibility>(i => i.ResponsibleObjectType == "Group" && i.ResponsibleObjectID == o.ID))
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
                            any = Any<IntersectNode>(i => i.ObjectType == "Intersect" && i.ObjectID == o.ID);
                            if (any) throw new ConflictException("Relationship Could not be Removed", "One or more relationships reference this relationship.");
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
                    var id = o.ID.ToString();

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

                #region Business logic : QuestionType
                if (entry.Entity is QuestionType)
                {
                    var o = entry.Entity as QuestionType;
                    var id = o.ID.ToString();

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

                #region Business logic : Report
                if (entry.Entity is Report)
                {
                    var o = entry.Entity as Report;
                    var id = o.ID.ToString();

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
                    var id = o.ID.ToString();

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
                    var id = o.ID.ToString();

                    switch (entry.State)
                    {
                        case EntityState.Added:
                            if (Any<ResponsibilityType>(i =>
                                i.Name == o.Name
                                )) throw new ArgumentException(Messages.Error_NameTaken);
                            break;
                        case EntityState.Deleted:
                            if (Any<Responsibility>(i =>
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

                #region Business logic : ResponsibilityTypeClaim
                if (entry.Entity is ResponsibilityTypeClaim)
                {
                    var o = entry.Entity as ResponsibilityTypeClaim;
                    var id = o.ID.ToString();

                    switch (entry.State)
                    {
                        case EntityState.Added:
                            if (Any<ResponsibilityTypeClaim>(i =>
                                i.Claim == o.Claim &&
                                i.ClaimObject == o.ClaimObject &&
                                i.ResponsibilityTypeID == o.ResponsibilityTypeID
                                )) throw new ArgumentException(Messages.Error_Claim_AlreadyAssignedToItem);
                            break;
                        case EntityState.Modified:
                            if (Any<ResponsibilityTypeClaim>(i =>
                                i.Claim == o.Claim &&
                                i.ClaimObject == o.ClaimObject &&
                                i.ResponsibilityTypeID == o.ResponsibilityTypeID &&
                                i.ID != o.ID
                                )) throw new ArgumentException(Messages.Error_Claim_AlreadyAssignedToItem);
                            break;
                    }
                }
                #endregion

                #region Business logic : ResponsibilityTypeObjectClaim
                if (entry.Entity is ResponsibilityTypeObjectClaim)
                {
                    var o = entry.Entity as ResponsibilityTypeObjectClaim;
                    var id = o.ID.ToString();

                    switch (entry.State)
                    {
                        case EntityState.Added:
                            if (Any<ResponsibilityTypeObjectClaim>(i =>
                                i.Claim == o.Claim &&
                                i.ClaimObject == o.ClaimObject &&
                                i.ObjectID == o.ObjectID &&
                                i.ObjectType == o.ObjectType &&
                                i.ResponsibilityTypeID == o.ResponsibilityTypeID
                                )) throw new ArgumentException(Messages.Error_Claim_AlreadyAssignedToItem);
                            break;
                        case EntityState.Modified:
                            if (Any<ResponsibilityTypeObjectClaim>(i => 
                                i.Claim == o.Claim &&
                                i.ClaimObject == o.ClaimObject &&
                                i.ObjectID == o.ObjectID &&
                                i.ObjectType == o.ObjectType &&
                                i.ResponsibilityTypeID == o.ResponsibilityTypeID &&
                                i.ID != o.ID
                                )) throw new ArgumentException(Messages.Error_Claim_AlreadyAssignedToItem);
                            break;
                    }
                }
                #endregion

                #region Business logic : StatisticType
                if (entry.Entity is StatisticType)
                {
                    var o = entry.Entity as StatisticType;
                    var id = o.ID.ToString();

                    switch (entry.State)
                    {
                        case EntityState.Added:
                            if (Any<StatisticType>(i => i.Name == o.Name && i.Object == o.Object && i.ObjectID == o.ObjectID))
                                throw new ArgumentException(Messages.Error_NameTaken);
                            break;
                        case EntityState.Modified:
                            if (Any<StatisticType>(i => i.Name == o.Name && i.Object == o.Object && i.ObjectID == o.ObjectID && i.ID != o.ID))
                                throw new ArgumentException(Messages.Error_NameTaken);
                            break;
                    }
                }
                #endregion

                #region Business logic : SurveyType
                if (entry.Entity is SurveyType)
                {
                    var o = entry.Entity as SurveyType;
                    var id = o.ID.ToString();

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
                        case EntityState.Added:
                            if (Any<Taxonomy>(i => i.Name == o.Name && i.TaxonomyTypeID == o.TaxonomyTypeID && i.ParentID == o.ParentID)) 
                                throw new ArgumentException(Messages.Error_NameTaken);
                            break;
                        case EntityState.Deleted:
                            var any = Any<Field>(f => f.FieldType.LookupObjectType == "Taxonomy" && f.FieldType.LookupObjectID == taxonomyTypeID && f.Value == id);
                            if (any) 
                                throw new ConflictException(Messages.Error_Taxonomy_RemoveTitle, Messages.Error_Taxonomy_FieldReference);
                            if (Any<core.entities.Attribute>(i => i.ObjectType == "Taxonomy" && i.ObjectID == o.ID))
                                throw new ConflictException(Messages.Error_Taxonomy_RemoveTitle, Messages.Error_Taxonomy_AttributeReference);
                            if (Any<IntersectNode>(i => i.ObjectType == "Taxonomy" && i.ObjectID == o.ID))
                                throw new ConflictException(Messages.Error_Taxonomy_RemoveTitle, Messages.Error_Taxonomy_RelationshipReference);
                            if (Any<Taxonomy>(i => i.ParentID == o.ID)) 
                                throw new ConflictException(Messages.Error_Taxonomy_RemoveTitle, Messages.Error_Taxonomy_ChildModelsExist);
                            if (Any<Responsibility>(i => i.ResponsibilityType.ResponsibilityTypeGroup == ResponsibilityTypeGroup.People && i.ObjectType == "Taxonomy" && i.ObjectID == o.ID)) 
                                throw new ConflictException(Messages.Error_Taxonomy_RemoveTitle, Messages.Error_Taxonomy_PeopleResponsibilitiesExist);
                            if (Any<Responsibility>(i => i.ResponsibilityType.ResponsibilityTypeGroup == ResponsibilityTypeGroup.Sourcing && i.ObjectType == "Taxonomy" && i.ObjectID == o.ID))
                                throw new ConflictException(Messages.Error_Taxonomy_RemoveTitle, Messages.Error_Taxonomy_SourcingResponsibilitiesExist);
                            break;
                        case EntityState.Modified:
                            if (Any<Taxonomy>(i => i.Name == o.Name && i.TaxonomyTypeID == o.TaxonomyTypeID && i.ParentID == o.ParentID && i.ID != o.ID)) 
                                throw new ArgumentException(Messages.Error_NameTaken);
                            break;
                    }

                    Caching.RemoveItem(key(TAXONOMY_BY_TYPE_PREFIX_KEY, o.TaxonomyTypeID));
                    Caching.RemoveItem(key(TAXONOMYDETAIL_BY_TYPE_PREFIX_KEY, o.TaxonomyTypeID));
                }
                #endregion

                #region Business logic : TaxonomyType
                if (entry.Entity is TaxonomyType)
                {
                    var o = entry.Entity as TaxonomyType;
                    var id = o.ID.ToString();

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
                        case EntityState.Deleted:
                            if (Any<Artifact>(i => i.TaxonomyTypeID == o.ID))
                                throw new ArgumentException(Messages.TaxonomyType_Assigned);
                            break;
                    }

                    Caching.RemoveItem(key(TAXONOMY_TYPES_KEY));
                }
                #endregion

                #region Business logic : TooltipTemplate
                if (entry.Entity is TooltipTemplate)
                {
                    var o = entry.Entity as TooltipTemplate;
                    var id = o.ID.ToString();

                    switch (entry.State)
                    {
                        case EntityState.Added:
                            if (Any<TooltipTemplate>(i => i.Name == o.Name && i.Action == o.Action))
                                throw new ArgumentException(Messages.Error_NameTaken);
                            break;
                        case EntityState.Modified:
                            if (Any<TooltipTemplate>(i => i.Name == o.Name && i.Action == o.Action && i.ID != o.ID))
                                throw new ArgumentException(Messages.Error_NameTaken);
                            break;
                    }
                }
                #endregion
            }
           
            try
            {
                returnValue = base.SaveChanges();
            }
            catch (OptimisticConcurrencyException)
            {
            }

            return returnValue;
        }

        protected override DbEntityValidationResult ValidateEntity(DbEntityEntry entityEntry, IDictionary<object, object> items)
        {
            return base.ValidateEntity(entityEntry, items);
        }

        #endregion
    }
}
