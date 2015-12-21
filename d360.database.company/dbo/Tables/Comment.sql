CREATE TABLE [dbo].[Comment] (
    [ID]                 INT            IDENTITY (1, 1) NOT NULL,
    [ParentID]           INT            NULL,
    [CommentTypeID]      INT            NOT NULL,
    [Body]               NVARCHAR (MAX) NOT NULL,
    [DateCreated]        DATETIME       NOT NULL,
    [CreatingResourceID] INT            NOT NULL,
    [OwnerObjectType]    VARCHAR (50)   NULL,
    [OwnerObjectID]      INT            NULL,
    [VisibilityID]       INT            NULL,
    [IsDeleted]          BIT            DEFAULT ((0)) NULL,
    [DateEdited]         DATETIME       NULL,
    CONSTRAINT [PK_Comment] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_Comment_Parent] FOREIGN KEY ([ParentID]) REFERENCES [dbo].[Comment] ([ID])
);




GO
CREATE NONCLUSTERED INDEX [IX_Comment_ParentID]
    ON [dbo].[Comment]([ParentID] ASC);


GO

CREATE TRIGGER [dbo].[Comment_AfterInsert]
   ON  [dbo].[Comment] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	insert into [queue].[Task] ([Action], [Custom], [Object], ObjectID)
		select	'Notify',
				'1',
				'Comment',
				ID
		from	inserted
		where	CommentTypeID in (2, 3, 4, 5, 7, 8, 9)--(2, 7, 8, 9)

