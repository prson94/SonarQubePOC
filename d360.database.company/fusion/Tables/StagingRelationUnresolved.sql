CREATE TABLE [fusion].[StagingRelationUnresolved] (
    [ID]                         BIGINT         IDENTITY (1, 1) NOT NULL,    
    [StartID]                    VARCHAR (500) NOT NULL,
    [EndID]                      VARCHAR (500) NOT NULL,    
	[CreatedOn] 				 DATETIME	    NOT NULL default CURRENT_TIMESTAMP,
    CONSTRAINT [PK_FusionStagingRelationUnresolved] PRIMARY KEY NONCLUSTERED ([ID] ASC)
);