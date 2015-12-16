CREATE TABLE [fusion].[StagingRelationMapping](	
	[ID] BIGINT IDENTITY(1,1) NOT NULL,
	[ExecutionID] [int] NOT NULL,
	[StartID] [nvarchar](500) NOT NULL,
	[EndID] [nvarchar](500) NOT NULL
);