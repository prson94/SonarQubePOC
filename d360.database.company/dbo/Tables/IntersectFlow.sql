CREATE TABLE [dbo].[IntersectFlow] (
    [ID]                  INT            IDENTITY (1, 1) NOT NULL,
    [IntersectFlowTypeID] INT            NOT NULL,
    [Formula]             NVARCHAR (MAX) NULL,
    [UpdatedOn]           DATETIME       NULL,
    [UpdatedBy]           INT            NULL,
    CONSTRAINT [PK_IntersectFlow] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_IntersectFlow_IntersectFlowType] FOREIGN KEY ([IntersectFlowTypeID]) REFERENCES [dbo].[IntersectFlowType] ([ID]) ON DELETE CASCADE
);

