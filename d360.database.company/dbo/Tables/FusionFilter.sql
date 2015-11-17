CREATE TABLE [dbo].[FusionFilter] (
    [FusionID]              INT            NOT NULL,
    [FusionAttributeTypeID] INT            NOT NULL,
    [Filter]                NVARCHAR (500) NOT NULL,
    CONSTRAINT [PK_FusionFilter] PRIMARY KEY CLUSTERED ([FusionID] ASC, [FusionAttributeTypeID] ASC),
    CONSTRAINT [FK_FusionFilter_Fusion] FOREIGN KEY ([FusionID]) REFERENCES [dbo].[Fusion] ([ID]) ON DELETE CASCADE,
    CONSTRAINT [FK_FusionFilter_FusionAttributeType] FOREIGN KEY ([FusionAttributeTypeID]) REFERENCES [dbo].[FusionAttributeType] ([ID]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_FusionFilter_FusionID]
    ON [dbo].[FusionFilter]([FusionID] ASC);

