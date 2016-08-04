CREATE TABLE [dbo].[Load] (
    [ID]            INT             IDENTITY (1, 1) NOT NULL,
    [File]          VARBINARY (MAX) NOT NULL,
    [Object]        VARCHAR (50)    CONSTRAINT [DF_Load_Object] DEFAULT (N'') NOT NULL,
    [ObjectID]      INT             CONSTRAINT [DF_Load_ObjectID] DEFAULT ((0)) NOT NULL,
    [Notes]         NVARCHAR (4000) NULL,
    [Extension]     VARCHAR (10)    CONSTRAINT [DF_Load_Extension] DEFAULT (N'.xlsx') NOT NULL,
    [Action]        VARCHAR (1)     CONSTRAINT [DF_Load_Action] DEFAULT (N'P') NOT NULL,
    [DateStarted]   DATETIME        CONSTRAINT [DF_Load_DateStarted] DEFAULT (getutcdate()) NOT NULL,
    [DateCompleted] DATETIME        NULL,
    [UpdatedBy]     INT             CONSTRAINT [CK_Load_UpdatedBy] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_Load] PRIMARY KEY CLUSTERED ([ID] ASC)
);












GO
CREATE TRIGGER [dbo].[Load_AfterInsert]
	ON [dbo].[Load]
	FOR INSERT
AS
	SET NOCOUNT ON;
	insert into [queue].[BulkLoad] (LoadID)
		select ID from inserted where [File] is not null
GO
DISABLE TRIGGER [dbo].[Load_AfterInsert]
    ON [dbo].[Load];


