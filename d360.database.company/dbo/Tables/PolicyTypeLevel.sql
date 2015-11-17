CREATE TABLE [dbo].[PolicyTypeLevel] (
    [PolicyTypeID] INT             NOT NULL,
    [Level]        INT             NOT NULL,
    [Name]         NVARCHAR (250)  NOT NULL,
    [Description]  NVARCHAR (4000) NULL,
    CONSTRAINT [PK_PolicyTypeLevel] PRIMARY KEY CLUSTERED ([PolicyTypeID] ASC, [Level] ASC),
    CONSTRAINT [FK_PolicyTypeLevel_PolicyType] FOREIGN KEY ([PolicyTypeID]) REFERENCES [dbo].[PolicyType] ([ID]) ON DELETE CASCADE
);

