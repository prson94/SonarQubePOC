CREATE TYPE [dbo].[StaticFieldCandidateTable] AS TABLE (
    [ObjectType]               VARCHAR (50)   NOT NULL,
    [ObjectID]                 INT            NOT NULL,
    [StaticFieldVersionTypeID] INT            NOT NULL,
    [Value]                    NVARCHAR (MAX) NULL,
    [CreatingResourceID]       INT            NOT NULL,
    [DateCreated]              DATETIME       NOT NULL);

