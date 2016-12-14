CREATE TABLE [dbo].[IssueType](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](250) NOT NULL,
	[Description] [nvarchar](max) NULL,	
	[IsSystem] [bit] not null,
	[UpdatedOn] [datetime] NULL,
	[UpdatedBy] [int] NULL,	
 CONSTRAINT [PK_IssueType] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
,
 CONSTRAINT [CONST_IssueType_Name] UNIQUE NONCLUSTERED 
(
	[Name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
)

GO




CREATE TRIGGER [dbo].[IssueType_AfterInsert]
   ON  [dbo].[IssueType]
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Add', [queue].WriteIndexXml('', 'IssueType', ID, coalesce(UpdatedBy, 0)), 'IssueType', ID from inserted

	merge	[cache].[Object] as T
	using	(
			select	'IssueType' as [Object],			ID as ObjectID,
					'IssueType' as ObjectType,			0 as ObjectTypeID
			from	inserted
			) as S
	on		T.[Object] = S.[Object] and T.[ObjectID] = S.[ObjectID]
	when	matched then
			update set	T.[ObjectType] = S.[ObjectType],
						T.[ObjectTypeID] = S.[ObjectTypeID]
	when	not matched then
			insert	( [Object],		[ObjectID],		[ObjectType],	[ObjectTypeID]		)
			values	( S.[Object],	S.[ObjectID],	S.[ObjectType], S.[ObjectTypeID]	);
GO
CREATE TRIGGER [dbo].[IssueType_AfterDelete]
   ON  [dbo].[IssueType] 
   AFTER DELETE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Delete', [queue].WriteIndexXml('Removed', 'IssueType', ID, coalesce(UpdatedBy, 0)), 'IssueType', ID from deleted
GO

CREATE TRIGGER [dbo].[IssueType_AfterUpdate]
   ON  [dbo].[IssueType] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Update', [queue].WriteIndexXml('', 'IssueType', ID, coalesce(UpdatedBy, 0)), 'IssueType', ID from inserted

	merge	[cache].[Object] as T
	using	(
			select	'IssueType' as [Object],			ID as ObjectID,
					'IssueType' as ObjectType,			0 as ObjectTypeID
			from	inserted
			) as S
	on		T.[Object] = S.[Object] and T.[ObjectID] = S.[ObjectID]
	when	matched then
			update set	T.[ObjectType] = S.[ObjectType],
						T.[ObjectTypeID] = S.[ObjectTypeID]
	when	not matched then
			insert	( [Object],		[ObjectID],		[ObjectType],	[ObjectTypeID]		)
			values	( S.[Object],	S.[ObjectID],	S.[ObjectType], S.[ObjectTypeID]	);

GO

-- predefined system workflow types and there fields.

begin
	declare @id             INT;
	insert into [dbo].[issuetype] values('Business Data Incorrect','This type of issue is raised when there is a problem with business data from a source system that is referenced in Data3Sixty.',1,getutcdate(),0)
	SET @id = SCOPE_IDENTITY();
	insert into fieldtype (Name,FriendlyName,[Type],[Object],ObjectID,SortOrder,IsRequired,IsListable) values('ProblemDesc','Problem Description','Html','IssueType', @id,1,1,1)
end

begin
	declare @id             INT;
	insert into [dbo].[issuetype] values('Governance Information Incorrect','This type of issue is raised when there is a problem with the data governance information contained in Data3Sixty.',1,getutcdate(),0)
	SET @id = SCOPE_IDENTITY();
	insert into fieldtype (Name,FriendlyName,[Type],[Object],ObjectID,SortOrder,IsRequired,IsListable) values('ProblemDesc','Problem Description','Html','IssueType', @id,1,1,1)
end




