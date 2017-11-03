CREATE TABLE [dbo].[ResponsibilityTypeRelationOverrideItem] (
    [ID]                   BIGINT                                      IDENTITY (1, 1) NOT NULL,
    [ResponsibilityTypeID] INT                                         NOT NULL,
    [AssetID]              BIGINT                                      NOT NULL,
    [SecurityAsset]        CHAR (1)                                    NOT NULL,
    [SecurityAssetID]      INT                                         NOT NULL,
    [EffectiveStartDate]   DATETIME2 (0) GENERATED ALWAYS AS ROW START NOT NULL,
    [EffectiveEndDate]     DATETIME2 (0) GENERATED ALWAYS AS ROW END   NOT NULL,
    CONSTRAINT [PK_ResponsibilityTypeRelationOverrideItem] PRIMARY KEY NONCLUSTERED ([ID] ASC),
    PERIOD FOR SYSTEM_TIME ([EffectiveStartDate], [EffectiveEndDate])
)
WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE=[dbo].[ResponsibilityTypeRelationOverrideItem_History], DATA_CONSISTENCY_CHECK=ON));


GO
CREATE TRIGGER ResponsibilityTypeRelationOverrideItem_AfterDelete
   ON  dbo.ResponsibilityTypeRelationOverrideItem
   AFTER DELETE
AS 
BEGIN
	SET NOCOUNT ON;

	update	T
	set		T.Overriden = 0
	from	ResponsibilityTypeRelationItem T
			inner join deleted S on T.RuleID > 0 and S.AssetID = T.AssetID and S.ResponsibilityTypeID = T.ResponsibilityTypeID and T.Overriden = 1
			left join ResponsibilityTypeRelationItem E on E.RuleID = 0 and E.AssetID = S.AssetID and E.ResponsibilityTypeID = S.ResponsibilityTypeID and E.OverrideItemID <> S.ID
	where	E.AssetID is null;

	delete	T
	from	ResponsibilityTypeRelationItem T
			inner join deleted S on T.OverrideItemID = S.ID;
END
GO
CREATE TRIGGER ResponsibilityTypeRelationOverrideItem_AfterUpdate
   ON  dbo.ResponsibilityTypeRelationOverrideItem
   AFTER UPDATE
AS 
BEGIN
	SET NOCOUNT ON;
	update	T
	set		T.AssetID = S.AssetID,
			T.ResponsibilityTypeID = S.ResponsibilityTypeID,
			T.SecurityAsset = S.SecurityAsset,
			T.SecurityAssetID = S.SecurityAssetID
	from	ResponsibilityTypeRelationItem T
			inner join inserted S on S.ID = T.OverrideItemID
END
GO
CREATE TRIGGER ResponsibilityTypeRelationOverrideItem_AfterInsert
   ON  dbo.ResponsibilityTypeRelationOverrideItem
   AFTER INSERT
AS 
BEGIN
	SET NOCOUNT ON;

	insert into ResponsibilityTypeRelationItem (RuleID, ResponsibilityTypeID, AssetID, SecurityAsset, SecurityAssetID, OverrideItemID) 
		select	0, 
				ResponsibilityTypeID, 
				AssetID, 
				SecurityAsset, 
				SecurityAssetID, 
				ID
		from	inserted;

	update	T
	set		T.Overriden = 1
	from	ResponsibilityTypeRelationItem T
			inner join inserted S on T.RuleID > 0 and S.AssetID = T.AssetID and S.ResponsibilityTypeID = T.ResponsibilityTypeID and T.Overriden = 0;
END