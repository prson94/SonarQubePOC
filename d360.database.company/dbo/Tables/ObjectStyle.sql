CREATE TABLE [dbo].[ObjectStyle] (
    [ObjectType]    VARCHAR (50) NOT NULL,
    [ObjectID]      INT          NOT NULL,
    [IconBackColor] VARCHAR (7)  NOT NULL,
    [IconForeColor] VARCHAR (7)  NOT NULL,
    [IconText]      VARCHAR (25) NOT NULL,
    CONSTRAINT [PK_ObjectStyle] PRIMARY KEY CLUSTERED ([ObjectType] ASC, [ObjectID] ASC)
);






GO


GO

CREATE TRIGGER [dbo].[ObjectStyle_AfterUpsert]
	ON [dbo].[ObjectStyle]
	FOR INSERT, UPDATE
	AS
	BEGIN
		SET NOCOUNT ON;

		--update	T
		--set		T.IconBackColor = coalesce(S.IconBackColor, '#000000'),
		--		T.IconForeColor = coalesce(S.IconForeColor, '#ffffff'),
		--		T.IconText = coalesce(S.IconText, 'leaf')
		--from	cache.ObjectDetails T
		--		inner join inserted S on T.ObjectType = S.ObjectType and T.ObjectTypeID = S.ObjectID;

		--update	T
		--set		T.IconBackColor = coalesce(S.IconBackColor, '#000000'),
		--		T.IconForeColor = coalesce(S.IconForeColor, '#ffffff'),
		--		T.IconText = coalesce(S.IconText, 'leaf')
		--from	cache.ObjectDetails T
		--		inner join inserted S on T.[Object] = S.ObjectType and T.ObjectID = S.ObjectID;
	END