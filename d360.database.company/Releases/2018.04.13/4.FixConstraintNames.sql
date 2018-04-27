declare @schema_name nvarchar(256)
declare @table_name nvarchar(256)
declare @col_name nvarchar(256)
declare @Command  nvarchar(1000)

set @schema_name = N'dbo'
set @table_name = N'ArtifactTypeExportTemplate'
set @col_name = N'ExportViewType'

select	@Command = 'ALTER TABLE ' + @schema_name + '.' + @table_name + ' drop constraint ' + d.name
from	sys.tables t
		join sys.default_constraints d on d.parent_object_id = t.object_id
		join sys.columns c on c.object_id = t.object_id and c.column_id = d.parent_column_id
where	t.name = @table_name
		and t.schema_id = schema_id(@schema_name)
		and c.name = @col_name

--select @Command
if @Command is not null
begin
	execute (@Command)
end
GO

ALTER TABLE [dbo].[ArtifactTypeExportTemplate] ADD  CONSTRAINT [DF_ArtifactTypeExportTemplate_ExportViewType]  DEFAULT ((0)) FOR [ExportViewType]
GO

set @col_name = N'IncludeUrl'

select	@Command = 'ALTER TABLE ' + @schema_name + '.' + @table_name + ' drop constraint ' + d.name
from	sys.tables t
		join sys.default_constraints d on d.parent_object_id = t.object_id
		join sys.columns c on c.object_id = t.object_id and c.column_id = d.parent_column_id
where	t.name = @table_name
		and t.schema_id = schema_id(@schema_name)
		and c.name = @col_name

--select @Command
if @Command is not null
begin
	execute (@Command)
end
GO

ALTER TABLE [dbo].[ArtifactTypeExportTemplate] ADD  CONSTRAINT [DF_ArtifactTypeExportTemplate_IncludeUrl]  DEFAULT ((1)) FOR [IncludeUrl]
GO

set @col_name = N'IncludeParent'

select	@Command = 'ALTER TABLE ' + @schema_name + '.' + @table_name + ' drop constraint ' + d.name
from	sys.tables t
		join sys.default_constraints d on d.parent_object_id = t.object_id
		join sys.columns c on c.object_id = t.object_id and c.column_id = d.parent_column_id
where	t.name = @table_name
		and t.schema_id = schema_id(@schema_name)
		and c.name = @col_name

--select @Command
if @Command is not null
begin
	execute (@Command)
end
GO

ALTER TABLE [dbo].[ArtifactTypeExportTemplate] ADD  CONSTRAINT [DF_ArtifactTypeExportTemplate_IncludeParent]  DEFAULT ((1)) FOR [IncludeParent]
GO


set @schema_name = N'dbo'
set @table_name = N'ArtifactTypeExportTemplateStyle'
set @col_name = N'IsBold'

select	@Command = 'ALTER TABLE ' + @schema_name + '.' + @table_name + ' drop constraint ' + d.name
from	sys.tables t
		join sys.default_constraints d on d.parent_object_id = t.object_id
		join sys.columns c on c.object_id = t.object_id and c.column_id = d.parent_column_id
where	t.name = @table_name
		and t.schema_id = schema_id(@schema_name)
		and c.name = @col_name

--select @Command
if @Command is not null
begin
	execute (@Command)
end
GO

ALTER TABLE [dbo].[ArtifactTypeExportTemplateStyle] ADD  CONSTRAINT [DF_ArtifactTypeExportTemplateStyle_IsBold]  DEFAULT ((0)) FOR [IsBold]
GO

set @schema_name = N'dbo'
set @table_name = N'AttributeType'
set @col_name = N'ShowNameInTree'

select	@Command = 'ALTER TABLE ' + @schema_name + '.' + @table_name + ' drop constraint ' + d.name
from	sys.tables t
		join sys.default_constraints d on d.parent_object_id = t.object_id
		join sys.columns c on c.object_id = t.object_id and c.column_id = d.parent_column_id
where	t.name = @table_name
		and t.schema_id = schema_id(@schema_name)
		and c.name = @col_name

--select @Command
if @Command is not null
begin
	execute (@Command)
end
GO

ALTER TABLE [dbo].[AttributeType] ADD  CONSTRAINT [DF_AttributeType_ShowNameInTree]  DEFAULT ((1)) FOR [ShowNameInTree]
GO

set @col_name = N'DisplayFormat'

select	@Command = 'ALTER TABLE ' + @schema_name + '.' + @table_name + ' drop constraint ' + d.name
from	sys.tables t
		join sys.default_constraints d on d.parent_object_id = t.object_id
		join sys.columns c on c.object_id = t.object_id and c.column_id = d.parent_column_id
where	t.name = @table_name
		and t.schema_id = schema_id(@schema_name)
		and c.name = @col_name

--select @Command
if @Command is not null
begin
	execute (@Command)
end
GO

ALTER TABLE [dbo].[AttributeType] ADD  CONSTRAINT [DF_AttributeType_DisplayFormat]  DEFAULT ('') FOR [DisplayFormat]
GO

set @schema_name = N'dbo'
set @table_name = N'CommentVote'
set @col_name = N'Vote'

select	@Command = 'ALTER TABLE ' + @schema_name + '.' + @table_name + ' drop constraint ' + d.name
from	sys.tables t
		join sys.default_constraints d on d.parent_object_id = t.object_id
		join sys.columns c on c.object_id = t.object_id and c.column_id = d.parent_column_id
where	t.name = @table_name
		and t.schema_id = schema_id(@schema_name)
		and c.name = @col_name

--select @Command
if @Command is not null
begin
	execute (@Command)
end
GO

ALTER TABLE [dbo].CommentVote ADD  CONSTRAINT DF_CommentVote_Vote  DEFAULT (0) FOR [Vote]
GO

set @schema_name = N'dbo'
set @table_name = N'Organization'
set @col_name = N'State'

select	@Command = 'ALTER TABLE ' + @schema_name + '.' + @table_name + ' drop constraint ' + d.name
from	sys.tables t
		join sys.default_constraints d on d.parent_object_id = t.object_id
		join sys.columns c on c.object_id = t.object_id and c.column_id = d.parent_column_id
where	t.name = @table_name
		and t.schema_id = schema_id(@schema_name)
		and c.name = @col_name

--select @Command
if @Command is not null
begin
	execute (@Command)
end
GO

ALTER TABLE [dbo].Organization ADD  CONSTRAINT DF_Organization_State  DEFAULT (0) FOR [State]
GO

--alter table [Survey] alter column [Object] varchar(50) NOT NULL

