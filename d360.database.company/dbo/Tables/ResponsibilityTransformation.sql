CREATE TABLE [dbo].[ResponsibilityTransformation] (
    [ID]                               INT             IDENTITY (1, 1) NOT NULL,
    [ResponsibilityID]                 INT             NOT NULL,
    [ResponsibilityTransformationType] INT             NOT NULL,
    [Description]                      NVARCHAR (4000) NOT NULL,
    [UpdatedOn]                        DATETIME        NULL,
    [UpdatedBy]                        INT             NULL,
    CONSTRAINT [PK_ResponsibilityTransformation] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_ResponsibilityTransformation_Responsibility] FOREIGN KEY ([ResponsibilityID]) REFERENCES [dbo].[Responsibility] ([ID]) ON DELETE CASCADE
);

