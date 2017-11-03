CREATE TABLE [dbo].[Nym] (
    [ID]          INT            IDENTITY (1, 1) NOT NULL,
    [Object]      VARCHAR (25)   NOT NULL,
    [ObjectID]    INT            NOT NULL,
    [Name]        NVARCHAR (250) NULL,
    [PredicateID] INT            NOT NULL,
    [UpdatedOn]   DATETIME       CONSTRAINT [DF_Nym_UpdatedOn] DEFAULT (getutcdate()) NULL,
    [UpdatedBy]   INT            NULL,
    [CreatedOn]   DATETIME       CONSTRAINT [DF_Nym_CreatedOn] DEFAULT (getutcdate()) NOT NULL,
    [CreatedBy]   INT            NOT NULL,
    [Visible]     BIT            CONSTRAINT [DF_Nym_Visible] DEFAULT ((1)) NOT NULL,
    CONSTRAINT [PK_Nym] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_Nym_Predicate] FOREIGN KEY ([PredicateID]) REFERENCES [dbo].[Predicate] ([ID]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_Nym_Visible]
    ON [dbo].[Nym]([Visible] ASC);


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