CREATE TABLE [dbo].[IntersectMapTransformation] (
    [TransformationID] INT NOT NULL,
    [IntersectMapID]   INT NOT NULL,
    CONSTRAINT [PK_IntersectMapTransformation] PRIMARY KEY CLUSTERED ([IntersectMapID] ASC, [TransformationID] ASC),
    CONSTRAINT [FK_IntersectMapTransformation_IntersectMap] FOREIGN KEY ([IntersectMapID]) REFERENCES [dbo].[IntersectMap] ([ID]),
    CONSTRAINT [FK_IntersectMapTransformation_Transformation] FOREIGN KEY ([TransformationID]) REFERENCES [dbo].[Transformation] ([ID])
);

