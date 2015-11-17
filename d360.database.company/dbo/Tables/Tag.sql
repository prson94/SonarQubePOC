CREATE TABLE [dbo].[Tag] (
    [ID]        INT            IDENTITY (1, 1) NOT NULL,
    [Name]      NVARCHAR (250) NOT NULL,
    [UpdatedOn] DATETIME       NULL,
    [UpdatedBy] INT            NULL,
    CONSTRAINT [PK_Tag] PRIMARY KEY CLUSTERED ([ID] ASC)
);

