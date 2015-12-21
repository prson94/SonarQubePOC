CREATE TABLE [dbo].[IntersectMapContextItem] (
    [IntersectMapID] INT          NOT NULL,
    [Object]         VARCHAR (50) NOT NULL,
    [ObjectID]       INT          NOT NULL,
    CONSTRAINT [PK_IntersectMapContextItem] PRIMARY KEY CLUSTERED ([IntersectMapID] ASC, [Object] ASC, [ObjectID] ASC),
    CONSTRAINT [FK_IntersectMapContextItem_IntersectMap] FOREIGN KEY ([IntersectMapID]) REFERENCES [dbo].[IntersectMap] ([ID])
);

