CREATE TYPE [dbo].[RelationshipTypeTable] AS TABLE (
    [ID]                        INT          IDENTITY (1, 1) NOT NULL,
    [startpromotedobjecttype]   VARCHAR (25) NULL,
    [startpromotedobjecttypeid] INT          NULL,
    [endpromotedobjecttype]     VARCHAR (25) NULL,
    [endpromotedobjecttypeid]   INT          NULL);

