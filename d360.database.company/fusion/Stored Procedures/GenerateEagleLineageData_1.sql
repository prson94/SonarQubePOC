
CREATE procedure [fusion].[GenerateEagleLineageData]
	@fusionId int,
	@includeEagleToBloomberg bit
as
begin
	SET NOCOUNT, ANSI_PADDING ON;
	SET ANSI_WARNINGS ON;

	declare @intersectTypeId int;
	--bloomberg type ids
	declare @bloombergMnemonicTypeId int = 301;

	--eagle type ids
	declare @eagleReportProfileTypeId int = 191;
	declare @eaglePortalQueryTypeId int = 192;
	declare @eagleDatamartFieldTypeId int = 193;
	declare @eagleDatamartModelTypeId int = 194;
	declare @eagleMessageStreamTypeId int = 196;
	declare @eagleReportRuleTypeId int = 197;
	declare @eagleFieldAttributeTypeId int = 201;
	declare @eagleInventoryOfFieldTypeId int = 205;
	declare @eagleFieldRuleTypeId int = 206;
	declare @eagleSourceRuleTypeId int = 208;
	declare @eagleSourceRuleItemTypeId int = 209;
	declare @eagleGroupingRuleTypeId int = 210;
	declare @eagleReferenceDataCenterStrategyTypeId int = 215;
	declare @eagleReferenceDataCenterValidationTypeId int = 216;
	declare @eagleReferenceDataCenterFieldGroupTypeId int = 218;
	declare @eagleReferenceDataCenterGoldCopyTypeId int = 217;

	-- validate the provided fusion id that its of fusiontype id 16	
	declare @fusionTypeId int;
	select @fusionTypeId = FusionTypeID from [dbo].[Fusion] where ID = @fusionId;
	if @fusionTypeId != 16
	begin
		raiserror('ERROR - The eagle fusion lineage generation process may only be run for the Eagle DB Fusion Type', 16, -1);
		return;
	end
	
	IF OBJECT_ID('tempdb..#maps') IS NOT NULL
		DROP TABLE #maps;

	create table #maps (	
		ID int identity primary key,			
		MapRuleItemID int,		
		SourceFusionAttributeID int,
		SourceFusionAttributeTypeID int,
		SourceObject nvarchar(500),		
		TargetFusionAttributeID int,
		TargetFusionAttributeTypeID int,
		TargetObject nvarchar(500)		
	);

	CREATE NONCLUSTERED INDEX [CIX_TempMaps] ON #maps ( SourceFusionAttributeID ASC, TargetFusionAttributeID ASC );


	if ( @includeEagleToBloomberg = 1 )
	begin
		----------------------------------------------------------
		-- BLOOMBERG MNEMNONIC TO EAGLE INVENTORY OF FIELD
		----------------------------------------------------------	
		select @intersectTypeId = id from intersecttype where subjectid = @eagleInventoryOfFieldTypeId and [subject] = 'FusionAttributeType' and objectid = @bloombergMnemonicTypeId and [object] = 'FusionAttributeType';	
		if @intersectTypeId is null
		begin
			raiserror('ERROR - Cannot identify the intersecttypeid for bloomberg mnemonic/ eagle db column relations', 16, -1);
			return;
		end

		insert into #maps 
			(SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject)
			select
				FA_s.ID as SourceFusionAttributeID,
				FA_s.FusionAttributeTypeID as SourceFusionAttributeTypeID,
				FA_s.Name as SourceObject,
				FA_t.ID as TargetFusionAttributeID,
				FA_t.FusionAttributeTypeID as TargetFusionAttributeTypeID,
				FA_t.Name as TargetObject
			from
				[dbo].[intersect] I
				inner join [dbo].FusionAttribute FA_t on (FA_t.ID = I.subjectID and I.intersecttypeid = @intersectTypeId and FA_t.FusionID = @fusionId)
				inner join [dbo].FusionAttribute FA_s on (FA_s.ID = I.objectID and I.intersecttypeid = @intersectTypeId);
	
		set @intersectTypeId = null;

		----------------------------------------------------------
		-- BLOOMBERG MNEMONIC TO EAGLE MESSAGE STREAM	
		----------------------------------------------------------
	
		select @intersectTypeId = id from intersecttype where subjectid = @eagleMessageStreamTypeId and [subject] = 'FusionAttributeType' and objectid = @bloombergMnemonicTypeId and [object] = 'FusionAttributeType';
		if @intersectTypeId is null
		begin
			raiserror('ERROR - Cannot identify the intersecttypeid for bloomberg mnemonic/ eagle message stream relations', 16, -1);
			return;
		end

		insert into #maps 
			(SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject)
			select
				FA_s.ID as SourceFusionAttributeID,
				FA_s.FusionAttributeTypeID as SourceFusionAttributeTypeID,
				FA_s.Name as SourceObject,
				FA_t.ID as TargetFusionAttributeID,
				FA_t.FusionAttributeTypeID as TargetFusionAttributeTypeID,
				FA_t.Name as TargetObject
			from
				[dbo].[intersect] I
				inner join [dbo].FusionAttribute FA_t on (FA_t.ID = I.subjectID and I.intersecttypeid = @intersectTypeId and FA_t.FusionID = @fusionId)
				inner join [dbo].FusionAttribute FA_s on (FA_s.ID = I.objectID and I.intersecttypeid = @intersectTypeId);
	
		set @intersectTypeId = null;						
	end
	----------------------------------------------------------
	-- INVENTORY OF FIELDS TO FIELD ATTRIBUTE	
	----------------------------------------------------------
	
	select @intersectTypeId = id from intersecttype where subjectid = @eagleFieldAttributeTypeId and [subject] = 'FusionAttributeType' and objectid = @eagleInventoryOfFieldTypeId and [object] = 'FusionAttributeType';
	if @intersectTypeId is null
	begin
		raiserror('ERROR - Cannot identify the intersecttypeid for eagle field attribute/ eagle db column relations', 16, -1);
		return;
	end

	insert into #maps 
		(SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject)
		select
			FA_s.ID as SourceFusionAttributeID,
			FA_s.FusionAttributeTypeID as SourceFusionAttributeTypeID,
			FA_s.Name as SourceObject,
			FA_t.ID as TargetFusionAttributeID,
			FA_t.FusionAttributeTypeID as TargetFusionAttributeTypeID,
			FA_t.Name as TargetObject
		from
			[dbo].[intersect] I
			inner join [dbo].FusionAttribute FA_t on (FA_t.ID = I.subjectID and I.intersecttypeid = @intersectTypeId and FA_t.FusionID = @fusionId)
			inner join [dbo].FusionAttribute FA_s on (FA_s.ID = I.objectID and I.intersecttypeid = @intersectTypeId and FA_s.FusionID = @fusionId);
	set @intersectTypeId = null;	
	
	----------------------------------------------------------
	-- FIELD ATTRIBUTE TO FIELD RULES	
	----------------------------------------------------------
	
	select @intersectTypeId = id from intersecttype where subjectid = @eagleFieldRuleTypeId and [subject] = 'FusionAttributeType' and objectid = @eagleFieldAttributeTypeId and [object] = 'FusionAttributeType';
	if @intersectTypeId is null
	begin
		raiserror('ERROR - Cannot identify the intersecttypeid for eagle field attribute/ eagle field rule', 16, -1);
		return;
	end

	insert into #maps 
		(SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject)
		select
			FA_s.ID as SourceFusionAttributeID,
			FA_s.FusionAttributeTypeID as SourceFusionAttributeTypeID,
			FA_s.Name as SourceObject,
			FA_t.ID as TargetFusionAttributeID,
			FA_t.FusionAttributeTypeID as TargetFusionAttributeTypeID,
			FA_t.Name as TargetObject
		from
			[dbo].[intersect] I
			inner join [dbo].FusionAttribute FA_t on (FA_t.ID = I.subjectID and I.intersecttypeid = @intersectTypeId and FA_t.FusionID = @fusionId)
			inner join [dbo].FusionAttribute FA_s on (FA_s.ID = I.objectID and I.intersecttypeid = @intersectTypeId and FA_s.FusionID = @fusionId);
	set @intersectTypeId = null;

	----------------------------------------------------------
	-- FIELD ATTRIBUTE TO FIELD ATTRIBUTE - This is for computed fields ie fields which use other fields
	----------------------------------------------------------
	
	select @intersectTypeId = id from intersecttype where subjectid = @eagleFieldAttributeTypeId and [subject] = 'FusionAttributeType' and objectid = @eagleFieldAttributeTypeId and [object] = 'FusionAttributeType';
	if @intersectTypeId is null
	begin
		raiserror('ERROR - Cannot identify the intersecttypeid for eagle field attribute/ eagle field attribute', 16, -1);
		return;
	end

	insert into #maps 
		(SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject)
		select
			FA_s.ID as SourceFusionAttributeID,
			FA_s.FusionAttributeTypeID as SourceFusionAttributeTypeID,
			FA_s.Name as SourceObject,
			FA_t.ID as TargetFusionAttributeID,
			FA_t.FusionAttributeTypeID as TargetFusionAttributeTypeID,
			FA_t.Name as TargetObject
		from
			[dbo].[intersect] I
			inner join [dbo].FusionAttribute FA_s on (FA_s.ID = I.subjectID and I.intersecttypeid = @intersectTypeId and FA_s.FusionID = @fusionId)
			inner join [dbo].FusionAttribute FA_t on (FA_t.ID = I.objectID and I.intersecttypeid = @intersectTypeId and FA_t.FusionID = @fusionId);
	set @intersectTypeId = null;

	----------------------------------------------------------
	-- FIELD ATTRIBUTE TO GROUPING RULES	
	----------------------------------------------------------
	
	select @intersectTypeId = id from intersecttype where subjectid = @eagleGroupingRuleTypeId and [subject] = 'FusionAttributeType' and objectid = @eagleFieldAttributeTypeId and [object] = 'FusionAttributeType';
	if @intersectTypeId is null
	begin
		raiserror('ERROR - Cannot identify the intersecttypeid for eagle field attribute/ eagle grouping rule', 16, -1);
		return;
	end

	insert into #maps 
		(SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject)
		select
			FA_s.ID as SourceFusionAttributeID,
			FA_s.FusionAttributeTypeID as SourceFusionAttributeTypeID,
			FA_s.Name as SourceObject,
			FA_t.ID as TargetFusionAttributeID,
			FA_t.FusionAttributeTypeID as TargetFusionAttributeTypeID,
			FA_t.Name as TargetObject
		from
			[dbo].[intersect] I
			inner join [dbo].FusionAttribute FA_t on (FA_t.ID = I.subjectID and I.intersecttypeid = @intersectTypeId and FA_t.FusionID = @fusionId)
			inner join [dbo].FusionAttribute FA_s on (FA_s.ID = I.objectID and I.intersecttypeid = @intersectTypeId and FA_s.FusionID = @fusionId);
	
	set @intersectTypeId = null;
	----------------------------------------------------------
	-- FIELD RULE TO REPORT PROFILE
	----------------------------------------------------------
	
	select @intersectTypeId = id from intersecttype where subjectid = @eagleReportRuleTypeId and [subject] = 'FusionAttributeType' and objectid = @eagleFieldRuleTypeId and [object] = 'FusionAttributeType';
	if @intersectTypeId is null
	begin
		raiserror('ERROR - Cannot identify the intersecttypeid for eagle field rule/ eagle report rule', 16, -1);
		return;
	end

	insert into #maps 
		(SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject)
		select
			FA_s.ID as SourceFusionAttributeID,
			FA_s.FusionAttributeTypeID as SourceFusionAttributeTypeID,
			FA_s.Name as SourceObject,
			FA_t.ID as TargetFusionAttributeID,
			FA_t.FusionAttributeTypeID as TargetFusionAttributeTypeID,
			FA_t.Name as TargetObject
		from
			[dbo].[intersect] I
			inner join [dbo].FusionAttribute FA_t on (FA_t.ID = I.subjectID and I.intersecttypeid = @intersectTypeId and FA_t.FusionID = @fusionId)
			inner join [dbo].FusionAttribute FA_s on (FA_s.ID = I.objectID and I.intersecttypeid = @intersectTypeId and FA_s.FusionID = @fusionId);
	
	set @intersectTypeId = null;
	----------------------------------------------------------
	-- FIELD RULE to PORTAL QUERY
	----------------------------------------------------------	
	select @intersectTypeId = id from intersecttype where subjectid = @eaglePortalQueryTypeId and [subject] = 'FusionAttributeType' and objectid = @eagleFieldRuleTypeId and [object] = 'FusionAttributeType';
	if @intersectTypeId is null
	begin
		raiserror('ERROR - Cannot identify the intersecttypeid for eagle field rule/ eagle portal query', 16, -1);
		return;
	end

	insert into #maps 
		(SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject)
		select
			FA_s.ID as SourceFusionAttributeID,
			FA_s.FusionAttributeTypeID as SourceFusionAttributeTypeID,
			FA_s.Name as SourceObject,
			FA_t.ID as TargetFusionAttributeID,
			FA_t.FusionAttributeTypeID as TargetFusionAttributeTypeID,
			FA_t.Name as TargetObject
		from
			[dbo].[intersect] I
			inner join [dbo].FusionAttribute FA_t on (FA_t.ID = I.subjectID and I.intersecttypeid = @intersectTypeId and FA_t.FusionID = @fusionId)
			inner join [dbo].FusionAttribute FA_s on (FA_s.ID = I.objectID and I.intersecttypeid = @intersectTypeId and FA_s.FusionID = @fusionId);
	
	set @intersectTypeId = null;
	----------------------------------------------------------
	-- RDC Validation to Field Attribute
	----------------------------------------------------------	
	select @intersectTypeId = id from intersecttype where subjectid = @eagleReferenceDataCenterValidationTypeId and [subject] = 'FusionAttributeType' and objectid = @eagleFieldAttributeTypeId and [object] = 'FusionAttributeType';
	if @intersectTypeId is null
	begin
		raiserror('ERROR - Cannot identify the intersecttypeid for eagle field attribute/ rdc validation', 16, -1);
		return;
	end

	insert into #maps 
		(SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject)
		select
			FA_s.ID as SourceFusionAttributeID,
			FA_s.FusionAttributeTypeID as SourceFusionAttributeTypeID,
			FA_s.Name as SourceObject,
			FA_t.ID as TargetFusionAttributeID,
			FA_t.FusionAttributeTypeID as TargetFusionAttributeTypeID,
			FA_t.Name as TargetObject
		from
			[dbo].[intersect] I
			inner join [dbo].FusionAttribute FA_t on (FA_t.ID = I.subjectID and I.intersecttypeid = @intersectTypeId and FA_t.FusionID = @fusionId)
			inner join [dbo].FusionAttribute FA_s on (FA_s.ID = I.objectID and I.intersecttypeid = @intersectTypeId and FA_s.FusionID = @fusionId);
	
	set @intersectTypeId = null;
	----------------------------------------------------------
	-- RDC Data Strategy to Field Attribute
	----------------------------------------------------------	
	select @intersectTypeId = id from intersecttype where subjectid = @eagleReferenceDataCenterStrategyTypeId and [subject] = 'FusionAttributeType' and objectid = @eagleFieldAttributeTypeId and [object] = 'FusionAttributeType';
	if @intersectTypeId is null
	begin
		raiserror('ERROR - Cannot identify the intersecttypeid for eagle field attribute/ rdc data strategy', 16, -1);
		return;
	end

	insert into #maps 
		(SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject)
		select
			FA_s.ID as SourceFusionAttributeID,
			FA_s.FusionAttributeTypeID as SourceFusionAttributeTypeID,
			FA_s.Name as SourceObject,
			FA_t.ID as TargetFusionAttributeID,
			FA_t.FusionAttributeTypeID as TargetFusionAttributeTypeID,
			FA_t.Name as TargetObject
		from
			[dbo].[intersect] I
			inner join [dbo].FusionAttribute FA_t on (FA_t.ID = I.subjectID and I.intersecttypeid = @intersectTypeId and FA_t.FusionID = @fusionId)
			inner join [dbo].FusionAttribute FA_s on (FA_s.ID = I.objectID and I.intersecttypeid = @intersectTypeId and FA_s.FusionID = @fusionId);
	
	set @intersectTypeId = null;
	----------------------------------------------------------
	-- RDC Data Strategy to RDC Gold Copy
	----------------------------------------------------------	
	select @intersectTypeId = id from intersecttype where subjectid = @eagleReferenceDataCenterGoldCopyTypeId and [subject] = 'FusionAttributeType' and objectid = @eagleReferenceDataCenterStrategyTypeId and [object] = 'FusionAttributeType';
	if @intersectTypeId is null
	begin
		raiserror('ERROR - Cannot identify the intersecttypeid for eagle rdc gold copy / rdc data strategy', 16, -1);
		return;
	end

	insert into #maps 
		(SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject)
		select
			FA_s.ID as SourceFusionAttributeID,
			FA_s.FusionAttributeTypeID as SourceFusionAttributeTypeID,
			FA_s.Name as SourceObject,
			FA_t.ID as TargetFusionAttributeID,
			FA_t.FusionAttributeTypeID as TargetFusionAttributeTypeID,
			FA_t.Name as TargetObject
		from
			[dbo].[intersect] I
			inner join [dbo].FusionAttribute FA_t on (FA_t.ID = I.subjectID and I.intersecttypeid = @intersectTypeId and FA_t.FusionID = @fusionId)
			inner join [dbo].FusionAttribute FA_s on (FA_s.ID = I.objectID and I.intersecttypeid = @intersectTypeId and FA_s.FusionID = @fusionId);
	
	set @intersectTypeId = null;
	----------------------------------------------------------
	-- DataMart Measure to Field Attribute
	----------------------------------------------------------	
	select @intersectTypeId = id from intersecttype where subjectid = @eagleDatamartFieldTypeId and [subject] = 'FusionAttributeType' and objectid = @eagleFieldAttributeTypeId and [object] = 'FusionAttributeType';
	if @intersectTypeId is null
	begin
		raiserror('ERROR - Cannot identify the intersecttypeid for eagle field attribute/ datamart field', 16, -1);
		return;
	end

	insert into #maps 
		(SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject)
		select
			FA_s.ID as SourceFusionAttributeID,
			FA_s.FusionAttributeTypeID as SourceFusionAttributeTypeID,
			FA_s.Name as SourceObject,
			FA_t.ID as TargetFusionAttributeID,
			FA_t.FusionAttributeTypeID as TargetFusionAttributeTypeID,
			FA_t.Name as TargetObject
		from
			[dbo].[intersect] I
			inner join [dbo].FusionAttribute FA_t on (FA_t.ID = I.subjectID and I.intersecttypeid = @intersectTypeId and FA_t.FusionID = @fusionId)
			inner join [dbo].FusionAttribute FA_s on (FA_s.ID = I.objectID and I.intersecttypeid = @intersectTypeId and FA_s.FusionID = @fusionId);
	
	set @intersectTypeId = null;
	----------------------------------------------------------
	-- DataMart Model to DataMart Measure - uses parent child relation from fusion...
	----------------------------------------------------------	
	
	insert into #maps 
		(SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject)
		select
			FA_s.ID as SourceFusionAttributeID,
			FA_s.FusionAttributeTypeID as SourceFusionAttributeTypeID,
			FA_s.Name as SourceObject,
			FA_t.ID as TargetFusionAttributeID,
			FA_t.FusionAttributeTypeID as TargetFusionAttributeTypeID,
			FA_t.Name as TargetObject
		from
			[dbo].FusionAttribute FA_s
			inner join [dbo].FusionAttribute FA_t on (FA_t.ParentID = FA_s.ID and FA_t.FusionAttributeTypeId = @eagleDatamartFieldTypeId)
		where
			FA_s.FusionAttributeTypeId = @eagleDatamartModelTypeId and FA_s.FusionID = @fusionId
	
	set @intersectTypeId = null;
	----------------------------------------------------------
	-- Report Profile to Report Rule
	----------------------------------------------------------	
	select @intersectTypeId = id from intersecttype where subjectid = @eagleReportProfileTypeId  and [subject] = 'FusionAttributeType' and objectid = @eagleReportRuleTypeId and [object] = 'FusionAttributeType';
	if @intersectTypeId is null
	begin
		raiserror('ERROR - Cannot identify the intersecttypeid for eagle report profile/ report rule', 16, -1);
		return;
	end

	insert into #maps 
		(SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject)
		select
			FA_s.ID as SourceFusionAttributeID,
			FA_s.FusionAttributeTypeID as SourceFusionAttributeTypeID,
			FA_s.Name as SourceObject,
			FA_t.ID as TargetFusionAttributeID,
			FA_t.FusionAttributeTypeID as TargetFusionAttributeTypeID,
			FA_t.Name as TargetObject
		from
			[dbo].[intersect] I
			inner join [dbo].FusionAttribute FA_t on (FA_t.ID = I.subjectID and I.intersecttypeid = @intersectTypeId and FA_t.FusionID = @fusionId)
			inner join [dbo].FusionAttribute FA_s on (FA_s.ID = I.objectID and I.intersecttypeid = @intersectTypeId and FA_s.FusionID = @fusionId);
	
	set @intersectTypeId = null;
	----------------------------------------------------------
	-- Report Rule to Source Rule
	----------------------------------------------------------	
	select @intersectTypeId = id from intersecttype where subjectid = @eagleReportRuleTypeId  and [subject] = 'FusionAttributeType' and objectid = @eagleSourceRuleTypeId and [object] = 'FusionAttributeType';
	if @intersectTypeId is null
	begin
		raiserror('ERROR - Cannot identify the intersecttypeid for eagle report rule / source rule', 16, -1);
		return;
	end

	insert into #maps 
		(SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject)
		select
			FA_s.ID as SourceFusionAttributeID,
			FA_s.FusionAttributeTypeID as SourceFusionAttributeTypeID,
			FA_s.Name as SourceObject,
			FA_t.ID as TargetFusionAttributeID,
			FA_t.FusionAttributeTypeID as TargetFusionAttributeTypeID,
			FA_t.Name as TargetObject
		from
			[dbo].[intersect] I
			inner join [dbo].FusionAttribute FA_t on (FA_t.ID = I.subjectID and I.intersecttypeid = @intersectTypeId and FA_t.FusionID = @fusionId)
			inner join [dbo].FusionAttribute FA_s on (FA_s.ID = I.objectID and I.intersecttypeid = @intersectTypeId and FA_s.FusionID = @fusionId);
	
	set @intersectTypeId = null;
	----------------------------------------------------------
	-- Reference Data Center Data Strategy to Source Rule
	----------------------------------------------------------	
	select @intersectTypeId = id from intersecttype where subjectid = @eagleReferenceDataCenterStrategyTypeId  and [subject] = 'FusionAttributeType' and objectid = @eagleSourceRuleTypeId and [object] = 'FusionAttributeType';
	if @intersectTypeId is null
	begin
		raiserror('ERROR - Cannot identify the intersecttypeid for eagle data strategy / source rule', 16, -1);
		return;
	end

	insert into #maps 
		(SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject)
		select
			FA_s.ID as SourceFusionAttributeID,
			FA_s.FusionAttributeTypeID as SourceFusionAttributeTypeID,
			FA_s.Name as SourceObject,
			FA_t.ID as TargetFusionAttributeID,
			FA_t.FusionAttributeTypeID as TargetFusionAttributeTypeID,
			FA_t.Name as TargetObject
		from
			[dbo].[intersect] I
			inner join [dbo].FusionAttribute FA_t on (FA_t.ID = I.subjectID and I.intersecttypeid = @intersectTypeId and FA_t.FusionID = @fusionId)
			inner join [dbo].FusionAttribute FA_s on (FA_s.ID = I.objectID and I.intersecttypeid = @intersectTypeId and FA_s.FusionID = @fusionId);
	
	set @intersectTypeId = null;
	----------------------------------------------------------
	-- Datamart model to Source Rule
	----------------------------------------------------------	
	select @intersectTypeId = id from intersecttype where subjectid = @eagleDatamartModelTypeId  and [subject] = 'FusionAttributeType' and objectid = @eagleSourceRuleTypeId and [object] = 'FusionAttributeType';
	if @intersectTypeId is null
	begin
		raiserror('ERROR - Cannot identify the intersecttypeid for eagle datamart model / source rule', 16, -1);
		return;
	end

	insert into #maps 
		(SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject)
		select
			FA_s.ID as SourceFusionAttributeID,
			FA_s.FusionAttributeTypeID as SourceFusionAttributeTypeID,
			FA_s.Name as SourceObject,
			FA_t.ID as TargetFusionAttributeID,
			FA_t.FusionAttributeTypeID as TargetFusionAttributeTypeID,
			FA_t.Name as TargetObject
		from
			[dbo].[intersect] I
			inner join [dbo].FusionAttribute FA_t on (FA_t.ID = I.subjectID and I.intersecttypeid = @intersectTypeId and FA_t.FusionID = @fusionId)
			inner join [dbo].FusionAttribute FA_s on (FA_s.ID = I.objectID and I.intersecttypeid = @intersectTypeId and FA_s.FusionID = @fusionId);
	
	set @intersectTypeId = null;	
	----------------------------------------------------------
	-- Portal Query to Report Profile 
	----------------------------------------------------------	
	select @intersectTypeId = id from intersecttype where subjectid = @eaglePortalQueryTypeId and [subject] = 'FusionAttributeType' and objectid = @eagleReportProfileTypeId and [object] = 'FusionAttributeType';
	if @intersectTypeId is null
	begin
		raiserror('ERROR - Cannot identify the intersecttypeid for eagle portal query/ report profile', 16, -1);
		return;
	end

	insert into #maps 
		(SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject)
		select
			FA_s.ID as SourceFusionAttributeID,
			FA_s.FusionAttributeTypeID as SourceFusionAttributeTypeID,
			FA_s.Name as SourceObject,
			FA_t.ID as TargetFusionAttributeID,
			FA_t.FusionAttributeTypeID as TargetFusionAttributeTypeID,
			FA_t.Name as TargetObject
		from
			[dbo].[intersect] I
			inner join [dbo].FusionAttribute FA_t on (FA_t.ID = I.subjectID and I.intersecttypeid = @intersectTypeId and FA_t.FusionID = @fusionId)
			inner join [dbo].FusionAttribute FA_s on (FA_s.ID = I.objectID and I.intersecttypeid = @intersectTypeId and FA_s.FusionID = @fusionId);
	
	set @intersectTypeId = null;	
	----------------------------------------------------------
	-- Source Rule to Source Interface
	----------------------------------------------------------
	insert into #maps 
		(SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject)
			select
					FA_s.ID as SourceFusionAttributeID,
					FA_s.FusionAttributeTypeID as SourceFusionAttributeTypeID,
					FA_s.Name as SourceObject,
					FA_t.ID as TargetFusionAttributeID,
					FA_t.FusionAttributeTypeID as TargetFusionAttributeTypeID,
					FA_t.Name as TargetObject
				from
					[dbo].FusionAttribute FA_t
					inner join [dbo].FusionAttribute FA_s on (FA_s.ParentID = FA_t.ID and FA_s.FusionAttributeTypeId = @eagleSourceRuleItemTypeId)
				where
					FA_t.FusionAttributeTypeId = @eagleSourceRuleTypeId and FA_t.FusionID = @fusionId;

	----------------------------------------------------------
	-- INSERT 
	-- update the map rule item id's of already inserted items
	----------------------------------------------------------
	update T
			set T.mapruleitemid = S.id
			from #maps T
				inner join [dbo].[mapruleitem] S on (S.[owner] = 'EAGLE LINEAGE' and S.SourceFusionAttributeID = T.SourceFusionAttributeID and S.TargetFusionAttributeID = T.TargetFusionAttributeID);
	
	-- insert the mapruleitem records
	MERGE
		INTO    mapruleitem mri
		USING   (
				select SourceFusionAttributeID, TargetFusionAttributeID from #maps where mapruleitemid is null
				) S
		ON      (1 = 0)
		WHEN NOT MATCHED THEN
		INSERT  (SourceFusionAttributeID, TargetFusionAttributeID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner])
		VALUES  (S.SourceFusionAttributeID, S.TargetFusionAttributeID, 0, getutcdate(), 0, getutcdate(), 'EAGLE LINEAGE');
		
		--delete any maprule item records that are not in the map

	delete from mapruleitem where [owner] = 'EAGLE LINEAGE' and id not in(select m.mapruleitemid from #maps m);

	--testing / debug
	--select * from #maps;
	-- end testing / debug
end