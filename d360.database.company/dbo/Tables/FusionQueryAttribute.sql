CREATE TABLE [dbo].[FusionQueryAttribute] (
    [ID]                         INT           IDENTITY (1, 1) NOT NULL,
    [FusionQueryAttributeTypeID] INT           NOT NULL,
    [SourceID]                   VARCHAR (250) NULL,
    [CreatedOn]                  DATETIME      NULL,
    [CreatedBy]                  INT           NULL,
    [UpdatedOn]                  DATETIME      NULL,
    [UpdatedBy]                  INT           NULL,
    [Deleted]                    BIT           CONSTRAINT [DF_FusionQueryAttribute_Deleted] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_FusionQueryAttribute] PRIMARY KEY NONCLUSTERED ([ID] ASC),
    CONSTRAINT [FK_FusionQueryAttribute_FusionQueryAttributeType] FOREIGN KEY ([FusionQueryAttributeTypeID]) REFERENCES [dbo].[FusionQueryAttributeType] ([ID]) ON DELETE CASCADE
);

