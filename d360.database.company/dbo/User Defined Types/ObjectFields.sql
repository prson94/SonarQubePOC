CREATE TYPE [dbo].[ObjectFields] AS TABLE (
    [FieldTypeID]   INT            NULL,
    [FieldTypeName] NVARCHAR (250) NOT NULL,
    [FieldValue]    NVARCHAR (MAX) NULL,
    PRIMARY KEY CLUSTERED ([FieldTypeName] ASC));

