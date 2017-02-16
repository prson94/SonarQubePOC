delete from [cache].[object] where [object] = 'FusionAttribute';
go

drop table [dbo].[SiteNavOrder]
go

CREATE NONCLUSTERED INDEX [IX_FusionAttribute_FusionID_Deleted_ParentID]
    ON [dbo].[FusionAttribute]([FusionID] ASC, [Deleted] ASC, [ParentID] ASC);
GO

DROP INDEX [IX_FusionID_ParentID] on Fusion
GO

ALTER TABLE FusionStatusLog ADD [FullRefresh]     BIT              CONSTRAINT [DF_FusionStatusLog_FullRefresh] DEFAULT ((0)) NOT NULL
GO

alter table MapItem add [Owner]             VARCHAR (100) NULL
go

alter table MapRuleItem add [Owner]             VARCHAR (100) NULL
go

alter table MapRuleItemMapItem add [Owner]             VARCHAR (100) NULL
go

CREATE FUNCTION dbo.GetWorkflowArtifactID(@Data XML)
RETURNS INT
WITH SCHEMABINDING
AS BEGIN
  DECLARE @ArtifactID INT

  SELECT  
    @ArtifactID = @Data.value('(fields/ArtifactID/text())[1]', 'int')

  RETURN @ArtifactID
END
GO

CREATE FUNCTION dbo.GetWorkflowStartDate(@Data XML)
RETURNS varchar(33) 
WITH SCHEMABINDING
AS BEGIN
  DECLARE @StartDate varchar(33)

  SELECT  
    @StartDate = @Data.value('(fields/StartDate/text())[1]', 'varchar(33)')

  RETURN @StartDate
END
GO

alter table Workflow add [ArtifactID] AS ([dbo].[GetWorkflowArtifactID]([Data])) PERSISTED
GO

CREATE XML INDEX [IXXML_Workflow_Data_Property]
    ON [dbo].[Workflow]([Data])
    USING XML INDEX [IXXML_Workflow_Data] FOR PROPERTY
    WITH (PAD_INDEX = OFF);
GO

CREATE PRIMARY XML INDEX [IXXML_WorkflowTypeRelation_Fields]
    ON [dbo].[WorkflowTypeRelation]([Fields])
    WITH (PAD_INDEX = OFF);
GO

CREATE XML INDEX [IXXML_WorkflowTypeRelation_Fields_Property]
    ON [dbo].[WorkflowTypeRelation]([Fields])
    USING XML INDEX [IXXML_WorkflowTypeRelation_Fields] FOR PROPERTY
    WITH (PAD_INDEX = OFF);
GO

CREATE XML INDEX [IXXML_WorkflowTypeRelation_Secondary_PATH]
    ON [dbo].[WorkflowTypeRelation]([Fields])
    USING XML INDEX [IXXML_WorkflowTypeRelation_Fields] FOR PATH
    WITH (PAD_INDEX = OFF);
GO

CREATE XML INDEX [IXXML_WorkflowTypeRelation_Secondary_VALUE]
    ON [dbo].[WorkflowTypeRelation]([Fields])
    USING XML INDEX [IXXML_WorkflowTypeRelation_Fields] FOR VALUE
    WITH (PAD_INDEX = OFF);
GO

alter view [cache].[ObjectDetails]
as
	select	D.[Object],
			D.[ObjectID],
			coalesce(O1.Name, O2.Name, O5.Name, O6.Name, O7.Name, O8.Name, O9.Name, O10.Name, O11.Name, O12.Name, O13.Name, case when O14.ResourceID is not null then O14.FirstName + ' ' + O14.LastName else null end, O15.Name, O16.Name, O17.Name, O18.Name, O21.Name, O22.Name, O23.Name, O24.Name, O25.DisplayValue, O26.Name, O27.Name, O28.Name, O29.Name, null) as Name, --O4.Name, 
			coalesce(O1.TextPath, O2.TextPath, O5.Name, O6.Name, O7.Name, O8.Name, O9.Name, O10.Name, O11.Name, O12.Name, O13.TextPath, case when O14.ResourceID is not null then O14.FirstName + ' ' + O14.LastName else null end, O15.Name, O16.Name, O17.TextPath, O18.Name, O21.Name, O22.Name, O23.Name, O24.Name, O25.DisplayValue, O26.Name, O27.Name, O28.Name, O29.Name, '') as TextPath, --O4.TextPath, 
			coalesce(O1.Description, O2.Description, O6.Description, O7.Description, O8.Description, O9.Description, O10.Description, O12.Description, O13.Description, O26.Description, NULL) as Description,
			case D.[Object]
				when 'Lookup' then dbo.GenerateNgObjectUrl('Lookup', O20.LookupTypeID, O20.ID)
				when 'LookupType' then dbo.GenerateNgObjectUrl('LookupType', O21.ID, 0)
				when 'ReferenceItem' then dbo.GenerateNgObjectUrl('ReferenceItem', O25.ReferenceItemTypeID, O25.ID)
				when 'ReferenceItemType' then dbo.GenerateNgObjectUrl('ReferenceItemType', O26.ID, 0)
				else dbo.GenerateNgObjectUrl(D.[Object], D.[ObjectTypeID], D.ObjectID) 
			end as Url,
			case 
				when P1.ID is not null then 'Artifact'
				when P2.ID is not null then 'Taxonomy'
				--when P4.ID is not null then 'FusionAttribute'
				when P7.ID is not null then 'ArtifactType'
				when P10.ID is not null then 'AttributeType'
				when P13.ID is not null then 'PolicyType'
				when P17.ID is not null then 'FusionAttributeType'
				else NULL
			end as Parent,
			coalesce(O1.ParentID, O2.ParentID, O7.ParentID, O10.ParentID, O13.ParentID, O17.ParentID, NULL) as ParentID, --O4.ParentID, 
			coalesce(P1.Name, P2.Name, P7.Name, P10.Name, P13.Name, P17.Name, NULL) as ParentName,	--P4.Name, 
			D.[ObjectType],
			D.ObjectTypeID,
			coalesce(OT1.Name, OT2.Name, OT5.Name, OT12.Name, OT13.Name, OT14.Name, OT15.Name, OT20.Name, OT24.Name, NULL) as ObjectTypeName, --OT4.TextPath, 
			coalesce(S.IconBackColor, '#000') as IconBackColor,
			coalesce(S.IconForeColor, '#fff') as IconForeColor,
			coalesce(S.IconText, 'leaf') as IconText,
			case D.[Object]
				when 'Lookup' then dbo.GenerateNgObjectUrl('Lookup', O20.LookupTypeID, O20.ID)
				when 'LookupType' then dbo.GenerateNgObjectUrl('LookupType', O21.ID, 0)
				when 'ReferenceItem' then dbo.GenerateNgObjectUrl('ReferenceItem', O25.ReferenceItemTypeID, O25.ID)
				when 'ReferenceItemType' then dbo.GenerateNgObjectUrl('ReferenceItemType', O26.ID, 0)
				else dbo.GenerateNgObjectUrl(D.[Object], D.[ObjectTypeID], D.ObjectID) 
			end as NgUrl
	from	cache.[Object] D with(nolock)
			left join Artifact O1 with(nolock) on D.[Object] = 'Artifact' and O1.ID = D.ObjectID
			left join ArtifactType OT1 with(nolock) on D.[Object] = 'Artifact' and OT1.ID = O1.ArtifactTypeID
			left join Artifact P1 with(nolock) on D.[Object] = 'Artifact' and P1.ID = O1.ParentID

			left join Taxonomy O2 with(nolock) on D.[Object] = 'Taxonomy' and O2.ID = D.ObjectID
			left join TaxonomyType OT2 with(nolock) on D.[Object] = 'Taxonomy' and OT2.ID = O2.TaxonomyTypeID
			left join Taxonomy P2 with(nolock) on D.[Object] = 'Taxonomy' and P2.ID = O2.ParentID

			--left join FusionAttribute O4 with(nolock) on D.[Object] = 'FusionAttribute' and O4.ID = D.ObjectID
			--left join FusionAttributeType OT4 with(nolock) on D.[Object] = 'FusionAttribute' and OT4.ID = O4.FusionAttributeTypeID
			--left join FusionAttribute P4 with(nolock) on D.[Object] = 'FusionAttribute' and P4.ID = O4.ParentID

			left join Fusion O5 with(nolock) on D.[Object] = 'Fusion' and O5.ID = D.ObjectID
			left join FusionType OT5 with(nolock) on D.[Object] = 'Fusion' and OT5.ID = O5.FusionTypeID

			left join FusionType O6 with(nolock) on D.[Object] = 'FusionType' and O6.ID = D.ObjectID

			left join ArtifactType O7 with(nolock) on D.[Object] = 'ArtifactType' and O7.ID = D.ObjectID
			left join ArtifactType P7 with(nolock) on D.[Object] = 'ArtifactType' and P7.ID = O7.ParentID

			left join TaxonomyType O8 with(nolock) on D.[Object] = 'TaxonomyType' and O8.ID = D.ObjectID

			left join ResponsibilityType O9 with(nolock) on D.[Object] = 'ResponsibilityType' and O9.ID = D.ObjectID

			left join AttributeType O10 with(nolock) on D.[Object] = 'AttributeType' and O10.ID = D.ObjectID
			left join AttributeType P10 with(nolock) on D.[Object] = 'AttributeType' and P10.ID = O10.ParentID

			left join IntersectType O11 with(nolock) on D.[Object] = 'IntersectType' and O11.ID = D.ObjectID

			left join [Rule] O12 with(nolock) on D.[Object] = 'Rule' and O12.ID = D.ObjectID
			left join	(
						select 1 as ID, 'Informational Rule' as Name
						union
						select 2 as ID, 'Quality Check Rule' as Name
						union
						select 3 as ID, 'Metric Rule' as Name
						union
						select 4 as ID, 'Profile Rule' as Name
						) OT12 on D.[Object] = 'Rule' and OT12.ID = O12.RuleType

			left join [Policy] O13 with(nolock) on D.[Object] = 'Policy' and O13.ID = D.ObjectID
			left join PolicyType OT13 with(nolock) on D.[Object] = 'Policy' and OT13.ID = O13.PolicyTypeID
			left join [Policy] P13 with(nolock) on D.[Object] = 'Policy' and P13.ID = O13.ParentID

			left join reporting.Global_Resource O14 with(nolock) on D.[Object] = 'Resource' and O14.ResourceID = D.ObjectID --and O14.Status = 'Active'
			left join (select 1 as ID, 'User' as Name) OT14 on D.[Object] = 'Resource' and OT14.ID = D.ObjectTypeID

			left join [Group] O15 with(nolock) on D.[Object] = 'Group' and O15.ID = D.ObjectID
			left join (
						select 0 as ID, 'Group' as Name
						union
						select 1 as ID, 'Group' as Name
					  ) OT15 on D.[Object] = 'Group' and OT15.ID = D.ObjectTypeID

			left join PolicyType O16 with(nolock) on D.[Object] = 'PolicyType' and O16.ID = D.ObjectID

			left join FusionAttributeType O17 with(nolock) on D.[Object] = 'FusionAttributeType' and O17.ID = D.ObjectID
			left join FusionAttributeType P17 with(nolock) on D.[Object] = 'FusionAttributeType' and P17.ID = O17.ParentID

			left join	(
						select 1 as ID, 'Informational Rule' as Name
						union
						select 2 as ID, 'Quality Check Rule' as Name
						union
						select 3 as ID, 'Metric Rule' as Name
						union
						select 4 as ID, 'Profile Rule' as Name
						) O18 on D.[Object] = 'RuleType' and O18.ID = D.ObjectID

			left join [Lookup] O20 with(nolock) on D.[Object] = 'Lookup' and O20.ID = D.ObjectID
			left join LookupType OT20 with(nolock) on D.[Object] = 'Lookup' and OT20.ID = O20.LookupTypeID

			left join [LookupType] O21 with(nolock) on D.[Object] = 'LookupType' and O21.ID = D.ObjectID

			left join	(
						select 0 as ID, 'User' as Name
						union
						select 1 as ID, 'User' as Name
						) O22 on D.[Object] = 'ResourceType' and O22.ID = D.ObjectID

			left join	(
						select 0 as ID, 'Group' as Name
						union
						select 1 as ID, 'Group' as Name
						) O23 on D.[Object] = 'GroupType' and O22.ID = D.ObjectID

			left join [Intersect] O24 with(nolock) on D.[Object] = 'Intersect' and O24.ID = D.ObjectID
			left join IntersectType OT24 with(nolock) on D.[Object] = 'Intersect' and OT24.ID = O24.IntersectTypeID

			left join ReferenceItem O25 with(nolock) on D.[Object] = 'ReferenceItem' and O25.ID = D.ObjectID
			left join ReferenceItemType OT25 with(nolock) on D.[Object] = 'ReferenceItem' and OT25.ID = O25.ReferenceItemTypeID

			left join ReferenceItemType O26 with(nolock) on D.[Object] = 'ReferenceItemType' and O26.ID = D.ObjectID

			left join FusionQueryAttributeType O27 with(nolock) on D.[Object] = 'FusionQueryAttributeType' and O27.ID = D.ObjectID

			left join IssueType O28 with(nolock) on D.[Object] = 'IssueType' and O28.ID = D.ObjectID

			left join	(
						select 0 as ID, 'Reference List' as Name						
			) O29 on D.[Object] = 'ReferenceItemType' and O29.ID = D.ObjectID
			
			left join ObjectStyle S with(nolock) on S.ObjectType = D.ObjectType and S.ObjectID = D.[ObjectTypeID]
GO

alter procedure [cache].[ReSynchronizeAllObjectDetails]
as
begin
	set nocount on;

	IF OBJECT_ID('tempdb..#Recache') IS NOT NULL
    DROP TABLE #Recache

	create table #Recache (
		[Object] varchar(50) not null,
		ObjectID int not null,
		ObjectType varchar(25) not null,
		ObjectTypeID int not null
	);

	declare @type varchar(50);
	
	begin
		set @type = 'Artifact'
		insert into #Recache
			SELECT	@type, ID, 'ArtifactType', ArtifactTypeID FROM Artifact;
	end;

	begin
		set @type = 'ArtifactType'
		insert into #Recache
			SELECT	@type, ID, @type, ID FROM ArtifactType;
	end;

	begin
		set @type = 'AttributeType';
		insert into #Recache
			SELECT	@type, ID, 'AttributeType', ID FROM AttributeType;
	end;

	begin
		set @type = 'Group';
		insert into #Recache
			SELECT	@type, ID, 'GroupType', 1 FROM [Group];
	end;

	begin
		set @type = 'Intersect';
		insert into #Recache
			SELECT	@type, ID, 'IntersectType', IntersectTypeID FROM [Intersect];
	end;

	begin
		set @type = 'IntersectType';
		insert into #Recache
			SELECT	@type, ID, @type, ID FROM IntersectType;
	end;

	begin
		set @type = 'Event';
		insert into #Recache
			SELECT	@type, O.ID, 'Rule', R.ID
			FROM	[Event] O
					INNER JOIN EventGroup G on G.ID = O.EventGroupID
					INNER JOIN [Rule] R on R.ID = G.RuleID;
	end;

	begin
		set @type = 'EventGroup';
		insert into #Recache
			SELECT	@type, ID, 'Rule', RuleID FROM EventGroup;
	end;

	begin
		set @type = 'Lookup';
		insert into #Recache
			SELECT	@type, ID, 'LookupType', LookupTypeID FROM [Lookup];
	end;

	begin
		set @type = 'LookupType';
		insert into #Recache
			SELECT	@type, ID, @type, ID FROM LookupType;
	end;

	begin
		set @type = 'Fusion';
		insert into #Recache
			SELECT	@type, ID, 'FusionType', FusionTypeID FROM Fusion;
	end;

	begin
		set @type = 'FusionType';
		insert into #Recache
			SELECT	@type, ID, @type, ID FROM FusionType;
	end;

/*	begin
		set @type = 'FusionAttribute';
		insert into #Recache
			SELECT	@type, ID, 'FusionAttributeType', FusionAttributeTypeID FROM FusionAttribute;
	end;*/
 
	begin
		set @type = 'FusionAttributeType';
		insert into #Recache
			SELECT	@type, ID, 'FusionType', FusionTypeID FROM FusionAttributeType;
	end;

	begin
		set @type = 'GroupType';
		insert into #Recache values (@type, 0, @type, 0);
		insert into #Recache values (@type, 1, @type, 0);
	end;

	begin
		set @type = 'Policy';
		insert into #Recache
			SELECT	@type, ID, 'PolicyType', PolicyTypeID FROM [Policy];
	end;

	begin
		set @type = 'PolicyType';
		insert into #Recache
			SELECT	@type, ID, 'PolicyType', ID FROM [PolicyType];
	end;

	begin
		set @type = 'ReferenceItemType';
		insert into #Recache
			SELECT	@type, ID, 'ReferenceItemType', ID FROM ReferenceItemType;
	end;

	begin
		set @type = 'Resource';
		insert into #Recache
			select	@type, ResourceID, 'ResourceType', 1 from reporting.Global_Resource;
	end;

	begin
		set @type = 'ResourceType';
		insert into #Recache values (@type, 0, @type, 0)
		insert into #Recache values (@type, 1, @type, 0)
	end;

	begin
		set @type = 'ResponsibilityType';
		insert into #Recache
			SELECT	@type, ID, @type, 0 FROM ResponsibilityType;
	end;

	begin
		INSERT INTO #Recache VALUES ('RuleType', 1, 'RuleType', 1)
		INSERT INTO #Recache VALUES ('RuleType', 2, 'RuleType', 2)
		INSERT INTO #Recache VALUES ('RuleType', 3, 'RuleType', 3)
		INSERT INTO #Recache VALUES ('RuleType', 4, 'RuleType', 4)

		set @type = 'Rule';
		insert into #Recache
			SELECT	@type, ID, 'RuleType', RuleType FROM [Rule];
	end;

	begin
		set @type = 'Taxonomy';
		insert into #Recache
			SELECT	@type, ID, 'TaxonomyType', TaxonomyTypeID FROM Taxonomy
	end;

	begin
		set @type = 'TaxonomyType';
		insert into #Recache
			SELECT	@type, ID, @type, ID FROM TaxonomyType;
	end;

	-- upsert the individual object into the cache table.
	merge	cache.[Object] as T
	using	(
			SELECT	*
			FROM	#Recache
			) as S
	on		(
			T.[Object] = S.[Object] and T.[ObjectID] = S.[ObjectID]
			)
	when matched then
			update	
			set		T.ObjectType = S.ObjectType,
					T.ObjectTypeID = S.ObjectTypeID
	when not matched then
			insert ( [Object], ObjectID, ObjectType, ObjectTypeID )
			values ( S.[Object], S.ObjectID, S.ObjectType, S.ObjectTypeID );
end
GO

alter procedure [dbo].[GetLineage]
--declare 
	@type varchar(50),
	@id int,
	@view int = 1

--set @type = 'Artifact'
--set @id = 2528--6381
--set @view = 3
as
begin
	declare @links table ([from] varchar(250), [to] varchar(250), category varchar(50))
	declare @nodes table (
		[key] varchar(250), 
		obj varchar(50), [objid] int, [type] varchar(50), typeName nvarchar(250), name nvarchar(500), 
		back varchar(7), fore varchar(7), template varchar(50), other varchar(500),

		HasSourceRules bit
		)
	declare @objects table (Type varchar(50), ID int)

	if @view in (0, 1, 2)
	begin
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


		declare @items table (
			ID int,
			SourceIntersectID int, 
			SourceSubjectTypeName nvarchar(500), SourceSubjectName nvarchar(500), SourceSubject varchar(50), SourceSubjectID int, SourceSubjectIconBackColor varchar(7), SourceSubjectIconForeColor varchar(7), 
			SourceObjectTypeName nvarchar(500), SourceObjectName nvarchar(500), SourceObject varchar(50), SourceObjectID int, SourceObjectIconBackColor varchar(7), SourceObjectIconForeColor varchar(7),
			
			TargetIntersectID int, 
			TargetSubjectTypeName nvarchar(500), TargetSubjectName nvarchar(500), TargetSubject varchar(50), TargetSubjectID int, TargetSubjectIconBackColor varchar(7), TargetSubjectIconForeColor varchar(7), 
			TargetObjectTypeName nvarchar(500), TargetObjectName nvarchar(500), TargetObject varchar(50), TargetObjectID int, TargetObjectIconBackColor varchar(7), TargetObjectIconForeColor varchar(7),

			HasSourceRules bit
		)

		insert into @items
			select	O.ID,
				
					O.SourceIntersectID,
					SI.SubjectTypeName,
					SI.SubjectName,
					SI.Subject,
					SI.SubjectID,
					SI.SubjectIconBackColor,
					SI.SubjectIconForeColor,
					SI.ObjectTypeName,
					SI.ObjectName,
					SI.Object,
					SI.ObjectID,
					SI.ObjectIconBackColor,
					SI.ObjectIconForeColor,

					O.TargetIntersectID,
					TI.SubjectTypeName,
					TI.SubjectName,
					TI.Subject,
					TI.SubjectID,
					TI.SubjectIconBackColor,
					TI.SubjectIconForeColor,
					TI.ObjectTypeName,
					TI.ObjectName,
					TI.Object,
					TI.ObjectID,
					TI.ObjectIconBackColor,
					TI.ObjectIconForeColor,

					case 
						when HSR.C > 0 then cast(1 as bit)
						else cast(0 as bit)
					end as HasSourceRules
			from	@points O
					inner join IntersectDetail SI on SI.ID = O.SourceIntersectID
					inner join IntersectDetail TI ON TI.ID = O.TargetIntersectID
					cross apply (
								select	count(1) as C
								from	MapItem MI 
										inner join MapSequence MS on MS.MapItemID = MI.ID and MI.TargetIntersectID = TI.ID
								) HSR
		
		if @view = 0
		begin
			select (
					select distinct
					cast(SI.IntersectTypeID as varchar) + '.' 
					+ cast(I.SourceSubjectID as varchar) + '.'
					+ cast(I.SourceObjectID as varchar) as [sourcekey],
					cast(TI.IntersectTypeID as varchar) + '.' 
					+ cast(I.TargetSubjectID as varchar) + '.'
					+ cast(I.TargetObjectID as varchar) as [targetkey],
					I.*,
					SI.IntersectTypeID as SourceIntersectTypeID,
					SIT.[Name] as SourceIntersectTypeName,
					TI.IntersectTypeID as TargetIntersectTypeID,
					TIT.[Name] as TargetIntersectTypeName
				from @items I
				inner join [Intersect] SI on SI.ID = I.SourceIntersectID
				inner join IntersectType SIT on SIT.ID = SI.IntersectTypeID
				inner join [Intersect] TI on TI.ID = I.TargetIntersectID
				inner join IntersectType TIT on TIT.ID = TI.IntersectTypeID
				for json path
			) as 'items'
			for json path, WITHOUT_ARRAY_WRAPPER
		end

		if @view = 1
		begin
			insert into @links
					select	distinct
							S.SourceSubject + '.' + cast(S.SourceSubjectID as varchar) as [from],
							S.TargetSubject + '.' + cast(S.TargetSubjectID as varchar) as [to],
							'' as category
					from	@items S
			insert into @nodes
					select	distinct
							I.SourceSubject + '.' + cast(I.SourceSubjectID as varchar) as [key],
							I.SourceSubject as [obj],
							I.SourceSubjectID as [objid], 
							I.SourceSubject as [type],
							I.SourceSubjectTypeName as typeName,
							I.SourceSubjectName as name,
							I.SourceSubjectIconBackColor as back,
							I.SourceSubjectIconForeColor as fore,
							case 
								when I.SourceSubject = @type and I.SourceSubjectID = @id then 'Focal'
								else 'Normal'
							end as template,
							null as other,
							0 as HasSourceRules--I.HasSourceRules
					from	@items I;
			--insert into @nodes
			merge	@nodes as T
			using	(
					select	distinct
							I.TargetSubject + '.' + cast(I.TargetSubjectID as varchar) as [key],
							I.TargetSubject as [obj],
							I.TargetSubjectID as [objid], 
							I.TargetSubject as [type],
							I.TargetSubjectTypeName as typeName,
							I.TargetSubjectName as name,
							I.TargetSubjectIconBackColor as back,
							I.TargetSubjectIconForeColor as fore,
							case 
								when I.TargetSubject = @type and I.TargetSubjectID = @id then 'Focal'
								else 'Normal'
							end as template,
							null as other,
							I.HasSourceRules
					from	@items I
					) S
			on		(T.[key] = S.[key])
			when	matched then
			update	set
					T.HasSourceRules = S.HasSourceRules
			when	not matched then
			insert	([key], obj, [objid], [type], typeName, name, back, fore, template, other, HasSourceRules)
			values	(S.[key], S.obj, S.[objid], S.[type], S.typeName, S.name, S.back, S.fore, S.template, S.other, S.HasSourceRules);
					--where	I.TargetSubject + '.' + cast(I.TargetSubjectID as varchar) not in (select [key] from @nodes)

			--select	* from	@items
			--select	* from	@links
			--select	* from	@nodes

			select	(
					select	*
					from	@links O
					for json path			
					) as 'links',
					(
					select	I.*,
							C.challenges,
							E.issues
					from	@nodes I
							cross apply (
											select count(1) as challenges   
											from Workflow W            			                          
											where W.WorkflowType = 4 and W.Data.exist('/fields/ArtifactID[text() = sql:column("I.objid")]') = 1 and W.DateCompleted is null   
										) C
							cross apply (
											select count(1) as issues   
											from Workflow W            			                          
											where W.WorkflowType = 3 and W.Data.exist('/fields/ArtifactID[text() = sql:column("I.objid")]') = 1 and W.DateCompleted is null   
										) E
					for json path			
					) as 'nodes'
			for json path, WITHOUT_ARRAY_WRAPPER
		end --view 1

		if @view = 2
		begin
			insert into @links
				select	distinct
						SourceSubject + '.' + cast(SourceSubjectID as varchar) as 'from',
						cast(SourceIntersectID as varchar) + '.S' as 'to',
						'Support' as category
				from	@items
				union
				select	distinct
						cast(SourceIntersectID as varchar) + '.S' as 'from',
						TargetSubject + '.' + cast(TargetSubjectID as varchar) as 'to',
						'' as category
				from	@items O
				where	(SourceObject + cast(SourceObjectID as varchar)) = (TargetObject + cast(TargetObjectID as varchar))
				union
				select	distinct
						cast(SourceIntersectID as varchar) + '.S' as 'from',
						cast(TargetIntersectID as varchar) + '.T' as 'to',
						'' as category
				from	@items O
				where	(SourceObject + cast(SourceObjectID as varchar)) <> (TargetObject + cast(TargetObjectID as varchar))
				--where	TargetIntersectID in (select SourceIntersectID from @items)
				union
				select	distinct
						cast(TargetIntersectID as varchar) + '.T' as 'from',
						TargetSubject + '.' + cast(TargetSubjectID as varchar) as 'to',
						'Support' as category
				from	@items
				where	(SourceObject + cast(SourceObjectID as varchar)) <> (TargetObject + cast(TargetObjectID as varchar))

			insert into @nodes
				select	distinct
						SourceSubject + '.' + cast(SourceSubjectID as varchar) as [key],
						SourceSubject as [obj],
						SourceSubjectID as [objid], 
						SourceSubject as [type],
						SourceSubjectTypeName as typeName,
						SourceSubjectName as name,
						SourceSubjectIconBackColor as back,
						SourceSubjectIconForeColor as fore,
						case 
							when SourceSubject = @type and SourceSubjectID = @id then 'Focal'
							else 'Normal'
						end as template,
						null as other,
						0 as HasSourceRules
				from	@items 

			insert into @nodes
				select	distinct
						cast(SourceIntersectID as varchar) + '.S' as [key],
						SourceObject as [obj],
						SourceObjectID as [objid], 
						SourceObject as [type],
						SourceObjectTypeName as typeName,
						SourceObjectName as name,
						SourceObjectIconBackColor as back,
						SourceObjectIconForeColor as fore,
						case 
							when SourceObject = @type and SourceObjectID = @id then 'SupportFocal'
							else 'SupportNormal'
						end as template,
						null as other,
						0 as HasSourceRules
				from	@items

			merge	@nodes as T
			using	(
					select	distinct
							cast(TargetIntersectID as varchar) + '.T' as [key],
							TargetObject as [obj],
							TargetObjectID as [objid], 
							TargetObject as [type],
							TargetObjectTypeName as typeName,
							TargetObjectName as name,
							TargetObjectIconBackColor as back,
							TargetObjectIconForeColor as fore,
							case 
								when TargetObject = @type and TargetObjectID = @id then 'SupportFocal'
								else 'SupportNormal'
							end as template,
							null as other,
							HasSourceRules
					from	@items
					where	(SourceObject + cast(SourceObjectID as varchar)) <> (TargetObject + cast(TargetObjectID as varchar))
					) S
			on		(T.[key] = S.[key])
			when	matched then
			update	set
					T.HasSourceRules = S.HasSourceRules
			when	not matched then
			insert	([key], obj, [objid], [type], typeName, name, back, fore, template, other, HasSourceRules)
			values	(S.[key], S.obj, S.[objid], S.[type], S.typeName, S.name, S.back, S.fore, S.template, S.other, S.HasSourceRules);

			merge	@nodes as T
			using	(
					select	distinct
							TargetSubject + '.' + cast(TargetSubjectID as varchar) as [key],
							TargetSubject as [obj],
							TargetSubjectID as [objid], 
							TargetSubject as [type],
							TargetSubjectTypeName as typeName,
							TargetSubjectName as name,
							TargetSubjectIconBackColor as back,
							TargetSubjectIconForeColor as fore,
							case 
								when TargetSubject = @type and TargetSubjectID = @id then 'Focal'
								else 'Normal'
							end as template,
							null as other,
							HasSourceRules
					from	@items
					) S
			on		(T.[key] = S.[key])
			when	matched then
			update	set
					T.HasSourceRules = S.HasSourceRules
			when	not matched then
			insert	([key], obj, [objid], [type], typeName, name, back, fore, template, other, HasSourceRules)
			values	(S.[key], S.obj, S.[objid], S.[type], S.typeName, S.name, S.back, S.fore, S.template, S.other, S.HasSourceRules);

			--select	* from	@links
			--select	* from	@nodes

			select	(
					select	*
					from	@links O
					for json path			
					) as 'links',
					(
					select	I.*,
							C.challenges,
							E.issues
					from	@nodes I
							cross apply (
											select count(1) as challenges   
											from Workflow W            			                          
											where W.WorkflowType = 4 and W.Data.exist('/fields/ArtifactID[text() = sql:column("I.objid")]') = 1 and W.DateCompleted is null   
										) C
							cross apply (
											select count(1) as issues   
											from Workflow W            			                          
											where W.WorkflowType = 3 and W.Data.exist('/fields/ArtifactID[text() = sql:column("I.objid")]') = 1 and W.DateCompleted is null   
										) E
					for json path			
					) as 'nodes'
			for json path, WITHOUT_ARRAY_WRAPPER
		end --view 2
	end

	if @view = 3
	begin
		declare @tFusionPoints table ( ID int, MapItemID int, SourceFusionAttributeID int, TargetFusionAttributeID int )

		declare @tItems table (
			MapItemID int, --MapID int,

			SourceIntersectID int, 
			SourceSubjectTypeName nvarchar(500), SourceSubjectName nvarchar(500), SourceSubject varchar(50), SourceSubjectID int,
			SourceObjectTypeName nvarchar(500), SourceObjectName nvarchar(500), SourceObject varchar(50), SourceObjectID int, 
			
			TargetIntersectID int, 
			TargetSubjectTypeName nvarchar(500), TargetSubjectName nvarchar(500), TargetSubject varchar(50), TargetSubjectID int, 
			TargetObjectTypeName nvarchar(500), TargetObjectName nvarchar(500), TargetObject varchar(50), TargetObjectID int
		)
	
		if @type = 'FusionAttribute'
			begin
				insert into @tFusionPoints
					select	I.ID,
							NULL,
							I.SourceFusionAttributeID,
							I.TargetFusionAttributeID
					from	MapRuleItem I
							inner join FusionAttribute SFA on SFA.ID = I.SourceFusionAttributeID and SFA.Deleted = 0
							inner join FusionAttribute TFA on TFA.ID = I.TargetFusionAttributeID and TFA.Deleted = 0
					where	I.SourceFusionAttributeID = @id or I.TargetFusionAttributeID = @id;

				with cte as (
					select	ID,
							SourceFusionAttributeID,
							TargetFusionAttributeID,
							1 as [Level]
					from	@tFusionPoints
					union all
					select	S.ID,
							S.SourceFusionAttributeID,
							S.TargetFusionAttributeID,
							T.[Level] + 1 as [Level]
					from	MapRuleItem S
							inner join cte T on T.SourceFusionAttributeID = S.TargetFusionAttributeID and S.ID <> T.ID
					where	T.[Level] <= 25
				)
				insert into @tFusionPoints
					select ID, NULL, SourceFusionAttributeID, TargetFusionAttributeID from cte where ID not in (select ID from @tFusionPoints)

				-- get all items directly tied to the focal object.
				insert into @tItems
					select	MI.ID,
					
							MI.SourceIntersectID,
							SI.SubjectTypeName,
							SI.SubjectName,
							SI.Subject,
							SI.SubjectID,
							SI.ObjectTypeName,
							SI.ObjectName,
							SI.Object,
							SI.ObjectID,

							MI.TargetIntersectID,
							TI.SubjectTypeName,
							TI.SubjectName,
							TI.Subject,
							TI.SubjectID,
							TI.ObjectTypeName,
							TI.ObjectName,
							TI.Object,
							TI.ObjectID

					from	@tFusionPoints F
							inner join MapRuleItemMapItem J on J.MapRuleItemID = F.ID
							inner join MapItem MI on MI.ID = J.MapItemID
							inner join IntersectDetail SI on SI.ID = MI.SourceIntersectID
							inner join IntersectDetail TI ON TI.ID = MI.TargetIntersectID


				-- get all items not directly tied to the focal object, but still tied to maps involved above.
				insert into @tItems
					select	MI.ID,
							--NULL,
					
							MI.SourceIntersectID,
							SI.SubjectTypeName,
							SI.SubjectName,
							SI.Subject,
							SI.SubjectID,
							SI.ObjectTypeName,
							SI.ObjectName,
							SI.Object,
							SI.ObjectID,

							MI.TargetIntersectID,
							TI.SubjectTypeName,
							TI.SubjectName,
							TI.Subject,
							TI.SubjectID,
							TI.ObjectTypeName,
							TI.ObjectName,
							TI.Object,
							TI.ObjectID

					from	MapItem MI
							inner join	(
										select	ID.MapItemID
										from	MapItemMap DM
												inner join @tItems D on D.MapItemID = DM.MapItemID
												inner join MapItemMap ID on ID.MapID = DM.MapID and ID.MapItemID not in (
																														select MapItemID from @tItems
																														)
										) O on O.MapItemID = MI.ID
							inner join [IntersectDetail] SI on SI.ID = MI.SourceIntersectID
							inner join [IntersectDetail] TI on TI.ID = MI.TargetIntersectID
			end
		else
			begin
				declare @tBusinessPoints table ( ID int, SourceIntersectID int, TargetIntersectID int )

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
								inner join [Predicate] P on P.ID = T.PredicateID and P.Type = 6
						where	(I.Subject = @type and I.SubjectID = @id) or (I.Object = @type and I.ObjectID = @id)
				end

				-- get all items directly tied to the focal object.
				insert into @tBusinessPoints
					select	MI.ID, MI.SourceIntersectID, MI.TargetIntersectID
					from	MapItem MI
							inner join [Intersect] SI on SI.ID = MI.SourceIntersectID
							inner join [Intersect] TI ON TI.ID = MI.TargetIntersectID
							inner join @objects O on	( (SI.Subject = O.Type and SI.SubjectID = O.ID) OR (SI.Object = O.Type and SI.ObjectID = O.ID)  ) OR 
														( (TI.Subject = O.Type and TI.SubjectID = O.ID) OR (TI.Object = O.Type and TI.ObjectID = O.ID)  )

				-- get all items not directly tied to the focal object, but still tied to maps involved above.
				insert into @tBusinessPoints
					select	MI.ID, MI.SourceIntersectID, MI.TargetIntersectID
					from	MapItem MI
							inner join	(
										select	ID.MapItemID
										from	MapItemMap DM
												inner join @tBusinessPoints D on D.ID = DM.MapItemID
												inner join MapItemMap ID on ID.MapID = DM.MapID and ID.MapItemID not in (
																														select ID from @tBusinessPoints
																														)
										) O on O.MapItemID = MI.ID;

				with cte as (
					select	ID,
							SourceIntersectID,
							TargetIntersectID,
							1 as [Level]
					from	@tBusinessPoints
					union all
					select	S.ID,
							S.SourceIntersectID,
							S.TargetIntersectID,
							T.[Level] + 1 as [Level]
					from	MapItem S
							inner join cte T on T.SourceIntersectID = S.TargetIntersectID and S.ID <> T.ID
					where	T.[Level] <= 25
				)
				insert into @tBusinessPoints
					select ID, SourceIntersectID, TargetIntersectID from cte where ID not in (select ID from @tBusinessPoints)

				insert into @tItems
					select	O.ID,
							--NULL,
					
							O.SourceIntersectID,
							SI.SubjectTypeName,
							SI.SubjectName,
							SI.Subject,
							SI.SubjectID,
							SI.ObjectTypeName,
							SI.ObjectName,
							SI.Object,
							SI.ObjectID,

							O.TargetIntersectID,
							TI.SubjectTypeName,
							TI.SubjectName,
							TI.Subject,
							TI.SubjectID,
							TI.ObjectTypeName,
							TI.ObjectName,
							TI.Object,
							TI.ObjectID

					from	@tBusinessPoints O
							inner join IntersectDetail SI on SI.ID = O.SourceIntersectID
							inner join IntersectDetail TI ON TI.ID = O.TargetIntersectID

				insert into @tFusionPoints
					select	J.MapRuleItemID,
							J.MapItemID,
							T.SourceFusionAttributeID,
							T.TargetFusionAttributeID
					from	@tItems I
							inner join MapRuleItemMapItem J on J.MapItemID = I.MapItemID
							inner join MapRuleItem T on T.ID = J.MapRuleItemID
							inner join FusionAttribute SFA on SFA.ID = T.SourceFusionAttributeID and SFA.Deleted = 0
							inner join FusionAttribute TFA on TFA.ID = T.TargetFusionAttributeID and TFA.Deleted = 0
			end

			--Load tables we will return to caller.
			insert into @links
				select	distinct
						cast(S.SourceFusionAttributeID as varchar) + '.' + coalesce(B.SourceSubject, '0') + '.' + coalesce(cast(B.SourceSubjectID as varchar), '0') as [from],
						cast(S.TargetFusionAttributeID as varchar) + '.' + coalesce(B.TargetSubject, '0') + '.' + coalesce(cast(B.TargetSubjectID as varchar), '0') as [to],
						'' as category
				from	@tFusionPoints S
						left join MapRuleItemMapItem J on J.MapRuleItemID = S.ID
						left join @tItems B on B.MapItemID = J.MapItemID
			insert into @nodes
				select	distinct
						cast(S.SourceFusionAttributeID as varchar) + '.' + coalesce(B.SourceSubject, '0') + '.' + coalesce(cast(B.SourceSubjectID as varchar), '0') as [key],
						'FusionAttribute' as [obj],
						SourceFusionAttributeID as [objid], 
						'FusionAttribute' as [type],
						T.Name as typeName,
						A.TextPath as name,
						'#000' as back,
						'#fff' as fore,
						'Fusion' as template,
						B.SourceSubjectTypeName + ' : ' + B.SourceSubjectName as other,
						null
				from	@tFusionPoints S
						inner join FusionAttribute A on A.ID = S.SourceFusionAttributeID
						inner join FusionAttributeType T on T.ID = A.FusionAttributeTypeID
						left join MapRuleItemMapItem J on J.MapRuleItemID = S.ID
						left join @tItems B on B.MapItemID = J.MapItemID
			insert into @nodes
				select	distinct
						cast(S.TargetFusionAttributeID as varchar) + '.' + coalesce(B.TargetSubject, '0') + '.' + coalesce(cast(B.TargetSubjectID as varchar), '0') as [key],
						'FusionAttribute' as [obj],
						TargetFusionAttributeID as [objid], 
						'FusionAttribute' as [type],
						T.Name as typeName,
						A.TextPath as name,
						'#000' as back,
						'#fff' as fore,
						'Fusion' as template,
						B.TargetSubjectTypeName + ' : ' + B.TargetSubjectName as other,
						null
				from	@tFusionPoints S
						inner join FusionAttribute A on A.ID = S.TargetFusionAttributeID
						inner join FusionAttributeType T on T.ID = A.FusionAttributeTypeID
						left join MapRuleItemMapItem J on J.MapRuleItemID = S.ID
						left join @tItems B on B.MapItemID = J.MapItemID
				where	cast(S.TargetFusionAttributeID as varchar) + '.' + coalesce(B.TargetSubject, '0') + '.' + coalesce(cast(B.TargetSubjectID as varchar), '0') not in (select [key] from @nodes)

				--gets rid of dupes
				delete	@nodes 
				where	other is null 
						and (obj + cast([objid] as varchar)) in (
																select	(obj + cast([objid] as varchar))
																from	@nodes 
																where	other is not null
															  )
				delete	T
				from	@links T
						left join @nodes S on S.[key] = T.[from] or S.[key] = T.[to]
				where	S.[key] is null

--select	* from	@links
--select	* from	@nodes

		select	(
				select	*
				from	@links O
				for json path			
				) as 'links',
				(
				select	*
				from	@nodes
				for json path			
				) as 'nodes'
		for json path, WITHOUT_ARRAY_WRAPPER
	end --view 3
end
GO


alter PROCEDURE [dbo].[GetRenderedTemplateBodyNg]-- 'Tooltip', 'Resource', 2, 'Preview'
--declare
	@TemplateType varchar(25),
	@Type varchar(50),
	@ID int,
	@Action varchar(50),
	@SubjectName VARCHAR (200) = 'Governing Domain'
--set @TemplateType = 'Lookup'
--set @Type = 'Artifact'
--set @ID = 7004--16435
--set @Action = 'Preview'--'Certificate'
AS
BEGIN
	SET NOCOUNT ON;

	declare @html nvarchar(max),
			@link nvarchar(2500),
			@icon nvarchar(250),
			@hasDynamicFields bit = 0,
			@hasStats bit = 0,
			@typeID int,

			@showIcon bit = 1,

			@current int,
			@max int,
			@name nvarchar(250),
			@value nvarchar(max);

	declare @tbl table (ID int identity, Name nvarchar(250), Value nvarchar(max));

	if @TemplateType = 'Email'
	begin
		select	@html = TemplateBody
		from	EmailTemplate
		where	Name = @Type
				and [Action] = @Action
	end

	if @TemplateType = 'Tooltip'
	begin
		select	@html = TemplateBody
		from	TooltipTemplate
		where	Name = @Type
				and [Action] = @Action
	end

	-- Get the static tokens, depending on the type.
	declare @n nvarchar(250), @t nvarchar(250), @s nvarchar(25), @v int, @dc datetime, @du datetime, @d nvarchar(4000);
		
	-- Get common fields
	select	@typeID = ObjectTypeID,
			@icon = '<div title=''' + ObjectTypeName + ''' class=''tooltip-icon'' style=''background-color: ' + IconBackColor + '; color: ' + IconForeColor + '''><i class=''fa fa-' + IconText + '''></i></div>',
			@n = Name,
			@t = ObjectTypeName,
			@d = Description,
			@link = NgUrl
	from	cache.ObjectDetails
	where	[Object] = @Type
			and ObjectID = @ID;

	--fusion attributes arent in cache
	if @Type = 'FusionAttribute'
	begin		
		select 
			@typeID = fa.fusionattributetypeid,
			@n = fa.name,
			@t = fat.textpath,
			@link = dbo.GenerateNgObjectUrl('FusionAttribute', fat.id, fa.id) 
		from fusionattribute fa 
			inner join fusionattributetype fat on (fa.fusionattributetypeid = fat.id) 
		where fa.id = @ID
	end

	if @n is not null
	begin
		if @link is null
		begin
			insert into @tbl values ('Name', @n)
		end
		else
		begin
			insert into @tbl values ('Name', '<a routerLink="/' + @link + '">' + @n + '</a>')
		end
		insert into @tbl values ('Description', @d)
	end
	insert into @tbl values ('Type', @t)

	if @Action = 'AssigningItemPreview'
	begin
		set @html = '<h3>{Name}</h3>'
	end

	if @Action = 'Certificate'
	begin
		set @html = '<h3>{Name}</h3>'

		declare @workflowID uniqueidentifier,
				@dateCertifiedOn varchar(10),
				@certifiers nvarchar(2500),
				@status varchar(50),
				@certIconColor varchar(10)

		select	@dateCertifiedOn = CONVERT(VARCHAR(10), DateLastCertified, 101),
				@status = Status
		from	Artifact A
		where	A.ID = @ID

		SELECT	@workflowID = W.ID,
				@certifiers = COALESCE(@certifiers + ', ', '') + R.FirstName + ' ' + R.LastName 
		from	(
				select		top 1
							ID,
							Data.value('(/fields/ArtifactID)[1]', 'int') as ArtifactID,
							DateCompleted
				from		Workflow
				where		WorkflowType = 2
							and Data.exist('/fields/ArtifactID[text() = sql:variable("@ID")]') = 1
				order by	DateCompleted desc
				) W
				inner join WorkflowResource WR on WR.WorkflowID = W.ID
				inner join reporting.Global_Resource R on R.ResourceID = WR.ResourceID

		if @dateCertifiedOn is null and @status != 'Certified'
			begin
				set @showIcon = 0

				set @html = @html + '<div><b>Not yet certified</b></div>'
				if @certifiers is not null
				begin
					set @html = @html + '<div>Certifying Users: {Certifiers}</div>'
				end
				if @workflowID is not null
				begin
					set @html = @html + '<div><a class=''btn btn-info'' routerLink=''/workflow/status/' + cast(@workflowID as varchar(50)) + '''>Go to this workflow status</a>.</div>'
				end
			end
		else
			begin
				if @status = 'Certified'
					begin
						set @certIconColor = '#EFC43D'
					end
				else 
					begin
						set @certIconColor = '#FFE183'
					end
				select	@icon = '<div style="background-color: transparent; color: ' + @certIconColor + '"><i class="fa fa-2x fa-certificate"></i></div>'
				set @html = @html + '<div>Last Certified On: ';
				if @dateCertifiedOn is null
					begin
						set @html = @html + 'Manually Certified';
					end
				else
					begin
						set @html = @html + '{CertifiedOn}';
					end
				set @html = @html + '</div>';
				if @status = 'Certified'
					begin
						if @Certifiers is not null
						begin
							set @html = @html + '<div>Certified By: {Certifiers}</div>'
						end
					end
				else 
					begin
						set @html = @html + '<div>Currently Under Certification Review</div>'
						set @html = @html + '<div>Certifying Users: {Certifiers}</div>'
						if @workflowID is not null
						begin
							set @html = @html + '<div><a class=''btn btn-info'' routerLink=''/workflow/status/' + cast(@workflowID as varchar(50)) + '''>Go to this workflow status</a>.</div>'
						end
					end
			end

		insert into @tbl values ('CertifiedOn', @dateCertifiedOn)
		insert into @tbl values ('Certifiers', @certifiers)
	end
	if @Action = 'JoinRequest'
	begin
		set @html = ''
	end
	if @Action = 'LookupPreview'
	begin
		set @html = '{Items}'
		
		if @Type = 'FusionAttribute'
		begin
			-- BUILD LIST HTML -----------------------------------------
			declare @fusionAttributeItemsHtml nvarchar(max)

			set @fusionAttributeItemsHtml = '<div style="height: 200px; overflow-y: scroll"><table class="hoverable bordered striped" style="width:100%"><thead>'
			set @fusionAttributeItemsHtml = @fusionAttributeItemsHtml + '<th style="margin-right: 15px">Name</th>'
			set @fusionAttributeItemsHtml = @fusionAttributeItemsHtml + '</thead><tbody>'

			select		--top 10 
						@fusionAttributeItemsHtml = @fusionAttributeItemsHtml + '<tr>' 
											+ '<td>' + Name + '</td>'
											+ '</tr>'
			from		FusionAttribute
			where		ParentID = @ID
			order by	Name asc

			set @fusionAttributeItemsHtml = @fusionAttributeItemsHtml + '</tbody>'
			set @fusionAttributeItemsHtml = @fusionAttributeItemsHtml + '</table></div>'
 
			insert into @tbl values ('Items', @fusionAttributeItemsHtml)
			------------------------------------------------------------------
		end;

		if @Type = 'LookupType' OR @Type = 'Lookup'
		begin
			-- BUILD LOOKUP LIST HTML -----------------------------------------
			declare @lookups table (RowID int identity, ID int)

			declare @MyLookupTypeID int
			if @Type = 'Lookup'
				begin
					select @MyLookupTypeID = LookupTypeID from [Lookup] where ID = @ID 
				end
			else
				begin
					set @MyLookupTypeID = @ID
				end

			insert into @lookups 
				select top 10 ID from [Lookup] where LookupTypeID = @MyLookupTypeID order by ID desc
		
			declare @lookupFieldTypes table (ID int identity, Name nvarchar(250))
			insert into @lookupFieldTypes
				select FriendlyName from FieldType where [Object] = 'LookupType' and ObjectID = @MyLookupTypeID order by SortOrder asc

			declare @lookupHtml nvarchar(max)

			set @lookupHtml = '<table class="hoverable bordered striped" style="width:100%">'

			-- Loop through field name list ---------
			set @lookupHtml = @lookupHtml + '<thead>'
			set		@current = 1
			select	@max = max(ID) from @lookupFieldTypes
			while @current <= @max
			begin
				select	@name = Name
				from	@lookupFieldTypes
				where	ID = @current

				set @lookupHtml = @lookupHtml + '<th style="margin-right: 15px">' + @name  + '</th>'

				set @current = @current + 1
			end
			set @lookupHtml = @lookupHtml + '</thead>'
			-----------------------------------------

			set @lookupHtml = @lookupHtml + '<tbody>'

			-- Loop through event list --------------
			select	@current = min(RowID) from @lookups
			select	@max = max(RowID) from @lookups

			while @current <= @max
			begin
				set @lookupHtml = @lookupHtml + '<tr>'	-- Open row for selected event.

				declare @lookupFields table (Name nvarchar(250), Value nvarchar(4000))
			
				declare @lookupID int

				select	@lookupID = ID from @lookups where RowID = @current

				insert into @lookupFields
					select		FriendlyName,
								FormattedValue
					from		FieldWithRelation
					where		ObjectType = 'Lookup' 
								and ObjectID = @lookupID

					-- Loop through each field for this selected event --
					declare @lfCurrent int,
							@lfMax int
					set		@lfCurrent = 1
					select	@lfMax = max(ID) from @lookupFieldTypes
					while @lfCurrent <= @lfMax
					begin
						select	@name = Name from @lookupFieldTypes where ID = @lfCurrent

						select @lookupHtml = @lookupHtml + '<td>' + coalesce(Value, '') + '</td>' from @lookupFields where Name = @name

						set @lfCurrent = @lfCurrent + 1
					end
					-----------------------------------------------------

				delete @lookupFields

				set @lookupHtml = @lookupHtml + '</tr>'	-- Close off row for selected lookup.

				set @current = @current + 1
			end
			-----------------------------------------

			set @lookupHtml = @lookupHtml + '</tbody>'

			set @lookupHtml = @lookupHtml + '</table>'

			insert into @tbl values ('Items', @lookupHtml)
			------------------------------------------------------------------
		end;

		if @Type = 'Resource' OR @Type = 'ResourceType'
		begin
			-- BUILD Resource LIST HTML -----------------------------------------
			declare @resourceItemsHtml nvarchar(max)

			set @resourceItemsHtml = '<table class="hoverable bordered striped" style="width:100%"><thead>'
			set @resourceItemsHtml = @resourceItemsHtml + '<th style="margin-right: 15px">First Name</th><th style="margin-right: 15px">Last Name</th><th>Email</th>'
			set @resourceItemsHtml = @resourceItemsHtml + '</thead><tbody>'

			select		top 10 
						@resourceItemsHtml = @resourceItemsHtml + '<tr>' + 
											'<td>' + FirstName + '</td>' + 
											'<td>' + LastName + '</td>' + 
											'<td>' + Email + '</td>'
											+ '</tr>'
			from		reporting.Global_Resource
			order by	LastName, FirstName asc

			set @resourceItemsHtml = @resourceItemsHtml + '</tbody>'
			set @resourceItemsHtml = @resourceItemsHtml + '</table>'
 
			insert into @tbl values ('Items', @resourceItemsHtml)
			------------------------------------------------------------------
		end;

		if @Type = 'ReferenceItem'
		begin

			declare @myReferenceListID int

			select	@myReferenceListID = ReferenceItemTypeID from ReferenceItem where ID = @ID
			-- BUILD LIST HTML -----------------------------------------
			declare @referenceItemHtml nvarchar(max)

			set @referenceItemHtml = '<table class="hoverable bordered striped" style="width:100%">'
			set @referenceItemHtml = @referenceItemHtml + '<thead><th style="margin-right: 15px">Name</th></thead>'
			set @referenceItemHtml = @referenceItemHtml + '<tbody>'



			select		top 10 
						@referenceItemHtml = @referenceItemHtml + '<tr>' + '<td>' + DisplayValue + '</td>' + '</tr>'             
			from		ReferenceItem
			where		ReferenceItemTypeID = @myReferenceListID
			order by	DisplayValue desc

			set @referenceItemHtml = @referenceItemHtml + '</tbody>'
			set @referenceItemHtml = @referenceItemHtml + '</table>'
 
			insert into @tbl values ('Items', @referenceItemHtml)
			------------------------------------------------------------------
		end;

		if @Type = 'ReferenceItemType'
		begin

		--	declare @myReferenceListID int

			--select	@myReferenceListID = ReferenceItemTypeID from ReferenceItem where ID = @ID
			-- BUILD LIST HTML -----------------------------------------
			declare @referenceItemTypeHtml nvarchar(max)

			set @referenceItemTypeHtml = '<table class="hoverable bordered striped" style="width:100%">'
			set @referenceItemTypeHtml = @referenceItemTypeHtml + '<thead><th style="margin-right: 15px">Display Value</th></thead>'
			set @referenceItemTypeHtml = @referenceItemTypeHtml + '<tbody>'



			select		top 10 
						@referenceItemTypeHtml = @referenceItemTypeHtml + '<tr>' + '<td>' + DisplayValue + '</td>' + '</tr>'             
			from		ReferenceItem
			where		ReferenceItemTypeID = @ID
			order by	DisplayValue desc

			set @referenceItemTypeHtml = @referenceItemTypeHtml + '</tbody>'
			set @referenceItemTypeHtml = @referenceItemTypeHtml + '</table>'
 
			insert into @tbl values ('Items', @referenceItemTypeHtml)
			------------------------------------------------------------------
		end;

	end
	
	if @Action = 'None'
	begin
		set @html = '<h3>{Name}</h3><div>'
	end

	if @Action = 'Preview'
	begin
		set @html = '<h3>{Name} <small style="right: 5px;">{Type}</small></h3><div>{Description}</div>'
		set @showIcon = 0

		if @Type = 'Artifact'
		begin
			insert into @tbl
			select	'Status', [Status]
			from	Artifact
			where	ID = @ID

			insert into @tbl
			select	'Path', TextPath
			from	Artifact
			where	ID = @ID

			insert into @tbl 
				select 'GoverningDomain', tt.name
				from
					artifact a
					inner join taxonomytype tt on (a.taxonomytypeid = tt.id and a.id = @ID)

			set @html = @html + '<div><b>' + @SubjectName + ':</b> {GoverningDomain}</div>'
			set @html = @html + '<div><b>Status:</b> {Status}</div>'
			set @html = @html + '<div><b>Path:</b> {Path}</div>'

			set @hasDynamicFields = 1
		end;

		if @Type = 'Event'
		begin
			declare @so nvarchar(250)
			select	@so = SourceID, 
					@s = [Status]
			from	[Event]
			where	ID = @ID

			insert into @tbl values ('Status', @s)
			insert into @tbl values ('SourceID', @so)

			set @html = @html + '<div><b>Status:</b> {Status}</div>'
			set @html = @html + '<div><b>SourceID:</b> {SourceID}</div>'

			set @hasDynamicFields = 1
		end;

		if @Type = 'EventGroup'
		begin
			insert into @tbl
				select	'Key', PublicID
				from	EventGroup
				where	ID = @ID

			-- BUILD EVENT LIST HTML -----------------------------------------
			declare @events table (ID int, SourceID nvarchar(250), Status varchar(25))
			insert into @events 
				select top 10 ID, SourceID, Status from [Event] where EventGroupID = @ID order by ID desc
		
			declare @eventFieldTypes table (ID int identity, Name nvarchar(250))
			insert into @eventFieldTypes
				select FriendlyName from FieldType where [Object] = 'Rule' and ObjectID = @typeID order by SortOrder asc
			insert into @eventFieldTypes values ('Source ID')
			insert into @eventFieldTypes values ('Status')

			declare @eventHtml nvarchar(max)

			set @eventHtml = '<table class="hoverable bordered striped" style="width:100%">'

			-- Loop through field name list ---------
			set @eventHtml = @eventHtml + '<thead>'
			set		@current = 1
			select	@max = max(ID) from @eventFieldTypes
			while @current <= @max
			begin
				select	@name = Name
				from	@eventFieldTypes
				where	ID = @current

				set @eventHtml = @eventHtml + '<th>' + @name  + '</th>'

				set @current = @current + 1
			end
			set @eventHtml = @eventHtml + '</thead>'
			-----------------------------------------

			set @eventHtml = @eventHtml + '<tbody>'

			-- Loop through event list --------------
			select	@current = min(ID) from @events
			select	@max = max(ID) from @events

			while @current <= @max
			begin
				set @eventHtml = @eventHtml + '<tr>'	-- Open row for selected event.

				declare @eventFields table (Name nvarchar(250), Value nvarchar(4000))
			
				insert into @eventFields
					select		FriendlyName,
								FormattedValue
					from		FieldWithRelation
					where		ObjectType = 'Event' 
								and ObjectID = @current

					-- Loop through each field for this selected event --
					declare @fCurrent int,
							@fMax int
					set		@fCurrent = 1
					select	@fMax = max(ID) from @eventFieldTypes
					while @fCurrent <= @fMax
					begin
						select	@name = Name from @eventFieldTypes where ID = @fCurrent

						select @eventHtml = @eventHtml + '<td>' + coalesce(Value, '') + '</td>' from @eventFields where Name = @name

						set @fCurrent = @fCurrent + 1
					end
					-----------------------------------------------------

					select @eventHtml = @eventHtml	+ 
										'<td>' + [SourceID] + '</td>' + 
										'<td>' + [Status] + '</td>' 
					from	@events 
					where	ID = @current

				delete @eventFields

				set @eventHtml = @eventHtml + '</tr>'	-- Close off row for selected event.

				set @current = @current + 1
			end
			-----------------------------------------

			set @eventHtml = @eventHtml + '</tbody>'

			set @eventHtml = @eventHtml + '</table>'

			insert into @tbl values ('Items', @eventHtml)

			set @html = @html + '<div><b>Key:</b> {Key}</div>'
			set @html = @html + '<div>Items: {Items}</div>'
			------------------------------------------------------------------
		end;

		if @Type = 'Intersect'
		begin
			insert into @tbl
				select	'Classification',
						case Classification
							when 1 then 'Critical'
							else 'Normal'
						end
				from	[Intersect]
				where	ID = @ID

			set @html = @html + '<div><b>Classification:</b> {Classification}</div>'
		end;

		
		if @Type = 'Issue'
		begin
			insert into @tbl values('Name', '')
			insert into @tbl values('Description', '')
					
			if exists (select id from issue where id = @ID)
			begin			
				set @html = @html + '<div><b>Issue Type:</b> {IssueType}</div>'
				set @html = @html + '<div><b>Criticality:</b> {Criticality}</div>'
						
				insert into @tbl 
					select 'IssueType', it.name 
					from issuetype it inner join issue i on(i.issuetypeid = it.id) 
					where i.id = @ID

				insert into @tbl 
					select 'Criticality', case when i.Criticality = 0 then 'Negligible' when i.Criticality = 1 then 'Low' when i.Criticality = 2 then 'Medium' when i.Criticality = 3 then 'High'  when i.Criticality = 4 then 'Critical' else 'N/A' end
					from issuetype it inner join issue i on(i.issuetypeid = it.id) 
					where i.id = @ID

				set @hasDynamicFields = 1
			end			
		end;

		if @Type = 'Responsibility'
		begin
			select	@n = T.Name, 
					@t = T.Name,
					@d = T.[Description]
			from	Responsibility O
					inner join ResponsibilityType T on T.ID = O.ResponsibilityTypeID
			where	O.ID = @ID
			
			declare @contextsHtml nvarchar(max)

			set @contextsHtml = '<table class="hoverable bordered striped" style="width:100%">' + 
								'<thead><th>List</th><th>Code</th></thead>' + 
								'<tbody>' + 
								(
								select		(select D.Name as 'td' for xml path(''), type),
											(select I.Code as 'td' for xml path(''), type)
								from		ResponsibilityContextItem R
											inner join ReferenceItem I on R.ResponsibilityID = @ID and R.ObjectType = 'ReferenceItem' and I.ID = R.ObjectID
											inner join ReferenceItemType D on D.ID = I.ReferenceItemTypeID
								FOR XML RAW('tr'), ELEMENTS
								) +
								'</tbody>' + 
								'</table>'

			insert into @tbl values ('Name', @n)
			insert into @tbl values ('Type', @t)
			insert into @tbl values ('Description', @d)
			insert into @tbl values ('Contexts', @contextsHtml)

			set @html = @html + '<div><b>Contexts:</b> {Contexts}</div>'
		end;

		if @Type = 'Resource'
		begin
			--declare @e nvarchar(500), @fn nvarchar(250), @ln nvarchar(250)
			--select	@e = Email, @fn = FirstName, @ln = LastName
			--from	reporting.Global_Resource
			--where	ResourceID = @ID

			--insert into @tbl values ('Email', @e)
			--insert into @tbl values ('FirstName', @fn)
			--insert into @tbl values ('LastName', @ln)
			--insert into @tbl values ('Role', '')

			--set @html = @html + '<div><b>Email:</b> {Email}</div>'
			--set @html = @html + '<div><b>First Name:</b> {FirstName}</div>'
			--set @html = @html + '<div><b>Last Name:</b> {LastName}</div>'

			set @hasDynamicFields = 1
		end;

		if @Type = 'Rule'
		begin
			insert into @tbl
				select	'Name', Name
				from	[Rule] O
				where	ID = @ID
			insert into @tbl
				select	'Description', Description
				from	[Rule] O
				where	ID = @ID
			--insert into @tbl
			--	select	'Status', Status
			--	from	[Rule] O
			--	where	ID = @ID

			--set @html = @html + '<div><b>Status:</b> {Status}</div>'

			set @hasDynamicFields = 1
		end;

		if @Type = 'RuleDimension'
		begin
			insert into @tbl
				select	'Description', [Description]
				from	RuleDimension
				where	ID = @ID
			insert into @tbl
				select	'Name', [Name]
				from	RuleDimension
				where	ID = @ID

			--set @html = @html + '<div><b>Path:</b> {Description}</div>'
						
		end;

		if @Type = 'Taxonomy'
		begin
			insert into @tbl
				select	'TextPath', TextPath
				from	Taxonomy O
				where	ID = @ID

			set @html = @html + '<div><b>Path:</b> {TextPath}</div>'

			set @hasDynamicFields = 1
		end;

		if @Type = 'TaxonomyType'
		begin
			insert into @tbl
				select	'Name', Name
				from	TaxonomyType O
				where	ID = @ID

			set @hasDynamicFields = 1
		end;
		
		-- If required, get dynamic fields to add to list.
		if @hasDynamicFields = 1
		begin
			select	@html = @html + '<div><b>' + FriendlyName + '</b>: ' + '{' + Name + '}' + '</div>' 
			from	FieldWithRelation
			where	ObjectType = @Type
					and ObjectID = @ID
					and Name not in (select Name from @tbl)

			insert into @tbl
				select	Name,
						FormattedValue
				from	FieldWithRelation
				where	ObjectType = @Type
						and ObjectID = @ID
						and Name not in (select Name from @tbl)
		end;
	end

	if @Action = 'Statistics'
	begin
		set @html = '<h3>{Name}</h3><div>{Statistics}</div>'

		set @hasStats = case @Type
							when 'Artifact' then 1
							when 'Taxonomy' then 1
							else 0
						end

		-- If required, build statistics table
		if @hasStats = 1
		begin
			-- BUILD STATS LIST HTML -----------------------------------------
			declare @statsHtml nvarchar(max)

			declare @stats table (ID int identity, Name nvarchar(250), Score int)
			insert into @stats 
				select	T.Name,
						coalesce(S.SCore, 0) as Score
				from	StatisticType T
						outer apply (
									select	top 1
											*
									from	Statistic
									where	StatisticTypeID = T.ID
											and ObjectType = @Type
											and ObjectID = @ID
									order by DateStart desc
									) S
				where	T.[Object] = @Type + 'Type' 
						and T.ObjectID = @typeID
						and T.PartOfScore = 1

			set @statsHtml = '<table class="hoverable bordered striped" style="width:100%">'

			-- Loop through field name list ---------
			set @statsHtml = @statsHtml + '<tbody>'
			set		@current = 1
			select	@max = max(ID) from @stats
			while @current <= @max
			begin
				select	@statsHtml = @statsHtml + '<tr><td>' + Name  + '</td>' + '<td>' + cast(Score as varchar(5))  + ' Points</td></tr>'
				from	@stats
				where	ID = @current

				set @current = @current + 1
			end
			set @statsHtml = @statsHtml + '</tbody>'
			-----------------------------------------

			insert into @tbl values ('Statistics', @statsHtml)

			------------------------------------------------------------------
		end;
	end

	-- Replace the fields in the template with the appropriate text value.
	set		@current = 1
	select	@max = max(ID) from @tbl

	while @current <= @max
	begin
		select	@name = '{' + Name + '}',
				@value = COALESCE(Value, '')
		from	@tbl 
		where	ID = @current

		if @showIcon = 1
		begin
			if @name = '{Name}' and @icon is not null
			begin
				update	@tbl 
				set		Value = '<div class="pull-left" style="width: 30px">' + @icon + '</div>' + '<div class="pull-right">' + @value + '</div>'
				where	ID = @current
				--set @usedIconAlready = 1
			end
		end

		set @html = REPLACE(@html, @name, @value)

		set @current = @current + 1
	end

	--if @showIcon = 1 and @icon is not null
	--begin
	--	set @html = @icon + '<br/>' + @html
	--end

	-- Return the properly formatted values.
	select	'' as Title,
			@html as Body;
END
GO

ALTER PROCEDURE [dbo].[ProcessEagleMCToEagleFieldRelations]
	@StagingFileID int,
	@FusionID int
AS
BEGIN	
	SET NOCOUNT ON;
		
	declare	@eagleStreamID int,
			@streamToFieldIntersectTypeID int,				
			@currentEagleFusionId int;

	declare	@IDList Table(IntersectID int,StageID Int);

	declare	@Intersects IDTable;

	declare	@MessageStreamFussionAttributeID int,
			@EagleFieldFusionAttributeID int;

	select	@MessageStreamFussionAttributeID = 196;
	select	@EagleFieldFusionAttributeID = 205;

	-- load the stream that we want to add relations ships for    
	select	@eagleStreamID = fusionattributeid 
	from	[fusion].[stagingfile] 
	where	id = @StagingFileID and 
			fusionID = @FusionID;
			
	if @eagleStreamID is null
	begin
		raiserror('ERROR : UNABLE TO LOCATE SPECIFIED STREAM INFORMATION FOR INPUT FUSION ID / STAGING ID', 15, 1);
		return;
	end;

	select @currentEagleFusionId = FusionID from [dbo].[fusionattribute] where id = @eagleStreamID

	-- add relationships for Stream (196) to Eagle DB Columns (205)
	-- using star tag field that is a field for for fusionattribute type 205 lookup fields to add rels for
	-- todo pull to separate proc
	if @eagleStreamID is not null
	begin
			Declare @StreamToFieldList Table(FieldFusionAttributeID int, StreamFusionAttributeID int,IntersectTypeID int, ID int);
			
			-- load the intersect type ids
			select	@streamToFieldIntersectTypeID = ID
			from	IntersectType
			where	(Subject = 'FusionAttributeType' and Object = 'FusionAttributeType') 
					and	( 
						(SubjectID = @MessageStreamFussionAttributeID and ObjectID = @EagleFieldFusionAttributeID) OR
						(SubjectID = @EagleFieldFusionAttributeID and ObjectID = @MessageStreamFussionAttributeID)
						)

			if @streamToFieldIntersectTypeID is null
			begin
				raiserror('ERROR : UNABLE TO LOCATE INTERSECT TYPE IDS FOR EAGLE TO EAGLE MESSAGE STREAMS', 15, 1);
				return;
			end;

			-- insert into in memory table variable the values we want to add intersects for
			insert into @StreamToFieldList
				select		fa.id, 
							sf.FusionAttributeID, 
							@streamToFieldIntersectTypeID, 
							ROW_NUMBER() OVER (Order by fa.id) AS 'RowNumber'
				from		field f 
							inner join FusionAttribute fa on f.ObjectID = fa.ID and fa.fusionid = @currentEagleFusionId
							inner join FieldType ft on f.fieldtypeid = ft.id
							inner join fusion.StagingFileItem sfi on sfi.tag = f.value				
							inner join fusion.StagingFile sf on sfi.stagingfileid = sf.id
							left join	(
										select	SubjectID,
												ObjectID,
												1 as hasExisting
										from	[Intersect]
										where	Subject = 'FusionAttribute' and Object= 'FusionAttribute'
										) existing on ( (existing.SubjectID = sf.FusionAttributeID and existing.ObjectID = fa.ID) OR (existing.SubjectID = fa.ID and existing.ObjectID = sf.FusionAttributeID) )
				where		fa.fusionattributetypeid = @EagleFieldFusionAttributeID and 
							ft.name = 'startag' and 
							sfi.stagingfileid = @StagingFileID and 
							existing.hasExisting is null
				group by	fa.id, sf.FusionAttributeID  -- grouping is used to eliminate duplicate star tag relations

			--insert intersect records and save there id's
			-- trick is to use merge to keep the sequence id and staging row ids
			-- http://stackoverflow.com/questions/15614261/using-output-clause-to-insert-value-not-in-inserted
			MERGE
				INTO    [Intersect] d
				USING   (
							SELECT	sr.IntersectTypeID, 
									2 as class,
									--sr.ID as srID,
									'FusionAttribute' as Subject,
									sr.StreamFusionAttributeID as SubjectID,
									'FusionAttribute' as Object,
									sr.FieldFusionAttributeID as ObjectID
							FROM	@StreamToFieldList sr							
						) s
				ON      (
						s.IntersectTypeID = d.IntersectTypeID 
						and s.Subject = d.Subject and s.SubjectID = d.SubjectID 
						and s.Object = d.Object and s.ObjectID = d.ObjectID
						)
				WHEN NOT MATCHED THEN
				INSERT  (IntersectTypeID, Classification, Description, Subject, SubjectID, Object, ObjectID)
				VALUES  (s.IntersectTypeID, s.class, NULL, s.Subject, s.SubjectID, s.Object, s.ObjectID);
				--OUTPUT  INSERTED.ID, s.srID into @IDList;
	end;
end
GO

ALTER PROCEDURE [fusion].[ProcessEagleMCToBBMnemonic]
	@StagingFileID int,
	@FusionID int
AS
BEGIN	
	SET NOCOUNT ON;
		
	declare		@eagleStreamID int,
				@streamToFieldIntersectTypeID int;

	declare		@IDList Table(IntersectID int,StageID Int);

	declare		@Intersects IDTable;

	declare		@MessageStreamFussionAttributeID int = 196,
				@BloombergMnemonicFusionID int = 301;
				
	-- load the stream that we want to add relations ships for    
	select @eagleStreamID = fusionattributeid from [fusion].[stagingfile] where id = @StagingFileID and fusionID = @FusionID
		
	if @eagleStreamID is null
	begin
		raiserror('ERROR : UNABLE TO LOCATE SPECIFIED STREAM INFORMATION FOR INPUT FUSION ID / STAGING ID', 15, 1);
		return;
	end;

	-- add relationships for Stream (196) to Eagle DB Columns (205)
	-- using star tag field that is a field for for fusionattribute type 205 lookup fields to add rels for
	-- todo pull to separate proc
	if @eagleStreamID is not null
	begin
			Declare @StreamToFieldList Table(FieldFusionAttributeID int, StreamFusionAttributeID int, IntersectTypeID int, ID int);
			
			-- load the intersect type ids
			select	@streamToFieldIntersectTypeID = ID
			from	[IntersectType]
			where	Subject = 'FusionAttributeType' and 
					Object = 'FusionAttributeType' and 
					(
						( SubjectID = @MessageStreamFussionAttributeID and ObjectID = @BloombergMnemonicFusionID ) OR
						( SubjectID = @BloombergMnemonicFusionID and ObjectID = @MessageStreamFussionAttributeID )
					)

			if @streamToFieldIntersectTypeID is null
			begin
				raiserror('ERROR : UNABLE TO LOCATE INTERSECT TYPE IDS FOR EAGLE TO EAGLE MESSAGE STREAMS', 15, 1);
				return;
			end;

			-- insert into in memory table variable the values we want to add intersects for
			insert into @StreamToFieldList
				select		fa.id, 
							sf.FusionAttributeID, 
							@streamToFieldIntersectTypeID, 
							ROW_NUMBER() OVER (Order by fa.id) AS 'RowNumber'
				from		fusionAttribute fa
							inner join [fusion].[StagingFileItem] sfi on (sfi.value = fa.name)				
							inner join [fusion].[StagingFile] sf on (sfi.stagingfileid = sf.id)
							left join [Intersect] I on	I.IntersectTypeID = @streamToFieldIntersectTypeID and 
														I.Subject = 'FusionAttribute' and 
														I.Object ='FusionAttribute' and
														(
															( SubjectID = sf.FusionAttributeID and ObjectID = fa.ID ) OR
															( SubjectID = fa.ID and ObjectID = sf.FusionAttributeID )
														)
					where		fa.fusionattributetypeid = @BloombergMnemonicFusionID and 
								sfi.stagingfileid = @StagingFileID and 
								I.ID is null
					group by	fa.id, sf.FusionAttributeID  -- grouping is used to eliminate duplicate star tag relations

			MERGE
				INTO    [Intersect] d
				USING   (
							SELECT	IntersectTypeID, 
									--ID,
									'FusionAttribute' as Subject,
									StreamFusionAttributeID as SubjectID,
									'FusionAttribute' as Object,
									FieldFusionAttributeID as ObjectID
							FROM	@StreamToFieldList							
						) s
				ON      (
						s.IntersectTypeID = d.IntersectTypeID 
						and s.Subject = d.Subject and s.SubjectID = d.SubjectID 
						and s.Object = d.Object and s.ObjectID = d.ObjectID
						)
				WHEN NOT MATCHED THEN
					INSERT  (IntersectTypeID, Classification, Description, Subject, SubjectID, Object, ObjectID)
					VALUES  (s.IntersectTypeID, 2, NULL, s.Subject, s.SubjectID, s.Object, s.ObjectID);
				--OUTPUT  INSERTED.ID, s.ID into @IDList;
										
			--insert into @Intersects 
			--	select idl.intersectid from @IDList idl;
			
			--declare @IntersectCount int
			--select @IntersectCount = count(1) from @Intersects
			
			--if @IntersectCount > 0 
			--begin				
			--	EXEC cache.SynchronizeRelationships @Intersects
			--end
	end;
end
GO

ALTER PROCEDURE [fusion].[ProcessEagleMCToBloombergRelations]	
	@StagingFileID int,
	@FusionID int
AS
BEGIN	
	SET NOCOUNT ON;
	
	
	declare		@eagleStreamID int;				
	declare		@IntersectCount int;
	Declare		@IDList Table(IntersectID int,StageID Int);
	declare		@Intersects IDTable;
	declare		@fieldToBBIntersectTypeID int;

	-- load the panel that we want to add relations ships for
    
	select @eagleStreamID = fusionattributeid from [fusion].[stagingfile] where id = @StagingFileID and fusionID = @FusionID
	
	if @eagleStreamID is null
	begin
		raiserror('ERROR : UNABLE TO LOCATE SPECIFIED STREAM INFORMATION FOR INPUT FUSION ID / STAGING ID', 15, 1);
		return;
	end;
			
	exec ProcessEagleMCToEagleFieldRelations @StagingFileID, @FusionID

	exec [fusion].[ProcessEagleMCToBBMnemonic] @StagingFileID, @FusionID


	-- add relations for Eagle Field (205) to Bloomberg mnemonic (301)
	if @eagleStreamID is not null
	begin
		Declare @BBToFieldList Table(FieldFusionAttributeID int, StreamFusionAttributeID int, IntersectTypeID int, ID int);
		
		-- load the intersect id's for message stream to bb mnemonic	

		select	@fieldToBBIntersectTypeID = ID
			from	[IntersectType]
			where	Subject = 'FusionAttributeType' and 
					Object = 'FusionAttributeType' and 
					(
						( SubjectID = 205 and ObjectID = 301 ) OR
						( SubjectID = 301 and ObjectID = 205 )
					)

		if @fieldToBBIntersectTypeID is null
		begin
			raiserror('ERROR : UNABLE TO LOCATE INTERSECT TYPE IDS FOR EAGLE TO BLOOMBERG INTERSECT', 15, 1);
			return;
		end

		-- load into memory the id's that we need to add intersects for
		insert into @BBToFieldList
			select	fa.id as 'fieldID', faBB.id as 'bbID', @fieldToBBIntersectTypeID, ROW_NUMBER() OVER (Order by sfi.id) AS 'RowNumber'
			from	field f 
					inner join fusionAttribute fa on (f.ObjectID = fa.ID)
					inner join fieldtype ft on (f.fieldtypeid = ft.id)
					inner join [fusion].[StagingFileItem] sfi on (sfi.tag = f.value)				
					inner join [fusion].[StagingFile] sf on (sfi.stagingfileid = sf.id)						
					inner join fusionAttribute faBB on (faBB.Name = sfi.value and faBB.fusionattributetypeid = 301)		
					left join [Intersect] I on	I.IntersectTypeID = @fieldToBBIntersectTypeID and 
												I.Subject = 'FusionAttribute' and 
												I.Object ='FusionAttribute' and
												(
													( I.SubjectID = faBB.ID and I.ObjectID = fa.ID ) OR
													( I.SubjectID = fa.ID and I.ObjectID = faBB.ID )
												)
			where	fa.fusionattributetypeid = 205 and 
					ft.name = 'startag' and 
					sfi.stagingfileid = @StagingFileID and 
					I.ID is null;

			MERGE
				INTO    [Intersect] d
				USING   (
							SELECT	IntersectTypeID, 
									--ID,
									'FusionAttribute' as Subject,
									StreamFusionAttributeID as SubjectID,
									'FusionAttribute' as Object,
									FieldFusionAttributeID as ObjectID
							FROM	@BBToFieldList
						) s
				ON      (
						s.IntersectTypeID = d.IntersectTypeID 
						and s.Subject = d.Subject and s.SubjectID = d.SubjectID 
						and s.Object = d.Object and s.ObjectID = d.ObjectID
						)
				WHEN NOT MATCHED THEN
				INSERT  (IntersectTypeID, Classification, Subject, SubjectID, Object, ObjectID)
				VALUES  (s.IntersectTypeID, 2, 'FusionAttribute', s.SubjectID, 'FusionAttribute', s.ObjectID);
				--OUTPUT  INSERTED.ID, s.ID into @IDList;										

			--insert into @Intersects 
			--	select idl.intersectid from @IDList idl;
						
			--select @IntersectCount = count(1) from @Intersects
			--if @IntersectCount > 0 
			--begin
			--	EXEC cache.SynchronizeRelationships @Intersects
			--end
	end;
END
GO


alter procedure [fusion].[ProcessFusionCacheInQueue]
--declare
	@FusionID int
--set @FusionID = 15
as
begin
	SET NOCOUNT, ANSI_PADDING ON;
	SET ANSI_WARNINGS ON;

	UPDATE  FusionAttribute
	SET		TextPath = utility.GetBreadcrumbStringWrapper('FusionAttribute', ID, '.')
	FROM	FusionAttribute 
	WHERE	FusionID = @FusionID and deleted = 0

end
GO

alter procedure [utility].[GetArtifactsUpForCertification]
as
begin
	set nocount on;
	declare @artifactTypes table (RowID int identity, ID int)
	declare @subjectAreas table (RowID int identity, ID int)

	-- loop control variables
	declare @current int,
			@max int

	-- certification loop instance variables
	declare @wt int = 2,
			@id int,
			@start datetime,
			@end datetime,
			@months int,
			@days int,
			@calculationDate datetime,
			@difMonths int,
			@calculationDateMinusDaysBefore date,
			@lastStartDate datetime,
			@minDate datetime = '1900-01-01 00:00:00.000',
			@DateFieldExists bit = 0,
			@currentDate datetime = CAST(CAST(year(getutcdate()) AS varchar) + '-' + CAST(month(getutcdate()) AS varchar) + '-' + CAST(day(getutcdate()) AS varchar) AS DATETIME)

	-- 1. CHECK ARTIFACT TYPES -------------------------------------
	-- get the artifact types that need to be checked
	insert into @artifactTypes
		select	T.ID
		from	ArtifactType T
				inner join WorkflowTypeRelation R on R.[Object] = 'ArtifactType' and R.ObjectID = T.ID and R.WorkflowType = @wt and R.[Enabled] = 1

--select * from @artifactTypes

	set @current = 1
	select @max = MAX(RowID) from @artifactTypes
	while @current <= @max
	begin
		-- set to default
		set @start = null
		set @end = null
		set @months = null
		set @days = null

		select @id = ID from @artifactTypes where RowID = @current

		select	@start = Fields.value('(/fields/CertificationStartDate)[1]', 'datetime'),
				@end = Fields.value('(/fields/CertificationEndDate)[1]', 'datetime'),
				@months = Fields.value('(/fields/MonthsUntilCertification)[1]', 'int'),
				@days = Fields.value('(/fields/DaysGivenToCompleteCertification)[1]', 'int'),
				@calculationDate = Fields.value('(/fields/DateForScheduleCalculation)[1]', 'datetime'),
				@lastStartDate = Fields.value('(/fields/CertificationStartDate)[1]', 'datetime')
		from	WorkflowTypeRelation
		where	[Object] = 'ArtifactType' and ObjectID = @id and WorkflowType = @wt

		if @end is null
		begin
			set @end = @minDate
		end

--select DATEADD(d, -60, '2015-07-31 00:00:00.000')

		set @calculationDateMinusDaysBefore = DATEADD(d, -@days, @calculationDate)
		select @difMonths = DATEDIFF(mm, @calculationDateMinusDaysBefore, getutcdate())

--select	@id as ArtifactTypeID,
--		@calculationDate as CalculationDate,
--		@calculationDateMinusDaysBefore as CalculationDateMinusDaysBefore,
--		@difMonths as NumMonthsSinceLastCertification,
--		@months as NumMonthsBetweenCertifications,
--		@lastStartDate as LastStartDate

		if ((@difMonths >= @months) and (DATEDIFF(mm, @end, getutcdate()) >= @months) OR @lastStartDate is null) --or (@difMonths % @months = 0)
		begin
			set @start = CAST(CAST(year(getutcdate()) AS varchar) + '-' + CAST(month(getutcdate()) AS varchar) + '-' + CAST(day(@calculationDateMinusDaysBefore) AS varchar) AS DATETIME) --CONVERT(date, getutcdate())
			set @end = CAST(CAST(year(getutcdate()) AS varchar) + '-' + CAST(month(getutcdate()) AS varchar) + '-' + CAST(day(@calculationDate) AS varchar) AS DATETIME) --DATEADD(d, @days, CONVERT(date, getutcdate()))
--select @start, @end, DATEDIFF(d, @start, @end)

			if DATEDIFF(d, @start, @end) < @days
			begin
				set @start = @currentDate
				set @end = DATEADD(d, @days, @currentDate)
			end

			select	@DateFieldExists = Fields.exist('fields/CertificationStartDate')
			from	WorkflowTypeRelation
			where	[Object] = 'ArtifactType' and ObjectID = @id and WorkflowType = @wt

--select @start, @end
--select @DateFieldExists as DateFieldExists

			if @DateFieldExists = 1
			begin
				update	WorkflowTypeRelation
				set		Fields.modify('delete (/fields/CertificationStartDate)')
				where	[Object] = 'ArtifactType' and ObjectID = @id and WorkflowType = @wt
			end

			update	WorkflowTypeRelation
			set		Fields.modify('insert <CertificationStartDate>{sql:variable("@start")}</CertificationStartDate> into (/fields)[1]')
			where	[Object] = 'ArtifactType' and ObjectID = @id and WorkflowType = @wt

			select	@DateFieldExists = Fields.exist('fields/CertificationEndDate')
			from	WorkflowTypeRelation
			where	[Object] = 'ArtifactType' and ObjectID = @id and WorkflowType = @wt

			if @DateFieldExists = 1
			begin
				update	WorkflowTypeRelation
				set		Fields.modify('delete (/fields/CertificationEndDate)')
				where	[Object] = 'ArtifactType' and ObjectID = @id and WorkflowType = @wt
			end

			update	WorkflowTypeRelation
			set		Fields.modify('insert <CertificationEndDate>{sql:variable("@end")}</CertificationEndDate> into (/fields)[1]')
			where	[Object] = 'ArtifactType' and ObjectID = @id and WorkflowType = @wt
		end

		-- Increment
		set @current = @current + 1
	end

	-- 2. CHECK VOCABULARIES ---------------------------------------
	-- get the vocabularies that need to be checked
	insert into @subjectAreas
		select	T.ID
		from	TaxonomyType T
				inner join WorkflowTypeRelation R on R.[Object] = 'TaxonomyType' and R.ObjectID = T.ID and R.WorkflowType = @wt and R.[Enabled] = 1

	set @current = 1
	select @max = MAX(RowID) from @subjectAreas
	while @current <= @max
	begin
	--	-- set to default
		set @start = null
		set @end = null
		set @months = null
		set @days = null
	
		select @id = ID from @subjectAreas where RowID = @current

		select	@start = Fields.value('(/fields/CertificationStartDate)[1]', 'datetime'),
				@end = Fields.value('(/fields/CertificationEndDate)[1]', 'datetime'),
				@months = Fields.value('(/fields/MonthsUntilCertification)[1]', 'int'),
				@days = Fields.value('(/fields/DaysGivenToCompleteCertification)[1]', 'int'),
				@calculationDate = Fields.value('(/fields/DateForScheduleCalculation)[1]', 'datetime'),
				@lastStartDate = Fields.value('(/fields/CertificationStartDate)[1]', 'datetime')
		from	WorkflowTypeRelation
		where	[Object] = 'TaxonomyType' and ObjectID = @id and WorkflowType = @wt
	
		if @months is not null and @days is not null
		begin
			if @end is null
			begin
				set @end = @minDate
			end

		set @calculationDateMinusDaysBefore = DATEADD(d, -@days, @calculationDate)
		select @difMonths = DATEDIFF(mm, @calculationDateMinusDaysBefore, getutcdate())
		
		if ((@difMonths >= @months) and (DATEDIFF(mm, @end, getutcdate()) >= @months) OR @lastStartDate is null)
			begin
				set @start = CAST(CAST(year(getutcdate()) AS varchar) + '-' + CAST(month(getutcdate()) AS varchar) + '-' + CAST(day(@calculationDateMinusDaysBefore) AS varchar) AS DATETIME)
				set @end = CAST(CAST(year(getutcdate()) AS varchar) + '-' + CAST(month(getutcdate()) AS varchar) + '-' + CAST(day(@calculationDate) AS varchar) AS DATETIME)

				if DATEDIFF(d, @start, @end) < @days
				begin
					set @start = @currentDate
					set @end = DATEADD(d, @days, @currentDate)
				end

				select	@DateFieldExists = Fields.exist('fields/CertificationStartDate')
				from	WorkflowTypeRelation
				where	[Object] = 'TaxonomyType' and ObjectID = @id and WorkflowType = @wt

				if @DateFieldExists = 1
				begin
					update	WorkflowTypeRelation
					set		Fields.modify('delete (/fields/CertificationStartDate)')
					where	[Object] = 'TaxonomyType' and ObjectID = @id and WorkflowType = @wt
				end

				update	WorkflowTypeRelation
				set		Fields.modify('insert <CertificationStartDate>{sql:variable("@start")}</CertificationStartDate> into (/fields)[1]')
				where	[Object] = 'TaxonomyType' and ObjectID = @id and WorkflowType = @wt

				select	@DateFieldExists = Fields.exist('fields/CertificationEndDate')
				from	WorkflowTypeRelation
				where	[Object] = 'TaxonomyType' and ObjectID = @id and WorkflowType = @wt

				if @DateFieldExists = 1
				begin
					update	WorkflowTypeRelation
					set		Fields.modify('delete (/fields/CertificationEndDate)')
					where	[Object] = 'TaxonomyType' and ObjectID = @id and WorkflowType = @wt
				end

				update	WorkflowTypeRelation
				set		Fields.modify('insert <CertificationEndDate>{sql:variable("@end")}</CertificationEndDate> into (/fields)[1]')
				where	[Object] = 'TaxonomyType' and ObjectID = @id and WorkflowType = @wt
			end
		end

		-- Increment
		set @current = @current + 1
	end

	-- 3. CHECK ARTIFACTS ------------------------------------------
--declare @wt int =2
	select	A.ID as ArtifactID,
--A.ArtifactTypeID,
--W.DateStarted,
			coalesce(V.Fields.value('(/fields/CertificationStartDate/text())[1]', 'datetime'), T.Fields.value('(/fields/CertificationStartDate/text())[1]', 'datetime')) as CertificationStartDate,
			coalesce(V.Fields.value('(/fields/CertificationEndDate/text())[1]', 'datetime'), T.Fields.value('(/fields/CertificationEndDate/text())[1]', 'datetime')) as CertificationEndDate
	from	Artifact A
			left join WorkflowTypeRelation T on T.[Object] = 'ArtifactType' and T.ObjectID = A.ArtifactTypeID and T.WorkflowType = @wt and T.[Enabled] = 1  and T.Parent is null and T.ParentID is null
			left join WorkflowTypeRelation V on V.[Object] = 'ArtifactType' and V.ObjectID = A.ArtifactTypeID and V.WorkflowType = @wt and V.[Enabled] = 1 and V.Parent = 'TaxonomyType' and V.ParentID = A.TaxonomyTypeID
			outer apply (
						select	max(DateStarted) as DateStarted
						from	Workflow
						where	artifactID = A.ID
								--and DateCompleted is null
						) W
	where	(
				W.DateStarted is null
				or
				(
					W.DateStarted is not null 
					and
					DATEDIFF(m, W.DateStarted, 
						coalesce(V.Fields.value('(/fields/CertificationStartDate/text())[1]', 'datetime'), T.Fields.value('(/fields/CertificationStartDate/text())[1]', 'datetime'))
					) > 0
				)
			)
			and
			(
				A.DateLastCertified is null 
				--or A.DateLastCertified < coalesce(V.Fields.value('(/fields/CertificationStartDate)[1]', 'datetime'), T.Fields.value('(/fields/CertificationStartDate)[1]', 'datetime'))
				or 
				(
					A.DateLastCertified is not null
					and DATEDIFF(m, 
						A.DateLastCertified, 
						coalesce(V.Fields.value('(/fields/CertificationStartDate/text())[1]', 'datetime'), T.Fields.value('(/fields/CertificationStartDate/text())[1]', 'datetime'))
					) > coalesce(V.Fields.value('(/fields/MonthsUntilCertification/text())[1]', 'int'), T.Fields.value('(/fields/MonthsUntilCertification/text())[1]', 'int'))
					and A.Status = 'Certified'
				)
				or A.Status <> 'Certified'
			)
			and A.Status <> 'Archived'
			and coalesce(V.Fields.value('(/fields/CertificationStartDate/text())[1]', 'datetime'), T.Fields.value('(/fields/CertificationStartDate/text())[1]', 'datetime')) is not null
			and A.ID not in (
							select	artifactid
							from	Workflow
							where	WorkflowType = @wt 
									and Data.value('(/fields/StartDate/text())[1]', 'datetime') between 
											coalesce(V.Fields.value('(/fields/CertificationStartDate/text())[1]', 'datetime'), T.Fields.value('(/fields/CertificationStartDate/text())[1]', 'datetime'))
											and coalesce(V.Fields.value('(/fields/CertificationEndDate/text())[1]', 'datetime'), T.Fields.value('(/fields/CertificationEndDate/text())[1]', 'datetime'))
							)
			and A.ID not in (
							select	ArtifactID
							from	Workflow
							where	WorkflowType = @wt 
									and DateCompleted is null
							)
			and A.ID not in (
							select	ArtifactID
							from	Workflow
							where	WorkflowType = @wt 
									and DATEDIFF(m, 
											DateStarted, 
											coalesce(V.Fields.value('(/fields/CertificationStartDate/text())[1]', 'datetime'), T.Fields.value('(/fields/CertificationStartDate/text())[1]', 'datetime'))
										) > coalesce(V.Fields.value('(/fields/MonthsUntilCertification/text())[1]', 'int'), T.Fields.value('(/fields/MonthsUntilCertification/text())[1]', 'int'))
							)
			and A.ID in (
						select	RD.ObjectID 
						from	[cache].[Responsibilities] RD
								left join WorkflowTypeRelation WTR_V on WTR_V.[Object] = 'ArtifactType' and WTR_V.ObjectID = RD.ObjectTypeID and WTR_V.WorkflowType = @wt and WTR_V.[Enabled] = 1 and WTR_V.ResponsibilityTypeID = RD.ResponsibilityTypeID and WTR_V.Parent = 'TaxonomyType' and WTR_V.ParentID = A.TaxonomyTypeID
								left join WorkflowTypeRelation WTR_T on WTR_T.[Object] = 'ArtifactType' and WTR_T.ObjectID = RD.ObjectTypeID and WTR_T.WorkflowType = @wt and WTR_T.[Enabled] = 1 and WTR_T.ResponsibilityTypeID = RD.ResponsibilityTypeID and WTR_T.Parent is null and WTR_T.ParentID is null
						where	RD.[Object] = 'Artifact' 
								and coalesce(WTR_V.ID, WTR_T.ID) is not null
						)

end
GO

CREATE TABLE [dbo].[FusionSchedule] (
    [FusionID]    INT      NOT NULL,
    [Day]         INT      NOT NULL,
    [Time]        TIME (7) NOT NULL,
    [FullRefresh] BIT      CONSTRAINT [DF_FusionSchedule_FullRefresh] DEFAULT ((0)) NOT NULL,
    [CreatedOn]   DATETIME NULL,
    [CreatedBy]   INT      NULL,
    [UpdatedOn]   DATETIME NULL,
    [UpdatedBy]   INT      NULL,
    CONSTRAINT [PK_FusionSchedule] PRIMARY KEY CLUSTERED ([FusionID] ASC, [Day] ASC, [Time] ASC),
    CONSTRAINT [FK_FusionSchedule_Fusion] FOREIGN KEY ([FusionID]) REFERENCES [dbo].[Fusion] ([ID]) ON DELETE CASCADE
);
GO

CREATE PROCEDURE GetReferenceItemValues	
	@listid int	
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
	
	-- load the fields for this item
	select id, 'Field' + cast(id as varchar(100)) as [Name] into #fieldtypes from fieldtype where object = 'ReferenceItemType' and objectid = @listid order by sortorder
	
	DECLARE @tsqlSelect nvarchar(max);
	DECLARE @tsqlFrom nvarchar(max);
	DECLARE @tsqlWhere nvarchar(max);
	DECLARE @tsql nvarchar(max);

	set @tsqlSelect = 'select ri.id as [ID] ,ri.code as [Code]';
	set @tsqlFrom = ' from [dbo].[referenceitem] ri';
	set @tsqlWhere = ' where ri.referenceitemtypeid = ' + cast(@listid as nvarchar(20));
	

	DECLARE @id int;
	DECLARE @index int = 0;
	DECLARE @name nvarchar(250);

	-- generate dynamic sql for each field
	DECLARE cur CURSOR FOR SELECT id, name FROM #fieldtypes
	OPEN cur

	FETCH NEXT FROM cur INTO @id, @name

	WHILE @@FETCH_STATUS = 0 BEGIN
		
		SET @tsqlSelect = @tsqlSelect + ',f'+ cast(@index as nvarchar(10)) + '.formattedvalue as [' + @name + ']';
		SET @tsqlFrom = @tsqlFrom + ' left outer join [dbo].[field] f' + cast(@index as nvarchar(10)) + ' on (ri.id = f' + cast(@index as nvarchar(10)) + '.objectid and f' + cast(@index as nvarchar(10)) + '.[objecttype] = ''ReferenceItem'' and f' + cast(@index as nvarchar(10)) + '.fieldtypeid = ' + cast(@id as nvarchar(20)) + ')';

		SET @index = @index + 1;
		FETCH NEXT FROM cur INTO @id, @name
	END

	CLOSE cur    
	DEALLOCATE cur

	SET @tsql = @tsqlSelect + @tsqlFrom + @tsqlWhere;
	--print @tsql
	EXEC sp_executesql @tsql;

END
GO


--add owner columns used by markit lineage
alter table mapruleitem add [Owner] varchar(100) null;
go

alter table mapitem add [Owner] varchar(100) null;
go

alter table mapruleitemmapitem add [Owner] varchar(100) null;
go

alter table [intersect] add [Owner] varchar(100) null;
go


-- Remove the unused xml nullable column path from fusionattribute table, its not used anywhere and just makes the tables rows bigger
ALTER TABLE fusionattribute DROP COLUMN [path]
go


CREATE INDEX IX_MapRuleItem_SourceFusionAttributeID_TargetFusionAttributeID ON [dbo].[MapRuleItem] (SourceFusionAttributeID asc, TargetFusionAttributeID asc); 
go