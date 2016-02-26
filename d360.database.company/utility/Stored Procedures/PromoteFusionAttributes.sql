CREATE PROCEDURE [utility].[PromoteFusionAttributes]
AS
BEGIN
	SET NOCOUNT ON;

	declare @d datetime = getutcdate(),
			@r int = 0,
			@RuleID int,
			@PromotionObjectType varchar(50),
			@PromotionObjectID int,
			@PromotionParentObjectType varchar(50),
			@PromotionParentObjectID int,
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

	EXEC @promotionNeedsToRun = [utility].[ShouldPromotionRun]

	if(@promotionNeedsToRun <= 0)
	BEGIN
		PRINT 'NO REASON TO RUN THE PROMOTION RULES WAS DETECTED';
		return;
	END;


	--Log this run get a new id from the fusion.promotion table
	insert into [dbo].[FusionAttributePromotionLogSummary] ( DateStarted )
									values ( CURRENT_TIMESTAMP)

	select @ExecutionID =  SCOPE_IDENTITY()

	IF OBJECT_ID('tempdb..#rules') IS NOT NULL
		DROP TABLE #rules;

	create table #rules (
		ID int identity,
		RuleID int,
		FusionID int,
		ObjectType varchar(25),
		ObjectID int,
		PromotionObjectType varchar(25),
		PromotionObjectID int,
		PromotionParentObjectType varchar(25),
		PromotionParentObjectID int,
		FilterFusionAttributeID int,
		FilterFusionAttributeTypeID int
	);

	IF OBJECT_ID('tempdb..#attributes') IS NOT NULL
		DROP TABLE #attributes;

	create table #attributes (
		ID int identity,
		RuleID int,
		FusionAttributeID int
	);

	IF OBJECT_ID('tempdb..#fields') IS NOT NULL
		DROP TABLE #fields;

	create table #fields (
		ID int, 
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
				R.PromotionObjectType,
				R.PromotionObjectID,
				R.PromotionParentObjectType,
				R.PromotionParentObjectID,
				I.FusionAttributeID as FilterFusionAttributeID,
				coalesce(A.FusionAttributeTypeID, R.ObjectID) as FilterFusionAttributeTypeID
		from	FusionAttributePromotionRule R
				inner join FusionAttributePromotionRuleItem I on I.FusionAttributePromotionRuleID = R.ID and R.[Enabled] = 1
				left join FusionAttribute A on A.ID = I.FusionAttributeID

	
	declare	@currentID int,
			@maxID int

	set		@currentID = 1
	select	@maxID = MAX(ID) from #rules

	select @NumberOfRules = count(1) from FusionAttributePromotionRule where [Enabled] = 1;

	while (@currentID <= @maxID)
	begin
		declare @ObjectType varchar(25),
				@ObjectID int,
				@FilterFusionAttributeID int,
				@FilterFusionAttributeTypeID int


		select	@RuleID = RuleID,
				@ObjectType = ObjectType,
				@ObjectID = ObjectID,
				@PromotionObjectType = PromotionObjectType,
				@PromotionObjectID = PromotionObjectID,
				@PromotionParentObjectType = PromotionParentObjectType,
				@PromotionParentObjectID = PromotionParentObjectID,
				@FusionID = FusionID,
				@FilterFusionAttributeID = FilterFusionAttributeID,
				@FilterFusionAttributeTypeID = FilterFusionAttributeTypeID
		from	#rules
		where	ID = @currentID

		if @ObjectID = @FilterFusionAttributeTypeID AND @FilterFusionAttributeID is not null
			begin
				-- You are on a specific nodes of same type.  Just copy to target table.
				insert into #attributes values (@RuleID, @FilterFusionAttributeID)
			end
		else
			begin
				-- You are on an attribute higher up in hierarchy.
				if @FilterFusionAttributeID is null
					begin
						--  If there is NO filtered attribute ID, then you need to get every attribute in system for the partiular fusion instance.
						insert into #attributes
							select	@RuleID, FA.ID 
							from	FusionAttribute FA 
									left join #attributes A on A.FusionAttributeID = FA.ID and A.RuleID = @RuleID and A.ID is null
							where	FA.FusionID = @FusionID 
									and FA.FusionAttributeTypeID = @ObjectID
					end
				else
					begin
						-- If there is a filter attribute ID, then traverse the hierarchy and get all attributes of the specified type.
						with fa as	(
									select	ID,
											ParentID,
											FusionAttributeTypeID
									from	FusionAttribute
									where	ID = @FilterFusionAttributeID
									union all
									select	C.ID,
											C.ParentID,
											C.FusionAttributeTypeID
									from	FusionAttribute C
											inner join fa P on C.ParentID = P.ID --and P.ID <> C.ID
									)
	
						insert into #attributes
							select	@RuleID, fa.ID 
							from	fa 
									left join #attributes A on A.FusionAttributeID = fa.ID and A.RuleID = @RuleID and A.ID is null
							where	fa.FusionAttributeTypeID = @ObjectID
					end
			end

		set @currentID = @currentID + 1
	end


	-- Load field values we are working with, first starting with the Name.
	insert into #fields
		select	A.ID,
				M.SourceFieldName,
				M.SourceFieldTypeID,
				M.TargetFieldName,
				M.TargetFieldTypeID,
				case 
					when M.SourceFieldName = 'Name' then FA.Name					
					when M.IsConstantValue = 1 then M.ConstantValue
				end				
		from	FusionAttributePromotionRuleMapping M
				inner join #attributes A on A.RuleID = M.FusionAttributePromotionRuleID
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

			declare @FusionAttributeTypeID int,
					@PromotedType varchar(50),
					@PromotedID int

			declare @fields table (SourceFieldName nvarchar(250), SourceFieldTypeID int, TargetFieldName nvarchar(250), TargetFieldTypeID int, Value nvarchar(4000))

			select	@RuleID = R.RuleID,
					@FusionID = R.FusionID,
					@FusionAttributeTypeID = R.ObjectID,
					@PromotionObjectType = R.PromotionObjectType,
					@PromotionObjectID = R.PromotionObjectID,
					@PromotionParentObjectType = R.PromotionParentObjectType,
					@PromotionParentObjectID = R.PromotionParentObjectID,
					@FusionAttributeID = A.FusionAttributeID,
					@PromotedType = P.ObjectType,
					@PromotedID = P.ObjectID
			from	#rules R
					inner join #attributes A on A.RuleID = R.RuleID and A.ID = @currentID
					left join FusionAttributePromotion P on P.FusionAttributeID = A.FusionAttributeID and P.FusionAttributePromotionRuleID = R.RuleID

			--Load up fields were are working with for this loop instance.
			insert into @fields
				select SourceFieldName, SourceFieldTypeID, TargetFieldName, TargetFieldTypeID, Value from #fields where ID = @currentID

			if exists(select 1 from @fields where TargetFieldName = 'Name')
				begin
					declare @code nvarchar(50) = null,
							@name nvarchar(250) = null,
							@description nvarchar(4000) = null

					select @code = Value from @fields where TargetFieldName = 'Code'
					select @name = Value from @fields where TargetFieldName = 'Name'
					select @description = coalesce(Value, '') from @fields where TargetFieldName = 'Description'

					if @PromotionObjectType = 'ArtifactType'
						begin
							set @PromotedType = 'Artifact'

							if @PromotedID is null
								begin
									select	@PromotedID = ID
									from	Artifact
									where	ArtifactTypeID = @PromotionObjectID
											and lower(Name) = lower(@name)
								end

							declare @modelTypeID int
							select @modelTypeID = min(ID) from TaxonomyType

							if @PromotedID is null
								begin
									insert into Artifact ( ParentID, ArtifactTypeID, TaxonomyTypeID, Name, Description, Status, UpdatedOn, UpdatedBy )
									values ( @PromotionParentObjectID, @PromotionObjectID, @modelTypeID, @name, @description, 'Draft', getutcdate(), 0 )

									select @PromotedID =  SCOPE_IDENTITY()

									set @NumberOfNewArtifacts = @NumberOfNewArtifacts +1;
								end
							else
							  begin
									declare @testArtifactName nvarchar(250) = null,
											@testArtifactDescription nvarchar(4000) = null,
											@testArtifactParentID int = null,
											@testArtifactTaxonomyTypeID int = null

									select	@testArtifactName = Name,
											@testArtifactDescription = coalesce(Description, ''),
											@testArtifactParentID = ParentID,
											@testArtifactTaxonomyTypeID = TaxonomyTypeID
									from	Artifact
									where	ID = @PromotedID
									
									if (@testArtifactName <> @name) 
										OR (@testArtifactDescription <> @description) 
										OR (@testArtifactParentID <> @PromotionParentObjectID) 
										OR (@testArtifactTaxonomyTypeID <> @modelTypeID)
									begin																				
										update	Artifact
										set		Name = @name,
												Description = @description,
												ParentID = @PromotionParentObjectID,
												TaxonomyTypeID = @modelTypeID
										where	ID = @PromotedID
									end
								end
						end
 
					if @PromotionObjectType = 'DomainType'
						begin
							if @PromotionParentObjectType is null and @PromotionParentObjectID is null
								begin
									set @PromotedType = 'Domain'
									
									-- You are promoting to a Domain (creating a list)
									if @PromotedID is null
										begin
											select	@PromotedID = ID
											from	Domain
											where	DomainTypeID = @PromotionObjectID
													and lower(Name) = lower(@name)
										end
 
									if @PromotedID is null
										begin
											insert into Domain  ( DomainTypeID, Name, Description ) 
											values ( @PromotionObjectID, @name, @description )

											select @PromotedID =  SCOPE_IDENTITY()

											set @NumberOfNewDomains = @NumberOfNewDomains +1;
										end
									else
										begin
											update	Domain
											set		Name = @name,
													Description = @description
											where	ID = @PromotedID
										end
								end
							else
								begin
									-- You are promoting domain items to a specific domain (list)
									set @PromotedType = 'DomainItem'

									if @PromotedType is null and @PromotedID is null
										begin
											select	@PromotedID = ID
											from	DomainItem
											where	DomainID = @PromotionParentObjectID
													and lower(Code) = lower(@code)
										end
 
									if @PromotedID is not null
										begin
											update	DomainItem
											set		Name = @name,
													Code = coalesce(@code, @name),
													Description = @description
											where	ID = @PromotedID
										end
									else
										begin
											insert into DomainItem ( DomainID, Name, Code, Description )
											values ( @PromotionParentObjectID, @name, coalesce(@code, @name), @description )

											select @PromotedID =  SCOPE_IDENTITY()

											set @NumberOfNewDomainItems = @NumberOfNewDomainItems +1;
										end
								end
						end

					if @PromotionObjectType = 'TaxonomyType'
						begin
							set @PromotedType = 'Taxonomy'

							if @PromotedID is null
								begin
									select	@PromotedID = ID
									from	Taxonomy
									where	TaxonomyTypeID = @PromotionObjectID
											and ParentID = @PromotionParentObjectID
											and lower(Name) = lower(@name)
								end

							if @PromotedID is null
								begin
									insert into Taxonomy	( ParentID, TaxonomyTypeID, Name, Description )
									values					( @PromotionParentObjectID, @PromotionObjectID, @name, @description )

									select @PromotedID =  SCOPE_IDENTITY()

									set @NumberOfNewTaxonomies = @NumberOfNewTaxonomies +1;
								end
							else
								begin
									update	Taxonomy
									set		Name = @Name,
											Description = @Description--,
											--ParentID = @PromotionParentObjectID
									where	ID = @PromotedID
 								end
						end

					-- Add/Update the promotion record to keep track of the auto-promotions
					if @PromotedType is not null and @PromotedID is not null
						begin
							-- Insert/Update the FusionAttributePromotion table to keep track of previously promoted objects.
							
							MERGE	FusionAttributePromotion AS T
							USING	(
									SELECT	@FusionAttributeID as FusionAttributeID, 
											@PromotedType as ObjectType, 
											@PromotedID as ObjectID, 
											@RuleID as RuleID,
											@PromotionObjectID as PromotedObjectTypeID
									) as S
							ON		T.FusionAttributeID = S.FusionAttributeID 
									and T.ObjectType = S.ObjectType 
									and T.ObjectID = S.ObjectID
							WHEN	MATCHED THEN
									UPDATE SET T.FusionAttributePromotionRuleID = S.RuleID, ObjectTypeID = S.PromotedObjectTypeID
							WHEN	NOT MATCHED THEN
									INSERT (FusionAttributeID, ObjectType, ObjectID, FusionAttributePromotionRuleID, ObjectTypeID) 
									VALUES (S.FusionAttributeID, S.ObjectType, S.ObjectID, S.RuleID, S.PromotedObjectTypeID);
						end

					-- Add/Update the dynamic fields involved.
					if @PromotedType is not null and @PromotedID is not null
						begin
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
											
											if @PromotedID is not null and @objectResultID is not null
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
											If not EXISTS (SELECT 1 FROM #fieldValues where ObjectType = @PromotedType and ObjectID = @PromotedID and FieldTypeID = @targetFieldTypeID) --avoid duplicates this happens in gmo
											begin
												insert into #fieldValues (ObjectType, ObjectID, FieldTypeID, Value) values(@PromotedType, @PromotedID, @targetFieldTypeID, @fieldValue)
											end
										end
						
									-- Delete the field we just finished processing.
									delete @fields where TargetFieldTypeID = @targetFieldTypeID
								end 
						end
				end -- Check to see if Target Field called NAME is present
								
		end try
		begin catch
			--SELECT 
				--ERROR_NUMBER() AS ErrorNumber
				--,ERROR_MESSAGE() AS ErrorMessage;
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
					select f.ObjectType as ObjectType,
							f.ObjectID as ObjectID,
							f.FieldTypeID as FieldTypeID,
							f.Value as Value
					from #fieldValues f inner join dbo.FieldType ft on (ft.ID = f.FieldTypeID)
				) as S
				on		T.ObjectType = S.ObjectType and T.ObjectID = S.ObjectID and T.FieldTypeID = S.FieldTypeID
				when	matched then
					update set T.Value = S.Value
				when	not matched then
					insert (ObjectTYpe, OBjectID, FieldTypeID, Value)
					values (S.ObjectType, S.ObjectID, S.FieldTypeID, S.Value);
	end

	-- Add new relations as needed
	exec [utility].[PromoteFusionAttributesRelations] @NumberOfNewRelations output

	-- Handle any fusionlookup fields
	exec [utility].[PromoteFusionAttributeLookups]
	
		
	--Log this run done
	update [dbo].[FusionAttributePromotionLogSummary]
	set DateCompleted = CURRENT_TIMESTAMP, 
		[PromotedTaxonomies] = @NumberOfNewTaxonomies, 
		[PromotedDomainItems] = @NumberOfNewDomainItems,  
		[PromotedDomains] = @NumberOfNewDomains,
		[PromotedArtifacts] = @NumberOfNewArtifacts,
		[TotalNewPromotions] = (@NumberOfNewTaxonomies + @NumberOfNewDomainItems + @NumberOfNewDomains + @NumberOfNewArtifacts),
		[AttributesConsidered]= @NumberOfAttributesTotal,
		[NumberOfRules] = @NumberOfRules ,
		[RelationshipsAdded] = @NumberOfNewRelations
	where ID = @ExecutionID;
	
END
