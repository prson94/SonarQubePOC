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

        public static string ArtifactActivitySpecificDateCountList = @"
select  at.name as Name,
	    count(1) as New,
        '/Home/ArtifactActivityOverlay?mode=new&artifactTypeID=' + cast(at.id as varchar) as NewUri							
from    Artifact a
        inner join artifacttype at on a.artifacttypeid = at.id
where   a.createdon > dateadd(day, @d, CURRENT_TIMESTAMP)
group by at.name,at.id order by at.name";

        public static string ArtifactActivityAllDateCountList = @"
select  at.name as Name,
	    count(1) as New,
        '/Home/ArtifactActivityOverlay?mode=new&artifactTypeID=' + cast(at.id as varchar) as NewUri							
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
	select	[ObjectID] as ID, [Name], [ParentID], [Url], [ObjectTypeName], [ObjectTypeID],
			[dbo].GenerateNgObjectUrl(ObjectType, ObjectTypeID, ObjectTypeID) as TypeUrl,
			0 as [Level]
	from	[cache].[ObjectDetails]
	where	[Object] = 'Artifact' and ObjectID = @id
	union all
	select	P.[ObjectID] as ID, P.[Name], P.[ParentID], P.[Url], P.[ObjectTypeName], P.[ObjectTypeID],
			[dbo].GenerateNgObjectUrl(P.ObjectType, P.ObjectTypeID, P.ObjectTypeID) as TypeUrl,
			C.[Level]-1 as [Level]
	from	[cache].[ObjectDetails] P
			inner join h C on P.[Object] = 'Artifact' and P.ObjectID = C.ParentID
	)
select ObjectTypeName as TypeName, TypeUrl, Name, Url from h order by [Level]";

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
							end as AllowSynonyms
					from	(
								select	IT.ID
								from	IntersectType IT
										inner join IntersectTypePredicate ITP on ITP.IntersectTypeID = IT.ID and ITP.PredicateType = 6 -- Synonym
								where	(IT.Subject = 'ArtifactType' and IT.SubjectID = @id) OR (IT.Object = 'ArtifactType' and IT.ObjectID = @id)
							) O
					) S on 1=1
		inner join	(
					select  case when count(1) > 0 then cast(1 as bit) else cast(0 as bit) end  as AllowPredicateHierarchies
					from	utility.RelationshipTypes T
							inner join IntersectTypePredicate TP on TP.IntersectTypeID = T.IntersectTypeID and T.SourceObjectType = 'ArtifactType' and T.SourceObjectID = @id
							inner join Predicate P on P.Type = TP.PredicateType and P.Type in (3)--, 4)
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

        public static string CurrentWorkflowTaskItem = @"
select  W.ID as WorkflowID,
        W.WorkflowType as Workflow,
        W.Data,
        W.DateStarted,
        WR.Activity
from    Workflow W
		inner join WorkflowResource WR on	WR.WorkflowID = W.ID 
			and W.DateCompleted is null
			and WR.ResourceID = @r
            and WR.IsComplete = 0
where   W.ID = @w";

        public static string CurrentUserWorkflowCount = @"
select	W.WorkflowType as Workflow,
        count(1) as [Count]
from    Workflow W
		inner join WorkflowResource WR on	WR.WorkflowID = W.ID 
		    and W.DateCompleted is null
		    and WR.ResourceID = @r
		    and WR.IsComplete = 0
group by	W.WorkflowType";

        public static string CurrentUserWorkflow1TaskItem =
@"select    W.ID as WorkflowID,
		    W.Data.value('(fields/ArtifactTypeID)[1]', 'int') as ID,
			A.Name as Name,
			A.Url as Url,
            W.DateStarted as StartDate,
			W.Data.value('(fields/Name)[1]', 'nvarchar(250)') as ProposedName,
			W.Data.value('(fields/Description)[1]', 'nvarchar(max)') as ProposedDescription,
			W.Data.value('(fields/RequestingResourceID)[1]', 'int') as RequestingResourceID,
			R.FirstName + ' ' + R.LastName as RequestingResourceName,
			W.Data.value('(fields/TaxonomyTypeID)[1]', 'int') as TaxonomyTypeID,
			TT.Name as TaxonomyTypeName,
		    WR.Activity
from	    Workflow W
		    inner join cache.ObjectDetails A on A.[Object] = 'ArtifactType' and A.ObjectID = W.Data.value('(fields/ArtifactTypeID)[1]', 'int')
			inner join reporting.Global_Resource R on R.ResourceID = W.Data.value('(fields/RequestingResourceID)[1]', 'int')
			inner join TaxonomyType TT on TT.ID = W.Data.value('(fields/TaxonomyTypeID)[1]', 'int')
			inner join WorkflowResource WR on	WR.WorkflowID = W.ID 
											    and W.DateCompleted is null
											    and WR.ResourceID = @r
												and W.WorkflowType = 1
                                                and WR.IsComplete = 0 
{0} 
order by    A.Name, W.Data.value('(fields/Name)[1]', 'nvarchar(250)')";

        public static string CurrentUserWorkflow2TaskItem = @"
select  W.ID as WorkflowID,
		W.Data.value('(fields/ArtifactID)[1]', 'int') as ID,
		A.Name as Name,
		A.Url as Url,
        A.ObjectTypeName as TypeName,
		W.Data.value('(fields/StartDate)[1]', 'datetime') as StartDate,
		W.Data.value('(fields/DueDate)[1]', 'datetime') as DueDate,
		WR.Activity
from    Workflow W
		inner join cache.ObjectDetails A on A.[Object] = 'Artifact' and A.ObjectID = W.Data.value('(fields/ArtifactID)[1]', 'int')
		inner join WorkflowResource WR on	WR.WorkflowID = W.ID 
			and W.DateCompleted is null
			and WR.ResourceID = @r
			and W.WorkflowType = 2
            and WR.IsComplete = 0 
        {0} 
order by    A.ObjectTypeName, A.Name";

        public static string CurrentUserWorkflow3TaskAllUsersItem = @"
select		W.ID as WorkflowID,
		    C.Body as Issue,
			W.DateStarted,
			W.DateCompleted        
			,A.Name
			,A.ObjectType
from	    Workflow W
		    inner join Comment C on C.ID = W.Data.value('(fields/CommentID)[1]', 'int')			
			inner join CommentRelation CR on CR.CommentID = C.ID and CR.ObjectType not in ('Resource', 'Group')
			left outer join cache.ObjectDetails A on A.[Object] = CR.ObjectType and A.ObjectID = CR.ObjectID
            where  W.WorkflowType = 3
order by    W.DateStarted desc
";

        public static string CurrentUserWorkflow3TaskItem = @"
select		W.ID as WorkflowID,
		    C.Body as Issue,
			R.ResourceID,
			R.FirstName + ' ' + R.LastName as ResourceName,
			dbo.GenerateObjectUrl('Resource', 0, R.ResourceID) as ResourceUrl,
			W.DateStarted,
		    WR.Activity
from	    Workflow W
		    inner join Comment C on C.ID = W.Data.value('(fields/CommentID)[1]', 'int')
			inner join reporting.Global_Resource R on R.ResourceID = W.Data.value('(fields/ResourceID)[1]', 'int')
			inner join WorkflowResource WR on	WR.WorkflowID = W.ID 
				and W.DateCompleted is null
				and WR.ResourceID = @r
				and W.WorkflowType = 3
                and WR.IsComplete = 0 
            {0} 
order by    W.DateStarted desc";


        public static string CurrentUserWorkflow3SpecificObjectTaskItem = @"
select		W.ID as WorkflowID,
		    C.Body as Issue,
			R.ResourceID,
			R.FirstName + ' ' + R.LastName as ResourceName,
			dbo.GenerateObjectUrl('Resource', 0, R.ResourceID) as ResourceUrl,
			W.DateStarted,
		    WR.Activity            
from	    Workflow W
		    inner join Comment C on C.ID = W.Data.value('(fields/CommentID)[1]', 'int')
			inner join reporting.Global_Resource R on R.ResourceID = W.Data.value('(fields/ResourceID)[1]', 'int')
            inner join CommentRelation CR on CR.CommentID = C.ID and CR.ObjectType not in ('Resource', 'Group')
			left outer join WorkflowResource WR on	WR.WorkflowID = W.ID 											    
                and WR.ResourceID = @r												
                and WR.IsComplete = 0 												                        
            where CR.ObjectType = @type and CR.ObjectId = @id and W.DateCompleted is null and W.WorkflowType = 3
order by    W.DateStarted desc";

        public static string CurrentUserWorkflow4TaskItem = @"
select		W.ID as WorkflowID,
		    C.Body as Issue,
			R.ResourceID,
			R.FirstName + ' ' + R.LastName as ResourceName,
			dbo.GenerateObjectUrl('Resource', 0, R.ResourceID) as ResourceUrl,
			W.DateStarted,
		    WR.Activity,
            A.Name as Name,
			A.Url as Url,
            A.ObjectTypeName as TypeName,
            A.ObjectID as ArtifactID
from	    Workflow W
		    inner join Comment C on C.ID = W.Data.value('(fields/CommentID)[1]', 'int')
			inner join reporting.Global_Resource R on R.ResourceID = W.Data.value('(fields/RequestingResourceID)[1]', 'int')
            left outer join cache.ObjectDetails A on A.[Object] = 'Artifact' and A.ObjectID = W.Data.value('(fields/ArtifactID)[1]', 'int')
			inner join WorkflowResource WR on	WR.WorkflowID = W.ID 
											    and W.DateCompleted is null
											    and WR.ResourceID = @r
												and W.WorkflowType = 4
                                                and WR.IsComplete = 0 
{0} 
order by    W.DateStarted desc";

        public static string DomainSettingsItem = @"
select	*
from	(
		select	case 
					when count(1) > 0 then cast(1 as bit)
					else cast(0 as bit)
				end as AllowAttributes
		from	AttributeTypeRelation
		where	ObjectType = 'DomainType' and ObjectID = @id
		) A";

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
							IntersectTypeID as ID
					from	[utility].[RelationshipTypes]
					where	SourceObjectType = @type and SourceObjectID = @id
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

        public static string FusionOwnershipRuleList = @"
select	I.ID,
        I.FusionAttributeOwnerRuleID,
        I.FusionAttributeID,
        case 
			when F.FusionAttributeTypeID = FT.ID then F.TextPath
			else coalesce(FT.Name + ' attributes under ' + F.TextPath, 'All ' + FT.Name + ' attributes') 
		end as FusionAttributeName
from	FusionAttributeOwnerRuleItem I
		inner join FusionAttributeOwnerRule R on R.ID = I.FusionAttributeOwnerRuleID
		inner join FusionAttributeType FT on FT.ID = R.ObjectID
		left join FusionAttribute F on F.ID = I.FusionAttributeID
where   I.FusionAttributeOwnerRuleID = @id";

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
                                select  RI.FusionAttributeID
                                from    FusionAttributePromotionRuleItem RI
                                        inner join FusionAttributePromotionRule R on R.ID = RI.FusionAttributePromotionRuleID and R.ID = @ruleID and R.FusionID = @fusionID and RI.FusionAttributeID is not null
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
                                from    FusionAttributePromotionRuleItem RI
                                        inner join FusionAttributePromotionRule R on R.ID = RI.FusionAttributePromotionRuleID and R.ID = @ruleID and R.FusionID = @fusionID and RI.FusionAttributeID is not null
                                )
        order by	Name
	end";

        public static string FusionRuleItemList = @"
select	I.ID,
        I.RuleID,
        I.FusionAttributeID,
        case 
			when F.FusionAttributeTypeID = FT.ID then F.TextPath
			else coalesce(FT.Name + ' attributes under ' + F.TextPath, 'All ' + FT.Name + ' attributes') 
		end as FusionAttributeName
from	[fusion].[RuleItem] I
		inner join [fusion].[Rule] R on R.ID = I.RuleID
		inner join FusionAttributeType FT on FT.ID = R.ObjectID
        left join FusionAttribute F on F.ID = I.FusionAttributeID
where   I.RuleID = @id
        ";

        public static string FusionPromotionRuleList = @"
select	I.ID,
        I.FusionAttributePromotionRuleID,
        I.FusionAttributeID,
        case 
			when F.FusionAttributeTypeID = FT.ID then F.TextPath
			else coalesce(FT.Name + ' attributes under ' + F.TextPath, 'All ' + FT.Name + ' attributes') 
		end as FusionAttributeName
from	FusionAttributePromotionRuleItem I
		inner join FusionAttributePromotionRule R on R.ID = I.FusionAttributePromotionRuleID
		inner join FusionAttributeType FT on FT.ID = R.ObjectID
        left join FusionAttribute F on F.ID = I.FusionAttributeID
where   I.FusionAttributePromotionRuleID = @id";

        public static string FusionPromotionRuleMappingList = @"
select	I.ID,
        I.FusionAttributePromotionRuleID,
        I.SourceFieldTypeID,
        coalesce(I.SourceFieldName, SF.FriendlyName + ' (' + SF.Name + ')') as SourceFieldName,
        I.TargetFieldTypeID,
        coalesce(I.TargetFieldName, TF.FriendlyName + ' (' + TF.Name + ')') as TargetFieldName
from	FusionAttributePromotionRuleMapping I
		left join FieldType SF on SF.ID = I.SourceFieldTypeID
		left join FieldType TF on TF.ID = I.TargetFieldTypeID
where   I.FusionAttributePromotionRuleID = @id";

        public static string FusionRuleMappingList = @"
select	I.ID,
        RS.RuleID,
        I.SourceFieldTypeID,
        coalesce(I.SourceFieldName, SF.FriendlyName + ' (' + SF.Name + ')', 'Constant: ' + I.ConstantValue) as SourceFieldName,
        I.TargetFieldTypeID,
        coalesce(I.TargetFieldName, TF.FriendlyName + ' (' + TF.Name + ')') as TargetFieldName
from	[fusion].[RuleStepMapping] I
        inner join [fusion].[RuleStep] RS on (I.RuleStepID = RS.ID)
		left join FieldType SF on SF.ID = I.SourceFieldTypeID
		left join FieldType TF on TF.ID = I.TargetFieldTypeID
where   I.RuleStepID = @id";

        public static string FusionStatisticsItem = @"select
	(select count(1) from fusion.agenterror where [date] > Dateadd(Day, -7, CURRENT_TIMESTAMP )) as AgentErrors,
	(select count(1) from fusion.execution where datestarted > Dateadd(Day, -7, CURRENT_TIMESTAMP )) as AgentExecutions,
	(select count(1) from fusionstatuslog where success = 1 and datestarted > Dateadd(Day, -7, CURRENT_TIMESTAMP )) as FusionExecutions,
	(select count(1) from fusion.error where [date] > Dateadd(Day, -7, CURRENT_TIMESTAMP )) as FusionErrors,
	(select count(1) from fusionattributepromotionlogsummary where datestarted > Dateadd(Day, -7, CURRENT_TIMESTAMP )) as NumberOfPromotions";

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
                    from    IntersectNode N 
                    where   ObjectType = 'Taxonomy' and ObjectID = h.ID
                    ) R";

        public static string LineageSearchQuery = @"
select  top 50
        objectid as id, 
        c.textpath as name, 
        iconbackcolor as backColor, 
        iconforecolor as foreColor, 
        c.objecttypename as typeName, 
        c.url, 
        c.object,
        c.objecttype,
        c.objecttypeid 
from    cache.objectdetails c 
where c.object = @type and c.objecttypeid = @id and lower(c.name) like lower(@search)";

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

        public static string ObjectRelationships = @"
select	ID,
        IntersectTypeID,
        Object,
		ObjectID,
		ObjectName as Name,
        ObjectUrl as Url,
		ObjectType as Type,
		ObjectTypeID as TypeID,
		ObjectTypeName as TypeName,
        ObjectIconBackColor as IconBackColor,
		ObjectIconForeColor as IconForeColor,
		ObjectIconText as IconText,
        Classification
from	IntersectDetail
where	Subject = @type and SubjectID = @id
union
select	ID,
        IntersectTypeID,
        Subject as Object,
		SubjectID as ObjectID,
		SubjectName as Name,
        SubjectUrl as Url,
		SubjectType as Type,
		SubjectTypeID as TypeID,
		SubjectTypeName as TypeName,
		SubjectIconBackColor as IconBackColor,
		SubjectIconForeColor as IconForeColor,
		SubjectIconText as IconText,
        Classification
from	IntersectDetail
where	Object = @type and ObjectID = @id
";

        public static string PolicySettingsItem = @"
select	* 
from	PolicyType T
		cross apply (
					select	case 
								when count(1) > 0 then cast(1 as bit)
								else cast(0 as bit)
							end as AllowAttributes
					from	AttributeTypeRelation
					where	ObjectType = 'PolicyType' and ObjectID = T.ID
					) R
where	T.ID = @id";

        public static string PredicateInfoByAllocationList = @"
select	p.id, p.name, p.type 
from	predicate p
		join intersecttypepredicate t on t.predicatetype = p.[type] 
									and t.intersecttypeid in (
															select	t.intersecttypeid 
															from	intersectmap m
																	join intersectnode n on n.id = subjectintersectnodeid
																	join intersecttypenode t on t.id = n.intersecttypenodeid
															where	m.id = @id
															union all
															select	t.intersecttypeid 
															from	intersectmap m
																	join intersectnode n on n.id = objectintersectnodeid
																	join intersecttypenode t on t.id = n.intersecttypenodeid
															where	m.id = @id
															)
where	p.type = (select type from intersectmap where id = @id)";

        public static string PredicateInfoByTypeList = @"
select id, name from predicate where type = @type";

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

        public static string RelationshipAttributesFieldList = @"
select	atr.AttributeTypeID as attributeID,
		at.Name as label,
		atr.AllowMultipleEntries as allowMultiple,
		at.description,
		c.*,
		case 
			when ch.fieldCount > 0 then cast(1 as bit)
			else cast(0 as bit)
		end as isComplex
from	AttributeTypeRelation atr
		inner join AttributeType at on at.ID = atr.AttributeTypeID and atr.ObjectType = 'IntersectType' and atr.ObjectID = @intersectTypeID
		cross apply (select count(1) as fieldCount from FieldType where [Object] = 'AttributeType' and ObjectID = atr.AttributeTypeID) c
		cross apply (select count(1) as fieldCount from AttributeType where ParentID = atr.AttributeTypeID) ch
order by	at.Name";

        public static string RelationshipTypeList = @"
select  R.ID,
		R.Subject,
		R.SubjectID,
		SD.TextPath as SubjectName,
		R.Object,
		R.ObjectID,
		TD.TextPath as ObjectName,
        R.PredicateType
from	RelationType R
		left join cache.ObjectDetails SD on SD.[Object] = R.Subject and SD.ObjectID = R.SubjectID
		left join cache.ObjectDetails TD on TD.[Object] = R.Object and TD.ObjectID = R.ObjectID
--where R.IsSystem = 0
order by    SD.Name,
			TD.Name";

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
select	J.IntersectMapID,
		J.SourceRuleID,
		S.Name,
		R.SourceObject,
		R.SourceObjectID,
		R.SourceObjectName,
		R.SourceTypeName,
		J.Description,
		(
		select substring(
						(
						SELECT  ', ' + D.TextPath AS 'data()' 
						from	SourceRuleContext JI
								inner join cache.ObjectDetails D on JI.Object = D.Object and JI.ObjectID = D.ObjectID and JI.SourceRuleID = J.ID
						FOR		XML PATH('')
						), 2, 2500)
		) as RuleContexts,
		(
		select substring(
						(
						SELECT  ', ' + D.TextPath AS 'data()' 
						from	IntersectMapSourceRuleContext JI
								inner join cache.ObjectDetails D on JI.Object = D.Object and JI.ObjectID = D.ObjectID and JI.IntersectMapSourceRuleID = J.ID
						FOR		XML PATH('')
						), 2, 2500)
		) as ItemContexts,
		J.SortOrder
from	IntersectMapSourceRule J
		inner join SourceRule S on S.ID = J.SourceRuleID and S.AppliesToObject = @focal and S.AppliesToObjectID = @focalID 
		inner join IntersectMap M on J.IntersectMapID = M.ID
		inner join cache.Relationships R on R.SourceIntersectNodeID = M.SubjectIntersectNodeID and R.TargetObject = @obj and R.TargetObjectID = @objID";

        public static string StatisticTypeDetailList = @"
select	S.*,
		D.Name as ObjectName
from	StatisticType S
		inner join cache.ObjectDetails D on D.Object = S.Object and D.ObjectID = S.ObjectID 
order by D.Name, S.Name";

        public static string SynonymOptions = @"
declare	@ot varchar(50),
		@otid int

select	@ot = ObjectType,
		@otid = ObjectTypeID
from	cache.Object 
where	Object = @type 
        and ObjectID = @id

select		D.Object + '|' + cast(D.ObjectID as varchar) + '|' + cast(P.ID as varchar) as ID,
			D.ObjectTypeName + ' :: ' + D.TextPath as Name,
            O.TargetingSubject
from		cache.ObjectDetails D
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
								inner join IntersectTypePredicate ITP on ITP.IntersectTypeID = IT.ID 
																		 and ITP.PredicateType = 6
																		 and (
																				(IT.Subject = @ot and IT.SubjectID = @otid) OR
																				(IT.Object = @ot and IT.ObjectID = @otid)
																			 )
						) O on O.Object = D.ObjectType and O.ObjectID = D.ObjectTypeID and D.ObjectTypeName is not null and D.Object + '|' + cast(D.ObjectID as varchar) <> @type + '|' + cast(@id as varchar)
            inner join [Predicate] P on P.Type = 6
order by	D.ObjectTypeName,
			D.TextPath";

        public static string SynonymsByObjectList = @"
select	I.ID as IntersectID,
		IM.ID as IntersectMapID,
		D.Object,
		D.ObjectID,
		D.TextPath as Name,
        D.ObjectTypeName,
		D.Description,
		D.Url
from	[Intersect] I
		inner join IntersectNode SN on SN.IntersectID = I.ID and SN.ObjectType = @type and SN.ObjectID = @id
		inner join IntersectNode TN on TN.IntersectID = I.ID and TN.ID <> SN.ID 
		inner join IntersectMap IM on 
								    (
										( IM.SubjectIntersectNodeID = SN.ID and IM.ObjectIntersectNodeID = TN.ID )
										OR ( IM.SubjectIntersectNodeID = TN.ID and IM.ObjectIntersectNodeID = SN.ID )
									)
                                    and IM.Type = 6
		inner join cache.ObjectDetails D on D.Object = case 
															when I.Subject = @type and I.SubjectID = @id then I.Object 
															else I.Subject
														end
											and D.ObjectID = case 
																when I.Subject = @type and I.SubjectID = @id then I.ObjectID 
																else I.SubjectID 
															 end";

        public static string TaxonomySettingsItem = @"
select	*
from	TaxonomyType T
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
										inner join IntersectTypePredicate ITP on ITP.IntersectTypeID = IT.ID and ITP.PredicateType = 6 -- Synonym
								where	(IT.Subject = 'TaxonomyType' and IT.SubjectID = @id) OR (IT.Object = 'TaxonomyType' and IT.ObjectID = @id)
							) O
					) S
where	T.ID = @id";
    }
}