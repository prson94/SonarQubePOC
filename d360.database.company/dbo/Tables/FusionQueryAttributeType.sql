CREATE TABLE [dbo].[FusionQueryAttributeType] (
    [ID]        INT             IDENTITY (1, 1) NOT NULL,
    [FusionID]  INT             NOT NULL,
    [Name]      NVARCHAR (250)  NOT NULL,
    [Query]     NVARCHAR (2500) NOT NULL,
    [CreatedOn] DATETIME        NULL,
    [CreatedBy] INT             NULL,
    [UpdatedOn] DATETIME        NULL,
    [UpdatedBy] INT             NULL,
    CONSTRAINT [PK_FusionQueryAttributeType] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_FusionQueryAttributeType_Fusion] FOREIGN KEY ([FusionID]) REFERENCES [dbo].[Fusion] ([ID]) ON DELETE CASCADE
);


GO
CREATE TRIGGER [dbo].[FusionQueryAttributeType_AfterUpsert]
   ON  [dbo].[FusionQueryAttributeType] 
   AFTER INSERT, UPDATE
AS 
	SET NOCOUNT ON;
	merge	[cache].[Object] as T
	using	(
			select	'FusionQueryAttributeType' as [Object],			ID as ObjectID,
					'Fusion' as ObjectType,					FusionID as ObjectTypeID
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

CREATE TRIGGER [dbo].[FusionQueryAttributeType_AfterDelete]
   ON  [dbo].[FusionQueryAttributeType] 
   AFTER DELETE
AS 
	SET NOCOUNT ON;
	delete	T
	from	[cache].[Object] T
			inner join deleted S on T.Object = 'FusionQueryAttributeType' and T.ObjectID = S.ID