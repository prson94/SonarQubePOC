CREATE TABLE [dbo].[FusionOwner] (
    [FusionID]   INT NOT NULL,
    [ArtifactID] INT NOT NULL,
    CONSTRAINT [PK_FusionOwner] PRIMARY KEY CLUSTERED ([FusionID] ASC, [ArtifactID] ASC),
    CONSTRAINT [FK_FusionOwner_Artifact] FOREIGN KEY ([ArtifactID]) REFERENCES [dbo].[Artifact] ([ID]) ON DELETE CASCADE,
    CONSTRAINT [FK_FusionOwner_Fusion] FOREIGN KEY ([FusionID]) REFERENCES [dbo].[Fusion] ([ID]) ON DELETE CASCADE
);

