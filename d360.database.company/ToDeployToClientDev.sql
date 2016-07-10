--add column ReportType default to legacy for powerbi/legacy reports
alter table Report add  [ReportType] Varchar(25) CONSTRAINT [DF_Report_ReportType] DEFAULT (('legacy')) NOT NULL
-- add column GUID
alter table Report add  [PowerBIReportID] Varchar(50) NULL
alter table Report add  [PowerBIDatasetID] Varchar(50) NULL

alter table MapItem add [DiagramKey]  VARCHAR (25) NULL
GO

alter table MapRuleItem add [FusionAttributeID] INT NOT NULL
go
alter table MapRuleItem add [IsSource] BIT NOT NULL
go
alter table MapRuleItem add [CreatedBy] INT NOT NULL
go
alter table MapRuleItem add [CreatedOn] DATETIME NOT NULL
go
alter table MapRuleItem add [UpdatedBy]  INT NOT NULL
go
alter table MapRuleItem add [UpdatedOn] DATETIME NOT NULL
go
alter table MapRuleItem drop column [SourceFusionAttributeID]
go
alter table MapRuleItem drop column [TargetFusionAttributeID]
go

CREATE NONCLUSTERED INDEX [IX_FusionAttribute_FusionID_TextPath] ON [dbo].[FusionAttribute]([FusionID] ASC, [TextPath] ASC);
GO

alter view [dbo].[IntersectTypeDetail]
as
	select	IT.ID,
			IT.Subject,
			IT.SubjectID,
			case IT.Subject
				when 'IntersectType' then utility.DeriveIntersectTypeName(SIT.ID)
				when 'GroupType' then 'Group'
				when 'ResourceType' then 'Resource'
				else coalesce(SAT.Name, SDT.Name, SFT.TextPath, SPT.Name, SRT.Name, STT.Name) 
			end as SubjectName,
			coalesce(SIcon.IconBackColor, '#000') as SubjectIconBackColor,
			coalesce(SIcon.IconForeColor, '#fff') as SubjectIconForeColor,
			coalesce(SIcon.IconText, substring(coalesce(SAT.Name, SDT.Name, SFT.Name, SPT.Name, SRT.Name, STT.Name, ''), 1, 2)) as SubjectIconText,
			
			IT.Object,
			IT.ObjectID,
			case IT.Object
				when 'IntersectType' then utility.DeriveIntersectTypeName(OIT.ID)
				when 'GroupType' then 'Group'
				when 'ResourceType' then 'Resource'
				else coalesce(OAT.Name, ODT.Name, OFT.TextPath, OPT.Name, ORT.Name, OTT.Name) 
			end as ObjectName,
			coalesce(OIcon.IconBackColor, '#000') as ObjectIconBackColor,
			coalesce(OIcon.IconForeColor, '#fff') as ObjectIconForeColor,
			--coalesce(OIcon.IconText, 'leaf') as ObjectIconText,
			coalesce(OIcon.IconText, substring(coalesce(OAT.Name, ODT.Name, OFT.Name, OPT.Name, ORT.Name, OTT.Name, ''), 1, 2)) as ObjectIconText,

			IT.PredicateID,
			P.Name as [PredicateName],
			P.Type as PredicateType,
			
			coalesce(IT.IsSystem, cast(0 as bit)) as IsSystem
	from	IntersectType IT with(nolock) 
			left join [Predicate] P with(nolock) on P.ID = IT.PredicateID 

			left join dbo.ArtifactType SAT with(nolock)			on IT.Subject = 'ArtifactType'			and SAT.ID = IT.SubjectID
			left join dbo.DomainType SDT with(nolock)			on IT.Subject = 'DomainType'			and SDT.ID = IT.SubjectID
			left join dbo.FusionAttributeType SFT with(nolock)	on IT.Subject = 'FusionAttributeType'	and SFT.ID = IT.SubjectID
			left join dbo.IntersectType SIT with(nolock)		on IT.Subject = 'IntersectType'			and SIT.ID = IT.SubjectID
			left join dbo.PolicyType SPT with(nolock)			on IT.Subject = 'PolicyType'			and SPT.ID = IT.SubjectID
			left join (
				select 1 as ID, 'Informational' as Name
				union
				select 2 as ID, 'Quality Check' as Name
				union
				select 3 as ID, 'Metric' as Name
				union
				select 4 as ID, 'Profile' as Name
			) SRT												on IT.Subject = 'RuleType'				and SRT.ID = IT.SubjectID 
			left join dbo.TaxonomyType STT with(nolock)			on IT.Subject = 'TaxonomyType'			and STT.ID = IT.SubjectID


			left join dbo.ArtifactType OAT with(nolock)			on IT.Object = 'ArtifactType'			and OAT.ID = IT.ObjectID
			left join dbo.DomainType ODT with(nolock)			on IT.Object = 'DomainType'				and ODT.ID = IT.ObjectID
			left join dbo.FusionAttributeType OFT with(nolock)	on IT.Object = 'FusionAttributeType'	and OFT.ID = IT.ObjectID
			left join dbo.IntersectType OIT with(nolock)		on IT.Object = 'IntersectType'			and OIT.ID = IT.ObjectID
			left join dbo.PolicyType OPT with(nolock)			on IT.Object = 'PolicyType'				and OPT.ID = IT.ObjectID
			left join (
				select 1 as ID, 'Informational' as Name
				union
				select 2 as ID, 'Quality Check' as Name
				union
				select 3 as ID, 'Metric' as Name
				union
				select 4 as ID, 'Profile' as Name
			) ORT												on IT.Object = 'RuleType'				and ORT.ID = IT.ObjectID
			left join dbo.TaxonomyType OTT with(nolock)			on IT.Object = 'TaxonomyType'			and OTT.ID = IT.ObjectID

			left join ObjectStyle SIcon with(nolock) on SIcon.ObjectType = IT.Subject and SIcon.ObjectID =	IT.SubjectID
			left join ObjectStyle OIcon with(nolock) on OIcon.ObjectType = IT.Object and OIcon.ObjectID = IT.ObjectID
	where	coalesce(SAT.ID, SDT.ID, SFT.ID, SPT.ID, SRT.ID, STT.ID) is not null
			and coalesce(OAT.ID, ODT.ID, [OFT].ID, OPT.ID, ORT.ID, OTT.ID) is not null
GO

alter procedure [dbo].[AddSingleIntersect]
	@ResourceID int,
	@IntersectTypeID int,
	@Subject varchar(50),			-- The start object type.
	@SubjectID int,					-- The start object ID.
	@Object varchar(50),			-- The end object type.
	@ObjectID int,					-- The end object ID.	
	@Classification int,
	@Description nvarchar(4000)
as
begin
	set nocount on;

	declare @Date datetime = getutcdate(),
			@ErrorMessage nvarchar(2500),
			@IntersectID int,
			@SubjectIntersectTypeNodeID int,
			@SubjectIntersectNodeID int,
			@ObjectIntersectTypeNodeID int,
			@ObjectIntersectNodeID int

	select	@IntersectID = I.ID,
			@SubjectIntersectTypeNodeID = N1.IntersectTypeNodeID,	@SubjectIntersectNodeID = N1.ID,
			@ObjectIntersectTypeNodeID = N2.IntersectTypeNodeID,	@ObjectIntersectNodeID = N2.ID
	from	[Intersect] I
			inner join IntersectNode N1 on N1.IntersectID = I.ID and N1.ObjectType = @Subject and N1.ObjectID = @SubjectID
			inner join IntersectNode N2 on N2.IntersectID = I.ID and N2.ObjectType = @Object and N2.ObjectID = @ObjectID

	if @IntersectID is not null and @IntersectID > 0
		begin
			-- Update

			update	[Intersect]
			set		Classification = @Classification,
					Description = @Description
			where	ID = @IntersectID

			--exec utility.AddAuditEntry 'Intersect', @IntersectID, @ResourceID, @Date, 'Updated', 'Intersect', @IntersectID
		end
	else
		begin
			-- Create

			declare @SubjectType varchar(50),
					@SubjectTypeID int,
					@ObjectType varchar(50),
					@ObjectTypeID int

			select	@SubjectType = ObjectType, @SubjectTypeID = ObjectTypeID	from cache.[Object] where [Object] = @Subject and ObjectID = @SubjectID 
			select	@ObjectType = ObjectType, @ObjectTypeID = ObjectTypeID		from cache.[Object] where [Object] = @Object and ObjectID = @ObjectID 

			select	distinct 
					@SubjectIntersectTypeNodeID = SourceIntersectTypeNodeID, 
					@ObjectIntersectTypeNodeID = TargetIntersectTypeNodeID
			from	utility.RelationshipTypes R 
			where	SourceObjectType = @SubjectType and SourceObjectID = @SubjectTypeID 
					and TargetObjectType = @ObjectType and TargetObjectID = @ObjectTypeID

			if @SubjectIntersectTypeNodeID is not null and @ObjectIntersectTypeNodeID is not null
				begin
					INSERT INTO [Intersect] (
						IntersectTypeID, 
						Classification, 
						[Description],
						[Subject], SubjectID,
						[Object], ObjectID,
						CreatedBy, CreatedOn,
						UpdatedBy, UpdatedOn				
					) 
					VALUES (
						@IntersectTypeID, 
						@Classification, 
						@Description,
						@Subject, @SubjectID,
						@Object, @ObjectID,
						@ResourceID, @Date,
						@ResourceID, @Date
					)

					SELECT @IntersectID = SCOPE_IDENTITY()

					INSERT INTO IntersectNode	(IntersectTypeNodeID, IntersectID, ObjectType, ObjectID) 
					VALUES						(@SubjectIntersectTypeNodeID, @IntersectID, @Subject, @SubjectID)

					SELECT @SubjectIntersectNodeID = SCOPE_IDENTITY()

					INSERT INTO IntersectNode	(IntersectTypeNodeID, IntersectID, ObjectType, ObjectID)
					VALUES						(@ObjectIntersectTypeNodeID, @IntersectID, @Object, @ObjectID)

					SELECT @ObjectIntersectNodeID = SCOPE_IDENTITY()

					insert into cache.[Object] ( [Object], [ObjectID], [ObjectType], [ObjectTypeID] )
					values	( 'Intersect', @IntersectID, 'IntersectType', @IntersectTypeID );

					insert into cache.Relationship ( IntersectID, SourceIntersectTypeNodeID, SourceIntersectNodeID, SourceObject, SourceObjectID, TargetIntersectTypeNodeID, TargetIntersectNodeID, TargetObject, TargetObjectID )
					values	( @IntersectID, @SubjectIntersectTypeNodeID, @SubjectIntersectNodeID, @Subject, @SubjectID, @ObjectIntersectTypeNodeID, @ObjectIntersectNodeID, @Object, @ObjectID );
					insert into cache.Relationship ( IntersectID, SourceIntersectTypeNodeID, SourceIntersectNodeID, SourceObject, SourceObjectID, TargetIntersectTypeNodeID, TargetIntersectNodeID, TargetObject, TargetObjectID )
					values	( @IntersectID, @ObjectIntersectTypeNodeID, @ObjectIntersectNodeID, @Object, @ObjectID, @SubjectIntersectTypeNodeID, @SubjectIntersectNodeID, @Subject, @SubjectID );

					--Update the responsibilities of the object that should inherit form the other (Taxonomy can push relationships down to artifact)
					if ( (@Subject = 'Taxonomy' and @Object = 'Artifact') OR (@Subject = 'Artifact' and @Object = 'Taxonomy') )
						begin
							if @Subject = 'Artifact'
							begin
								exec [cache].[SynchronizeResponsibilitiesForObject] @Subject, @SubjectID
							end
							if @Object = 'Artifact'
							begin
								exec [cache].[SynchronizeResponsibilitiesForObject] @Object, @ObjectID
							end
						end

					--exec utility.AddAuditEntry @Subject, @SubjectID, @ResourceID, @Date, 'Created', 'Intersect', @IntersectID
					--exec utility.AddAuditEntry @Object, @ObjectID, @ResourceID, @Date, 'Created', 'Intersect', @IntersectID
				end
		end

	select * from [Intersect] where ID = @IntersectID
end
GO

CREATE FUNCTION GetFusionAttributesByOwningArtifact
(
	@ArtifactID int
)
RETURNS 
@tbl TABLE 
(
	ID int
)
AS
BEGIN
		declare @h table (ID int);

		with h as	(
					select	ID,
							ParentID
					from	Artifact
					where	ID = @ArtifactID
					union all
					select	P.ID,
							P.ParentID
					from	Artifact P
							inner join h as C on C.ParentID = P.ID
					)
		insert into @h
			select ID from h;
	
		--with fa as	(
		--			select	A.ID,
		--					A.ParentID
		--			from	FusionAttributeOwnerRule R
		--					inner join FusionAttributeOwnerRuleItem RI on RI.FusionAttributeOwnerRuleID = R.ID and R.RelationshipOwnerObjectType = 'Artifact'
		--					inner join @h H on H.ID = R.RelationshipOwnerObjectID
		--					inner join FusionAttribute A on (
		--													(RI.FusionAttributeID is not null and A.ID = RI.FusionAttributeID) OR 
		--													(RI.FusionAttributeID is null and A.FusionAttributeTypeID = R.ObjectID)
		--													)
		--													AND A.FusionID = R.FusionID
		--			union all
		--			select	C.ID,
		--					C.ParentID
		--			from	FusionAttribute C
		--					inner join fa P on C.ParentID = P.ID
		--			)

		with f as	(
					select	R.FusionID
					from	FusionAttributeOwnerRule R
							inner join @h H on H.ID = R.RelationshipOwnerObjectID and R.RelationshipOwnerObjectType = 'Artifact'
					)

		--INSERT INTO @tbl
		--	SELECT	ID
		--	FROM	fa

		INSERT INTO @tbl
			SELECT	distinct
					FusionID
			FROM	f
	
	RETURN 
END
GO

CREATE procedure [dbo].[GetLineage]
--declare 
	@type varchar(50),
	@id int

--set @type = 'Artifact'
--set @id = 4651;
as
begin
	declare @items table (
		IntersectID int, IntersectTypeID int,
		SubjectTypeName nvarchar(500), SubjectName nvarchar(500), Subject varchar(50), SubjectID int, SubjectIconBackColor varchar(7), SubjectIconForeColor varchar(7), 
		ObjectTypeName nvarchar(500), ObjectName nvarchar(500), Object varchar(50), ObjectID int, 
		MapID int, MapItemID int, Transformation nvarchar(4000), IntersectRoleID int, IntersectRole nvarchar(250),
		IsSource bit, [DiagramKey] varchar(25)
	)
	
	-- get all items directly tied to the focal object.
	insert into @items
		select	MI.IntersectID,
				I.IntersectTypeID,
				
				I.SubjectTypeName,
				I.SubjectName,
				I.Subject,
				I.SubjectID,
				I.SubjectIconBackColor,
				I.SubjectIconForeColor,

				I.ObjectTypeName,
				I.ObjectName,
				I.Object,
				I.ObjectID,
				
				MI.MapID,
				MI.ID as MapItemID,
				M.Transformation,
				M.IntersectRoleID,
				IR.Name,
				MI.IsSource,
				MI.[DiagramKey]
		from	[IntersectDetail] I
				inner join MapItem MI on MI.IntersectID = I.ID 
											and ( 
												(I.Subject = @type and I.SubjectID = @id) 
												OR (I.Object = @type and I.ObjectID = @id) 
												)
				inner join Map M on M.ID = MI.MapID
				left join IntersectRole IR on IR.ID = M.IntersectRoleID

	-- get all items not directly tied to the focal object, but still tied to maps involved above.
	insert into @items
		select	MI.IntersectID,
				I.IntersectTypeID,
				
				I.SubjectTypeName,
				I.SubjectName,
				I.Subject,
				I.SubjectID,
				I.SubjectIconBackColor,
				I.SubjectIconForeColor,

				I.ObjectTypeName,
				I.ObjectName,
				I.Object,
				I.ObjectID,

				MI.MapID,
				MI.ID as MapItemID,
				M.Transformation,
				M.IntersectRoleID,
				IR.Name,
				MI.IsSource,
				MI.[DiagramKey]
		from	[IntersectDetail] I
				inner join MapItem MI on MI.IntersectID = I.ID
				inner join Map M on M.ID = MI.MapID
				left join IntersectRole IR on IR.ID = M.IntersectRoleID
				inner join @items IT on IT.MapID = MI.MapID and MI.ID not in (select MapItemID from @items)
	
	select	(
			select	S.MapID as id,
					S.DiagramKey as 'from',
					S.IntersectID as 'fromIntersectId',
					T.DiagramKey as 'to',
					T.IntersectID as 'toIntersectId',
					S.IntersectRole as 'role', 
					S.intersectRoleId, 
					0 as mappingRuleCount, 
					S.Transformation as transformation,
					S.intersectTypeId
			from	@items S
					inner join @items T on T.MapID = S.MapID and T.MapItemID <> S.MapItemID and S.IsSource = 1 and T.IsSource = 0
			for json path			
			) as 'links',
			(
			select	I1.DiagramKey as [key],
					I1.Subject as [obj],
					I1.SubjectID as [objid], 
					I1.Subject as [type],
					I1.Subject as objecttype,
					I1.SubjectID as objecttypeid, 
					I1.SubjectTypeName as typeName,
					I1.SubjectName as name,
					I1.SubjectIconBackColor as back,
					I1.SubjectIconForeColor as fore,
					I1.IntersectID as intersectId,
					0 as sourceRuleCount,
					0 as mappingRuleCount,
					C.challengeCount,
					0 as openEventCount,
					I.openIssueCount,
					(
					select	MapID,
							MapItemID
					from	@items
					where	DiagramKey = I1.DiagramKey
					for json auto
					) as mapItems
			from	(
					select	distinct
							IntersectID,
							SubjectTypeName,
							SubjectName,
							Subject,
							SubjectID,
							SubjectIconBackColor,
							SubjectIconForeColor,
							ObjectName,
							Object,
							ObjectID,
							DiagramKey
					from	@items 
					) I1
					cross apply (
									select count(1) as challengeCount     
									from Workflow W            			                          
									where W.WorkflowType = 4 and W.Data.exist('/fields/ArtifactID[text() = sql:column("I1.SubjectID")]') = 1 and W.DateCompleted is null   
								) C
					cross apply (
									select count(1) as openIssueCount   
									from Workflow W            			                          
									where W.WorkflowType = 3 and W.Data.exist('/fields/ArtifactID[text() = sql:column("I1.SubjectID")]') = 1 and W.DateCompleted is null   
								) I
		for json path			
			) as 'nodes'
	for json path, WITHOUT_ARRAY_WRAPPER
end
GO

ALTER PROCEDURE [fusion].[Rules]
AS
BEGIN
	SET NOCOUNT ON;

	declare @d datetime = getutcdate(),
			@r int = 0,
			@RuleID int,
			@FusionID int,
			@FusionAttributeID int,
			@ExecutionID int,
			@NumberOfRules int,			
			@NumberOfNewTaxonomies int,
			@NumberOfNewDomainItems int,
			@NumberOfNewDomains int,
			@NumberOfNewArtifacts int,
			@NumberOfAttributesTotal int,
			@NumberOfNewRelations int,
			@promotionNeedsToRun bit
	

	set	@NumberOfRules = 0;	
	set @NumberOfNewTaxonomies = 0;
	set @NumberOfNewDomainItems = 0;
	set @NumberOfNewDomains = 0;
	set @NumberOfNewArtifacts = 0;
	set @promotionNeedsToRun = 0;

	--First check if there is anything to do

	--EXEC @promotionNeedsToRun = [utility].[ShouldPromotionRun]

	--if(@promotionNeedsToRun <= 0)
	--BEGIN
	--	PRINT 'NO REASON TO RUN THE PROMOTION RULES WAS DETECTED';
	--	return;
	--END;


	--Log this run get a new id from the fusion.promotion table
	--insert into [dbo].[FusionAttributePromotionLogSummary] ( DateStarted ) values ( CURRENT_TIMESTAMP)
	--select @ExecutionID =  SCOPE_IDENTITY()

	IF OBJECT_ID('tempdb..#rules') IS NOT NULL
		DROP TABLE #rules;

	create table #rules (
		ID int identity,
		RuleID int,
		FusionID int,
		ObjectType varchar(25),
		ObjectID int,
		FilterFusionAttributeID int,
		FilterFusionAttributeTypeID int
	);

	IF OBJECT_ID('tempdb..#attributes') IS NOT NULL
		DROP TABLE #attributes;

	create table #attributes (
		ID int identity,
		RuleID int,
		RuleStepID int,
		[Action] varchar(25),
		FusionAttributeID int
	);

	IF OBJECT_ID('tempdb..#fields') IS NOT NULL
		DROP TABLE #fields;

	create table #fields (
		ID int, 
		RuleID int,
		RuleStepID int,
		SourceFieldName nvarchar(250), 
		SourceFieldTypeID int, 
		TargetFieldName nvarchar(250), 
		TargetFieldTypeID int, 
		Value nvarchar(4000)
	);

	IF OBJECT_ID('tempdb..#fieldValues') IS NOT NULL
		DROP TABLE #fieldValues;

	create table #fieldValues (
		ObjectType varchar(50), 
		ObjectID int, 
		FieldTypeID int, 
		Value nvarchar(4000)
	);
	
	insert into #rules
		select	R.ID,
				R.FusionID,
				R.ObjectType,
				R.ObjectID,
				I.FusionAttributeID as FilterFusionAttributeID,
				coalesce(A.FusionAttributeTypeID, R.ObjectID) as FilterFusionAttributeTypeID
		from	[fusion].[Rule] R
				inner join [fusion].[RuleItem] I on I.RuleID = R.ID and R.[Enabled] = 1
				left join FusionAttribute A on A.ID = I.FusionAttributeID


	
	declare	@currentID int,
			@maxID int

	set		@currentID = 1
	select	@maxID = MAX(ID) from #rules

	select @NumberOfRules = count(1) from #rules;

	--BEGIN: Determine the target fusion attributes to promote.
	while (@currentID <= @maxID)
	begin
		declare @FusionObjectType varchar(25),
				@FusionObjectID int,
				@FilterFusionAttributeID int,
				@FilterFusionAttributeTypeID int


		select	@RuleID = RuleID,
				@FusionObjectType = ObjectType,
				@FusionObjectID = ObjectID,
				@FusionID = FusionID,
				@FilterFusionAttributeID = FilterFusionAttributeID,
				@FilterFusionAttributeTypeID = FilterFusionAttributeTypeID
		from	#rules
		where	ID = @currentID

		if @FusionObjectID = @FilterFusionAttributeTypeID AND @FilterFusionAttributeID is not null
			begin
				-- You are on a specific nodes of same type.  Just copy to target table.
				insert into #attributes 
					select	@RuleID, 
							S.ID,
							S.[Action],
							@FilterFusionAttributeID
					from	[fusion].[RuleStep] S
					where	S.RuleID = @RuleID
					order by S.Step
			end
		else
			begin
				-- You are on an attribute higher up in hierarchy.
				if @FilterFusionAttributeID is null
					begin
						--  If there is NO filtered attribute ID, then you need to get every attribute in system for the partiular fusion instance.
						insert into #attributes
							select	@RuleID, 
									S.ID,
									S.[Action],
									FA.ID
							from	FusionAttribute FA 
									inner join [fusion].[RuleStep] S on S.RuleID = @RuleID and FA.FusionID = @FusionID and FA.FusionAttributeTypeID = @FusionObjectID
									left join #attributes A on A.FusionAttributeID = FA.ID and A.RuleID = S.RuleID and A.ID is null
							order by FA.ID, S.Step
					end
				else
					begin
						-- If there is a filter attribute ID, then traverse the hierarchy and get all attributes of the specified type.
						with FA as	(
									select	ID,
											ParentID,
											FusionAttributeTypeID
									from	FusionAttribute
									where	ID = @FilterFusionAttributeID
											and FusionID = @FusionID
									union all
									select	C.ID,
											C.ParentID,
											C.FusionAttributeTypeID
									from	FusionAttribute C
											inner join fa P on C.ParentID = P.ID --and P.ID <> C.ID
									)
	
						insert into #attributes
							select	@RuleID, 
									S.ID,
									S.[Action],
									FA.ID
							from	FA 
									inner join [fusion].[RuleStep] S on S.RuleID = @RuleID and FA.FusionAttributeTypeID = @FusionObjectID
									left join #attributes A on A.FusionAttributeID = FA.ID and A.RuleID = S.RuleID and A.ID is null
							where	FA.FusionAttributeTypeID = @FusionObjectID
							order by FA.ID, S.Step
					end
			end

		set @currentID = @currentID + 1
	end --end while loop
	--END: Determine the target fusion attributes to promote.

	-- Load field values we are working with, first starting with the Name.
	insert into #fields
		select	A.ID,
				RS.RuleID,
				M.RuleStepID,
				M.SourceFieldName,
				M.SourceFieldTypeID,
				M.TargetFieldName,
				M.TargetFieldTypeID,
				case 
					when M.SourceFieldName = 'Name' then FA.Name					
					when M.IsConstantValue = 1 then M.ConstantValue
				end				
		from	[fusion].[RuleStepMapping] M
				inner join [fusion].[RuleStep] RS on M.RuleStepID = RS.ID
				inner join #attributes A on A.RuleID = RS.RuleID
				inner join FusionAttribute FA on FA.ID = A.FusionAttributeID 

	
	-- Update the fields table above with values for all dynamic fields.
	update	T
	set		T.Value = S.Value
	from	#fields T
			inner join #attributes A on A.ID = T.ID
			inner join Field S on S.ObjectType = 'FusionAttribute' and S.ObjectID = A.FusionAttributeID and S.FieldTypeID = T.SourceFieldTypeID 

--BEGIN: TESTING ---------------------------------------
/*
select * from #rules
select * from #attributes
select * from #fields
select * from FusionAttributePromotion where RuleID = 6

select * from IntersectMap where ID = 1424
select * from IntersectNode where ID = 720728
select * from [Intersect] where ID = 362728
delete FusionAttributePromotion where RuleID = 34
select	A.ID,
		R.RuleID,
		R.FusionID,
		R.ObjectID as FusionAttributeTypeID,
		R.PromotionObjectType,
		R.PromotionObjectID,
		R.PromotionParentObjectType,
		R.PromotionParentObjectID,
		A.FusionAttributeID
from	#rules R
		inner join #attributes A on A.RuleID = R.RuleID
*/
--END: TESTING ------------------------------------------

	set		@currentID = 1
	select	@maxID = MAX(ID) from #attributes

	set @NumberOfAttributesTotal = @maxID;
	
	while (@currentID <= @maxID)
	begin
		begin try

			declare @FusionAttributeTypeID int = null,
					@RuleStepID int = null,
					@Action varchar(25) = null,
					@ResultObject varchar(50) = null,
					@ResultObjectID int = null

			declare @fields table (SourceFieldName nvarchar(250), SourceFieldTypeID int, TargetFieldName nvarchar(250), TargetFieldTypeID int, Value nvarchar(4000))
			declare @settings table (Name nvarchar(100), Value nvarchar(250))
			
			select	@RuleID = R.RuleID,
					@RuleStepID = A.RuleStepID,
					@Action = A.[Action],
					@FusionID = R.FusionID,
					@FusionAttributeTypeID = R.ObjectID,
					@FusionAttributeID = A.FusionAttributeID,
					@ResultObject = P.ObjectType,
					@ResultObjectID = P.ObjectID
			from	#rules R
					inner join #attributes A on A.RuleID = R.RuleID and A.ID = @currentID
					left join [Fusion].RulePromotion P on P.FusionAttributeID = A.FusionAttributeID and P.RuleID = R.RuleID and P.RuleStepID = A.RuleStepID

			delete from @fields -- clear out previous fields
			--Load fields were are working with for this loop instance.
			insert into @fields
				select SourceFieldName, SourceFieldTypeID, TargetFieldName, TargetFieldTypeID, Value from #fields where ID = @currentID and RuleStepID = @RuleStepID

			delete from @settings -- clear out previous settings
			--Load settings were are working with for this loop instance.
			insert into @settings
				select Name, Value from [fusion].[RuleStepSetting] RSS inner join [fusion].[RuleStep] RS on (RSS.RuleStepID = RS.ID) where RS.RuleID = @RuleID and RS.ID = @RuleStepID
				
			--BEGIN: Promote action
			if @Action = 'Promote'
			begin
				declare @ObjectTypeToPromoteTo varchar(50) = null,
						@ObjectTypeIDToPromoteTo int = null,
						@ParentObjectSearchType nvarchar(250) = null,
						@ParentSearchObject varchar(50) = null,
						@ParentSearchObjectID int = null,
						@ParentObject varchar(50) = null,
						@ParentObjectID int = null

				select	@ObjectTypeToPromoteTo		= Value from @settings where Name = 'Object'
				select	@ObjectTypeIDToPromoteTo	= Value from @settings where Name = 'ObjectID'
				select	@ParentObjectSearchType		= Value from @settings where Name = 'ParentObjectSearch'
				select	@ParentSearchObject			= Value from @settings where Name = 'ParentObject'
				select	@ParentSearchObjectID		= Value from @settings where Name = 'ParentObjectID'

				if exists(select 1 from @fields where TargetFieldName = 'Name')
				begin
					declare @code nvarchar(50) = null,
							@name nvarchar(250) = null,
							@description nvarchar(4000) = null

					select @code = Value from @fields where TargetFieldName = 'Code'
					select @name = Value from @fields where TargetFieldName = 'Name'
					select @description = coalesce(Value, '') from @fields where TargetFieldName = 'Description'

					--BEGIN: Find parent based on search type
					if @ParentObjectSearchType = 'Direct'
					begin
						set @ParentObject = @ParentSearchObject
						set @ParentObjectID = @ParentSearchObjectID
					end

					if @ParentObjectSearchType = 'FusionOwner'
					begin
						select	@ParentObject = RelationshipOwnerObjectType,
								@ParentObjectID = RelationshipOwnerObjectID
						from	FusionAttributeOwnerRule
						where	@ParentSearchObject = 'Owner'
								and FusionID = @FusionID
								and ID = @ParentSearchObjectID
					end

					if @ParentObjectSearchType = 'ResultFromStep'
					begin
						select	@ParentObject = ObjectType,
								@ParentObjectID = ObjectID
						from	[fusion].[RulePromotion]
						where	@ParentSearchObject = 'Step'
								and RuleID = @RuleID
								and RuleStepID = @ParentSearchObjectID
								and FusionAttributeID = @FusionAttributeID
					end
					--END: Find parent based on search type

					print @ParentObject
					print @ParentObjectID

					--BEGIN: Determine object type to promote as
					if @ObjectTypeToPromoteTo = 'ArtifactType'
					begin
						set @ResultObject = 'Artifact'

						if @ResultObjectID is null
						begin
							select	@ResultObjectID = ID
							from	Artifact
							where	ArtifactTypeID = @ObjectTypeIDToPromoteTo
									and lower(Name) = lower(@name)
						end

						declare @modelTypeID int
						select @modelTypeID = min(ID) from TaxonomyType

						if @ResultObjectID is null
						begin
							if @ParentObjectID = 0
							begin
								set @ParentObjectID = null
							end

							insert into Artifact ( ParentID, ArtifactTypeID, TaxonomyTypeID, Name, Description, Status, UpdatedOn, UpdatedBy )
							values ( @ParentObjectID, @ObjectTypeIDToPromoteTo, @modelTypeID, @name, @description, 'Draft', getutcdate(), 0 )

							select @ResultObjectID =  SCOPE_IDENTITY()
							set @NumberOfNewArtifacts = @NumberOfNewArtifacts +1;
						end
						else
						begin
							declare @testArtifactName nvarchar(250) = null,
									@testArtifactDescription nvarchar(4000) = null,
									@testArtifactParentID int = null,
									@testArtifactTaxonomyTypeID int = null

							select	@testArtifactName = Name,
									@testArtifactDescription = Description,
									@testArtifactParentID = ParentID,
									@testArtifactTaxonomyTypeID = TaxonomyTypeID
							from	Artifact
							where	ID = @ResultObjectID

							if (@testArtifactName <> @name) 
								OR (@testArtifactDescription <> @description) 
								OR (@testArtifactParentID <> @ParentObjectID) 
								OR (@testArtifactTaxonomyTypeID <> @modelTypeID)
							begin
								update	Artifact
								set		Name = @name,
										Description = @description,
										ParentID = @ParentObjectID,
										TaxonomyTypeID = @modelTypeID
								where	ID = @ResultObjectID
							end
						end
					end
					--END: IF ArtifactType

					if @ObjectTypeToPromoteTo = 'DomainType'
					begin
						if @ParentObject is null and @ParentObjectID is null
							begin
								set @ResultObject = 'Domain'
									
								-- You are promoting to a Domain (creating a list)
								if @ResultObjectID is null
									begin
										select	@ResultObjectID = ID
										from	Domain
										where	DomainTypeID = @ObjectTypeIDToPromoteTo
												and lower(Name) = lower(@name)
									end
 
								if @ResultObjectID is null
									begin
										insert into Domain  ( DomainTypeID, Name, Description ) 
										values ( @ObjectTypeIDToPromoteTo, @name, @description )

										select @ResultObjectID =  SCOPE_IDENTITY()

										set @NumberOfNewDomains = @NumberOfNewDomains +1;
									end
								else
									begin
										update	Domain
										set		Name = @name,
												Description = @description
										where	ID = @ResultObjectID
									end
							end
						else
							begin
								-- You are promoting domain items to a specific domain (list)
								set @ResultObject = 'DomainItem'

								if @ResultObject is null and @ResultObjectID is null
									begin
										select	@ResultObjectID = ID
										from	DomainItem
										where	DomainID = @ParentObjectID
												and lower(Code) = lower(@code)
									end
 
								if @ResultObjectID is not null
									begin
										update	DomainItem
										set		Name = @name,
												Code = coalesce(@code, @name),
												Description = @description
										where	ID = @ResultObjectID
									end
								else
									begin
										insert into DomainItem ( DomainID, Name, Code, Description )
										values ( @ParentObject, @name, coalesce(@code, @name), @description )

										select @ResultObjectID =  SCOPE_IDENTITY()

										set @NumberOfNewDomainItems = @NumberOfNewDomainItems +1;
									end
							end
					end
					--END: IF DomainType

					if @ObjectTypeToPromoteTo = 'TaxonomyType'
					begin
						set @ResultObject = 'Taxonomy'

						if @ResultObjectID is null
							begin
								select	@ResultObjectID = ID
								from	Taxonomy
								where	TaxonomyTypeID = @ObjectTypeIDToPromoteTo
										and ParentID = @ParentObjectID
										and lower(Name) = lower(@name)
							end

						if @ResultObjectID is null
							begin
								insert into Taxonomy	( ParentID, TaxonomyTypeID, Name, Description )
								values					( @ParentObjectID, @ObjectTypeIDToPromoteTo, @name, @description )

								select @ResultObjectID =  SCOPE_IDENTITY()

								set @NumberOfNewTaxonomies = @NumberOfNewTaxonomies +1;
							end
						else
							begin
								update	Taxonomy
								set		Name = @Name,
										Description = @Description--,
										--ParentID = @PromotionParentObjectID
								where	ID = @ResultObjectID
 							end
					end
					--END: IF TaxonomyType

					--END: Determine object type to promote as

				end -- END: Check to see if Target Field called NAME is present

			end --END: Promote action

			--BEGIN: Find Action
			if @Action = 'Find'
			begin
				declare @FindSearchType nvarchar(250) = null,
						@FindSearchObject varchar(50) = null,
						@FindSearchObjectID int = null,
						@FindFilterField int = null,
						@FindFilterFieldValue nvarchar(250) = null,
						@FindTargetField int = null,
						@FindParent int = null

				select	@FindSearchType			= Value from @settings where Name = 'ObjectSearch'
				select	@FindSearchObject		= Value from @settings where Name = 'Object'
				select	@FindSearchObjectID		= Value from @settings where Name = 'ObjectID'
				select	@FindFilterField		= Value from @settings where Name = 'FilterField'
				select	@FindTargetField		= Value from @settings where Name = 'TargetField'
				select	@FindParent		= Value from @settings where Name = 'FindParent'
																
				if @FindSearchType = 'Fusion'
				begin					
					if @FindFilterField > 0
					begin
						select	@FindFilterFieldValue = Value
						from	@fields
						where	SourceFieldTypeID = @FindFilterField
					end
					else
					begin
						select	@FindFilterFieldValue = Value
						from	@fields
						where	SourceFieldName = 'Name'
					end
					
					if @FindFilterFieldValue is not null
					begin
						select	top 1
								@ResultObject = 'FusionAttribute',
								@ResultObjectID = ID
						from	FusionAttribute
						where	@FindSearchObject = 'FusionAttributeType'
								and FusionAttributeTypeID = @FindSearchObjectID
								and (TextPath = @FindFilterFieldValue or Name = @FindFilterFieldValue)
					end

				end

				--BEGIN: Find based on search type
				if @FindSearchType = 'FusionOwner'
				begin
					select	@ResultObject = RelationshipOwnerObjectType,
							@ResultObjectID = RelationshipOwnerObjectID
					from	FusionAttributeOwnerRule
					where	@FindSearchObject = 'Owner'
							and FusionID = @FusionID
							and ID = @FindSearchObjectID
				end

				if @FindSearchType = 'Glossary'					
				begin									
					if @FindFilterField > 0
					begin
						select	@FindFilterFieldValue = Value
						from	@fields
						where	SourceFieldTypeID = @FindFilterField
					end
					else
					begin
						select	@FindFilterFieldValue = Value
						from	@fields
						where	SourceFieldName = 'Name'	
						
											
					end
									

					if @FindFilterFieldValue is not null
					begin
						if @FindSearchObject = 'ArtifactType' and  ( @FindTargetField is null or @FindTargetField <= 0)
						begin							
							select	top 1
									@ResultObject = 'Artifact',
									@ResultObjectID = ID
							from	Artifact
							where	ArtifactTypeID = @FindSearchObjectID
									and (TextPath = @FindFilterFieldValue or Name = @FindFilterFieldValue)
						end

						if @FindSearchObject = 'ArtifactType' and @FindTargetField > 0
						begin							
							select	top 1
									@ResultObject = 'Artifact',
									@ResultObjectID = a.ID
							from	Artifact a
									inner join field f on(a.ID = f.ObjectID and f.Objecttype = 'Artifact' and f.fieldtypeid = @FindTargetField)
							where	a.ArtifactTypeID = @FindSearchObjectID									
									and (f.FormattedValue = @FindFilterFieldValue)
						end

						if @FindSearchObject = 'TaxonomyType'
						begin
							select	top 1
									@ResultObject = 'Taxonomy',
									@ResultObjectID = ID
							from	Taxonomy
							where	TaxonomyTypeID = @FindSearchObjectID
									and (TextPath = @FindFilterFieldValue or Name = @FindFilterFieldValue)
						end
					end

--select @ResultObjectID
				end

				if @FindSearchType = 'ResultFromStep' and @FindParent is not null
				begin
					select	@ResultObject = co.parent,
							@ResultObjectID = co.parentid
					from	[fusion].[RulePromotion] rp
						inner join [cache].[objectdetails] co on(co.[object] = rp.objecttype and co.objectid = rp.objectid)
					where	@FindSearchObject = 'Step'
							and rp.RuleID = @RuleID
							and rp.RuleStepID = @FindSearchObjectID
							and rp.FusionAttributeID = @FusionAttributeID
				end

				if @FindSearchType = 'ResultFromStep' and @FindParent is null
				begin
					select	@ResultObject = ObjectType,
							@ResultObjectID = ObjectID
					from	[fusion].[RulePromotion]
					where	@FindSearchObject = 'Step'
							and RuleID = @RuleID
							and RuleStepID = @FindSearchObjectID
							and FusionAttributeID = @FusionAttributeID
				end
				--END: Find based on search type
			end --END: Find Action
			
			--BEGIN: Lineage Action
			if @Action = 'Lineage'
			begin
				declare @IntersectTypeID int = null,
						@SubjectSearchType nvarchar(250) = null,
						@SubjectSearchObject varchar(50) = null,
						@SubjectSearchObjectID int = null,
						@Subject varchar(50) = null,
						@SubjectID int = null,
						@ObjectSearchType nvarchar(250) = null,
						@ObjectSearchObject varchar(50) = null,
						@ObjectSearchObjectID int = null,
						@Object varchar(50) = null,
						@ObjectID int = null,
						@FocalSearchType nvarchar(250) = null,
						@FocalSearchObject varchar(50) = null,
						@FocalSearchObjectID int = null,
						@Focal varchar(50) = null,
						@FocalID int = null,
						@PredicateID int = null,
						@IntersectID int = null

				select	@IntersectTypeID			= Value from @settings where Name = 'IntersectType'
				select	@SubjectSearchType			= Value from @settings where Name = 'SubjectSearch'
				select	@SubjectSearchObject		= Value from @settings where Name = 'Subject'
				select	@SubjectSearchObjectID		= Value from @settings where Name = 'SubjectID'
				select	@ObjectSearchType			= Value from @settings where Name = 'ObjectSearch'
				select	@ObjectSearchObject			= Value from @settings where Name = 'Object'
				select	@ObjectSearchObjectID		= Value from @settings where Name = 'ObjectID'
				select	@FocalSearchType			= Value from @settings where Name = 'FocalSearch'
				select	@FocalSearchObject			= Value from @settings where Name = 'Focal'
				select	@FocalSearchObjectID		= Value from @settings where Name = 'FocalID'
				select	@PredicateID				= Value from @settings where Name = 'Predicate'
				
				--BEGIN: Find subject based on search type
				if @SubjectSearchType = 'Direct'
				begin
					set @Subject = @SubjectSearchObject
					set @SubjectID = @SubjectSearchObjectID
				end

				if @SubjectSearchType = 'FusionOwner'
				begin
					select	@Subject = RelationshipOwnerObjectType,
							@SubjectID = RelationshipOwnerObjectID
					from	FusionAttributeOwnerRule
					where	@SubjectSearchObject = 'Owner'
							and FusionID = @FusionID
							and ID = @SubjectSearchObjectID
				end

				if @SubjectSearchType = 'ResultFromStep'
				begin
					select	@Subject = ObjectType,
							@SubjectID = ObjectID
					from	[Fusion].[RulePromotion]
					where	@SubjectSearchObject = 'Step'
							and RuleID = @RuleID
							and RuleStepID = @SubjectSearchObjectID
							and FusionAttributeID = @FusionAttributeID
				end

				if @SubjectSearchType = 'Self'
				begin
					set @Subject = 'FusionAttribute'
					set @SubjectID = @FusionAttributeID
				end
				--END: Find subject based on search type

				--BEGIN: Find object based on search type
				if @ObjectSearchType = 'Direct'
				begin
					set @Object = @ObjectSearchObject
					set @ObjectID = @ObjectSearchObjectID
				end

				if @ObjectSearchType = 'FusionOwner'
				begin
					select	@Object = RelationshipOwnerObjectType,
							@ObjectID = RelationshipOwnerObjectID
					from	FusionAttributeOwnerRule
					where	@ObjectSearchObject = 'Owner'
							and FusionID = @FusionID
							and ID = @ObjectSearchObjectID
				end

				if @ObjectSearchType = 'ResultFromStep'
				begin
					select	@Object = ObjectType,
							@ObjectID = ObjectID
					from	[fusion].[RulePromotion]
					where	@ObjectSearchObject = 'Step'
							and RuleID = @RuleID
							and RuleStepID = @ObjectSearchObjectID
							and FusionAttributeID = @FusionAttributeID
				end

				if @ObjectSearchType = 'Self'
				begin
					set @Object = 'FusionAttribute'
					set @ObjectID = @FusionAttributeID
				end
				--END: Find object based on search type

				--BEGIN: Find focal based on search type
				if @FocalSearchType = 'Direct'
				begin
					set @Focal = @FocalSearchObject
					set @FocalID = @FocalSearchObjectID
				end

				if @FocalSearchType = 'FusionOwner'
				begin
					select	@Focal = RelationshipOwnerObjectType,
							@FocalID = RelationshipOwnerObjectID
					from	FusionAttributeOwnerRule
					where	@FocalSearchObject = 'Owner'
							and FusionID = @FusionID
							and ID = @FocalSearchObjectID
				end

				if @FocalSearchType = 'ResultFromStep'
				begin
					select	@Focal = ObjectType,
							@FocalID = ObjectID
					from	[fusion].[RulePromotion]
					where	@FocalSearchObject = 'Step'
							and RuleID = @RuleID
							and RuleStepID = @FocalSearchObjectID
							and FusionAttributeID = @FusionAttributeID
				end

				if @FocalSearchType = 'Self'
				begin
					set @Focal = 'FusionAttribute'
					set @FocalID = @FusionAttributeID
				end
				--END: Find focal based on search type

				declare @SubjectType varchar(50) = null,
						@SubjectTypeID int = null,
						@SubjectIntersectNodeID int = null,
						@SubjectIntersectTypeNodeID int = null,

						@ObjectType varchar(50) = null,
						@ObjectTypeID int = null,
						@ObjectIntersectNodeID int = null,
						@ObjectIntersectTypeNodeID int = null,

						@PredicateType int = null

				--BEGIN: Relate Subject to Object
				--Check to see if we have all the required data to create the relationship.
				if @IntersectTypeID is not null and @subject is not null and @SubjectID is not null and @Object is not null and @ObjectID is not null
				begin					
					-- Validate that intersect type exists.
					if exists(select 1 from IntersectType where ID = @IntersectTypeID)
					begin
--select @Subject, @SubjectID, @Object, @ObjectID
						select	@IntersectID = isect.ID,
								@SubjectIntersectNodeID = inode2.ID,
								@ObjectIntersectNodeID = inode1.ID
						from	[Intersect] isect
								inner join [intersectnode] inode1 on(isect.id = inode1.intersectid and inode1.objecttype = isect.object and inode1.objectid = isect.objectid)
								inner join [intersectnode] inode2 on(isect.id = inode2.intersectid and inode2.objecttype = isect.subject and inode2.objectid = isect.subjectid)
						where	Subject = @Subject 
								and isect.SubjectID = @SubjectID 
								and isect.Object = @Object 
								and isect.ObjectID = @ObjectID
								and isect.IntersectTypeID = @IntersectTypeID							
--select @IntersectID
						if @IntersectID is null
						begin
							select	@SubjectType = ObjectType, @SubjectTypeID = ObjectTypeID from ObjectCache where Object = @Subject and ObjectID = @SubjectID
							select	@ObjectType = ObjectType, @ObjectTypeID = ObjectTypeID from ObjectCache where Object = @Object and ObjectID = @ObjectID

							select	@SubjectIntersectTypeNodeID = SourceIntersectTypeNodeID, 
									@ObjectIntersectTypeNodeID = TargetIntersectTypeNodeID
							from	utility.RelationshipTypes R 
							where	SourceObjectType = @SubjectType and SourceObjectID = @SubjectTypeID 
									and TargetObjectType = @ObjectType and TargetObjectID = @ObjectTypeID
									and IntersectTypeID = @IntersectTypeID

							if @SubjectIntersectTypeNodeID is not null and @ObjectIntersectTypeNodeID is not null
							begin
								begin try


									insert into [Intersect] (IntersectTypeID, Classification, Subject, SubjectID, Object, ObjectID, Deleted, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn)
									values					(@IntersectTypeID, 2, @Subject, @SubjectID, @Object, @ObjectID, 0, @r, @d, @r, @d)  

									select @IntersectID = SCOPE_IDENTITY()

									insert into IntersectNode	(IntersectTypeNodeID, IntersectID, ObjectType, ObjectID)
									values						(@SubjectIntersectTypeNodeID, @IntersectID, @Subject, @SubjectID)

									select @SubjectIntersectNodeID = SCOPE_IDENTITY()

									insert into IntersectNode	(IntersectTypeNodeID, IntersectID, ObjectType, ObjectID)
									values						(@ObjectIntersectTypeNodeID, @IntersectID, @Object, @ObjectID)

									select @ObjectIntersectNodeID = SCOPE_IDENTITY()

									--cache logic
									insert into cache.[Object] ( [Object], [ObjectID], [ObjectType], [ObjectTypeID] ) values	( 'Intersect', @IntersectID, 'IntersectType', @IntersectTypeID );
									insert into cache.Relationship ( IntersectID, SourceIntersectTypeNodeID, SourceIntersectNodeID, SourceObject, SourceObjectID, TargetIntersectTypeNodeID, TargetIntersectNodeID, TargetObject, TargetObjectID )
									values	( @IntersectID, @SubjectIntersectTypeNodeID, @SubjectIntersectNodeID, @Subject, @SubjectID, @ObjectIntersectTypeNodeID, @ObjectIntersectNodeID, @Object, @ObjectID );
									insert into cache.Relationship ( IntersectID, SourceIntersectTypeNodeID, SourceIntersectNodeID, SourceObject, SourceObjectID, TargetIntersectTypeNodeID, TargetIntersectNodeID, TargetObject, TargetObjectID )
									values	( @IntersectID, @ObjectIntersectTypeNodeID, @ObjectIntersectNodeID, @Object, @ObjectID, @SubjectIntersectTypeNodeID, @SubjectIntersectNodeID, @Subject, @SubjectID );

									--Update the responsibilities of the object that should inherit form the other (Taxonomy can push relationships down to artifact)
									if ( (@Subject = 'Taxonomy' and @Object = 'Artifact') OR (@Subject = 'Artifact' and @Object = 'Taxonomy') )
									begin
										if @Subject = 'Artifact'
										begin
											exec [cache].[SynchronizeResponsibilitiesForObject] @Subject, @SubjectID
										end
										if @Object = 'Artifact'
										begin
											exec [cache].[SynchronizeResponsibilitiesForObject] @Object, @ObjectID
										end
									end

									exec utility.AddAuditEntry @Subject, @SubjectID, @r, @d, 'Created', 'Intersect', @IntersectID
									exec utility.AddAuditEntry @Object, @ObjectID, @r, @d, 'Created', 'Intersect', @IntersectID
																											
									set @ResultObjectID = @IntersectID
								end try
								begin catch
									select ERROR_MESSAGE()
								end catch

							end
						end
					end
				end
				--END: Relate Subject to Object

				--BEGIN: Add IntersectMap
				if @SubjectIntersectNodeID is not null and @ObjectIntersectNodeID is not null
				begin					
					select @PredicateType = Type from Predicate where ID = @PredicateID
					if @PredicateType is not null
					begin
						declare @intersectMap table (ID int)
						MERGE	IntersectMap AS T
						USING	(
								SELECT	@SubjectIntersectNodeID as SubjectIntersectNodeID, 
										@ObjectIntersectNodeID as ObjectIntersectNodeID, 
										@PredicateID as PredicateID, 
										@PredicateType as Type
								) as S
						ON		T.SubjectIntersectNodeID = S.SubjectIntersectNodeID
								and T.ObjectIntersectNodeID = S.ObjectIntersectNodeID 
								and T.PredicateID = S.PredicateID 
						WHEN	MATCHED THEN
								UPDATE SET	T.Type = S.Type
						WHEN	NOT MATCHED THEN
								INSERT (SubjectIntersectNodeID, ObjectIntersectNodeID, PredicateID, Type) 
								VALUES (S.SubjectIntersectNodeID, S.ObjectIntersectNodeID, S.PredicateID, S.Type)
						OUTPUT inserted.ID into @intersectMap;
					
						set @ResultObject = 'IntersectMap'
						select top 1 @ResultObjectID = ID from @intersectMap
						delete from @intersectMap				
					end
				end
				--END: Add IntersectMap


			end --END: Lineage Action

			--BEGIN: Relate Action
			if @Action = 'Relate'
			begin
				declare @R_IntersectTypeID int = null,
						@R_SubjectSearchType nvarchar(250) = null,
						@R_SubjectSearchObject varchar(50) = null,
						@R_SubjectSearchObjectID int = null,
						@R_Subject varchar(50) = null,
						@R_SubjectID int = null,
						@R_ObjectSearchType nvarchar(250) = null,
						@R_ObjectSearchObject varchar(50) = null,
						@R_ObjectSearchObjectID int = null,
						@R_Object varchar(50) = null,
						@R_ObjectID int = null,
						@R_IntersectID int = null

				select	@R_SubjectSearchType		= Value from @settings where Name = 'SubjectSearch'
				select	@R_SubjectSearchObject		= Value from @settings where Name = 'Subject'
				select	@R_SubjectSearchObjectID	= Value from @settings where Name = 'SubjectID'
				select	@R_ObjectSearchType			= Value from @settings where Name = 'ObjectSearch'
				select	@R_ObjectSearchObject		= Value from @settings where Name = 'Object'
				select	@R_ObjectSearchObjectID		= Value from @settings where Name = 'ObjectID'
				select	@R_IntersectTypeID			= Value from @settings where Name = 'IntersectType'


				--BEGIN: Find subject based on search type
				if @R_SubjectSearchType = 'Direct'
				begin
					set @R_Subject = @R_SubjectSearchObject
					set @R_SubjectID = @R_SubjectSearchObjectID
				end

				if @R_SubjectSearchType = 'FusionOwner'
				begin
					select	@R_Subject = RelationshipOwnerObjectType,
							@R_SubjectID = RelationshipOwnerObjectID
					from	FusionAttributeOwnerRule
					where	@R_SubjectSearchObject = 'Owner'
							and FusionID = @FusionID
							and ID = @R_SubjectSearchObjectID
				end

				if @R_SubjectSearchType = 'ResultFromStep'
				begin
					select	@R_Subject = ObjectType,
							@R_SubjectID = ObjectID
					from	[fusion].RulePromotion
					where	@R_SubjectSearchObject = 'Step'
							and RuleID = @RuleID
							and RuleStepID = @R_SubjectSearchObjectID
							and FusionAttributeID = @FusionAttributeID

--select @R_Subject, @R_SubjectID
				end

				if @R_SubjectSearchType = 'Self'
				begin
					set @R_Subject = 'FusionAttribute'
					set @R_SubjectID = @FusionAttributeID
				end
				--END: Find subject based on search type
				
				--BEGIN: Find object based on search type
				if @R_ObjectSearchType = 'Direct'
				begin
					set @R_Object = @R_ObjectSearchObject
					set @R_ObjectID = @R_ObjectSearchObjectID
				end

				if @R_ObjectSearchType = 'FusionOwner'
				begin
					select	@R_Object = RelationshipOwnerObjectType,
							@R_ObjectID = RelationshipOwnerObjectID
					from	FusionAttributeOwnerRule
					where	@R_ObjectSearchObject = 'Owner'
							and FusionID = @FusionID
							and ID = @R_ObjectSearchObjectID
				end

				if @R_ObjectSearchType = 'ResultFromStep'
				begin
					select	@R_Object = ObjectType,
							@R_ObjectID = ObjectID
					from	[Fusion].[RulePromotion]
					where	@R_ObjectSearchObject = 'Step'
							and RuleID = @RuleID
							and RuleStepID = @R_ObjectSearchObjectID
							and FusionAttributeID = @FusionAttributeID
				end

				if @R_ObjectSearchType = 'Self'
				begin
					set @R_Object = 'FusionAttribute'
					set @R_ObjectID = @FusionAttributeID

				end
				--END: Find object based on search type


				--Check to see if we have all the required data to create the relationship.
				if @R_IntersectTypeID is not null and @R_subject is not null and @R_SubjectID is not null and @R_Object is not null and @R_ObjectID is not null
				begin
					-- Validate that intersect type exists.
					if exists(select 1 from IntersectType where ID = @R_IntersectTypeID)
					begin
						set @ResultObject = 'Intersect'
--select @Subject, @SubjectID, @Object, @ObjectID
						select	@R_IntersectID = ID
						from	[Intersect]
						where	Subject = @R_Subject 
								and SubjectID = @R_SubjectID 
								and Object = @R_Object 
								and ObjectID = @R_ObjectID
								and IntersectTypeID = @R_IntersectTypeID

						if @R_IntersectID is null
						begin
							declare @R_SubjectType varchar(50) = null,
									@R_SubjectTypeID int = null,
									@R_SubjectIntersectTypeNodeID int = null,
									@R_ObjectType varchar(50) = null,
									@R_ObjectTypeID int = null,
									@R_ObjectIntersectTypeNodeID int = null

							select	@R_SubjectType = ObjectType, @R_SubjectTypeID = ObjectTypeID from ObjectCache where Object = @R_Subject and ObjectID = @R_SubjectID
							select	@R_ObjectType = ObjectType, @R_ObjectTypeID = ObjectTypeID from ObjectCache where Object = @R_Object and ObjectID = @R_ObjectID

							select	@R_SubjectIntersectTypeNodeID = SourceIntersectTypeNodeID, 
									@R_ObjectIntersectTypeNodeID = TargetIntersectTypeNodeID
							from	utility.RelationshipTypes R 
							where	SourceObjectType = @R_SubjectType and SourceObjectID = @R_SubjectTypeID 
									and TargetObjectType = @R_ObjectType and TargetObjectID = @R_ObjectTypeID
									and IntersectTypeID = @R_IntersectTypeID


							if @R_SubjectIntersectTypeNodeID is not null and @R_ObjectIntersectTypeNodeID is not null
							begin
								begin try
									declare @R_SubjectIntersectNodeID int = null,
											@R_ObjectIntersectNodeID int = null

									insert into [Intersect] (IntersectTypeID, Classification, Subject, SubjectID, Object, ObjectID, Deleted, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn)
									values					(@R_IntersectTypeID, 2, @R_Subject, @R_SubjectID, @R_Object, @R_ObjectID, 0, @r, @d, @r, @d)  

									select @R_IntersectID = SCOPE_IDENTITY()

									insert into IntersectNode	(IntersectTypeNodeID, IntersectID, ObjectType, ObjectID)
									values						(@R_SubjectIntersectTypeNodeID, @R_IntersectID, @R_Subject, @R_SubjectID)

									select @R_SubjectIntersectNodeID = SCOPE_IDENTITY()

									insert into IntersectNode	(IntersectTypeNodeID, IntersectID, ObjectType, ObjectID)
									values						(@R_ObjectIntersectTypeNodeID, @R_IntersectID, @R_Object, @R_ObjectID)

									select @R_ObjectIntersectNodeID = SCOPE_IDENTITY()

									--cache logic
									insert into cache.[Object] ( [Object], [ObjectID], [ObjectType], [ObjectTypeID] ) values	( 'Intersect', @R_IntersectID, 'IntersectType', @R_IntersectTypeID );
									insert into cache.Relationship ( IntersectID, SourceIntersectTypeNodeID, SourceIntersectNodeID, SourceObject, SourceObjectID, TargetIntersectTypeNodeID, TargetIntersectNodeID, TargetObject, TargetObjectID )
									values	( @R_IntersectID, @R_SubjectIntersectTypeNodeID, @R_SubjectIntersectNodeID, @R_Subject, @R_SubjectID, @R_ObjectIntersectTypeNodeID, @R_ObjectIntersectNodeID, @R_Object, @R_ObjectID );
									insert into cache.Relationship ( IntersectID, SourceIntersectTypeNodeID, SourceIntersectNodeID, SourceObject, SourceObjectID, TargetIntersectTypeNodeID, TargetIntersectNodeID, TargetObject, TargetObjectID )
									values	( @R_IntersectID, @R_ObjectIntersectTypeNodeID, @R_ObjectIntersectNodeID, @R_Object, @R_ObjectID, @R_SubjectIntersectTypeNodeID, @R_SubjectIntersectNodeID, @R_Subject, @R_SubjectID );

									--Update the responsibilities of the object that should inherit form the other (Taxonomy can push relationships down to artifact)
									if ( (@R_Subject = 'Taxonomy' and @R_Object = 'Artifact') OR (@R_Subject = 'Artifact' and @R_Object = 'Taxonomy') )
									begin
										if @R_Subject = 'Artifact'
										begin
											exec [cache].[SynchronizeResponsibilitiesForObject] @R_Subject, @R_SubjectID
										end
										if @R_Object = 'Artifact'
										begin
											exec [cache].[SynchronizeResponsibilitiesForObject] @R_Object, @R_ObjectID
										end
									end

									exec utility.AddAuditEntry @R_Subject, @R_SubjectID, @r, @d, 'Created', 'Intersect', @R_IntersectID
									exec utility.AddAuditEntry @R_Object, @R_ObjectID, @r, @d, 'Created', 'Intersect', @R_IntersectID

									set @ResultObjectID = @R_IntersectID
								end try
								begin catch
									select ERROR_MESSAGE()
								end catch

							end
						end
						else
						begin
							set @ResultObjectID = @R_IntersectID
						end
					end
				end


			end --END: Relate Action


			-- Add/Update the promotion record to keep track of the auto-promotions
			if @ResultObject is not null and @ResultObjectID is not null
			begin
				-- Insert/Update the FusionAttributePromotion table to keep track of previously promoted objects.
				MERGE	[fusion].[RulePromotion] AS T
				USING	(
						SELECT	@FusionAttributeID as FusionAttributeID, 
								@ResultObject as ObjectType, 
								@ResultObjectID as ObjectID, 
								@RuleID as RuleID,
								0 as PromotedObjectTypeID,
								@RuleStepID as RuleStepID
						) as S
				ON		T.RuleID = S.RuleID
						and T.RuleStepID = S.RuleStepID 
						and T.FusionAttributeID = S.FusionAttributeID 
						and T.ObjectType = S.ObjectType 
						and T.ObjectID = S.ObjectID
				WHEN	MATCHED THEN
						UPDATE SET	T.RuleID = S.RuleID, 
									T.ObjectTypeID = S.PromotedObjectTypeID
				WHEN	NOT MATCHED THEN
						INSERT (FusionAttributeID, ObjectType, ObjectID, RuleID, RuleStepID, ObjectTypeID) 
						VALUES (S.FusionAttributeID, S.ObjectType, S.ObjectID, S.RuleID, S.RuleStepID, S.PromotedObjectTypeID);


				-- Add/Update the dynamic fields involved.

				-- First, clean up fields table variable of static fields to prepare for dynamic field work below.
				delete @fields where TargetFieldTypeID = 0

				-- Now insert the dynamic fields
				while exists (select 1 from @fields)
				begin
					declare @targetFieldTypeID int,
							@field_Type varchar(25),
							@lookupObjectType varchar(25),
							@lookupObjectID int,
							@fieldValue nvarchar(4000),
							@shouldInsert bit = 0

					select	top 1 
							@targetFieldTypeID = TargetFieldTypeID,
							@fieldValue = Value
					from	@fields
									
					select	@field_Type = [Type],
							@lookupObjectType = LookupObjectType,
							@lookupObjectID = LookupObjectID									
						from	FieldType 
						where	ID = @targetFieldTypeID

					if @field_Type = 'Lookup'
					begin
						declare @objectResultID int

						if @lookupObjectType = 'Artifact'
							begin
								select	top 1
										@objectResultID = ID
								from	Artifact
								where	ArtifactTypeID = @lookupObjectID and Name = @fieldValue
							end
						if @lookupObjectType = 'Domain'
							begin
								select	top 1
										@objectResultID = ID
								from	DomainItem
								where	DomainID = @lookupObjectID and Name = @fieldValue
							end
						if @lookupObjectType = 'Lookup'
							begin
								select	top 1
										@objectResultID = L.ID
								from	[Lookup] L
										inner join Field F on F.ObjectType = @lookupObjectType and F.ObjectID = L.ID and L.LookupTypeID = @lookupObjectID and F.FieldTypeID = @targetFieldTypeID and F.FormattedValue = @fieldValue
							end
											
						if @ResultObjectID is not null and @objectResultID is not null
							begin
								-- Lookup values properly resolved, so you can now insert the Field record.
													
								set @shouldInsert = 1
								set @fieldValue = cast(@objectResultID as nvarchar(4000))
							end
					end									
					else
					begin
						-- This is a text value, so just insert it into the Field table for the promoted object.
						set @shouldInsert = 1
					end

					if @shouldInsert = 1
					begin
						If not EXISTS (SELECT 1 FROM #fieldValues where ObjectType = @ResultObject and ObjectID = @ResultObjectID and FieldTypeID = @targetFieldTypeID) --avoid duplicates this happens in gmo
						begin
							insert into #fieldValues (ObjectType, ObjectID, FieldTypeID, Value) values(@ResultObject, @ResultObjectID, @targetFieldTypeID, @fieldValue)
						end
					end
						
					-- Delete the field we just finished processing.
					delete @fields where TargetFieldTypeID = @targetFieldTypeID
				end --END: while

			end --END: IF when checking for promotiontype


		end try
		begin catch
			SELECT 
				ERROR_NUMBER() AS ErrorNumber
				,ERROR_MESSAGE() AS ErrorMessage;
		end catch

		set @currentID = @currentID + 1
	end


	-- write the field values from the temp table to the field table
	-- the field table has a trigger doing this once outside the loop causes the trigger to only fire this one time.
	If EXISTS (SELECT 1 FROM #fieldValues)
	begin
		--debug shows values 
		--select * from #fieldValues

		merge	Field as T
		using	(
				select	f.ObjectType as ObjectType,
						f.ObjectID as ObjectID,
						f.FieldTypeID as FieldTypeID,
						f.Value as Value
				from	#fieldValues f 
						inner join dbo.FieldType ft on (ft.ID = f.FieldTypeID)
				) as S
		on		T.ObjectType = S.ObjectType and T.ObjectID = S.ObjectID and T.FieldTypeID = S.FieldTypeID
		when	matched then
				update set T.Value = S.Value
		when	not matched then
				insert (ObjectType, ObjectID, FieldTypeID, Value) values (S.ObjectType, S.ObjectID, S.FieldTypeID, S.Value);
	end

	---- Add new relations as needed
	--exec [utility].[PromoteFusionAttributesRelations] @NumberOfNewRelations output

	---- Handle any fusionlookup fields
	--exec [utility].[PromoteFusionAttributeLookups]


	----Log this run done
	--update [dbo].[FusionAttributePromotionLogSummary]
	--set	DateCompleted = CURRENT_TIMESTAMP, 
	--	[PromotedTaxonomies] = @NumberOfNewTaxonomies, 
	--	[PromotedDomainItems] = @NumberOfNewDomainItems,  
	--	[PromotedDomains] = @NumberOfNewDomains,
	--	[PromotedArtifacts] = @NumberOfNewArtifacts,
	--	[TotalNewPromotions] = (@NumberOfNewTaxonomies + @NumberOfNewDomainItems + @NumberOfNewDomains + @NumberOfNewArtifacts),
	--	[AttributesConsidered]= @NumberOfAttributesTotal,
	--	[NumberOfRules] = @NumberOfRules ,
	--	[RelationshipsAdded] = @NumberOfNewRelations
	--where ID = @ExecutionID;
END
go