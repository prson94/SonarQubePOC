CREATE TABLE [dbo].[MapType](
	[MapClass] [smallint] NOT NULL,
	[Name] [nvarchar](250) NOT NULL,
	[Description] [nvarchar](max) NULL,
	[CreatedOn] [datetime] NULL,
	[CreatedBy] [int] NULL,
	[UpdatedOn] [datetime] NULL,
	[UpdatedBy] [int] NULL,
	[ID] [int] IDENTITY(1,1) NOT NULL,
 CONSTRAINT [PK_MapType] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
)
GO

ALTER TABLE [dbo].[MapType] ADD  CONSTRAINT [DF_MapType_MapClass]  DEFAULT ((1)) FOR [MapClass]
GO

ALTER TABLE [dbo].[MapType] ADD  CONSTRAINT [DF_MapType_CreatedOn]  DEFAULT (getutcdate()) FOR [CreatedOn]
GO

ALTER TABLE [dbo].[MapType] ADD  CONSTRAINT [DF_MapType_CreatedBy]  DEFAULT ((0)) FOR [CreatedBy]
GO

ALTER TABLE [dbo].[MapType] ADD  CONSTRAINT [DF_MapType_UpdatedOn]  DEFAULT (getutcdate()) FOR [UpdatedOn]
GO

ALTER TABLE [dbo].[MapType] ADD  CONSTRAINT [DF_MapType_UpdatedBy]  DEFAULT ((0)) FOR [UpdatedBy]
GO