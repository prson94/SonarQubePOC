CREATE TABLE [dbo].[ScoreType] (
    [ID]          INT             IDENTITY (1, 1) NOT NULL,
    [Name]        NVARCHAR (250)  NOT NULL,
    [Description] NVARCHAR (4000) NULL,
    [CreatedOn]   DATETIME        CONSTRAINT [DF_ScoreType_CreatedOn] DEFAULT (getutcdate()) NULL,
    [CreatedBy]   INT             CONSTRAINT [DF_ScoreType_CreatedBy] DEFAULT ((0)) NULL,
    [UpdatedOn]   DATETIME        CONSTRAINT [DF_ScoreType_UpdatedOn] DEFAULT (getutcdate()) NULL,
    [UpdatedBy]   INT             CONSTRAINT [DF_ScoreType_UpdatedBy] DEFAULT ((0)) NULL,
    CONSTRAINT [PK_ScoreType] PRIMARY KEY CLUSTERED ([ID] ASC)
);

