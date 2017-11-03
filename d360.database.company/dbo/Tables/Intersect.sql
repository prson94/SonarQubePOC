CREATE TABLE [dbo].[Intersect] (
    [ID]              INT           IDENTITY (1, 1) NOT NULL,
    [IntersectTypeID] INT           NOT NULL,
    [Name]            AS            ([utility].[DeriveIntersectNameWrapper]([ID])),
    [Subject]         VARCHAR (50)  NULL,
    [SubjectID]       INT           NULL,
    [Object]          VARCHAR (50)  NULL,
    [ObjectID]        INT           NULL,
    [Deleted]         BIT           CONSTRAINT [DF_Intersect_Deleted] DEFAULT ((0)) NULL,
    [CreatedBy]       INT           CONSTRAINT [DF_Intersect_CreatedBy] DEFAULT ((0)) NULL,
    [CreatedOn]       DATETIME      CONSTRAINT [DF_Intersect_CreatedOn] DEFAULT (getutcdate()) NULL,
    [UpdatedBy]       INT           CONSTRAINT [DF_Intersect_UpdatedBy] DEFAULT ((0)) NULL,
    [UpdatedOn]       DATETIME      CONSTRAINT [DF_Intersect_UpdatedOn] DEFAULT (getutcdate()) NULL,
    [owner]           VARCHAR (100) NULL,
    [Visible]         BIT           CONSTRAINT [DF_Intersect_Visible] DEFAULT ((1)) NOT NULL,
    CONSTRAINT [PK_Intersect] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_Intersect_IntersectType] FOREIGN KEY ([IntersectTypeID]) REFERENCES [dbo].[IntersectType] ([ID]) ON DELETE CASCADE,
    CONSTRAINT [UQ_Intersect] UNIQUE NONCLUSTERED ([IntersectTypeID] ASC, [Subject] ASC, [SubjectID] ASC, [Object] ASC, [ObjectID] ASC)
);




















GO
CREATE NONCLUSTERED INDEX [IX_Intersect_IntersectTypeID]
    ON [dbo].[Intersect]([IntersectTypeID] ASC);


GO

GO





GO



GO
CREATE NONCLUSTERED INDEX [IX_Intersect_Subject]
    ON [dbo].[Intersect]([Subject] ASC, [SubjectID] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_Intersect_Object]
    ON [dbo].[Intersect]([Object] ASC, [ObjectID] ASC);


GO

CREATE NONCLUSTERED INDEX [IX_Intersect_Visible] 
	ON [dbo].[Intersect] ( Visible ASC );
go

GO

GO
CREATE NONCLUSTERED INDEX [IX_Intersect_IntersectTypeID_Subject_Object]
    ON [dbo].[Intersect]([IntersectTypeID] ASC, [Subject] ASC, [SubjectID] ASC, [Object] ASC, [ObjectID] ASC);

