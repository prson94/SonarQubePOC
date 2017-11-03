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
