CREATE TABLE [dbo].[AlertFlag] (
    [ID]         INT          IDENTITY (1, 1) NOT NULL,
    [ObjectType] VARCHAR (50) NOT NULL,
    [ObjectID]   INT          NOT NULL,
    [CommentID]  INT          NOT NULL,
    [Date]       DATETIME     NOT NULL,
    [Active]     BIT          NOT NULL,
    CONSTRAINT [PK_AlertFlag] PRIMARY KEY CLUSTERED ([ID] ASC)
);


GO
CREATE NONCLUSTERED INDEX [IX_AlertFlag_ObjectType-ObjectID-Active]
    ON [dbo].[AlertFlag]([ObjectType] ASC, [ObjectID] ASC, [Active] DESC);


GO
CREATE NONCLUSTERED INDEX [IX_AlertFlag_Date]
    ON [dbo].[AlertFlag]([Date] DESC);


GO
CREATE NONCLUSTERED INDEX [IX_AlertFlag_Active]
    ON [dbo].[AlertFlag]([Active] DESC);


GO
EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'The column contains a pre-defined set of values based on the SystemObjects enumeration in code.', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'AlertFlag', @level2type = N'COLUMN', @level2name = N'ObjectType';


GO
EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'This collumn contains the integer ID of the underlying object this row is associated with.  This combined with the ObjectType column value gives the location of the item.', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'AlertFlag', @level2type = N'COLUMN', @level2name = N'ObjectID';

