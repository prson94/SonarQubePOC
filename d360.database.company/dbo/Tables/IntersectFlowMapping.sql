CREATE TABLE [dbo].[IntersectFlowMapping] (
    [ID]              INT            IDENTITY (1, 1) NOT NULL,
    [ParentID]        INT            NULL,
    [IntersectFlowID] INT            NOT NULL,
    [Definition]      NVARCHAR (MAX) NULL,
    [Formula]         NVARCHAR (MAX) NULL,
    [UpdatedOn]       DATETIME       NULL,
    [UpdatedBy]       INT            NULL,
    CONSTRAINT [PK_IntersectFlowMapping] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_IntersectFlowMapping_IntersectFlow] FOREIGN KEY ([IntersectFlowID]) REFERENCES [dbo].[IntersectFlow] ([ID]) ON DELETE CASCADE,
    CONSTRAINT [FK_IntersectFlowMapping_Parent] FOREIGN KEY ([ParentID]) REFERENCES [dbo].[IntersectFlowMapping] ([ID])
);

