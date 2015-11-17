CREATE TABLE [dbo].[IntersectFlowType] (
    [ID]                         INT            IDENTITY (1, 1) NOT NULL,
    [Name]                       NVARCHAR (250) NOT NULL,
    [IntersectFlowConfiguration] INT            NOT NULL,
    [Description]                NVARCHAR (MAX) NULL,
    [UpdatedOn]                  DATETIME       NULL,
    [UpdatedBy]                  INT            NULL,
    CONSTRAINT [PK_IntersectFlowType] PRIMARY KEY CLUSTERED ([ID] ASC)
);

