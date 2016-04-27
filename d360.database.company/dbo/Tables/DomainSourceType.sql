
CREATE TABLE [dbo].[DomainSourceType](
	[ArtifactTypeID] [int] NOT NULL
)

GO

ALTER TABLE [dbo].[DomainSourceType]  WITH CHECK ADD FOREIGN KEY([ArtifactTypeID])
REFERENCES [dbo].[ArtifactType] ([ID])
GO
