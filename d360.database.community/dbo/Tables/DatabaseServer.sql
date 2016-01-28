CREATE TABLE [dbo].[DatabaseServer] (
    [ID]       INT           IDENTITY (1, 1) NOT NULL,
    [Server]   VARCHAR (250) NOT NULL,
    [Username] VARCHAR (25)  NOT NULL,
    [Password] VARCHAR (25)  NOT NULL,
	[FusionQueue] VARCHAR(250)	   NOT NULL DEFAULT ('fusion-queue'),
    CONSTRAINT [PK_DatabaseServer] PRIMARY KEY CLUSTERED ([ID] ASC)
);

