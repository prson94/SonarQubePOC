-- ExecutionAsset table changes.
alter table [integration].[ExecutionAsset] add [Uid] uniqueidentifier constraint DF_IntegrationExecutionAsset_Uid default(newid()) not null
GO;

ALTER TABLE [integration].[ExecutionAsset] DROP CONSTRAINT [PK_IntegrationExecutionAsset]
GO;

DROP INDEX [CIX_IntegrationExecutionAsset] ON [integration].[ExecutionAsset] WITH ( ONLINE = OFF )
GO;

ALTER TABLE [integration].[ExecutionAsset] ADD  CONSTRAINT [PK_IntegrationExecutionAsset] PRIMARY KEY CLUSTERED ( [Uid] ASC )
GO;

ALTER TABLE [integration].[ExecutionAsset] ADD  CONSTRAINT [UQ_IntegrationExecutionAsset] UNIQUE ( [ExecutionID] DESC, [SynchedAssetTypeID] ASC, [SourceID] ASC )
GO;


-- Role table changes.
alter table [integration].[SynchedAssetTypeRoleItem] add ResponsibilityTypeID int null
GO;
update	T
set		T.ResponsibilityTypeID = S.ID
from	[integration].[SynchedAssetTypeRoleItem] T
		inner join ResponsibilityType S on S.Name = T.RoleName
GO;
alter table [integration].[SynchedAssetTypeRoleItem] alter column ResponsibilityTypeID int not null
GO;
alter table [integration].[SynchedAssetTypeRoleItem] drop column RoleName
GO;

ALTER TABLE [integration].[SynchedAssetTypeRoleItem] WITH CHECK ADD CONSTRAINT [FK_IntegrationSynchedAssetTypeRoleItem_ResponsibilityType] FOREIGN KEY([ResponsibilityTypeID]) REFERENCES [dbo].[ResponsibilityType] ([ID])
ALTER TABLE [integration].[SynchedAssetTypeRoleItem] CHECK CONSTRAINT [FK_IntegrationSynchedAssetTypeRoleItem_ResponsibilityType]
GO;

-- Addition of ExecutionAssetField table
create table integration.ExecutionAssetField (
	[Uid] uniqueidentifier NOT NULL, 
	Section int not null, 
	FieldName nvarchar(250) NOT NULL, 
	FieldValue nvarchar(max) null
);
ALTER TABLE [integration].[ExecutionAssetField] ADD  CONSTRAINT [PK_IntegrationExecutionAssetField] PRIMARY KEY CLUSTERED ( [Uid] ASC, Section ASC, FieldName ASC );
CREATE NONCLUSTERED INDEX [IX_IntegrationExecutionAsset_FieldName] ON [integration].[ExecutionAssetField] ( [FieldName] ASC );
CREATE NONCLUSTERED INDEX IX_IntegrationExecutionAsset_Section_Include ON [integration].[ExecutionAssetField] ([Section]) INCLUDE ([FieldValue]);
CREATE NONCLUSTERED INDEX IX_IntegrationExecutionAssetField_Section_FieldName_Include ON [integration].[ExecutionAssetField] ([Section],[FieldName]) INCLUDE ([FieldValue]);
GO;

-- Addition of ExecutionAssetTypeMetricRelationshipLog table
CREATE TABLE [integration].[ExecutionAssetTypeMetricRelationshipLog](
	[Uid] uniqueidentifier constraint DF_IntegrationExecutionAssetTypeMetricRelationshipLog_Uid default(newid()) not null,
	[ExecutionID] [bigint] NOT NULL,
	[SynchedAssetTypeID] [int] NOT NULL,
	[Action] varchar(1) not null,
	SubjectSourceID nvarchar(250),
	ObjectSourceID nvarchar(250), 
	IntersectID int
	CONSTRAINT [PK_IntegrationExecutionAssetTypeMetricLog] PRIMARY KEY CLUSTERED ( [Uid] ASC )
)
GO;

--Index changes
DROP INDEX [IX_IntegrationExecutionUnresolvedRelationItem_ObjectInfo] ON [integration].[ExecutionUnresolvedRelationItem]
DROP INDEX [IX_IntegrationExecutionUnresolvedRelationItem_SubjectInfo] ON [integration].[ExecutionUnresolvedRelationItem]
GO;

CREATE NONCLUSTERED INDEX [IX_IntegrationExecutionUnresolvedRelationItem_SubjectAssetID_Include] ON [integration].[ExecutionUnresolvedRelationItem] ([SubjectAssetID]) INCLUDE ([IntersectTypeID],[SubjectAssetTypeID],[SubjectSourceID])
CREATE NONCLUSTERED INDEX [IX_IntegrationExecutionUnresolvedRelationItem_ObjectAssetID_Include] ON [integration].[ExecutionUnresolvedRelationItem] ([ObjectAssetID]) INCLUDE ([IntersectTypeID],[ObjectAssetTypeID],[ObjectSourceID])
GO;


--ALTER procedure [integration].[ProcessExecutionAssetType] (pull the latest proc)