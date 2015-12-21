CREATE TABLE [fusion].[StagingItemArchive] (
    [ID]                      BIGINT          IDENTITY (1, 1) NOT NULL,
    [RowID]                   INT             NOT NULL,
    [Name]                    NVARCHAR (250)  NULL,
    [Value]                   NVARCHAR (4000) NULL,
    [FusionAttributeTypeID]   INT             NULL,
    [FusionAttributeID]       INT             NULL,
    [SourceID]                NVARCHAR (250)  NULL,
    [ParentFusionAttributeID] INT             NULL,
    [ParentSourceID]          NVARCHAR (250)  NULL,
    [FieldTypeID]             INT             NULL,
    [OldValue]                NVARCHAR (4000) NULL,
    [Action]                  VARCHAR (1)     NULL,
    [Message]                 NVARCHAR (4000) NULL,
    [FieldExists]             BIT             NULL,
    [ExecutionID]             INT             CONSTRAINT [DF_FusionStagingItemArchive] DEFAULT ((0)) NOT NULL,
    [ValueAsShortChar]        VARCHAR (250)   NULL,
    [ValueAsInt]              INT             NULL,
    CONSTRAINT [PK_FusionStagingItemArchive] PRIMARY KEY CLUSTERED ([ID] ASC)
);



