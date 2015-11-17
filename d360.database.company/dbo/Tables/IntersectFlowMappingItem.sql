CREATE TABLE [dbo].[IntersectFlowMappingItem] (
    [IntersectFlowMappingID] INT      NOT NULL,
    [IntersectID]            INT      NOT NULL,
    [FromIntersectNodeID]    INT      NOT NULL,
    [ToIntersectNodeID]      INT      NOT NULL,
    [IntersectTypeRoleID]    INT      NOT NULL,
    [UpdatedOn]              DATETIME NULL,
    [UpdatedBy]              INT      NULL,
    CONSTRAINT [PK_IntersectFlowMappingItem] PRIMARY KEY CLUSTERED ([IntersectFlowMappingID] ASC, [IntersectID] ASC),
    CONSTRAINT [FK_IntersectFlowMappingItem_FromIntersectNodeID] FOREIGN KEY ([FromIntersectNodeID]) REFERENCES [dbo].[IntersectNode] ([ID]),
    CONSTRAINT [FK_IntersectFlowMappingItem_Intersect] FOREIGN KEY ([IntersectID]) REFERENCES [dbo].[Intersect] ([ID]) ON DELETE CASCADE,
    CONSTRAINT [FK_IntersectFlowMappingItem_IntersectFlowMapping] FOREIGN KEY ([IntersectFlowMappingID]) REFERENCES [dbo].[IntersectFlowMapping] ([ID]) ON DELETE CASCADE,
    CONSTRAINT [FK_IntersectFlowMappingItem_ToIntersectNodeID] FOREIGN KEY ([ToIntersectNodeID]) REFERENCES [dbo].[IntersectNode] ([ID])
);

