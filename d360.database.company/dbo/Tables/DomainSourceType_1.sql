CREATE TABLE [dbo].[DomainSourceType] (
    [ArtifactTypeID] INT NOT NULL,
    CONSTRAINT [FK_DomainSourceType_ArtifactType] FOREIGN KEY ([ArtifactTypeID]) REFERENCES [dbo].[ArtifactType] ([ID])
);

