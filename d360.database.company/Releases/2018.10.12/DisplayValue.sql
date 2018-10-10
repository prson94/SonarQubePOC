
CREATE TABLE [dbo].[AssetDisplayValue](
	[AssetID] [bigint] NOT NULL,
	[DisplayValue] [nvarchar](max) NOT NULL,
	[DisplayValueHash] [nvarchar](50) NULL,	
	[UpdatedOn] [datetime] NOT NULL DEFAULT(getutcdate())	
 CONSTRAINT [PK_AssetDisplayValue] PRIMARY KEY NONCLUSTERED 
(
	[AssetID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
)
go


create PROCEDURE UpdateDependentObjectTypeDisplayValues		
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
go

create PROCEDURE GenerateAssetDisplayValue	
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
GO

CREATE PROCEDURE GenerateAssetTypeDisplayValues	
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
GO



CREATE PROCEDURE GenerateAllAssetTypeDisplayValues	
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
GO


CREATE PROCEDURE CheckDisplayValues	
	
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
GO

