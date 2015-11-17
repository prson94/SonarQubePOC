CREATE TABLE [dbo].[RelatedArtifact] (
    [GroupID]    INT NOT NULL,
    [ArtifactID] INT NOT NULL,
    CONSTRAINT [PK_RelatedArtifact] PRIMARY KEY CLUSTERED ([GroupID] ASC, [ArtifactID] ASC),
    CONSTRAINT [FK_RelatedArtifact_Artifact] FOREIGN KEY ([ArtifactID]) REFERENCES [dbo].[Artifact] ([ID])
);

