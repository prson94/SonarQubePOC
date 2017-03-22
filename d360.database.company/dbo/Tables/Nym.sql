CREATE TABLE [dbo].[Nym] (
	[ID]			 INT			IDENTITY (1, 1) NOT NULL,
    [Object]     VARCHAR (25)   NOT NULL,
    [ObjectID]       INT            NOT NULL,    
    [Name]	         NVARCHAR (250) NULL,
    [PredicateID]	 INT			NOT NULL,	 
	[UpdatedOn]		 DATETIME		NULL default GETUTCDATE(),
	[UpdatedBy]		 INT			NULL,
	[CreatedOn]		 DATETIME		NOT NULL default GETUTCDATE(),
	[CreatedBy]		 INT			NOT NULL,
	[Visible]		 BIT			NOT NULL default (1),
	CONSTRAINT [PK_Nym] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_Nym_Predicate] FOREIGN KEY ([PredicateID]) REFERENCES [dbo].[Predicate] ([ID]) ON DELETE CASCADE
);

go

CREATE NONCLUSTERED INDEX [IX_Nym_Visible] 
	ON [dbo].[Nym] ( Visible ASC );
go

CREATE TRIGGER [dbo].[Nym_AfterDelete]
   ON  [dbo].[Nym] 
   AFTER DELETE
AS 
	SET NOCOUNT ON;
	declare @ot varchar(50) = 'Synonym'

	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select	'ObjectIndex',
				'D', 
				@ot, 
				ID
		from	deleted;

GO

ALTER TABLE [dbo].[Nym] ENABLE TRIGGER [Nym_AfterDelete]
GO


CREATE TRIGGER [dbo].[Nym_AfterUpsert]
   ON  [dbo].[Nym] 
   AFTER INSERT, UPDATE
AS 
	SET NOCOUNT ON;
	declare @ot varchar(50) = 'Synonym'

	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select	'ObjectIndex',
				case 
					when D.ID is not null then 'U'
					else 'A'
				end, 
				@ot, 
				I.ID
		from	inserted I
				left join deleted D on D.ID = I.ID;

GO

ALTER TABLE [dbo].[Nym] ENABLE TRIGGER [Nym_AfterUpsert]
GO