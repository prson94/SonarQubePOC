alter table [integration].[Setting] add DeleteExecutionTimeoutHours int constraint DF_IntegrationSetting_DeleteExecutionTimeoutHours default(192) not null
alter table [integration].[SynchedAssetType] add DeleteExecutionTimeoutHours int null
GO