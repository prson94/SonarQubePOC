CREATE TABLE [dbo].[Issue](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[IssueTypeID] [int] NOT NULL,	
	[Object] varchar(50) not null,
	[ObjectID] int not null,
	[ObjectType] varchar(25) not null,
	[ObjectTypeID] int not null,
	[CreatedOn] [datetime] NOT NULL,
	[CreatedBy] [int] NOT NULL,	
	[UpdatedOn] [datetime] NOT NULL DEFAULT GETUTCDATE(),
	[UpdatedBy] [int] NULL,	
	[Criticality] [int] NOT NULL DEFAULT 0,
	[CommentID] [int] NULL,
	CONSTRAINT [PK_Issue] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_Issue_IssueType] FOREIGN KEY ([IssueTypeID]) REFERENCES [dbo].[IssueType] ([ID]) ON DELETE CASCADE
)