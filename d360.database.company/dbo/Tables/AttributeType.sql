CREATE TABLE [dbo].[AttributeType] (
    [ID]                      INT             IDENTITY (50000, 1) NOT NULL,
    [ParentID]                INT             NULL,
    [Name]                    NVARCHAR (250)  NOT NULL,
    [Description]             NVARCHAR (4000) NULL,
    [TextFormatString]        NVARCHAR (250)  NOT NULL,
    [AttributeTypeCategoryID] INT             NULL,
    [UpdatedOn]               DATETIME        NULL,
    [UpdatedBy]               INT             NULL,
    CONSTRAINT [PK_AttributeType] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_AttributeType_AttributeTypeCategory] FOREIGN KEY ([AttributeTypeCategoryID]) REFERENCES [dbo].[AttributeTypeCategory] ([ID]),
    CONSTRAINT [FK_AttributeType_ParentAttributeType] FOREIGN KEY ([ParentID]) REFERENCES [dbo].[AttributeType] ([ID])
);


GO
CREATE NONCLUSTERED INDEX [IX_Attribute_ParentID]
    ON [dbo].[AttributeType]([ParentID] ASC);


GO
CREATE TRIGGER [dbo].[AttributeType_AfterDelete]
   ON  [dbo].[AttributeType] 
   AFTER DELETE
AS 
	SET NOCOUNT ON;

	DELETE	A
	FROM	AttributeTypeRelation as A
			INNER JOIN deleted AS d
	ON		A.AttributeTypeID = d.ID

	DELETE	F
	FROM	FieldType as F
			INNER JOIN deleted AS d
	ON		F.[Object] = 'AttributeType' and F.ObjectID = d.ID

	insert into [queue].[ObjectVersion] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select 'AttributeType', ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Removed', 'AttributeType', ID from deleted


GO
CREATE TRIGGER [dbo].[AttributeType_AfterInsert]
   ON  [dbo].[AttributeType] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	insert into [queue].[ObjectVersion] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select 'AttributeType', ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Created', 'AttributeType', ID from inserted

	declare @tbl table (RowID int identity, ID int)
	insert into @tbl 
		select ID from inserted

	declare @current int = 1,
			@max int,
			@thisID int
	select @max = max(RowID) from @tbl

	while @current <= @max
	begin
		select @thisID = ID from @tbl where RowID = @current
		exec [cache].[SynchronizeObjectDetails] 'AttributeType', @thisID
		set @current = @current + 1
	end

GO
CREATE TRIGGER [dbo].[AttributeType_AfterUpdate]
   ON  [dbo].[AttributeType] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;

	insert into [queue].[ObjectVersion] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select 'AttributeType', ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Updated', 'AttributeType', ID from inserted

	declare @tbl table (RowID int identity, ID int)
	insert into @tbl 
		select ID from inserted

	declare @current int = 1,
			@max int,
			@thisID int
	select @max = max(RowID) from @tbl

	while @current <= @max
	begin
		select @thisID = ID from @tbl where RowID = @current
		exec [cache].[SynchronizeObjectDetails] 'AttributeType', @thisID
		set @current = @current + 1
	end

GO
CREATE TRIGGER [dbo].[AttributeType_Delete]
   ON  [dbo].[AttributeType] 
FOR DELETE AS

BEGIN
DECLARE @Count int
SET @Count = 0;

IF 0 < (
	SELECT count(*) FROM deleted atts JOIN  statisticType statType on statType.Configuration.exist ('/fields[ObjectID=sql:column("atts.ID") and ObjectType="AttributeType"]') = 1
)

BEGIN
RAISERROR('You cannot delete an attribute if it is being used in an Analytic.  Please delete the analytic before deleting the attribute.',16,1)
ROLLBACK TRANSACTION
RETURN;
END

END
