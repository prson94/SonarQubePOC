CREATE TABLE [dbo].[FusionAttribute] (
    [ID]                    INT             IDENTITY (1, 1) NOT NULL,
    [ParentID]              INT             NULL,
    [Name]                  NVARCHAR (250)  NOT NULL,
    [FusionID]              INT             NOT NULL,
    [FusionAttributeTypeID] INT             NOT NULL,
    [SourceID]              VARCHAR (250)   NULL,
    [Deleted]               BIT             CONSTRAINT [DF_FusionAttribute_Deleted] DEFAULT ((0)) NOT NULL,
    [Path]                  XML             NULL,
    [TextPath]              NVARCHAR (2500) NULL,
    CONSTRAINT [PK_FusionAttribute] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_FusionAttribute_Fusion] FOREIGN KEY ([FusionID]) REFERENCES [dbo].[Fusion] ([ID]) ON DELETE CASCADE,
    CONSTRAINT [FK_FusionAttribute_FusionAttributeType] FOREIGN KEY ([FusionAttributeTypeID]) REFERENCES [dbo].[FusionAttributeType] ([ID]) ON DELETE CASCADE
);




GO
CREATE NONCLUSTERED INDEX [IX_FusionAttribute_FusionAttributeTypeID]
    ON [dbo].[FusionAttribute]([FusionAttributeTypeID] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_FusionAttribute_FusionID]
    ON [dbo].[FusionAttribute]([FusionID] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_FusionAttribute_FusionID-FusionAttributeTypeID-ParentID]
    ON [dbo].[FusionAttribute]([FusionID] ASC, [FusionAttributeTypeID] ASC, [ParentID] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_FusionAttribute_FusionID-FusionAttributeTypeID-SourceID]
    ON [dbo].[FusionAttribute]([FusionID] ASC, [FusionAttributeTypeID] ASC, [SourceID] ASC);


GO



GO



GO
CREATE NONCLUSTERED INDEX [IX_FusionAttribute_FusionID-SourceID]
    ON [dbo].[FusionAttribute]([FusionID] ASC, [SourceID] ASC);

