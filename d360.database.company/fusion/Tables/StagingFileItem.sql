CREATE TABLE [fusion].[StagingFileItem]
(
       [StagingFileID] INT NOT NULL,
       [Tag] NVARCHAR(500)  NOT NULL,
       [Value] NVARCHAR(500) NOT NULL,
	   [ChangeType] int not null
)
go

CREATE CLUSTERED INDEX [CIX_StagingFileItem]
    ON [fusion].[StagingFileItem]([StagingFileID] ASC);



ALTER TABLE [fusion].[StagingFileItem]  WITH CHECK ADD  CONSTRAINT [FK_StagingFileItem_StagingFile] FOREIGN KEY([StagingFileID])
	REFERENCES [fusion].[StagingFile] ([ID])
	ON DELETE CASCADE
GO

ALTER TABLE [fusion].[StagingFileItem] CHECK CONSTRAINT [FK_StagingFileItem_StagingFile]
GO
