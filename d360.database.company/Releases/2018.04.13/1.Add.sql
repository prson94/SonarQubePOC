ALTER DATABASE D3S_145 SET COMPATIBILITY_LEVEL = 130
GO

CREATE SCHEMA [integration]
    AUTHORIZATION [dbo];
GO

CREATE SCHEMA [lineage]
    AUTHORIZATION [dbo];
GO


DROP TABLE AssetType
GO

CREATE TABLE [dbo].[AssetType](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](250) NOT NULL,
	[Description] [nvarchar](max) NULL,
	[Class] [int] NOT NULL,
	[DisplayFormat] [nvarchar](250) NOT NULL,
	[State] [int] NOT NULL,
	[Hierarchical] [bit] NOT NULL,
	[HierarchyPredicateID] [int] NULL,
	[HierarchyIntersectTypeID] [int] NULL,
	[HierarchyMaximumDepth] [int] NOT NULL,
	[Object] [varchar](50) NOT NULL,
	[ObjectID] [int] NOT NULL,
	[CreatedOn] [datetime] NULL,
	[CreatedBy] [int] NULL,
	[UpdatedOn] [datetime] NULL,
	[UpdatedBy] [int] NULL,
	[EffectiveStartDate] [datetime2](0) GENERATED ALWAYS AS ROW START NOT NULL,
	[EffectiveEndDate] [datetime2](0) GENERATED ALWAYS AS ROW END NOT NULL,
	[Notes] [nvarchar](max) NULL,
	CONSTRAINT [PK_AssetType] PRIMARY KEY NONCLUSTERED ( [ID] ASC ),
	PERIOD FOR SYSTEM_TIME ([EffectiveStartDate], [EffectiveEndDate])
) WITH ( SYSTEM_VERSIONING = ON ( HISTORY_TABLE = [dbo].[AssetType_History] ) )
GO

ALTER TABLE [dbo].[AssetType] ADD  CONSTRAINT [DF_AssetType_Class]  DEFAULT ((1)) FOR [Class]
GO

ALTER TABLE [dbo].[AssetType] ADD  CONSTRAINT [DF_AssetType_DisplayFormat]  DEFAULT ('{ID}') FOR [DisplayFormat]
GO

ALTER TABLE [dbo].[AssetType] ADD  CONSTRAINT [DF_AssetType_State]  DEFAULT ((1)) FOR [State]
GO

ALTER TABLE [dbo].[AssetType] ADD  CONSTRAINT [DF_AssetType_Hierarchical]  DEFAULT ((0)) FOR [Hierarchical]
GO

ALTER TABLE [dbo].[AssetType] ADD  CONSTRAINT [DF_AssetType_HierarchyMaximumDepth]  DEFAULT ((0)) FOR [HierarchyMaximumDepth]
GO

/*

CREATE TABLE [dbo].[AssetTypeExportTemplate](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[AssetTypeID] [int] NOT NULL,
	[Name] [nvarchar](250) NOT NULL,
	[Description] [nvarchar](max) NULL,
	[IncludeFields] [nvarchar](1000) NULL,
	[ExportViewType] [smallint] NOT NULL,
	[IncludeParent] [bit] NOT NULL,
	[IncludeUrl] [bit] NOT NULL,
	[TemplateFile] [varbinary](max) NULL,
	[CreatedOn] [datetime] NULL,
	[CreatedBy] [int] NULL,
	[UpdatedOn] [datetime] NULL,
	[UpdatedBy] [int] NULL,
	CONSTRAINT [PK_AssetTypeExportTemplate] PRIMARY KEY NONCLUSTERED ([ID] ASC)
)
GO

ALTER TABLE [dbo].[AssetTypeExportTemplate] ADD  CONSTRAINT [DF_AssetTypeExportTemplate_IncludeParent]  DEFAULT ((0)) FOR [IncludeParent]
GO

ALTER TABLE [dbo].[AssetTypeExportTemplate] ADD  CONSTRAINT [DF_AssetTypeExportTemplate_IncludeUrl]  DEFAULT ((0)) FOR [IncludeUrl]
GO

ALTER TABLE [dbo].[AssetTypeExportTemplate]  WITH CHECK ADD  CONSTRAINT [FK_AssetTypeExportTemplate_AssetType] FOREIGN KEY([AssetTypeID]) REFERENCES [dbo].[AssetType] ([ID])
GO

ALTER TABLE [dbo].[AssetTypeExportTemplate] CHECK CONSTRAINT [FK_AssetTypeExportTemplate_AssetType]
GO

CREATE TABLE [dbo].[AssetTypeExportTemplateStyle](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[AssetTypeExportTemplateID] [int] NOT NULL,
	[Column] [int] NOT NULL,
	[Row] [int] NOT NULL,
	[Color] [int] NULL,
	[BackgroundColor] [int] NULL,
	[IsBold] [bit] NOT NULL,
	CONSTRAINT [PK_AssetTypeExportTemplateStyle] PRIMARY KEY NONCLUSTERED (	[ID] ASC )
)
GO

ALTER TABLE [dbo].[AssetTypeExportTemplateStyle] ADD  CONSTRAINT [DF_AssetTypeExportTemplateStyle_IsBold]  DEFAULT ((0)) FOR [IsBold]
GO

ALTER TABLE [dbo].[AssetTypeExportTemplateStyle]  WITH CHECK ADD  CONSTRAINT [FK_AssetTypeExportTemplateStyle_AssetTypeExportTemplate] FOREIGN KEY([AssetTypeExportTemplateID]) REFERENCES [dbo].[AssetTypeExportTemplate] ([ID])
GO

ALTER TABLE [dbo].[AssetTypeExportTemplateStyle] CHECK CONSTRAINT [FK_AssetTypeExportTemplateStyle_AssetTypeExportTemplate]
GO

CREATE TABLE [dbo].[AssetTypeLevel](
	[AssetTypeID] [int] NOT NULL,
	[Level] [int] NOT NULL,
	[Name] [nvarchar](250) NOT NULL,
	[Description] [nvarchar](4000) NULL,
	CONSTRAINT [PK_AssetTypeLevel] PRIMARY KEY NONCLUSTERED ( [AssetTypeID] ASC, [Level] ASC )
)
GO

ALTER TABLE [dbo].[AssetTypeLevel]  WITH CHECK ADD  CONSTRAINT [FK_AssetTypeLevel_AssetType] FOREIGN KEY([AssetTypeID]) REFERENCES [dbo].[AssetType] ([ID])
GO

ALTER TABLE [dbo].[AssetTypeLevel] CHECK CONSTRAINT [FK_AssetTypeLevel_AssetType]
GO

CREATE TABLE [dbo].[AssetTypeQuery](
	[ID] [int] NOT NULL,
	[Query] [nvarchar](250) NOT NULL,
	CONSTRAINT [PK_AssetTypeQuery] PRIMARY KEY NONCLUSTERED ( [ID] ASC )
)
GO

ALTER TABLE [dbo].[AssetTypeQuery]  WITH CHECK ADD  CONSTRAINT [FK_AssetTypeQuery_AssetType] FOREIGN KEY([ID]) REFERENCES [dbo].[AssetType] ([ID])
GO

ALTER TABLE [dbo].[AssetTypeQuery] CHECK CONSTRAINT [FK_AssetTypeQuery_AssetType]
GO

CREATE TABLE [dbo].[AssetTypeStyle](
	[ID] [int] NOT NULL,
	[IconBackColor] [varchar](7) NOT NULL,
	[IconForeColor] [varchar](7) NOT NULL,
	[IconText] [varchar](25) NULL,
	[Icon] [varchar](50) NULL,
	CONSTRAINT [PK_AssetTypeStyle] PRIMARY KEY NONCLUSTERED ( [ID] ASC )
)
GO

ALTER TABLE [dbo].[AssetTypeStyle]  WITH CHECK ADD  CONSTRAINT [FK_AssetTypeStyle_AssetType] FOREIGN KEY([ID]) REFERENCES [dbo].[AssetType] ([ID])
GO

ALTER TABLE [dbo].[AssetTypeStyle] CHECK CONSTRAINT [FK_AssetTypeStyle_AssetType]
GO
*/

DROP TABLE dbo.Asset
GO

CREATE TABLE [dbo].[Asset](
	[ID] [bigint] IDENTITY(1,1) NOT NULL,
	[AssetTypeID] [int] NOT NULL,
	[State] [int] NOT NULL,
	[Object] [varchar](50) NOT NULL,
	[ObjectID] [int] NOT NULL,
	[SourceID] [nvarchar](500) NULL,
	[CreatedOn] [datetime] NULL,
	[CreatedBy] [int] NULL,
	[UpdatedOn] [datetime] NULL,
	[UpdatedBy] [int] NULL,
	[EffectiveStartDate] [datetime2](0) GENERATED ALWAYS AS ROW START NOT NULL,
	[EffectiveEndDate] [datetime2](0) GENERATED ALWAYS AS ROW END NOT NULL,
	[KeyHash] [varchar](50) NULL,
	[FieldHash] [varchar](50) NULL,
	CONSTRAINT [PK_Asset] PRIMARY KEY NONCLUSTERED ( [ID] ASC ),
	PERIOD FOR SYSTEM_TIME ([EffectiveStartDate], [EffectiveEndDate])
) WITH ( SYSTEM_VERSIONING = ON ( HISTORY_TABLE = [dbo].[Asset_History] ) )
GO

ALTER TABLE [dbo].[Asset] ADD  CONSTRAINT [DF_Asset_State]  DEFAULT ((1)) FOR [State]
GO

ALTER TABLE [dbo].[Asset]  WITH CHECK ADD  CONSTRAINT [FK_Asset_AssetType] FOREIGN KEY([AssetTypeID]) REFERENCES [dbo].[AssetType] ([ID])
GO

ALTER TABLE [dbo].[Asset] CHECK CONSTRAINT [FK_Asset_AssetType]
GO

/*
Asset Dependency table here. If needed.
*/

CREATE TABLE [dbo].[AssetProfile](
	[AssetID] [bigint] NOT NULL,
	[RowCount] [int] NOT NULL,
	[UniqueCount] [int] NOT NULL,
	[Uniqueness] [real] NULL,
	[NullCount] [int] NOT NULL,
	[EmptyCount] [int] NOT NULL,
	[MinimumValue] [nvarchar](255) NULL,
	[MaximumValue] [nvarchar](255) NULL,
	[DataType] [nvarchar](255) NULL,
	[Completeness] [real] NULL,
	[FormatCount] [int] NULL,
	[OverallDataType] [nvarchar](255) NULL,
	[DominantDataType] [nvarchar](255) NULL,
	[Precision] [int] NULL,
	[Scale] [int] NULL,
	[MinimumLength] [int] NULL,
	[MaximumLenght] [int] NULL,
	[TotalSum] [float] NULL,
	[StandardDeviation] [float] NULL,
	[AlphanumericAverageLength] [real] NULL,
	[AlphanumericChecksum] [real] NULL,
	[AlphanumericCompleteness] [int] NULL,
	[AlphanumericCount] [int] NULL,
	[AverageFormatFrequency] [real] NULL,
	[AverageFrequency] [real] NULL,
	[IntegerAverage] [real] NULL,
	[StandardDeviationFormatFrequency] [real] NULL,
	[StandardDeviationFrequency] [real] NULL,
	[BlankCount] [int] NULL,
	[ByteLength] [int] NULL,
	[AverageCount] [real] NULL,
	[LeastCommonFormatCount] [int] NULL,
	[LeastCommonValueCount] [int] NULL,
	[MostCommonFormatCount] [int] NULL,
	[MostCommonValueCount] [int] NULL,
	[DateAverageLength] [real] NULL,
	[DateChecksum] [nvarchar](255) NULL,
	[DateCompletness] [int] NULL,
	[DateCount] [int] NULL,
	[DateFormatCount] [int] NULL,
	[DateLeastCommonValue] [datetime] NULL,
	[DateLeastCommonFormat] [nvarchar](255) NULL,
	[DateLeastCommonFormatCount] [nvarchar](255) NULL,
	[DateLeastCommonCount] [int] NULL,
	[DateMostCommonValue] [datetime] NULL,
	[DateMostCommonFormat] [nvarchar](255) NULL,
	[DateMostCommonFormatCount] [nvarchar](255) NULL,
	[DateMostCommonCount] [int] NULL,
	[DateMaximumValue] [datetime] NULL,
	[DateMaximumCount] [int] NULL,
	[DateMinimumValue] [datetime] NULL,
	[DateMaximumLength] [int] NULL,
	[DateMinimumCount] [int] NULL,
	[DateMinimumLength] [int] NULL,
	[DateLengthDeviation] [real] NULL,
	[DateUniqueCount] [int] NULL,
	[DecimalAverage] [real] NULL,
	[DecimalAverageLength] [real] NULL,
	[DecimalCompleteness] [int] NULL,
	[DecimalCount] [int] NULL,
	[DecimalFormats] [int] NULL,
	[DecimalLeastCommon] [real] NULL,
	[DecimalLeastCommonCount] [int] NULL,
	[DecimalLeastCommonFormat] [nvarchar](255) NULL,
	[DecimalLeastCommonFormatCount] [int] NULL,
	[DecimalLengthDeviation] [real] NULL,
	[DecimalMaximumLength] [int] NULL,
	[DecimalMaximum] [real] NULL,
	[DecimalMaximumCount] [int] NULL,
	[DecimalMinimumLength] [int] NULL,
	[DecimalMinimum] [real] NULL,
	[DecimalMinimumCount] [int] NULL,
	[DecimalMostCommon] [real] NULL,
	[DecimalMostCommonCount] [int] NULL,
	[DecimalMostCommonFormat] [nvarchar](255) NULL,
	[DecimalMostCommonFormatCount] [int] NULL,
	[DecimalPrecision] [int] NULL,
	[DecimalScale] [int] NULL,
	[DecimalTotalSum] [real] NULL,
	[DecimalUniqueCount] [int] NULL,
	[DecimalValueDeviation] [real] NULL,
	[DeviationOfLength] [real] NULL,
	[DocumentedFormat] [nvarchar](255) NULL,
	[DocumentedLength] [int] NULL,
	[DocumentedMaximumValue] [nvarchar](255) NULL,
	[DocumentedMinimumValue] [nvarchar](255) NULL,
	[DocumentedNullable] [nvarchar](255) NULL,
	[DocumentedPrecision] [int] NULL,
	[DocumentedScale] [int] NULL,
	[DocumentedDataType] [nvarchar](255) NULL,
	[EncodingType] [nvarchar](255) NULL,
	[ExternalName] [nvarchar](255) NULL,
	[FailedMeasures] [bit] NULL,
	[FailedRows] [bit] NULL,
	[FrequentValues] [bit] NULL,
	[HasNulls] [bit] NULL,
	[HighAmounts] [bit] NULL,
	[IgnoredRows] [int] NULL,
	[ImplicitDecimalPoint] [bit] NULL,
	[IntegerAverageLength] [int] NULL,
	[IntegerCompleteness] [int] NULL,
	[IntegerCount] [int] NULL,
	[IntegerFormatCount] [int] NULL,
	[IntegerLeastCommonValue] [int] NULL,
	[IntegerLeastCommonCount] [int] NULL,
	[IntegerLeastCommonFormat] [nvarchar](155) NULL,
	[IntegerLeastCommonFormatCount] [int] NULL,
	[IntegerLengthDeviation] [real] NULL,
	[IntegerMaximumLength] [int] NULL,
	[IntegerMaximumValue] [int] NULL,
	[IntegerMaximumValueCount] [int] NULL,
	[IntegerMinimumLength] [int] NULL,
	[IntegerMinimumValue] [int] NULL,
	[IntegerMinimumValueCount] [int] NULL,
	[IntegerMostCommonValue] [int] NULL,
	[IntegerMostCommonCount] [int] NULL,
	[IntegerMostCommonFormat] [nvarchar](155) NULL,
	[IntegerMostCommonFormatCount] [int] NULL,
	[IntegerPrecision] [int] NULL,
	[IntegerTotalSum] [int] NULL,
	[IntegerUniqueCount] [int] NULL,
	[IntegerValueDeviation] [real] NULL,
	[IsASequence] [bit] NULL,
	[KeyCheck] [bit] NULL,
	[Language] [varchar](255) NULL,
	[LastValidated] [datetime] NULL,
	[LastValidatedBy] [varchar](255) NULL,
	[LeastCommonFormat] [varchar](255) NULL,
	[LeastCommonValue] [varchar](255) NULL,
	[LengthAtStart] [varchar](255) NULL,
	[LongValues] [bit] NULL,
	[LowValues] [bit] NULL,
	[MaximumExpectedFormatFrequency] [int] NULL,
	[MaximumExpectedFrequency] [int] NULL,
	[MaximumExpectedLength] [int] NULL,
	[MaximumExpectedNumber] [real] NULL,
	[MaximumCount] [int] NULL,
	[MinimumExpectedFormatFrequency] [int] NULL,
	[MinimumExpectedFrequency] [int] NULL,
	[MinimumExpectedLength] [int] NULL,
	[MinimumExpectedNumber] [real] NULL,
	[MinimumCount] [int] NULL,
	[MissingValues] [bit] NULL,
	[ModifiedDate] [datetime] NULL,
	[ModifiedBy] [varchar](255) NULL,
	[ModifiedReason] [varchar](255) NULL,
	[MoneyAverageValue] [real] NULL,
	[MoneyAverageLength] [int] NULL,
	[MoneyCompleteness] [int] NULL,
	[MoneyCountValue] [int] NULL,
	[MoneyFormatCount] [int] NULL,
	[MoneyLeastCommonValue] [real] NULL,
	[MoneyLeastCommonCount] [int] NULL,
	[MoneyLeastCommonFormatCount] [int] NULL,
	[MoneyLengthDeviation] [real] NULL,
	[MoneyMaximumLength] [int] NULL,
	[MoneyMaximumValue] [real] NULL,
	[MoneyMaximumCount] [int] NULL,
	[MoneyMinimumLength] [int] NULL,
	[MoneyMinimumValue] [real] NULL,
	[MoneyMinimumCount] [int] NULL,
	[MoneyMostCommon] [real] NULL,
	[MoneyMostCommonCount] [int] NULL,
	[MoneyMostCommonFormat] [varchar](255) NULL,
	[MoneyMostCommonFormatCount] [int] NULL,
	[MoneyPrecision] [int] NULL,
	[MoneyScale] [int] NULL,
	[MoneyTotalSum] [real] NULL,
	[MoneyUniqueCount] [int] NULL,
	[MoneyValueDeviation] [real] NULL,
	[MostCommonFormat] [varchar](255) NULL,
	[MostCommonValue] [varchar](255) NULL,
	[NativeType] [varchar](255) NULL,
	[NegativeCount] [int] NULL,
	[NegativeValues] [bit] NULL,
	[NoteCount] [int] NULL,
	[NullType] [varchar](255) NULL,
	[PassedMeasure] [varchar](255) NULL,
	[PassedRows] [int] NULL,
	[Position] [int] NULL,
	[RareFormats] [bit] NULL,
	[RareValues] [bit] NULL,
	[ReferenceID] [varchar](255) NULL,
	[RelationshipCount] [int] NULL,
	[RuleCount] [int] NULL,
	[Schema] [varchar](255) NULL,
	[SchemaExternalName] [varchar](255) NULL,
	[ShortValues] [bit] NULL,
	[SignType] [varchar](255) NULL,
	[StandardDeviationOfFormatFrequency] [real] NULL,
	[StandardDeviationOfFrequency] [real] NULL,
	[StandardDeviationofValues] [real] NULL,
	[TableConnection] [varchar](255) NULL,
	[TableExternalName] [varchar](255) NULL,
	[TableID] [varchar](255) NULL,
	[Version] [real] NULL,
	[ZeroCount] [int] NULL,
	[CreatedOn] [datetime] NULL,
	[CreatedBy] [int] NULL,
	[UpdatedOn] [datetime] NULL,
	[UpdatedBy] [int] NULL,
	[EffectiveStartDate] [datetime2](0) GENERATED ALWAYS AS ROW START NOT NULL,
	[EffectiveEndDate] [datetime2](0) GENERATED ALWAYS AS ROW END NOT NULL,
	CONSTRAINT [PK_AssetProfile] PRIMARY KEY NONCLUSTERED (	[AssetID] ASC ),
	PERIOD FOR SYSTEM_TIME ([EffectiveStartDate], [EffectiveEndDate])
) WITH ( SYSTEM_VERSIONING = ON ( HISTORY_TABLE = [dbo].[AssetProfile_History] ) )
GO



CREATE TABLE [api].[Service](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[UriPrefix] [varchar](100) NOT NULL,
	[Name] [nvarchar](500) NOT NULL,
	[Description] [nvarchar](max) NULL,
	[MaximumCacheAge] [int] NOT NULL,
	CONSTRAINT [PK_Api_Service] PRIMARY KEY NONCLUSTERED ( [ID] ASC )
)
GO

ALTER TABLE [api].[Service] ADD  CONSTRAINT [DF_Api_Service_MaximumCacheAge]  DEFAULT ((3600)) FOR [MaximumCacheAge]
GO

CREATE TABLE [api].[Endpoint](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[ServiceID] [int] NOT NULL,
	[UriPrefix] [varchar](100) NOT NULL,
	[Name] [nvarchar](500) NOT NULL,
	[Description] [nvarchar](max) NULL,
	CONSTRAINT [PK_Api_Endpoint] PRIMARY KEY NONCLUSTERED (	[ID] ASC )
)
GO

ALTER TABLE [api].[Endpoint]  WITH CHECK ADD  CONSTRAINT [FK_Endpoint_Service] FOREIGN KEY([ServiceID]) REFERENCES [api].[Service] ([ID]) ON DELETE CASCADE
GO

ALTER TABLE [api].[Endpoint] CHECK CONSTRAINT [FK_Endpoint_Service]
GO

CREATE TABLE [api].[EndpointVersion](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[EndpointID] [int] NOT NULL,
	[UriPrefix] [varchar](100) NOT NULL,
	[MajorVersion] [int] NOT NULL,
	[MinorVersion] [int] NOT NULL,
	CONSTRAINT [PK_Api_EndpointVersion] PRIMARY KEY NONCLUSTERED ( [ID] ASC )
)
GO

ALTER TABLE [api].[EndpointVersion]  WITH CHECK ADD  CONSTRAINT [FK_EndpointVersion_Endpoint] FOREIGN KEY([EndpointID]) REFERENCES [api].[Endpoint] ([ID]) ON DELETE CASCADE
GO

ALTER TABLE [api].[EndpointVersion] CHECK CONSTRAINT [FK_EndpointVersion_Endpoint]
GO

CREATE TABLE [api].[Entity](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[EndpointVersionID] [int] NOT NULL,
	[AssetTypeID] [int] NOT NULL,
	CONSTRAINT [PK_Api_Entity] PRIMARY KEY NONCLUSTERED ( [ID] ASC )
)
GO

ALTER TABLE [api].[Entity]  WITH CHECK ADD  CONSTRAINT [FK_Entity_AssetType] FOREIGN KEY([AssetTypeID]) REFERENCES [dbo].[AssetType] ([ID]) ON DELETE CASCADE
GO

ALTER TABLE [api].[Entity] CHECK CONSTRAINT [FK_Entity_AssetType]
GO

ALTER TABLE [api].[Entity]  WITH CHECK ADD  CONSTRAINT [FK_Entity_EndpointVersion] FOREIGN KEY([EndpointVersionID]) REFERENCES [api].[EndpointVersion] ([ID]) ON DELETE CASCADE
GO

ALTER TABLE [api].[Entity] CHECK CONSTRAINT [FK_Entity_EndpointVersion]
GO

CREATE TABLE [api].[EntityFieldType](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[EntityID] [int] NOT NULL,
	[FieldTypeID] [int] NOT NULL,
	[JsonFieldNameOverride] [nvarchar](250) NULL,
	[XmlFieldNameOverride] [nvarchar](250) NULL,
	[AllowSelect] [bit] NOT NULL,
	[AllowSort] [bit] NOT NULL,
	[AllowFilter] [bit] NOT NULL
)
GO

ALTER TABLE [api].[EntityFieldType] ADD  CONSTRAINT [PK_Api_EntityFieldType] PRIMARY KEY NONCLUSTERED ( [EntityID] ASC, [FieldTypeID] ASC )
GO



ALTER TABLE [api].[EntityFieldType]  WITH CHECK ADD  CONSTRAINT [FK_EntityFieldType_Entity] FOREIGN KEY([EntityID]) REFERENCES [api].[Entity] ([ID]) ON DELETE CASCADE
GO

ALTER TABLE [api].[EntityFieldType] CHECK CONSTRAINT [FK_EntityFieldType_Entity]
GO

ALTER TABLE [api].[EntityFieldType]  WITH CHECK ADD  CONSTRAINT [FK_EntityFieldType_FieldType] FOREIGN KEY([FieldTypeID]) REFERENCES [dbo].[FieldType] ([ID]) ON DELETE CASCADE
GO

ALTER TABLE [api].[EntityFieldType] CHECK CONSTRAINT [FK_EntityFieldType_FieldType]
GO

CREATE TABLE [api].[EntityUri](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[EntityID] [int] NOT NULL,
	[UriType] [int] NOT NULL,
	[Format] [varchar](500) NOT NULL,
	CONSTRAINT [PK_Api_EntityUri] PRIMARY KEY NONCLUSTERED ( [ID] ASC )
)
GO

ALTER TABLE [api].[EntityUri]  WITH CHECK ADD  CONSTRAINT [FK_EntityUri_Entity] FOREIGN KEY([EntityID]) REFERENCES [api].[Entity] ([ID]) ON DELETE CASCADE
GO

ALTER TABLE [api].[EntityUri] CHECK CONSTRAINT [FK_EntityUri_Entity]
GO

CREATE TABLE [dbo].[ContractAcceptance] (
    [ID]             INT      IDENTITY (1, 1) NOT NULL,
    [ResourceID]     INT      NOT NULL,
    [Accepted]       BIT      NOT NULL,
    [AcceptedOn]     DATETIME NOT NULL,
    [ContractID]     INT      NOT NULL,
    [OrganizationID] INT      NULL,
    PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_ContractAcceptance_Contract] FOREIGN KEY ([ContractID]) REFERENCES [dbo].[Contract] ([ID]),
    CONSTRAINT [FK_ContractAcceptance_Organization] FOREIGN KEY ([OrganizationID]) REFERENCES [dbo].[Organization] ([ID])
);
GO

CREATE TABLE [dbo].[Links] (
    [IntersectID]     INT    NULL,
    [IntersectTypeID] INT    NULL,
    [PredicateID]     INT    NULL,
    [PredicateType]   INT    NULL,
    [SubjectID]       BIGINT NULL,
    [ObjectID]        BIGINT NULL,
    INDEX [ix_graphid] UNIQUE NONCLUSTERED ($edge_id)
) AS EDGE;
GO

CREATE NONCLUSTERED INDEX [ix_fromid]
    ON [dbo].[Links]($from_id ASC, $to_id ASC);
GO

CREATE NONCLUSTERED INDEX [ix_toid]
    ON [dbo].[Links]($to_id ASC, $from_id ASC);
GO

CREATE TABLE [dbo].[Nodes] (
    [AssetID]     BIGINT NULL,
    [AssetTypeID] INT    NULL,
    INDEX [ix_graphid] UNIQUE NONCLUSTERED ($node_id)
) AS NODE;
GO

CREATE TABLE [dbo].[OrganizationType] (
    [ID]            INT            IDENTITY (1, 1) NOT NULL,
    [Name]          NVARCHAR (250) NOT NULL,
    [Description]   NVARCHAR (MAX) NULL,
    [DisplayFormat] NVARCHAR (250) CONSTRAINT [DF_OrganizationType_DisplayFormat] DEFAULT ('{Name}') NOT NULL,
    [CreatedBy]     INT            CONSTRAINT [DF_OrganizationType_CreatedBy] DEFAULT ((0)) NULL,
    [CreatedOn]     DATETIME       CONSTRAINT [DF_OrganizationType_CreatedOn] DEFAULT (getutcdate()) NULL,
    [UpdatedBy]     INT            CONSTRAINT [DF_OrganizationType_UpdatedBy] DEFAULT ((0)) NULL,
    [UpdatedOn]     DATETIME       CONSTRAINT [DF_OrganizationType_UpdatedOn] DEFAULT (getutcdate()) NULL,
    [State]         INT            CONSTRAINT [DF_OrganizationType_State] DEFAULT ((1)) NOT NULL,
    CONSTRAINT [PK_OrganizationType] PRIMARY KEY CLUSTERED ([ID] ASC)
);
GO

CREATE TRIGGER [dbo].[OrganizationType_AfterDelete]
   ON  [dbo].[OrganizationType] 
   AFTER DELETE
AS 
	SET NOCOUNT ON
	update	AssetType
	set		[State] = 3
	where	Object = 'OrganizationType' and ObjectID in (select ID from deleted)

	delete AssetType where Object = 'OrganizationType' and ObjectID in (select ID from deleted)
GO

CREATE TRIGGER [dbo].[OrganizationType_AfterInsert]
   ON  [dbo].[OrganizationType] 
   AFTER INSERT
AS 
	SET NOCOUNT ON
	insert into AssetType (Name, Description, Class, DisplayFormat, [State], [Hierarchical], [HierarchyMaximumDepth], [Object], [ObjectID], [CreatedOn], [CreatedBy], [UpdatedOn], [UpdatedBy])
		select Name, Description, 10, DisplayFormat, 1, 0, 1, 'OrganizationType', ID, coalesce(CreatedOn, getutcdate()), CreatedBy, coalesce(UpdatedOn, getutcdate()), UpdatedBy from inserted
GO

CREATE TRIGGER [dbo].[OrganizationType_AfterUpdate]
   ON  [dbo].[OrganizationType] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON
	update	T
	set		T.Name = S.Name,
			T.Description = S.Description,
			T.DisplayFormat = S.DisplayFormat,
			T.UpdatedBy = S.UpdatedBy,
			T.UpdatedOn = S.UpdatedOn
	from	AssetType T
			inner join inserted S on T.Object = 'OrganizationType' and T.ObjectID = S.ID
GO

CREATE TABLE [integration].[Setting] (
    [ID]                INT             IDENTITY (1, 1) NOT NULL,
    [SourceUri]         NVARCHAR (2500) NOT NULL,
    [SourceUser]        VARCHAR (250)   NOT NULL,
    [SourcePassword]    VARCHAR (250)   NOT NULL,
    [TargetResourceID]  INT             NOT NULL,
    [IntegrationSystem] INT             CONSTRAINT [DF_IntegrationSetting_IntegrationSystem] DEFAULT ((1)) NOT NULL,
    [LastRefreshOn]     DATETIME        NULL,
    [RefreshInterval]   INT             CONSTRAINT [DF_IntegrationSetting_RefreshInterval] DEFAULT ((24)) NOT NULL,
    CONSTRAINT [PK_IntegrationSetting] PRIMARY KEY NONCLUSTERED ([ID] ASC)
);
GO

CREATE FUNCTION integration.GetObjectByAssetTypeId
(
    @id int
)
RETURNS varchar(50)
AS
BEGIN
    DECLARE @o varchar(50)

    SELECT @o = [Object] from AssetType where ID = @id

    RETURN @o
END
GO

CREATE FUNCTION integration.GetObjectIDByAssetTypeId
(
    @id int
)
RETURNS int
AS
BEGIN
    DECLARE @o int

    SELECT @o = [ObjectID] from AssetType where ID = @id

    RETURN @o
END
GO

CREATE TABLE [integration].[SynchedAssetType] (
    [ID]                   INT           IDENTITY (1, 1) NOT NULL,
    [IntegrationSettingID] INT           NOT NULL,
    [SourceAssetTypeName]  VARCHAR (500) NOT NULL,
    [AssetTypeID]          INT           NOT NULL,
    [Object]               AS            ([integration].[GetObjectByAssetTypeId]([AssetTypeID])),
    [ObjectID]             AS            ([integration].[GetObjectIDByAssetTypeId]([AssetTypeID])),
    [ToGovern]             BIT           CONSTRAINT [DF_IntegrationAssetType_ToGovern] DEFAULT ((1)) NOT NULL,
    [Active]               BIT           CONSTRAINT [DF_IntegrationAssetType_Active] DEFAULT ((0)) NOT NULL,
    [OptionalIDName]       VARCHAR (50)  NULL,
    [OptionalID]           INT           NULL,
    [LastSynchOn]          DATETIME      NULL,
    CONSTRAINT [PK_IntegrationSynchedAssetType] PRIMARY KEY NONCLUSTERED ([ID] ASC),
    CONSTRAINT [FK_IntegrationSynchedAssetType_IntegrationSetting] FOREIGN KEY ([IntegrationSettingID]) REFERENCES [integration].[Setting] ([ID])
);
GO

CREATE TABLE [integration].[SynchedAssetTypeFieldItem] (
    [ID]                       INT            IDENTITY (1, 1) NOT NULL,
    [SynchedAssetTypeID]       INT            NOT NULL,
    [IncludeInPropertyRequest] BIT            CONSTRAINT [DF_IntegrationSynchedAssetTypeFieldItem_IncludeInPropertyRequest] DEFAULT ((1)) NOT NULL,
    [SourceField]              VARCHAR (250)  NOT NULL,
    [TargetField]              VARCHAR (250)  NOT NULL,
    [ParentContextPosition]    INT            NULL,
    [IsArray]                  BIT            CONSTRAINT [DF_IntegrationSynchedAssetTypeFieldItem_IsArray] DEFAULT ((0)) NOT NULL,
    [DefaultValue]             NVARCHAR (250) NULL,
	ArrayValueDelimiter			varchar(10) null,
	ArrayValueFieldName			varchar(50) null,
	Active bit constraint DF_IntegrationSynchedAssetTypeFieldItem_Active default(1) not null,
    CONSTRAINT [PK_IntegrationSynchedAssetTypeFieldItem] PRIMARY KEY NONCLUSTERED ([ID] ASC),
    CONSTRAINT [FK_IntegrationSynchedAssetTypeFieldItem_IntegrationSynchedAssetType] FOREIGN KEY ([SynchedAssetTypeID]) REFERENCES [integration].[SynchedAssetType] ([ID])
);
GO

CREATE TABLE [integration].[SynchedAssetTypeRelationItem] (
    [ID]                       INT           IDENTITY (1, 1) NOT NULL,
    [SynchedAssetTypeID]       INT           NOT NULL,
    [IncludeInPropertyRequest] BIT           CONSTRAINT [DF_IntegrationSynchedAssetTypeRelationItem_IncludeInPropertyRequest] DEFAULT ((1)) NOT NULL,
    [SourceField]              VARCHAR (250) NOT NULL,
    [PredicateType]            INT           NOT NULL,
    [IsSubject]                BIT           CONSTRAINT [DF_IntegrationSynchedAssetTypeRelationItem_IsSubject] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_IntegrationSynchedAssetTypeRelationItem] PRIMARY KEY NONCLUSTERED ([ID] ASC),
    CONSTRAINT [FK_IntegrationSynchedAssetTypeRelationItem_IntegrationSynchedAssetType] FOREIGN KEY ([SynchedAssetTypeID]) REFERENCES [integration].[SynchedAssetType] ([ID])
);
GO

CREATE TABLE [integration].[SynchedAssetTypeRoleItem] (
    [ID]                       INT            IDENTITY (1, 1) NOT NULL,
    [SynchedAssetTypeID]       INT            NOT NULL,
    [IncludeInPropertyRequest] BIT            CONSTRAINT [DF_IntegrationSynchedAssetTypeRoleItem_IncludeInPropertyRequest] DEFAULT ((1)) NOT NULL,
    [SourceIdField]            VARCHAR (250)  NOT NULL,
    [SourceNameField]          VARCHAR (250)  NOT NULL,
    [RoleName]                 NVARCHAR (250) NOT NULL,
    CONSTRAINT [PK_IntegrationSynchedAssetTypeRoleItem] PRIMARY KEY NONCLUSTERED ([ID] ASC),
    CONSTRAINT [FK_IntegrationSynchedAssetTypeRoleItem_IntegrationSynchedAssetType] FOREIGN KEY ([SynchedAssetTypeID]) REFERENCES [integration].[SynchedAssetType] ([ID])
);
GO

CREATE TABLE [integration].[SynchedAssetTypeRelationItemTarget](
	ID int IDENTITY(1,1) NOT NULL,
	SynchedAssetTypeRelationItemID int NOT NULL,
	SourceAssetType varchar(250) NOT NULL,
	IntersectTypeID [int] NOT NULL,
	CONSTRAINT [PK_IntegrationSynchedAssetTypeRelationItemTarget] PRIMARY KEY NONCLUSTERED ( [ID] ASC )
)
GO

ALTER TABLE [integration].[SynchedAssetTypeRelationItemTarget]  WITH CHECK ADD  CONSTRAINT [FK_IntegrationSynchedAssetTypeRelationItem_IntegrationSynchedAssetTypeRelationItem] FOREIGN KEY(SynchedAssetTypeRelationItemID) REFERENCES [integration].[SynchedAssetTypeRelationItem] ([ID])
GO
ALTER TABLE [integration].[SynchedAssetTypeRelationItemTarget] CHECK CONSTRAINT [FK_IntegrationSynchedAssetTypeRelationItem_IntegrationSynchedAssetTypeRelationItem]
GO

ALTER TABLE [integration].[SynchedAssetTypeRelationItemTarget]  WITH CHECK ADD  CONSTRAINT [FK_IntegrationSynchedAssetTypeRelationItem_IntersectType] FOREIGN KEY(IntersectTypeID) REFERENCES [dbo].[IntersectType] ([ID])
GO
ALTER TABLE [integration].[SynchedAssetTypeRelationItemTarget] CHECK CONSTRAINT [FK_IntegrationSynchedAssetTypeRelationItem_IntersectType]
GO

CREATE TABLE [integration].[UnresolvedRelationItem](
	ID uniqueidentifier constraint DF_IntegrationUnresolvedRelationItem_ID default(newid()) NOT NULL,
	SubjectSourceID nvarchar(250) NOT NULL,
	ObjectSourceID nvarchar(250) NOT NULL,
	IntersectTypeID int NOT NULL,
	AttemptCount int NOT NULL,
	MostRecentAttemptOn datetime NOT NULL,
	CONSTRAINT [PK_IntegrationUnresolvedRelationItem] PRIMARY KEY NONCLUSTERED ( [ID] ASC )
)
GO

CREATE INDEX CIX_IntegrationUnresolvedRelationItem ON integration.UnresolvedRelationItem ( IntersectTypeID ASC, SubjectSourceID ASC, ObjectSourceID ASC )
GO

CREATE CLUSTERED INDEX [CIX_IntegrationSynchedAssetTypeRelationItem] ON [integration].[SynchedAssetTypeRelationItem] ( [SynchedAssetTypeID] ASC, [SourceField] ASC )
GO

CREATE CLUSTERED INDEX [CIX_IntegrationSynchedAssetTypeRelationItemTarget] ON [integration].[SynchedAssetTypeRelationItemTarget] ( SynchedAssetTypeRelationItemID ASC, [IntersectTypeID] ASC )
GO

CREATE CLUSTERED INDEX [CIX_IntegrationSynchedAssetTypeRoleItem] ON [integration].[SynchedAssetTypeRoleItem] ( [SynchedAssetTypeID] ASC, [SourceIdField] ASC )
GO


EXEC sp_rename 'dbo.FieldType', 'FieldTypeOld'; 
GO
EXEC sp_rename 'PK_FieldType', 'PK_FieldTypeOld'; 
GO
ALTER TABLE [dbo].[FieldTypeOld] DROP CONSTRAINT [CK_FieldType_IsListable]
ALTER TABLE [dbo].[FieldTypeOld] DROP CONSTRAINT [CK_FieldType_IsPrimaryFilter]
ALTER TABLE [dbo].[FieldTypeOld] DROP CONSTRAINT [CK_FieldType_IsRequired]
ALTER TABLE [dbo].[FieldTypeOld] DROP CONSTRAINT [CK_FieldType_Object]
ALTER TABLE [dbo].[FieldTypeOld] DROP CONSTRAINT [CK_FieldType_ObjectID]
ALTER TABLE [dbo].[FieldTypeOld] DROP CONSTRAINT [CK_FieldType_SortOrder]
ALTER TABLE [dbo].[FieldTypeOld] DROP CONSTRAINT DF_FieldType_AllowAllValue
ALTER TABLE [dbo].[FieldTypeOld] DROP CONSTRAINT DF_FieldType_ColumnOrder
ALTER TABLE [dbo].[FieldTypeOld] DROP CONSTRAINT DF_FieldType_IsDisplayable
ALTER TABLE [dbo].[FieldTypeOld] DROP CONSTRAINT DF_FieldType_IsEditable
ALTER TABLE [dbo].[FieldTypeOld] DROP CONSTRAINT DF_FieldType_IsPartOfKey
GO

CREATE TABLE [dbo].[FieldType](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](250) NOT NULL,
	[FriendlyName] [nvarchar](250) NOT NULL,
	[Description] [nvarchar](4000) NULL,
	[DisplayDescription] [nvarchar](4000) NULL,
	[FormDescription] [nvarchar](4000) NULL,
	[Type] [varchar](25) NOT NULL,
	[LookupObjectType] [varchar](25) NULL,
	[LookupObjectID] [int] NULL,
	[LookupDisplayFormat] [nvarchar](250) NULL,
	[MinimumLength] [int] NULL,
	[MaximumLength] [int] NULL,
	[Length] [int] NULL,
	[Pattern] [varchar](1000) NULL,
	[Object] [varchar](50) NOT NULL,
	[ObjectID] [int] NOT NULL,
	[SortOrder] [int] NOT NULL,
	[IsRequired] [bit] NOT NULL,
	[IsListable] [bit] NOT NULL,
	[ValidationDescription] [nvarchar](500) NULL,
	[Category] [nvarchar](250) NULL,
	[IsDisplayable] [bit] NOT NULL,
	[IsEditable] [bit] NOT NULL,
	[DefaultValue] [nvarchar](max) NULL,
	[DefaultFormattedValue] [nvarchar](max) NULL,
	[AllowAllValue] [bit] NOT NULL,
	[AllowAllLabel] [nvarchar](250) NULL,
	[IsPrimaryFilter] [bit] NOT NULL,
	[LookupEditFormat] [nvarchar](250) NULL,
	[IsPartOfKey] [bit] NOT NULL,
	[ColumnOrder] [int] NOT NULL,
	[ColumnWidth] [int] NULL,
	[LookupObjectFieldTypeID] [int] NULL,
	[AllowMultipleValues] [bit] NOT NULL,
	[EffectiveStartDate] [datetime2](0) GENERATED ALWAYS AS ROW START NOT NULL,
	[EffectiveEndDate] [datetime2](0) GENERATED ALWAYS AS ROW END NOT NULL,
	[AssetTypeID] [int] NULL,
	[ParentFieldTypeID] [int] NOT NULL,
	CONSTRAINT [PK_FieldType] PRIMARY KEY CLUSTERED ( [ID] ASC ),
	PERIOD FOR SYSTEM_TIME ([EffectiveStartDate], [EffectiveEndDate])
) WITH ( SYSTEM_VERSIONING = ON ( HISTORY_TABLE = [dbo].[FieldType_History] ) )
GO

ALTER TABLE [dbo].[FieldType] ADD  CONSTRAINT [CK_FieldType_Object]  DEFAULT ('') FOR [Object]
GO

ALTER TABLE [dbo].[FieldType] ADD  CONSTRAINT [CK_FieldType_ObjectID]  DEFAULT ((0)) FOR [ObjectID]
GO

ALTER TABLE [dbo].[FieldType] ADD  CONSTRAINT [CK_FieldType_SortOrder]  DEFAULT ((0)) FOR [SortOrder]
GO

ALTER TABLE [dbo].[FieldType] ADD  CONSTRAINT [CK_FieldType_IsRequired]  DEFAULT ((0)) FOR [IsRequired]
GO

ALTER TABLE [dbo].[FieldType] ADD  CONSTRAINT [CK_FieldType_IsListable]  DEFAULT ((0)) FOR [IsListable]
GO

ALTER TABLE [dbo].[FieldType] ADD  CONSTRAINT [DF_FieldType_IsDisplayable]  DEFAULT ((1)) FOR [IsDisplayable]
GO

ALTER TABLE [dbo].[FieldType] ADD  CONSTRAINT [DF_FieldType_IsEditable]  DEFAULT ((1)) FOR [IsEditable]
GO

ALTER TABLE [dbo].[FieldType] ADD  CONSTRAINT [DF_FieldType_AllowAllValue]  DEFAULT ((0)) FOR [AllowAllValue]
GO

ALTER TABLE [dbo].[FieldType] ADD  CONSTRAINT [CK_FieldType_IsPrimaryFilter]  DEFAULT ((0)) FOR [IsPrimaryFilter]
GO

ALTER TABLE [dbo].[FieldType] ADD  CONSTRAINT [DF_FieldType_IsPartOfKey]  DEFAULT ((0)) FOR [IsPartOfKey]
GO

ALTER TABLE [dbo].[FieldType] ADD  CONSTRAINT [DF_FieldType_ColumnOrder]  DEFAULT ((1)) FOR [ColumnOrder]
GO

ALTER TABLE [dbo].[FieldType] ADD  CONSTRAINT [DF_FieldType_AllowMultipleValues]  DEFAULT ((0)) FOR [AllowMultipleValues]
GO

ALTER TABLE [dbo].[FieldType] ADD  DEFAULT ((0)) FOR [ParentFieldTypeID]
GO

SET IDENTITY_INSERT FieldType ON
INSERT INTO [FieldType] (ID,[Name],[FriendlyName],[Description],[DisplayDescription],[FormDescription],[Type],[LookupObjectType],[LookupObjectID],[LookupDisplayFormat],[MinimumLength],[MaximumLength],[Length],[Pattern],[Object],[ObjectID],[SortOrder],[IsRequired],[IsListable],[ValidationDescription],[Category],[IsDisplayable],[IsEditable],[DefaultValue],DefaultFormattedValue,[AllowAllValue],[AllowAllLabel],[IsPrimaryFilter],[LookupEditFormat],[IsPartOfKey],[ColumnOrder],[ColumnWidth],[LookupObjectFieldTypeID],[AllowMultipleValues])
	SELECT	[ID]
			,[Name]
			,[FriendlyName]
			,[Description]
			,[DisplayDescription]
			,[FormDescription]
			,[Type]
			,[LookupObjectType]
			,[LookupObjectID]
			,[LookupDisplayFormat]
			,[MinimumLength]
			,[MaximumLength]
			,[Length]
			,[Pattern]
			,[Object]
			,[ObjectID]
			,[SortOrder]
			,[IsRequired]
			,[IsListable]
			,[ValidationDescription]
			,[Category]
			,[IsDisplayable]
			,[IsEditable]
			,[DefaultValue]
			,[DefaultFormattedValue]
			,[AllowAllValue]
			,[AllowAllLabel]
			,[IsPrimaryFilter]
			,[LookupEditFormat]
			,[IsPartOfKey]
			,[ColumnOrder]
			,[ColumnWidth]
			,[LookupObjectFieldTypeID],
			0
	  FROM	[FieldTypeOld]
SET IDENTITY_INSERT FieldType OFF
GO



EXEC sp_rename 'dbo.Field', 'FieldOld'; 
GO
EXEC sp_rename 'PK_Field', 'PK_FieldOld'; 
GO
ALTER TABLE [dbo].[FieldOld] DROP CONSTRAINT [FK_Field_FieldType]
GO


CREATE TABLE [dbo].[Field](
	[AssetID] [bigint] NULL,
	[ObjectType] [varchar](50) NOT NULL,
	[ObjectID] [int] NOT NULL,
	[FieldTypeID] [int] NOT NULL,
	[Value] [nvarchar](max) NULL,
	[FormattedValue] [nvarchar](max) NULL,
	[EffectiveStartDate] [datetime2](0) GENERATED ALWAYS AS ROW START NOT NULL,
	[EffectiveEndDate] [datetime2](0) GENERATED ALWAYS AS ROW END NOT NULL,
	[UpdatedBy] [int] NOT NULL,
	CONSTRAINT [PK_Field] PRIMARY KEY NONCLUSTERED ( [ObjectType] ASC, [ObjectID] ASC, [FieldTypeID] ASC ),
	PERIOD FOR SYSTEM_TIME ([EffectiveStartDate], [EffectiveEndDate])
) WITH ( SYSTEM_VERSIONING = ON ( HISTORY_TABLE = [dbo].[Field_History] ) )
GO

ALTER TABLE [dbo].[Field] ADD  CONSTRAINT [DF_Field_UpdatedBy]  DEFAULT ((0)) FOR [UpdatedBy]
GO

ALTER TABLE [dbo].[Field]  WITH CHECK ADD  CONSTRAINT [FK_Field_FieldType] FOREIGN KEY([FieldTypeID]) REFERENCES [dbo].[FieldType] ([ID]) ON DELETE CASCADE
GO

ALTER TABLE [dbo].[Field] CHECK CONSTRAINT [FK_Field_FieldType]
GO

INSERT INTO [dbo].[Field] ([ObjectType], [ObjectID], [FieldTypeID], [Value], [FormattedValue], [UpdatedBy])
	SELECT	[ObjectType], [ObjectID], [FieldTypeID], [Value], [FormattedValue], 0
	FROM	FieldOld
GO

CREATE TABLE [dbo].[FieldValue] (
    [ID]          INT            IDENTITY (1, 1) NOT NULL,
    [ObjectType]  VARCHAR (50)   NOT NULL,
    [ObjectID]    INT            NOT NULL,
    [FieldTypeID] INT            NOT NULL,
    [Value]       NVARCHAR (250) NULL,
    CONSTRAINT [PK_FieldValue] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_FieldValue_FieldType] FOREIGN KEY ([FieldTypeID]) REFERENCES [dbo].[FieldType] ([ID]) ON DELETE CASCADE
);
GO

CREATE NONCLUSTERED INDEX [IX_FieldValue_ObjectTypeObjectIDFieldTypeID]
    ON [dbo].[FieldValue]([ObjectType] ASC, [ObjectID] ASC, [FieldTypeID] ASC);
GO

DROP TABLE IntersectGroup
GO


CREATE TABLE [dbo].[IntersectGroup](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[BusinessTransformation] [nvarchar](max) NULL,
	[TechnicalTransformation] [nvarchar](max) NULL,
	[State] [int] NOT NULL,
	[Owner] [varchar](100) NULL,
	[CreatedBy] [int] NOT NULL,
	[CreatedOn] [datetime] NOT NULL,
	[UpdatedBy] [int] NOT NULL,
	[UpdatedOn] [datetime] NOT NULL,
	[EffectiveStartDate] [datetime2](0) GENERATED ALWAYS AS ROW START NOT NULL,
	[EffectiveEndDate] [datetime2](0) GENERATED ALWAYS AS ROW END NOT NULL,
 CONSTRAINT [PK_IntersectGroup] PRIMARY KEY NONCLUSTERED 
(
	[ID] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF),
	PERIOD FOR SYSTEM_TIME ([EffectiveStartDate], [EffectiveEndDate])
)
WITH
(
SYSTEM_VERSIONING = ON ( HISTORY_TABLE = [dbo].[IntersectGroup_History] )
)
GO

ALTER TABLE [dbo].[IntersectGroup] ADD  CONSTRAINT [DF_IntersectGroup_State]  DEFAULT ((1)) FOR [State]
GO

ALTER TABLE [dbo].[IntersectGroup] ADD  CONSTRAINT [DF_IntersectGroup_CreatedBy]  DEFAULT ((0)) FOR [CreatedBy]
GO

ALTER TABLE [dbo].[IntersectGroup] ADD  CONSTRAINT [DF_IntersectGroup_CreatedOn]  DEFAULT (getutcdate()) FOR [CreatedOn]
GO

ALTER TABLE [dbo].[IntersectGroup] ADD  CONSTRAINT [DF_IntersectGroup_UpdatedBy]  DEFAULT ((0)) FOR [UpdatedBy]
GO

ALTER TABLE [dbo].[IntersectGroup] ADD  CONSTRAINT [DF_IntersectGroup_UpdatedOn]  DEFAULT (getutcdate()) FOR [UpdatedOn]
GO

CREATE TABLE [dbo].[IntersectGroupItem](
	[IntersectGroupID] [int] NOT NULL,
	[IntersectID] [int] NOT NULL,
	[State] [int] NOT NULL,
	[CreatedBy] [int] NOT NULL,
	[CreatedOn] [datetime] NOT NULL,
	[UpdatedBy] [int] NOT NULL,
	[UpdatedOn] [datetime] NOT NULL,
	[EffectiveStartDate] [datetime2](0) GENERATED ALWAYS AS ROW START NOT NULL,
	[EffectiveEndDate] [datetime2](0) GENERATED ALWAYS AS ROW END NOT NULL,
	CONSTRAINT [PK_IntersectGroupItem] PRIMARY KEY CLUSTERED ( [IntersectGroupID] ASC, [IntersectID] ASC ),
	PERIOD FOR SYSTEM_TIME ([EffectiveStartDate], [EffectiveEndDate])
) WITH ( SYSTEM_VERSIONING = ON ( HISTORY_TABLE = [dbo].[IntersectGroupItem_History] ) )
GO

ALTER TABLE [dbo].[IntersectGroupItem] ADD  CONSTRAINT [DF_IntersectGroupItem_State]  DEFAULT ((1)) FOR [State]
GO

ALTER TABLE [dbo].[IntersectGroupItem] ADD  CONSTRAINT [DF_IntersectGroupItem_CreatedBy]  DEFAULT ((0)) FOR [CreatedBy]
GO

ALTER TABLE [dbo].[IntersectGroupItem] ADD  CONSTRAINT [DF_IntersectGroupItem_CreatedOn]  DEFAULT (getutcdate()) FOR [CreatedOn]
GO

ALTER TABLE [dbo].[IntersectGroupItem] ADD  CONSTRAINT [DF_IntersectGroupItem_UpdatedBy]  DEFAULT ((0)) FOR [UpdatedBy]
GO

ALTER TABLE [dbo].[IntersectGroupItem] ADD  CONSTRAINT [DF_IntersectGroupItem_UpdatedOn]  DEFAULT (getutcdate()) FOR [UpdatedOn]
GO

ALTER TABLE [dbo].[IntersectGroupItem]  WITH CHECK ADD  CONSTRAINT [FK_IntersectGroupItem_IntersectGroup] FOREIGN KEY([IntersectGroupID]) REFERENCES [dbo].[IntersectGroup] ([ID]) ON DELETE CASCADE
GO

ALTER TABLE [dbo].[IntersectGroupItem] CHECK CONSTRAINT [FK_IntersectGroupItem_IntersectGroup]
GO

EXEC sp_rename 'dbo.IntersectType', 'IntersectTypeOld'
GO
EXEC sp_rename 'PK_IntersectType', 'PK_IntersectTypeOld'
GO
EXEC sp_rename 'UQ_IntersectType', 'UQ_IntersectTypeOld'
GO
ALTER TABLE [dbo].[IntersectTypeOld] DROP CONSTRAINT [DF_IntersectType_SubjectCardinality]
ALTER TABLE [dbo].[IntersectTypeOld] DROP CONSTRAINT [DF_IntersectType_ObjectCardinality]
GO


CREATE TABLE [dbo].[IntersectType](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [varchar](250) NULL,
	[Subject] [varchar](50) NULL,
	[SubjectID] [int] NULL,
	[Object] [varchar](50) NULL,
	[ObjectID] [int] NULL,
	[IsSystem] [bit] NULL,
	[State] [int] NOT NULL,
	[CreatedBy] [int] NULL,
	[CreatedOn] [datetime] NULL,
	[UpdatedBy] [int] NULL,
	[UpdatedOn] [datetime] NULL,
	[PredicateID] [int] NULL,
	[SubjectCardinality] [int] NOT NULL,
	[ObjectCardinality] [int] NOT NULL,
	[EffectiveStartDate] [datetime2](0) GENERATED ALWAYS AS ROW START NOT NULL,
	[EffectiveEndDate] [datetime2](0) GENERATED ALWAYS AS ROW END NOT NULL,
	CONSTRAINT [PK_IntersectType] PRIMARY KEY NONCLUSTERED ( [ID] ASC ),
	CONSTRAINT [UQ_IntersectType] UNIQUE CLUSTERED (
		[Subject] ASC,
		[SubjectID] ASC,
		[Object] ASC,
		[ObjectID] ASC,
		[PredicateID] ASC
	),
	PERIOD FOR SYSTEM_TIME ([EffectiveStartDate], [EffectiveEndDate])
) WITH ( SYSTEM_VERSIONING = ON ( HISTORY_TABLE = [dbo].[IntersectType_History] ) )
GO

ALTER TABLE [dbo].[IntersectType] ADD  CONSTRAINT [DF_IntersectType_State]  DEFAULT ((1)) FOR [State]
GO

ALTER TABLE [dbo].[IntersectType] ADD  CONSTRAINT [DF_IntersectType_SubjectCardinality]  DEFAULT ((2)) FOR [SubjectCardinality]
GO

ALTER TABLE [dbo].[IntersectType] ADD  CONSTRAINT [DF_IntersectType_ObjectCardinality]  DEFAULT ((2)) FOR [ObjectCardinality]
GO

SET IDENTITY_INSERT IntersectType ON
INSERT INTO [dbo].[IntersectType]
           (ID,[Subject],[SubjectID],[Object],[ObjectID],[IsSystem],[State],[CreatedBy],[CreatedOn],[UpdatedBy],[UpdatedOn]
		   ,[PredicateID],[SubjectCardinality],[ObjectCardinality])
SELECT [ID]
      ,[Subject]
      ,[SubjectID]
      ,[Object]
      ,[ObjectID]
      ,[IsSystem]
	  ,1
      ,[CreatedBy]
      ,[CreatedOn]
      ,[UpdatedBy]
      ,[UpdatedOn]
      ,[PredicateID]
      ,[SubjectCardinality]
      ,[ObjectCardinality]
  FROM [dbo].[IntersectTypeOld]
SET IDENTITY_INSERT IntersectType OFF
go


EXEC sp_rename 'dbo.Intersect', 'IntersectOld'
GO
EXEC sp_rename 'PK_Intersect', 'PK_IntersectOld'
GO
EXEC sp_rename 'UQ_Intersect', 'UQ_IntersectOld'
GO
ALTER TABLE [dbo].[IntersectOld] DROP CONSTRAINT [FK_Intersect_IntersectType]
GO
ALTER TABLE [dbo].[IntersectOld] DROP CONSTRAINT [DF_Intersect_CreatedBy]
ALTER TABLE [dbo].[IntersectOld] DROP CONSTRAINT [DF_Intersect_CreatedOn]
ALTER TABLE [dbo].[IntersectOld] DROP CONSTRAINT [DF_Intersect_Deleted]
ALTER TABLE [dbo].[IntersectOld] DROP CONSTRAINT [DF_Intersect_UpdatedBy]
ALTER TABLE [dbo].[IntersectOld] DROP CONSTRAINT [DF_Intersect_UpdatedOn]
ALTER TABLE [dbo].[IntersectOld] DROP CONSTRAINT [DF_Intersect_Visible]
GO




CREATE TABLE [dbo].[Intersect](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[IntersectTypeID] [int] NOT NULL,
	[Name] [nvarchar](250) NULL,
	[Subject] [varchar](50) NULL,
	[SubjectID] [int] NULL,
	[Object] [varchar](50) NULL,
	[ObjectID] [int] NULL,
	[State] [int] NOT NULL,
	[CreatedBy] [int] NULL,
	[CreatedOn] [datetime] NULL,
	[UpdatedBy] [int] NULL,
	[UpdatedOn] [datetime] NULL,
	[Owner] [varchar](100) NULL,
	[Deleted] [bit] NULL,
	[Visible] [bit] NOT NULL,
	[EffectiveStartDate] [datetime2](0) GENERATED ALWAYS AS ROW START NOT NULL,
	[EffectiveEndDate] [datetime2](0) GENERATED ALWAYS AS ROW END NOT NULL,
	CONSTRAINT [PK_Intersect] PRIMARY KEY NONCLUSTERED ( [ID] ASC ),
	CONSTRAINT [UQ_Intersect] UNIQUE CLUSTERED (
		[IntersectTypeID] ASC,
		[Subject] ASC,
		[SubjectID] ASC,
		[Object] ASC,
		[ObjectID] ASC
	),
	PERIOD FOR SYSTEM_TIME ([EffectiveStartDate], [EffectiveEndDate])
) WITH ( SYSTEM_VERSIONING = ON ( HISTORY_TABLE = [dbo].[Intersect_History] ) )
GO

ALTER TABLE [dbo].[Intersect] ADD  CONSTRAINT [DF_Intersect_State]  DEFAULT ((1)) FOR [State]
GO

ALTER TABLE [dbo].[Intersect] ADD  CONSTRAINT [DF_Intersect_CreatedBy]  DEFAULT ((0)) FOR [CreatedBy]
GO

ALTER TABLE [dbo].[Intersect] ADD  CONSTRAINT [DF_Intersect_CreatedOn]  DEFAULT (getutcdate()) FOR [CreatedOn]
GO

ALTER TABLE [dbo].[Intersect] ADD  CONSTRAINT [DF_Intersect_UpdatedBy]  DEFAULT ((0)) FOR [UpdatedBy]
GO

ALTER TABLE [dbo].[Intersect] ADD  CONSTRAINT [DF_Intersect_UpdatedOn]  DEFAULT (getutcdate()) FOR [UpdatedOn]
GO

ALTER TABLE [dbo].[Intersect] ADD  CONSTRAINT [DF_Intersect_Deleted]  DEFAULT ((0)) FOR [Deleted]
GO

ALTER TABLE [dbo].[Intersect] ADD  CONSTRAINT [DF_Intersect_Visible]  DEFAULT ((1)) FOR [Visible]
GO

ALTER TABLE [dbo].[Intersect]  WITH CHECK ADD  CONSTRAINT [FK_Intersect_IntersectType] FOREIGN KEY([IntersectTypeID]) REFERENCES [dbo].[IntersectType] ([ID]) ON DELETE CASCADE
GO

ALTER TABLE [dbo].[Intersect] CHECK CONSTRAINT [FK_Intersect_IntersectType]
GO

SET IDENTITY_INSERT [Intersect] ON
INSERT INTO [Intersect] (ID, IntersectTypeID, Subject, SubjectID, Object, ObjectID, State, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, Owner, Deleted, Visible)
	SELECT [ID]
		  ,[IntersectTypeID]
		  ,[Subject]
		  ,[SubjectID]
		  ,[Object]
		  ,[ObjectID]
		  ,1
		  ,[CreatedBy]
		  ,[CreatedOn]
		  ,[UpdatedBy]
		  ,[UpdatedOn]
		  ,[Owner]
		  ,[Deleted]
		  ,[Visible]
	  FROM [dbo].[IntersectOld]
SET IDENTITY_INSERT [Intersect] OFF
GO


create view AssetApiModel
as
select	ID,
		AssetTypeID
		,SourceID
from	Asset
GO

CREATE FUNCTION [dbo].[GetAssetDisplayValue]()
RETURNS TABLE 
AS
RETURN 
(
	select		A.AssetTypeID,
				A.ID,
				A.Object,
				A.ObjectID,
				string_agg(coalesce(D.FormattedValue, D.value), '') as DisplayValue
	from		Asset A
				inner join AssetType T on T.ID = A.AssetTypeID 
				outer apply (
							select	TL.value,
									coalesce(case when TF.Value = 'FirstName' then R.FirstName + ' ' else R.LastName end, F.FormattedValue, RI.Code, FA.TextPath) as FormattedValue
							from	string_split(replace(T.DisplayFormat, '{', '|'), '|') TF
									cross apply string_split(replace(TF.[value], '}', '|'), '|') TL
									left join FieldType FT on FT.AssetTypeID = T.ID and FT.Name like TL.Value
									left join Field F on F.FieldTypeID = FT.ID and F.AssetID = A.ID
									left join ReferenceItem RI on TL.Value = 'Code' and A.Object = 'ReferenceItem' and RI.ID = A.ObjectID
									left join FusionAttribute FA on TL.Value = 'Name' and A.Object = 'FusionAttribute' and FA.ID = A.ObjectID
									left join reporting.Global_resource R on TF.Value in ('FirstName', 'LastName') and A.Object = 'Resource' and R.ResourceID = A.ObjectID
							where	RTRIM(TF.value) <> ''
									and RTRIM(TL.value) <> ''
							) D
	group by	A.AssetTypeID,
				A.ID,
				A.Object,
				A.ObjectID
)
GO

CREATE FUNCTION [dbo].[GetAssetDisplayValueById]
(
	@Id bigint
)
RETURNS TABLE 
AS
RETURN 
(
	select		top 1
				string_agg(coalesce(D.FormattedValue, D.value), '') as DisplayValue
	from		dbo.Asset A
				inner join dbo.AssetType T on T.ID = A.AssetTypeID 
				outer apply (
							select	TF.value,
									coalesce(case when TF.Value = 'FirstName' then R.FirstName + ' ' else R.LastName end, F.FormattedValue, RI.Code, FA.Name) as FormattedValue
							from	string_split(replace(replace(T.DisplayFormat, '{', '|'),'}','|'), '|') TF									
									left join dbo.FieldType FT on FT.AssetTypeID = T.ID and FT.Name = TF.Value
									left join dbo.Field F on F.FieldTypeID = FT.ID and F.AssetID = A.ID
									left join dbo.ReferenceItem RI on TF.Value = 'Code' and A.Object = 'ReferenceItem' and RI.ID = A.ObjectID
									left join dbo.FusionAttribute FA on TF.Value = 'Name' and A.Object = 'FusionAttribute' and FA.ID = A.ObjectID
									left join reporting.Global_resource R on TF.Value in ('FirstName', 'LastName') and A.Object = 'Resource' and R.ResourceID = A.ObjectID
							where	RTRIM(TF.value) <> ''									
							) D
	where A.ID = @Id
)
GO

CREATE FUNCTION [dbo].[GetAssetFieldHash]()
RETURNS TABLE 
AS
RETURN 
(
	select		A.AssetTypeID,
				A.ID,
				CONVERT(
					varchar(32), 
					SUBSTRING(HASHBYTES('SHA1', STRING_AGG(cast(FieldTypeID as nvarchar) + ':' + Value, char(59))), 3, 32), 
					2) as FieldHash
	from		Asset A
				inner join FieldType FT on FT.AssetTypeID = A.AssetTypeID
				inner join Field F on F.AssetID = A.ID and FT.ID = F.FieldTypeID 
	group by	A.AssetTypeID, A.ID
)
GO

CREATE FUNCTION [dbo].[GetAssetKeyHash]()
RETURNS TABLE 
AS
RETURN 
(
	

select		A.AssetTypeID,
				A.ID,
				CONVERT(
					varchar(32), 
					SUBSTRING(HASHBYTES('SHA1', STRING_AGG(cast(FieldTypeID as nvarchar) + ':' + Value, char(59))), 3, 32), 
					2) as KeyHash
	from		Asset A
				inner join Field F on F.ObjectType = A.Object and F.ObjectID = A.ObjectID and A.Object != 'ReferenceItem'
				inner join FieldType FT on FT.ID = F.FieldTypeID 
										and FT.AssetTypeID = A.AssetTypeID
										and FT.IsPartOfKey = 1
	group by	A.AssetTypeID, A.ID
union
select		A.AssetTypeID,
				A.ID,
				CONVERT(
					varchar(32), 
					SUBSTRING(HASHBYTES('SHA1', STRING_AGG(r.code, char(59))), 3, 32), 
					2) as KeyHash
	from		Asset A
				inner join referenceitem r on (a.object = 'ReferenceItem' and r.id = a.objectid)
	group by	A.AssetTypeID, A.ID

)
GO

CREATE view PredicateIntersect
as
select	I.ID as IntersectID,
		I.IntersectTypeID,
		I.Subject,
		I.SubjectID,
		I.Object,
		I.ObjectID,
		I.[State],
		T.PredicateID,
		P.Name as PredicateName,
		P.Inverse as PredicateInverse,
		P.Type as PredicateType
from	[Intersect] I
		inner join IntersectType T on T.ID = I.IntersectTypeID
		inner join [Predicate] P on P.ID = T.PredicateID
GO


create view [dbo].[AssetWithType]
as
	select	A.ID,
			A.AssetTypeID,
			A.State,
			A.Object,
			A.ObjectID,
			A.SourceID,
			A.CreatedOn,
			A.CreatedBy,
			A.UpdatedOn,
			A.UpdatedBy,
			T.Class as AssetTypeClass,
			T.Description as AssetTypeDescription,
			T.Name as TypeName,
			T.Object as Type,
			T.ObjectID as TypeID,
			coalesce(S.IconBackColor, '#000') as BackColor,
			coalesce(S.IconForeColor, '#fff') as ForeColor,
			coalesce(S.IconText, 'leaf') as Icon
	from	Asset A
			inner join AssetType T on T.ID = A.AssetTypeID
			left join ObjectStyle S on S.ObjectType = T.Object and S.ObjectID = T.ObjectID
GO

CREATE view [dbo].[AssetDetail]
as
	select	A.ID,
			A.AssetTypeID,
			A.State,
			A.Object,
			A.ObjectID,
			A.SourceID,
			D.DisplayValue,
			K.KeyHash,
			F.FieldHash,
			A.CreatedOn,
			A.CreatedBy,
			A.UpdatedOn,
			A.UpdatedBy,
			A.AssetTypeClass,
			A.AssetTypeDescription,
			A.TypeName,
			A.Type,
			A.TypeID,
			A.BackColor,
			A.ForeColor,
			A.Icon
	from	AssetWithType A
			cross apply dbo.GetAssetDisplayValueById(A.ID) D	--left join GetAssetDisplayValue() D on D.ID = A.ID
			left join GetAssetKeyHash() K on K.ID = A.ID
			left join GetAssetFieldHash() F on F.ID = A.ID
GO

CREATE VIEW [dbo].[AssetWithFieldInfo]   
AS   
SELECT   
     ss.ID
     ,ss.[Object]
	 ,ss.ObjectID
	 ,ss.EffectiveStartDate
	 ,(gr.firstname + ' ' + gr.lastname) as ResourceName
	 ,ft.id as FieldTypeID
	 ,ft.FriendlyName  
	 ,f.FormattedValue
FROM [dbo].[asset] ss   
	inner join [dbo].[assettype] sst on (sst.id = ss.assettypeid)
	inner JOIN [dbo].[fieldtype] ft on (ft.[object] = sst.[object] and ft.[objectid] = sst.[objectid])
	left join [dbo].[field] f on(ss.[object] = f.objecttype and ss.[objectid] = f.objectid and f.fieldtypeid= ft.id)
	left join reporting.global_resource gr on (ss.updatedby = gr.resourceid)
GO

create view AssetWithoutReadPermission 
as
select		O.AssetID,
			A.Object,
			A.ObjectID,
			case O.SecurityAsset
				when 'G' then ReGr.ResourceID
				when 'O' then OrRe.ResourceID
				when 'R' then O.SecurityAssetID
				else null
			end as ResourceID
from		dbo.ResponsibilityTypeRelationItem O
			inner join dbo.Asset A on A.ID = O.AssetID
			inner join dbo.AssetType T on T.ID = A.AssetTypeID
			left join dbo.OrganizationResource OrRe on O.SecurityAsset = 'O' and OrRe.OrganizationID = O.SecurityAssetID
			left join dbo.ResourceGroup ReGr on O.SecurityAsset = 'G' and ReGr.GroupID = O.SecurityAssetID
			left join dbo.ResponsibilityTypeObjectClaim C on C.ResponsibilityTypeID = O.ResponsibilityTypeID and C.ObjectType = T.Object and C.ObjectID = T.ObjectID and C.Claim = 1 and C.ClaimObject = 1
where		O.Overriden = 0
			and C.ObjectID is null 
group by	O.AssetID,
			A.Object,
			A.ObjectID,
			case O.SecurityAsset
				when 'G' then ReGr.ResourceID
				when 'O' then OrRe.ResourceID
				when 'R' then O.SecurityAssetID
				else null
			end
GO

create view FieldApiModel
as
select	AssetID,
		FieldTypeID,
		Name,
		FormattedValue as Value
from	Field F inner join FieldType T on T.ID = F.FieldTypeID
GO

CREATE VIEW [dbo].[FieldDetail]
AS
	SELECT	T.ID as FieldTypeID,
			T.Name,
			T.FriendlyName,
			A.ID as AssetID,
			A.Object,
			A.ObjectID,
			coalesce(F.Value, T.DefaultValue) as Value,
			case
				when T.AllowAllValue = 1 and F.FormattedValue = '0' then T.AllowAllLabel
				when F.FormattedValue is not null then F.FormattedValue
				when T.DefaultFormattedValue is not null then T.DefaultFormattedValue
				else null
			end as FormattedValue
	FROM	Asset A
			inner join FieldType T on T.AssetTypeID = A.AssetTypeID
			left join Field F on F.FieldTypeID = T.ID and F.ObjectType = A.Object and F.ObjectID = A.ObjectID
	WHERE	(F.Value is not null OR T.DefaultValue is not null)
GO

ALTER TABLE [ResponsibilityTypeRelationRule] ADD [Context] NVARCHAR (MAX) NULL
GO
ALTER TABLE [ResponsibilityTypeRelationOverrideItem] ADD [Context] NVARCHAR (MAX) NULL
GO

CREATE VIEW [dbo].[ResponsibilityDetails]
AS 
select	O.AssetID,
		A.Object,
		A.ObjectID,
		T.Object as Type,
		T.ObjectID as TypeID,
		R.Name as RuleName,
		coalesce(X.Context,R.Context) as Context,
		O.ResponsibilityTypeID,
		RT.Name as ResponsibilityTypeName,
		GrRe.FirstName,
		GrRe.LastName,
		case O.SecurityAsset
			when 'G' then ReGr.ResourceID
			when 'O' then OrRe.ResourceID
			when 'R' then O.SecurityAssetID
			else null
		end as ResourceID,
		O.SecurityAsset,
		O.SecurityAssetID,
		case O.SecurityAsset
			when 'G' then Gr.Name
			when 'O' then Org.Name
			when 'R' then GrRe.LastName + ', ' + GrRe.FirstName
			else null
		end as SecurityAssetName,
		O.Overriden,
		O.OverrideItemID
from	dbo.ResponsibilityTypeRelationItem O
		inner join dbo.Asset A on A.ID = O.AssetID
		inner join dbo.AssetType T on T.ID = A.AssetTypeID
		left join dbo.ResponsibilityTypeRelationRule R on R.ID = O.RuleID
		left join dbo.ResponsibilityTypeRelationOverrideItem X 
		on X.ResponsibilityTypeID = O.ResponsibilityTypeID
			and x.AssetID=o.AssetID
			and x.SecurityAsset=o.SecurityAsset
			and x.SecurityAssetID=o.SecurityAssetID
		inner join dbo.ResponsibilityType RT on RT.ID = O.ResponsibilityTypeID
		left join dbo.OrganizationResource OrRe on O.SecurityAsset = 'O' and OrRe.OrganizationID = O.SecurityAssetID
		left join dbo.Organization Org on O.SecurityAsset = 'O' and Org.ID = OrRe.OrganizationID
		left join dbo.ResourceGroup ReGr on O.SecurityAsset = 'G' and ReGr.GroupID = O.SecurityAssetID
		left join dbo.[Group] Gr on O.SecurityAsset = 'G' and Gr.ID = ReGr.GroupID
		inner join reporting.Global_Resource GrRe on GrRe.ResourceID =	case O.SecurityAsset
																			when 'G' then ReGr.ResourceID
																			when 'O' then OrRe.ResourceID
																			when 'R' then O.SecurityAssetID
																			else null
																		end
where	O.Overriden = 0
GO

create FUNCTION [dbo].[GetAssetLevelById]
(
	@Id bigint
)
RETURNS TABLE 
AS
RETURN 
(
	with c as (
		select	T.ID as AssetID,
				T.Object as AssetObject,
				T.ObjectID as AssetObjectID,
				T.ID,
				T.Object,
				T.ObjectID,
				1 as Level
		from	Asset T
				cross apply dbo.GetAssetDisplayValueById(T.ID) D
				left join PredicateIntersect I on I.Object = T.Object and I.ObjectID = T.ObjectID and I.PredicateType in (3, 4)
		where	T.ID = @Id
		union all
		select	c.AssetID,
				c.AssetObject,
				c.AssetObjectID,
				T.ID,
				T.Object,
				T.ObjectID,
				c.Level + 1 as Level
		from	Asset T
				cross apply dbo.GetAssetDisplayValueById(T.ID) D
				inner join PredicateIntersect I on I.Subject = T.Object and I.SubjectID = T.ObjectID and I.PredicateType in (3, 4)
				inner join c on I.Object = c.Object and I.ObjectID = c.ObjectID
		
	)

	select	top 1 
			AssetID as ID, 
			AssetObject as Object, 
			AssetObjectID as ObjectID, 
			Level 
	from	c
	order by Level desc
)
GO

CREATE FUNCTION [dbo].[GetAssetTextPath]
(
	@delimiter nvarchar(5)
)
RETURNS TABLE 
AS
RETURN 
(
	with h as (
		select	T.ID,
				T.Object,
				T.ObjectID,
				cast(null as bigint) as ParentID,
				D.DisplayValue as TextPath,
				1 as [Level]
		from	Asset T
				inner join dbo.GetAssetDisplayValue() D on D.ID = T.ID
				left join PredicateIntersect I on I.Object = T.Object and I.ObjectID = T.ObjectID and I.PredicateType in (3, 4)
		where	I.IntersectID is null
		union all
		select	T.ID,
				T.Object,
				T.ObjectID,
				P.ID as ParentID,
				P.TextPath + @delimiter + D.DisplayValue as TextPath,
				P.[Level] + 1 as [Level]
		from	Asset T
				inner join dbo.GetAssetDisplayValue() D on D.ID = T.ID
				inner join PredicateIntersect I on I.Object = T.Object and I.ObjectID = T.ObjectID and I.PredicateType in (3, 4)
				inner join h as P on I.Subject = P.Object and I.SubjectID = P.ObjectID
	)

	select * from h
)
GO

CREATE FUNCTION GetParentByAssetID
(	
	@id bigint
)
RETURNS TABLE 
AS
RETURN 
(
	SELECT 
		P.ID, 
		I.ID as IntersectID, 
		Y.ID as IntersectTypeID from Asset A
	inner join AssetType T on T.ID = A.AssetTypeID
	inner join [IntersectType] Y on Y.[Object] = T.[Object] and Y.ObjectID = T.ObjectID
	inner join [Predicate] R on R.ID = Y.PredicateID
		and R.[Type] = case when Y.[Subject] = 'PolicyType' or Y.[Subject] = 'TaxonomyType' then 4 else 3 end 
	inner join [Intersect] I on I.IntersectTypeID = Y.ID and I.[Object] = A.[Object] and I.ObjectID = A.ObjectID
	inner join Asset P on P.[Object] = I.[Subject] and P.ObjectID = I.SubjectID
	where A.ID = @id
)
GO

CREATE FUNCTION [dbo].[GetAssetTextPathById]
(
	@Id bigint,
	@delimiter nvarchar(5)
)
RETURNS TABLE 
AS
RETURN 
(

	with c as (
		select	T.ID as AssetID,
				P.ID as ParentID,
				T.Object as AssetObject,
				T.ObjectID as AssetObjectID,
				T.ID,
				T.Object,
				T.ObjectID,
				D.DisplayValue,
				1 as Level
		from	Asset T
				outer apply dbo.GetAssetDisplayValueById(T.ID) D
				outer apply dbo.GetParentByAssetID(T.ID) P
		where	T.ID = @Id
		union all
		select	c.AssetID,
				P.ID as ParentID,
				c.AssetObject,
				c.AssetObjectID,
				T.ID,
				T.Object,
				T.ObjectID,
				D.DisplayValue + @delimiter + c.DisplayValue as DisplayValue,
				c.Level + 1 as Level
		from	Asset T
				outer apply dbo.GetAssetDisplayValueById(T.ID) D
				outer apply dbo.GetParentByAssetID(T.ID) P
				inner join c on c.ParentID = T.ID
	)

	select	top 1 
			AssetID as ID, 
			AssetObject as Object, 
			AssetObjectID as ObjectID, 
			DisplayValue as TextPath, 
			Level 
	from	c
	order by Level desc
)
GO

CREATE FUNCTION [dbo].[GetAssetUrl]
(	
	@Type varchar(50),
	@TypeID int,
	@ObjectID int = 0
)
RETURNS TABLE 
AS
RETURN 
(
	-- Add the SELECT statement with parameter references here
	SELECT CASE @Type
		WHEN 'Artifact' THEN 'artifact/' +  + CAST(@TypeID as varchar) + '/' + CAST(@ObjectID as varchar)
		WHEN 'ArtifactType' THEN 'artifact/' + CAST(@TypeID as varchar)
		WHEN 'Domain' THEN 'domain/' +  + CAST(@TypeID as varchar) + '/' +  + CAST(@ObjectID as varchar)
		WHEN 'DomainType' THEN 'domain/' + CAST(@TypeID as varchar)
		WHEN 'ReferenceItem' THEN 'reference/' +  + CAST(@TypeID as varchar)-- + '/' +  + CAST(@ObjectID as varchar)
		WHEN 'ReferenceItemType' THEN 'reference/' + CAST(@TypeID as varchar)
		WHEN 'FusionAttribute' THEN 'fusion/fusionattribute/' + CAST(@TypeID as varchar) + '/' + CAST(@ObjectID as varchar)		
		WHEN 'Fusion' THEN 'fusion/' + CAST(@TypeID as varchar) + '/' + + CAST(@ObjectID as varchar)
		WHEN 'FusionType' THEN 'fusion/' + CAST(@TypeID as varchar)
		WHEN 'Group' THEN 'group/' + CAST(@ObjectID as varchar)	
		WHEN 'Lookup' THEN 'admin/lookups/' + CAST(@TypeID as varchar) + '/' + + CAST(@ObjectID as varchar)
		WHEN 'LookupType' THEN 'admin/lookups/' + CAST(@TypeID as varchar)
		WHEN 'Policy' THEN 'policy/' + CAST(@TypeID as varchar(15)) + '/id/' + CAST(@ObjectID as varchar)
		WHEN 'PolicyType' THEN 'policy/' + CAST(@TypeID as varchar) + '/structure'		
		WHEN 'Resource' THEN 'resource/' + CAST(@ObjectID as varchar)
		WHEN 'ResourceType' THEN 'resource/list/' + CAST(@TypeID as varchar)
		WHEN 'Rule' THEN 'quality/rule/' + CAST(@TypeID as varchar) + '/' + CAST(@ObjectID as varchar)
		WHEN 'RuleType' THEN 'quality/rule/' + CAST(@TypeID as varchar)	
		WHEN 'Taxonomy' THEN 'model/' + CAST(@TypeID as varchar) + '/id/' + CAST(@ObjectID as varchar)
		WHEN 'TaxonomyType' THEN 'model/' + CAST(@ObjectID as varchar) + '/structure'	
		WHEN 'ShoppingCartType' THEN 'cart/' + CAST(@ObjectID as varchar)	
	end as Url
)
GO

CREATE FUNCTION [dbo].[GetAssetHasChildrenByAssetID]
(
	@assetId bigint,
	@predicateType int
)
RETURNS TABLE 
AS
RETURN 
(
	select	
	case 
            when count(1) > 0 then cast(1 as bit) 
            else cast(0 as bit) 
        end as HasChildren
				    from	dbo.Asset A
							inner join dbo.[Intersect] I on I.Subject = A.Object and I.SubjectID = A.ObjectID
							inner join dbo.[IntersectType] IT on I.IntersectTypeID = IT.ID
							inner join dbo.[Predicate] P on P.ID = IT.PredicateID and P.Type <> @predicateType
                            inner join dbo.Asset IA on IA.Object = I.Object and IA.ObjectID = I.ObjectID 
                            inner join dbo.AssetType IAT on IAT.ID = IA.AssetTypeID                            
					where A.ID = @assetId
)
GO

CREATE FUNCTION [dbo].[GetChildObjectIds]
(
	@type varchar(50),
	@id int
)
RETURNS TABLE
AS
RETURN
(
	select I.ObjectID as ChildID from Asset A
	inner join AssetType ST on ST.ID = A.AssetTypeID
	inner join [IntersectType] T on T.[Subject] = ST.[Object] and T.SubjectID = ST.ObjectID
	inner join [Predicate] P on P.ID = T.PredicateID and P.[Type] = 3
	inner join [Intersect] I on I.Subject = @type and I.SubjectID = @id and I.IntersectTypeID = T.ID
	where A.[Object] = @type and A.ObjectID = @id
)
GO

create FUNCTION [dbo].[GetArtifactChildByAssetID]
(
	@Id bigint
)
RETURNS TABLE 
AS
RETURN 
(
	select	A.ID as ID,
			A.ObjectID as ObjectID,
			I.SubjectID as ChildID,
            ID.DisplayValue as ChildDisplayValue,
			IAT.[ObjectID] as ChildArtifactTypeID,
			IAT.Name as ChildArtifactTypeName			
				    from	dbo.Asset A
							inner join dbo.[Intersect] I on I.Subject = A.Object and I.SubjectID = A.ObjectID
							inner join dbo.[IntersectType] IT on I.IntersectTypeID = IT.ID
							inner join dbo.[Predicate] P on P.ID = IT.PredicateID and P.Type = 3
                            inner join dbo.Asset IA on IA.Object = 'Artifact' and IA.ObjectID = I.ObjectID 
                            inner join dbo.AssetType IAT on IAT.ID = IA.AssetTypeID
                            cross apply dbo.GetAssetDisplayValueById(IA.ID) ID							
					where A.[Object] = 'Artifact' and A.ID = @Id
)
GO

create FUNCTION [dbo].[GetArtifactParentByAssetID]
(
	@Id bigint
)
RETURNS TABLE 
AS
RETURN 
(
	select	A.ID as ID,
			A.ObjectID as ObjectID,
			I.SubjectID as ParentID,
            ID.DisplayValue as ParentDisplayValue,
			PUrl.Url as ParentUrl
				    from	dbo.Asset A
							inner join dbo.[Intersect] I on I.Object = A.Object and I.ObjectID = A.ObjectID
							inner join dbo.[IntersectType] IT on I.IntersectTypeID = IT.ID
							inner join dbo.[Predicate] P on P.ID = IT.PredicateID and P.Type = 3
                            inner join dbo.Asset IA on IA.Object = 'Artifact' and IA.ObjectID = I.SubjectID 
                            inner join dbo.AssetType IAT on IAT.ID = IA.AssetTypeID
                            cross apply dbo.GetAssetDisplayValueById(IA.ID) ID
							cross apply dbo.GetAssetUrl('Artifact', IAT.ObjectID, I.SubjectID) PUrl
					where A.[Object] = 'Artifact' and A.ID = @Id
)
GO

ALTER TABLE Organization ADD [State] INT CONSTRAINT [DF_Organization_State] DEFAULT ((1)) NOT NULL
GO

ALTER TABLE [Contract] add [PublishedOn]    DATETIME       NULL
ALTER TABLE [Contract] add [State]          INT            CONSTRAINT [DF_Contract_State] DEFAULT ((1)) NOT NULL
ALTER TABLE [Contract] add [UpdatedOn]      DATETIME       NULL
ALTER TABLE [Contract] add [UpdatedBy]      INT            NULL
ALTER TABLE [Contract] add [CreatedOn]      DATETIME       NULL
ALTER TABLE [Contract] add [CreatedBy]      INT            NULL
GO

CREATE FUNCTION [dbo].[GetContractValidations] 
(	
	@ResourceID int
)
RETURNS TABLE 
AS
RETURN 
(

--declare @ResourceID int;
--select @ResourceID = 3243;

select 
		C.ID as ContractID, 
		C.OrganizationID, 
		C.ContractType,
		case when H.ID is null then 
			0 
		else 
			1 
		end as Accepted,
		R.IsFirstUser
	from [Contract] C 
	inner join 
	( 
		select r.ResourceID, i.OrganizationID,
		case when (select count(*) from OrganizationResource where OrganizationID = i.OrganizationID and Accepted = 1) > 0 then
			0
		else
			1
		end as IsFirstUser
		from OrganizationInvitation i
		inner join reporting.Global_resource r on r.Email = i.Email
		union all
		select o.ResourceID, o.OrganizationID,
		case when (select count(*) from Organization where ID = o.OrganizationID and Accepted = 0) > 0 then
			1
		else
			0
		end as IsFirstUser 
		from OrganizationResource o
		union all
		select r.ResourceID, d.OrganizationID,
		case when (select count(*) from Organization where ID = o.ID and Accepted = 0) > 0 then
			1
		else
			0
		end as IsFirstUser 
		from OrganizationDomain D
		inner join Organization O on O.ID = D.OrganizationID and O.[State] = 1
		inner join reporting.Global_resource R on r.Email like '%@' + d.Domain
	) R on R.OrganizationID = C.OrganizationID and R.ResourceID = @ResourceID
	left join ContractAcceptance H on H.ContractID = C.ID and H.OrganizationID = C.OrganizationID 
		and H.AcceptedOn > C.PublishedOn and H.ResourceID = R.ResourceID
	where 
		C.[State] = 1 and C.PublishedOn is not null and C.OrganizationID is not null 

	union all

	select 
		C.ID as ContractID, 
		null as OrganizationID, 
		C.ContractType,
		case when (C.ContractType = 2 and H.ID is null) or (C.ContractType = 1 and (H2.AcceptedOn is null or H2.AcceptedOn < C.PublishedOn)) then 
			0 
		else 
			1 
		end as Accepted,
		case when (C.ContractType = 2 and H.ID is null) or (C.ContractType = 1 and (H2.AcceptedOn is null or H2.AcceptedOn < C.PublishedOn)) then
			1
		else
			0
		end  as IsFirstUser
	from [Contract] C 
	left join ContractAcceptance H on H.ContractID = C.ID and H.OrganizationID is null
		and H.AcceptedOn > C.PublishedOn and H.ResourceID = @ResourceID
	left join (
		select max(AcceptedOn) as AcceptedOn, ContractID from ContractAcceptance A
		where organizationid is null and A.ResourceID in (--resources who share an org with this resource		
			select distinct ResourceID from
			( 
				select i.OrganizationID, r.ResourceID from OrganizationInvitation i
				inner join reporting.Global_resource r on r.Email = i.Email
				union all
				select o.OrganizationID, o.ResourceID from OrganizationResource o
				union all
				select d.OrganizationID, r.ResourceID from OrganizationDomain D
				inner join Organization O on O.ID = D.OrganizationID and O.[State] = 1
				inner join reporting.Global_resource r on r.Email like '%@' + d.Domain
			) z where z.OrganizationID in (--orgs this resource is a member of
				select x.OrganizationID from 
				(
					select i.OrganizationID, r.ResourceID from OrganizationInvitation i
					inner join reporting.Global_resource r on r.Email = i.Email
					union all
					select o.OrganizationID, o.ResourceID from OrganizationResource o
					union all
					select d.OrganizationID, r.ResourceID from OrganizationDomain D
					inner join Organization O on O.ID = D.OrganizationID and O.[State] = 1
					inner join reporting.Global_resource r on r.Email like '%@' + d.Domain
				) x  where ResourceID = @ResourceID
			)
		)
		group by ContractID
	) H2 on H2.ContractID = C.ID
	where 
		C.[State] = 1 and C.OrganizationID is null and C.PublishedOn is not null
		and @ResourceID in ( --if the user isn't in an org or invited, they don't need to accept the default contracts
			select r.ResourceID from OrganizationInvitation i
			inner join reporting.Global_resource r on r.Email = i.Email
			union all
			select o.ResourceID from OrganizationResource o
			union all
			select r.ResourceID from OrganizationDomain D
			inner join Organization O on O.ID = D.OrganizationID and O.[State] = 1
			inner join reporting.Global_resource r on r.Email like '%@' + d.Domain
		)
)
GO

CREATE FUNCTION [dbo].[GetIntersectNames]
(	
	@id int
)
RETURNS TABLE 
AS
RETURN 
(	
		SELECT	COALESCE(S_A.DisplayValue, S_I.Name, 'Map') + ' / ' + COALESCE(O_A.DisplayValue, 'Map') as Name
					FROM	[Intersect] I
					outer apply (
							select d.DisplayValue  from asset a cross apply GetAssetDisplayValueById(a.ID) d where a.[object] = I.Subject and a.objectid = I.SubjectID and I.Subject != 'Intersect'
						) S_A
					outer apply (
							select d.DisplayValue  from asset a cross apply GetAssetDisplayValueById(a.ID) d where a.[object] = I.Object and a.objectid = I.ObjectID and I.Object != 'Intersect'
						) O_A
					outer apply (
							select 
								 s_d.displayvalue + ' / ' + o_d.displayvalue as Name
							from [intersect] i_s 							
							inner join asset a_s on(a_s.objectid = i_s.subjectid and a_s.[object] = i_s.[subject])
							cross apply GetAssetDisplayValueById(a_s.ID) s_d
							inner join asset a_o on(a_o.objectid = i_s.objectid and a_o.[object] = i_s.[object])
							cross apply GetAssetDisplayValueById(a_o.ID) o_d
							where 
								i_s.id = I.SubjectID and I.Subject = 'Intersect'
						) S_I
					WHERE	I.ID = @id													
)
GO

CREATE FUNCTION [dbo].[GetIntersectTypeNames]
(	
	@id int
)
RETURNS TABLE 
AS
RETURN 
(
	SELECT	COALESCE(SA.Name, SD.Name, SF.TextPath, SM.Name, SP.Name, SR.Name, ST.Name, SI.Name, SQF.Name, SRef.Name, SRes.Name, '') + 
							' [' + coalesce(P.Name,'/') + '] ' + 
							COALESCE(OA.Name, OD.Name, [OF].TextPath, OM.Name, OP.Name, [OR].Name, OT.Name, OQF.Name, ORef.Name, ORes.Name, '') as Name
					FROM	[IntersectType] I
							left join ArtifactType SA on I.Subject = 'ArtifactType' and SA.ID = I.SubjectID
							left join ArtifactType OA on I.Object = 'ArtifactType' and OA.ID = I.ObjectID

							left join ReferenceItemType SD on I.Subject = 'ReferenceItemType' and SD.ID = I.SubjectID
							left join ReferenceItemType OD on I.Object = 'ReferenceItemType' and OD.ID = I.ObjectID

							left join [FusionAttributeType] SF on I.Subject = 'FusionAttributeType' and SF.ID = I.SubjectID
							left join [FusionAttributeType] [OF] on I.Object = 'FusionAttributeType' and [OF].ID = I.ObjectID

							left join [FusionQueryAttributeType] SQF on I.Subject = 'FusionQueryAttributeType' and SQF.ID = I.SubjectID
							left join [FusionQueryAttributeType] [OQF] on I.Object = 'FusionQueryAttributeType' and [OQF].ID = I.ObjectID

							--left join [IntersectType] SI on I.Subject = 'IntersectType' and SI.ID = I.SubjectID
							left join (
								SELECT	I_sub.ID, COALESCE(SA_sub.Name, SD_sub.Name, SF_sub.TextPath, SM_sub.Name, SP_sub.Name, SR_sub.Name, ST_sub.Name, SQF_sub.Name, '') + 
										' [' + coalesce(P_sub.Name,'/') + '] ' + 
										COALESCE(OA_sub.Name, OD_sub.Name, [OF_sub].TextPath, OM_sub.Name, OP_sub.Name, [OR_sub].Name, OT_sub.Name, OQF_sub.Name, '') as Name
								FROM	[IntersectType] I_sub
										left join ArtifactType SA_sub on I_sub.Subject = 'ArtifactType' and SA_sub.ID = I_sub.SubjectID
										left join ArtifactType OA_sub on I_sub.Object = 'ArtifactType' and OA_sub.ID = I_sub.ObjectID

										left join ReferenceItemType SD_sub on I_sub.Subject = 'ReferenceItemType' and SD_sub.ID = I_sub.SubjectID
										left join ReferenceItemType OD_sub on I_sub.Object = 'ReferenceItemType' and OD_sub.ID = I_sub.ObjectID

										left join [FusionAttributeType] SF_sub on I_sub.Subject = 'FusionAttributeType' and SF_sub.ID = I_sub.SubjectID
										left join [FusionAttributeType] [OF_sub] on I_sub.Object = 'FusionAttributeType' and [OF_sub].ID = I_sub.ObjectID

										left join [FusionQueryAttributeType] SQF_sub on I_sub.Subject = 'FusionQueryAttributeType' and SQF_sub.ID = I_sub.SubjectID
										left join [FusionQueryAttributeType] [OQF_sub] on I_sub.Object = 'FusionQueryAttributeType' and [OQF_sub].ID = I_sub.ObjectID

										left join [MapType] SM_sub on I_sub.Subject = 'MapType' and SM_sub.ID = I_sub.SubjectID
										left join [MapType] OM_sub on I_sub.Object = 'MapType' and OM_sub.ID = I_sub.ObjectID

										left join [PolicyType] SP_sub on I_sub.Subject = 'PolicyType' and SP_sub.ID = I_sub.SubjectID
										left join [PolicyType] OP_sub on I_sub.Object = 'PolicyType' and OP_sub.ID = I_sub.ObjectID

										left join [RuleType] SR_sub on I_sub.Subject = 'RuleType' and SR_sub.ID = I_sub.SubjectID
										left join [RuleType] [OR_sub] on I_sub.Object = 'RuleType' and [OR_sub].ID = I_sub.ObjectID

										left join [TaxonomyType] ST_sub on I_sub.Subject = 'TaxonomyType' and ST_sub.ID = I_sub.SubjectID
										left join [TaxonomyType] OT_sub on I_sub.Object = 'TaxonomyType' and OT_sub.ID = I_sub.ObjectID

										left join [Predicate] P_sub on P_sub.ID = I_sub.PredicateID
								--where I_sub.ID = I.SubjectID and I.Subject = 'IntersectType'
							   ) SI on (I.Subject = 'IntersectType' and SI.ID = I.SubjectID)

							left join [MapType] SM on I.Subject = 'MapType' and SM.ID = I.SubjectID
							left join [MapType] OM on I.Object = 'MapType' and OM.ID = I.ObjectID

							left join [PolicyType] SP on I.Subject = 'PolicyType' and SP.ID = I.SubjectID
							left join [PolicyType] OP on I.Object = 'PolicyType' and OP.ID = I.ObjectID

							left join [RuleType] SR on I.Subject = 'RuleType' and SR.ID = I.SubjectID
							left join [RuleType] [OR] on I.Object = 'RuleType' and [OR].ID = I.ObjectID

							left join [TaxonomyType] ST on I.Subject = 'TaxonomyType' and ST.ID = I.SubjectID
							left join [TaxonomyType] OT on I.Object = 'TaxonomyType' and OT.ID = I.ObjectID

							left join (
								select 0 as ID, 'Reference Item Type' as Name								
							  ) SRef on I.Subject = 'ReferenceItemType' and I.SubjectID = 0

							 left join (
								select 0 as ID, 'Reference Item Type' as Name								
							 ) ORef on I.Object = 'ReferenceItemType' and I.ObjectID = 0

							left join (
								select 1 as ID, 'Resource' as Name								
							  ) SRes on I.Subject = 'ResourceType'

							left join (
								select 1 as ID, 'Resource' as Name								
							  ) ORes on I.Object = 'ResourceType'

							left join [Predicate] P on P.ID = I.PredicateID
					WHERE	I.ID = @id				
)
GO

CREATE FUNCTION [dbo].[GetReferenceItemDisplayValue]
(
	@referenceItemId int,
	@fieldTypeId int
)
RETURNS TABLE 
AS
RETURN 
(
	select		top 1
				string_agg(coalesce(D.FormattedValue, D.value), '') as DisplayValue
	from		FieldType FT_1 
				inner join referenceitemtype RIT on RIT.ID = FT_1.LookupObjectID
				inner join referenceitem RI on RI.ReferenceItemTypeID = RIT.ID and RI.ID = @referenceItemId
				outer apply (
							select	TL.value,
									coalesce(F.FormattedValue, RI.Code) as FormattedValue
							from	string_split(replace(FT_1.LookupDisplayFormat, '{', '|'), '|') TF
									cross apply string_split(replace(TF.[value], '}', '|'), '|') TL
									left join FieldType FT on FT.Object = 'ReferenceItemType' and FT.ObjectID = RIT.ID and FT.Name like TL.Value
									left join Field F on F.FieldTypeID = FT.ID and F.ObjectID = RI.ID and F.objecttype = 'ReferenceItem'
									left join ReferenceItem RI on TL.Value = 'Code' and RI.ID = RI.ID									
							where	RTRIM(TF.value) <> ''
									and RTRIM(TL.value) <> ''
							) D
	where FT_1.ID = @fieldTypeId
)
GO

CREATE FUNCTION [utility].[GetAssetBusinessKey]
(
--declare
	@ID int-- = 6
)
RETURNS TABLE
AS
RETURN 
(
	select		@ID as ID,
				STRING_AGG(Value, '-') as [Key]
	from		(
				select		top 100 percent
							Code as Value
				from		Asset A
							inner join ReferenceItem R on A.Object = 'ReferenceItem' and R.ID = A.ObjectID and A.ID = @ID
				union
				select		*
				from		(
							select		top 100 percent
										coalesce(F.FormattedValue, '') as Value
							from		Asset A
										inner join AssetType T on T.ID = A.AssetTypeID and A.ID = @ID
										inner join Field F on F.ObjectType = A.Object and F.ObjectID = A.ObjectID
										inner join FieldType FT on FT.ID = F.FieldTypeID 
																and FT.Object = T.Object and FT.ObjectID = T.ObjectID
																and FT.IsPartOfKey = 1
							order by	FT.ID
							) F
				) A
)
GO

CREATE FUNCTION utility.GetIntersectNames
(	
	@id int
)
RETURNS TABLE 
AS
RETURN 
(
	SELECT	COALESCE(SA.DisplayValue, SD.Name, SF.TextPath, 'Map', SP.TextPath, SR.DisplayValue, ST.TextPath, SI.Name, '') + ' / ' + COALESCE(OA.DisplayValue, OD.Name, [OF].TextPath, 'Map', OP.TextPath, [OR].DisplayValue, OT.TextPath, '') as Name
					FROM	[Intersect] I
							left join Artifact SA on I.Subject = 'Artifact' and SA.ID = I.SubjectID
							left join Artifact OA on I.Object = 'Artifact' and OA.ID = I.ObjectID

							left join ReferenceItemType SD on I.Subject = 'ReferenceItemType' and SD.ID = I.SubjectID
							left join ReferenceItemType OD on I.Object = 'ReferenceItemType' and OD.ID = I.ObjectID

							left join [FusionAttribute] SF on I.Subject = 'FusionAttribute' and SF.ID = I.SubjectID
							left join [FusionAttribute] [OF] on I.Object = 'FusionAttribute' and [OF].ID = I.ObjectID


							left join [Intersect] SI on I.Subject = 'Intersect' and SI.ID = I.SubjectID

							left join [Map] SM on I.Subject = 'Map' and SM.ID = I.SubjectID
							left join [Map] OM on I.Object = 'Map' and OM.ID = I.ObjectID

							left join [Policy] SP on I.Subject = 'Policy' and SP.ID = I.SubjectID
							left join [Policy] OP on I.Object = 'Policy' and OP.ID = I.ObjectID

							left join [Rule] SR on I.Subject = 'Rule' and SR.ID = I.SubjectID
							left join [Rule] [OR] on I.Object = 'Rule' and [OR].ID = I.ObjectID

							left join [Taxonomy] ST on I.Subject = 'Taxonomy' and ST.ID = I.SubjectID
							left join [Taxonomy] OT on I.Object = 'Taxonomy' and OT.ID = I.ObjectID

					WHERE	I.ID = @id					
)
GO

create FUNCTION utility.ObjectFields
(	
	-- Add the parameters for the function here
	@Object varchar(50),
	@ObjectID int
)
RETURNS TABLE 
AS
RETURN 
(
	SELECT	FT.Name as 'Field',
				F.FormattedValue as 'Value'
		FROM	Field F
				inner join FieldType FT on FT.ID = F.FieldTypeID and F.ObjectType = @Object and F.ObjectID = @ObjectID
)
GO

CREATE FUNCTION [lineage].[GetTrailForObject]
(	
	@Object varchar(50), 
	@ObjectID int,
	@Forward bit
)
RETURNS @tbl TABLE
(
	IntersectID int, 
	IntersectTypeID int, 
	[Subject] varchar(50), 
	SubjectID int, 
	[Object] varchar(50), 
	ObjectID int, 
	[State] int, 
	PredicateID int, 
	PredicateName varchar(max), 
	PredicateInverse varchar(max), 
	PredicateType int, 
	Visited bit
)
AS
BEGIN


	--TESTING---------------------
	--declare @tbl table (IntersectID int, IntersectTypeID int, Subject varchar(50), SubjectID int, Object varchar(50), ObjectID int, State int, PredicateID int, PredicateName varchar(max), PredicateInverse varchar(max), PredicateType int, Visited bit);
	--declare @Object varchar(50);
	--declare @ObjectID int;
	--declare @Forward bit;

	--select @Object = 'Artifact',
	--	   @ObjectID = 973683,
	--	   @Forward = 1;
	-------------------------------


	insert into @tbl
	select 
		P.*,
		0 as Visited 
	from PredicateIntersect P
	where 
		((@Forward = 1 and [Subject] = @Object and SubjectID = @ObjectID) OR
		(@Forward = 0 and [Object] = @Object and ObjectID = @ObjectID)) AND
		PredicateType = 1 AND P.[State] <> 3;
		

	declare @i int;
	select @i = count(*) from @tbl where Visited = 0;

	while @i != 0
	begin
		declare @intersectId int;
		select top 1 @intersectId = IntersectID from @tbl where Visited = 0; 

		update @tbl
		set Visited = 1
		where IntersectID = @intersectId;

		insert into @tbl
		select 
			P.*,
			0 as Visited 
		from PredicateIntersect P
		cross apply (select * from @tbl where IntersectID = @intersectId) I
		where 
			((@Forward = 1 and P.[Subject] = I.[Object] and P.SubjectID = I.ObjectID) OR
			(@Forward = 0 and P.[Object] = I.[Subject] and P.ObjectID = I.SubjectID)) AND
			P.PredicateType = 1 AND P.[State] <> 3 AND P.IntersectID not in (select IntersectID from @tbl);

		select @i = count(*) from @tbl where Visited = 0;
	end

	RETURN
END
GO

create function [dbo].[CheckIfObjectExistsWithParent]
(
	@ObjectType varchar(50), -- = 'ArtifactType'
	@ObjectTypeID int, -- = 1
	@ObjectID int, -- = 4651
	@Fields nvarchar(max), -- = '[{"id": 53072, "value":"Country Of Risk"}, {"id": 53096, "value":"Description for Country Of Risk"}]'	
	@ParentID int = -1
)
returns bit
as
begin
	declare @exists bit = 0;
	declare @numberOfKeyFields int = 0;
	declare @numberOfKeyMatches int = 0;
	declare @parentIntersectType int = 0;	
	declare @tbl table (ID int, Value nvarchar(max))

	insert into @tbl
		select	F.*
		from	openjson(@Fields) with (ID int 'strict $.ID', Value nvarchar(max) '$.Value') as F
				inner join FieldType T on T.ID = F.ID and T.Object = @ObjectType and T.ObjectID = @ObjectTypeID and T.IsPartOfKey = 1

	declare @results table (ID int, ObjectID int)

	-- do we only need to check items on the same level as the existing object?
	if (@ObjectType = 'PolicyType' or @ObjectType = 'TaxonomyType')
	begin
		select @parentIntersectType = IT.id 
			from 
				IntersectType IT
				inner join [Predicate] P on (IT.PredicateID = P.ID)
			where 
				[subject] = @ObjectType and [object] = @ObjectType and [subjectid] = @ObjectTypeID and [ObjectId] = @ObjectTypeID  and P.[Type] = 4;

		if ( @ParentID is null or @ParentID <=0 ) and @ObjectID is not null
		begin					
			select @ParentId = [subjectid] 
			from 
				[Intersect] I 
			where 
				I.IntersectTypeId = @parentIntersectType and I.ObjectID = @ObjectID;
		end;

			-- if it doesnt have a parent only consider top level items
			if ( @ParentId is not null and @ParentId > 0)
			begin
				if @ObjectID is not null -- edit existing item not top level
				begin
					insert into @results
						select	T.ID,
								F.ObjectID 
						from	@tbl T										
								inner join Field F on F.FieldTypeID = T.ID and F.Value = T.Value and (F.ObjectID <> @ObjectID)
								inner join [Intersect] I on (I.IntersectTypeID = @parentIntersectType and I.ObjectID = F.ObjectID and I.SubjectID = @ParentId)
				end
				else-- new item item not top level
				begin
					insert into @results
						select	T.ID,
								F.ObjectID 
						from	@tbl T										
								inner join Field F on F.FieldTypeID = T.ID and F.Value = T.Value
								inner join [Intersect] I on (I.IntersectTypeID = @parentIntersectType and I.ObjectID = F.ObjectID and I.SubjectID = @ParentId)
				end
			end
			else
			begin
				if @ObjectID is not null -- edit existing item top level
				begin
					insert into @results
						select	T.ID,
								F.ObjectID 
						from	@tbl T										
								inner join Field F on F.FieldTypeID = T.ID and F.Value = T.Value and (F.ObjectID <> @ObjectID)
						where 
							not exists (select 1 from [Intersect] I where (I.IntersectTypeID = @parentIntersectType and I.ObjectID = F.ObjectID)	)					
				end
				else
				begin -- new item item top level
					insert into @results
						select	T.ID,
								F.ObjectID 
						from	@tbl T										
								inner join Field F on F.FieldTypeID = T.ID and F.Value = T.Value
						where 
							not exists (select 1 from [Intersect] I where (I.IntersectTypeID = @parentIntersectType and I.ObjectID = F.ObjectID)	)					
				end
			end				
	end
	else
	begin
		insert into @results
			select	T.ID,
					F.ObjectID 
			from	@tbl T
					left join Field F on F.FieldTypeID = T.ID and F.Value = T.Value and ( (@ObjectID is null) OR (@ObjectID is not null and F.ObjectID <> @ObjectID) )
	end

	if exists(select 1 from @results)
		begin
			if exists(select 1 from @results where ObjectID is null)
				begin
					set	@exists = 0
				end
			else
				begin
					-- need to check if there are multiple keys does the same object have all? so check that the count of key fields in tbl matches the count in results for that object
					select @numberOfKeyMatches = a.maxcount from (select top 1 objectid, count(1) as maxcount from @results group by objectid order by 2 desc) a ;
					select @numberOfKeyFields = count(1) from @tbl

					if (@numberOfKeyMatches = @numberOfKeyFields)
					begin
						set @exists = 1
					end
					else
					begin
						set @exists = 0
					end
				end
		end
	else
		begin
			set @exists = 0
		end

	return @exists
end
GO

CREATE function [dbo].[CheckIfObjectExists]
(
	@ObjectType varchar(50), -- = 'ArtifactType'
	@ObjectTypeID int, -- = 1
	@ObjectID int, -- = 4651
	@Fields nvarchar(max) -- = '[{"id": 53072, "value":"Country Of Risk"}, {"id": 53096, "value":"Description for Country Of Risk"}]'
)
returns bit
as
begin
	declare @result bit = 0;
	select @result = [dbo].[CheckIfObjectExistsWithParent] (@ObjectType, @ObjectTypeID, @ObjectID, @Fields, default)

	return @result;
end
GO

CREATE FUNCTION GetAssetLevelScalar
(
	-- Add the parameters for the function here
	@assetId bigint,
	@predicateType int
)
RETURNS int
AS
BEGIN
	-- Declare the return variable here
	DECLARE @level int = 1;
	Declare @objecttype varchar(20);
	Declare @objectId int;

	select @objecttype = [object], @objectId = objectID from Asset where ID = @assetID;
	
	WHILE @@ROWCOUNT != 0 BEGIN

		select top 1
			@objectType = I.Subject,
			@objectId = I.SubjectID,
			@level = @level+1
		from	[intersect] I 
			inner join [intersecttype] IT on I.IntersectTypeID = IT.ID
			inner join [predicate] P on IT.PredicateID = P.ID
		where
			I.Object = @objecttype and I.ObjectID = @objectId and P.[Type]= @predicateType		
	END

	-- Return the result of the function
	RETURN @level

END
GO

CREATE FUNCTION [dbo].[GetParentObjectId]
(
	@type varchar(50),
	@id int
)
RETURNS int
AS
BEGIN
	return
	(
	select I.SubjectID as ParentID from Asset A
	inner join AssetType ST on ST.ID = A.AssetTypeID
	inner join [IntersectType] T on T.Object = ST.Object and T.ObjectID = ST.ObjectID
	inner join [Predicate] P on P.ID = T.PredicateID and P.[Type] = 3
	inner join [Intersect] I on I.Object = @type and I.ObjectID = @id and I.IntersectTypeID = T.ID
	where A.Object = @type and A.ObjectID = @id
	)
END
GO

CREATE FUNCTION GetWorkflowConditionLabels
(
	@conditions xml
)
RETURNS xml
AS
BEGIN
	declare @recordCount int;

	declare @results table (id int, FieldTypeID int, ValueType varchar(max), [Value] nvarchar(max), Operator varchar(max), VersionStepID int, FormInputID varchar(max), ValueLabel varchar(max));

	select 
		 @recordCount = count(*)
	from 
		@conditions.nodes('/Conditions/Condition') c(x);

		insert into @results (id, FieldTypeID, VersionStepID, FormInputID, ValueType, [Value], Operator, ValueLabel)
			select
			row_number() over (order by x.value('@FieldTypeID', 'int'), x.value('@VersionStepID', 'int'), x.value('@FormInputID', 'varchar(max)')) as id,
			 x.value('@FieldTypeID', 'int') as FieldTypeID
			,x.value('@VersionStepID', 'int') as VersionStepID  
			,x.value('@FormInputID', 'varchar(max)') as FormInputID
			,x.value('@ValueType', 'varchar(max)') as ValueType  
			,x.value('@Value', 'varchar(max)') as [Value]  
			,x.value('@Operator', 'varchar(max)') as [Operator] 
			,null as ValueLabel
		from 
			@conditions.nodes('/Conditions/Condition') c(x)
		left join FieldType FT on FT.ID = x.value('@FieldTypeID', 'int')
		left join workflow.VersionStep VS on VS.ID = x.value('@VersionStepID', 'int')

		
	while(@recordCount > 0)
	begin
		if (select top 1 ValueType from @results where id = @recordCount) in ('U', 'L')
		begin
		
			if ((select FieldTypeID from @results where id = @recordCount) is not null)
			begin
				declare @valueLabel varchar(max);

				select @valueLabel = coalesce(RI.DisplayValue, R.[Value])
				from 
					FieldType FT
				inner join @results R on R.id = @recordCount and FT.ID = R.FieldTypeID
				left join LookupType LT on FT.LookupObjectType = 'Lookup' and LT.ID = FT.LookupObjectID
				left join [Lookup] L on L.ID = (select top 1 [Value] from @results where id = @recordCount)
				left join ReferenceItem RI on FT.LookupObjectType = 'ReferenceItem' and FT.LookupObjectID = RI.ReferenceItemTypeID and RI.ID = R.[Value]

				update r
				set r.ValueLabel = @valueLabel
				from @results r
				where r.id = @recordCount;

			end
			
			if ((select FormInputID from @results where id = @recordCount) is not null)
			begin
				declare @fields xml, @valueLabel2 varchar(max);

				select @fields = VS.fields from 
				workflow.VersionStep VS
				inner join @results R on R.id = @recordCount and VS.ID = R.VersionStepID;


				select 
					@valueLabel2 = coalesce(RI.DisplayValue, R.[Value])
				from @fields.nodes('fields/form/field') f(x)
				inner join @results R on R.id = @recordCount
				inner join FieldType FT on FT.ID = x.value('@referenceFieldId', 'int')
				left join LookupType LT on FT.LookupObjectType = 'Lookup' and LT.ID = FT.LookupObjectID
				left join [Lookup] L on L.ID = (select top 1 [Value] from @results where id = @recordCount)
				left join ReferenceItem RI on FT.LookupObjectType = 'ReferenceItem' and FT.LookupObjectID = RI.ReferenceItemTypeID and RI.ID = R.[Value]
				where x.value('@id', 'varchar(max)') = R.FormInputID;


				update r
				set r.ValueLabel = @valueLabel2
				from @results r
				where r.id = @recordCount;


			end
		end	
		else
		begin
			update r
			set r.ValueLabel = r.[Value]
			from @results r
			where r.id = @recordCount;
		end


		set @recordCount = @recordCount - 1;
	end

	RETURN 
		coalesce(
		 (select 
			r.FieldTypeID as 'Condition/@FieldTypeID',
			r.VersionStepID as 'Condition/@VersionStepID',
			r.FormInputID as 'Condition/@FormInputID',
			r.ValueType as 'Condition/@ValueType',
			r.[Value] as 'Condition/@Value',
			r.Operator as 'Condition/@Operator',
			r.ValueLabel as 'Condition/@ValueLabel' 
		from @results r
		for xml path(''), root('Conditions'))
		,
		'<Conditions />');
END
GO

CREATE FUNCTION [dbo].[GetWorkflowResponsibleUsers]
(
	@itemStepId int,
	@firstResponse bit
)
RETURNS varchar(max)
AS
BEGIN
RETURN (
	
	select coalesce(string_agg(X.ResponsibleUsers, ', '), '[unknown]') as ResponsibleUsers from
	(
		select distinct
			case when @firstResponse = 1 then
					GR.FirstName + ' ' + GR.LastName
			else
				coalesce(
					GR2.FirstName + ' ' + GR2.LastName,
					GR.FirstName + ' ' + GR.LastName, 
					NULL)
			end as ResponsibleUsers
		from	workflow.ItemStep IST
		left join workflow.Item I on I.ID = IST.ItemID
		left join workflow.ItemAssignment IA on IA.ItemID = I.ID	
		left join reporting.Global_resource GR on GR.ResourceID = IST.CompletedBy
		left join reporting.Global_resource GR2 on GR2.ResourceID = IA.ResourceObjectID
		where
			IST.ID = @itemStepId
		group by GR.FirstName, GR.LastName, GR2.FirstName, GR2.LastName, IST.ID ,IST.ItemID, IST.StepID, IA.ID
	) X		
)
END
GO

CREATE FUNCTION [dbo].[OrganizationAccepted]
(
    @OrganizationID int
)
RETURNS bit
AS
BEGIN

declare @accepted bit;
set @accepted = 1;

	select 
		@accepted = 
		case when count(*) > 0 then
			0
		else
			1
		end
	from (
		select 
			C.ID as ContractID, 
			case when H.ID is null then
				0
			else
				1
			end as Accepted
		from [Contract] C
		inner join Organization O on O.ID = C.OrganizationID and O.ID = 1
		inner join reporting.Global_resource R on R.Email = O.AdministratorEmail
		left join ContractAcceptanceHistory H on H.ContractID = C.ID and H.ResourceID = R.ResourceID and H.OrganizationID = O.ID 
			and H.AcceptedOn >= C.PublishedOn
		where 
			C.[State] = 1 and C.ContractType = 1 and C.PublishedOn is not null

		union all

		select 
			C.ID as ContractID, 
			case when H.ID is null then
				0
			else
				1
			end as Accepted
		from [Contract] C
		inner join reporting.Global_resource R on R.Email = (select AdministratorEmail from Organization where ID = @OrganizationID)
		left join ContractAcceptanceHistory H on H.ContractID = C.ID and H.ResourceID = R.ResourceID and H.OrganizationID is null 
			and H.AcceptedOn >= C.PublishedOn
		where 
			C.[State] = 1 and C.ContractType = 1 and C.PublishedOn is not null and C.OrganizationID is null
		) X
	where 
		X.Accepted = 0

	return (@accepted);

END
GO

CREATE FUNCTION [dbo].[StripHTML] (@HTMLText NVARCHAR(MAX))
RETURNS NVARCHAR(MAX) AS
BEGIN
    DECLARE @Start INT
    DECLARE @End INT
    DECLARE @Length INT
    SET @Start = CHARINDEX('<',@HTMLText)
    SET @End = CHARINDEX('>',@HTMLText,CHARINDEX('<',@HTMLText))
    SET @Length = (@End - @Start) + 1
    WHILE @Start > 0 AND @End > 0 AND @Length > 0
    BEGIN
        SET @HTMLText = STUFF(@HTMLText,@Start,@Length,'')
        SET @Start = CHARINDEX('<',@HTMLText)
        SET @End = CHARINDEX('>',@HTMLText,CHARINDEX('<',@HTMLText))
        SET @Length = (@End - @Start) + 1
    END
    RETURN LTRIM(RTRIM(@HTMLText))
END
GO

create FUNCTION [utility].[GenerateFormattedMultipleValue]
	-- Add the parameters for the stored procedure here	
	(@DisplayFormat nvarchar(250),
	@LookupObjectType varchar(25),
	@LookupObjectID int,
	@Value nvarchar(max))	
RETURNS nvarchar(max)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	
	declare @currentValue nvarchar(1000);	
	declare @FormattedValue nvarchar(max);

	--print 'Display Format is :' + @DisplayFormat;

	set @FormattedValue = '';

	-- split the values
	declare cursor1 cursor read_only for SELECT value FROM STRING_SPLIT(@Value, ',') WHERE RTRIM(value) <> '';  

	open cursor1

	fetch next from cursor1 into @currentValue;
	
	while @@fetch_status = 0
	begin
		--print @currentValue

		if @FormattedValue != ''
		begin
			set @FormattedValue = @FormattedValue + ',';
		end
		
		set @FormattedValue = @FormattedValue + utility.GetFormattedFieldLookupValueWithMultiple('Lookup', @DisplayFormat, @LookupObjectType, @LookupObjectID, @currentValue,0);

		fetch next from cursor1 into @currentValue
	end

	close cursor1

	deallocate cursor1

	--print @FormattedValue

	return @FormattedValue
	
END
GO

CREATE FUNCTION [utility].[GetAssetDisplayValue]
(
	@ID bigint
)
RETURNS nvarchar(max)
AS
BEGIN
	declare @formattedValue nvarchar(max)

	select	@formattedValue = DisplayValue
	from	dbo.GetAssetDisplayValue()
	where	ID = @ID
	--declare @tokens table(ID int identity(1,1), Token nvarchar(100), Field nvarchar(100))
	--declare @fieldValues table(Field nvarchar(100), Value nvarchar(max))
	--declare @displayFormat nvarchar(250),
	--		@Object varchar(50),
	--		@ObjectID int,
	--		@ObjectType varchar(50),
	--		@ObjectTypeID int

	--select	@displayFormat = DisplayFormat,
	--		@Object = A.Object,
	--		@ObjectID = A.ObjectID,
	--		@ObjectType = T.Object,
	--		@ObjectTypeID = T.ObjectID
	--from	Asset A
	--		inner join AssetType T on T.ID = A.AssetTypeID and A.ID = @ID;

	--if @Object = 'ReferenceItem'
	--begin
	--	insert into @fieldValues
	--		SELECT 'Code',
	--				Code
	--		FROM	ReferenceItem
	--		WHERE	ID = @ObjectID
	--end

	--set @formattedValue = @displayFormat

	--while patindex('%{%',@formattedValue) > 0
	--begin
	--	declare @txt nvarchar(100) = SUBSTRING(@formattedValue, patindex('%{%',@formattedValue), PATINDEX('%}%', @formattedValue))
	--	insert into @tokens Values (@txt, REPLACE(REPLACE(@txt,'{',''),'}',''))
	--	set @formattedValue = replace(@formattedValue, @txt, '')
	--end

	--insert into @fieldValues
	--	SELECT	FT.Name,
	--			F.FormattedValue
	--	FROM	Field F
	--			inner join FieldType FT on FT.ID = F.FieldTypeID and F.ObjectType = @Object and F.ObjectID = @ObjectID
	
	--declare @current int,
	--		@max int

	--set @current = 1
	--select @max = Max(ID) from @tokens
	--set @formattedValue = @displayFormat

	--while(@current <= @max)
	--begin
	--	declare @currentToken nvarchar(100) = null,
	--			@currentField nvarchar(100) = null,
	--			@currentValue nvarchar(max) = null,
	--			@lkpType nvarchar(250) = null, 
	--			@lkpID int = null, 
	--			@lkpFormat nvarchar(250) = null

	--	select	@currentField = Field, 
	--			@currentToken = Token 
	--	from	@tokens
	--	where	ID = @current

	--	select	@currentValue = Value
	--	from	@fieldValues 
	--	where	Field = @currentField

	--	if @currentValue is not null
	--	begin
	--		SET @formattedValue = REPLACE(@formattedValue, @currentToken, @currentValue)
	--	end
	--	else
	--	begin
	--		SET @formattedValue = REPLACE(@formattedValue, @currentToken, '')
	--	end

	--	SET @current = @current + 1
	--end

	return @formattedValue
END
GO

CREATE FUNCTION [utility].[GetAssetDisplayValueWrapper]
(
	@ID bigint
)
RETURNS nvarchar(max)
AS
BEGIN
	return utility.GetAssetDisplayValue(@ID)
END
GO

CREATE FUNCTION [utility].[GetAssetHash]
(
--declare
	@ID bigint,-- = 733,
	@KeyFieldOnly bit-- = 1	
)
RETURNS varchar(50)
AS
BEGIN
	declare @hash varchar(50)

	select		@hash = CONVERT(
					varchar(32), 
					SUBSTRING(HASHBYTES('SHA1', STRING_AGG(cast(FieldTypeID as nvarchar) + ':' + Value, char(59))), 3, 32), 
					2)
	from		(
				select		top 100 percent
							F.FieldTypeID,
							coalesce(F.Value, '') as Value
				from		Asset A
							inner join AssetType T on T.ID = A.AssetTypeID and A.ID = @ID
							inner join Field F on F.ObjectType = A.Object and F.ObjectID = A.ObjectID
							inner join FieldType FT on FT.ID = F.FieldTypeID 
													and FT.Object = T.Object and FT.ObjectID = T.ObjectID
													and ( (@KeyFieldOnly = 1 and FT.IsPartOfKey = @KeyFieldOnly) or (@KeyFieldOnly = 0 and 1=1) )
				order by	FT.ID
				) A

	return @hash
END
GO

CREATE FUNCTION [utility].[GetAssetHashWrapper]
(
--declare
	@ID bigint,-- = 733,
	@KeyFieldOnly bit-- = 1	
)
RETURNS varchar(50)
AS
BEGIN
	return utility.GetAssetHash(@ID, @KeyFieldOnly)
END
GO

CREATE FUNCTION [utility].[GetFormattedFieldLookupValueWithMultiple]
(
	@Type varchar(25),
	@DisplayFormat nvarchar(250),
	@LookupObjectType varchar(25),
	@LookupObjectID int,
	@Value nvarchar(max),
	@SupportsMultipleValues bit	
)
RETURNS nvarchar(max)
AS
BEGIN
	declare @formattedValue nvarchar(max)
	
	if @Value is null
	begin
		return null
	end

	if @LookupObjectType is null
	begin
		set @formattedValue  = @Value

		if @Type = 'Link' OR @Type = 'UncLink'
		begin
			declare @linkName nvarchar(max),
					@linkUrl nvarchar(max)

			if charindex('|', @Value, 1) > 1
				begin
					SELECT @linkName = SUBSTRING(@Value, 1, PATINDEX('%|%', @Value)-1)
					SELECT @linkUrl = SUBSTRING(@Value, PATINDEX('%|%', @Value)+1, LEN(@Value))

					set @formattedValue = '<a href="' + @linkUrl + '" target="_blank">' + @linkName + '</a>'
				end
			else
				begin
					if @Value <> '' AND @Value <> '|' AND @Value IS NOT NULL
						begin
							if LEFT(@Value, 1) = '|'
								begin
									--no name, default to url
									set @formattedValue = '<a href="' + SUBSTRING(@Value,2, LEN(@Value)) + '" target="_blank">' + SUBSTRING(@Value,2, LEN(@Value)) + '</a>'
								end
							else
								begin
									set @formattedValue = '<a href="' + @Value + '" target="_blank">' + @Value + '</a>'
								end
						end
					else
						begin
							set @formattedValue = null
						end
				end
		end

	end	
	else
	begin
		if @SupportsMultipleValues = 1
		begin	
			set @formattedValue =  utility.GenerateFormattedMultipleValue (@DisplayFormat, @LookupObjectType, @LookupObjectID, @Value)
		end
		else if @LookupObjectType = 'ReferenceItemType'
		begin
			select @formattedValue = Name from ReferenceItemType where id = @Value;		
		end
		else
		begin
			declare @tokens table(ID int identity(1,1), Token nvarchar(100), Field nvarchar(100))
			declare @fieldValues table(Field nvarchar(100), Value nvarchar(max), LookupObjectType nvarchar(250), LookupObjectID int, LookupDisplayFormat nvarchar(250))

			set @formattedValue = @DisplayFormat
	
			while patindex('%{%',@formattedValue) > 0
			 begin
				declare @txt nvarchar(100) = SUBSTRING(@formattedValue, patindex('%{%',@formattedValue), PATINDEX('%}%', @formattedValue))
				insert into @tokens Values (@txt, REPLACE(REPLACE(@txt,'{',''),'}',''))
				set @formattedValue = replace(@formattedValue, @txt, '')
			end

			insert into @fieldValues
				select	distinct
						V.Name,
						V.Value,
						V.LookupObjectType,
						V.LookupObjectID,
						V.LookupDisplayFormat
				from	(
						SELECT	ID,
								Name,
								'Artifact' as ObjectType
						FROM	ArtifactType
						WHERE	@LookupObjectType = 'Artifact' and ID = @LookupObjectID
						UNION
						SELECT	ID,
								Name,
								'Lookup' as ObjectType
						FROM	[LookupType]
						WHERE	@LookupObjectType = 'Lookup' and ID = @LookupObjectID
						UNION
						SELECT	ID,
								Name,
								'ReferenceItem' as ObjectType
						FROM	[ReferenceItemType]
						WHERE	@LookupObjectType = 'ReferenceItem' and ID = @LookupObjectID
						UNION										
						SELECT	1 as ID,
								'User' as Name,
								'Resource' as ObjectType
						WHERE	@LookupObjectType = 'Resource'-- and ID = @LookupObjectID
						UNION
						SELECT	ID,
								Name,
								'Taxonomy' as ObjectType
						FROM	TaxonomyType
						WHERE	@LookupObjectType = 'Taxonomy' and ID = @LookupObjectID
						) L
						outer apply (

									SELECT	IT.Name,
											[IF].Value,
											[IT].LookupObjectType,
											COALESCE([IT].LookupObjectID, 0) as LookupObjectID,
											[IT].LookupDisplayFormat
									FROM	Field [IF]
											inner join FieldType IT ON [IF].FieldTypeID = IT.ID 
																	and [IF].ObjectType = L.ObjectType
																	/*and [IF].ObjectID = case 
																							when dbo.IsInteger(@Value) = 1 then @Value
																							else 0
																						end*/
																	and [IF].ObjectID = case 
																							when TRY_CAST(@Value AS int) IS NULL  then 0 --not an int
																							else @Value -- int
																						end

								
									UNION

									SELECT	P.FieldName as Name,
											p.FieldValue as Value,
											NULL as LookupObjectType,
											NULL as LookupObjectID,
											NULL as LookupDisplayFormat
									FROM	(
											SELECT	ID as AID,
													CAST(ID as nvarchar(max)) as ID,
													CAST(DisplayValue as nvarchar(max)) as TextPath
											FROM	Artifact A
											WHERE	A.ID = CAST(@Value as int)
													and L.ObjectType = 'Artifact'
											) A
											unpivot	(
													FieldValue for FieldName in (ID, TextPath)
													) p

									UNION

									SELECT	P.FieldName as Name,
											p.FieldValue as Value,
											NULL as LookupObjectType,
											NULL as LookupObjectID,
											NULL as LookupDisplayFormat
									FROM	(
											SELECT	ID,
													CAST(TextPath as nvarchar(max)) as TextPath
											FROM	Taxonomy A
											WHERE	A.ID = CAST(@Value as int)
													and L.ObjectType = 'Taxonomy'
											) A
											unpivot	(
													FieldValue for FieldName in (TextPath)
													) p

									UNION

									SELECT	P.FieldName as Name,
											p.FieldValue as Value,
											NULL as LookupObjectType,
											NULL as LookupObjectID,
											NULL as LookupDisplayFormat
									FROM	(
											SELECT	ID,
													CAST(Code as nvarchar(max)) as Code
											FROM	ReferenceItem A
											WHERE	A.ID = @Value
													and L.ObjectType = 'ReferenceItem'
											) A
											unpivot	(
													FieldValue for FieldName in (Code)
													) p

									UNION

									SELECT	P.FieldName as Name,
											p.FieldValue as Value,
											NULL as LookupObjectType,
											NULL as LookupObjectID,
											NULL as LookupDisplayFormat
									FROM	(
											SELECT	ID,
													CAST(Name as nvarchar(max)) as Name,
													CAST(Description as nvarchar(max)) as Description
											FROM	ReferenceItemType A
											WHERE	A.ID = @Value
													and L.ObjectType = 'ReferenceItemType'
											) A
											unpivot	(
													FieldValue for FieldName in (Name, Description)
													) p

									UNION

									SELECT	P.FieldName as Name,
											p.FieldValue as Value,
											NULL as LookupObjectType,
											NULL as LookupObjectID,
											NULL as LookupDisplayFormat
									FROM	(
											SELECT	ResourceID as ID,
													CAST(FirstName as nvarchar(max)) as FirstName,
													CAST(LastName as nvarchar(max)) as LastName,
													CAST(Email as nvarchar(max)) as Email
											FROM	reporting.Global_Resource A
											WHERE	A.ResourceID = @Value
													and L.ObjectType = 'Resource'
											) A
											unpivot	(
													FieldValue for FieldName in (FirstName, LastName, Email)
													) p
									) V

			declare @current int,
					@max int

			set @current = 1
			select @max = Max(ID) from @tokens

			set @formattedValue = @DisplayFormat

			while(@current <= @max)
			begin
				declare @currentToken nvarchar(100) = null,
						@currentField nvarchar(100) = null,
						@currentValue nvarchar(max) = null,
						@lkpType nvarchar(250) = null, 
						@lkpID int = null, 
						@lkpFormat nvarchar(250) = null

				select	@currentField = Field, 
						@currentToken = Token 
				from	@tokens
				where	ID = @current

				select	@currentValue = Value,
						@lkpType = LookupObjectType,
						@lkpID = LookupObjectID,
						@lkpFormat = LookupDisplayFormat
				from	@fieldValues 
				where	Field = @currentField

				if @currentValue is not null
				begin
					if @lookupObjectType is not null and @lkpID is not null
					begin
						select @currentValue = utility.GetFormattedFieldLookupValueWithMultiple(@Type, @lkpFormat, @lkpType, @lkpID, @currentValue, @SupportsMultipleValues)
					end

					SET @formattedValue = REPLACE(@formattedValue, @currentToken, @currentValue)
				end
				else
				begin
					SET @formattedValue = REPLACE(@formattedValue, @currentToken, '')
				end

				SET @current = @current + 1
			end
		end
	end

	return @formattedValue
END
GO

--ALTER TABLE AttributeType add DisplayFormat nvarchar(250) null
--UPDATE AttributeType set DisplayFormat = TextFormatString

CREATE FUNCTION utility.GetObjectDisplayValue
(
	@Object varchar(50),
	@ObjectID int,
	@ObjectTypeID int	
)
RETURNS nvarchar(max)
AS
BEGIN
	declare @formattedValue nvarchar(max)
	declare @tokens table(ID int identity(1,1), Token nvarchar(100), Field nvarchar(100))
	declare @fieldValues table(Field nvarchar(100), Value nvarchar(max))
	declare @displayFormat nvarchar(250)

	if @Object = 'Artifact'
	begin
		set @displayFormat = (select DisplayFormat from [ArtifactType] where ID = @ObjectTypeID);
	end
	if @Object = 'Attribute'
	begin
		set @displayFormat = (select DisplayFormat from [AttributeType] where ID = @ObjectTypeID);
	end
	if @Object = 'FusionQueryAttribute'
	begin
		set @displayFormat = (select DisplayFormat from FusionQueryAttributeType where ID = @ObjectTypeID);
	end
	if @Object = 'Policy'
	begin
		set @displayFormat = (select DisplayFormat from [PolicyType] where ID = @ObjectTypeID);
	end
	if @Object = 'ReferenceItem'
	begin
		set @displayFormat = (select DisplayFormat from [ReferenceItemType] where ID = @ObjectTypeID);

		insert into @fieldValues
			SELECT 'Code',
					Code
			FROM	ReferenceItem
			WHERE	ID = @ObjectID
	end
	if @Object = 'Rule'
	begin
		set @displayFormat = (select DisplayFormat from [RuleType] where ID = @ObjectTypeID);
	end
	if @Object = 'Taxonomy'
	begin
		set @displayFormat = (select DisplayFormat from [TaxonomyType] where ID = @ObjectTypeID);
	end

	set @formattedValue = @displayFormat

	while patindex('%{%',@formattedValue) > 0
	begin
		declare @txt nvarchar(100) = SUBSTRING(@formattedValue, patindex('%{%',@formattedValue), PATINDEX('%}%', @formattedValue))
		insert into @tokens Values (@txt, REPLACE(REPLACE(@txt,'{',''),'}',''))
		set @formattedValue = replace(@formattedValue, @txt, '')
	end

	insert into @fieldValues
		SELECT	FT.Name,
				F.FormattedValue
		FROM	Field F
				inner join FieldType FT on FT.ID = F.FieldTypeID and F.ObjectType = @Object and F.ObjectID = @ObjectID
	
	declare @current int,
			@max int

	set @current = 1
	select @max = Max(ID) from @tokens
	set @formattedValue = @displayFormat

	while(@current <= @max)
	begin
		declare @currentToken nvarchar(100) = null,
				@currentField nvarchar(100) = null,
				@currentValue nvarchar(max) = null,
				@lkpType nvarchar(250) = null, 
				@lkpID int = null, 
				@lkpFormat nvarchar(250) = null

		select	@currentField = Field, 
				@currentToken = Token 
		from	@tokens
		where	ID = @current

		select	@currentValue = Value
		from	@fieldValues 
		where	Field = @currentField

		if @currentValue is not null
		begin
			SET @formattedValue = REPLACE(@formattedValue, @currentToken, @currentValue)
		end
		else
		begin
			SET @formattedValue = REPLACE(@formattedValue, @currentToken, '')
		end

		SET @current = @current + 1
	end

	return @formattedValue
END
GO

CREATE TYPE [utility].[FieldValue] AS TABLE (
    [Field] NVARCHAR (250) NOT NULL,
    [Value] NVARCHAR (MAX) NOT NULL);
GO

CREATE FUNCTION [utility].[GetObjectDisplayValueDeterministic]
(
	@Object varchar(50),
	@ObjectID int,
	@ObjectTypeID int, 
	@displayFormat nvarchar(250),
	@fieldValues utility.FieldValue readonly	
)
RETURNS nvarchar(max)
WITH SCHEMABINDING
AS
BEGIN
	declare @formattedValue nvarchar(max)
	declare @tokens table(ID int identity(1,1), Token nvarchar(100), Field nvarchar(100))
	--declare @fieldValues table(Field nvarchar(100), Value nvarchar(max))
	/*declare @displayFormat nvarchar(250)

	if @Object = 'Artifact'
	begin
		set @displayFormat = (select DisplayFormat from [ArtifactType] where ID = @ObjectTypeID);
	end
	if @Object = 'Attribute'
	begin
		set @displayFormat = (select DisplayFormat from [AttributeType] where ID = @ObjectTypeID);
	end
	if @Object = 'FusionQueryAttribute'
	begin
		set @displayFormat = (select DisplayFormat from FusionQueryAttributeType where ID = @ObjectTypeID);
	end
	if @Object = 'Policy'
	begin
		set @displayFormat = (select DisplayFormat from [PolicyType] where ID = @ObjectTypeID);
	end
	if @Object = 'ReferenceItem'
	begin
		set @displayFormat = (select DisplayFormat from [ReferenceItemType] where ID = @ObjectTypeID);

		insert into @fieldValues
			SELECT 'Code',
					Code
			FROM	ReferenceItem
			WHERE	ID = @ObjectID
	end
	if @Object = 'Rule'
	begin
		set @displayFormat = (select DisplayFormat from [RuleType] where ID = @ObjectTypeID);
	end
	if @Object = 'Taxonomy'
	begin
		set @displayFormat = (select DisplayFormat from [TaxonomyType] where ID = @ObjectTypeID);
	end
	*/
	set @formattedValue = @displayFormat

	while patindex('%{%',@formattedValue) > 0
	begin
		declare @txt nvarchar(100) = SUBSTRING(@formattedValue, patindex('%{%',@formattedValue), PATINDEX('%}%', @formattedValue))
		insert into @tokens Values (@txt, REPLACE(REPLACE(@txt,'{',''),'}',''))
		set @formattedValue = replace(@formattedValue, @txt, '')
	end

	/*insert into @fieldValues
		SELECT	FT.Name,
				F.FormattedValue
		FROM	Field F
				inner join FieldType FT on FT.ID = F.FieldTypeID and F.ObjectType = @Object and F.ObjectID = @ObjectID
	*/
	declare @current int,
			@max int

	set @current = 1
	select @max = Max(ID) from @tokens
	set @formattedValue = @displayFormat

	while(@current <= @max)
	begin
		declare @currentToken nvarchar(100) = null,
				@currentField nvarchar(100) = null,
				@currentValue nvarchar(max) = null,
				@lkpType nvarchar(250) = null, 
				@lkpID int = null, 
				@lkpFormat nvarchar(250) = null

		select	@currentField = Field, 
				@currentToken = Token 
		from	@tokens
		where	ID = @current

		select	@currentValue = Value
		from	@fieldValues 
		where	Field = @currentField

		if @currentValue is not null
		begin
			SET @formattedValue = REPLACE(@formattedValue, @currentToken, @currentValue)
		end
		else
		begin
			SET @formattedValue = REPLACE(@formattedValue, @currentToken, '')
		end

		SET @current = @current + 1
	end

	return @formattedValue
END
GO

CREATE FUNCTION utility.GetObjectDisplayValueWrapper
(
	@Object varchar(50),
	@ObjectID int,
	@ObjectTypeID int	
)
RETURNS nvarchar(max)
AS
BEGIN
	RETURN utility.GetObjectDisplayValue(@Object, @ObjectID, @ObjectTypeID)
END
GO

CREATE FUNCTION [utility].[GetObjectHash]
(
--declare
	@Object varchar(50),-- = 'Artifact',
	@ObjectID int,-- = 733,
	@ObjectTypeID int,-- = 2,
	@KeyFieldOnly bit-- = 1	
)
RETURNS varchar(50)
AS
BEGIN
	declare @hash varchar(50)

	select		@hash = CONVERT(
					varchar(32), 
					SUBSTRING(HASHBYTES('SHA1', STRING_AGG(cast(FieldTypeID as nvarchar) + ':' + Value, char(59))), 3, 32), 
					2)
	from		(
				select		top 100 percent
							F.FieldTypeID,
							coalesce(F.Value, '') as Value
				from		Field F
							inner join FieldType FT on FT.ID = F.FieldTypeID 
													and F.ObjectType = @Object and F.ObjectID = @ObjectID 
													and FT.Object = @Object + 'Type' and FT.ObjectID = @ObjectTypeID
													and ( (@KeyFieldOnly = 1 and FT.IsPartOfKey = @KeyFieldOnly) or (@KeyFieldOnly = 0 and 1=1) )
				order by	FT.ID
				) A

	return @hash
END
GO

CREATE FUNCTION [utility].[GetObjectHashWrapper]
(
	@Object varchar(50),
	@ObjectID int,
	@ObjectTypeID int,
	@KeyFieldOnly bit
)
RETURNS varchar(50)
AS
BEGIN
	return utility.GetObjectHash(@Object, @ObjectID, @ObjectTypeID, @KeyFieldOnly)
END
GO

CREATE PROCEDURE [workflow].[changeItemState]
	@workflowID int,
	@workflowStepID int = 0,
	@workflowItemID int = 0
AS
BEGIN
	declare @xmlSettings xml;
	declare @stateValue int;
	declare @objectType varchar(20);
	declare @objectId int;

	SET NOCOUNT ON;

	select @xmlSettings = settings from [workflow].[VersionStep] where id = @workflowStepID
    select @stateValue = T.C.value('.','int') from @xmlSettings.nodes('(/settings/State)') as T(C);

	--get the 

	select @objectType = object, @objectId = objectid from [workflow].[item] where id = @workflowItemID;

	if @objectType = 'Intersect'
	begin
		update [dbo].[intersect] set [state] = @stateValue where id = @objectId;
	end

END
GO

CREATE procedure [lineage].[GetByObject]
--declare
	@Object varchar(50),-- = 'Artifact',
	@ObjectID int-- = 1101
as
begin
	--Hold the raw lineage records.
	declare @tbl table (IntersectID int, IntersectTypeID int, 
						Subject varchar(50), SubjectID int, Object varchar(50), ObjectID int, [State] int, 
						PredicateID int, PredicateName nvarchar(250), PredicateInverse nvarchar(250), PredicateType int, 
						IntersectGroupID int null
						)

	-- Get the direct lineage going backward from the provided object.
	insert into @tbl
		select	L.IntersectID,
				L.IntersectTypeID,
				L.[Subject],
				L.SubjectID,
				L.[Object],
				L.ObjectID,
				L.[State],
				L.PredicateID,
				L.PredicateName,
				L.PredicateInverse,
				L.PredicateType,
				G.IntersectGroupID 
		from	lineage.GetTrailForObject(@Object, @ObjectID, 0) L	
				left join IntersectGroupItem G on G.IntersectID = L.IntersectID

	-- Get the direct lineage going foreward from the provided object.
	insert into @tbl
		select	L.IntersectID,
				L.IntersectTypeID,
				L.[Subject],
				L.SubjectID,
				L.[Object],
				L.ObjectID,
				L.[State],
				L.PredicateID,
				L.PredicateName,
				L.PredicateInverse,
				L.PredicateType,
				G.IntersectGroupID 
		from	lineage.GetTrailForObject(@Object, @ObjectID, 1) L	
				left join IntersectGroupItem G on G.IntersectID = L.IntersectID

	-- Hold the intersect IDs that are part of an IntersectGroup from one of the retrieved intersects above.
	declare @groupIntersects table (IntersectGroupID int, IntersectID int)

	-- Get the intersects that are part of an IntersectGroup from one of intersects above, but not yet pulled back in the temp table (i.e. does not exist in the lineage)
	insert into @groupIntersects
		select	GI.IntersectGroupID,
				GI.IntersectID
		from	@tbl O
				inner join IntersectGroupItem GI on GI.IntersectGroupID = O.IntersectGroupID and GI.IntersectID not in (select IntersectID from @tbl)

	-- Get the intersect record itself, for each ID pulled back as part of the group query above.
	insert into @tbl
		select	P.IntersectID,
				P.IntersectTypeID,
				P.[Subject],
				P.SubjectID,
				P.[Object],
				P.ObjectID,
				P.[State],
				P.PredicateID,
				P.PredicateName,
				P.PredicateInverse,
				P.PredicateType,
				G.IntersectGroupID
		from	PredicateIntersect P
				inner join @groupIntersects G on G.IntersectID = P.IntersectID

	-- Go back for each group intersectID retrieved above and get backward-facing lineage, that is not already present in the lineage @tbl
	insert into @tbl
		select	Src.IntersectID,
				Src.IntersectTypeID,
				Src.[Subject],
				Src.SubjectID,
				Src.[Object],
				Src.ObjectID,
				Src.[State],
				Src.PredicateID,
				Src.PredicateName,
				Src.PredicateInverse,
				Src.PredicateType,
				null
		from	PredicateIntersect P
				inner join @groupIntersects G on G.IntersectID = P.IntersectID
				cross apply lineage.GetTrailForObject(P.Subject, P.SubjectID, 0) Src
		where	Src.IntersectID not in (select IntersectID from @tbl)


	-- Return the full results to the caller.
	select	distinct
			I.IntersectID,
			I.IntersectGroupID,
			T.IntersectTypeID,
			SA.ID as SubjectAssetID,
			I.Subject,
			I.SubjectID,
			SA.DisplayValue as SubjectName,
			SA.BackColor as SubjectBackColor,
			SA.ForeColor as SubjectForeColor,
			SA.TypeName as SubjectTypeName,
			SA.Type as SubjectType,
			SA.TypeID as SubjectTypeID,
			SA.AssetTypeID as SubjectAssetTypeID,

			OA.ID as ObjectAssetID,
			I.Object,
			I.ObjectID,
			OA.DisplayValue as ObjectName,
			OA.BackColor as ObjectBackColor,
			OA.ForeColor as ObjectForeColor,
			OA.TypeName as ObjectTypeName,
			OA.Type as ObjectType,
			OA.TypeID as ObjectTypeID,
			OA.AssetTypeID as ObjectAssetTypeID,

			I.[State],

			I.PredicateName as [Predicate]
	from	@tbl I
			inner join [Intersect] T on T.ID = I.IntersectID
			inner join AssetDetail SA on SA.Object = I.Subject and SA.ObjectID = I.SubjectID
			inner join AssetDetail OA on OA.Object = I.Object and OA.ObjectID = I.ObjectID
end
GO

CREATE procedure [fusion].[GenerateFoundationLineage]
as
begin
	--select	ST.FormattedValue as SourceFusionAttributeTypeID,
	--		S.TextPath as SourceName,
	--		TT.FormattedValue as TargetFusionAttributeTypeID,
	--		T.TextPath as TargetName,
	--		IT.ID
	--from	FusionAttribute MA
	--		inner join FieldWithRelation ST on ST.ObjectType = 'FusionAttribute' and ST.ObjectID = MA.ID and ST.Name = 'SourceTypeID'
	--		inner join FusionAttributeType S on S.ID = ST.FormattedValue
	--		inner join FieldWithRelation TT on TT.ObjectType = 'FusionAttribute' and TT.ObjectID = MA.ID and TT.Name = 'TargetTypeID'
	--		inner join FusionAttributeType T on T.ID = TT.FormattedValue
	--		left join IntersectType IT on IT.Subject = 'FusionAttributeType' and IT.SubjectID = ST.FormattedValue and IT.Object = 'FusionAttributeType' and IT.ObjectID = TT.FormattedValue
	--where	MA.FusionID = @fusionID and MA.FusionAttributeTypeID = 50032
	--group by ST.FormattedValue, S.TextPath, TT.FormattedValue, T.TextPath, IT.ID

	DROP TABLE IF EXISTS #Maps

	select		IT.ID as IntersectTypeID,
				'FusionAttribute' as Subject,
				--ST.FormattedValue as SourceFusionAttributeTypeID,
				--S.FormattedValue as SourceObjectID,
				SA.ID as SubjectID,
				--TT.FormattedValue as TargetFusionAttributeTypeID,
				--T.FormattedValue as TargetObjectID,
				'FusionAttribute' as Object,
				TA.ID as ObjectID
	into		#Maps
	from		FusionAttribute MA
				inner join FieldWithRelation ST on ST.ObjectType = 'FusionAttribute' and ST.ObjectID = MA.ID and ST.Name = 'SourceTypeID'
				inner join FieldWithRelation S on S.ObjectType = 'FusionAttribute' and S.ObjectID = MA.ID and S.Name = 'SourceObjectID'
				inner join FieldWithRelation TT on TT.ObjectType = 'FusionAttribute' and TT.ObjectID = MA.ID and TT.Name = 'TargetTypeID'
				inner join FieldWithRelation T on T.ObjectType = 'FusionAttribute' and T.ObjectID = MA.ID and T.Name = 'TargetObjectID'
				inner join FusionAttribute SA on SA.FusionID = MA.FusionID and SA.FusionAttributeTypeID = ST.FormattedValue and SA.SourceID = S.FormattedValue
				inner join FusionAttribute TA on TA.FusionID = MA.FusionID and TA.FusionAttributeTypeID = TT.FormattedValue and TA.SourceID = T.FormattedValue
				left join IntersectTypeDetail IT on IT.Subject = 'FusionAttributeType' and IT.SubjectID = ST.FormattedValue and IT.Object = 'FusionAttributeType' and IT.ObjectID = TT.FormattedValue and IT.PredicateType = 1
	where		MA.FusionAttributeTypeID = 1476 --and SA.ID <> TA.ID (slows down query quite a bit)
	group by	SA.ID, TA.ID, IT.ID

	DROP TABLE IF EXISTS #Types

	select	distinct
			IntersectTypeID 
	into	#Types
	from	#Maps 
	where	IntersectTypeID is not null

	delete	T
	from	[Intersect] T
			inner join #Types I on	I.IntersectTypeID = T.IntersectTypeID
			left join #Maps M on	M.IntersectTypeID = T.IntersectTypeID 
									and M.Subject = T.Subject and M.SubjectID = T.SubjectID 
									and M.Object = T.Object and M.ObjectID = T.ObjectID
	where	M.IntersectTypeID is null

	insert into [Intersect] (IntersectTypeID, Subject, SubjectID, Object, ObjectID, [Owner])
		select	M.IntersectTypeID, 
				M.Subject, M.SubjectID, 
				M.Object, M.ObjectID, 
				'FOUNDATION'
		from	#Maps M 
				left join [Intersect] T on	M.IntersectTypeID = T.IntersectTypeID 
											and M.Subject = T.Subject and M.SubjectID = T.SubjectID 
											and M.Object = T.Object and M.ObjectID = T.ObjectID
		where	M.IntersectTypeID is not null 
				and T.ID is null;

	--merge	[Intersect] T
	--using	(
	--		select	distinct
	--				* 
	--		from	#Maps 
	--		where	IntersectTypeID is not null
	--		) S
	--		on (T.IntersectTypeID = S.IntersectTypeID and T.Subject = S.Subject and T.Object = S.Object and T.SubjectID = S.SubjectID and T.ObjectID = S.ObjectID)
	----when	not matched by source and T.IntersectTypeID = S.IntersectTypeID
	----then	delete
	--when	not matched by target
	--then	INSERT (IntersectTypeID, Subject, SubjectID, Object, ObjectID, [Owner])
	--		VALUES (S.IntersectTypeID, S.Subject, S.SubjectID, S.Object, S.ObjectID, 'FOUNDATION');
end
GO

CREATE proc [dbo].[GetPageInformation]
--declare 
	@o varchar(50),-- = 'Artifact',
	@oid int,-- = 23450,
	@rid int --= 1
as
begin
	declare @breadcrumbsRaw table ([Level] int, [TypeName] nvarchar(500), [Name] nvarchar(max), [TypeUrl] nvarchar(2500), [Url] nvarchar(2500));
	declare @breadcrumbs table ([Name] nvarchar(max), [Url] nvarchar(2500), Active bit, IsType bit);

	with h as
		(
		select	A.ID,
				A.[ObjectID], 
				A.AssetTypeID,
				I.SubjectID as [ParentID], 
				0 as [Level]
		from	Asset A
				left join PredicateIntersect I on I.Object = A.Object and I.ObjectID = A.ObjectID and I.PredicateType = 3
		where	A.[Object] = @o and A.ObjectID = @oid
		union all
		select	P.ID,
				P.[ObjectID] as ID, 
				P.AssetTypeID,
				I.SubjectID as ParentID, 
				h.[Level]-1 as [Level]
		from	Asset P
				inner join h on P.[Object] = @o and P.ObjectID = h.ParentID
				outer apply (
							select	SubjectID
							from	PredicateIntersect 
							where	Object = P.Object 
									and ObjectID = P.ObjectID 
									and PredicateType = 3
							) I
		)

	insert into @breadcrumbsRaw
		select		distinct	
					[Level],
					ltrim(rtrim(T.Name)),
					ltrim(rtrim(D.DisplayValue)),
					UT.Url,
					U.Url
		from		h 
					inner join AssetType T on T.ID = h.AssetTypeID
					left join dbo.GetAssetDisplayValue() D on D.ID = h.ID
					cross apply dbo.GetAssetUrl(@o, T.ObjectID, h.ObjectID) U
					cross apply dbo.GetAssetUrl(T.Object, T.ObjectID, T.ObjectID) UT
		where		ltrim(rtrim(T.Name)) is not null
					and ltrim(rtrim(D.DisplayValue)) is not null
		order by	[Level]

	declare @max int = 0,
			@min int
	select	@min = min([Level]) from @breadcrumbsRaw

	insert into @breadcrumbs values ('Glossary', null, 0, 0)

	while @min <= @max
	begin
		insert into @breadcrumbs
			select	TypeName, TypeUrl, 0, 1 from @breadcrumbsRaw where [Level] = @min

		insert into @breadcrumbs
			select	Name, 
					Url, 
					case @min when 0 then 1 else 0 end, 
					0 
			from	@breadcrumbsRaw 
			where	[Level] = @min

		set @min = @min + 1
	end

	select	distinct
			A.ID,
			O.ID as AssetID,
			OD.DisplayValue,
			T.Name as [TypeName],
			case 
				when Dash.[Count] > 0 then cast(1 as bit)
				else cast(0 as bit)
			end as HasDashboards,
			case 
				when Work.[Count] > 0 then cast(1 as bit)
				else cast(0 as bit)
			end as HasWorkflow,
			case 
				when Child.[Count] > 0 then cast(1 as bit)
				else cast(0 as bit)
			end as HasChildArtifacts,
			case 
				when Attr.[Count] > 0 then cast(1 as bit)
				else cast(0 as bit)
			end as AllowAttributes,
			case 
				when Hier.[Count] > 0 then cast(1 as bit)
				else cast(0 as bit)
			end as AllowPredicateHierarchies,
			(
			select	*
			from	(
					select	P.ID as [ID],
							P.Name as [Name]
					from	[Predicate] P
					where	exists(SELECT * FROM IntersectType IT WHERE P.[type] = 6 and P.ID = IT.PredicateID and ((IT.Subject = T.Object and IT.SubjectID = T.ObjectID) OR (IT.Object = T.Object and IT.ObjectID =T.ObjectID)))
					union	
					select	P.ID as [ID], 
							P.Name as [Name] 
					from	[NymRelation] R 
							inner join [dbo].[predicate] P on P.ID = R.PredicateID where R.[Object] = T.Object and R.ObjectID = T.ObjectID
					) NMT
			for		json path
			)
			as NymTypes,
			(
			select	* 
			from	@breadcrumbs
			for		json path
			) as Breadcrumbs
	from	Artifact A 
			inner join Asset O on O.Object = @o and O.ObjectID = A.ID 
			inner join AssetType T on T.ID = O.AssetTypeID
			left join dbo.GetAssetDisplayValue() OD on OD.ID = O.ID
			--cross apply [dbo].GetAssetDisplayValueById(O.ID) as OD
			cross apply (
						select	count(1) as [Count]
						from	Report
						where	ObjectType = O.Object
								and ObjectID = T.ObjectID
						) Dash
			cross apply (
						select	count(1) as [Count]
						from	workflow.EventRegistration WER
								inner join workflow.Type WT on WER.TypeID = WT.ID and WT.PublishedVersionID is not null and WT.[State] = 1 and WER.ChangeType = 8 --ACTIVE
						where	WER.Object = T.Object
								and WER.ObjectID = T.ObjectID
						) Work
			cross apply (
						select	count(1) as [Count]
						from	[PredicateIntersect]
						where	Subject = O.Object
								and SubjectID = O.ObjectID
								and PredicateType = 3
						) Child
			cross apply (
						select	count(1) as [Count]
						from	AttributeTypeRelation
						where	ObjectType = T.Object and ObjectID = T.ObjectID
						) Attr
			cross apply (
						select	count(1) as [Count]
						from	IntersectType IT
								inner join [Predicate] P on P.ID = IT.PredicateID and P.[Type] = 3 -- TypeOf
						where	((IT.Subject = T.Object and IT.SubjectID = T.ObjectID) OR (IT.Object = T.Object and IT.ObjectID = T.ObjectID))
						) Hier
			left join AssetWithoutReadPermission RP on RP.ResourceID = @rid and RP.AssetID = O.ID 
	where   A.ID = @oid and A.[Visible] = 1 and RP.AssetID is null
	for json path, WITHOUT_ARRAY_WRAPPER
end
GO

create procedure GenerateAssetTypeSql
--declare	
		@type varchar(50),-- = 'ArtifactType',--'TaxonomyType',
		@id int,-- = 1,
		@pt int,-- = 3,--4,
		@showPassword bit = 0
as
begin
	set nocount on;
	declare @avoids table (Type varchar(250));
	insert into @avoids values ('File'), ('FusionLookup'), ('Attribute'), ('FilteredLookup'), ('ComplexRelationLookup'), ('DataTableSelect'), ('OwnershipLookup'), ('RefListRelationship');

	/*
	select	A.ID as AssetID,
			A.ObjectID as ID,
			P.ParentID,
			A.AssetTypeID,
			T.ObjectID as TypeID
	from	Asset A
			inner join AssetType T on T.ID = A.AssetTypeID and T.Object = @type and T.ObjectID = @id
			outer apply (
						select	I.SubjectID as ParentID
						from	[Intersect] I
								inner join IntersectType IT on IT.ID = I.IntersectTypeID 
								inner join [Predicate] P on P.ID = IT.PredicateID and P.Type = @pt
						where	I.Object = A.Object and I.ObjectID = A.ObjectID
						) P
	*/

	select	'left join Field F'+cast(ID as nvarchar)+' on F'+cast(ID as nvarchar)+'.FieldTypeID = ' + cast(ID as nvarchar) + '  and A.Object = F'+cast(ID as nvarchar)+'.ObjectType and A.ObjectID = F'+cast(ID as nvarchar)+'.ObjectID' + 
			case [Type]
				when 'Relationship' 
				then ' left join [Intersect] I'+cast(ID as nvarchar)+' on I'+cast(ID as nvarchar)+'.IntersectTypeID = F'+cast(ID as nvarchar)+'.LookupObjectID' +
										 ' A.Object = case I'+cast(ID as nvarchar)+'.Subject and A.ObjectID = I'+cast(ID as nvarchar)+'.SubjectID then I'+cast(ID as nvarchar)+'.Object = F'+cast(ID as nvarchar)+'.ObjectType else I'+cast(ID as nvarchar)+'.Subject = F'+cast(ID as nvarchar)+'.ObjectType end and' + 
										 ' A.Object = case I'+cast(ID as nvarchar)+'.Subject and A.ObjectID = I'+cast(ID as nvarchar)+'.SubjectID then I'+cast(ID as nvarchar)+'.ObjectID = F'+cast(ID as nvarchar)+'.ObjectID else I'+cast(ID as nvarchar)+'.SubjectID = F'+cast(ID as nvarchar)+'.ObjectID end and' + 
										 ' left join dbo.GetAssetDisplayValue() R'+cast(ID as nvarchar)+' on' + 
										 ' R'+cast(ID as nvarchar)+'.Object = case F'+cast(ID as nvarchar)+'.ObjectType = I'+cast(ID as nvarchar)+'.Subject and F'+cast(ID as nvarchar)+'.ObjectID = I'+cast(ID as nvarchar)+'.SubjectID then I'+cast(ID as nvarchar)+'.Object else I'+cast(ID as nvarchar)+'.Subject end and' + 
										 ' R'+cast(ID as nvarchar)+'.ObjectID = case F'+cast(ID as nvarchar)+'.ObjectType = I'+cast(ID as nvarchar)+'.Subject and F'+cast(ID as nvarchar)+'.ObjectID = I'+cast(ID as nvarchar)+'.SubjectID then I'+cast(ID as nvarchar)+'.ObjectID else I'+cast(ID as nvarchar)+'.SubjectID end'
				when 'FieldFromRelationship' 
				then ' left join [Intersect] I'+cast(ID as nvarchar)+' on I'+cast(ID as nvarchar)+'.IntersectTypeID = F'+cast(ID as nvarchar)+'.LookupObjectID' +
										 ' A.Object = case I'+cast(ID as nvarchar)+'.Subject and A.ObjectID = I'+cast(ID as nvarchar)+'.SubjectID then I'+cast(ID as nvarchar)+'.Object = F'+cast(ID as nvarchar)+'.ObjectType else I'+cast(ID as nvarchar)+'.Subject = F'+cast(ID as nvarchar)+'.ObjectType end and' + 
										 ' A.Object = case I'+cast(ID as nvarchar)+'.Subject and A.ObjectID = I'+cast(ID as nvarchar)+'.SubjectID then I'+cast(ID as nvarchar)+'.ObjectID = F'+cast(ID as nvarchar)+'.ObjectID else I'+cast(ID as nvarchar)+'.SubjectID = F'+cast(ID as nvarchar)+'.ObjectID end and' + 
										 ' left join [Field] RF'+cast(ID as nvarchar)+' on' + 
										 ' RF'+cast(ID as nvarchar)+'.FieldTypeID = ' + cast(LookupObjectFieldTypeID as nvarchar) + ' and' +
										 ' RF'+cast(ID as nvarchar)+'.ObjectType = case F'+cast(ID as nvarchar)+'.ObjectType = I'+cast(ID as nvarchar)+'.Subject and F'+cast(ID as nvarchar)+'.ObjectID = I'+cast(ID as nvarchar)+'.SubjectID then I'+cast(ID as nvarchar)+'.Object else I'+cast(ID as nvarchar)+'.Subject end and' + 
										 ' RF'+cast(ID as nvarchar)+'.ObjectID = case F'+cast(ID as nvarchar)+'.ObjectType = I'+cast(ID as nvarchar)+'.Subject and F'+cast(ID as nvarchar)+'.ObjectID = I'+cast(ID as nvarchar)+'.SubjectID then I'+cast(ID as nvarchar)+'.ObjectID else I'+cast(ID as nvarchar)+'.SubjectID end'
				else ''
			end as JoinStatement,
			case [Type]
				when 'Date' then 'cast(F'+cast(ID as nvarchar)+'.Value as Date)'
				when 'DateTime' then 'cast(F'+cast(ID as nvarchar)+'.Value as DateTime)'
				when 'Decimal' then 'cast(F'+cast(ID as nvarchar)+'.Value as decimal(18,10))'
				when 'Html' then 'dbo.StripHTML(F'+cast(ID as nvarchar)+'.Value)'
				when 'Lookup' then 'F'+cast(ID as nvarchar)+'.FormattedValue'
				when 'Number' then 'cast(F'+cast(ID as nvarchar)+'.Value as int)'
				when 'Relationship' then 'R'+cast(ID as nvarchar)+'.DisplayValue'
				when 'FieldFromRelationship' then 'RF'+cast(ID as nvarchar)+'.FormattedValue'
				when 'Password' then case when @showPassword = 1 then 'F'+cast(ID as nvarchar)+'.Value' else '''*****''' end
				else 'F'+cast(ID as nvarchar)+'.Value'
			end + ' as [' + Name + ']' as ColumnStatement,
			case [Type]
				when 'Date' then 'cast(F'+cast(ID as nvarchar)+'.Value as Date)'
				when 'DateTime' then 'cast(F'+cast(ID as nvarchar)+'.Value as DateTime)'
				when 'Decimal' then 'cast(F'+cast(ID as nvarchar)+'.Value as decimal(18,10))'
				when 'Html' then 'dbo.StripHTML(F'+cast(ID as nvarchar)+'.Value)'
				when 'Lookup' then 'F'+cast(ID as nvarchar)+'.FormattedValue'
				when 'Number' then 'cast(F'+cast(ID as nvarchar)+'.Value as int)'
				when 'Relationship' then 'R'+cast(ID as nvarchar)+'.DisplayValue'
				when 'FieldFromRelationship' then 'RF'+cast(ID as nvarchar)+'.FormattedValue'
				when 'Password' then case when @showPassword = 1 then 'F'+cast(ID as nvarchar)+'.Value' else '''*****''' end
				else 'F'+cast(ID as nvarchar)+'.Value'
			end as SortStatement,
			Name,
			ColumnOrder,
			IsListable,
			SortOrder
	from	FieldType
	where	Type not in (select Type from @avoids)
			and Object = @type
			and ObjectID = @id

	--select	string_agg(CN, ', ') as [Columns]
	--from	(
	--		select		top 100 percent	
	--					case [Type]
	--						when 'Date' then 'cast(F'+cast(ID as nvarchar)+'.Value as Date)'
	--						when 'DateTime' then 'cast(F'+cast(ID as nvarchar)+'.Value as DateTime)'
	--						when 'Decimal' then 'cast(F'+cast(ID as nvarchar)+'.Value as decimal(18,10))'
	--						when 'Html' then 'dbo.StripHTML(F'+cast(ID as nvarchar)+'.Value)'
	--						when 'Lookup' then 'F'+cast(ID as nvarchar)+'.FormattedValue'
	--						when 'Number' then 'cast(F'+cast(ID as nvarchar)+'.Value as int)'
	--						when 'Relationship' then 'R'+cast(ID as nvarchar)+'.DisplayValue'
	--						when 'FieldFromRelationship' then 'RF'+cast(ID as nvarchar)+'.FormattedValue'
	--						when 'Password' then '''*****'''
	--						else 'F'+cast(ID as nvarchar)+'.Value'
	--					end + ' as [' + Name + ']' as CN
	--		from		FieldType
	--		where		Type not in (select Type from @avoids)
	--					and Object = @type
	--					and ObjectID = @id
	--		order by	ColumnOrder
	--		) O

	--select	string_agg(CN, ',') as [Sorts]
	--from	(
	--		select		top 100 percent	
	--					case [Type]
	--						when 'Date' then 'cast(F'+cast(ID as nvarchar)+'.Value as Date)'
	--						when 'DateTime' then 'cast(F'+cast(ID as nvarchar)+'.Value as DateTime)'
	--						when 'Decimal' then 'cast(F'+cast(ID as nvarchar)+'.Value as decimal(18,10))'
	--						when 'Html' then 'dbo.StripHTML(F'+cast(ID as nvarchar)+'.Value)'
	--						when 'Lookup' then 'F'+cast(ID as nvarchar)+'.FormattedValue'
	--						when 'Number' then 'cast(F'+cast(ID as nvarchar)+'.Value as int)'
	--						when 'Relationship' then 'R'+cast(ID as nvarchar)+'.DisplayValue'
	--						when 'FieldFromRelationship' then 'RF'+cast(ID as nvarchar)+'.FormattedValue'
	--						when 'Password' then '''*****'''
	--						else 'F'+cast(ID as nvarchar)+'.Value'
	--					end as CN
	--		from		FieldType
	--		where		Type not in (select Type from @avoids)
	--					and Object = @type
	--					and ObjectID = @id
	--		order by	SortOrder
	--		) O
end
GO




CREATE NONCLUSTERED INDEX [IX_Asset_AssetType_KeyHash_Include]
    ON [dbo].[Asset]([AssetTypeID] ASC, [KeyHash] ASC)
    INCLUDE([ID])
GO

CREATE NONCLUSTERED INDEX [IX_Asset_AssetTypeID_Include]
    ON [dbo].[Asset]([AssetTypeID] ASC)
    INCLUDE([ID], [Object], [ObjectID])
GO

CREATE NONCLUSTERED INDEX [IX_Asset_Object_Include]
    ON [dbo].[Asset]([Object] ASC)
    INCLUDE([ID], [AssetTypeID], [ObjectID])
GO

CREATE NONCLUSTERED INDEX [IX_Asset_Object_ObjectID_Include]
    ON [dbo].[Asset]([Object] ASC, [ObjectID] ASC)
    INCLUDE([ID], [AssetTypeID])
GO

CREATE TRIGGER [dbo].[Asset_AfterDelete]
	ON [dbo].[Asset]
	AFTER DELETE
AS
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Object], [ObjectID])
        select	'Delete', 
				Object, 
				ObjectID 
		from	deleted;
GO

CREATE TRIGGER [dbo].[Asset_AfterInsert]
   ON  [dbo].[Asset] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Object], [ObjectID], [Custom])
        select	'Add', 
				I.Object, 
				I.ObjectID,
				[queue].WriteIndexXml('', I.[Object], I.ObjectID, coalesce(I.CreatedBy, 0)) 
		from	inserted I;
GO

CREATE TRIGGER [dbo].[Asset_AfterUpdate]
   ON  [dbo].[Asset] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Object], [ObjectID], [Custom])
        select	'Update', 
				I.Object, 
				I.ObjectID,
				[queue].WriteIndexXml('', I.[Object], I.ObjectID, coalesce(I.UpdatedBy, 0)) 
		from	inserted I
GO

CREATE NONCLUSTERED INDEX [IX_AssetType_Object_ObjectID_Include]
    ON [dbo].[AssetType]([Object] ASC, [ObjectID] ASC)
    INCLUDE([ID])
GO

CREATE NONCLUSTERED INDEX [IX_Intersect_IntersectTypeID]
    ON [dbo].[Intersect]([IntersectTypeID] ASC)
GO

CREATE NONCLUSTERED INDEX [IX_IntersectType_PredicateID]
    ON [dbo].[IntersectType]([PredicateID] ASC)
GO
