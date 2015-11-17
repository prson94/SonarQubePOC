CREATE TABLE [dbo].[IntersectFlowItem] (
    [ID]                  INT      IDENTITY (1, 1) NOT NULL,
    [ParentID]            INT      NULL,
    [IntersectFlowID]     INT      NOT NULL,
    [IntersectID]         INT      NOT NULL,
    [FromIntersectNodeID] INT      NOT NULL,
    [ToIntersectNodeID]   INT      NOT NULL,
    [UpdatedOn]           DATETIME NULL,
    [UpdatedBy]           INT      NULL,
    CONSTRAINT [PK_IntersectFlowItem] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_IntersectFlowItem_FromIntersectNodeID] FOREIGN KEY ([FromIntersectNodeID]) REFERENCES [dbo].[IntersectNode] ([ID]),
    CONSTRAINT [FK_IntersectFlowItem_Intersect] FOREIGN KEY ([IntersectID]) REFERENCES [dbo].[Intersect] ([ID]) ON DELETE CASCADE,
    CONSTRAINT [FK_IntersectFlowItem_IntersectFlow] FOREIGN KEY ([IntersectFlowID]) REFERENCES [dbo].[IntersectFlow] ([ID]) ON DELETE CASCADE,
    CONSTRAINT [FK_IntersectFlowItem_Parent] FOREIGN KEY ([ParentID]) REFERENCES [dbo].[IntersectFlowItem] ([ID]),
    CONSTRAINT [FK_IntersectFlowItem_ToIntersectNodeID] FOREIGN KEY ([ToIntersectNodeID]) REFERENCES [dbo].[IntersectNode] ([ID])
);

