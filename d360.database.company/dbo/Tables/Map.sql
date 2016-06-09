CREATE TABLE [dbo].[Map] (
    [ID]              INT             IDENTITY (1, 1) NOT NULL,
    [Name]            NVARCHAR (250)  NOT NULL,
    [IntersectRoleID] INT             NULL,
    [Transformation]  NVARCHAR (4000) NULL,
    [CreatedBy]       INT             CONSTRAINT [DF_Map_CreatedBy] DEFAULT ((0)) NOT NULL,
    [CreatedOn]       DATETIME        CONSTRAINT [DF_Map_CreatedOn] DEFAULT (getutcdate()) NOT NULL,
    [UpdatedBy]       INT             CONSTRAINT [DF_Map_UpdatedBy] DEFAULT ((0)) NOT NULL,
    [UpdatedOn]       DATETIME        CONSTRAINT [DF_Map_UpdatedOn] DEFAULT (getutcdate()) NOT NULL,
    CONSTRAINT [PK_Map] PRIMARY KEY NONCLUSTERED ([ID] ASC),
    CONSTRAINT [FK_Map_IntersectRole] FOREIGN KEY ([IntersectRoleID]) REFERENCES [dbo].[IntersectRole] ([ID])
);

