CREATE TABLE [dbo].[AttributeTypeRelation] (
    [AttributeTypeID]      INT          NOT NULL,
    [ObjectType]           VARCHAR (50) NOT NULL,
    [ObjectID]             INT          NOT NULL,
    [AllowMultipleEntries] BIT          CONSTRAINT [DF_AttributeTypeRelation_AllowMultipleEntries] DEFAULT ((1)) NOT NULL,
    CONSTRAINT [PK_AttributeTypeRelation] PRIMARY KEY CLUSTERED ([AttributeTypeID] ASC, [ObjectType] ASC, [ObjectID] ASC),
    CONSTRAINT [FK_AttributeTypeRelation_AttributeType] FOREIGN KEY ([AttributeTypeID]) REFERENCES [dbo].[AttributeType] ([ID]) ON DELETE CASCADE
);

