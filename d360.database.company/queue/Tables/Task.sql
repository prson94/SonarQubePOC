CREATE TABLE [queue].[Task](
	[ID] [uniqueidentifier] NOT NULL CONSTRAINT [DF_QueueTask_ID]  DEFAULT (newid()),
	[Action] [varchar](50) NOT NULL,
	[Custom] [varchar](500) NULL,
	[Object] [varchar](50) NOT NULL,
	[ObjectID] [int] NOT NULL,
	[Date] [datetime] NOT NULL CONSTRAINT [DF_QueueTask_Date]  DEFAULT (getutcdate()),
	[MachineAssigned] [varchar](250) NULL,
	[HasError] [bit] NOT NULL CONSTRAINT [DF_QueueTask_HasError]  DEFAULT ((0)),
	[ErrorMessage] [nvarchar](max) NULL,
	[NumberOfRetries] [int] NOT NULL CONSTRAINT [DF_QueueTask_NumberOfRetries]  DEFAULT ((0)),
	CONSTRAINT [PK_QueueTask] PRIMARY KEY NONCLUSTERED ( [ID] ASC )
)
GO
CREATE CLUSTERED INDEX [CIX_QueueTask] ON [queue].[Task] ( [Date] ASC )
GO