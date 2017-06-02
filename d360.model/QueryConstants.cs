namespace d360.model
{
    public static class QueryConstants
    {
        public static string AgentErrorList = @"
select	ER.Date,
        ERI.Message,
        ER.MachineName,                
        F.ID as FusionID, 
        F.Name as Fusion,             
        FT.Name as FusionType
from	fusion.AgentError ER
    inner join fusion.AgentErrorItem ERI on ER.ID = ERI.AgentErrorID                            
    inner join Fusion F on F.ID = ER.FusionID
    inner join FusionType FT on FT.ID = F.FusionTypeID";

        public static string AgentHistoryList = @"
select  S.DateStarted, 
        S.DateCompleted, 
        S.MachineQueuedOn, 
        S.Success,             
        S.Message, 
        F.ID as FusionID, 
        F.Name as Fusion, 
        F.FusionTypeID,
        FT.Name as FusionType 
from    FusionStatusLog S 
        inner join Fusion F on F.ID = S.FusionID
        inner join FusionType FT on FT.ID = F.FusionTypeID
        order by S.DateStarted desc";

        public static string AgentHistoryExportList = @"
select top {0}  S.DateStarted, 
        S.DateCompleted, 
        S.MachineQueuedOn, 
        S.Success,             
        S.Message, 
        F.ID as FusionID, 
        F.Name as Fusion, 
        F.FusionTypeID,
        FT.Name as FusionType 
from    FusionStatusLog S 
        inner join Fusion F on F.ID = S.FusionID
        inner join FusionType FT on FT.ID = F.FusionTypeID
        ";

        public static string ArtifactActivitySpecificDateCountList = @"
select 
	Name,
	sum(New) as New,
	sum(Total) as Total,
	Id
from
(select  at.name as Name,
	    1 as New,        					
		0 as Total,
        at.id as Id		
from    Artifact a
        inner join artifacttype at on a.artifacttypeid = at.id
where   a.createdon > dateadd(day, @d, CURRENT_TIMESTAMP)
union all
select  at.name as Name,
	    0 as New,   
		1 as Total,     					
        at.id as Id		
from    Artifact a
        inner join artifacttype at on a.artifacttypeid = at.id
where a.updatedon > dateadd(day, @d, CURRENT_TIMESTAMP)) T
group by Name, Id";

        public static string ArtifactActivityAllDateCountList = @"
select  at.name as Name,
	    count(1) as New,        					
        count(1) as Total,
        at.id as Id								
from    Artifact a
        inner join artifacttype at on a.artifacttypeid = at.id                        
group by at.name,at.id order by at.name";

        public static string ArtifactBreadcrumbItem = @"
with h as
	(
	select	[ObjectID] as ID, [Name], [ParentID], [Url], [ObjectTypeName], [ObjectTypeID],
			[dbo].GenerateObjectUrl(ObjectType, ObjectTypeID, ObjectTypeID) as TypeUrl,
			0 as [Level]
	from	[cache].[ObjectDetails]
	where	[Object] = 'Artifact' and ObjectID = @id
	union all
	select	P.[ObjectID] as ID, P.[Name], P.[ParentID], P.[Url], P.[ObjectTypeName], P.[ObjectTypeID],
			[dbo].GenerateObjectUrl(P.ObjectType, P.ObjectTypeID, P.ObjectTypeID) as TypeUrl,
			C.[Level]-1 as [Level]
	from	[cache].[ObjectDetails] P
			inner join h C on P.[Object] = 'Artifact' and P.ObjectID = C.ParentID
	)
select ObjectTypeName as TypeName, TypeUrl, Name, Url from h order by [Level]";

        public static string ArtifactNgBreadcrumbItem = @"
with h as
	(
	select	[ObjectID] as ID, [Name], [ParentID], [NgUrl] as Url, [ObjectTypeName], [ObjectTypeID],
			[dbo].GenerateNgObjectUrl(ObjectType, ObjectTypeID, ObjectTypeID) as TypeUrl,
			0 as [Level]
	from	[cache].[ObjectDetails]
	where	[Object] = 'Artifact' and ObjectID = @id
	union all
	select	P.[ObjectID] as ID, P.[Name], P.[ParentID], P.[NgUrl] as Url, P.[ObjectTypeName], P.[ObjectTypeID],
			[dbo].GenerateNgObjectUrl(P.ObjectType, P.ObjectTypeID, P.ObjectTypeID) as TypeUrl,
			C.[Level]-1 as [Level]
	from	[cache].[ObjectDetails] P
			inner join h C on P.[Object] = 'Artifact' and P.ObjectID = C.ParentID
	)
select ObjectTypeName as TypeName, TypeUrl, Name, Url from h order by [Level]";

        public static string ObjectNymTypes = @"
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

        public static string ArtifactSettingsItem = @"
select	*
from	(
		select	case 
					when count(1) > 0 then cast(1 as bit)
					else cast(0 as bit)
				end as AllowAttributes
		from	AttributeTypeRelation
		where	ObjectType = 'ArtifactType' and ObjectID = @id
		) A		
		inner join	(
					select	case 
								when count(1) > 0 then cast(1 as bit)
								else cast(0 as bit) 
							end as AllowPredicateHierarchies
					from	(
								select	IT.ID
								from	IntersectType IT
										inner join [Predicate] P on P.ID = IT.PredicateID and P.[Type] = 3 -- TypeOf
								where	((IT.Subject = 'ArtifactType' and IT.SubjectID = @id) OR (IT.Object = 'ArtifactType' and IT.ObjectID = @id))

							) O
					) P on 1=1";

        public static string ArtifactTypeStatisticsList = @"
select		T.ID,
			T.ParentID,
			T.Name,
			T.Description,
            cast(1 as bit) as expanded,
			AC.*,
			BC.*
from		ArtifactType T
			cross apply (
						select	count(1) AS [Total]
						from	Artifact
						where	ArtifactTypeID = T.ID
								and Status in ('Draft', 'Under Review', 'Certified')
						) AC
			cross apply (
						select	[Draft], [Under Review] as UnderReview, [Certified]
						from	(
								select		Status
								from		Artifact
								where		ArtifactTypeID = T.ID
											and Status in ('Draft', 'Under Review', 'Certified')
								) S
						pivot	(
								count(Total) for Status in ([Draft], [Under Review], [Certified])
								) as pt
						) BC
order by	T.ParentID,
			T.Name";
             

        public static string ExecutionErrorList = @"
select  ER.Date,
        ER.Error,
        ER.ExecutionID,
        F.ID as FusionID, 
        F.Name as Fusion, 
        F.FusionTypeID,
        FT.Name as FusionType
from	fusion.Error ER
        inner join fusion.Execution EX on EX.ID = ER.ExecutionID
        inner join Fusion F on F.ID = EX.FusionID
        inner join FusionType FT on FT.ID = F.FusionTypeID";

        public static string ExecutionErrorExportList = @"
select  ER.Date,
        ER.Error,
        ER.ExecutionID,
        F.ID as FusionID, 
        F.Name as Fusion, 
        F.FusionTypeID,
        FT.Name as FusionType
from	fusion.Error ER
        inner join fusion.Execution EX on EX.ID = ER.ExecutionID
        inner join Fusion F on F.ID = EX.FusionID
        inner join FusionType FT on FT.ID = F.FusionTypeID where EX.ID = {0}";

        public static string ExecutionHistoryList = @"
select	E.ID,
		    E.RawLogFileName,
		    E.DateStarted,
		    E.DateCompleted,
		    E.Adds,
		    E.Updates,
		    E.Deletes,
		    X.[C] as ErrorCount,
		    R.[C] as ResultCount,
            F.ID as FusionID, 
            F.Name as Fusion, 
            F.FusionTypeID,
            FT.Name as FusionType
from	    fusion.Execution E
            inner join Fusion F on F.ID = E.FusionID
            inner join FusionType FT on FT.ID = F.FusionTypeID
            cross apply (
			            select count(1) as [C] from fusion.Error where ExecutionID = E.ID
			            ) X
            cross apply (
			            select count(1) as [C] from fusion.Result where ExecutionID = E.ID
			            ) R 
order by    DateStarted desc";

        public static string ExecutionHistoryExportList = @"
select	top {0} E.ID,
		    E.RawLogFileName,
		    E.DateStarted,
		    E.DateCompleted,
		    E.Adds,
		    E.Updates,
		    E.Deletes,
		    X.[C] as ErrorCount,
		    R.[C] as ResultCount,
            F.ID as FusionID, 
            F.Name as Fusion, 
            F.FusionTypeID,
            FT.Name as FusionType
from	    fusion.Execution E
            inner join Fusion F on F.ID = E.FusionID
            inner join FusionType FT on FT.ID = F.FusionTypeID
            cross apply (
			            select count(1) as [C] from fusion.Error where ExecutionID = E.ID
			            ) X
            cross apply (
			            select count(1) as [C] from fusion.Result where ExecutionID = E.ID
			            ) R";

        public static string ExecutionResultList = @"
select	A.TextPath as FusionAttribute,
        AT.TextPath as FusionAttributeType,
        E.ExecutionID,
        E.FusionAttributeID,
        E.Body,
        E.FieldTypeID,
        E.FieldName,
        case E.[Action] when 'A' then 'Added' when 'U' then 'Updated' else 'Removed' end as [Action],
        E.OldValue,
        E.NewValue,
        E.ID,
        F.ID as FusionID, 
        F.Name as Fusion, 
        F.FusionTypeID,
        FT.Name as FusionType
from	fusion.Result E
        inner join FusionAttribute A on A.ID = E.FusionAttributeID 
        inner join FusionAttributeType AT on AT.ID = A.FusionAttributeTypeID
        inner join Fusion F on F.ID = A.FusionID
        inner join FusionType FT on FT.ID = F.FusionTypeID
where   ExecutionID = {0}";

        public static string FilterableAttributeTypesByTypeList = @"
with relations as	(
					select	'IntersectType' as [Type],
							ID
					from	IntersectType
					where	(Subject = @type and SubjectID = @id) OR (Object = @type and ObjectID = @id)
					union
					select	@type as [Type],
							@id as ID
					)
select		T.ID,
			T.Name
from		AttributeTypeRelation ATR
			inner join relations R on R.[Type] = ATR.ObjectType and R.ID = ATR.ObjectID
			inner join AttributeType T on T.ID = ATR.AttributeTypeID
where       T.ID not in (select ObjectID from FieldType where [Object] = 'AttributeType' and ObjectID = T.ID and [Type] in ('Html', 'Link', 'UncLink') and CHARINDEX(Name, T.TextFormatString) > 0)
group by	T.ID,
			T.Name
order by	T.Name";

        public static string FilterableAttributeValuesList = @"
with types as	(
				select	'Intersect' as [Object],
						IntersectID as ID
				from	cache.Relationships
				where	SourceType = @type and SourceTypeID = @id
				union
				select	[Object] as [Object],
						ObjectID
				from	cache.ObjectDetails
				where	ObjectType = @type 
						and ObjectTypeID = @id
				)
select	A.FormattedValue as Name 
from	AttributeDetail A
		inner join types O on O.[Object] = A.ObjectType and O.ID = A.ObjectID and A.AttributeTypeID = @attributeTypeID
group by A.FormattedValue
order by A.FormattedValue";

        public static string FusionBreadcrumbItem = @"
select  f.parentID as 'parentID', 
	    f.name as 'name', 
	    f.id as 'id', 
	    f.fusionattributetypeid as 'typeid',
	    ft.name as 'typename'                                    
from    fusionattribute f
        inner join fusionattributetype ft on (f.fusionattributetypeid = ft.id)
where   f.id = @item";

        public static string FusionConfigurationFromFusionAttributeItem = @"
select  f.name as 'ItemName',
	    f.fusionID as 'ID',
	    f.parentID as 'ParentID',
	    f.fusionattributetypeid as 'FusionAttributeTypeID',
	    fu.fusiontypeid as 'FusionTypeID',
	    fu.name as 'Name',
	    fu.[description] as 'Description',
        f.id as 'SelectedID'
from    fusionattribute f
	    inner join fusion fu on (f.fusionID = fu.id)
	    left outer join fusionattribute fp on (f.parentID = fp.id)
where   f.id = @id";

        public static string FusionOwnershipChildAttributeNodeList = @"
declare @tbl table (ID int, ParentID int);

with at as	(
			select	ID,
					ParentID
			from	FusionAttributeType
			where	ID = @targetFusionAttributeTypeID
			union all
			select	P.ID,
					P.ParentID
			from	FusionAttributeType P
					inner join at C on C.ParentID = P.ID and P.ID <> C.ID
			)
insert into @tbl 
	select * from at

if @currentFusionAttributeTypeID = 0 and @fusionAttributeID = 0
	begin
		select		A.ID,
                    A.ParentID,
					A.FusionAttributeTypeID,
					A.Name
		from		FusionAttribute A
					inner join @tbl t on t.ParentID is null and A.FusionAttributeTypeiD = t.ID and A.FusionID = @fusionID
        where       A.ID not in (
                                select  RI.FusionAttributeID
                                from    FusionAttributeOwnerRuleItem RI
                                        inner join FusionAttributeOwnerRule R on R.ID = RI.FusionAttributeOwnerRuleID and R.ID = @ruleID and R.FusionID = @fusionID and RI.FusionAttributeID is not null
                                )
		order by	A.Name
	end
else
	begin
		select		A.ID,
                    A.ParentID,
					A.FusionAttributeTypeID,
					A.Name
		from		FusionAttribute A
					inner join @tbl t on t.ParentID = @currentFusionAttributeTypeID 
								and A.FusionAttributeTypeiD = t.ID 
								and A.ParentID = @fusionAttributeID
								and A.FusionID = @fusionID
        where       A.ID not in (
                                select  RI.FusionAttributeID
                                from    FusionAttributeOwnerRuleItem RI
                                        inner join FusionAttributeOwnerRule R on R.ID = RI.FusionAttributeOwnerRuleID and R.ID = @ruleID and R.FusionID = @fusionID and RI.FusionAttributeID is not null
                                )
        order by	Name
	end";

        public static string FusionPromotionChildAttributeNodeList = @"
declare @tbl table (ID int, ParentID int);

with at as	(
			select	ID,
					ParentID
			from	FusionAttributeType
			where	ID = @targetFusionAttributeTypeID
			union all
			select	P.ID,
					P.ParentID
			from	FusionAttributeType P
					inner join at C on C.ParentID = P.ID and P.ID <> C.ID
			)
insert into @tbl 
	select * from at

if @currentFusionAttributeTypeID = 0 and @fusionAttributeID = 0
	begin
		select		A.ID,
                    A.ParentID,
					A.FusionAttributeTypeID,
					A.Name
		from		FusionAttribute A
					inner join @tbl t on t.ParentID is null and A.FusionAttributeTypeiD = t.ID and A.FusionID = @fusionID
        where       A.ID not in (
                                select  RI.ObjectID
                                from    fusion.RuleItem RI
                                        inner join fusion.[Rule] R on R.ID = RI.RuleID and R.ID = @ruleID and R.FusionID = @fusionID and RI.ObjectID is not null and RI.ObjectType = 'FusionAttribute'
                                )
		order by	A.Name
	end
else
	begin
		select		A.ID,
                    A.ParentID,
					A.FusionAttributeTypeID,
					A.Name
		from		FusionAttribute A
					inner join @tbl t on t.ParentID = @currentFusionAttributeTypeID 
								and A.FusionAttributeTypeiD = t.ID 
								and A.ParentID = @fusionAttributeID
								and A.FusionID = @fusionID
        where       A.ID not in (
                                select  RI.ObjectID
                                from    fusion.RuleItem RI
                                        inner join fusion.[Rule] R on R.ID = RI.RuleID and R.ID = @ruleID and R.FusionID = @fusionID and RI.ObjectID is not null and RI.ObjectType = 'FusionAttribute'
                                )
        order by	Name
	end";

        public static string FusionRuleItemList = @"
select	I.ID,
        I.RuleID,
        I.ObjectID,
		case when I.ObjectType = 'FusionAttribute' and F.FusionAttributeTypeID = FT.ID then F.TextPath
			when I.ObjectType = 'FusionAttribute' then coalesce(FT.Name + ' attributes under ' + F.TextPath, 'All ' + FT.Name + ' attributes') 
			when I.ObjectType = 'FusionQueryAttribute' and I.ObjectID is not null then QFT.Name
			when I.ObjectType = 'FusionQueryAttribute' then'All ' + QT.Name + ' query attributes'
        end as FusionAttributeName
from	[fusion].[RuleItem] I
		inner join [fusion].[Rule] R on R.ID = I.RuleID
		left join FusionAttributeType FT on FT.ID = R.ObjectID and I.ObjectType = 'FusionAttribute'
        left join FusionAttribute F on F.ID = I.ObjectID and I.ObjectType = 'FusionAttribute'
		left join FusionQueryAttributeType QT on QT.ID = R.ObjectID and I.ObjectType = 'FusionQueryAttribute'
		left join FieldType QFT on I.ObjectType = 'FusionQueryAttribute' and QFT.ID = I.ObjectID
where   I.RuleID = @id
        ";

        public static string FusionRuleMappingList = @"
select	I.ID,
        RS.RuleID,
        I.SourceFieldTypeID,
        coalesce(case when I.SourceFieldTypeID = 0 then I.SourceFieldName end, SF.FriendlyName + ' (' + SF.Name + ')', 'Constant: ' + I.ConstantValue) as SourceFieldName,
        I.TargetFieldTypeID,
        coalesce(case when I.TargetFieldTypeID = 0 then I.TargetFieldName end, TF.FriendlyName + ' (' + TF.Name + ')') as TargetFieldName
from	[fusion].[RuleStepMapping] I
        inner join [fusion].[RuleStep] RS on (I.RuleStepID = RS.ID)
		left join FieldType SF on SF.ID = I.SourceFieldTypeID
		left join FieldType TF on TF.ID = I.TargetFieldTypeID
where   I.RuleStepID = @id";

        public static string FusionStatisticsItem = @"select
	(select count(1) from fusion.agenterror where [date] > Dateadd(Day, @days, CURRENT_TIMESTAMP )) as AgentErrors,
	(select count(1) from fusion.execution where datestarted > Dateadd(Day, @days, CURRENT_TIMESTAMP )) as AgentExecutions,
    (select count(1) from fusion.execution where datestarted > Dateadd(Day, @days, CURRENT_TIMESTAMP )) as FusionExecutions,	
	(select count(1) from fusion.error where [date] > Dateadd(Day, @days, CURRENT_TIMESTAMP )) as FusionErrors,
	(select sum(PromotedTaxonomies) + sum(PromotedDomainItems) + sum(PromotedDomains) + sum(PromotedArtifacts) from fusion.RuleLog where datestarted > Dateadd(Day, @days, CURRENT_TIMESTAMP )) as NumberOfPromotions";

        public static string GroupResourceInfoList = @"
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
        inner join reporting.Global_Resource R on R.ResourceID = RG.ResourceID";

        public static string InformationCatalogDiagramData = @"
with h as (
select		top 100 percent	
			T.ID,
			0 as ParentID,
			T.Name,
            dbo.GenerateObjectUrl('Taxonomy', T.TaxonomyTypeID, T.ID) as Url
from		Taxonomy T
where	    T.TaxonomyTypeID = @id
			and T.ParentID is null
order by	Name
union all
select		top 100 percent	
			C.ID,
			C.ParentID,
			C.Name,
            dbo.GenerateObjectUrl('Taxonomy', C.TaxonomyTypeID, C.ID) as Url
from		Taxonomy C
			inner join h on h.ID = C.ParentID
order by	C.Name
)
select	0 as ID, 
		null as ParentID,
		Name,
        dbo.GenerateObjectUrl('TaxonomyType', ID, ID) as Url,
        cast(0 as bit) as RelationshipsExist
from	TaxonomyType
where	ID = @ID
union
select	ID, 
		ParentID, 
		Name,
        Url,
        cast(R.RelationshipsExist as bit) as RelationshipsExist
from	h
        cross apply (
                    select  case 
                                when count(1) > 0 then 1
                                else 0
                            end as RelationshipsExist
                    from    [Intersect] N 
                    where   ([Subject] = 'Taxonomy' and [SubjectID] = h.ID) OR ([Object] = 'Taxonomy' and [ObjectID] = h.ID)
                    ) R";


        public static string LookupAllocations = @"
	SELECT	FT.Name as FieldTypeName,
			D.ObjectID,
			D.Name as ObjectName,
			D.ObjectType,
			D.ObjectTypeName,
            D.Url as ObjectUrl
	FROM	FieldType FT
			inner join cache.ObjectDetails D on D.[Object] = FT.[Object] and D.ObjectID = FT.ObjectID
	WHERE	FT.LookupObjectType = @type
            AND FT.LookupObjectID = @id";

        public static string MapItemsForMapSequenceManagement = @"
declare @objects table (Type varchar(50), ID int)

insert into @objects values (@type, @id)

if not exists(
	select	MI.ID
	from	MapItem MI
			inner join IntersectDetail SI on SI.ID = MI.SourceIntersectID
			inner join IntersectDetail TI ON TI.ID = MI.TargetIntersectID
	where 	( (SI.Subject = @type and SI.SubjectID = @id) OR (SI.Object = @type and SI.ObjectID = @id)  )
			OR ( (TI.Subject = @type and TI.SubjectID = @id) OR (TI.Object = @type and TI.ObjectID = @id)  )
)
begin
	insert into @objects
		select	case 
					when I.Subject = @type and I.SubjectID = @id then I.Object
					else I.Subject
				end,
				case 
					when I.Subject = @type and I.SubjectID = @id then I.ObjectID 
					else I.SubjectID 
				end
		from	[Intersect] I
				inner join IntersectType T on T.ID = I.IntersectTypeID 
				inner join Predicate P on P.ID = T.PredicateID and P.Type = 6
		where	(I.Subject = @type and I.SubjectID = @id) or (I.Object = @type and I.ObjectID = @id)
end

declare @points table ( ID int, SourceIntersectID int, TargetIntersectID int )

-- get all items directly tied to the focal object.
insert into @points
	select	MI.ID, MI.SourceIntersectID, MI.TargetIntersectID
	from	MapItem MI
			inner join [Intersect] SI on SI.ID = MI.SourceIntersectID
			inner join [Intersect] TI ON TI.ID = MI.TargetIntersectID
			inner join @objects O on	( (SI.Subject = O.Type and SI.SubjectID = O.ID) OR (SI.Object = O.Type and SI.ObjectID = O.ID)  ) OR 
										( (TI.Subject = O.Type and TI.SubjectID = O.ID) OR (TI.Object = O.Type and TI.ObjectID = O.ID)  )

-- get all items not directly tied to the focal object, but still tied to maps involved above.
insert into @points
	select	MI.ID, MI.SourceIntersectID, MI.TargetIntersectID
	from	MapItem MI
			inner join	(
						select	ID.MapItemID
						from	MapItemMap DM
								inner join @points D on D.ID = DM.MapItemID
								inner join MapItemMap ID on ID.MapID = DM.MapID and ID.MapItemID not in (
																										select ID from @points
																										)
						) O on O.MapItemID = MI.ID;

with cte as (
	select	ID,
			SourceIntersectID,
			TargetIntersectID,
			1 as [Level]
	from	@points
	union all
	select	S.ID,
			S.SourceIntersectID,
			S.TargetIntersectID,
			T.[Level] + 1 as [Level]
	from	MapItem S
			inner join cte T on T.SourceIntersectID = S.TargetIntersectID and S.ID <> T.ID
	where	T.[Level] <= 25
)
insert into @points
	select ID, SourceIntersectID, TargetIntersectID from cte where ID not in (select ID from @points)

select	O.ID,
		O.SourceIntersectID,
		SI.SubjectName + ' ' + coalesce(SI.PredicateName, 'stores') + ' ' + SI.ObjectName as [Source],
		O.TargetIntersectID,
		TI.SubjectName + ' ' + coalesce(TI.PredicateName, 'stores') + ' ' + TI.ObjectName as [Target]
from	@points O
		inner join IntersectDetail SI on SI.ID = O.SourceIntersectID
		inner join IntersectDetail TI ON TI.ID = O.TargetIntersectID";

        public static string FusionAttributeRelationshipAllCountsWithZero = @"
select	IT.ID as IntersectTypeID,
		case 
			when (IT.Subject = 'FusionAttributeType' and IT.SubjectID = fa.fusionattributetypeid) then IT.[Object]
			else IT.[Subject]
		end as [Object],
		case 
			when (IT.Subject = 'FusionAttributeType' and IT.SubjectID = fa.fusionattributetypeid) then IT.[ObjectID]
			else IT.[SubjectID]
		end as [ObjectID],		
		I.[Count],
		case 
			when (IT.Subject = 'FusionAttributeType' and IT.SubjectID = fa.fusionattributetypeid) then IT.[ObjectName] 
			else IT.SubjectName
		end + IIF(P.ID is not null, ' [' + P.Name + ']', '') as [Name]
from	[dbo].[fusionattribute] fa	
		inner join IntersectTypeDetail IT on ( 
										(IT.Subject = 'FusionAttributeType' and IT.SubjectID = fa.fusionattributetypeid) OR 
										(IT.Object = 'FusionAttributeType' and IT.ObjectID = fa.fusionattributetypeid) 
									   )
		left join [Predicate] P on P.ID = IT.PredicateID
		cross apply (
					select	count(1) as [Count]
					from	[Intersect] 
					where	IntersectTypeID = IT.ID 
							and (
								(Subject = 'FusionAttribute' and SubjectID = @objId) or 
								(Object = 'FusionAttribute' and ObjectID = @objId)
								)
					) I
where	fa.ID = @objId
order by case 
			when (IT.Subject = 'FusionAttributeType' and IT.SubjectID = fa.fusionattributetypeid) then IT.[ObjectName] 
			else IT.SubjectName
		end + IIF(P.ID is not null, ' [' + P.Name + ']', '')
";

        public static string FusionQueryAttributeRelationshipAllCountsWithZero = @"
select	IT.ID as IntersectTypeID,
		case 
			when (IT.Subject = 'FusionQueryAttributeType' and IT.SubjectID = fa.fusionqueryattributetypeid) then IT.[Object]
			else IT.[Subject]
		end as [Object],
		case 
			when (IT.Subject = 'FusionQueryAttributeType' and IT.SubjectID = fa.fusionqueryattributetypeid) then IT.[ObjectID]
			else IT.[SubjectID]
		end as [ObjectID],		
		I.[Count],
		case 
			when (IT.Subject = 'FusionQueryAttributeType' and IT.SubjectID = fa.fusionqueryattributetypeid) then IT.[ObjectName] 
			else IT.SubjectName
		end + IIF(P.ID is not null, ' [' + P.Name + ']', '') as [Name]
from	[dbo].[fusionQueryattribute] fa	
		inner join IntersectTypeDetail IT on ( 
										(IT.Subject = 'FusionQueryAttributeType' and IT.SubjectID = fa.fusionqueryattributetypeid) OR 
										(IT.Object = 'FusionQueryAttributeType' and IT.ObjectID = fa.fusionqueryattributetypeid) 
									   )
		left join [Predicate] P on P.ID = IT.PredicateID
		cross apply (
					select	count(1) as [Count]
					from	[Intersect] 
					where	IntersectTypeID = IT.ID 
							and (
								(Subject = 'FusionQueryAttribute' and SubjectID = @objId) or 
								(Object = 'FusionQueryAttribute' and ObjectID = @objId)
								)
					) I
where	fa.ID = @objId
order by case 
			when (IT.Subject = 'FusionQueryAttributeType' and IT.SubjectID = fa.fusionqueryattributetypeid) then IT.[ObjectName] 
			else IT.SubjectName
		end + IIF(P.ID is not null, ' [' + P.Name + ']', '')
";

        public static string ObjectRelationshipAllCountsWithZero = @"
select	IT.ID as IntersectTypeID,
		case 
			when (IT.Subject = CO.ObjectType and IT.SubjectID = CO.ObjectTypeID) then IT.[Object]
			else IT.[Subject]
		end as [Object],
		case 
			when (IT.Subject = CO.ObjectType and IT.SubjectID = CO.ObjectTypeID) then IT.[ObjectID]
			else IT.[SubjectID]
		end as [ObjectID],		
		I.[Count],
		case 
			when (IT.Subject = CO.ObjectType and IT.SubjectID = CO.ObjectTypeID) then IT.[ObjectName] 
			else IT.SubjectName
		end + IIF(P.ID is not null, ' [' + P.Name + ']', '') as [Name]
from	cache.[Object] CO	
		inner join IntersectTypeDetail IT on ( 
										(IT.Subject = CO.ObjectType and IT.SubjectID = CO.ObjectTypeID) OR 
										(IT.Object = CO.ObjectType and IT.ObjectID = CO.ObjectTypeID) 
									   )
		left join [Predicate] P on P.ID = IT.PredicateID
		cross apply (
					select	count(1) as [Count]
					from	[Intersect] 
					where	IntersectTypeID = IT.ID AND [Visible] = 1
							and (
								(Subject = @obj and SubjectID = @objId) or 
								(Object = @obj and ObjectID = @objId)
								)
					) I
where	CO.[Object] = @obj and CO.ObjectID = @objId
order by case 
			when (IT.Subject = CO.ObjectType and IT.SubjectID = CO.ObjectTypeID) then IT.[ObjectName] 
			else IT.SubjectName
		end + IIF(P.ID is not null, ' [' + P.Name + ']', '')";

        
        public static string ObjectRelationshipTypeIDs = @"
select	distinct
        I.IntersectTypeID
from	[IntersectDetail] I
where	(I.Subject = @obj and I.SubjectID = @objid and I.ObjectType = @objtype and I.ObjectTypeID = @objtypeid) OR
		(I.Object = @obj and I.ObjectID = @objid and I.SubjectType = @objtype and I.SubjectTypeID = @objtypeid)";

        public static string ObjectRelationships = @"
select	ID,
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

        public static string PolicySettingsItem = @"
select	T.*, PT.Name as 'PolicyTypeClass', R.*
from	PolicyType T
        inner join PolicyTypeClass PT on (T.PolicyTypeClassID = PT.ID)
		cross apply (
					select	case 
								when count(1) > 0 then cast(1 as bit)
								else cast(0 as bit)
							end as AllowAttributes
					from	AttributeTypeRelation
					where	ObjectType = 'PolicyType' and ObjectID = T.ID
					) R
where	T.ID = @id";

        public static string PromotionHistoryList = @"
select	ID,
		DateStarted,
		DateCompleted,
		PromotedTaxonomies,
		PromotedDomainItems,
		PromotedDomains,
		PromotedArtifacts,
		TotalNewPromotions,
		AttributesConsidered,
        NumberOfRules,
        RelationshipsAdded
from    fusion.RuleLog
order by    DateStarted desc";

        
        public static string ResponsibilityList = @"
select distinct  
        r.responsibilityid as [ID],
		r.responsibilitytype as [Type],
		r.responsibleobjectname as [Name],
		r.responsibleobjecturl as [Url],
		d.PrimaryOwnerResourceName as [Owner],
		d.PrimaryOwnerResourceUrl as [OwnerUrl],
		d.ContextItems as [Context]
from    cache.Responsibilities r
		inner join ResponsibilityDetail d on d.ResponsibilityID = r.ResponsibilityID
where   r.objectid = @ObjectID and r.[object] = @ObjectType";

        public static string SourceRuleList = @"
select	R.SubjectName + ' ' + coalesce(R.PredicateName, 'stores') + ' ' + R.ObjectName as SubjectName,
		R.SubjectID,
		R.SubjectUrl,
		R.SubjectTypeName,
		MS.Description,
		(
		select substring(
						(
						SELECT  ', ' + D.TextPath AS 'data()' 
						from	MapSequenceContext MSC
								inner join cache.ObjectDetails D on MSC.Object = D.Object and MSC.ObjectID = D.ObjectID and MSC.MapSequenceID = MS.ID
						FOR		XML PATH('')
						), 2, 2500)
		) as Contexts,
		MS.Sequence
from	MapItem MI
		inner join MapSequence MS on MS.MapItemID = MI.ID
		inner join IntersectDetail R on R.ID = MI.SourceIntersectID
where	(
			@focal + cast(@focalID as varchar) <> @obj + cast(@objID as varchar) and 
			MI.TargetIntersectID in 
			( 
				select	ID 
				from	[Intersect] 
				where	( 
						(Subject = @focal and SubjectID = @focalID and Object = @obj and ObjectID = @objID) OR 
						(Subject = @obj and SubjectID = @objID and Object = @focal and ObjectID = @focalID) 
						) 
			)
		) OR
		(
			@focal + cast(@focalID as varchar) = @obj + cast(@objID as varchar) and 
			MI.TargetIntersectID in 
			( 
				select	ID 
				from	[Intersect] 
				where	( 
						(Subject = @focal and SubjectID = @focalID) OR 
						(Object = @focal and ObjectID = @focalID) 
						) 
			)
		)
order by MS.Sequence";

        public static string ScoreTypeMetricDetailList = @"
select	S.*,
		D.Name as ObjectName
from	ScoreTypeMetric S
		inner join cache.ObjectDetails D on D.Object = S.Object and D.ObjectID = S.ObjectID 
where   S.ScoreTypeID = @id and S.Deleted = 0
order by D.Name, S.Name";


        public static string SynonymTypes = @"
        declare	@ot varchar(50),
		        @otid int

        select	@ot = ObjectType,
		        @otid = ObjectTypeID
        from	cache.Object 
        where	Object = @type 
                and ObjectID = @id


        select 
            d.Name, d.Object + '|' + cast(d.ObjectID as varchar(50)) as [Value], d.Object, d.ObjectID from intersecttype IT        
        inner join cache.ObjectDetails d on
	        case when IT.Subject = @ot then
		        IT.Object
	        else
		        IT.Subject
	        end = d.Object 
	        and
	        case when IT.SubjectID = @otid then
		        IT.ObjectID
	        else
		        IT.SubjectID
	        end = d.ObjectID
        where 
	        ((IT.Subject = @ot and IT.SubjectID = @otid) OR (IT.Object = @ot and IT.ObjectID = @otid)) and IT.predicateid = @predicateId";

        public static string SynonymOptions = @"
declare	@ot varchar(50),
		@otid int

select	@ot = ObjectType,
		@otid = ObjectTypeID
from	cache.Object 
where	Object = @object 
        and ObjectID = @objectId

select		D.Object + '|' + cast(D.ObjectID as varchar) + '|' + cast(P.ID as varchar) as ID,
			D.ObjectTypeName + ' :: ' + D.TextPath as Name,
            O.TargetingSubject
from cache.ObjectDetails d		
{0}
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
						) O on O.Object = D.ObjectType and O.ObjectID = D.ObjectTypeID and D.ObjectTypeName is not null and D.Object + '|' + cast(D.ObjectID as varchar) <> @object + '|' + cast(@objectId as varchar)
            inner join [Predicate] P on P.ID = @predicateId
			where (@query = '') or (@query != '' and d.textpath like '%'+@query+'%')
order by	D.ObjectTypeName,
			D.TextPath
";

        public static string SynonymsByObjectList = @"
select	I.ID as IntersectID,
		D.Object,
		D.ObjectID,
		TT.Name as SubjectArea,
        TT.ID as TaxonomyTypeID,
		AP.ID as ParentID,
        dbo.GenerateObjectUrl('Artifact', AP.ArtifactTypeID, AP.ID) as ParentUrl,
        AP.Name as ParentName,
		D.Name,
        D.ObjectTypeName,
		D.Description,
		D.Url        
        ,null as 'CustomID'
from	[Intersect] I
		inner join IntersectType T on T.ID = I.IntersectTypeID  and T.PredicateID = @predicateId		
        inner join cache.ObjectDetails D on D.Object = case 
															when I.Subject = @type and I.SubjectID = @id then I.Object 
															else I.Subject
														end
											and D.ObjectID = case 
																when I.Subject = @type and I.SubjectID = @id then I.ObjectID 
																else I.SubjectID 
															 end
        left join Artifact A on A.ID = D.ObjectID
        left join Artifact AP on AP.ID = A.ParentID
		left join TaxonomyType TT on TT.ID = A.TaxonomyTypeID
where	(I.Subject = @type and I.SubjectID = @id) or (I.Object = @type and I.ObjectID = @id) and I.visible = 1
union
select 
	null as IntersectID
	,null as 'Object'
	,-1 as 'ObjectID'
	,null as SubjectArea
	,null as TaxonomyTypeID
	,null as ParentID
	,null as ParentUrl
	,null as ParentName
	,S.Name
	,'Custom' as ObjectTypeName
	,null as Description
	,null as Url	
    ,S.ID as 'CustomID'
from 
	[dbo].[nym] s	
where s.[object] = @type and s.[objectID] = @id and s.PredicateID = @predicateId and s.Visible = 1
";

        public static string TaxonomySettingsItem = @"
select	
	T.ID,
	T.Name,
	T.Description,
	T.MaximumDepth,
	T.TaxonomyTypeClassID,
	T.UpdatedOn,
	T.UpdatedBy,
	A.AllowAttributes,
	S.AllowSynonyms,
	Tc.Name as ClassificationName,
    (select cast(count(1) as bit) from report r where r.ObjectType = 'TaxonomyType' and r.ObjectID = @id and r.ReportType != 'legacy') as HasDashboards	
from	TaxonomyType T
		inner join taxonomytypeclass Tc on (T.TaxonomyTypeClassID = Tc.ID)		
		cross apply (
					select	case 
								when count(1) > 0 then cast(1 as bit)
								else cast(0 as bit)
							end as AllowAttributes
					from	AttributeTypeRelation
					where	ObjectType = 'TaxonomyType' and ObjectID = T.ID
					) A
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
where	T.ID = @id";

        public static string InvalidTextPaths = @"
            select 
	            od.[object],
	            od.[objectid],
	            od.[name],
	            od.[textpath],
	            utility.GetBreadcrumbStringWrapper(od.[object], od.[objectid], '/') as 'correctTextpath'
            from
	            cache.objectdetails od
            where 
	            od.[textpath] != utility.GetBreadcrumbStringWrapper(od.[object], od.[objectid], '/')
		            and
	            od.[object] in ('Artifact','Taxonomy', 'Policy')
";

        public static string SimilarItems = @"
                    select top 10
						objectid,
	                    Name,
	                    Url, 
	                    IconForeColor, 
	                    IconBackColor, 
	                    [Description],
	                    objecttypeid,
						case when Name like @query + '%' then
							0
						else
							1
						end as rnk
                    from 
	                    cache.objectdetails
                    where 
	                    [object] = @type
	                    and (@typeID is null or objectTypeID = @typeID)
	                    and Name like '%' + @query + '%'
					order by rnk
            ";

        public static string ImpactAnalysisDiagram = @"
declare @links table ([from] varchar(250), [to] varchar(250), [text] varchar(50), predicateid int, intersectid int)
declare @nodes table ([key] varchar(250), obj varchar(50), [objid] int, typeName nvarchar(250), typeNamePlural nvarchar(250), [type] nvarchar(250), typeId int, name nvarchar(500), back varchar(7), fore varchar(7), [predicate] nvarchar(250), predicateid int, intersectid int, isLeaf bit)

	insert into @nodes
		select	D.Object + cast(D.ObjectID as varchar),
				D.Object,
				D.ObjectID,
				D.ObjectTypeName,
				D.ObjectTypeName,
				D.ObjectType,
				D.ObjectTypeID,
				D.TextPath,
				D.IconBackColor,
				D.IconForeColor,
				case 
					when I.Subject = @type and I.SubjectID = @id then coalesce(P.Name, 'uses')
					else coalesce(P.Inverse, 'used in')
				end as [Predicate],
				P.ID as PredicateID,
				I.ID,
				1 as isLeaf
		from	[Intersect] I
				inner join cache.ObjectDetails D on 
									D.Object = case 
												when I.Subject = @type and I.SubjectID = @id then I.Object
												else I.Subject
											   end 
									and
									D.ObjectID = case 
												when I.Subject = @type and I.SubjectID = @id then I.ObjectID
												else I.SubjectID
											   end
				inner join IntersectType T on T.ID = I.IntersectTypeID
				left join [Predicate] P on P.ID = T.PredicateID
		where	( 
					(I.Subject = @type and I.SubjectID = @id) OR 
					(I.Object = @type and I.ObjectID = @id)  
				)
                and D.[Object] != 'Map';
	
	insert into @links
		select	@type + cast(@id as varchar),
				[key],
				[predicate],
				[predicateid],
				[intersectid]
		from	@nodes


	insert into @nodes
		select	D.Object + cast(D.ObjectID as varchar),
				D.Object,
				D.ObjectID,
				D.ObjectTypeName,
				D.ObjectTypeName,
				D.ObjectType,
				D.ObjectTypeID,
				D.TextPath,
				D.IconBackColor,
				D.IconForeColor,
				null,
				null,
				null,
				1 as isLeaf
		from	cache.ObjectDetails D
		where	Object = @type and ObjectID = @id

		--check for downstream relationships to pre-emptively show/hide expander button
		update n
		set isLeaf = 0
		from @nodes n
		inner join [Intersect] I on ((I.[Subject] = n.[obj] AND I.[SubjectID] = n.[objid])
			OR (I.[Object] = n.[obj] AND I.[ObjectID] = n.[objid])) 
			AND I.ID <> n.intersectid;

		update @nodes
		set isLeaf = 0
		where intersectid is null;

	select	(
			select * from @links for json path			
			) as 'links',
			(
			select * from @nodes for json path			
			) as 'nodes'
	for json path, WITHOUT_ARRAY_WRAPPER";

        public static string ImpactAnalysisDiagramFusion = @"
    declare @links table ([from] varchar(250), [to] varchar(250), [text] varchar(50), predicateid int, intersectid int)
    declare @nodes table ([key] varchar(250), obj varchar(50), [objid] int, typeName nvarchar(250), typeNamePlural nvarchar(250), [type] nvarchar(250), typeId int, name nvarchar(500), back varchar(7), fore varchar(7), [predicate] nvarchar(250), predicateid int, intersectid int)

    declare @typeName varchar(50), @typeId int;

    select @typeName=ObjectType, @typeId=ObjectTypeID from cache.ObjectDetails
    where object = @type and objectid = @id;

    insert into @nodes
    select D.Object + cast(D.ObjectID as varchar),
				    D.Object,
				    D.ObjectID,
				    D.ObjectTypeName,
				    D.ObjectTypeName,
				    D.ObjectType,
				    D.ObjectTypeID,
				    D.TextPath,
				    D.IconBackColor,
				    D.IconForeColor,
				    case 
					    when I.Subject = @type and I.SubjectID = @id then coalesce(P.Name, 'uses')
					    else coalesce(P.Inverse, 'used in')
				    end as [Predicate],
				    P.ID as PredicateID,
				    I.ID
    from [Intersect] I
    inner join IntersectType T on I.IntersectTypeID = T.ID AND
	    ((T.Subject = @typeName and T.SubjectID = @typeId and T.Object = 'FusionAttributeType') OR
	     (T.Object = @typeName and T.ObjectID = @typeId and T.Subject ='FusionAttributeTYpe'))
    inner join cache.ObjectDetails D on D.Object = case 
												    when I.Subject = @type and I.SubjectID = @id then I.Object
												    else I.Subject
											       end 
									    and
									    D.ObjectID = case 
												    when I.Subject = @type and I.SubjectID = @id then I.ObjectID
												    else I.SubjectID
											       end
    left join Predicate P on P.ID = T.PredicateID
    where
    (I.Subject = @type AND I.SubjectID = @id) OR (I.Object = @type AND I.ObjectID = @id);

    insert into @links
	    select	@type + cast(@id as varchar),
			    [key],
			    [predicate],
			    [predicateid],
			    [intersectid]
	    from	@nodes;

    select	(
		    select * from @links for json path			
		    ) as 'links',
		    (
		    select * from @nodes for json path			
		    ) as 'nodes'
    for json path, WITHOUT_ARRAY_WRAPPER;
";

        public static string FusionRuleStepPromotionHistory = @"select
	P.ID,
	P.AttributeID,
    P.AttributeType,
	FA.Name as AttributeName,
	P.ObjectType as [Object],
	P.ObjectID,
	D.Name as ObjectName,
	D.NgUrl as ObjectUrl,
	P.CreatedOn,
	P.UpdatedOn
from fusion.RulePromotion P
join cache.ObjectDetails D on D.Object = P.ObjectType and D.ObjectID = P.ObjectID
join FusionAttribute FA ON P.AttributeID = FA.ID and P.AttributeType = 'FusionAttribute'
where P.RuleStepID = @id
union all
select
	P.ID,
	P.AttributeID,
    P.AttributeType,
	FA.DisplayValue as AttributeName,
	P.ObjectType as [Object],
	P.ObjectID,
	D.Name as ObjectName,
	D.NgUrl as ObjectUrl,
	P.CreatedOn,
	P.UpdatedOn
from fusion.RulePromotion P
join cache.ObjectDetails D on D.Object = P.ObjectType and D.ObjectID = P.ObjectID
join FusionQueryAttribute FA ON P.AttributeID = FA.ID and P.AttributeType = 'FusionQueryAttribute'
where P.RuleStepID = @id;";

        public static string MapItems = @"
select	MI.ID as MapItemID,
				
		SI.ObjectTypeName as SourceType,
		SI.ObjectName as SourceName,
		SI.Object as Source,
		SI.ObjectID as SourceID,

		SF.Name as SourceFusion,
		SFA.TextPath as SourceFusionAttribute,
		SFT.TextPath as SourceFusionAttributeType,

		TI.ObjectTypeName as TargetType,
		TI.ObjectName as TargetName,
		TI.Object as Target,
		TI.ObjectID as TargetID,

		TF.Name as TargetFusion,
		TFA.TextPath as TargetFusionAttribute,
		TFT.TextPath as TargetFusionAttributeType

from	MapItem MI
		inner join IntersectDetail SI on SI.ID = MI.SourceIntersectID
		inner join IntersectDetail TI ON TI.ID = MI.TargetIntersectID
		left join MapRuleItemMapItem J on J.MapItemID = MI.ID
		left join MapRuleItem MRI on MRI.ID = J.MapRuleItemID
		left join FusionAttribute SFA on SFA.ID = MRI.SourceFusionAttributeID
		left join FusionAttributeType SFT on SFT.ID = SFA.FusionAttributeTypeID
		left join Fusion SF on SF.ID = SFA.FusionID
		left join FusionAttribute TFA on TFA.ID = MRI.TargetFusionAttributeID
		left join FusionAttributeType TFT on TFT.ID = TFA.FusionAttributeTypeID
		left join Fusion TF on TF.ID = TFA.FusionID
where 	(SI.Subject = @source and SI.SubjectID = @sourceID)
		AND (TI.Subject = @target and TI.SubjectID = @targetID)";

        public static string WorkflowDiagramNodes = @"
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
			left join (
				select stepid, count(stepid) as RunCount from workflow.itemstep
				group by stepid
			) i on i.stepid = vs.id
            where t.id = @id and vs.[State] = 1 and v.id = coalesce((select top 1 id from workflow.version where typeid = @id and version = @version), (select top 1 id from workflow.version where typeid = @id order by [version] desc))
";

        public static string WorkflowDiagramLinks = @"
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

        public static string WorkflowObjectTypes = @"
            select 'ArtifactType|' + cast(t.id as varchar) as value, t.id, 'ArtifactType' as [type], 'Artifact Type :: ' +  t.Name as [label], count(*) as [count] 
            from artifacttype t
            left join artifact a on a.artifacttypeid = t.id
            group by t.ID, t.Name
            union all
            select 'RuleType|' + cast(t.id as varchar) as value, t.id, 'RuleType' as [type], 'Rule Type :: ' + t.Name as [label], count(*) as [count] 
            from ruletype t
            left join [rule] a on a.ruletypeid = t.id
            group by t.id, t.name
            union all
            select 'PolicyType|' + cast(t.id as varchar) as value, t.id, 'PolicyType' as [type], 'Policy Type :: ' + t.Name as [label], count(*) as [count] 
            from policytype t
            left join [policy] a on a.policytypeid = t.id
            group by t.id, t.name
            union all
            select 'TaxonomyType|' + cast(t.id as varchar) as value, t.id, 'TaxonomyType' as [type], 'Model Type :: ' + t.Name as [label], count(*) as [count] 
            from taxonomytype t
            left join taxonomy a on a.taxonomytypeid = t.id
            group by t.id, t.name
            union all
            select 'IssueType|' + cast(t.id as varchar) as value, t.id, 'IssueType' as [type], 'Action Type :: ' + t.Name as [label], count(*) as [count] 
            from issuetype t
            left join issue a on a.issuetypeid = t.id
            group by t.id, t.name
			union all
            select 'Fusion|' + cast(t.id as varchar) as value, t.id, 'Fusion' as [type], 'Fusion :: ' + t.Name as [label], 1 as [count] 
            from fusion t
            group by t.id, t.name
            union all
            select 'IntersectType|' + cast(t.id as varchar) as value, t.id, 'IntersectType' as [type], 'Relationship :: ' + t.Name as [label], 1 as [count] 
            from intersecttype t
            group by t.id, t.name
            union all
			select 'ShoppingCartType|' + cast(t.id as varchar) as value, t.id, 'ShoppingCartType' as [type], 'Shopping Cart :: ' + t.Name as [label], 1 as [count]
			from shoppingcarttype t
			group by t.id, t.name
";

        public static string WorkflowList = @"
                select t.ID
                    ,t.Name
                    ,t.CreatedOn
					,coalesce(rc.FirstName + ' ' + rc.LastName, '') as CreatedBy
                    ,t.UpdatedOn
					,coalesce(ru.FirstName + ' ' + ru.LastName, '') as UpdatedBy
                    ,e.ChangeType
                    ,coalesce(d.Name, st.Name) as TypeName,
					case when t.PublishedVersionID is not null then
						'Version ' + cast(v.Version as varchar) + ' Published'
					else
						'Unpublished'
					end as Published,
					case when d.[Object] = 'ArtifactType' then
						'Artifact'
					when d.[Object] = 'RuleType' then
						'Rule'
					when d.[Object] = 'PolicyType' then
						'Policy'
					when d.[Object] = 'TaxonomyType' then
						'Model'
					when d.[Object] = 'IssueType' then
						'Action'
                    when d.[Object] = 'IntersectType' then
						'Relationship'
                    when e.[Object] = 'ShoppingCartType' then
                        'Shopping Cart'
					else
						''
					end as [Type] 
                from workflow.type t
                inner join workflow.eventregistration e on e.typeid = t.id
                left join cache.objectdetails d on d.object = e.object and d.objectid= e.objectid 
                left join ShoppingCartType st on st.ID = e.objectid and e.object = 'ShoppingCartType'
				left join workflow.version v on v.id = t.publishedversionid
				left join reporting.Global_Resource rc on rc.ResourceID = t.CreatedBy
				left join reporting.Global_Resource ru on ru.ResourceID = t.UpdatedBy
				where t.State = 1  ";

        public static string ShoppingCartItemList = @"
                select 
	                i.Object, 
	                i.ObjectID, 
	                coalesce(d.[Name],fa.[Name]) as [Name],
	                coalesce(d.ObjectTypeName, fat.[Name], case when i.[Object] = 'ReferenceItemType' then 'Reference List' else null end) as ObjectTypeName  
                from
	                Shoppingcartitem i
                left join cache.ObjectDetails d on d.ObjectID = i.ObjectID and d.[Object] = i.[Object]
                left join fusionattribute fa on fa.ID = i.ObjectID and i.[Object] = 'FusionAttribute'
                left join fusionattributetype fat on fat.id = fa.FusionAttributeTypeID
                where 
	                i.ShoppingCartID = @id";

    }
}