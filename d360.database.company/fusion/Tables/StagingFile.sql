CREATE TABLE [fusion].[StagingFile]
(
       [ID] INT IDENTITY(1,1) NOT NULL,
       [FusionID] INT NOT NULL,
       [FusionAttributeID] INT NOT NULL,
       [File] nvarchar(500) not null,
       [UpdatedOn] datetime not null,
       CONSTRAINT [PK_StagingFile] PRIMARY KEY CLUSTERED ([ID]),
	   CONSTRAINT [UC_StagingFile] UNIQUE([FusionID],[FusionAttributeID]) 
)