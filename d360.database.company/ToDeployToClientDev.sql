--update old fieldtype html records to have no MaximumLength (Task 1789)
update FieldType
set MinimumLength = 1,
	MaximumLength = NULL
where [Type] = 'Html' AND IsRequired = 1;
go

update FieldType
set MinimumLength = NULL, MaximumLength = NULL
where [Type] = 'Html' AND IsRequired = 0;
go

--update RuleItem to use objectid/type instead of FusionAttributeID
sp_RENAME 'fusion.RuleItem.FusionAttributeID' , 'ObjectID', 'COLUMN'

alter table fusion.RuleItem add ObjectType nvarchar(250);
go

update fusion.RuleItem
set ObjectType = 'FusionAttribute' where ObjectType is null;
go

--add attribute type column to RulePromotion
sp_RENAME 'fusion.RulePromotion.FusionAttributeID' , 'AttributeID', 'COLUMN';

alter table fusion.RulePromotion drop constraint FK_FusionRulePromotion_FusionAttribute;
go
alter table fusion.RulePromotion add AttributeType varchar(25);
go
update fusion.RulePromotion set AttributeType = 'FusionAttribute' where AttributeType is null;
go
alter table fusion.RulePromotion alter column AttributeType varchar(25) not null;
go

CREATE TABLE [dbo].[Nym] (
    [ID]          INT            IDENTITY (1, 1) NOT NULL,
    [Object]      VARCHAR (25)   NOT NULL,
    [ObjectID]    INT            NOT NULL,
    [Name]        NVARCHAR (250) NULL,
    [PredicateID] INT            NOT NULL,
    [UpdatedOn]   DATETIME       DEFAULT (getutcdate()) NULL,
    [UpdatedBy]   INT            NULL,
    [CreatedOn]   DATETIME       DEFAULT (getutcdate()) NOT NULL,
    [CreatedBy]   INT            NOT NULL,
    CONSTRAINT [PK_Nym] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_Nym_Predicate] FOREIGN KEY ([PredicateID]) REFERENCES [dbo].[Predicate] ([ID]) ON DELETE CASCADE
);
GO

CREATE TRIGGER [dbo].[Nym_AfterDelete]
   ON  [dbo].[Nym] 
   AFTER DELETE
AS 
	SET NOCOUNT ON;
	declare @ot varchar(50) = 'Synonym'

	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select	'ObjectIndex',
				'D', 
				@ot, 
				ID
		from	deleted;
GO

CREATE TRIGGER [dbo].[Nym_AfterUpsert]
   ON  [dbo].[Nym] 
   AFTER INSERT, UPDATE
AS 
	SET NOCOUNT ON;
	declare @ot varchar(50) = 'Synonym'

	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select	'ObjectIndex',
				case 
					when D.ID is not null then 'U'
					else 'A'
				end, 
				@ot, 
				I.ID
		from	inserted I
				left join deleted D on D.ID = I.ID;
GO


CREATE TABLE [dbo].[NymRelation] (
    [ID]          INT          IDENTITY (1, 1) NOT NULL,
    [PredicateID] INT          NOT NULL,
    [Object]      VARCHAR (25) NOT NULL,
    [ObjectID]    INT          NOT NULL,
    [UpdatedOn]   DATETIME     NOT NULL,
    [UpdatedBy]   INT          NOT NULL,
    CONSTRAINT [PK_NymRelation] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_NymRelation_PredicateType] FOREIGN KEY ([PredicateID]) REFERENCES [dbo].[Predicate] ([ID]) ON DELETE CASCADE,
    CONSTRAINT [CONST_NymRelation_Name] UNIQUE NONCLUSTERED ([PredicateID] ASC, [Object] ASC, [ObjectID] ASC)
);
GO


alter table fusion.RulePromotion alter column AttributeType varchar(25) not null;
go

-- change 'synonym of' predicate to 'Synonym'
update predicate set name = 'Synonym', inverse = 'Synonym' where issystem = 1 and name = 'synonym of' and [type]  = 6
go

/*
CREATE TABLE [dbo].[SynonymTypeRelation] (
    [ID]          INT          IDENTITY (1, 1) NOT NULL,
    [PredicateID] INT          NOT NULL,
    [ObjectType]  VARCHAR (50) NOT NULL,
    [ObjectID]    INT          NOT NULL,
    [UpdatedOn]   DATETIME     NOT NULL,
    [UpdatedBy]   INT          NOT NULL,
    CONSTRAINT [PK_SynonymTypeRelation] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_SynonymTypeRelation_PredicateType] FOREIGN KEY ([PredicateID]) REFERENCES [dbo].[Predicate] ([ID]) ON DELETE CASCADE,
    CONSTRAINT [CONST_SynonymTypeRelation_Name] UNIQUE NONCLUSTERED ([PredicateID] ASC, [ObjectType] ASC, [ObjectID] ASC)
);
GO
*/

ALTER FUNCTION [utility].[GetFormattedFieldLookupValue]
(
	@Type varchar(25),
	@DisplayFormat nvarchar(250),
	@LookupObjectType varchar(25),
	@LookupObjectID int,
	@Value nvarchar(max)	
)
RETURNS nvarchar(max)
AS
BEGIN
	declare @formattedValue nvarchar(max)
	
	if @LookupObjectType is null
	begin
		set @formattedValue  = @Value

		if @Type = 'Link' OR @Type = 'UncLink'
		begin
			declare @linkName nvarchar(max),
					@linkUrl nvarchar(max)

			if charindex('|', @Value, 1) > 1
				begin
					SELECT @linkName = SUBSTRING(@Value, 1, PATINDEX('%|%', @Value)-1)
					SELECT @linkUrl = SUBSTRING(@Value, PATINDEX('%|%', @Value)+1, LEN(@Value))

					set @formattedValue = '<a href="' + @linkUrl + '" target="_blank">' + @linkName + '</a>'
				end
			else
				begin
					if @Value <> '' AND @Value <> '|' AND @Value IS NOT NULL
						begin
							if LEFT(@Value, 1) = '|'
								begin
									--no name, default to url
									set @formattedValue = '<a href="' + SUBSTRING(@Value,2, LEN(@Value)) + '" target="_blank">' + SUBSTRING(@Value,2, LEN(@Value)) + '</a>'
								end
							else
								begin
									set @formattedValue = '<a href="' + @Value + '" target="_blank">' + @Value + '</a>'
								end
						end
					else
						begin
							set @formattedValue = null
						end
				end
		end

	end	
	else
	begin
		if @LookupObjectType = 'ReferenceItemType'
		begin
			select @formattedValue = Name from ReferenceItemType where id = @Value;		
		end
		else
		begin
			declare @tokens table(ID int identity(1,1), Token nvarchar(100), Field nvarchar(100))
			declare @fieldValues table(Field nvarchar(100), Value nvarchar(max), LookupObjectType nvarchar(250), LookupObjectID int, LookupDisplayFormat nvarchar(250))

			set @formattedValue = @DisplayFormat
	
			while patindex('%{%',@formattedValue) > 0
			 begin
				declare @txt nvarchar(100) = SUBSTRING(@formattedValue, patindex('%{%',@formattedValue), PATINDEX('%}%', @formattedValue))
				insert into @tokens Values (@txt, REPLACE(REPLACE(@txt,'{',''),'}',''))
				set @formattedValue = replace(@formattedValue, @txt, '')
			end

			insert into @fieldValues
				select	distinct
						V.Name,
						V.Value,
						V.LookupObjectType,
						V.LookupObjectID,
						V.LookupDisplayFormat
				from	(
						SELECT	ID,
								Name,
								'Artifact' as ObjectType
						FROM	ArtifactType
						WHERE	@LookupObjectType = 'Artifact' and ID = @LookupObjectID
						UNION
						SELECT	ID,
								Name,
								'Lookup' as ObjectType
						FROM	[LookupType]
						WHERE	@LookupObjectType = 'Lookup' and ID = @LookupObjectID
						UNION
						SELECT	ID,
								Name,
								'ReferenceItem' as ObjectType
						FROM	[ReferenceItemType]
						WHERE	@LookupObjectType = 'ReferenceItem' and ID = @LookupObjectID
						UNION										
						SELECT	1 as ID,
								'User' as Name,
								'Resource' as ObjectType
						WHERE	@LookupObjectType = 'Resource'-- and ID = @LookupObjectID
						UNION
						SELECT	ID,
								Name,
								'Taxonomy' as ObjectType
						FROM	TaxonomyType
						WHERE	@LookupObjectType = 'Taxonomy' and ID = @LookupObjectID
						) L
						outer apply (

									SELECT	IT.Name,
											[IF].Value,
											[IT].LookupObjectType,
											COALESCE([IT].LookupObjectID, 0) as LookupObjectID,
											[IT].LookupDisplayFormat
									FROM	Field [IF]
											inner join FieldType IT ON [IF].FieldTypeID = IT.ID 
																	and [IF].ObjectType = L.ObjectType
																	and [IF].ObjectID = case 
																							when dbo.IsInteger(@Value) = 1 then @Value
																							else 0
																						end
								
									UNION

									SELECT	P.FieldName as Name,
											p.FieldValue as Value,
											NULL as LookupObjectType,
											NULL as LookupObjectID,
											NULL as LookupDisplayFormat
									FROM	(
											SELECT	ID as AID,
													CAST(ID as nvarchar(max)) as ID,
													CAST(Name as nvarchar(max)) as Name,
													CAST(Description as nvarchar(max)) as Description,
													CAST(TextPath as nvarchar(max)) as TextPath
											FROM	Artifact A
											WHERE	A.ID = CAST(@Value as int)
													and L.ObjectType = 'Artifact'
											) A
											unpivot	(
													FieldValue for FieldName in (ID, Name, Description, TextPath)
													) p

									UNION

									SELECT	P.FieldName as Name,
											p.FieldValue as Value,
											NULL as LookupObjectType,
											NULL as LookupObjectID,
											NULL as LookupDisplayFormat
									FROM	(
											SELECT	ID,
													CAST(Name as nvarchar(max)) as Name,
													CAST(Description as nvarchar(max)) as Description,
													CAST(TextPath as nvarchar(max)) as TextPath
											FROM	Taxonomy A
											WHERE	A.ID = CAST(@Value as int)
													and L.ObjectType = 'Taxonomy'
											) A
											unpivot	(
													FieldValue for FieldName in (Name, Description, TextPath)
													) p

									UNION

									SELECT	P.FieldName as Name,
											p.FieldValue as Value,
											NULL as LookupObjectType,
											NULL as LookupObjectID,
											NULL as LookupDisplayFormat
									FROM	(
											SELECT	ID,
													CAST(Code as nvarchar(max)) as Code
											FROM	ReferenceItem A
											WHERE	A.ID = @Value
													and L.ObjectType = 'ReferenceItem'
											) A
											unpivot	(
													FieldValue for FieldName in (Code)
													) p

									UNION

									SELECT	P.FieldName as Name,
											p.FieldValue as Value,
											NULL as LookupObjectType,
											NULL as LookupObjectID,
											NULL as LookupDisplayFormat
									FROM	(
											SELECT	ID,
													CAST(Name as nvarchar(max)) as Name,
													CAST(Description as nvarchar(max)) as Description
											FROM	ReferenceItemType A
											WHERE	A.ID = @Value
													and L.ObjectType = 'ReferenceItemType'
											) A
											unpivot	(
													FieldValue for FieldName in (Name, Description)
													) p

									UNION

									SELECT	P.FieldName as Name,
											p.FieldValue as Value,
											NULL as LookupObjectType,
											NULL as LookupObjectID,
											NULL as LookupDisplayFormat
									FROM	(
											SELECT	ResourceID as ID,
													CAST(FirstName as nvarchar(max)) as FirstName,
													CAST(LastName as nvarchar(max)) as LastName,
													CAST(Email as nvarchar(max)) as Email
											FROM	reporting.Global_Resource A
											WHERE	A.ResourceID = @Value
													and L.ObjectType = 'Resource'
											) A
											unpivot	(
													FieldValue for FieldName in (FirstName, LastName, Email)
													) p
									) V

			declare @current int,
					@max int

			set @current = 1
			select @max = Max(ID) from @tokens

			set @formattedValue = @DisplayFormat

			while(@current <= @max)
			begin
				declare @currentToken nvarchar(100) = null,
						@currentField nvarchar(100) = null,
						@currentValue nvarchar(max) = null,
						@lkpType nvarchar(250) = null, 
						@lkpID int = null, 
						@lkpFormat nvarchar(250) = null

				select	@currentField = Field, 
						@currentToken = Token 
				from	@tokens
				where	ID = @current

				select	@currentValue = Value,
						@lkpType = LookupObjectType,
						@lkpID = LookupObjectID,
						@lkpFormat = LookupDisplayFormat
				from	@fieldValues 
				where	Field = @currentField

				if @currentValue is not null
				begin
					if @lookupObjectType is not null and @lkpID is not null
					begin
						select @currentValue = utility.GetFormattedFieldLookupValue(@Type, @lkpFormat, @lkpType, @lkpID, @currentValue)
					end

					SET @formattedValue = REPLACE(@formattedValue, @currentToken, @currentValue)
				end

				SET @current = @current + 1
			end
		end
	end

	return @formattedValue
END
GO

ALTER PROCEDURE [fusion].[Rules] 
AS
BEGIN
	SET NOCOUNT ON;

	declare @d datetime = getutcdate(),
			@r int = 0,
			@RuleID int,
			@FusionID int,
			@AttributeID int,
			@ParentAttributeID int,
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

	--if(@promotionNeedsToRun <= 0)
	--BEGIN
	--	PRINT 'NO REASON TO RUN THE PROMOTION RULES WAS DETECTED';
	--	return;
	--END;

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
		FilterAttributeID int,
		FilterAttributeTypeID int
	);

	IF OBJECT_ID('tempdb..#attributes') IS NOT NULL
		DROP TABLE #attributes;

	create table #attributes (
		ID int identity,
		RuleID int,
		RuleStepID int,
		[Action] varchar(25),
		AttributeID int,
		ParentAttributeID int null,
		AttributeType varchar(25)
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
		Value nvarchar(max)
	);
	
	IF OBJECT_ID('tempdb..#fieldValues') IS NOT NULL
		DROP TABLE #fieldValues;

	create table #fieldValues (
		ObjectType varchar(50), 
		ObjectID int, 
		FieldTypeID int, 
		Value nvarchar(max)
	);
	
	insert into #rules
		select	R.ID,
				R.FusionID,
				R.ObjectType,
				R.ObjectID,
				I.ObjectID as FilterAttributeID,
				coalesce(A.FusionAttributeTypeID, Q.ID, R.ObjectID) as FilterAttributeTypeID--coalesce(A.FusionAttributeTypeID, F.ObjectID, Q.ID, R.ObjectID) as FilterAttributeTypeID
		from	[fusion].[Rule] R
				inner join [fusion].[RuleItem] I on I.RuleID = R.ID and R.[Enabled] = 1
				left join FusionAttribute A on A.ID = I.ObjectID AND I.ObjectType = 'FusionAttribute'
				left join FusionQueryAttributeType Q on Q.ID = R.ObjectID and R.ObjectType = 'FusionQueryAttribute'
				--left join FieldType F on F.ID = I.ObjectID and I.ObjectType = 'FusionQueryAttribute'

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
				@FilterAttributeID int,
				@FilterAttributeTypeID int

		select	@RuleID = RuleID,
				@FusionObjectType = ObjectType,
				@FusionObjectID = ObjectID,
				@FusionID = FusionID,
				@FilterAttributeID = FilterAttributeID,
				@FilterAttributeTypeID = FilterAttributeTypeID
		from	#rules
		where	ID = @currentID

		if @FusionObjectID = @FilterAttributeTypeID AND @FilterAttributeID is not null
			begin
				-- You are on a specific nodes of same type.  Just copy to target table.
				insert into #attributes 
					select	@RuleID, 
							S.ID,
							S.[Action],
							@FilterAttributeID,
							A.ParentID,
							@FusionObjectType
					from	[fusion].[RuleStep] S
							inner join FusionAttribute A on A.ID = @FilterAttributeID
					where	S.RuleID = @RuleID
					order by S.Step
			end
		else
			begin
				if @FusionObjectType = 'FusionQueryAttributeType'
					begin
						--take all query attributes
						if @FilterAttributeID is null
							begin
								insert into #attributes
									select	@RuleID,
											S.ID,
											S.[Action],
											FT.ID,
											NULL,
											@FusionObjectType
									from	FusionQueryAttribute FT
											inner join fusion.RuleStep S on S.RuleID = @RuleID and FT.FusionQueryAttributeTypeID = @FusionObjectID
							end
						else
							--take specific query attribute
							begin
								insert into #attributes
									select	@RuleID,
											S.ID,
											S.[Action],
											FT.ID,
											NULL,
											@FusionObjectType
									from	FusionQueryAttribute FT
											inner join fusion.RuleStep S on S.RuleID = @RuleID and FT.FusionQueryAttributeTypeID = @FusionObjectID and FT.ID = @FilterAttributeID
							end
					end
				else
					begin
						-- You are on an attribute higher up in hierarchy.	
						if @FilterAttributeID is null
						begin
							--  If there is NO filtered attribute ID, then you need to get every attribute in system for the partiular fusion instance.
							insert into #attributes
								select	@RuleID, 
										S.ID,
										S.[Action],
										FA.ID,
										FA.ParentID,
										@FusionObjectType
								from	FusionAttribute FA 
										inner join [fusion].[RuleStep] S on S.RuleID = @RuleID and FA.FusionID = @FusionID and FA.FusionAttributeTypeID = @FusionObjectID
										left join #attributes A on A.AttributeID = FA.ID and A.AttributeType = 'FusionAttributeType' and A.RuleID = S.RuleID and A.ID is null
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
										where	ID = @FilterAttributeID
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
										FA.ID,
										FA.ParentID,
										@FusionObjectType
								from	FA 
										inner join [fusion].[RuleStep] S on S.RuleID = @RuleID and FA.FusionAttributeTypeID = @FusionObjectID
										left join #attributes A on A.AttributeID = FA.ID and A.AttributeType = 'FusionAttributeType' and A.RuleID = S.RuleID and A.ID is null
								where	FA.FusionAttributeTypeID = @FusionObjectID
								order by FA.ID, S.Step
						end
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
				inner join [fusion].[RuleStep] RS on M.RuleStepID = RS.ID and (M.SourceFieldName in ('ID', 'Name', 'TextPath') OR M.IsConstantValue = 1)
				inner join #attributes A on A.RuleID = RS.RuleID
				inner join FusionAttribute FA on FA.ID = A.AttributeID and A.AttributeType = 'FusionAttributeType'

	insert into #fields
		select	A.ID,
				RS.RuleID,
				M.RuleStepID,
				M.SourceFieldName,
				M.SourceFieldTypeID,
				M.TargetFieldName,
				M.TargetFieldTypeID,
				F.FormattedValue
		from	[fusion].[RuleStepMapping] M
				inner join [fusion].[RuleStep] RS on M.RuleStepID = RS.ID and (M.SourceFieldName not in ('ID', 'Name', 'TextPath') AND M.IsConstantValue = 0)
				inner join #attributes A on A.RuleID = RS.RuleID and A.AttributeType = 'FusionAttributeType' --and A.AttributeID = M.SourceFieldTypeID
				inner join Field F on F.ObjectType = 'FusionAttribute' and F.ObjectID = A.AttributeID
				inner join FieldType FT on FT.ID = F.FieldTypeID and M.SourceFieldName = FT.Name


	--insert fusion query attribute fields
	insert into #fields
		select	A.ID,
				RS.RuleID,
				M.RuleStepID,
				M.SourceFieldName,
				M.SourceFieldTypeID,
				M.TargetFieldName,
				M.TargetFieldTypeID,
				case 
					when M.SourceFieldName = 'ID' then cast(A.AttributeID as nvarchar)
					when M.IsConstantValue = 1 then M.ConstantValue
				end
		from	[fusion].[RuleStepMapping] M
				inner join [fusion].[RuleStep] RS on M.RuleStepID = RS.ID and (M.SourceFieldName = 'ID' OR M.IsConstantValue = 1)
				inner join #attributes A on A.RuleID = RS.RuleID and A.AttributeType = 'FusionQueryAttributeType'

	insert into #fields
		select	A.ID,
				RS.RuleID,
				M.RuleStepID,
				M.SourceFieldName,
				M.SourceFieldTypeID,
				M.TargetFieldName,
				M.TargetFieldTypeID,
				F.FormattedValue
		from	[fusion].[RuleStepMapping] M
				inner join [fusion].[RuleStep] RS on M.RuleStepID = RS.ID and (M.SourceFieldName <> 'ID' AND M.IsConstantValue = 0)
				inner join #attributes A on A.RuleID = RS.RuleID and A.AttributeType = 'FusionQueryAttributeType' --and A.AttributeID = M.SourceFieldTypeID
				inner join Field F on F.ObjectType = 'FusionQueryAttribute' and F.ObjectID = A.AttributeID
				inner join FieldType FT on FT.ID = F.FieldTypeID and M.SourceFieldName = FT.Name

	-- Update the fields table above with values for all dynamic fields.
	--update	T
	--set		T.Value = S.Value
	--from	#fields T
	--		inner join #attributes A on A.ID = T.ID and A.AttributeType = 'FusionQueryAttributeType'
	--		inner join Field S on S.ObjectType = 'FusionQueryAttribute' and S.ObjectID = A.AttributeID;

	--update	T
	--set		T.Value = S.Value
	--from	#fields T
	--		inner join #attributes A on A.ID = T.ID and A.AttributeType = 'FusionAttributeType'
	--		inner join Field S on S.ObjectType = 'FusionAttribute' and S.ObjectID = A.AttributeID and S.FieldTypeID = T.SourceFieldTypeID

--BEGIN: TESTING ---------------------------------------

--select * from #rules;
--select * from #attributes order by ID;
--select * from #fields order by ID;

--drop table #attributes;
--drop table #fields;
--drop table #rules;

--END: TESTING ------------------------------------------

	set		@currentID = 1
	select	@maxID = MAX(ID) from #attributes

	set @NumberOfAttributesTotal = @maxID;
	
	while (@currentID <= @maxID)
	begin
		begin try

			declare @AttributeTypeID int = null,
					@AttributeType varchar(25) = null,
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
					@AttributeTypeID = R.ObjectID,
					@AttributeID = A.AttributeID,
					@AttributeType = replace(A.AttributeType,'Type',''),
					@ResultObject = P.ObjectType,
					@ResultObjectID = P.ObjectID
			from	#rules R
					inner join #attributes A on A.RuleID = R.RuleID and A.ID = @currentID
					left join [Fusion].RulePromotion P on P.AttributeID = A.AttributeID and P.AttributeType = replace(A.AttributeType, 'Type','') and P.RuleID = R.RuleID and P.RuleStepID = A.RuleStepID

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
								and AttributeID = @AttributeID
								and AttributeType = @AttributeType
					end
					--END: Find parent based on search type

--print @ParentObject
--print @ParentObjectID

					--BEGIN: Determine object type to promote as
					if @ObjectTypeToPromoteTo = 'ArtifactType'
					begin
						set @ResultObject = 'Artifact'

						if (@ResultObjectID is null) or not exists(select 1 from Artifact where ID = @ResultObjectID)
						begin
							select	@ResultObjectID = ID
							from	Artifact
							where	ArtifactTypeID = @ObjectTypeIDToPromoteTo
									and lower(Name) = lower(@name)

							if not exists(select 1 from Artifact where ID = @ResultObjectID)
							begin
								set @ResultObjectID = null
							end
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

										--DEBUGGING------------------------
										--select 
										--	@ParentObjectID as ParentObjectID,
										--	@ObjectTypeIDToPromoteTo as ObjectTypeIDToPromoteTo,
										--	@modelTypeID as modelTypeID, 
										--	@name as [name], 
										--	@description as [description], 
										--	@ResultObject as ResultObject, 
										--	@ResultObjectID as ResultObjectID,
										--    @RuleID as RuleID, @RuleStepID as RuleStepID;

										-- select * from @fields;
										------------------------------------

										insert into Artifact ( ParentID, ArtifactTypeID, TaxonomyTypeID, Name, Description, Status, UpdatedOn, UpdatedBy, CreatedOn )
										values ( @ParentObjectID, @ObjectTypeIDToPromoteTo, @modelTypeID, @name, @description, 'Draft', getutcdate(), 0, getutcdate() )

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
													TaxonomyTypeID = @modelTypeID,
													UpdatedOn = getutcdate(),
													UpdatedBy = 0
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

						if (@ResultObject is null and @ResultObjectID is null) or not exists(select 1 from ReferenceItem where ID = @ResultObjectID)
						begin
							select	@ResultObjectID = ID
							from	ReferenceItem
							where	ReferenceItemTypeID = @ParentObjectID
									and lower(Code) = lower(@code)

							if not exists(select 1 from ReferenceItem where ID = @ResultObjectID)
							begin
								set @ResultObjectID = null
							end
						end
 
						if @ResultObjectID is null
						begin
							insert into ReferenceItem ( ReferenceItemTypeID, Code, CreatedOn, CreatedBy, UpdatedOn, UpdatedBy )
							values ( @ParentObject, @code, getutcdate(), 0, getutcdate(), 0 )

							select @ResultObjectID =  SCOPE_IDENTITY()

							set @NumberOfNewReferenceItems = @NumberOfNewReferenceItems +1;
						end
					end
					--END: IF ReferenceType

					if @ObjectTypeToPromoteTo = 'TaxonomyType'
					begin
						set @ResultObject = 'Taxonomy'

						if (@ResultObjectID is null) or not exists(select 1 from Taxonomy where ID = @ResultObjectID)
						begin
							select	@ResultObjectID = ID
							from	Taxonomy
							where	TaxonomyTypeID = @ObjectTypeIDToPromoteTo
									and ParentID = @ParentObjectID
									and lower(Name) = lower(@name)

							if not exists(select 1 from Taxonomy where ID = @ResultObjectID)
							begin
								set @ResultObjectID = null
							end
						end

						if @ResultObjectID is null
						begin
							insert into Taxonomy	( ParentID, TaxonomyTypeID, Name, Description, UpdatedOn, UpdatedBy )
							values					( @ParentObjectID, @ObjectTypeIDToPromoteTo, @name, @description, getutcdate(), 0 )

							select @ResultObjectID =  SCOPE_IDENTITY()

							set @NumberOfNewTaxonomies = @NumberOfNewTaxonomies +1;
						end
						else
						begin
							update	Taxonomy
							set		Name = @Name,
									Description = @Description,
									UpdatedOn = getutcdate(),
									UpdatedBy = 0--,
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
											and ObjectType = @AttributeType
											and ObjectID = @AttributeID
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
									if @AttributeType = 'FusionQueryAttribute'
										begin
											select @FindFilterFieldValue = [Name]
											from FieldType FT
											where FT.ID = @AttributeID
										end
									else
										begin
											select	@FindFilterFieldValue = TextPath
											from	FusionAttribute
											where	ID = @AttributeID
										end
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
						if @AttributeType = 'FusionQueryAttribute'
							begin
								select top 1 
										@ResultObject = 'FusionQueryAttribute',
										@ResultObjectID = ID
								from	FieldType
								where	@FindSearchObject = 'FusionQueryAttributeType'
										and ObjectID = @FindSearchObjectID
										and [Object] = 'FusionQueryAttributeType'
										and Name = @FindFilterFieldValue
							end
						else
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
						if @FindFilterField = -2
							begin
								select	@FindFilterFieldValue = Name
								from	FusionAttribute
								where	ID = @ParentAttributeID
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
							and rp.AttributeID = @AttributeID
							and rp.AttributeType = @AttributeType

				end

				if @FindSearchType = 'ResultFromStep' and @FindParent is null
				begin
					select	@ResultObject = ObjectType,
							@ResultObjectID = ObjectID
					from	[fusion].[RulePromotion]
					where	@FindSearchObject = 'Step'
							and RuleID = @RuleID
							and RuleStepID = @FindSearchObjectID
							and AttributeID = @AttributeID
							and AttributeType = @AttributeType
				end

				if @FindSearchType = 'Promotion' and @FindTargetField is null --by parent
				begin
					select	@ResultObject = ObjectType,
						    @ResultObjectID = ObjectID
					from	[fusion].[RulePromotion]
					join	FusionAttribute A on A.ID = @AttributeID
					join	FusionAttribute AP on AP.ID = A.ParentID
					where	RuleStepID = @PromotionRuleStepID
							and AttributeID = AP.ID and AttributeType = 'FusionAttribute'
				end

				if @FindSearchType = 'Promotion' and @FindTargetField is not null -- by field
				begin
					select	@ResultObject = R.ObjectType, 
							@ResultObjectID = R.ObjectID 
					from	[fusion].[RulePromotion] R
					join	FusionAttribute SA on SA.ID = R.AttributeID
					join	Field SF on SF.ObjectType = 'FusionAttribute' 
							and SF.ObjectID = SA.ID 
							and SF.FieldTypeID = @FindFilterField
					join	FusionAttribute TA on TA.ID = @AttributeID
					join	Field TF on TF.ObjectType = 'FusionAttribute' 
							and TF.ObjectID = TA.ID 
							and TF.FieldTypeID = @FindTargetField
					where	R.RuleStepID = @PromotionRuleStepID 
							and SF.Value = TF.Value
							and R.AttributeType = 'FusionAttribute'
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
							and AttributeID = @AttributeID
							and AttributeType = @AttributeType
				end

				if @SearchType = 'Self'
				begin
					set @FindRelationObject = 'FusionAttribute'
					set @FindRelationObjectID = @AttributeID
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
						and AttributeID = @AttributeID
						and AttributeType = @AttributeType
				--END: Find subject based on search type

				--BEGIN: Find object based on search type
				select	@Object = ObjectType,
						@ObjectID = ObjectID
				from	[fusion].[RulePromotion]
				where	RuleID = @RuleID
						and RuleStepID = @ObjectSearchID
						and AttributeID = @AttributeID
						and AttributeType = @AttributeType
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
							and AttributeID = @AttributeID
							and AttributeType = @AttributeType

					select	@TechnicalObject = ObjectType,
							@TechnicalObjectID = ObjectID
					from	[fusion].[RulePromotion]
					where	RuleID = @RuleID
							and RuleStepID = @TechnicalObjectSearchID
							and AttributeID = @AttributeID
							and AttributeType = @AttributeType
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
							and AttributeID = @AttributeID
							and AttributeType = @AttributeType
				end

				if @R_SubjectSearchType = 'Self'
				begin
					set @R_Subject = @AttributeType
					set @R_SubjectID = @AttributeID
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
							and AttributeID = @AttributeID
							and AttributeType = @AttributeType
				end

				if @R_ObjectSearchType = 'Self'
				begin
					set @R_Object = @AttributeType
					set @R_ObjectID = @AttributeID

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
						SELECT	@AttributeID as AttributeID,
								@AttributeType as AttributeType, 
								@ResultObject as ObjectType, 
								@ResultObjectID as ObjectID, 
								@RuleID as RuleID,
								0 as PromotedObjectTypeID,
								@RuleStepID as RuleStepID
						) as S
				ON		T.RuleID = S.RuleID
						and T.RuleStepID = S.RuleStepID 
						and T.AttributeID = S.AttributeID 
						and T.AttributeType = S.AttributeType
						and T.ObjectType = S.ObjectType 
						and T.ObjectID = S.ObjectID
				WHEN	MATCHED THEN
						UPDATE SET	T.RuleID = S.RuleID, 
									T.ObjectTypeID = S.PromotedObjectTypeID,
									T.UpdatedOn = getutcdate()
				WHEN	NOT MATCHED THEN
						INSERT (AttributeID, AttributeType, ObjectType, ObjectID, RuleID, RuleStepID, ObjectTypeID, CreatedOn, UpdatedOn) 
						VALUES (S.AttributeID, S.AttributeType, S.ObjectType, S.ObjectID, S.RuleID, S.RuleStepID, S.PromotedObjectTypeID, getutcdate(), getutcdate());


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
				ERROR_LINE() as ErrorLine
				,ERROR_NUMBER() AS ErrorNumber
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
GO

--insert referenceitemtype into cache object so it shows up correctly on relationshiptype def screen
insert into cache.[object] ([object],[objectid],[objecttype],[objecttypeid]) values('ReferenceItemType',0,'ReferenceItemType',0)
go


-----Remove unique guid from fusion results table for performance / as it is not used and is slow------------------

-- drop old primary key
ALTER TABLE [fusion].[result]
DROP CONSTRAINT PK_FusionResult;
GO

-- drop the constraint that generates the id
ALTER TABLE [fusion].[result]
DROP CONSTRAINT DF_FusionResult_ID;
GO

-- make the unique id column nullable
ALTER TABLE [fusion].[result] ALTER COLUMN [ID] uniqueidentifier NULL ;
GO

-- add index on fusion id and parent id to fusion attribute table
CREATE INDEX IX_FusionID_ParentID 
	ON FusionAttribute (FusionID, ParentID);  
GO