CREATE TABLE [dbo].[IntersectFlowMappingContextItem] (
    [IntersectFlowMappingID] INT NOT NULL,
    [DomainItemID]           INT NOT NULL,
    CONSTRAINT [PK_IntersectFlowMappingContextItem] PRIMARY KEY CLUSTERED ([IntersectFlowMappingID] ASC, [DomainItemID] ASC),
    CONSTRAINT [FK_IntersectFlowMappingContextItem_DomainItem] FOREIGN KEY ([DomainItemID]) REFERENCES [dbo].[DomainItem] ([ID]) ON DELETE CASCADE,
    CONSTRAINT [FK_IntersectFlowMappingContextItem_IntersectFlowMapping] FOREIGN KEY ([IntersectFlowMappingID]) REFERENCES [dbo].[IntersectFlowMapping] ([ID]) ON DELETE CASCADE
);

