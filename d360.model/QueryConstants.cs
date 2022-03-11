using d360.core;
using d360.core.enums;
using d360.core.resources;

namespace d360.model
{
    public static class QueryConstants
    {
        public static readonly string HighLevelTypeCaseStatement = $@"case 
				when T.Object = 'ArtifactType' and T.[Class] = 1 then '{CommonNames.AssetTypeClass_Business.CleanForSql()}: ' 
                when T.Object = 'ArtifactType' and T.[Class] = 8 then '{CommonNames.AssetTypeClass_Technical.CleanForSql()}: ' 
				when T.Object = 'PolicyType' then '{CommonNames.AssetTypeClass_Policy.CleanForSql()}: ' 
				when T.Object = 'ReferenceItemType' then 'Reference: ' 
				when T.Object = 'RuleType' then '{CommonNames.AssetTypeClass_Rule.CleanForSql()}: ' 
				when T.Object = 'TaxonomyType' then '{CommonNames.AssetTypeClass_Model.CleanForSql()}: '
				when T.Object = 'AttributeType' then 'Attribute: '
				when T.Object = 'GroupType' then 'Group: '
				when T.Object = 'OrganizationType' then 'Organization: '
				when T.Object = 'ResourceType' then 'Resource: '
				else ''
			end ";

        public static readonly string ArtifactActivitySpecificDateCountList = @"
select 
	Name,
	sum(New) as New,
	sum(Total) as Total,
	Id
from
(select  at.Name,
	    1 as New,        					
		0 as Total,
        at.id as Id		
from    Asset a
        inner join AssetType at on a.assettypeid = at.id and at.Object = 'ArtifactType'
where   a.createdon > dateadd(day, @d, CURRENT_TIMESTAMP)
union all
select  at.Name,
	    0 as New,   
		1 as Total,     					
        at.id as Id		
from    Asset a
        inner join AssetType at on a.assettypeid = at.id and at.Object = 'ArtifactType'
where a.updatedon > dateadd(day, @d, CURRENT_TIMESTAMP)) T
group by Name, Id";

        public static readonly string ArtifactActivityAllDateCountList = @"
select  at.Name,
	    count(1) as New,        					
        count(1) as Total,
        at.id as Id								
from    Asset a
        inner join AssetType at on a.assettypeid = at.id and at.Object = 'ArtifactType'                       
group by at.name,at.id order by at.name";

        public static readonly string ObjectNymTypes = @"
                                select 
	                                P.ID as [ID],
	                                P.Name as [Name]
                                from [Predicate] P
                                where exists
	                                (SELECT *  
                                    FROM IntersectType IT
                                    WHERE P.[type] = 6 and P.ID = IT.PredicateID and ((IT.Subject = @ot and IT.SubjectID = @id) OR (IT.Object = @ot and IT.ObjectID = @id)))
                                union
									select 
                                        P.ID as [ID], P.Name as [Name] 
                                from  [dbo].[NymRelation] R inner join [dbo].[predicate] P on P.ID = R.PredicateID where R.[Object] = @ot and R.ObjectID = @id
    ";
        
        public static readonly string GroupResourceInfoList = @"
select  RG.GroupID,
        R.Email,
        R.FirstName,
        R.LastName,
        R.ResourceID,
        case 
            when G.PrimaryOwnerResourceID = R.ResourceID then 'Primary'
            when G.SecondaryOwnerResourceID = R.ResourceID then 'Secondary'
	        else ''
        end as [Owner]
from    [Group] G
        inner join ResourceGroup RG on RG.GroupID = G.ID and G.ID = @id
        inner join reporting.Global_Resource R on R.ResourceID = RG.ResourceID
        Where R.State = @userStatus";

        public static readonly string InformationCatalogDiagramData = $@"
select	0 as ID, 
		null as AssetID,
        null as ParentID,
		Name,
		null as ObjectID,
		null as Object,
		null as uid
from	AssetType
where	ObjectID = @ID and Object ='TaxonomyType'
union
select	A.ObjectID as ID, 
        A.ID as AssetID,
        coalesce(P.SubjectID, 0) as ParentID, 
        A.DisplayValue as Name,
		A.ObjectID,
		A.Object,	
		A.uid
from	AssetDetail A
		outer apply (
					select	I.SubjectID
					from	[Intersect] I
                            inner join IntersectType IT on IT.ID = I.IntersectTypeID and I.Object = A.Object and I.ObjectID = A.ObjectID
							inner join [Predicate] P on P.ID = IT.PredicateID and P.Type = 4
					) P
where   A.Type = 'TaxonomyType' and A.TypeID = @ID AND A.[State] = 1";


        public static readonly string LookupAllocations = @"
	SELECT	FT.Name as FieldTypeName,
			FT.ObjectID,
			coalesce(D.[Name], ITN.[Name]) as ObjectName,
			FT.[Object] as ObjectType,
			null as ObjectTypeName,
            AUrl.[Url] as ObjectUrl
	FROM	FieldType FT
			left join AssetType D on FT.[Object] <> 'IntersectType' and D.[Object] = FT.[Object] and D.ObjectID = FT.ObjectID
			left join IntersectType T on FT.[Object] = 'IntersectType' and T.ID = FT.ObjectID
			outer apply [dbo].[GetAssetTypeUrlById](D.ID) AUrl
			outer apply dbo.GetIntersectTypeNames(T.ID) ITN
	WHERE	FT.LookupObjectType = @type
            AND FT.LookupObjectID = @id";
		        

		public static readonly string ReferenceListTypeRelationshipsAllCountsWithZero = @"
select	IT.ID as IntersectTypeID,
        IT.uid,
		IT.Object,
		IT.ObjectID,
		I.[Count],
		IT.Name,
		IT.Cardinality,
		case
            when IT.PredicateType in ({0}) then cast(0 as bit)
            else cast(1 as bit)
        end as AllowEditFromRelationshipEditor,
		IT.IsSubject,
		IT.SameSubjectAndObject,
        at.uid as ObjectUid
from	(
select 
			ITD.ID,
			ITD.UID,
			ITD.PredicateType,
				case 
				when (ITD.Subject = 'ReferenceItemType' and ITD.SubjectID = 0) then ITD.[Object]
				else ITD.[Subject]
			end as [Object],
			case 
				when (ITD.Subject = 'ReferenceItemType' and ITD.SubjectID = 0) then ITD.[ObjectID]
				else ITD.[SubjectID]
			end as [ObjectID],
						case 
				when (ITD.Subject = 'ReferenceItemType' and ITD.SubjectID = 0) then ITD.ObjectAssetTypeID
				else ITD.SubjectAssetTypeID
			end as ObjectAssetTypeID,	
			case 
				when (ITD.Subject = 'ReferenceItemType' and ITD.SubjectID = 0) then ITD.[ObjectName] 
				else ITD.SubjectName
			end + 
			case 
				when (ITD.Subject = 'ReferenceItemType' and ITD.SubjectID = 0) then ' [' + coalesce(ITD.PredicateName, 'N/A') + ']'
				when (ITD.Object = 'ReferenceItemType' and ITD.ObjectID = 0) then ' [' + coalesce(ITD.PredicateInverse, 'N/A') + ']'
			end as [Name],
			case 
				when (ITD.Subject = 'ReferenceItemType' and ITD.SubjectID = 0) then ITD.[ObjectCardinality] 
				else ITD.SubjectCardinality
			end as Cardinality,
			case 
				when (ITD.Subject = 'ReferenceItemType' and ITD.SubjectID = 0) then cast(1 as bit)
				else cast(0 as bit)
			end as IsSubject,
			cast(0 as bit) as SameSubjectAndObject
			from IntersectTypeDetail ITD 
			where	ITD.SubjectAssetTypeID <> ITD.ObjectAssetTypeID 
					and ((ITD.Subject = 'ReferenceItemType' and ITD.SubjectID = 0) 
						or ((ITD.Object = 'ReferenceItemType' and ITD.ObjectID = 0)))
			union all
			select ITD.ID, ITD.[UID], ITD.PredicateType, ITD.[Object], ITD.ObjectID, ITD.ObjectAssetTypeID, ITD.ObjectName + ' [' + coalesce(ITD.PredicateName, 'N/A') + ']' as [Name], ITD.[ObjectCardinality], cast(1 as bit) as IsSubject, cast(1 as bit) as SameSubjectAndObject from IntersectTypeDetail ITD where ITD.[Subject] = 'ReferenceItemType' and ITD.SubjectID = 0 and ITD.SubjectAssetTypeID = ITD.ObjectAssetTypeID
			union all
			select ITD.ID, ITD.[UID], ITD.PredicateType, ITD.[Subject], ITD.SubjectID, ITD.ObjectAssetTypeID, ITD.SubjectName + ' [' + coalesce(ITD.PredicateInverse, 'N/A') + ']' as [Name], ITD.SubjectCardinality, cast(0 as bit) as IsSubject, cast(1 as bit) as SameSubjectAndObject  from IntersectTypeDetail ITD where ITD.[Subject] = 'ReferenceItemType' and ITD.SubjectID = 0 and ITD.SubjectAssetTypeID = ITD.ObjectAssetTypeID
		
) IT 
		left join AssetType AT on AT.Object = @obj and AT.ObjectID =@objId
		cross apply (
					select	count(1) as [Count]
					from	[Intersect] 
					where	IntersectTypeID = IT.ID AND [Visible] = 1
							and (
							 (IT.SameSubjectAndObject = 1 and IT.IsSubject = 1 and ([Subject] = @obj and SubjectID = @objId)) or
							 (IT.SameSubjectAndObject = 1 and IT.IsSubject = 0 and ([Object] = @obj and ObjectID = @objId)) or
							 (IT.SameSubjectAndObject = 0 and (([Subject] = @obj and SubjectID = @objId) or ([Object] = @obj and ObjectID = @objId)))
							)
					) I
order by IT.[Name]
";

        public static readonly string ObjectRelationshipAllCountsWithZero = @"
select	IT.[uid],
		IT.ID as IntersectTypeID,
		A.[UID] as ObjectUid, 
		IT.[Object],
		IT.ObjectID,
		I.[Count],
		IT.[Name],
		IT.Cardinality,
		IT.IsSubject,
		IT.SameSubjectAndObject,
		case when IT.PredicateType in ({0}) then
			cast(0 as bit)
		else
			cast(1 as bit)
		end as AllowEditFromRelationshipEditor
from	Asset A
		inner join AssetType T on T.ID = A.AssetTypeID
		cross apply (
			select	ITD.ID,
					ITD.[UID],
					ITD.PredicateType,
					case when ITD.SubjectAssetTypeID = T.ID then
						ITD.[Object]
					else
						ITD.[Subject]
					end as [Object],
					case when ITD.SubjectAssetTypeID = T.ID then
						ITD.[ObjectID]
					else
						ITD.[SubjectID]
					end as [ObjectID],
					case when ITD.SubjectAssetTypeID = T.ID then
						ITD.ObjectAssetTypeID
					else
						ITD.SubjectAssetTypeID
					end as ObjectAssetTypeID,
					case when ITD.SubjectAssetTypeID = T.ID then 
						ITD.[ObjectName] 
					else 
						ITD.SubjectName
					end + 
					case when ITD.SubjectAssetTypeID = T.ID then  
						' [' + coalesce(ITD.PredicateName, 'N/A') + ']'
					else 
						' [' + coalesce(ITD.PredicateInverse, 'N/A') + ']'
					end as [Name],
					case when ITD.SubjectAssetTypeID = T.ID then  
						ITD.[ObjectCardinality] 
					else 
						ITD.SubjectCardinality
					end as Cardinality,
					case when ITD.SubjectAssetTypeID = T.ID then
						cast(1 as bit)
					else
						cast(0 as bit)
					end as IsSubject,
					cast(0 as bit) as SameSubjectAndObject
			from	IntersectTypeDetail ITD 
			where	(ITD.SubjectAssetTypeID = T.ID or ITD.ObjectAssetTypeID = T.ID) and ITD.SubjectAssetTypeID <> ITD.ObjectAssetTypeID
			union all
			select ITD.ID, ITD.[UID], ITD.PredicateType, ITD.[Object], ITD.ObjectID, ITD.ObjectAssetTypeID, ITD.ObjectName + ' [' + coalesce(ITD.PredicateName, 'N/A') + ']' as [Name], ITD.[ObjectCardinality], cast(1 as bit) as IsSubject, cast(1 as bit) as SameSubjectAndObject from IntersectTypeDetail ITD where ITD.SubjectAssetTypeID = T.ID and ITD.SubjectAssetTypeID = ITD.ObjectAssetTypeID
			union all
			select ITD.ID, ITD.[UID], ITD.PredicateType, ITD.[Subject], ITD.SubjectID, ITD.ObjectAssetTypeID, ITD.SubjectName + ' [' + coalesce(ITD.PredicateInverse, 'N/A') + ']' as [Name], ITD.SubjectCardinality, cast(0 as bit) as IsSubject, cast(1 as bit) as SameSubjectAndObject  from IntersectTypeDetail ITD where ITD.SubjectAssetTypeID = T.ID and ITD.SubjectAssetTypeID = ITD.ObjectAssetTypeID

		) IT
		cross apply (
			select count(*) as [Count] from [Intersect]
			where IT.ID = IntersectTypeID and Visible = 1 and
			(
			 (IT.SameSubjectAndObject = 1 and IT.IsSubject = 1 and ([Subject] = @obj and SubjectID = @objId)) or
			 (IT.SameSubjectAndObject = 1 and IT.IsSubject = 0 and ([Object] = @obj and ObjectID = @objId)) or
			 (IT.SameSubjectAndObject = 0 and (([Subject] = @obj and SubjectID = @objId) or ([Object] = @obj and ObjectID = @objId)))
			)
		) I
where	A.[Object] = @obj and A.ObjectID = @objId and IT.PredicateType not in ({1})
order by IT.[Name]
";

        
        public static readonly string ObjectRelationshipTypeIDs = @"
select	distinct
        I.IntersectTypeID
from	[IntersectDetail] I
where	(I.Subject = @obj and I.SubjectID = @objid and I.ObjectType = @objtype and I.ObjectTypeID = @objtypeid) OR
		(I.Object = @obj and I.ObjectID = @objid and I.SubjectType = @objtype and I.SubjectTypeID = @objtypeid)";

        public static readonly string ObjectRelationships = @"
select	ID,
        [Uid],
        IntersectTypeID,
        case when (Subject = @type and SubjectID = @id) then Object else Subject end as Object,
		case when (Subject = @type and SubjectID = @id) then ObjectID else SubjectID end as ObjectID,
		case when (Subject = @type and SubjectID = @id) then ObjectName else SubjectName end as Name,
        case when (Subject = @type and SubjectID = @id) then ObjectUrl else SubjectUrl end as Url,
		case when (Subject = @type and SubjectID = @id) then ObjectType else SubjectType end as Type,
		case when (Subject = @type and SubjectID = @id) then ObjectTypeID else SubjectTypeID end as TypeID,
		case when (Subject = @type and SubjectID = @id) then ObjectTypeName else SubjectTypeName end as TypeName,
        case when (Subject = @type and SubjectID = @id) then ObjectIconBackColor else SubjectIconBackColor end as IconBackColor,
		case when (Subject = @type and SubjectID = @id) then ObjectIconForeColor else SubjectIconForeColor end as IconForeColor,
		case when (Subject = @type and SubjectID = @id) then ObjectIconText else SubjectIconText end as IconText
from	IntersectDetail
where	(Subject = @type and SubjectID = @id) or (Object = @type and ObjectID = @id)
order by case when (Subject = @type and SubjectID = @id) then ObjectName else SubjectName end
";

        public static readonly string PolicySettingsItem = @"
select	T.Name, 
		T.Description, 
		T.HierarchyMaximumDepth,
		T.Uid,
		T.ObjectID
from	AssetType T 
where T.[object]='PolicyType' and	T.ObjectID = @id";

        public static readonly string RuleSettingsItem = @"
select	T.*,
			case 
				when Work.[Count] > 0 then cast(1 as bit)
				else cast(0 as bit)
			end as HasWorkflow
from	AssetType T 		
		cross apply (
						select	count(1) as [Count]
						from	workflow.EventRegistration WER
								inner join workflow.Type WT on WER.TypeID = WT.ID and WT.PublishedVersionID is not null and WT.[State] = 1 and WER.ChangeType = 8 
						where	WER.Object = T.Object
								and WER.ObjectID = T.ObjectID
						) Work
where T.[object]='RuleType' and		T.ObjectID = @id";


        public static readonly string SynonymTypes = @"
        declare	@ot varchar(50),
		        @otid int

        select	@ot = T.Object,
		        @otid = T.ObjectID
        from	Asset A 
                inner join AssetType T on T.ID = A.AssetTypeID  and A.Object = @type and A.ObjectID = @id 


        select 
            d.[Name], 
			d.[Object] + '|' + cast(d.ObjectID as varchar(50)) as [Value], 
			d.[Object], 
			d.ObjectID, 
			IT.[uid],
			case when IT.Subject = @ot and IT.SubjectID = @otid then
		        1
	        else
		        0
	        end as IntersectTypeIsSubjectSide
		from intersecttype IT        
        inner join (
			select A.[Name], A.[Object], A.ObjectID from AssetType A
			union all
			select TN.[Name], 'IntersectType' as [Object], ID as ObjectID from IntersectType T
			cross apply dbo.GetIntersectTypeNames(T.ID) TN
		) d on
	        case when IT.Subject = @ot then
		        IT.Object
	        else
		        IT.Subject
	        end = d.[Object] 
	        and
	        case when IT.SubjectID = @otid then
		        IT.ObjectID
	        else
		        IT.SubjectID
	        end = d.ObjectID
        where 
	        ((IT.Subject = @ot and IT.SubjectID = @otid) OR (IT.Object = @ot and IT.ObjectID = @otid)) and IT.predicateid = @predicateId";

        public static readonly string SynonymOptions = @"
declare	@ot varchar(50),
		@otid int 

select	@ot = T.Object,
		@otid = T.ObjectID
from	Asset A 
        inner join AssetType T on T.ID = A.AssetTypeID  and A.Object = @object and A.ObjectID = @objectId 

select		D.TypeName + ' :: ' + D.DisplayValue as Name,
            O.TargetingSubject,
			d.uid as [uid]
from AssetDetail d		
			inner join (
						select	case 
									when IT.Subject = @ot and IT.SubjectID = @otid then IT.Object
									else IT.Subject
								end as Object,
								case 
									when IT.Subject = @ot and IT.SubjectID = @otid then IT.ObjectID
									else IT.SubjectID
								end as ObjectID,
								case 
									when IT.Subject = @ot and IT.SubjectID = @otid then cast(0 as bit)
									else cast(1 as bit)
								end as TargetingSubject
						from	IntersectType IT
                                inner join Predicate P on   P.ID = IT.PredicateID 
                                                            and P.ID = @predicateId
														    and (
															    (IT.Subject = @ot and IT.SubjectID = @otid) OR
															    (IT.Object = @ot and IT.ObjectID = @otid)
															    )
						) O on O.Object = D.Type and O.ObjectID = D.TypeID and D.Object + '|' + cast(D.ObjectID as varchar) <> @object + '|' + cast(@objectId as varchar)
            inner join [Predicate] P on P.ID = @predicateId
			where (@query = '') or (@query != '' and d.DisplayValue like '%'+@query+'%') and d.Type = @type and d.typeid = @typeid
order by	D.TypeName,
			D.DisplayValue
";

        public static readonly string SynonymsByObjectList = @"
select	I.ID as IntersectID,
		S.[Object],
		S.ObjectID,
		P.SubjectID as ParentID,
		dbo.GenerateAssetUrl(SP.ID) as ParentUrl,
		DP.DisplayValue as ParentName,
		D.DisplayValue as [Name],
        ST.[Name] as ObjectTypeName,
		null as [Description],
		dbo.GenerateAssetUrl(S.ID) as [Url]       
        ,null as [CustomID]
		,I.uid as [IntersectUid]
		,T.uid as [IntersectTypeUid]		
		,S.uid as [AssetUid]
		,ST.Uid as [AssetTypeUid]

from	[Intersect] I
		inner join IntersectType T on T.ID = I.IntersectTypeID  and T.PredicateID = @predicateId	
		inner join Asset S on 
			S.[Object] = case 
				when I.[Subject] = @type and I.SubjectID = @id then I.[Object] 
				else I.[Subject]
			end
			and S.ObjectID = case 
				when I.[Subject] = @type and I.SubjectID = @id then I.ObjectID 
				else I.SubjectID 
			end	
		inner join AssetType ST on ST.ID = S.AssetTypeID
		cross apply dbo.GetAssetDisplayValueById(S.ID) D
		outer apply (
			select I.* from [Intersect] I
			inner join IntersectTypeDetail D on D.[Object] = ST.[Object] and D.ObjectID = ST.ObjectID and PredicateType = 3
			where I.IntersectTypeID = D.ID and I.ObjectID = S.ObjectID and I.[Object] = S.[Object]
		) P
		left join Asset SP on SP.[Object] = P.[Subject] and SP.ObjectID = P.SubjectID
		left join AssetType SPT on SPT.ID = SP.AssetTypeID
		cross apply dbo.GetAssetDisplayValueById(SP.ID) DP
where	(I.Subject = @type and I.SubjectID = @id) or (I.[Object] = @type and I.ObjectID = @id) and I.visible = 1
union
select 
	null as IntersectID
	,null as [Object]
	,-1 as ObjectID
	,null as ParentID
	,null as ParentUrl
	,null as ParentName
	,S.[Name]
	,'Custom' as ObjectTypeName
	,null as [Description]
	,null as [Url]	
    ,S.ID as CustomID
	,null as [IntersectUid]
	,null as [IntersectTypeUid]
	,null as [AssetUid]
    ,null as [AssetTypeUid]
from 
	[dbo].[nym] s	
where s.[object] = @type and s.[objectID] = @id and s.PredicateID = @predicateId and s.Visible = 1
";

        public static readonly string TaxonomySettingsItem = @"
select	
	T.ObjectID as ID,
	T.Name,
	T.Description,
	T.HierarchyMaximumDepth as MaximumDepth,
	T.UpdatedOn,
	T.UpdatedBy,	
	S.AllowSynonyms,
    T.Uid,
    (select cast(count(1) as bit) from report r where r.ObjectType = 'TaxonomyType' and r.ObjectID = @id and r.ReportType != 'legacy') as HasDashboards	
from	AssetType T		
		cross apply (
					select	case 
								when count(1) > 0 then cast(1 as bit)
								else cast(0 as bit) 
							end as AllowSynonyms
					from	(
								select	IT.ID
								from	IntersectType IT
										inner join [Predicate] ITP on ITP.ID = IT.PredicateID and ITP.Type = 6 -- Synonym
								where	(IT.Subject = 'TaxonomyType' and IT.SubjectID = @id) OR (IT.Object = 'TaxonomyType' and IT.ObjectID = @id)
							) O
					) S
where	T.ObjectID = @id and T.Object='TaxonomyType'";

        public static readonly string ShoppingCartItemList = @"
                select 
	                i.Object, 
	                i.ObjectID, 
	                d.[DisplayValue] as [Name],
	                coalesce(d.TypeName, case when i.[Object] = 'ReferenceItemType' then 'Reference List' else null end) as ObjectTypeName,
					u.Url  
                from
	                Shoppingcartitem i
                left join assetdetail d on d.id = i.[Objectid]                
				cross apply getasseturlbyid(d.ID) u
                where 
	                i.ShoppingCartID = @id";

        public static readonly string SiteNavPermissions = @"
            select p.SiteNavID, p.Object, p.ObjectID, 
			CASE p.Object WHEN 'Resource' then 'User' ELSE p.Object END
			+ ' :: ' + coalesce(g.Name,r.FirstName + ' ' + r.Lastname) as Name from sitenavpermission p
            left join [Group] g on g.ID = p.ObjectID and p.Object = 'Group'
            left join reporting.Global_Resource r on r.ResourceID = p.ObjectID and p.Object = 'Resource'
            where p.SiteNavID = @id";

        #region Workflow

        public static readonly string WorkflowDiagramNodes = @"
	   select 
	            cast(vs.ID as varchar) as [Key],
	            vs.XPosition,
	            vs.YPosition,
	            vs.StepType,
	            vs.ActivityType,
	            vs.Settings,
                vs.Fields,
	            vs.Name,
				i.RunCount
            from workflow.[type] t
            inner join workflow.[version] v on v.typeid = t.id
            inner join workflow.[versionstep] vs on vs.versionid = v.id
			outer apply (
				select 
					s.stepid, 
					case when vs.Settings.value('/settings[1]/WaitForAllTransitions[1]','varchar(max)') = 'true' then
						count(s.stepid) / coalesce((select count(*) from workflow.versionsteptransition vst where vst.toversionstepid = vs.id), 1)
					else
						count(s.stepid) 
					end as RunCount
				from workflow.itemstep s 
				where s.stepid = vs.id
				group by s.stepid
			) i 
            where t.id = @id and vs.[State] = 1 and v.id = coalesce((select top 1 id from workflow.version where typeid = @id and version = @version), (select top 1 id from workflow.version where typeid = @id order by [version] desc))
";

        public static readonly string WorkflowDiagramLinks = @"
			select 
	            cast(vst.ID as varchar) as [Key],
	            vst.FromVersionStepID as FromKey,
	            vst.ToVersionStepID as ToKey,
	            vst.TransitionType,
	            vst.Condition,
                vst.Settings,
	            vst.Name,
                vst.FromPortID,
                vst.ToPortID
            from workflow.[type] t
            inner join workflow.[version] v on v.typeid = t.id
            inner join workflow.[versionstep] vs on vs.versionid = v.id
            inner join workflow.[versionsteptransition] vst on vst.fromversionstepid = vs.id
            where t.id = @id and vst.State = 1 and v.id = coalesce((select top 1 id from workflow.version where typeid = @id and version = @version), (select top 1 id from workflow.version where typeid = @id order by [version] desc))
";

        public static readonly string WorkflowObjectTypes = $@"
           	select * from (
                select 
		            [object] + '|' + cast(objectId as varchar) as [value],
		            objectId as id,
		            [object] as [type],		
		            case when T.[object] = 'ArtifactType' and T.[Class] = 1 then
			            'Business Asset'
                    when T.[object] = 'ArtifactType' and T.[Class] = 8 then
			            'Technical Asset'
		            when T.[object] = 'RuleType' then
			            'Rule Type'
		            when T.[object] = 'PolicyType' then
			            'Policy Type'
		            when T.[object] = 'ReferenceItemType' then
			            'Reference List'
		            when T.[object] = 'TaxonomyType' then
			            'Model Type'
		            when T.[object] = 'ShoppingCartType' then
			            'Shopping Cart'
		            else
			            ''
		            end + ' :: ' + coalesce(P.[Path], T.[Name]) as label, 
		            assetCount.[count]
	            from 
		            AssetType T
					cross apply dbo.GetAssetTypeTextPathById(T.ID, ' / ') P
		            cross apply 
		            (
				            select count(*) as [count] from Asset A
				            where A.AssetTypeID = T.ID
		            ) assetCount
	            where
		            T.[object] in ('ArtifactType','TaxonomyType','PolicyType','RuleType','ShoppingCartType','ReferenceItemType')
	            union all
                select 'IntersectType|' + cast(t.id as varchar) as value, t.id, 'IntersectType' as [type], 'Relationship :: ' + t_name.Name as [label], 1 as [count] 
                from intersecttype t
				inner join [Predicate] p on p.ID=t.PredicateID and p.Type not in ({(int)PredicateType.Diagram}, {(int)PredicateType.DiagramUse}, {(int)PredicateType.DiagramReference})
	            cross apply dbo.GetIntersectTypeNames(t.ID) t_name			
                group by t.id, t_name.name
	            union all
	            select 'IssueType|' + cast(t.id as varchar) as value, t.id, 'IssueType' as [type], 'Action Type :: ' + t.Name as [label], count(*) as [count] 
                from issuetype t
                left join issue a on a.issuetypeid = t.id
                group by t.id, t.name
                ) o
                order by o.label
";

        public static readonly string WorkflowList = @"
                  select t.ID
                    ,t.Name
                    ,t.Description
                    ,t.CreatedOn
					,coalesce(rc.FirstName + ' ' + rc.LastName, '') as CreatedBy
                    ,t.UpdatedOn
					,coalesce(ru.FirstName + ' ' + ru.LastName, '') as UpdatedBy
                    ,e.ChangeType
                    ,coalesce(d.Name, ITN.Name, it_t.Name, st.Name) as TypeName,
					case when t.PublishedVersionID is not null then
						'Version ' + cast(v.Version as varchar) + ' Published'
					else
						'Unpublished'
					end as Published,
					case when e.[Object] = 'ArtifactType' and D.[Class] = 1 then
						'Business Asset'
                    when e.[Object] = 'ArtifactType' and D.[Class] = 8 then
						'Technical Asset'
					when e.[Object] = 'RuleType' then
						'Rule'
					when e.[Object] = 'PolicyType' then
						'Policy'
					when e.[Object] = 'TaxonomyType' then
						'Model'
					when e.[Object] = 'IssueType' then
						'Action'
                    when e.[Object] = 'IntersectType' then
						'Relationship'
                    when e.[Object] = 'ShoppingCartType' then
                        'Shopping Cart'
					when e.[Object] = 'ReferenceItemType' then
					'Reference List'
					else
						''
					end as [Type],
                    t.State as State
                from workflow.type t
                inner join workflow.eventregistration e on e.typeid = t.id
                left join AssetType D on D.Object = E.Object and D.ObjectID = e.ObjectID 
                left join issuetype it_t on e.object = 'IssueType' and it_t.id = e.objectid
				left join IntersectType IT on e.Object = 'IntersectType' and e.objectid = IT.ID
				outer apply dbo.GetIntersectTypeNames(IT.ID) ITN
                left join ShoppingCartType st on st.ID = e.objectid and e.object = 'ShoppingCartType'
				left join workflow.version v on v.id = t.publishedversionid
				left join reporting.Global_Resource rc on rc.ResourceID = t.CreatedBy
				left join reporting.Global_Resource ru on ru.ResourceID = t.UpdatedBy
				where t.State in (1,4)
                order by t.Name asc";

        public static readonly string WorkflowVersionStepHistory = @"
  select 
	IST.ID as ItemStepID, 
	convert(nvarchar(max),IST.Fields) as Fields,
	IST.StartedOn,
	IST.CompletedOn,
	RS.FirstName + ' ' + RS.LastName as StartedBy,
	RC.FirstName + ' ' + RC.LastName as CompletedBy,
	coalesce(s.[Object], I.[Object]) as [Object],
	coalesce(s.ObjectID, I.ObjectID) as ObjectID,
	coalesce(dv.DisplayValue, ISD.SubjectShortName + ' [' + ISD.PredicateName + '] ' + ISD.ObjectShortName , '[unknown]') as [Name], 
	coalesce(D.TypeName, DITN.Name, '[unknown]') as ObjectTypeName,
	UL.[Url] as NgUrl, 
	coalesce(D.DisplayValue, DIN.Name, '[unknown]') as TextPath,
	VS.[Name] as StepName, 
	dbo.GetWorkflowResponsibleUsers(IST.ID, 0) as Assignments,
	convert(nvarchar(max),VS.Settings) as Settings,
	VS.StepType as StepType,
	VS.ActivityType,
	case when IST.CompletedOn is not null then
		case when VS.ActivityType = 2 then
			'Status was changed to '  + convert(xml,convert(nvarchar(max),VS.Settings)).value('/settings[1]/Status[1]/text()[1]','nvarchar(max)')
		when VS.ActivityType = 3 then
			'Form completed by ' + 
				case when IST.CompletedBy is not null and convert(xml,convert(nvarchar(max),VS.Settings)).value('/settings[1]/FormResponseType[1]/text()[1]', 'nvarchar(max)') = 'FirstResponse'  then
					dbo.GetWorkflowResponsibleUsers(IST.ID, 1)
				else
					dbo.GetWorkflowResponsibleUsers(IST.ID, 0)
				end
		when VS.ActivityType = 1 then
			case when convert(xml,convert(nvarchar(max),VS.Settings)).value('/settings[1]/MessageRecipientType[1]/text()[1]','nvarchar(max)') = 'Initiator' then
				'Email sent to ' + RS.FirstName + ' ' + RS.LastName
			when convert(xml,convert(nvarchar(max),VS.Settings)).value('/settings[1]/MessageRecipientType[1]/text()[1]','nvarchar(max)') = 'SpecificUser' then
				'Email sent to ' + coalesce(convert(xml,convert(nvarchar(max),VS.Settings)).value('/settings[1]/MessageToUser[1]/text()[1]','nvarchar(max)'), '[unknown]')
			when convert(xml,convert(nvarchar(max),VS.Settings)).value('/settings[1]/MessageRecipientType[1]/text()[1]','nvarchar(max)') = 'Responsibility' then
				'Email sent to ' + [dbo].[GetEmailStepRecipients](IST.ID)
			else
				'Email sent'
			end
		else
			'Step completed'
		end
	when VS.ActivityType = 3 then --form
		'Waiting for form completion by ' + dbo.GetWorkflowResponsibleUsers(IST.ID, 0)
	when VS.ActivityType = 1 then --email
		'An error occurred or the email is currently queued for sending'
	else
		''
	end as [Comment],
	case when IST.CompletedOn is not null then
		'Complete'
	else
		'In Progress'
	end as [Status],
	null as SettingsObject,
	null as FieldsObject

from 
	workflow.ItemStep IST
	left join workflow.Item I on I.ID = IST.ItemID
	left join Issue s on I.[Object] = 'Issue' and S.ID = I.ObjectID
	left join [IntersectDetail] ISD on coalesce(s.[Object], I.[Object]) = 'Intersect' and ISD.ID = coalesce(s.ObjectID, I.ObjectID)
	left join Asset A on A.[Object] = coalesce(s.[Object], I.[Object]) and A.ObjectID = coalesce(s.ObjectID, I.ObjectID)
	left join AssetType AST on AST.id = A.AssetTypeID
	outer apply dbo.GetAssetDisplayValueById(A.ID) DV
	outer apply dbo.GetAssetUrlById(A.ID) UL
	inner join workflow.VersionStep VS on VS.ID = IST.StepID
	left join AssetDetail D on D.Object = I.Object and D.ObjectID = I.ObjectID
	left join [Intersect] DI on 'Intersect' = I.Object and DI.ID = I.ObjectID
    left join IntersectType DIT on DIT.ID = DI.IntersectTypeID
	outer apply dbo.GetIntersectNames(DI.ID) DIN	
	outer apply dbo.GetIntersectTypeNames(DIT.ID) DITN
	left join reporting.Global_resource RS on RS.ResourceID = IST.StartedBy
	left join reporting.Global_resource RC on RC.ResourceID = IST.CompletedBy
	inner join workflow.[Version] V on V.ID = VS.VersionID
	inner join workflow.[Type] T on T.ID = V.TypeID
	outer apply (
		select case when vsw.Settings.value('/settings[1]/WaitForAllTransitions[1]','nvarchar(max)') = 'true' then
			1
		else
			0
		end as [value]
		from workflow.versionstep vsw where vsw.id = ist.stepid
	) waitForAll
where 
	VS.ID = @id
	and (waitForAll.[value] = 0 or (waitForAll.[value] = 1 and ist.id = (select max(id) from workflow.itemstep where itemid = ist.itemid and stepid = ist.stepid)))
order by IST.StartedOn desc, IST.CompletedOn desc
";

        public static readonly string WorkflowTypeList = @"
             with a as
            (
            select 
	            t.id as TypeID,
	            t.Name, 
	            case when v.ID = t.PublishedVersionID then 
		            cast(v.Version as varchar) + ' (Published)' 
	            else 
		            cast(v.Version as varchar) 
	            end as VersionName, 
	            v.Version, 
	            v.UpdatedOn,
	            r.FirstName + ' ' + r.LastName as UpdatedBy,  
	            coalesce(ta.Name, it.Name,ITypeName.Name) as ObjectTypeName, 
	            coalesce(isst.Object, ta.Object) as Object, 
	            coalesce(isst.ObjectID, ta.ObjectID) as ObjectID, 
	            dbo.GenerateAssetTypeUrl(ta.ID) as NgUrl, 
	            v.id as VersionID,
	            dbo.GetWorkflowObjectsSummary(v.id, @filteredObject, @filteredObjectId) as ObjectNames, 
 	            null as Responsibility, 
	            null as SpecificUser,
	            case when count(s.StepID) > 0 then
		            case when max(vs.ActivityType) = 3 then
			            'Waiting on user action'
		            else
			            'Incomplete'
		            end
	            else
		            'Complete'
	            end as [Status],
	            max(s.StepID) as CurrentStepID
            from workflow.type t
            join workflow.eventregistration e on e.typeid = t.id
            join workflow.version v on v.typeid = t.id
			left join AssetType ta on ta.object = e.object and ta.objectId = e.objectid
            left join reporting.Global_resource r on r.ResourceID = v.UpdatedBy
            left join (select distinct object, objectid, versionid from workflow.item where Object='Issue') i on i.versionid = v.id
			left join Issue iss on i.Object ='Issue' and iss.ID = i.ObjectID
			left join Asset issa on issa.Object = iss.Object and issa.ObjectID = iss.ObjectID
			left join AssetType isst on isst.ID = issa.AssetTypeID
			left outer join [dbo].[issuetype] it on(iss.issuetypeid = it.id) 
            left join workflow.versionstep vs on vs.versionid = v.id
            left join workflow.itemstep s on s.stepid = vs.id and s.CompletedOn is null
            left join workflow.itemassignment ia on ia.itemid = s.itemid
			left  join [dbo].[intersect] inter on (i.[object]='Intersect' and inter.id=i.[objectId])
            outer apply dbo.GetIntersectTypeNames(inter.IntersectTypeId) ITypeName
            where {0} t.State <> 3
            {1}
            group by t.id, t.name, v.Version, v.UpdatedOn, v.UpdatedBy,ta.Name, coalesce(isst.Object, ta.Object), 
            coalesce(isst.ObjectID,ta.ObjectID),it.Name,ITypeName.Name, ta.ID, v.id, t.PublishedVersionID, r.FirstName, r.LastName
			)
            select 
	            a.*,
	            vs.Settings,
	            vs.ActivityType, 
	            vs.StepType, 
	            null as ResponsibleUser, 
	            null as SpecificUser, 
	            s.StartedBy 
            from a
            left join workflow.versionstep vs on vs.id = a.currentstepid
            left join (
	            select 
		            stepid, 
		            string_agg(r.Firstname + ' ' + r.LastName,', ') as StartedBy
	            from (select distinct stepid, startedby from workflow.itemstep) i
	            left join reporting.Global_resource r on r.ResourceID = i.StartedBy
	            group by stepid
            ) s on s.stepid = a.currentstepid
            order by a.Name asc, a.Version desc, a.UpdatedOn desc";

        public static readonly string WorkflowAssignments = @"
select
	                                 WT.[Name] as 'WorkflowName'
                                    ,WT.ID as TypeID
                                    ,WV.[Version] as [Version]
	                                ,WI.[Object] as 'Object'
	                                ,WI.[ObjectID] as 'ObjectID'
	                                ,WI.StartedOn as 'StartedOn'
	                                ,WI.StartedBy as 'StartedByResourceID'
                                    ,WI.ID as 'ItemID'
	                                ,GR.FirstName + ' ' + GR.LastName as 'StartedBy'
	                                ,case 
										when WI.[Object] = 'Issue' then IT.[Name]
										else AD.TypeName
									end as 'TypeName'
	                                ,AD.[Type] as 'ObjectType'
	                                ,AD.TypeID as 'ObjectTypeID'
	                                ,coalesce(CAD.DisplayValue, ISD.SubjectShortName + ' [' + ISD.PredicateName + '] ' + ISD.ObjectShortName, AD.DisplayValue, IT.[Name], '(item deleted)') as 'ObjectName'
	                                ,WIS.ID as 'ItemStepID'
	                                ,WVS.[Name] as 'StepName'
	                                ,WVS.StepType as 'StepType'
	                                ,WVS.ActivityType as 'ActivityType'
                                    ,ISS.[Object] as 'IssueObject'
									,ISS.[ObjectID] as 'IssueObjectID'
                                    ,CAD.DisplayValue as 'IssueObjectName'
                                    ,case when coalesce(CAD.DisplayValue, ISD.SubjectShortName + ' [' + ISD.PredicateName + '] ' + ISD.ObjectShortName, AD.DisplayValue, IT.[Name]) is null then
                                        cast(1 as bit)
                                    else
                                        cast(0 as bit)       
                                    end as Deleted
                                from
	                                [workflow].[Type] WT
	                                inner join workflow.[Version] WV on WT.ID = WV.TypeID
	                                inner join workflow.Item WI on WV.ID = WI.VersionID
	                                inner join reporting.Global_Resource GR on WI.StartedBy = GR.ResourceID
									left join AssetDetail AD on AD.[Object] = WI.[Object] and AD.[ObjectID] = WI.[ObjectID]
									left join IntersectDetail ISD on WI.[Object] = 'Intersect' and WI.ObjectID = ISD.ID
	                                inner join workflow.ItemStep WIS on WIS.ItemID = WI.ID and WIS.CompletedOn is null
                                    inner join workflow.ItemAssignment WIA on WIA.ItemID = WI.ID and WIA.ResourceObject = 'Resource' and WIA.ResourceObjectID = @resourceId
	                                inner join workflow.VersionStep WVS on WVS.ID = WIS.StepID
                                    left outer join Issue ISS on WI.[ObjectID] = ISS.ID and WI.[Object] = 'Issue'
									left outer join AssetDetail CAD on ISS.ObjectID = CAD.ObjectID and CAD.[Object] = ISS.[Object]
                                    left outer join Issuetype IT on ISS.IssueTypeID = IT.ID
                                where
                                     WT.ID in ({0}) and WI.CompletedOn is null and WVS.StepType = 2 and WVS.ActivityType = 3";

        public static readonly string WorkflowItemSteps = @"
      select 
	            IST.ID,
	            IST.ItemID,
	            IST.StepID,
	            S.[Name],
	            S.StepType,
	            S.ActivityType,
	            case when IST.CompletedOn is null then
		            cast(0 as bit)
	            else
		            cast(1 as bit)
	            end as Complete,
	            IST.StartedOn,
	            RS.FirstName + ' ' + RS.LastName as StartedBy,
	            IST.CompletedOn,
	            RC.FirstName + ' ' + RC.LastName as CompletedBy,
                VSSettings.MessageRecipientType,
                case when S.ActivityType = 3 and IST.CompletedOn is null and VSSettings.MessageRecipientType = 'Initiator' then
					IAR.Email
                when S.ActivityType = 3 and IST.CompletedOn is null and VSSettings.MessageRecipientType = 'SpecificUser' then
					VSSettings.MessageToUser
				when S.ActivityType = 3 and IST.CompletedOn is null and VSSettings.MessageRecipientType = 'Group' then
					(select STRING_AGG(CONCAT(R.FirstName, ' ', R.LastName), ', ')
						from (
							select rg.ResourceID FROM [resourcegroup] rg
							inner join dbo.Asset a on a.[Object] = 'Group' and rg.groupid = a.[ObjectId]
							where A.uid = VSSettings.MessageToGroup
							and rg.ResourceID NOT IN (
											select 
								r.value('@fromResourceId','int')
								from workflow.itemstep
								cross apply Fields.nodes('/fields[1]/Reassigned') AS x(r)
								where StepID = IST.StepID
								)
							UNION ALL
							select 
								r.value('@toResourceId','int')
								from workflow.itemstep
								cross apply Fields.nodes('/fields[1]/Reassigned') AS x(r)
								where StepID = IST.StepID
						) subq
						INNER JOIn reporting.Global_Resource R on R.ResourceID = subq.ResourceID
					)
				when S.ActivityType = 3 and IST.CompletedOn is not null then
					Forms.Responses
				else
                    null
                end as Assignee,
                IST.Fields,
				case when E.Object = 'IssueType' then
					cast(1 as bit)
				else
					cast(0 as bit)
				end as IsIssueType,
				I.[Object],
				I.ObjectID,
                E.TypeID,
				IST.Uid as UID
            from 
            workflow.ItemStep IST
            inner join workflow.Item I on I.ID = IST.ItemID
            inner join workflow.VersionStep S on S.ID = IST.StepID
			cross apply (
				select
					coalesce(convert(xml,convert(nvarchar(max),Settings)).value('/settings[1]/WaitForAllTransitions[1]/text()[1]','nvarchar(max)'), 'false') as WaitForAllTransitions,
					convert(xml,convert(nvarchar(max),Settings)).value('/settings[1]/MessageRecipientType[1]/text()[1]','nvarchar(max)') as MessageRecipientType,
					coalesce(convert(xml,convert(nvarchar(max),S.Settings)).value('/settings[1]/MessageToUser[1]/text()[1]','nvarchar(max)'), '[unknown]') as MessageToUser,
					coalesce(convert(xml,convert(nvarchar(max),S.Settings)).value('/settings[1]/MessageToGroup[1]/text()[1]','nvarchar(max)'), '[unknown]') as MessageToGroup
				from workflow.VersionStep where ID = S.ID
			) VSSettings
			inner join workflow.[Version] V on V.ID = S.VersionID
			inner join workflow.EventRegistration E on E.TypeID = V.TypeID
			left join (
				select 
					IA.ItemID, 
					IA.ItemStepID, 
					RI.FirstName + ' ' + RI.LastName as [Name],
                    RI.Email
				from workflow.ItemAssignment IA
				inner join reporting.Global_Resource RI on RI.ResourceID = IA.ResourceObjectID
			) IAR on IAR.ItemID = IST.ItemID and (IAR.ItemStepID = IST.ID or IAR.ItemStepID is null) and VSSettings.MessageRecipientType = 'Initiator'
            left join reporting.Global_resource RS on RS.ResourceID = IST.StartedBy
            left join reporting.Global_resource RC on RC.ResourceID = IST.CompletedBy
			outer apply (
				select 
					string_agg(G.FirstName + ' ' + G.LastName, ',') as Responses
				from workflow.itemstep
				cross apply Fields.nodes('/fields[1]/form') AS x(r)
				inner join reporting.Global_Resource G on G.ResourceID = r.value('@ResourceID','int')
				where ID = IST.ID
			) Forms
            where IST.ItemID = @itemId
			and ((VSSettings.WaitForAllTransitions = 'true' and (IST.CompletedOn is not null or IAR.ItemStepID is not null or exists(select 1 from workflow.ItemAssignment ia where ia.ItemStepID = IST.ID)  )) 
				or (VSSettings.WaitForAllTransitions = 'false'))
            order by IST.StartedOn, IST.CompletedOn";

        #endregion
    }
}
