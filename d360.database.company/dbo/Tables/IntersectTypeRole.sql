CREATE TABLE [dbo].[IntersectTypeRole] (
    [ID]        INT            IDENTITY (1, 1) NOT NULL,
    [Name]      NVARCHAR (250) NOT NULL,
    [UpdatedOn] DATETIME       NULL,
    [UpdatedBy] INT            NULL,
    CONSTRAINT [PK_IntersectTypeRole] PRIMARY KEY CLUSTERED ([ID] ASC)
);


GO


CREATE TRIGGER [dbo].[IntersectTypeRole_AfterUpdate]
	ON [dbo].[IntersectTypeRole]
	FOR UPDATE
	AS
	BEGIN
		SET NOCOUNT ON;
		update	T
		set		T.[Role] = I.Name
		from	[cache].[Relationships] T
				inner join [Intersect] S on S.ID = T.IntersectID
				inner join inserted I on I.ID = S.IntersectTypeRoleID
	END
