CREATE PROCEDURE [fusion].[Rules] 
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
			@NumberOfNewReferenceItems int,
			@NumberOfNewReferences int,
			@NumberOfNewArtifacts int,
			@NumberOfAttributesTotal int,
			@NumberOfNewRelations int,
			@promotionNeedsToRun bit
	
	set	@NumberOfRules = 0;	
	set @NumberOfNewTaxonomies = 0;
	set @NumberOfNewReferenceItems = 0;
	set @NumberOfNewReferences = 0;
	set @NumberOfNewArtifacts = 0;
	set @promotionNeedsToRun = 1;

	--First check if there is anything to do
	EXEC @promotionNeedsToRun = [utility].[ShouldPromotionRun]

	if(@promotionNeedsToRun <= 0)
	BEGIN
		PRINT 'NO REASON TO RUN THE PROMOTION RULES WAS DETECTED';
		return;
	END;

	--Log this run get a new id from the fusion.promotion table
	insert into [fusion].[RuleLog] ( DateStarted ) values ( CURRENT_TIMESTAMP)
	select @ExecutionID =  SCOPE_IDENTITY()

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
					when M.SourceFieldName = 'ID' then cast(FA.ID as nvarchar)
					when M.SourceFieldName = 'Name' then FA.Name
					when M.SourceFieldName = 'TextPath' then FA.TextPath
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
						select	@ParentObject = 'Artifact',
								@ParentObjectID = ArtifactID
						from	FusionOwner
						where	@ParentSearchObject = 'Owner'
								and FusionID = @FusionID
								--and ID = @ParentSearchObjectID
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

						declare @modelTypeID int = null
						declare @taxonomyTypeValue nvarchar(250)

						select @taxonomyTypeValue = Value from @fields where TargetFieldName = 'TaxonomyTypeID'

--fusion.Rules
						if (@taxonomyTypeValue <> '' and @taxonomyTypeValue is not null)
						begin
							select @modelTypeID = ID from TaxonomyType where Name = ltrim(rtrim(@taxonomyTypeValue))
						end

						if @taxonomyTypeValue is null
						begin
							select @modelTypeID = min(ID) from TaxonomyType
						end

						if @ResultObjectID is null
							begin
								if @ParentObjectID = 0
								begin
									set @ParentObjectID = null
								end

								if @modelTypeID is not null
									begin
										insert into Artifact ( ParentID, ArtifactTypeID, TaxonomyTypeID, Name, Description, Status, UpdatedOn, UpdatedBy )
										values ( @ParentObjectID, @ObjectTypeIDToPromoteTo, @modelTypeID, @name, @description, 'Draft', getutcdate(), 0 )

										select @ResultObjectID =  SCOPE_IDENTITY()
										set @NumberOfNewArtifacts = @NumberOfNewArtifacts +1;
									end
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

								if @modelTypeID is not null
									begin
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
					end
					--END: IF ArtifactType

					if @ObjectTypeToPromoteTo = 'ReferenceItemType' OR @ObjectTypeToPromoteTo = 'ReferenceItem'
					begin
						-- You are promoting Reference items to a specific Reference (list)
						set @ResultObject = 'ReferenceItem'

						if @ResultObject is null and @ResultObjectID is null
							begin
								select	@ResultObjectID = ID
								from	ReferenceItem
								where	ReferenceItemTypeID = @ParentObjectID
										and lower(Code) = lower(@code)
							end
 
						if @ResultObjectID is null
							begin
								insert into ReferenceItem ( ReferenceItemTypeID, Code )
								values ( @ParentObject, @code )

								select @ResultObjectID =  SCOPE_IDENTITY()

								set @NumberOfNewReferenceItems = @NumberOfNewReferenceItems +1;
							end
					end
					--END: IF ReferenceType

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
						@FindParent int = null,
						@PromotionRuleStepID int = null

				select	@FindSearchType			= Value from @settings where Name = 'ObjectSearch'
				select	@FindSearchObject		= Value from @settings where Name = 'Object'
				select	@FindSearchObjectID		= Value from @settings where Name = 'ObjectID'
				select	@FindFilterField		= Value from @settings where Name = 'FilterField'
				select	@FindTargetField		= Value from @settings where Name = 'TargetField'
				select	@FindParent				= Value from @settings where Name = 'FindParent'
																
				if @FindSearchType = 'Fusion'
				begin					
					if @FindFilterField > 0
						begin
							if not exists(select 1 from @fields where SourceFieldTypeID = @FindFilterField)
								begin
									select	@FindFilterFieldValue = Value
									from	FieldWithRelation
									where	FieldTypeID = @FindFilterField
											and ObjectType = 'FusionAttribute'
											and ObjectID = @FusionAttributeID
								end
							else
								begin
									select	@FindFilterFieldValue = Value
									from	@fields
									where	SourceFieldTypeID = @FindFilterField
								end
						end
					else
						begin
							if not exists(select 1 from @fields where SourceFieldName = 'Name')
								begin
									select	@FindFilterFieldValue = TextPath
									from	FusionAttribute
									where	ID = @FusionAttributeID
								end
							else
								begin
									select	@FindFilterFieldValue = Value
									from	@fields
									where	SourceFieldName = 'Name'
								end
						end
					
					if @FindFilterFieldValue is not null
					begin
						select	top 1
								@ResultObject = 'FusionAttribute',
								@ResultObjectID = ID
						from	FusionAttribute
						where	@FindSearchObject = 'FusionAttributeType'
								and FusionAttributeTypeID = @FindSearchObjectID
								and (SourceID = @FindFilterFieldValue or TextPath = @FindFilterFieldValue or Name = @FindFilterFieldValue)
					end
				end

				--BEGIN: Find based on search type
				if @FindSearchType = 'FusionOwner'
				begin
					set	@ResultObject = 'Artifact'
					set @ResultObjectID = @FindSearchObjectID
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

				if @FindSearchType = 'Promotion' and @FindTargetField is null --by parent
				begin
					select	@ResultObject = ObjectType,
						    @ResultObjectID = ObjectID
					from	[fusion].[RulePromotion]
					join	FusionAttribute A on A.ID = @FusionAttributeID
					join	FusionAttribute AP on AP.ID = A.ParentID
					where	RuleStepID = @PromotionRuleStepID
							and FusionAttributeID = AP.ID
				end

				if @FindSearchType = 'Promotion' and @FindTargetField is not null -- by field
				begin
					select	@ResultObject = R.ObjectType, 
							@ResultObjectID = R.ObjectID 
					from	[fusion].[RulePromotion] R
					join	FusionAttribute SA on SA.ID = R.FusionAttributeID
					join	Field SF on SF.ObjectType = 'FusionAttribute' 
							and SF.ObjectID = SA.ID 
							and SF.FieldTypeID = @FindFilterField
					join	FusionAttribute TA on TA.ID = @FusionAttributeID
					join	Field TF on TF.ObjectType = 'FusionAttribute' 
							and TF.ObjectID = TA.ID 
							and TF.FieldTypeID = @FindTargetField
					where	R.RuleStepID = @PromotionRuleStepID 
							and SF.Value = TF.Value
				end

				--END: Find based on search type
			end --END: Find Action


			--BEGIN: FindRelation Action
			if @Action = 'FindRelation'
			begin
				declare @IntersectTypeID		int = null,
						@SearchType				nvarchar(250) = null,
						@FindRelationObject		varchar(50) = null,
						@FindRelationObjectID	int = null

				select	@IntersectTypeID		= Value from @settings where Name = 'IntersectType'
				select	@SearchType				= Value from @settings where Name = 'Search'
				select	@FindSearchObjectID		= Value from @settings where Name = 'ID'

				--BEGIN: Find based on search type

				if @SearchType = 'ResultFromStep'
				begin
					select	@FindRelationObject = ObjectType,
							@FindRelationObjectID = ObjectID
					from	[fusion].[RulePromotion]
					where	RuleID = @RuleID
							and RuleStepID = @FindSearchObjectID
							and FusionAttributeID = @FusionAttributeID
				end

				if @SearchType = 'Self'
				begin
					set @FindRelationObject = 'FusionAttribute'
					set @FindRelationObjectID = @FusionAttributeID
				end

				if @FindRelationObject is not null and @FindRelationObjectID is not null
				begin
					select	top 1
							@ResultObject = case 
												when (Subject = @FindRelationObject and SubjectID = @FindRelationObjectID) then Object
												else Subject
											end,
							@ResultObjectID = case 
												when (Subject = @FindRelationObject and SubjectID = @FindRelationObjectID) then ObjectID
												else SubjectID
											end
					from	[Intersect]
					where	IntersectTypeID = @IntersectTypeID
							and (
									(Subject = @FindRelationObject and SubjectID = @FindRelationObjectID) 
									OR (Object = @FindRelationObject and ObjectID = @FindRelationObjectID)
								)
				end

				--END: Find based on search type

			end --END: FindRelation Action

			
			--BEGIN: Lineage Action
			if @Action = 'Lineage'
			begin
				declare @SubjectSearchID int = null,
						@ObjectSearchID int = null,
						@Subject varchar(50) = null,
						@SubjectID int = null,
						@Object varchar(50) = null,
						@ObjectID int = null,

						@TechnicalSubjectSearchID int = null,
						@TechnicalObjectSearchID int = null,
						@RoleID int = null,

						@TechnicalSubject varchar(50) = null,
						@TechnicalSubjectID int  = null,
						@TechnicalObject varchar(50) = null,
						@TechnicalObjectID int  = null

				select	@SubjectSearchID			= Value from @settings where Name = 'SubjectID'
				select	@ObjectSearchID				= Value from @settings where Name = 'ObjectID'

				select	@TechnicalSubjectSearchID	= Value from @settings where Name = 'TechnicalSubjectID'
				select	@TechnicalObjectSearchID	= Value from @settings where Name = 'TechnicalObjectID'

				select	@RoleID						= Value from @settings where Name = 'Role'
				
				--BEGIN: Find subject based on search type, ALWAYS ResultFromStep
				select	@Subject = ObjectType,
						@SubjectID = ObjectID
				from	[Fusion].[RulePromotion]
				where	RuleID = @RuleID
						and RuleStepID = @SubjectSearchID
						and FusionAttributeID = @FusionAttributeID
				--END: Find subject based on search type

				--BEGIN: Find object based on search type
				select	@Object = ObjectType,
						@ObjectID = ObjectID
				from	[fusion].[RulePromotion]
				where	RuleID = @RuleID
						and RuleStepID = @ObjectSearchID
						and FusionAttributeID = @FusionAttributeID
				--END: Find object based on search type

				declare @Map table (ID int)

				--BEGIN: Add Map
				if @Subject = 'Intersect' and @SubjectID is not null and @Object = 'Intersect' and @ObjectID is not null
				begin
					MERGE	MapItem AS T
					USING	(
							SELECT	@SubjectID as SourceIntersectID, 
									@ObjectID as TargetIntersectID
							) as S
					ON		T.SourceIntersectID = S.SourceIntersectID
							and T.TargetIntersectID = S.TargetIntersectID 
					WHEN	NOT MATCHED THEN
							INSERT (SourceIntersectID, TargetIntersectID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn) 
							VALUES (S.SourceIntersectID, S.TargetIntersectID, 0, getutcdate(), 0, getutcdate())
					OUTPUT inserted.ID into @Map;
					
					set @ResultObject = 'MapItem'
					select top 1 @ResultObjectID = ID from @Map
				end
				--END: Add Map

				--BEGIN: Find subject based on search type, ALWAYS ResultFromStep
				if @TechnicalSubjectSearchID is not null and @TechnicalObjectSearchID is not null
				begin
					select	@TechnicalSubject = ObjectType,
							@TechnicalSubjectID = ObjectID
					from	[Fusion].[RulePromotion]
					where	RuleID = @RuleID
							and RuleStepID = @TechnicalSubjectSearchID
							and FusionAttributeID = @FusionAttributeID

					select	@TechnicalObject = ObjectType,
							@TechnicalObjectID = ObjectID
					from	[fusion].[RulePromotion]
					where	RuleID = @RuleID
							and RuleStepID = @TechnicalObjectSearchID
							and FusionAttributeID = @FusionAttributeID
				end
				--END: Find object based on search type

				declare @MapRule table (ID int)

				--BEGIN: Add Map
				if	@TechnicalSubject = 'FusionAttribute' and @TechnicalSubjectID is not null 
					and @TechnicalObject = 'FusionAttribute' and @TechnicalObjectID is not null
				begin
					MERGE	MapRuleItem AS T
					USING	(
							SELECT	@TechnicalSubjectID as SourceFusionAttributeID, 
									@TechnicalObjectID as TargetFusionAttributeID
							) as S
					ON		T.SourceFusionAttributeID = S.SourceFusionAttributeID
							and T.TargetFusionAttributeID = S.TargetFusionAttributeID 
					WHEN	NOT MATCHED THEN
							INSERT (SourceFusionAttributeID, TargetFusionAttributeID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn) 
							VALUES (S.SourceFusionAttributeID, S.TargetFusionAttributeID, 0, getutcdate(), 0, getutcdate())
					OUTPUT inserted.ID into @Map;
					
					set @ResultObject = 'MapRuleItem'
					select top 1 @ResultObjectID = ID from @MapRule
				end
				--END: Add Map

				if exists(select ID from @Map) and exists(select ID from @MapRule)
				begin
					merge	MapRuleItemMapItem as T
					using	(
							select	B.ID as MapItemID,
									T.ID as MapRuleItemID
							from	@Map B
									inner join @MapRule T on 1=1
							) as S
					on		T.MapRuleItemID = S.MapRuleItemID and T.MapItemID = S.MapItemID
					when	not matched then
							insert (MapRuleItemID, MapItemID)
							values (S.MapRuleItemID, S.MapItemID);

					delete from @Map
					delete from @MapRule
				end

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
					set	@R_Subject = 'Artifact'
					set @R_SubjectID = @R_ObjectSearchObjectID
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
					set	@R_Object = 'Artifact'
					set @R_ObjectID = @R_ObjectSearchObjectID
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
				if @R_IntersectTypeID is not null and @R_Subject is not null and @R_SubjectID is not null and @R_Object is not null and @R_ObjectID is not null
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

							select	@R_SubjectType = ObjectType, @R_SubjectTypeID = ObjectTypeID from cache.[object] where Object = @R_Subject and ObjectID = @R_SubjectID
							select	@R_ObjectType = ObjectType, @R_ObjectTypeID = ObjectTypeID from cache.[object] where Object = @R_Object and ObjectID = @R_ObjectID

							select	@R_IntersectTypeID = ID
							from	[IntersectType] R 
							where	Subject = @R_SubjectType and SubjectID = @R_SubjectTypeID 
									and Object = @R_ObjectType and ObjectID = @R_ObjectTypeID;


							if @R_IntersectTypeID is not null
							begin
								begin try
									insert into [Intersect] (IntersectTypeID, Classification, Subject, SubjectID, Object, ObjectID, Deleted, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn)
									values					(@R_IntersectTypeID, 2, @R_Subject, @R_SubjectID, @R_Object, @R_ObjectID, 0, @r, @d, @r, @d)  

									select @R_IntersectID = SCOPE_IDENTITY()

									--cache logic
									insert into cache.[Object] ( [Object], [ObjectID], [ObjectType], [ObjectTypeID] ) values	( 'Intersect', @R_IntersectID, 'IntersectType', @R_IntersectTypeID );

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

									set @NumberOfNewRelations = @NumberOfNewRelations + 1

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
						if @lookupObjectType = 'ReferenceItemType'
							begin
								select	top 1
										@objectResultID = ID
								from	ReferenceItem
								where	ReferenceItemTypeID = @lookupObjectID and Code = @fieldValue
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
	update	[fusion].[RuleLog]
	set		DateCompleted = CURRENT_TIMESTAMP, 
			[PromotedTaxonomies] = @NumberOfNewTaxonomies, 
			[PromotedDomainItems] = @NumberOfNewReferenceItems,  
			[PromotedDomains] = @NumberOfNewReferences,
			[PromotedArtifacts] = @NumberOfNewArtifacts,
			[TotalNewPromotions] = (@NumberOfNewTaxonomies + @NumberOfNewReferenceItems + @NumberOfNewReferences + @NumberOfNewArtifacts),
			[AttributesConsidered]= @NumberOfAttributesTotal,
			[NumberOfRules] = @NumberOfRules ,
			[RelationshipsAdded] = @NumberOfNewRelations
	where	ID = @ExecutionID;
END