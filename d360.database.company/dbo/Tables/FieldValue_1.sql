CREATE TABLE [dbo].[FieldValue] (
    [ID]          INT            IDENTITY (1, 1) NOT NULL,
    [ObjectType]  VARCHAR (50)   NOT NULL,
    [ObjectID]    INT            NOT NULL,
    [FieldTypeID] INT            NOT NULL,
    [Value]       NVARCHAR (250) NULL,
    PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_FieldValue_FieldType] FOREIGN KEY ([FieldTypeID]) REFERENCES [dbo].[FieldType] ([ID]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_FieldValue_ObjectTypeObjectIDFieldTypeID]
    ON [dbo].[FieldValue]([ObjectType] ASC, [ObjectID] ASC, [FieldTypeID] ASC);

