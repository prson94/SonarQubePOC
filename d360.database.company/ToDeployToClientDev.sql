--BEGIN: Migrate Score Metric relationship XML--------------------------------------------------------------------------------------------------------------

if OBJECT_ID('tempdb..#tempScoreMetric') IS NOT NULL DROP TABLE #tempScoreMetric;
go

create table #tempScoreMetric (ID int, Object varchar(50), ObjectID int, Configuration XML);
go

insert into #tempScoreMetric 
select 
	s.ID,
	s.Object,
	s.ObjectID,
	s.Configuration
from 
	scoretypemetric s 
	outer apply s.Configuration.nodes('/fields/CheckObjects') as R(r)
where 
	s.checktype = 5 and s.deleted = 0 and s.Configuration.exist('/fields/CheckObjects/Object') = 1;

declare @i int;
select @i = count(*) from #tempScoreMetric;

while @i != 0
begin
	declare @config xml;
	declare @newConfig xml;
	declare @rowId int;
	
	select top 1 
		@config = Configuration, 
		@rowId = ID,
		@newConfig = ''
	from #tempScoreMetric;
	
	select 
		@newConfig = '<fields><CheckObjects>' + string_agg('<IntersectType>' + cast(T.ID as varchar) + '</IntersectType>', '') + '</CheckObjects></fields>'
	from IntersectType T
	inner join 
	(
		select 
		R.r.value('(Type/text())[1]', 'varchar(50)') as [Object], 
		R.r.value('(ID/text())[1]', 'int') as ObjectID 
		from @config.nodes('/fields/CheckObjects/*') as R(r)
	) obj on ((T.[Object] = obj.[Object] and T.ObjectID = obj.ObjectID) or (T.[Subject] = obj.[Object] and T.SubjectID = obj.ObjectID))
	inner join #tempScoreMetric m on m.ID = @rowId
	where ((T.[Object] = m.[Object] and T.objectID = m.ObjectID) or (T.[Subject] =  m.[Object] and T.SubjectID = m.ObjectID))

	update ScoreTypeMetric
	set Configuration = @newConfig
	where ID = @rowID;

	delete from #tempScoreMetric where ID = @rowID;

	select @i = count(*) from #tempScoreMetric;
end
--END: Migrate Score Metric relationship XML----------------------------------------------------------------------------------------------------------------




--add missing pk to sitenav. needed for fk on permission
alter table SiteNav add constraint PK_SiteNav primary key (ID);
go

--shopping cart type
insert into ShoppingCartType values ('Shopping Cart');
go


CREATE TABLE [workflow].[ItemAssignment](
	ID [bigint] IDENTITY(1,1),
	ItemID [bigint] NOT NULL,
	ResourceObject varchar(50) NOT NULL,
	ResourceObjectID int NOT NULL,
	[Active] [bit] NOT NULL,
	CreatedBy int not null,
	CreatedOn datetime NOT NULL,
	UpdatedBy int not null,
	UpdatedOn datetime NOT NULL,
	CONSTRAINT [PK_WorkflowItemAssignment] PRIMARY KEY CLUSTERED ( [ID] ASC )
)
GO

ALTER TABLE [workflow].[ItemAssignment] ADD  CONSTRAINT [DF_WorkflowItemAssignment_Active]  DEFAULT ((1)) FOR [Active]
GO

ALTER TABLE [workflow].[ItemAssignment]  WITH CHECK ADD  CONSTRAINT [FK_WorkflowItemAssignment_WorkflowItem] FOREIGN KEY([ItemID])
REFERENCES [workflow].[Item] ([ID])
ON DELETE CASCADE
GO

ALTER TABLE [workflow].[ItemAssignment] CHECK CONSTRAINT [FK_WorkflowItemAssignment_WorkflowItem]
GO

ALTER TABLE [workflow].[ItemStep] add  ResourceObject varchar(50) NULL
GO
ALTER TABLE [workflow].[ItemStep] add  ResourceObjectID int NULL
GO

CREATE SCHEMA [analytics]
GO

CREATE TABLE [analytics].[Action](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Value] [varchar](50) NOT NULL,
	CONSTRAINT [PK_Analytics_Action] PRIMARY KEY NONCLUSTERED ( [ID] ASC )
)
GO

CREATE CLUSTERED INDEX CIX_Analytics_Action ON [analytics].[Action] ( [Value] ASC )
GO

CREATE TABLE [analytics].[BrowserLanguage](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Value] [varchar](500) NOT NULL,
	CONSTRAINT [PK_Analytics_BrowserLanguage] PRIMARY KEY NONCLUSTERED ( [ID] ASC )
)
GO

CREATE CLUSTERED INDEX CIX_Analytics_BrowserLanguage ON [analytics].[BrowserLanguage] ( [Value] ASC )
GO

CREATE TABLE [analytics].[Host](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Value] [varchar](50) NOT NULL,
	CONSTRAINT [PK_Analytics_Host] PRIMARY KEY NONCLUSTERED ( [ID] ASC )
)
GO

CREATE CLUSTERED INDEX CIX_Analytics_Host ON [analytics].[Host] ( [Value] ASC )
GO

CREATE TABLE [analytics].[Ip](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Value] [varchar](100) NOT NULL,
	CONSTRAINT [PK_Analytics_Ip] PRIMARY KEY NONCLUSTERED ( [ID] ASC )
)
GO

CREATE CLUSTERED INDEX CIX_Analytics_Ip ON [analytics].[Ip] ( [Value] ASC )
GO

CREATE TABLE [analytics].[Object](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Value] [varchar](50) NOT NULL,
	CONSTRAINT [PK_Analytics_Object] PRIMARY KEY NONCLUSTERED ( [ID] ASC )
)
GO

CREATE CLUSTERED INDEX CIX_Analytics_Object ON [analytics].[Object] ( [Value] ASC )
GO

CREATE TABLE [analytics].[UserAgent](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Value] [nvarchar](250) NULL,
	CONSTRAINT [PK_Analytics_UserAgent] PRIMARY KEY NONCLUSTERED ( [ID] ASC )
)
GO

CREATE CLUSTERED INDEX CIX_Analytics_UserAgent ON [analytics].[UserAgent] ( [Value] ASC )
GO

CREATE TABLE [analytics].[Statistic](
	[ID] [uniqueidentifier] NOT NULL,
	[Object] [int] NOT NULL,
	[ObjectID] [int] NOT NULL,
	[IpID] [int] NOT NULL,
	[UserAgentID] [int] NOT NULL,
	[HostID] [int] NOT NULL,
	[BrowserLanguageID] [int] NOT NULL,
	[ActionID] [smallint] NOT NULL,
	[ResourceID] [int] NOT NULL,
	[Timestamp] [datetime] NOT NULL,
	CONSTRAINT [PK_Analytics_Statistic] PRIMARY KEY NONCLUSTERED ( [ID] ASC )
)
GO

ALTER TABLE [analytics].[Statistic] ADD  CONSTRAINT [DF_Analytics_Statistic_ID]  DEFAULT (newid()) FOR [ID]
GO

ALTER TABLE [analytics].[Statistic] ADD  CONSTRAINT [DF_Analytics_Statistic_ResourceID]  DEFAULT ((0)) FOR [ResourceID]
GO

CREATE CLUSTERED INDEX CIX_Analytics_Statistic ON [analytics].[Statistic] ( Object ASC, ObjectID ASC )
GO

CREATE NONCLUSTERED INDEX IX_Analytics_Statistic_Object ON [analytics].[Statistic] ( Object ASC, ObjectID ASC )
GO

CREATE NONCLUSTERED INDEX IX_Analytics_Statistic_Timestamp ON [analytics].[Statistic] ( [Timestamp] ASC )
GO

alter table FieldType add IsDisplayable bit not null constraint DF_FieldType_IsDisplayable default(1)
go
alter table FieldType add IsEditable bit not null constraint DF_FieldType_IsEditable default(1)
go

update FieldType set IsEditable = 0 where Object = 'FusionAttributeType'
GO

