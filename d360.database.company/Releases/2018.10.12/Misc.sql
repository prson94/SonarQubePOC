alter table [integration].[SynchedAssetTypeRelationItemTarget] add UseForWriteBack bit constraint DF_IntegrationSynchedAssetTypeRelationItemTarget_UseForWriteBack default(0) not null;
GO;

--alter view AssetDetail
GO;


ALTER proc [dbo].[GetPageInformation]
--declare 
	@o varchar(50),-- = 'Artifact',
	@oid int,-- = 23450,
	@rid int --= 1
as
begin
	declare @breadcrumbsRaw table ([Level] int, [TypeName] nvarchar(500), [Name] nvarchar(max), [TypeUrl] nvarchar(2500), [Url] nvarchar(2500));
	declare @breadcrumbs table ([Name] nvarchar(max), [Url] nvarchar(2500), Active bit, IsType bit);

	with h as
		(
		select	A.ID,
				A.[ObjectID], 
				A.AssetTypeID,
				I.SubjectID as [ParentID], 
				0 as [Level]
		from	Asset A
				left join PredicateIntersect I on I.Object = A.Object and I.ObjectID = A.ObjectID and I.PredicateType = 3
		where	A.[Object] = @o and A.ObjectID = @oid
		union all
		select	P.ID,
				P.[ObjectID] as ID, 
				P.AssetTypeID,
				I.SubjectID as ParentID, 
				h.[Level]-1 as [Level]
		from	Asset P
				inner join h on P.[Object] = @o and P.ObjectID = h.ParentID
				outer apply (
							select	SubjectID
							from	PredicateIntersect 
							where	Object = P.Object 
									and ObjectID = P.ObjectID 
									and PredicateType = 3
							) I
		)

	insert into @breadcrumbsRaw
		select		distinct	
					[Level],
					ltrim(rtrim(T.Name)),
					ltrim(rtrim(D.DisplayValue)),
					UT.Url,
					U.Url
		from		h 
					inner join AssetType T on T.ID = h.AssetTypeID
					left join dbo.GetAssetDisplayValue() D on D.ID = h.ID
					cross apply dbo.GetAssetUrl(@o, T.ObjectID, h.ObjectID) U
					cross apply dbo.GetAssetUrl(T.Object, T.ObjectID, T.ObjectID) UT
		where		ltrim(rtrim(T.Name)) is not null
					and ltrim(rtrim(D.DisplayValue)) is not null
		order by	[Level]

	declare @max int = 0,
			@min int
	select	@min = min([Level]) from @breadcrumbsRaw

	insert into @breadcrumbs values ('Glossary', null, 0, 0)

	while @min <= @max
	begin
		insert into @breadcrumbs
			select	TypeName, TypeUrl, 0, 1 from @breadcrumbsRaw where [Level] = @min

		insert into @breadcrumbs
			select	Name, 
					Url, 
					case @min when 0 then 1 else 0 end, 
					0 
			from	@breadcrumbsRaw 
			where	[Level] = @min

		set @min = @min + 1
	end

	select	distinct
			O.[Uid],
			A.ID,
			O.ID as AssetID,
			O.AssetTypeID,
			OD.DisplayValue,
			T.Name as [TypeName],
			case 
				when Dash.[Count] > 0 then cast(1 as bit)
				else cast(0 as bit)
			end as HasDashboards,
			case 
				when Work.[Count] > 0 then cast(1 as bit)
				else cast(0 as bit)
			end as HasWorkflow,
			case 
				when Child.[Count] > 0 then cast(1 as bit)
				else cast(0 as bit)
			end as HasChildArtifacts,
			case 
				when Attr.[Count] > 0 then cast(1 as bit)
				else cast(0 as bit)
			end as AllowAttributes,
			case 
				when Hier.[Count] > 0 then cast(1 as bit)
				else cast(0 as bit)
			end as AllowPredicateHierarchies,
			(
			select	*
			from	(
					select	P.ID as [ID],
							P.Name as [Name]
					from	[Predicate] P
					where	exists(SELECT * FROM IntersectType IT WHERE P.[type] = 6 and P.ID = IT.PredicateID and ((IT.Subject = T.Object and IT.SubjectID = T.ObjectID) OR (IT.Object = T.Object and IT.ObjectID =T.ObjectID)))
					union	
					select	P.ID as [ID], 
							P.Name as [Name] 
					from	[NymRelation] R 
							inner join [dbo].[predicate] P on P.ID = R.PredicateID where R.[Object] = T.Object and R.ObjectID = T.ObjectID
					) NMT
			for		json path
			)
			as NymTypes,
			(
			select	* 
			from	@breadcrumbs
			for		json path
			) as Breadcrumbs
	from	Artifact A 
			inner join Asset O on O.Object = @o and O.ObjectID = A.ID 
			inner join AssetType T on T.ID = O.AssetTypeID
			left join dbo.GetAssetDisplayValue() OD on OD.ID = O.ID
			--cross apply [dbo].GetAssetDisplayValueById(O.ID) as OD
			cross apply (
						select	count(1) as [Count]
						from	Report
						where	ObjectType = O.Object
								and ObjectID = T.ObjectID
						) Dash
			cross apply (
						select	count(1) as [Count]
						from	workflow.EventRegistration WER
								inner join workflow.Type WT on WER.TypeID = WT.ID and WT.PublishedVersionID is not null and WT.[State] = 1 and WER.ChangeType = 8 --ACTIVE
						where	WER.Object = T.Object
								and WER.ObjectID = T.ObjectID
						) Work
			cross apply (
						select	count(1) as [Count]
						from	[PredicateIntersect]
						where	Subject = O.Object
								and SubjectID = O.ObjectID
								and PredicateType = 3
						) Child
			cross apply (
						select	count(1) as [Count]
						from	AttributeTypeRelation
						where	ObjectType = T.Object and ObjectID = T.ObjectID
						) Attr
			cross apply (
						select	count(1) as [Count]
						from	IntersectType IT
								inner join [Predicate] P on P.ID = IT.PredicateID and P.[Type] = 3 -- TypeOf
						where	((IT.Subject = T.Object and IT.SubjectID = T.ObjectID) OR (IT.Object = T.Object and IT.ObjectID = T.ObjectID))
						) Hier
	where   A.ID = @oid 
			and A.[Visible] = 1 
			and not exists (select 1 from ResponsibilityDetail where PermissionsBitMask & 1 = 0 and ResourceID = @rid and ( (AssetID = O.ID) OR (AssetTypeID = O.AssetTypeID and AssetID = 0)))
	for json path, WITHOUT_ARRAY_WRAPPER
end
GO;


CREATE TABLE [dbo].[AssetDisplayFieldTypes](
	[AssetID] [bigint] NOT NULL,
	[FieldTypeID] [int] NOT NULL,
	[UpdatedOn] [datetime] NOT NULL,
 CONSTRAINT [PK_AssetDisplayFieldTypes] PRIMARY KEY NONCLUSTERED 
(
	[AssetID] ASC,
	[FieldTypeID] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF)
)
GO;

ALTER TABLE [dbo].[AssetDisplayFieldTypes] ADD  CONSTRAINT [DF_AssetDisplayFieldTypes_UpdatedOn]  DEFAULT (getutcdate()) FOR [UpdatedOn]
GO;

CREATE NONCLUSTERED INDEX [IX_Field_FieldTypeID_Include_date] ON [dbo].[Field] ([FieldTypeID]) INCLUDE ([UpdatedOn])
GO;

CREATE PROCEDURE [dbo].[GenerateAssetTypeDisplayValues]	
	@AssetTypeID int
AS
BEGIN
		SET NOCOUNT ON;
  DECLARE @trancount int;
  SET @trancount = @@trancount;
  BEGIN TRY
    IF @trancount = 0
      BEGIN TRANSACTION
      ELSE
        SAVE TRANSACTION usp_my_procedure_name;

			--delete by the asset type
			delete from AssetDisplayValue where assetid in (select id from asset where assettypeid = @AssetTypeID);

			insert into AssetDisplayValue (AssetID, DisplayValue, DisplayValueHash, UpdatedOn)
				select
					A.ID,
					ADV.DisplayValue,
					CONVERT(NVARCHAR(32),HashBytes('SHA1', ADV.DisplayValue),2) as DisplayValueHash,
					getutcdate()
				from
					Asset A
					cross apply GetAssetDisplayValueByID(A.ID) ADV
				where 
					A.AssetTypeID = @AssetTypeID and ADV.DisplayValue is not null		

				lbexit:
      IF @trancount = 0
      COMMIT;
  END TRY
  BEGIN CATCH
    DECLARE @error int,
            @message varchar(4000),
            @xstate int;

    SELECT
      @error = ERROR_NUMBER(),
      @message = ERROR_MESSAGE(),
      @xstate = XACT_STATE();

    IF @xstate = -1
      ROLLBACK;
    IF @xstate = 1 AND @trancount = 0
      ROLLBACK
    IF @xstate = 1 AND @trancount > 0
      ROLLBACK TRANSACTION usp_my_procedure_name;

    RAISERROR ('GenerateAllAssetTypeDisplayValues: %d: %s', 16, 1, @error, @message);
  END CATCH
END
GO;

CREATE PROCEDURE [dbo].[UpdateDependentObjectTypeDisplayValues]		
	@ChangedObject varchar(20),
	@ChangedObjectTypeID int
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	Declare @ObjectType varchar(20);
	Declare @ObjectID int;
	Declare @AssetTypeID int;

	SELECT @ChangedObject = REPLACE(@ChangedObject, 'Type', '');


	-- if there are any lookups on this type update this asset types display values 

	if exists (select 1 from FieldType where LookupObjectType = @ChangedObject and LookupObjectID = @ChangedObjectTypeID)
	begin
		Print 'Found dependent lookup fields updating them'
		-- loop through the affected types update there display values and call this function with there info
		Declare curP cursor LOCAL For

		 select distinct [Object] as ObjectType, ObjectID, AssetTypeID from FieldType where LookupObjectType = @ChangedObject and LookupObjectID = @ChangedObjectTypeID and AssetTypeID is not null

		OPEN curP 
		Fetch Next From curP Into @ObjectType, @ObjectID,@AssetTypeID

		While @@Fetch_Status = 0 Begin

			print 'Updating dependent AssetTypeID'
			print @AssetTypeID

			exec GenerateAssetTypeDisplayValues	@AssetTypeID

			--exec UpdateDependentObjectTypeDisplayValues  @ObjectType, @ObjectID

		Fetch Next From curP Into @ObjectType, @ObjectID,@AssetTypeID

		End -- End of Fetch

		Close curP
		Deallocate curP
	end

END
GO;

CREATE PROCEDURE [dbo].[GenerateAssetDisplayValue]	
	@AssetID bigint,
	@Object varchar(20),
	@ObjectID int
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	DECLARE @DisplayValue NVARCHAR(MAX);
	DECLARE @DisplayValueHash NVARCHAR(50);

	if @AssetID is null or @AssetID <= 0
	begin
		select @AssetID = id from asset where [object] = @Object and [objectid] = @ObjectID		
	end

	Select @displayValue = DisplayValue from GetAssetDisplayValueById(@AssetID);
	SELECT @DisplayValueHash = CONVERT(NVARCHAR(32),HashBytes('SHA1', @displayValue),2)
		
	Print 'DisplayValue: ' + @DisplayValue
	Print 'DisplayValueHash: ' + @DisplayValueHash
	-- if exists update
	

	if exists(select 1 from AssetDisplayValue where AssetID = @AssetID)
	begin		
			UPDATE AssetDisplayValue
				SET DisplayValue = A.DisplayValue,
					DisplayValueHash = @DisplayValueHash,
					UpdatedOn = getutcdate()
				FROM GetAssetDisplayValueById(@AssetID) A		
				where AssetID = @AssetID	
	end
	else
	begin
			insert into AssetDisplayValue (AssetID,DisplayValue,DisplayValueHash,UpdatedOn) values(@AssetID,@displayValue,@DisplayValueHash,getutcdate())
	end	

	Declare @assetObjectType varchar(20);
	Declare @assetObjectID int;
	
	select @assetObjectType = ATT.[Object], @assetObjectID = ATT.ObjectID from Asset A inner join AssetType ATT on A.AssetTypeID = ATT.ID where A.id = @AssetID

	exec UpdateDependentObjectTypeDisplayValues @assetObjectType,@assetObjectID	
END
GO;

CREATE PROCEDURE [dbo].[GenerateAllAssetTypeDisplayValues]	
AS
BEGIN
	SET NOCOUNT ON;
  DECLARE @trancount int;
  SET @trancount = @@trancount;
  BEGIN TRY
    IF @trancount = 0
      BEGIN TRANSACTION
      ELSE
        SAVE TRANSACTION usp_my_procedure_name;

			--delete by the asset type
			delete from AssetDisplayValue;

			insert into AssetDisplayValue (AssetID, DisplayValue, DisplayValueHash, UpdatedOn)
				select
					A.ID,
					ADV.DisplayValue,
					CONVERT(NVARCHAR(32),HashBytes('SHA1', ADV.DisplayValue),2) as DisplayValueHash,
					getutcdate()
				from
					Asset A
					cross apply GetAssetDisplayValueByID(A.ID) ADV		
				where ADV.DisplayValue is not null and A.[Object] != 'FusionAttribute'

		lbexit:
      IF @trancount = 0
      COMMIT;
  END TRY
  BEGIN CATCH
    DECLARE @error int,
            @message varchar(4000),
            @xstate int;

    SELECT
      @error = ERROR_NUMBER(),
      @message = ERROR_MESSAGE(),
      @xstate = XACT_STATE();

    IF @xstate = -1
      ROLLBACK;
    IF @xstate = 1 AND @trancount = 0
      ROLLBACK
    IF @xstate = 1 AND @trancount > 0
      ROLLBACK TRANSACTION usp_my_procedure_name;

    RAISERROR ('GenerateAllAssetTypeDisplayValues: %d: %s', 16, 1, @error, @message);
  END CATCH
END
GO;

CREATE PROCEDURE [dbo].[CheckDisplayValues]	
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	-- CHECK FOR ASSETS MISSING DISPLAY VALUES AND INSERT THEM
	insert into AssetDisplayValue (AssetID, DisplayValue, DisplayValueHash, UpdatedOn)
				select
					A.ID,
					ADV.DisplayValue,
					CONVERT(NVARCHAR(32),HashBytes('SHA1', ADV.DisplayValue),2) as DisplayValueHash,
					getutcdate()
				from
					Asset A
					cross apply GetAssetDisplayValueByID(A.ID) ADV		
				where ADV.DisplayValue is not null and not exists ( select 1 from assetdisplayvalue ad where ad.assetid = A.id)	
					and A.[Object] != 'FusionAttribute'
	
END
GO;