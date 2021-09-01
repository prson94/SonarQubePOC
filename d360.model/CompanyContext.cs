using d360.core;
using d360.core.entities;
using d360.core.entities.Contracts;
using d360.core.entities.Views;
using d360.core.entities.Graph;
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
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Design.PluralizationServices;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using d360.core.entities.Metric;
using d360.model.helpers;
using System.Text;

namespace d360.model
{
    [DbConfigurationType(typeof(AzureConfiguration))]
    public partial class CompanyContext : BaseContext, ICompanyContext
    {
        internal IQueueSource QueueSource;
        internal IStorageProvider Storage;

        readonly CommunityContext Community;

        bool IsEventingEnabled;

        public int ApiTimeout
        {
            get
            {
                return GetSettingValue<int>(Setting.ApiTimeout);
            }
        }

        #region Ctors

        public CompanyContext(ICommunityContext community, ICachingProvider caching, IQueueSource queueSource, ISecurityContextProvider context, IStorageProvider storage, bool skipCacheCheck = false)
            : base(community.GetCompanyConnectionString(skipCacheCheck))
        {
            Database.SetInitializer<CompanyContext>(null); //dont create any tables if they dont exist.

            Community = (CommunityContext)community;
            Caching = caching;
            QueueSource = queueSource;
            Storage = storage;

            CurrentCompanyID = context.CompanyID;
            CurrentDomainSettingID = context.DomainSettingID;
            CurrentResourceID = context.ResourceID;
            CurrentResourceIsAdmin = context.IsAdministrator;
            CurrentCompanyDomain = context.CompanyPrefix;

            //output queries in debug mode to console
            if (System.Diagnostics.Debugger.IsAttached)
            {
                this.Database.Log = s => System.Diagnostics.Debug.WriteLine(s);
            }

            var eventBusValue = (ConfigurationManager.AppSettings["EventBusTopicEnabled"] ?? "").ToUpper();

            if (eventBusValue == "TRUE")
            {
                IsEventingEnabled = true;
            }
            else
            {
                IsEventingEnabled = false;
            }
        }

        #endregion

        #region DbSets

        public DbSet<AssetProcessDiagram> AssetProcessDiagrams { get; set; }

        public DbSet<AssetTypeExportTemplate> AssetTypeExportTemplates { get; set; }

        public DbSet<AssetTypeExportTemplateStyle> AssetTypeExportTemplateStyles { get; set; }

        public DbSet<AssetTypeStyle> AssetTypeStyles { get; set; }

        public DbSet<Comment> Comments { get; set; }

        public DbSet<CommentRelation> CommentRelations { get; set; }

        public DbSet<CommentVote> CommentVotes { get; set; }

        public DbSet<ContractAcceptance> ContractAcceptance { get; set; }

        public DbSet<Favorite> Favorites { get; set; }

        public DbSet<Field> Fields { get; set; }

        public DbSet<FieldJsonProperty> FieldJsonProperties { get; set; }

        public DbSet<FieldLookupValue> FieldLookupValues { get; set; }                          /* VIEW */

        public DbSet<FieldWithRelation> FieldWithRelations { get; set; }                        /* VIEW */

        public DbSet<FieldType> FieldTypes { get; set; }

        public DbSet<FieldTypeLookup> FieldTypeLookups { get; set; }

        public DbSet<Follow> Follows { get; set; }

        public DbSet<FollowDetail> FollowDetails { get; set; }                                  /* VIEW */
                        
        public DbSet<GraphFilter> GraphFilters { get; set; }

        public DbSet<HelpResource> HelpResources { get; set; }

        public DbSet<Intersect> Intersects { get; set; }

        public DbSet<IntersectDetail> IntersectDetails { get; set; }                /* VIEW */

        public DbSet<IntersectTypeDetail> IntersectTypeDetails { get; set; }        /* VIEW */

        public DbSet<IntersectType> IntersectTypes { get; set; }

        public DbSet<Issue> Issues { get; set; }

        public DbSet<core.entities.IssueType> IssueTypes { get; set; }

        public DbSet<IssueTypeRelation> IssueTypeRelations { get; set; }

        public DbSet<Nym> Nyms { get; set; }

        public DbSet<NymRelation> NymRelations { get; set; }

        public DbSet<Predicate> Predicates { get; set; }

        public DbSet<Question> Questions { get; set; }

        public DbSet<QuestionType> QuestionTypes { get; set; }

        public DbSet<QuestionTypeOption> QuestionTypeOptions { get; set; }

        public DbSet<Report> Reports { get; set; }

        public DbSet<ReportResponsibility> ReportResponsibilities { get; set; }

        public DbSet<d360.core.entities.Rule> Rules { get; set; }

        public DbSet<SiteNav> SiteNav { get; set; }

        public DbSet<SiteNavPermission> SiteNavPermissions { get; set; }

        public DbSet<ShoppingCartType> ShoppingCartTypes { get; set; }

        public DbSet<ShoppingCart> ShoppingCarts { get; set; }

        public DbSet<ShoppingCartItem> ShoppingCartItems { get; set; }

        public DbSet<Shortcut> Shortcuts { get; set; }

        public DbSet<Survey> Surveys { get; set; }

        public DbSet<SurveyType> SurveyTypes { get; set; }

        public DbSet<AssetTypeLevel> AssetTypeLevels { get; set; }

        public DbSet<Tag> Tags { get; set; }
        public DbSet<ConnectorLabel> ConnectorLabels { get; set; }
        public DbSet<AssetTag> AssetTags { get; set; }

        public DbSet<AuditField> AuditFields { get; set; }

        public DbSet<Audit> Audits { get; set; }

        public DbSet<AssetDataProfile> AssetDataProfile { get; set; }
        public DbSet<AssetDataProfileSample> AssetDataProfileSample { get; set; }

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
                        }
                    });
                }
                catch
                {
                    // surpress exceptions
                }

                SaveChanges();
            }
        }

        public void Enqueue(string queueName, QueueObject item)
        {
            QueueSource.CreateMessage(queueName, item);
        }

        public JObject GetPageInformation(SystemObjects o, int oid)
        {
            var jsonRows = Database.Connection.Query<string>("exec GetPageInformation @o, @oid, @rid", new { o = o.ToString(), oid, rid = CurrentResourceID });
            if (jsonRows.Count() == 0)
            {
                return null;
            }

            var json = string.Concat(jsonRows);
            return JObject.Parse(json);
        }

        public async Task<IEnumerable<TypeIdentifierInfoModel>> GetTypeIdentifierInfoModel(TypeIdentifierInfoModelType type, Guid guid)
        {
            IEnumerable<TypeIdentifierInfoModel> result;
            switch (type)
            {
                case TypeIdentifierInfoModelType.ActionType:
                    result = await QueryAsync<TypeIdentifierInfoModel>("select null, Uid, 'IssueType' as Object, ID as ObjectID from IssueType where Uid = @uid", new { uid = guid }).ConfigureAwait(false);
                    break;
                case TypeIdentifierInfoModelType.AssetType:
                    result = await QueryAsync<TypeIdentifierInfoModel>("select ID, Uid, Object, ObjectID from AssetType where Uid = @uid", new { uid = guid });
                    break;
                case TypeIdentifierInfoModelType.RelationshipType:
                    result = await QueryAsync<TypeIdentifierInfoModel>("select null, Uid, 'IntersectType' as Object, ID as ObjectID from IntersectType where Uid = @uid", new { uid = guid }).ConfigureAwait(false);
                    break;
                default:
                    throw new ArgumentNullException("Invalid TypeIdentifierInfoModel.");
            }
            return result;
        }

        public Task<List<IntersectTypeApiViewModel>> GetActiveIntersectTypesByObjectType(int id, SystemObjects type)
        {
            return GetRelationshipTypes(null, $"where I.State = 1 and ((I.SubjectID = {id} and I.[Subject] = '{type.ToString()}') or (I.ObjectID = {id} and I.Object = '{type.ToString()}'))");
        }

        public List<AllocationPossibility> GetAllocationOptions()
        {
            var list = Database.Connection.Query<AllocationPossibility>($@"
select	T.Object as ObjectType, 
		T.ObjectID as ObjectTypeID, 
        T.[Class],
        P.[Path] as Name
from	AssetType T
        cross apply dbo.GetAssetTypeTextPathById(T.ID, ' / ') P
where	T.[Class] in (1,2,6,7,8,9)").ToList();

            list = list.OrderBy(i => i.ClassName).ThenBy(i => i.Name).ToList();

            return list;
        }

        public async Task<IEnumerable<AllowedIntersectionType>> GetAllowedIntersectionTypes(string type, int id)
        {
            return await Database.Connection
                .QueryAsync<AllowedIntersectionType>("GetAllowedIntersectionTypes @SourceType, @SourceTypeID",
                new
                {
                    SourceType = new Dapper.DbString { Value = type, IsAnsi = true, IsFixedLength = true, Length = 50 },
                    SourceTypeID = id
                }).ConfigureAwait(false);
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
				and Type not in ('DataTableSelect', 'ComplexRelationLookup', 'RelationLookup') --these are calculated fields, and should not be selectable.
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

            return await Database.Connection.QueryAsync<FieldFilterModel>(sql, new
            {
                SourceType = new Dapper.DbString { IsAnsi = true, IsFixedLength = true, Length = 50, Value = type.ToString() },
                SourceTypeID = id
            }).ConfigureAwait(false);
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

        public Dictionary<string, object> GetRelationshipFieldItems(int fieldTypeID, string @object = null, int? objectID = null, int offset = 0, int rows = 25, string query = null, bool includeSelection = true)
        {
            var ft = GetById<FieldType>(fieldTypeID);
            bool hasCardinalityOne = false;

            if (!ft.LookupObjectID.HasValue)
            {
                throw new ArgumentNullException("Invalid Relationship field encountered no relationship type to lookup found in definition.");
            }

            var sql = @"select
                                            [ID],
                                            [Subject],
                                            [SubjectID],
                                            [SubjectCardinality],
                                            [Object],
                                            [ObjectID],
                                            [ObjectCardinality],
                                            [PredicateID] from [dbo].[intersecttype] where ID = @ID";
            var intersectType = Database.Connection.QueryFirstOrDefault<IntersectType>(sql, new { ID = ft.LookupObjectID.Value });

            if (intersectType == null)
            {
                var error = new Dictionary<string, object>();
                error.Add("RelationshipError", "Invalid or deleted relationship type encountered on Relationship field " + ft.FriendlyName + ".");
                return error;
            }

            int count = 0, objID = 0;
            string countSql, obj, selectedSql;

            bool isSubject = (intersectType.Subject == ft.Object && intersectType.SubjectID == ft.ObjectID);
            bool sameSubjectObject = (intersectType.Subject == intersectType.Object && intersectType.SubjectID == intersectType.ObjectID);
            obj = isSubject ? intersectType.Object : intersectType.Subject;
            objID = isSubject ? intersectType.ObjectID : intersectType.SubjectID;

            var cardinalityCheckSQL = "";
            if (intersectType.SubjectCardinality == Cardinality.One)
            {
                if (isSubject)
                {
                    hasCardinalityOne = true;
                }

                cardinalityCheckSQL += " and not exists (select ID from [Intersect] where IntersectTypeID = @intersectTypeID and IT.SubjectCardinality = 1 and Object = {0} and ObjectID = {1} and I.Id is null)";
            }
            if (intersectType.ObjectCardinality == Cardinality.One)
            {
                if (!isSubject)
                {
                    hasCardinalityOne = true;
                }

                cardinalityCheckSQL += " and not exists (select ID from [Intersect] where IntersectTypeID = @intersectTypeID and IT.ObjectCardinality = 1 and Subject = {0} and SubjectID = {1} and I.Id is null)";
            }

            string intersectJoin = @"((I.[Subject] = {0} and I.SubjectID = {1} and I.[Object] = @fieldObject and I.ObjectID = @fieldObjectID) or
	                            (I.[Object] = {0} and I.ObjectID = {1} and I.[Subject] = @fieldObject and I.SubjectID = @fieldObjectID))";

            if (!sameSubjectObject)
            {
                if (isSubject)
                {
                    intersectJoin = @"(I.[Subject] = {0} and I.SubjectID = {1} and I.[Object] = @fieldObject and I.ObjectID = @fieldObjectID)";
                }
                else
                {
                    intersectJoin = @"(I.[Object] = {0} and I.ObjectID = {1} and I.[Object] = @fieldObject and I.ObjectID = @fieldObjectID)";
                }
            }

            string formattedCardinalityCheck = string.Format(cardinalityCheckSQL, $"'{obj.Replace("Type", "")}'", "AD.[ObjectId]");
            string formattedIntersectJoin = string.Format(intersectJoin, $"'{obj.Replace("Type", "")}'", "AD.[ObjectId]");

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
                        formattedCardinalityCheck = string.Format(cardinalityCheckSQL, "A.[Object]", "A.[ObjectId]");
                        formattedIntersectJoin = string.Format(intersectJoin, "A.[Object]", "A.[ObjectId]");

                        countSql = $@"select count(*) from AssetType A with (nolock)
                        inner join [IntersectType] IT on IT.Id = @intersectTypeID
 left join [Intersect] I on I.IntersectTypeID = @intersectTypeID and {formattedIntersectJoin}
                        where A.[Object] = @obj  and (@query is null or A.Name like '%' + @query + '%') {formattedCardinalityCheck}";
                        sql = $@"select  A.ObjectID as [Value], A.[Name] as [Text], case when I.ID is not null then 1 else 0 end as Selected
                            from AssetType A with (nolock)
                            inner join [IntersectType] IT on IT.Id = @intersectTypeID
                            left join [Intersect] I on I.IntersectTypeID = @intersectTypeID and {formattedIntersectJoin}
                            where A.[Object] = @obj and (@query is null or A.[Name] like '%' + @query + '%')
                            {formattedCardinalityCheck}
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
                        formattedCardinalityCheck = string.Format(cardinalityCheckSQL, "A.[Object]", "A.[ObjectId]");
                        formattedIntersectJoin = string.Format(intersectJoin, "A.[Object]", "A.[ObjectId]");

                        countSql = $@"select count(*) from Asset A with (nolock)
                        inner join AssetType T with (nolock) on T.ID = A.AssetTypeID
                        inner join [IntersectType] IT on IT.Id = @intersectTypeID
 left join [Intersect] I on I.IntersectTypeID = @intersectTypeID and {formattedIntersectJoin}
						cross apply dbo.GetAssetDisplayValueById(a.ID) D
                        where T.[Object] = @obj and T.ObjectID = @objID and (@query is null or D.DisplayValue like '%' + @query + '%')
                            and not (A.Object = @fieldObject and a.ObjectID = @fieldObjectID) {formattedCardinalityCheck}";

                        sql = $@"select  A.ObjectID as [Value], D.DisplayValue as [Text], case when I.ID is not null then 1 else 0 end as Selected
                            from Asset A with (nolock)
							inner join AssetType T with (nolock) on T.ID = A.AssetTypeID
                            inner join [IntersectType] IT on IT.Id = @intersectTypeID
							cross apply dbo.GetAssetDisplayValueById(a.ID) D
                            left join [Intersect] I on I.IntersectTypeID = @intersectTypeID and {formattedIntersectJoin}
                            where T.[Object] = @obj and T.ObjectID = @objID and (@query is null or D.DisplayValue like '%' + @query + '%')
                            and not (A.Object = @fieldObject and a.ObjectID = @fieldObjectID)                            
                            {formattedCardinalityCheck}
                            order by 3 desc, D.DisplayValue asc
                            OFFSET @offset ROWS FETCH NEXT @rows ROWS ONLY";
                    }

                    break;
                case "ArtifactType":
                case "PolicyType":
                case "RuleType":
                case "TaxonomyType":
                    formattedCardinalityCheck = string.Format(cardinalityCheckSQL, "A.[Object]", "A.[ObjectId]");
                    formattedIntersectJoin = string.Format(intersectJoin, "A.[Object]", "A.[ObjectId]");


                    countSql = $@"select count(*) from AssetWithType A with (nolock)
                        {(string.IsNullOrEmpty(query) ? "" : " cross apply GetAssetTextPathById(A.ID, '/') P ")}
                        inner join [IntersectType] IT on IT.Id = @intersectTypeID
 left join [Intersect] I on I.IntersectTypeID = @intersectTypeID and {formattedIntersectJoin}
                        where A.[Type] = @obj and A.TypeID = @objID and not (A.Object = @fieldObject and A.ObjectID = @fieldObjectID)
                        {(string.IsNullOrEmpty(query) ? "" : " and (P.TextPath like '%' + @query + '%')")}
                        {formattedCardinalityCheck}";
                    sql = $@"select distinct A.ObjectID as Value, P.TextPath as Text, case when I.ID is not null then 1 else 0 end as Selected 
                            from AssetWithType A with (nolock)
                            cross apply GetAssetTextPathById(A.ID, '/') P 
                            inner join [IntersectType] IT on IT.Id = @intersectTypeID
                            left join [Intersect] I on I.IntersectTypeID = @intersectTypeID and {formattedIntersectJoin}
                            where A.[Type] = @obj and A.TypeID = @objID and (@query is null or P.TextPath like '%' + @query + '%')
                                and not (A.Object = @fieldObject and a.ObjectID = @fieldObjectID) {formattedCardinalityCheck}
                            order by 3 desc, P.TextPath asc
                            OFFSET @offset ROWS FETCH NEXT @rows ROWS ONLY";
                    break;
                case "FusionAttributeType":
                    formattedCardinalityCheck = string.Format(cardinalityCheckSQL, "'FusionAttribute'", "F.Id");
                    formattedIntersectJoin = string.Format(intersectJoin, "'FusionAttribute'", "F.Id");

                    countSql = $@"select count(*) from FusionAttribute F
                                    inner join Fusion FF on FF.ID = F.FusionID
                                    inner join [IntersectType] IT on IT.Id = @intersectTypeID
                        left join [Intersect] I on I.IntersectTypeID = @intersectTypeID and {formattedIntersectJoin}
                                where FusionAttributeTypeID = @objID and F.Deleted = 0 and (@query is null or F.TextPath like '%' + @query + '%')
                                {formattedCardinalityCheck}";
                    sql = $@"select F.ID as Value, FF.Name + '.' + F.TextPath as Text, case when I.ID is not null then 1 else 0 end as Selected   
                            from FusionAttribute F with (nolock)
                            inner join Fusion FF on FF.ID = F.FusionID
                            inner join [IntersectType] IT on IT.Id = @intersectTypeID
                            left join [Intersect] I on I.IntersectTypeID = @intersectTypeID and {formattedIntersectJoin}
                            where F.FusionAttributeTypeID = @objID and F.Deleted = 0 and (@query is null or F.TextPath like '%' + @query + '%')
                            {formattedCardinalityCheck}
                            order by 3 desc, TextPath asc
                            OFFSET @offset ROWS FETCH NEXT @rows ROWS ONLY";
                    break;
                case "ResourceType":
                    formattedCardinalityCheck = string.Format(cardinalityCheckSQL, "'Resource'", "R.ResourceID");
                    formattedIntersectJoin = string.Format(intersectJoin, "'Resource'", "R.ResourceID");

                    countSql = $@"select count(*) from reporting.Global_Resource R 
                                inner join [IntersectType] IT on IT.Id = @intersectTypeID
                            left join [Intersect] I on I.IntersectTypeID = @intersectTypeID and {formattedIntersectJoin}
                                    where (@query is null or R.LastName + ', ' + R.FirstName like '%' + @query + '%')
                                and not ('Resource' = @fieldObject and R.ResourceID = @fieldObjectID)
                               {formattedCardinalityCheck}";
                    sql = $@"select R.ResourceID as Value, R.LastName + ', ' + R.FirstName as Text, case when I.ID is not null then 1 else 0 end as Selected 
                            from reporting.[Global_Resource] R
                            inner join [IntersectType] IT on IT.Id = @intersectTypeID
                            left join [Intersect] I on I.IntersectTypeID = @intersectTypeID and {formattedIntersectJoin}
                            where (@query is null or R.LastName + ', ' + R.FirstName like '%' + @query + '%')
                                and not ('Resource' = @fieldObject and R.ResourceID = @fieldObjectID)
                            {formattedCardinalityCheck}
                            order by 3 desc, R.LastName + ', ' + R.FirstName asc  
                            OFFSET @offset ROWS FETCH NEXT @rows ROWS ONLY";
                    break;
                default:
                    throw new InvalidOperationException("Unexpected object type = " + obj);
            }

            if (offset == 0 || query != null)
            {
                count = Database.Connection.QueryFirstOrDefault<int>(countSql, new { obj, objID, query, fieldObject = @object ?? obj, fieldObjectID = objectID ?? objID, intersectTypeID = intersectType.ID });
            }

            List<dynamic> selected = null, items = null;

            if (includeSelection)
            {
                selected = Query<dynamic>(selectedSql, new { obj = @object, objID = objectID, intersectTypeID = intersectType.ID }).ToList();
            }

            if (!includeSelection)
            {
                items = Query<dynamic>(sql, new { offset, rows, query, obj, objID, fieldObject = @object ?? obj, fieldObjectID = objectID ?? objID, intersectTypeID = intersectType.ID }).ToList();
            }

            var dict = new Dictionary<string, object>();

            if (includeSelection)
            {
                dict.Add("Selection", selected.ToList());
            }
            if (!includeSelection)
            {
                dict.Add("Items", items.ToList());
            }
            dict.Add("Count", count);
            dict.Add("HasCardinalityOne", hasCardinalityOne);

            return dict;
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
		TypeName,
		Type,
	    TypeID,
        uid
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
where   [ObjectID] = @id and [Object] = @type", new { id = objectId, type = new DbString { Value = objectType, IsFixedLength = true, Length = 20, IsAnsi = true } }).SingleOrDefault();

            return model;
        }

        public ObjectDetail GetObjectDetail(string type, long id)
        {
            var model = Database.Connection.QuerySingleOrDefault<ObjectDetail>("SELECT * FROM utility.ObjectDetail(@type, @id)", new { type = new DbString { Value = type, IsAnsi = true, Length = 50 }, id });

            if ((model != null) && PluralCultureHelper.IsNeutralCultureEnglish())
            {
                var pluralize = PluralizationService.CreateService(System.Globalization.CultureInfo.CurrentCulture);
                model.PluralizedName = pluralize.Pluralize(model.Name ?? "");
            }
            return model;
        }

        public string GetObjectTypePath(string type, long id)
        {
            string query = string.Format("SELECT utility.GetObjectTypePath('{0}', {1}) as path", type, id);
            return Database.SqlQuery<string>(query).SingleOrDefault();
        }

        public AssetTypeStyle GetAssetTypeStyle(int assetTypeId)
        {
            return Filter<AssetTypeStyle>(i => i.ID == assetTypeId).FirstOrDefault();
        }

        public AssetTypeStyle GetAssetTypeStyle(Guid assetTypeUid)
        {
            var assetType = Filter<AssetType>(i => i.uid == assetTypeUid).FirstOrDefault();
            if (assetType != null)
            {
                return GetAssetTypeStyle(assetType.ID);
            }
            return null;
        }

        public AssetTypeStyle GetAssetTypeStyle(string type, int id)
        {
            var assetType = Filter<AssetType>(i => i.Object == type && i.ObjectID == id).FirstOrDefault();
            if (assetType != null)
            {
                return GetAssetTypeStyle(assetType.ID);
            }
            return null;
        }

        public AssetType GetParentType(int id, SystemObjects obj)
        {
            if (id < 0)
            {
                return null;
            }

            var sql = @"select a.id from IntersectType I
                               inner join [Predicate] P on P.ID = I.PredicateID
            inner join AssetType a on a.object = i.subject and a.objectid = i.subjectid
                               where P.[Type] = @type and i.[Object] = @object and i.[ObjectID] = @objectId";

            var parentId = Query<int>(sql, new { type = (int)PredicateType.InterTypeHierarchy, @object = obj.ToString(), objectId = id }).FirstOrDefault();

            if (parentId < 1)
            {
                return null;
            }

            return GetById<AssetType>(parentId);
        }

        public AssetType GetParentTypeById(long assetTypeId)
        {
            if (assetTypeId < 0)
            {
                return null;
            }

            var assetType = GetById<AssetType>(((int)assetTypeId));

            if (assetType == null)
            {
                return null;
            }

            return GetParentType(assetType.ObjectID, SystemObjectHelper.GetSystemObjects(assetType.Class));
        }

        public string GetIntersectTypeName(IntersectType intersectType)
        {
            string @sql = "SELECT * FROM [dbo].[GetIntersectTypeNames] (@id)";
            var itName = Query<string>(sql, new { id = intersectType.ID }).FirstOrDefault();

            return itName != null ? itName : "Name";
        }

        public bool TypeHasParent(SystemObjects type, int id, PredicateType parentFunctionalType = PredicateType.InterTypeHierarchy)
        {

            var sql = @"select 1 from IntersectType I
                    inner join [Predicate] P on P.ID = I.PredicateID
                    where P.[Type] = @type and [Object] = @object and ObjectID = @objectId";

            return Query<dynamic>(sql, new { type = (int)parentFunctionalType, @object = new DbString { Value = type.ToString(), IsAnsi = true, Length = 50, IsFixedLength = true }, objectId = id }).Any();
        }

        public AssetDetail GetParentObject(int id, SystemObjects obj)
        {
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


            if (id < 0)
            {
                return default(AssetDetail);
            }

            var sql = @"select a.Id from PredicateIntersect I
                    inner join IntersectType T on T.ID = I.IntersectTypeID
                    inner join Asset a on a.object = i.subject and a.objectid = i.subjectid
                    where I.PredicateType = @type and I.[Object] = @obj and I.ObjectID = @objectId";

            var parentId = Query<int>(sql, new { type = (int)predicateType, obj = new DbString { Value = obj.ToString(), IsFixedLength = true, Length = 20, IsAnsi = true }, objectId = id }).FirstOrDefault();
            if (parentId < 1)
            {
                return default(AssetDetail);
            }

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
            return (GetFollowingParent(type, objectID, resourceID) != null);
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
                        default:
                            followType = FollowType.Single;
                            break;
                    }

                    if (includeChildren || objectID == 0)
                    {
                        followType = FollowType.Parent;
                    }

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
            {
                throw new NotFoundException("Subject");
            }

            if (objectDetail == null)
            {
                throw new NotFoundException("Object");
            }

            if (subject == "ReferenceItemType")
            {
                subjectDetail.TypeID = 0;
            }

            if (@object == "ReferenceItemType")
            {
                objectDetail.TypeID = 0;
            }

            var intersectType = GetById<IntersectType>(intersectTypeID);

            if (intersectType == null)
            {
                throw new NotFoundException("Intersect Type");
            }

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
            {
                throw new NotFoundException("Subject");
            }

            if (objectDetail == null)
            {
                throw new NotFoundException("Object");
            }

            var intersectType = GetById<IntersectType>(intersectTypeID);

            if (intersectType == null)
            {
                throw new NotFoundException("Intersect Type");
            }

            if (
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
            if (item == null)
            {
                throw new NotFoundException("Relationship");
            }
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


        public List<IntersectTypeOption> GetIntersectTypeOptions(
            SystemObjects? subject = null, int? subjectID = null,
            SystemObjects? @object = null, int? objectID = null,
            int? predicateID = null,
            List<AssetTypeClass> limitToClasses = null)
        {
            string noClassLimitSql = "";
            string classLimitSql = "";
            var dbArgs = new DynamicParameters();


            List<string> excludedClasses = new List<string>
            {
                SystemObjects.FusionType.ToString(),
                SystemObjects.OrganizationType.ToString(),
                SystemObjects.FusionAttributeType.ToString()
            };

            if (limitToClasses != null && limitToClasses.Count > 0)
            {
                classLimitSql = " and T.[Class] in (" + string.Join(",", limitToClasses.Select(i => (int)i)) + ")";
            }

            var predicate = Predicates.FirstOrDefault(x => x.ID == predicateID);
            string excludeClassInStatement = string.Join(",", excludedClasses.Select(x => "'" + x + "'"));
            string whereStatement = "";

            if (predicate != null && predicate.Type == PredicateType.DiagramReference)
            {
                whereStatement = $@" and exists(select top 1 it.id from intersecttype it
				 inner join Predicate p on it.PredicateID = p.ID and p.Type = {(int)PredicateType.Diagram}
				 where it.subject = T.object and it.subjectid = T.objectid
				)";
            }

            if (subject.HasValue && subjectID.HasValue && limitToClasses != null)
            {
                if (limitToClasses.Contains(AssetTypeClass.Reference))
                {
                    noClassLimitSql += @" UNION SELECT	0 as ID, 'Reference :: List' as Name, 'ReferenceItemType' as Type";
                }
            }

            var sql = $@"
    SELECT		I.ID,
				I.Name,
				I.Type
	FROM		(
                select	T.ObjectID as ID,
		                case 
			                when T.Object = 'ArtifactType' and T.[Class] = 1 then '{CommonNames.AssetTypeClass_Business.CleanForSql()} :: '
                            when T.Object = 'ArtifactType' and T.[Class] = 8 then '{CommonNames.AssetTypeClass_Technical.CleanForSql()} :: '
			                when T.Object = 'FusionAttributeType' then 'Fusion Attribute :: ' + FT.Name + ' / '
			                when T.Object = 'GroupType' then 'Security :: '
			                when T.Object = 'PolicyType' then '{CommonNames.AssetTypeClass_Policy.CleanForSql()} :: '
			                when T.Object = 'ReferenceItemType' then 'Reference :: '
			                when T.Object = 'ResourceType' then 'Security :: '
                            when T.Object = 'RuleType' then '{CommonNames.AssetTypeClass_Rule.CleanForSql()} :: '
                            when T.Object = 'TaskType' and T.[Class] = 15 then '{CommonNames.AssetTypeClass_Task.CleanForSql()} :: ' 
                            when T.Object = 'TaxonomyType' then '{CommonNames.AssetTypeClass_Model.CleanForSql()} :: '
		                end + coalesce(P.[Path], T.Name) as Name,
		                T.Object as Type
                from	AssetType T
		                cross apply dbo.GetAssetTypeTextPathById(T.ID, '/') P
                        left join FusionAttributeType FAT on T.Object = 'FusionAttributeType' and FAT.ID = T.ObjectID 
                        left join FusionType FT on FT.ID = FAT.FusionTypeID 
                where	T.Object not in ({excludeClassInStatement}){classLimitSql}
			 	{noClassLimitSql}{whereStatement}
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

            return Database.Connection.Query<IntersectTypeOption>(sql, dbArgs).ToList();
        }

        public List<Predicate> GetPredicateOptions(int lineageVersion, SystemObjects subject, int subjectID, SystemObjects? @object = null, int? objectID = null, int? predicateID = null)
        {
            var sSubject = subject.ToString();
            var allowedFunctionalTypes = PredicateType.Simple.GetAsList().Where(p => p.AllowIntersectTypeAssignment && p.AllowEditFromRelationshipEditor).ToList();

            if (sSubject == "IntersectType")
            {
                allowedFunctionalTypes.RemoveAll(p => !p.AllowIntersectTypeAsSubject);
            }
            else
            {
                var subjectAssetType = Filter<AssetType>(i => i.Object == sSubject && i.ObjectID == subjectID).FirstOrDefault();
                if (subjectAssetType == null)
                {
                    throw new ArgumentNullException("Subject asset type does not exist.");
                }
                allowedFunctionalTypes.RemoveAll(p => !p.SubjectAssetClassesSupported.Contains(subjectAssetType.Class));
            }

            var sql = @"
select	P.*
from	[Predicate] P
		left join IntersectType I on I.PredicateID = P.ID 
			and I.Subject = @s and I.SubjectID = @sid 
			and ( (@o is null) or (@o is not null and I.Object = @o and I.ObjectID = @oid) )
			and ( (@pid is null) or (@pid is not null and I.PredicateID <> @pid) )
where	I.ID is null";

            var predicates = Query<Predicate>(sql, new
            {
                s = new DbString { IsAnsi = true, Value = subject.ToString() },
                sid = subjectID,
                o = new DbString { IsAnsi = true, Value = @object.ToString() },
                oid = objectID,
                pid = predicateID
            }).ToList()
            .Where(i => i.Type.AsInfoModel().AllowIntersectTypeAssignment &&
                        i.Type.AsInfoModel().AllowEditFromRelationshipEditor &&
                        i.Type.AsInfoModel().LineageVersionsSupported.Contains(lineageVersion)
                  );

            predicates = predicates.Where(i => i.Type.In(allowedFunctionalTypes.Select(p => p.ID).ToArray()));

            return predicates.ToList();
        }

        public IntersectType UpsertIntersectType(IntersectType model, int lineageVersion)
        {
            var predicateModel = GetById<Predicate>(model.PredicateID.Value);

            if (!predicateModel.Type.AsInfoModel().AllowIntersectTypeAssignment)
            {
                throw new GenericException(System.Net.HttpStatusCode.Conflict, "Predicate", "Not allowed to add a relationship type with this predicate.");
            }
            if (($"{model.Subject}{model.SubjectID}" != $"{model.Object}{model.ObjectID}") && !predicateModel.Type.AsInfoModel().AllowDifferentSubjectObject)
            {
                throw new GenericException(System.Net.HttpStatusCode.Conflict, "Predicate", "The subject and object must be the same when using this Predicate.");
            }
            if (($"{model.Subject}{model.SubjectID}" == $"{model.Object}{model.ObjectID}") && predicateModel.Type.AsInfoModel().ForceDifferentSubjectObject)
            {
                throw new GenericException(System.Net.HttpStatusCode.Conflict, "Predicate", "The subject and object may not be the same when using this Predicate.");
            }

            if (!predicateModel.Type.AsInfoModel().LineageVersionsSupported.Contains(lineageVersion))
            {
                throw new GenericException(System.Net.HttpStatusCode.Conflict, "Predicate", $"Your current version of lineage does not support using this predicates of type {predicateModel.Type.AsInfoModel().Name}.");
            }

            AssetType subjectAssetType = null;
            AssetType objectAssetType = null;
            subjectAssetType = Filter<AssetType>(i => i.Object == model.Subject && i.ObjectID == model.SubjectID).FirstOrDefault();
            objectAssetType = Filter<AssetType>(i => i.Object == model.Object && i.ObjectID == model.ObjectID).FirstOrDefault();

            model.SubjectUid = subjectAssetType?.uid;
            model.ObjectUid = objectAssetType?.uid;

            var predicateInfo = predicateModel.Type.AsInfoModel();

            if (!predicateInfo.SubjectAssetClassesSupported.Contains(subjectAssetType.Class))
            {
                throw new GenericException(System.Net.HttpStatusCode.Conflict, "Predicate", $"When using this predicate your subject must be one of the following classes : {predicateInfo.SubjectAssetClassesSupported}.");
            }
            if (!predicateInfo.ObjectAssetClassesSupported.Contains(objectAssetType.Class))
            {
                throw new GenericException(System.Net.HttpStatusCode.Conflict, "Predicate", $"When using this predicate your object must be one of the following classes : {predicateInfo.ObjectAssetClassesSupported}.");
            }

            if (predicateModel.Type == PredicateType.BusinessToTechnical || predicateModel.Type == PredicateType.Transformation)
            {
                if (predicateModel.Type == PredicateType.Transformation)
                {
                    if (!subjectAssetType.UseAsTransformation && !objectAssetType.UseAsTransformation)
                    {
                        throw new GenericException(System.Net.HttpStatusCode.Conflict, "Predicate", $"When using this predicate either your subject or object must support being used as a transformation.");
                    }
                    if (subjectAssetType.UseAsTransformation && objectAssetType.UseAsTransformation)
                    {
                        throw new GenericException(System.Net.HttpStatusCode.Conflict, "Predicate", $"When using this predicate either your subject or object must support being used as a transformation, but not both.");
                    }
                }
            }

            if (model.ID > 0)
            {
                Update(model);
            }
            else
            {
                Add(model);
            }

            return model;
        }

        #endregion

        #region Social

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
            string query = string.Format("GetRenderedTemplateBodyNg '{0}', '{1}', {2}, '{3}', '{4}', {5}", templateType, type.ToString(), id, action, string.Empty, CurrentResourceID);
            var model = Database.SqlQuery<RenderTemplateModel>(query).SingleOrDefault();
            var html = "";
            if (model != null)
            {
                html = model.Body;
            }
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
                        AddQE(events, ChangeType.Delete, new EventObjectInfo
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

        public async Task<T> GetDatabaseJsonAsObjectAsync<T>(string query, DynamicParameters dbArgs, int timeout = 90)
        {
            var jsonStrings = await QueryAsync<string>(query, dbArgs, timeout).ConfigureAwait(false);
            var json = string.Join("", jsonStrings);

            return JsonConvert.DeserializeObject<T>(json);
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FieldTypeLookup>().HasRequired(t => t.FieldType).WithOptional(t => t.FieldTypeLookup).WillCascadeOnDelete(true);

            modelBuilder.Entity<AssetTypeStyle>().HasRequired(t => t.AssetType).WithOptional(t => t.AssetTypeStyle).WillCascadeOnDelete(true);

            modelBuilder.Entity<MetricAssetVersionConditionItemValue>().HasRequired(t => t.Item).WithMany(t => t.Values).WillCascadeOnDelete(true);
            modelBuilder.Entity<MetricAssetVersionConditionItem>().HasRequired(t => t.Condition).WithMany(t => t.Items).WillCascadeOnDelete(true);
            modelBuilder.Entity<MetricAssetVersionCondition>().HasRequired(t => t.Version).WithMany(t => t.Conditions).WillCascadeOnDelete(true);

            modelBuilder.Entity<FieldType>().Property(x => x.MinimumLength).HasPrecision(38, 18);
            modelBuilder.Entity<FieldType>().Property(x => x.MaximumLength).HasPrecision(38, 18);
            modelBuilder.Entity<FieldType>().Property(x => x.Increment).HasPrecision(38, 18);

            modelBuilder.Entity<FieldWithRelation>().Property(x => x.MinimumLength).HasPrecision(38, 18);
            modelBuilder.Entity<FieldWithRelation>().Property(x => x.MaximumLength).HasPrecision(38, 18);

            modelBuilder.Entity<core.entities.Rule>().Property(x => x.Threshold).HasPrecision(4, 3);

            
            modelBuilder.Entity<Question>().HasMany<QuestionTypeOption>(i => i.QuestionTypeOptions).WithMany(i => i.Questions).Map(i =>
            {
                i.MapLeftKey("QuestionID").MapRightKey("QuestionTypeOptionID").ToTable("QuestionOption");
            });

            modelBuilder.Entity<Score>().HasMany<ScoreItem>(i => i.Items).WithMany(i => i.Scores).Map(i =>
            {
                i.MapLeftKey("ScoreUid").MapRightKey("ScoreItemUid").ToTable("ScoreItemLink", "metrics");
            });

            base.OnModelCreating(modelBuilder);
        }

        public IEnumerable<T> Query<T>(string sql, object param = null, int timeout = 90)
        {
            return Database.Connection.Query<T>(sql, param, null, false, timeout);
        }

        public async Task<IEnumerable<TReturn>> QueryAsync<TFirst, TSecond, TReturn>(string sql, Func<TFirst, TSecond, TReturn> map, string splitOn, object param = null, int timeout = 90)
        {
            return await Database.Connection.QueryAsync<TFirst, TSecond, TReturn>(sql, map: map, param: param, splitOn: splitOn).ConfigureAwait(false);
        }

        public async Task<IEnumerable<dynamic>> QueryAsync(string sql, object param = null, int timeout = 90)
        {
            return await Database.Connection.QueryAsync(sql, param, null, timeout).ConfigureAwait(false);
        }

        public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object param = null, int timeout = 90)
        {
            return await Database.Connection.QueryAsync<T>(sql, param, null, timeout).ConfigureAwait(false);
        }
        public async Task<T> QueryFirstOrDefaultAsync<T>(string sql, object param = null, int timeout = 90)
        {
            return await Database.Connection.QueryFirstOrDefaultAsync<T>(sql, param, null, timeout);
        }

        public async Task<SqlMapper.GridReader> QueryMultipleAsync(string sql, object param = null, int timeout = 90)
        {
            return await Database.Connection.QueryMultipleAsync(sql, param, null, timeout);
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
            {
                exists = Query<bool>("select dbo.CheckIfObjectExistsWithParent(@t, @tid, @oid, @f, 0) as Val", new { t = attr.Type.ToString(), tid = attr.TypeID, oid = entity.ID, f = fieldsJson }).First();
            }
            else
            {
                exists = Query<bool>("select dbo.CheckIfObjectExistsWithParent(@t, @tid, null, @f, @p) as Val", new { t = attr.Type.ToString(), tid = attr.TypeID, f = fieldsJson, p = parentId }).First();
            }

            if (exists)
            {
                throw new ArgumentException($"{attr.Object} already exists.");
            }


            bool returnValue = true;

            if (isUpdate)
            {
                ObjectContext.ObjectStateManager.ChangeObjectState(entity, EntityState.Modified);
            }
            else
            {
                returnValue = Add<T>(entity);

                //Disable eventing after adding so update event doesnt trigger and cause duplicates without changed field
                this.IsEventingEnabled = false;
            }

            if (fields != null && fields.Count > 0)
            {
                fields.ForEach(i =>
                {
                    i.ObjectID = entity.ID;
                });
                AddOrUpdateFields(fields);
            }
            else
            {
                SaveChanges();
            }

            this.IsEventingEnabled = true;
            CreateOrUpdateDisplayValue(0, attr.Type.ToString(), entity.ID);

            return returnValue;
        }
                
        public void CreateOrUpdateDisplayValue(long assetId, string objectType = "", int objectId = -1)
        {
            Database.Connection.Execute("exec GenerateAssetDisplayValue @assetID, @objType,@objId", new { assetID = assetId, objId = objectId, objType = new DbString { Value = objectType.Replace("Type", ""), IsFixedLength = true, Length = 20, IsAnsi = true } }, null, 2400);
        }

        public void CreateOrUpdateTypeDisplayValuesAsync(int objectTypeId, string objectType)
        {
            //check if assettype is part of custom folder, if so, update its value
            var navSiteItem = SiteNav.FirstOrDefault(x => x.Object == objectType && x.ObjectID == objectTypeId && x.ParentID != null);
            if (navSiteItem != null)
            {
                var assetTypeName = AssetTypes.FirstOrDefault(x => x.Object == objectType && x.ObjectID == objectTypeId)?.Name;
                if (!string.IsNullOrEmpty(assetTypeName))
                {
                    navSiteItem.Title = navSiteItem.Name = assetTypeName;
                    SaveChanges();
                }
            }

            Enqueue(Config.GetValue<string>("DisplayValueQueue"), new DisplayUpdateInfo { CompanyID = CurrentCompanyID, ObjectTypeID = objectTypeId, ObjectType = objectType });
        }

        public void RebuildAssetGraphRequest()
        {
            Enqueue(Config.GetValue<string>("AssetGraphQueue"), new RebuildAssetGraphModel { CompanyID = CurrentCompanyID });
        }

        public void RebuildDisplayValuesRequest()
        {
            Enqueue(Config.GetValue<string>("DisplayValueQueue"), new DisplayUpdateInfo { CompanyID = CurrentCompanyID, RebuildAll = true });
        }

        public void RebuildIndexRequest()
        {
            Enqueue(Config.GetValue<string>("SearchIndexQueue"), new ReindexModel { CompanyID = CurrentCompanyID });
        }

        private void AddQE(List<EventInfo> events, ChangeType action, EventObjectInfo item)
        {
            // if assettype id is specified lookup object type info as workflow subscriber still works off object objectid...
            if (item.AssetTypeID > 0 && item.ObjectTypeID <= 0)
            {
                var assetType = AssetTypes.FirstOrDefault(x => x.ID == item.AssetTypeID);

                if (assetType != null)
                {
                    item.ObjectType = (SystemObjects)(Enum.Parse(typeof(SystemObjects), assetType.Object));
                    item.ObjectTypeID = assetType.ObjectID;
                }
            }

            events.Add(new EventInfo
            {
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

                #region Business logic : IUIDMetadata
                if (entry.Entity is IUIDMetadata)
                {
                    var o = entry.Entity as IUIDMetadata;
                    o.UID = Guid.NewGuid();
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

                #region Business logic : Field
                if (entry.Entity is Field)
                {

                    var field = (Field)entry.Entity;

                    field.UpdatedOn = DateTime.UtcNow;

                    if (field.FieldType != null && (field.FieldType.Type == "Html" || field.FieldType.Type == "Link"))
                    {
                        var sanitizer = new HtmlSanitizer();
                        sanitizer.AllowedSchemes.Add("data");
                        field.Value = sanitizer.Sanitize(field.Value);
                    }

                    // Need to determine if this field value has changed if not we dont want to tell workflow
                    // added items have not changed from the previous value so ignore.
                    // please dont run a query for each modified field if there are 10 we are running 10 queries.  Do one query.
                    if (entry.State != EntityState.Added)
                    {
                        fieldsToCheckForChanges.Add(field);
                    }

                }
                #endregion

                #region Business logic : FieldType
                if (entry.Entity is FieldType)
                {
                    var o = entry.Entity as FieldType;

                    if (entry.State == EntityState.Added)
                    {

                        if (Any<FieldType>(i => i.Object == o.Object && i.ObjectID == o.ObjectID && i.Name == o.Name))
                        {
                            throw new ArgumentException(Messages.Error_NameTaken);
                        }
                    }
                    if (entry.State == EntityState.Deleted)
                    {
                        if (o.Type == DataType.JSON.ToString())
                        {
                            var count = Query<int>("select count(1) from FieldType T cross apply openjson(T.[Definition]) with (FieldTypeID int '$.FieldTypeID') D where AssetTypeID = @at and [Type] = 'JsonElement' and D.FieldTypeID = @ft", new { at = o.AssetTypeID, ft = o.ID }).Single();
                            if (count > 0)
                            {
                                throw new ArgumentException(Messages.Error_Item_FieldJsonAttributeReferences);
                            }
                        }
                    }
                    if (entry.State == EntityState.Modified)
                    {
                        if (Any<FieldType>(i => i.Object == o.Object && i.ObjectID == o.ObjectID && i.Name == o.Name && i.ID != o.ID))
                        {
                            throw new ArgumentException(Messages.Error_NameTaken);
                        }
                    }
                }
                #endregion
                                
                #region Business logic : Group
                if (entry.Entity is Group)
                {
                    var o = entry.Entity as Group;
                    if (entry.State == EntityState.Added)
                    {
                        if (Any<Group>(i => i.Name == o.Name))
                        {
                            throw new ArgumentException(Messages.Error_NameTaken);
                        }
                    }
                    if (entry.State == EntityState.Modified)
                    {
                        if (Any<Group>(i => i.Name == o.Name && i.ID != o.ID))
                        {
                            throw new ArgumentException(Messages.Error_NameTaken);
                        }
                    }
                    if (entry.State == EntityState.Deleted)
                    {
                        if (Any<ResponsibilityTypeRelationOverrideItem>(i => i.SecurityAsset == "G" && i.SecurityAssetID == o.ID))
                        {
                            throw new ConflictException(string.Format(Messages.Error_NotRemoved_Tokenized, o.Name), Messages.Error_ResponsibilitiesAssignedToGroup);
                        }
                    }
                }
                #endregion

                #region Business logic : Intersect
                if (entry.Entity is Intersect)
                {
                    var o = entry.Entity as Intersect;
                    var id = o.ID.ToString();
                    var intersectTypeID = o.IntersectTypeID;
                    if (entry.State == EntityState.Deleted)
                    {
                        var any = Any<Field>(f => f.FieldType.LookupObjectType == "Intersect" && f.FieldType.LookupObjectID == intersectTypeID && f.Value == id);
                        if (any)
                        {
                            throw new ConflictException("Relationship Could not be Removed", "One or more fields reference this relationship.");
                        }
                        any = Any<Intersect>(i => (i.Subject == "Intersect" && i.SubjectID == o.ID) || (i.Object == "Intersect" && i.ObjectID == o.ID));
                        if (any)
                        {
                            throw new ConflictException("Relationship Could not be Removed", "One or more relationships reference this relationship.");
                        }
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

                    if (entry.State == EntityState.Added)
                    {
                        o.uid = Guid.NewGuid();
                        var addCheck = Query<string>(sql).SingleOrDefault();
                        if (!string.IsNullOrEmpty(addCheck))
                        {
                            throw new ConflictException("Relationship Type Cannot Be Created", addCheck);
                        }
                    }
                    else if (entry.State == EntityState.Modified)
                    {
                        var updateCheck = Query<string>(sql).SingleOrDefault();
                        if (!string.IsNullOrEmpty(updateCheck))
                        {
                            throw new ConflictException("Relationship Type Cannot Be Updated", updateCheck);
                        }
                    }
                }
                #endregion

                #region Business logic : AssetType
                if (entry.Entity is AssetType)
                {
                    var o = entry.Entity as AssetType;
                    if (string.IsNullOrWhiteSpace(o.Name))
                    {
                        throw new ArgumentException(Messages.Error_Name_Required);
                    }
                }
                #endregion

                #region Business logic : QuestionType
                if (entry.Entity is QuestionType)
                {
                    var o = entry.Entity as QuestionType;
                    if (entry.State == EntityState.Added)
                    {
                        if (Any<QuestionType>(i => i.SurveyTypeID == o.SurveyTypeID && i.Name == o.Name))
                        {
                            throw new ArgumentException(Messages.Error_NameTaken);
                        }
                    }
                    if (entry.State == EntityState.Modified)
                    {
                        if (Any<QuestionType>(i => i.SurveyTypeID == o.SurveyTypeID && i.Name == o.Name && i.ID != o.ID))
                        {
                            throw new ArgumentException(Messages.Error_NameTaken);
                        }
                    }
                }
                #endregion

                #region Business logic : Report
                if (entry.Entity is Report)
                {
                    var o = entry.Entity as Report;
                    if (entry.State == EntityState.Added)
                    {
                        if (Any<Report>(i => i.Name == o.Name))
                        {
                            throw new ArgumentException(Messages.Error_NameTaken);
                        }
                    }
                    if (entry.State == EntityState.Modified)
                    {
                        if (Any<Report>(i => i.Name == o.Name && i.ID != o.ID))
                        {
                            throw new ArgumentException(Messages.Error_NameTaken);
                        }
                    }
                }
                #endregion

                #region Business logic : ResponsibilityType
                if (entry.Entity is ResponsibilityType)
                {
                    var o = entry.Entity as ResponsibilityType;
                    if (entry.State == EntityState.Added)
                    {
                        if (Any<ResponsibilityType>(i => i.Name == o.Name))
                        {
                            throw new ArgumentException(Messages.Error_NameTaken);
                        }
                    }
                    if (entry.State == EntityState.Modified)
                    {
                        if (Any<ResponsibilityType>(i => i.Name == o.Name && i.ID != o.ID))
                        {
                            throw new ArgumentException(Messages.Error_NameTaken);
                        }

                    }
                    if (entry.State == EntityState.Deleted)
                    {
                        if (Any<ResponsibilityDetail>(i => i.ResponsibilityTypeID == o.ID))
                        {
                            throw new ArgumentException(Messages.Error_ResponsibilityType_ExistingResponsibilities);
                        }

                    }
                }
                #endregion

                #region Business logic : RuleType

                if (entry.Entity is RuleType)
                {
                    var o = entry.Entity as RuleType;
                    if (entry.State == EntityState.Added)
                    {
                        if (Any<RuleType>(i => i.Name == o.Name))
                        {
                            throw new ArgumentException(Messages.Error_NameTaken);
                        }
                    }
                    if (entry.State == EntityState.Modified)
                    {
                        if (Any<RuleType>(i => i.Name == o.Name && i.ID != o.ID))
                        {
                            throw new ArgumentException(Messages.Error_NameTaken);
                        }
                    }
                }

                #endregion

                #region Business logic : SurveyType
                if (entry.Entity is SurveyType)
                {
                    var o = entry.Entity as SurveyType;
                    if (entry.State == EntityState.Added)
                    {
                        if (Any<SurveyType>(i => i.Name == o.Name))
                        {
                            throw new ArgumentException(Messages.Error_NameTaken);
                        }
                    }
                    if (entry.State == EntityState.Modified)
                    {
                        if (Any<SurveyType>(i => i.Name == o.Name && i.ID != o.ID))
                        {
                            throw new ArgumentException(Messages.Error_NameTaken);
                        }
                    }
                }
                #endregion

                #region Business logic : Tag
                if (entry.Entity is Tag)
                {
                    var o = entry.Entity as Tag;
                    if (entry.State == EntityState.Added)
                    {
                        if (Any<Tag>(i => i.Value == o.Value && i.State == State.Active))
                        {
                            throw new ArgumentException(Messages.Error_NameTaken);
                        }
                    }
                    if (entry.State == EntityState.Modified)
                    {
                        if (Any<Tag>(i => i.Value == o.Value && i.ID != o.ID && i.State == State.Active))
                        {
                            throw new ArgumentException(Messages.Error_NameTaken);
                        }
                    }
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
                var fieldSql = new StringBuilder();

                foreach (var item in fieldsToCheckForChanges)
                {

                    if (item.ObjectID > 0 && item.FieldTypeID > 0 && !string.IsNullOrEmpty(item.ObjectType))
                    {
                        if (fieldSql.Length != 0)
                        {
                            fieldSql.Append(" or ");
                        }
                        fieldSql.Append($"(f.ObjectID = {item.ObjectID} and f.[ObjectType] = '{item.ObjectType}' and f.FieldTypeID = {item.FieldTypeID})");
                    }
                }

                if (fieldSql.Length != 0)
                {
                    var sql = $"select f.ObjectID, f.ObjectType, f.Value, f.FieldTypeID from field f where {fieldSql.ToString()}";

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
            catch (OptimisticConcurrencyException e)
            {
                Console.WriteLine(e.Message);
            }

            // create events for the objects this needs to be done after save changes so we have new objects id's
            if (IsEventingEnabled)
            {
                CreateEventsForObjectsRequiringTracking(modifiedEventEntities, addedEventEntities, deletedEventEntities, changedFields);
            }

            return returnValue;
        }


        private void CreateEventsForObjectsRequiringTracking(IEnumerable<IEventTrackedEntity> modifiedEntities, IEnumerable<IEventTrackedEntity> addedEntities, IEnumerable<IEventTrackedEntity> deletedEntities, List<Field> changedFields)
        {
            //get any objects that implement EventTrackedEntity so we can add messages for them
            var events = new List<EventInfo>();
            var fieldEvents = new List<EventObjectInfo>();

            //we need to create event objects for field changes. Add them here
            if (changedFields != null)
            {
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
            }

            foreach (var fieldEvent in fieldEvents)
            {
                AddQE(events, ChangeType.Update, fieldEvent);
            }


            if (modifiedEntities != null)
            {
                foreach (var modified in modifiedEntities)
                {
                    AddQE(events, ChangeType.Update, modified.GetEventObjectInfo());
                }
            }

            if (addedEntities != null)
            {
                foreach (var added in addedEntities)
                {
                    AddQE(events, ChangeType.Add, added.GetEventObjectInfo());
                }
            }

            if (deletedEntities != null)
            {
                foreach (var deleted in deletedEntities)
                {
                    AddQE(events, ChangeType.Delete, deleted.GetEventObjectInfo());
                }
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

        #region Dynamic Field Methods

        public void getDynamicFieldJoinStatements(int typeID, string type, out string joins, out string columns, bool includeIdColumn = true, bool useFriendlyName = false, bool listableOnly = true, List<FieldType> fields = null, string idColumn = "A.ID", bool ruleMeansEvent = true, bool enableRelationshipFields = true, bool includeKeyColumnOnly = false)
        {
            var columnbuilder = new StringBuilder();
            columns = "";
            var joinbuilder = new StringBuilder();
            joins = "";

            var fieldTypeRelationType = type;
            if (type == "Rule")
            {
                if (ruleMeansEvent)
                {
                    type = "Event";
                }
                else
                {
                    fieldTypeRelationType += "Type";
                }
            }
            else
            {
                fieldTypeRelationType += "Type";
            }

            if (fields == null)
            {
                if (listableOnly)
                {
                    fields = Filter<FieldType>(i => i.Object == fieldTypeRelationType && i.ObjectID == typeID && i.IsListable).OrderBy(i => i.ColumnOrder).ToList();
                }
                else
                {
                    fields = Filter<FieldType>(i => i.Object == fieldTypeRelationType && i.ObjectID == typeID).OrderBy(i => i.ColumnOrder).ToList();
                }

                if (includeKeyColumnOnly)
                {
                    fields = fields.Where(x => x.IsPartOfKey == true).ToList();
                }
            }

            var relationFieldInfos = getRelationFieldData(fieldTypeRelationType, typeID, fields);

            foreach (var f in fields)
            {
                var name = $"Field{f.ID}";
                var friendlyName = f.FriendlyName.Replace("[", "").Replace("]", "");

                if (f.Type == DataType.Relationship.ToString())
                {
                    if (enableRelationshipFields)
                    {
                        var relationFieldInfo = relationFieldInfos.SingleOrDefault(i => i.FieldTypeID == f.ID);

                        if (relationFieldInfo != null)
                        {
                            var isReferenceItemType = (relationFieldInfo.Object == SystemObjects.ReferenceItemType.ToString());
                            var isFusionAttributeType = (relationFieldInfo.Object == SystemObjects.FusionAttributeType.ToString());
                            var isTaxonomyType = (relationFieldInfo.Object == SystemObjects.TaxonomyType.ToString());
                            var isPolicyType = (relationFieldInfo.Object == SystemObjects.PolicyType.ToString());
                            var isArtifactType = (relationFieldInfo.Object == SystemObjects.ArtifactType.ToString());
                            var useAssetTable = isPolicyType || isTaxonomyType || isArtifactType;
                            var useAssetTypeTable = isReferenceItemType;

                            var tableName = relationFieldInfo.Object.Replace("Type", "");
                            var typeIDColumnName = relationFieldInfo.Object + "ID";

                            if (includeIdColumn)
                            {
                                columnbuilder.Append($"{name}_T.ID as [{name}ID], ");
                            }

                            if (isReferenceItemType || isFusionAttributeType)
                            {
                                columnbuilder.Append($"{name}_OT.Name");
                            }
                            else if (isTaxonomyType || isPolicyType)
                            {
                                columnbuilder.Append($"{name}_OTT.TextPath");
                            }
                            else
                            {
                                columnbuilder.Append($"{name}_OTD.DisplayValue");
                            }

                            columnbuilder.Append($" as [{(useFriendlyName ? friendlyName : name)}],");

                            joinbuilder.Append($" left join [Intersect] {name}_T on {name}_T.IntersectTypeID = {f.LookupObjectID} and");
                            joinbuilder.Append(relationFieldInfo.IsSubject ? $" {name}_T.Subject = '{type.Replace("Type", "")}' and {name}_T.SubjectID = {idColumn}" : $" {name}_T.Object = '{type.Replace("Type", "")}' and {name}_T.ObjectID = {idColumn}");
                            if (!useAssetTable && !useAssetTypeTable)
                            {
                                joinbuilder.Append($" left join [{tableName}] {name}_OT on {name}_OT.{typeIDColumnName} = {relationFieldInfo.ObjectID} AND ");
                                joinbuilder.Append($"{name}_OT.ID = {name}_T." + (relationFieldInfo.IsSubject ? "ObjectID" : "SubjectID"));
                            }
                            else if (useAssetTypeTable)
                            {
                                joinbuilder.Append($" left join [AssetType] {name}_OT on ");
                                joinbuilder.Append($" {name}_OT.ObjectId = {name}_T." + (relationFieldInfo.IsSubject ? "ObjectID" : "SubjectID"));
                                joinbuilder.Append($" and {name}_OT.Object ='{relationFieldInfo.Object}' and  {name}_T." + (relationFieldInfo.IsSubject ? "Object" : "Subject") + $"= '{relationFieldInfo.Object}'");
                            }
                            else
                            {
                                joinbuilder.Append($" left join dbo.asset {name}_OT on {name}_OT.[Object] = '{tableName}' and {name}_OT.ObjectID = {relationFieldInfo.ObjectID} AND ");
                                joinbuilder.Append($"{name}_OT.ID = {name}_T." + (relationFieldInfo.IsSubject ? "ObjectID" : "SubjectID"));
                            }

                            if (isTaxonomyType || isPolicyType)
                            {
                                joinbuilder.Append($" left join asset {name}_AS on {name}_AS.Object = '{tableName}' and  {name}_AS.ObjectId = {name}_T." + (relationFieldInfo.IsSubject ? "ObjectID" : "SubjectID"));
                                joinbuilder.Append($" outer apply [dbo].GetAssetTextPathById({name}_AS.ID, '/') {name}_OTT");
                            }
                            else if (!isReferenceItemType && !isFusionAttributeType)
                            {
                                joinbuilder.Append($" left join asset {name}_AS on {name}_AS.Object = '{tableName}' and  {name}_AS.ObjectId = {name}_T." + (relationFieldInfo.IsSubject ? "ObjectID" : "SubjectID"));
                                joinbuilder.Append($" cross apply [dbo].GetAssetDisplayValueById({name}_AS.ID) {name}_OTD");
                            }
                        }
                    }
                }
                else if (f.Type == DataType.FieldFromRelationship.ToString())
                {
                    if (enableRelationshipFields)
                    {
                        var relationFieldInfo = relationFieldInfos.SingleOrDefault(i => i.FieldTypeID == f.ID);

                        if (relationFieldInfo != null)
                        {
                            var relationshipLookupFieldType = GetById<FieldType>(f.LookupObjectFieldTypeID ?? 0);
                            if (relationshipLookupFieldType != null)
                            {
                                if (relationshipLookupFieldType.Type == DataType.JsonElement.ToString())
                                {
                                    var jsonElementDefinition = JsonConvert.DeserializeObject<FieldTypeDefinition_JsonElement>(relationshipLookupFieldType.Definition);
                                    var sqlType = DetermineSqlDataTypeForFieldType(relationshipLookupFieldType);

                                    if (includeIdColumn)
                                    {
                                        columnbuilder.Append($"{name}_T.ID as [{name}ID], ");
                                    }
                                    columnbuilder.Append($"try_cast({name}_P.Value as {sqlType}) as [{(useFriendlyName ? friendlyName : name)}], ");

                                    joinbuilder.Append($" left join [Intersect] {name}_T on {name}_T.IntersectTypeID = {f.LookupObjectID} and");
                                    joinbuilder.Append(relationFieldInfo.IsSubject ? $" {name}_T.Subject = '{type.Replace("Type", "")}' and {name}_T.SubjectID = {idColumn}" : $" {name}_T.Object = '{type.Replace("Type", "")}' and {name}_T.ObjectID = {idColumn}");
                                    joinbuilder.Append($" left join [Field] {name}_OT on {name}_OT.FieldTypeID = {jsonElementDefinition.FieldTypeID}");
                                    joinbuilder.Append($" and {name}_OT.ObjectType = {name}_T." + (relationFieldInfo.IsSubject ? "Object" : "Subject"));
                                    joinbuilder.Append($" and {name}_OT.ObjectID = {name}_T." + (relationFieldInfo.IsSubject ? "ObjectID" : "SubjectID"));
                                    joinbuilder.Append($" left join FieldJsonProperty {name}_P on {name}_P.FieldID = {name}_OT.ID and {name}_P.[Path] = '{jsonElementDefinition.Path.CleanForSql()}' ");

                                }
                                else
                                {
                                    if (includeIdColumn)
                                    {
                                        columnbuilder.Append($"{name}_T.ID as [{name}ID], ");
                                    }
                                    columnbuilder.Append($"{name}_OT.FormattedValue as [{(useFriendlyName ? friendlyName : name)}], ");

                                    joinbuilder.Append($" left join [Intersect] {name}_T on {name}_T.IntersectTypeID = {f.LookupObjectID} and");
                                    joinbuilder.Append(relationFieldInfo.IsSubject ? $" {name}_T.Subject = '{type.Replace("Type", "")}' and {name}_T.SubjectID = {idColumn}" : $" {name}_T.Object = '{type.Replace("Type", "")}' and {name}_T.ObjectID = {idColumn}");
                                    joinbuilder.Append($" left join [Field] {name}_OT on {name}_OT.FieldTypeID = {relationshipLookupFieldType.ID}");
                                    joinbuilder.Append($" and {name}_OT.ObjectType = {name}_T." + (relationFieldInfo.IsSubject ? "Object" : "Subject"));
                                    joinbuilder.Append($" and {name}_OT.ObjectID = {name}_T." + (relationFieldInfo.IsSubject ? "ObjectID" : "SubjectID"));
                                    joinbuilder.Append(" ");
                                }
                            }
                        }
                    }
                }
                else if (f.Type == DataType.Decimal.ToString())
                {
                    if (includeIdColumn)
                    {
                        columnbuilder.Append($"{name}_T.Value as [{name}ID], ");
                    }
                    columnbuilder.Append($@"case     
    when {name}_T.FormattedValue is not null then try_cast({name}_T.FormattedValue as decimal(38,6))
    when {name}_TT.DefaultValue is not null then try_cast({name}_TT.DefaultFormattedValue  as decimal(38,6))
    else null 
end as [{(useFriendlyName ? friendlyName : name)}], ");

                    joinbuilder.Append($@" inner join FieldType {name}_TT on {name}_TT.ID = {f.ID} and {name}_TT.Object = '{fieldTypeRelationType}' and {name}_TT.ObjectID = {typeID} 
left join Field {name}_T on {name}_T.ObjectType = '{type}' and {name}_T.ObjectID = {idColumn} and {name}_T.FieldTypeID = {name}_TT.ID ");
                }
                else if (f.Type == DataType.Number.ToString())
                {
                    if (includeIdColumn)
                    {
                        columnbuilder.Append($"{name}_T.Value as [{name}ID], ");
                    }
                    columnbuilder.Append($@"case     
    when {name}_T.FormattedValue is not null then try_cast({name}_T.FormattedValue as bigint)
    when {name}_TT.DefaultValue is not null then try_cast({name}_TT.DefaultFormattedValue  as bigint)
    else null 
end as [{(useFriendlyName ? friendlyName : name)}], ");

                    joinbuilder.Append($@" inner join FieldType {name}_TT on {name}_TT.ID = {f.ID} and {name}_TT.Object = '{fieldTypeRelationType}' and {name}_TT.ObjectID = {typeID} 
left join Field {name}_T on {name}_T.ObjectType = '{type}' and {name}_T.ObjectID = {idColumn} and {name}_T.FieldTypeID = {name}_TT.ID ");
                }
                else if (f.Type == DataType.JsonElement.ToString())
                {
                    var jsonElementDefinition = JsonConvert.DeserializeObject<FieldTypeDefinition_JsonElement>(f.Definition);

                    var sqlType = DetermineSqlDataTypeForFieldType(f);

                    columnbuilder.Append($@"try_cast({name}_P.FormattedValue as {sqlType}) as [{(useFriendlyName ? friendlyName : name)}], ");

                    joinbuilder.Append($@" 
left join Field {name}_T on {name}_T.ObjectType = '{type}' and {name}_T.ObjectID = {idColumn} and {name}_T.FieldTypeID = {jsonElementDefinition.FieldTypeID} 
left join FieldJsonProperty {name}_P on {name}_P.FieldID = {name}_T.ID and {name}_P.[Path] = '{jsonElementDefinition.Path.CleanForSql()}' ");
                }
                else if (f.Type == DataType.Path.ToString())
                {
                    columnbuilder.Append($@"graph.GetPath({name}_GAN.Segments, ' > ', ' / ') as [{(useFriendlyName ? friendlyName : name)}], ");
                    joinbuilder.Append($@" inner join graph.AssetNode {name}_GAN on {name}_GAN.ID = A.ID ");
                }
                else if (f.Type == DataType.Score.ToString())
                {
                    columnbuilder.Append($@"{name}_SC.FormattedValue as [{(useFriendlyName ? friendlyName : name)}], ");
                    joinbuilder.Append($@" outer apply dbo.GetAssetScoreById(A.ID, {f.ScoreType}) {name}_SC ");
                }
                else if (f.Type == DataType.Tag.ToString())
                {
                    string assetIdPath = "A.Id";


                    if (includeIdColumn)
                    {
                        columnbuilder.Append($"{name}_T.Value as [{name}ID], ");
                    }

                    columnbuilder.Append($@"(select string_agg(T.Value,'|') within group (order by T.Value) from AssetTag AT inner join Tag T on T.ID = AT.TagID  where AssetId = {assetIdPath}) as [{(useFriendlyName ? friendlyName : name)}], ");

                    joinbuilder.Append($@" inner join FieldType {name}_TT on {name}_TT.ID = {f.ID} and {name}_TT.Object = '{fieldTypeRelationType}' and {name}_TT.ObjectID = {typeID} 
left join Field {name}_T on {name}_T.ObjectType = '{type}' and {name}_T.ObjectID = {idColumn} and {name}_T.FieldTypeID = {name}_TT.ID ");
                }
                else if (f.Type == DataType.Lookup.ToString() && LookupFieldHasColorItem(f))
                {
                    string fieldJoin = f.AllowMultipleValues ? "cross apply STRING_SPLIT(fi.Value, ',') SPFfi" : "";
                    string fieldclause = f.AllowMultipleValues ? "try_cast(SPFfi.value as int)" : "try_cast(fi.Value as int) and datalength(fi.Value) < 1000";
                    string whereClause = (type == SystemObjects.Intersect.ToString()) ? $@" fi.ObjectID = A.ID and fi.ObjectType = '{type}'" : "fi.AssetID = A.Id";

                    columnbuilder.Append($"{name}_T.value as [{name}],");
                    joinbuilder.Append($@" outer apply(
                            select value = (
                                SELECT
                                COALESCE(ADV.DisplayValue, AC.Code) as name,
                                COALESCE(JSON_VALUE(ACJ.ColorJSON, '$.Value'), '{{emptycolor}}') as color
                                FROM field fi
                                {fieldJoin}
                                inner join Asset AC on AC.Object = '{f.LookupObjectType}' and AC.ObjectID = {fieldclause}
                                cross apply dbo.GetAssetColorJsonByColor(AC.Color) ACJ
                                cross apply GetAssetDisplayValueByID(AC.ID) ADV
                                where FieldTypeID = {f.ID} and {whereClause}
								for json path)
							){name}_T(value)");
                }
                else
                {
                    if (includeIdColumn)
                    {
                        columnbuilder.Append($"{name}_T.Value as [{name}ID], ");
                    }
                    columnbuilder.Append($@"case 
    when {name}_TT.AllowAllValue = 1 and {name}_T.Value = '0' then {name}_TT.AllowAllLabel 
    when {name}_T.FormattedValue is not null then {name}_T.FormattedValue 
    when {name}_TT.DefaultValue is not null then {name}_TT.DefaultFormattedValue 
    else '' 
end as [{(useFriendlyName ? friendlyName : name)}], ");

                    joinbuilder.Append($@" inner join FieldType {name}_TT on {name}_TT.ID = {f.ID} and {name}_TT.Object = '{fieldTypeRelationType}' and {name}_TT.ObjectID = {typeID} 
left join Field {name}_T on {name}_T.ObjectType = '{type}' and {name}_T.ObjectID = {idColumn} and {name}_T.FieldTypeID = {name}_TT.ID ");
                }
            }
            columns = columnbuilder.ToString();
            joins = joinbuilder.ToString();
            fields = null;
        }
        public bool LookupFieldHasColorItem(FieldType fieldType)
        {
            if (fieldType.LookupObjectType != null && fieldType.LookupObjectID.HasValue)
            {
                var obj = fieldType.LookupObjectType == "ReferenceItem" ? "ReferenceItemType" : fieldType.LookupObjectType;
                if (obj != "ReferenceItemType")
                {
                    return false;
                }
                var assettype = AssetTypes.FirstOrDefault(x => x.Object == obj && x.ObjectID == fieldType.LookupObjectID);
                if (assettype != null)
                {
                    return Assets.Any(x => x.AssetTypeID == assettype.ID && x.Color != null);
                }
            }
            return false;
        }


        public List<RelationshipDirectionFieldInfo> getRelationFieldData(string type, int typeID, List<FieldType> fields)
        {
            var relationFieldInfos = new List<RelationshipDirectionFieldInfo>();
            var relationshipFields = fields.Where(i => i.Type == DataType.Relationship.ToString() || i.Type == DataType.FieldFromRelationship.ToString()).ToList();
            if (relationshipFields != null)
            {
                if (relationshipFields.Count > 0)
                {
                    var intersectTypeIDs = relationshipFields.Select(i => i.LookupObjectID.Value).ToList();
                    var intersectTypes = Filter<IntersectType>(i => intersectTypeIDs.Contains(i.ID)).ToList();
                    foreach (var rF in relationshipFields)
                    {
                        var relationFieldInfo = new RelationshipDirectionFieldInfo { FieldTypeID = rF.ID, IntersectTypeID = rF.LookupObjectID.Value };
                        var intersectType = intersectTypes.SingleOrDefault(i => i.ID == relationFieldInfo.IntersectTypeID);
                        if (intersectType != null)
                        {
                            relationFieldInfo.IsSubject = (intersectType.Subject == type && intersectType.SubjectID == typeID);
                            relationFieldInfo.Object = relationFieldInfo.IsSubject ? intersectType.Object : intersectType.Subject;
                            relationFieldInfo.ObjectID = relationFieldInfo.IsSubject ? intersectType.ObjectID : intersectType.SubjectID;

                            relationFieldInfos.Add(relationFieldInfo);
                        }
                    }
                }
            }
            return relationFieldInfos;
        }

        #endregion

        #region API Query Parameter Parsing

        public void ParseAdvancedFilterQueryParameter(IEnumerable<KeyValuePair<string, string>> queryParams, List<DefaultFilter> fieldList, out DynamicParameters dbArgs, out List<string> whereStatements)
        {
            dbArgs = new DynamicParameters();
            whereStatements = new List<string>();

            if (queryParams.Any(x => x.Key.Equals("_filter", StringComparison.OrdinalIgnoreCase)))
            {
                var value = queryParams.FirstOrDefault(x => x.Key.ToLower() == "_filter").Value;
                if (!string.IsNullOrEmpty(value))
                {
                    var filterExpressionParser = new FilterExpressionParser(this, FilterExpressionParseType.CustomFields, false);
                    filterExpressionParser.OverrideAllowedDefaultFields(fieldList);
                    Dictionary<string, object> sqlParams = new Dictionary<string, object>();
                    List<int> filteredFields = new List<int>();
                    whereStatements.Add("(" + filterExpressionParser.Parse(value, out sqlParams, out filteredFields) + ")");

                    foreach (var item in sqlParams)
                    {
                        dbArgs.Add(item.Key, item.Value);
                    }
                }
            }
        }

        public void ParseSimpleFilterQueryParameter(IEnumerable<KeyValuePair<string, string>> queryParams, List<DefaultFilter> fieldList, out DynamicParameters dbArgs, out List<string> whereStatements)
        {
            var wheres = new List<string>();
            var dbs = new DynamicParameters();
            if (queryParams.Any(x => x.Key.Equals("_simpleFilter", StringComparison.OrdinalIgnoreCase)))
            {
                var value = queryParams.FirstOrDefault(x => x.Key.ToLower() == "_simplefilter").Value;
                if (!string.IsNullOrEmpty(value))
                {
                    fieldList.ForEach(f =>
                    {
                        switch (f.SqlFieldType)
                        {
                            case SqlFieldType.Text:
                                wheres.Add($"{f.SqlExpression} like @S_{f.ApiName}");
                                dbs.Add($"@S_{f.ApiName}", value + "%");
                                break;
                            case SqlFieldType.Boolean:
                                bool filterBool;
                                if (bool.TryParse(value, out filterBool))
                                {
                                    wheres.Add($"{f.SqlExpression} = @S_{f.ApiName}");
                                    dbs.Add($"@S_{f.ApiName}", filterBool);
                                }
                                break;
                            case SqlFieldType.Date:
                            case SqlFieldType.DateTime:
                                DateTime filterDate;
                                if (DateTime.TryParse(value, out filterDate))
                                {
                                    wheres.Add($"{f.SqlExpression} = @S_{f.ApiName}");
                                    dbs.Add($"@S_{f.ApiName}", filterDate);
                                }
                                break;
                            case SqlFieldType.Decimal:
                            case SqlFieldType.Number:
                                decimal filterNumber;
                                if (decimal.TryParse(value, out filterNumber))
                                {
                                    var places = filterNumber.GetNumberOfDecimalPlaces();
                                    var ceil = decimal.Round(decimal.Parse(value + "9"), places);
                                    wheres.Add($"{f.SqlExpression} between @S_{f.ApiName}_1 and @S_{f.ApiName}_2");
                                    dbs.Add($"@S_{f.ApiName}_1", filterNumber);
                                    dbs.Add($"@S_{f.ApiName}_2", ceil);
                                }
                                break;
                            case SqlFieldType.Guid:
                                Guid filterGuid;
                                if (Guid.TryParse(value, out filterGuid))
                                {
                                    wheres.Add($"{f.SqlExpression} = @S_{f.ApiName}");
                                    dbs.Add($"@S_{f.ApiName}", filterGuid);
                                }
                                break;
                            default:
                                break;
                        }
                    });
                }
            }
            whereStatements = wheres;
            dbArgs = dbs;
        }

        public string ParseOrderColumn(IEnumerable<KeyValuePair<string, string>> queryParams, List<DefaultFilter> fields, string defaultColumn)
        {
            var column = defaultColumn;
            if (queryParams.Any(x => x.Key.Equals("_order", StringComparison.OrdinalIgnoreCase)))
            {
                string order = queryParams.FirstOrDefault(x => x.Key.Equals("_order", StringComparison.OrdinalIgnoreCase)).Value;

                var field = fields.FirstOrDefault(i => i.ApiName.Equals(order, StringComparison.OrdinalIgnoreCase));

                if (field == null)
                {
                    throw new GenericException(System.Net.HttpStatusCode.BadRequest, "Invalid request", "Invalid order by passed in the request.");
                }

                column = field.SqlExpression;
            }

            return column;
        }

        public string ParseOrderDirection(IEnumerable<KeyValuePair<string, string>> queryParams, string defaultDirection = "desc")
        {
            var direction = defaultDirection;
            if (queryParams.Any(x => x.Key.Equals("_direction", StringComparison.OrdinalIgnoreCase) || x.Key.Equals("_sort", StringComparison.OrdinalIgnoreCase)))
            {
                string[] allowedDirections = new string[] { "asc", "desc" };
                string order = queryParams.FirstOrDefault(x => x.Key.Equals("_direction", StringComparison.OrdinalIgnoreCase) || x.Key.Equals("_sort", StringComparison.OrdinalIgnoreCase)).Value;

                if (allowedDirections.Contains(order.Trim().ToLower()))
                {
                    direction = order;
                }
                else
                {
                    throw new GenericException(System.Net.HttpStatusCode.BadRequest, "Invalid request", "Invalid direction by passed in the request.");
                }
            }
            return direction;
        }

        public int ParsePageNumber(IEnumerable<KeyValuePair<string, string>> queryParams, int defaultPage = 1)
        {
            var size = defaultPage;
            if (queryParams.Any(x => x.Key.Equals("_pageNum", StringComparison.OrdinalIgnoreCase)))
            {
                if (int.TryParse(queryParams.FirstOrDefault(x => x.Key.Equals("_pageNum", StringComparison.OrdinalIgnoreCase)).Value, out size))
                {
                    if (size < 1)
                    {
                        size = defaultPage;
                    }
                }
            }
            return size;
        }

        public int ParsePageSize(IEnumerable<KeyValuePair<string, string>> queryParams, int defaultSize = 250)
        {
            var size = defaultSize;
            if (queryParams.Any(x => x.Key.Equals("_pageSize", StringComparison.OrdinalIgnoreCase)))
            {
                if (int.TryParse(queryParams.FirstOrDefault(x => x.Key.Equals("_pageSize", StringComparison.OrdinalIgnoreCase)).Value, out size))
                {
                    if (size < 1)
                    {
                        size = defaultSize;
                    }
                }
            }
            return size;
        }

        public string ParsePageOffsetSql(int pageNumber, int pageSize, int pageSizeLimit = 10000)
        {
            var offset = "";
            if (pageSize > 0 || pageNumber > 0)
            {
                if (pageSize < 1) pageSize = 1;
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize > pageSizeLimit) pageSize = pageSizeLimit;
                if (pageNumber > 10000) pageNumber = 10000;
                offset = $" offset {pageSize * (pageNumber - 1)} rows fetch next {pageSize} rows only ";
            }
            return offset;
        }

        #endregion

        public int GetObjectId(Guid objectUid, SystemObjects objectType)
        {
            int objectId = -1;
            switch (objectType)
            {
                case SystemObjects.Tag:
                    objectId = Tags.FirstOrDefault(x => x.uid == objectUid).ID;
                    break;
                case SystemObjects.IntersectType:
                    objectId = IntersectTypes.FirstOrDefault(x => x.uid == objectUid).ID;
                    break;
                case SystemObjects.Predicate:
                    objectId = Predicates.FirstOrDefault(x => x.UID == objectUid).ID;
                    break;
                case SystemObjects.IssueType:
                    objectId = IssueTypes.FirstOrDefault(x => x.uid == objectUid).ID;
                    break;
                case SystemObjects.ArtifactType:
                case SystemObjects.PolicyType:
                case SystemObjects.ReferenceItemType:
                case SystemObjects.RuleType:
                case SystemObjects.TaxonomyType:
                    objectId = AssetTypes.FirstOrDefault(x => x.uid == objectUid)?.ObjectID ?? 0;
                    break;
                case SystemObjects.ResourceType:
                    objectId = Community.Resources.FirstOrDefault(x => x.Uid == objectUid).ID;
                    break;
                case SystemObjects.TaskType:
                    objectId = Assets.FirstOrDefault(x => x.uid == objectUid && x.Object == "Task").ObjectID;
                    break;
                case SystemObjects.ConnectorLabel:
                    objectId = ConnectorLabels.FirstOrDefault(x => x.uid == objectUid).ID;
                    break;
                default:
                    objectId = Assets.FirstOrDefault(x => x.uid == objectUid)?.ObjectID ?? 0;
                    if (objectId <= 0)
                    {
                        throw new ArgumentNullException($"Asset not found based on uid '{objectUid}'");
                    }
                    break;
            }
            return objectId;
        }

        public Guid GetAssetUid(int objectId, SystemObjects assetType)
        {
            try
            {
                return Assets.FirstOrDefault(x => x.Object == assetType.ToString() && x.ObjectID == objectId).uid;
            }
            catch
            {
                throw new ArgumentNullException($"Object not part of assets table!");
            }
        }

        public decimal? GetAssetScore(long assetId, ScoreType type)
        {
            string sql = $@"
select      top 1
            cast(S.Value * 100 as decimal(18,1)) as 'Score'                            
from        Asset A                            
            inner join metrics.Score S on S.AssetUid = A.[uid] and S.EffectiveDate <= getutcdate()
            inner join metrics.Allocation Al on Al.Uid = S.AllocationUid and Al.ScoreType = @type and (Al.OverrideName is null or Al.OverrideName = '')
where       A.ID = @assetId 
order by    S.EffectiveDate desc";
            return Query<decimal?>(sql, new { assetId, type = (int)type }).FirstOrDefault();
        }

        public decimal? GetPreviousAssetScore(long assetId, ScoreType type)
        {
            string sql = $@"
select      top 1
            cast(S.Value * 100 as decimal(18,1)) as 'Score'                            
from        Asset A                            
            inner join metrics.Score S on S.AssetUid = A.[uid] and S.EffectiveDate <= getutcdate()
            inner join metrics.Allocation Al on Al.Uid = S.AllocationUid and Al.ScoreType = @type and (Al.OverrideName is null or Al.OverrideName = '')
            cross apply (
                select top 1 EffectiveDate from Asset AP
                inner join metrics.Score SA on SA.AssetUid = AP.[uid] and SA.EffectiveDate <= getutcdate()
                inner join metrics.Allocation ALP on ALP.Uid = SA.AllocationUid and ALP.ScoreType = @type and (ALP.OverrideName is null or ALP.OverrideName = '')
                where AP.ID = @assetId
            ) P
where       A.ID = @assetId and S.EffectiveDate < P.EffectiveDate
order by    S.EffectiveDate desc";
            return Query<decimal?>(sql, new { assetId, type = (int)type }).FirstOrDefault();
        }

        public Dictionary<Guid, string> GetAssetTypePathsByAssetClasses(List<int> assetClassIds)
        {
            var dbArgs = new DynamicParameters();
            var sql = $@"select AT.[uid] as AssetUID, P.[Path] as assetTypePath 
                            from AssetType AT cross apply dbo.GetAssetTypeTextPathById(AT.ID, ' / ') P 
                            where AT.class in({string.Join(",", assetClassIds.ToArray())})";

            return Query<dynamic>(sql).ToDictionary(x => (Guid)x.AssetUID, x => x.assetTypePath as string);
        }

        public int GetFieldLookupValue(string lookupObjectType, int lookupObjectId, int fieldTypeId, string value)
        {
            return Query<int>(@"select value
  from[dbo].[FieldLookupValue]
  where LookupObjectType = @obj and LookupObjectID = @objId and FieldTypeID = @f and Text = @value",

new { obj = lookupObjectType, objId = lookupObjectId, f = fieldTypeId, value = value }).FirstOrDefault();
        }

        public string GetDiagramUrlForDiagramAsset(Guid assetUid)
        {
            var diagramUrl = $@"select 
                            '/sidebar/visualization/browser/'+lower(cast(a.uid as nvarchar(36)))+'/Process/' + lower(cast(@assetUid as nvarchar(36)))
                            from AssetProcessDiagram APD
                            cross apply (SELECT *
                            FROM OPENJSON(APD.Diagram,'$.nodeDataArray')
                            WITH (   
            
                                          uid uniqueidentifier '$.key'
                             ) 
                            )Json
                            inner join asset a on a.id = apd.AssetID
                            where json.uid = @assetUid";
            return Query<string>(diagramUrl, new { assetUid }).FirstOrDefault();
        }
        
        public bool HasRelationshipInProcessDiagram(Guid intersectTypeUid)
        {
            return Query<int>(@"select count(*) from processexpandeddata ped
                            inner join IntersectType it on it.uid = @intersectTypeUid
                            where ped.DiagramAssetTypeUid = it.SubjectUid and 
                            (ped.FromAssetTypeUid = it.ObjectUid or ped.ToAssetTypeUid = it.objectuid)",
                            new { intersectTypeUid }).FirstOrDefault() > 0;
        }

        public void CreateEventsForAddedActions(List<Issue> actions)
        {
            CreateEventsForObjectsRequiringTracking(null, actions, null, null);
        }

        public string GetCounterFieldValue(int fieldTypeId, long assetId)
        {
            return Query<string>(@"
                select top 1 ISNULL(FT.CounterPrefix,'') + cast(fcv.value as nvarchar(20) )
                from fieldcountervalue fcv
                inner join FieldType FT on FT.ID = fcv.FieldTypeId
                where fcv.AssetId=@assetId and fcv.FieldTypeId=@fieldTypeId",
                              new { fieldTypeId, assetId }).FirstOrDefault();
        }


        #region Environment Settings

        private class EnvironmentSetting
        {
            public Setting ID { get; set; }
            public string Value { get; set; }
        }

        private string SettingsCacheKey { get { return $"Settings_{CurrentCompanyID}"; } }

        public void DeleteSetting(Setting setting)
        {
            // In essence, this would set back to the default, if any.
            Connection.Execute("delete Setting where ID = @id", new { id = (int)setting });
            Caching.RemoveItem(SettingsCacheKey);
        }

        public SettingInfo GetSetting(Setting setting)
        {
            return GetSettings().SingleOrDefault(s => s.ID == setting);
        }

        public T GetSettingValue<T>(Setting setting)
        {
            var info = GetSetting(setting);

            T checkType = default(T);
            if (checkType is Guid)
            {
                var guid = Guid.Parse(info.Value);
                return (T)(Convert.ChangeType(guid, typeof(T)));
            }

            return (T)(Convert.ChangeType(info.Value, typeof(T)));
        }

        public List<SettingInfo> GetSettings()
        {
            // Get the list of settings from the D3S_###.dbo.Setting table.
            // Get the full list of settings from the Setting enum.
            // Return a list of SettingInfo, merging the values present from the environment into the SettingInfo.Value property.

            var overrides = Caching.GetItem<List<EnvironmentSetting>>(SettingsCacheKey);
            if (overrides == null)
            {
                overrides = Query<EnvironmentSetting>("select * from Setting").ToList();
                Caching.SetItem(SettingsCacheKey, overrides, true, 3);
            }
            var settings = Setting.ActionMessage.GetAsList();

            settings.ForEach(s =>
            {
                if (overrides.Any(o => o.ID == s.ID))
                {
                    s.Value = overrides.First(o => o.ID == s.ID).Value;
                }
                else
                {
                    s.Value = s.DefaultValue;
                }
            });

            return settings;
        }

        public Dictionary<string, string> GetSettingsAsDictionary()
        {
            return GetSettings().ToDictionary(k => k.ID.ToString(), v => v.Value);
        }

        public void UpsertSetting(Setting setting, string value)
        {
            Connection.Execute(@"
if exists(select 1 from [Setting] where ID = @ID) 
begin 
    update [Setting] set [Value] = @value where ID = @ID 
end 
else 
begin 
    insert [Setting] values (@ID, @value) 
end", new { ID = (int)setting, value });
            Caching.RemoveItem(SettingsCacheKey);
        }

        #endregion
    }
}
