CREATE TABLE [dbo].[FusionAttributeTypeCustomQuery] (
    [ID]                    INT            IDENTITY (1, 1) NOT NULL,
    [FusionID]              INT            NOT NULL,
    [FusionAttributeTypeID] INT            NOT NULL,
    [Query]                 NVARCHAR (MAX) NULL,
    CONSTRAINT [PK_FusionAttributeTypeCustomQuery] PRIMARY KEY NONCLUSTERED ([ID] ASC),
    CONSTRAINT [FK_FusionAttributeTypeCustomQuery_Fusion] FOREIGN KEY ([FusionID]) REFERENCES [dbo].[Fusion] ([ID]),
    CONSTRAINT [FK_FusionAttributeTypeCustomQuery_FusionAttributeType] FOREIGN KEY ([FusionAttributeTypeID]) REFERENCES [dbo].[FusionAttributeType] ([ID])
);

