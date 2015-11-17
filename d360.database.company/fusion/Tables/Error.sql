CREATE TABLE [fusion].[Error] (
    [ExecutionID] INT            NOT NULL,
    [Date]        DATETIME       NOT NULL,
    [Error]       NVARCHAR (MAX) NULL,
    CONSTRAINT [PK_FusionError] PRIMARY KEY CLUSTERED ([ExecutionID] DESC, [Date] ASC)
);

