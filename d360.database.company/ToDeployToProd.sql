/*
--------------------------------------------------------------------------------------
 This file contains a list of SQL files that need to be executed when releasing 
 to production in the next cycle.
--------------------------------------------------------------------------------------
*/

DROP TABLE [fusion].[StagingError]
GO
DROP TABLE [fusion].[StagingItem]
GO
DROP TABLE [fusion].[StagingItemArchive]
GO
DROP TABLE [fusion].[StagingRelationArchive]
GO
DROP TABLE [fusion].[StagingRelationMapping]
GO
DROP TABLE [fusion].[StagingStatistic]
GO
DROP TABLE [fusion].[StepStatistic]
GO

DROP procedure [fusion].[ProcessFusionInQueue]
go

alter table [Load] add [UpdatedBy] INT NOT NULL CONSTRAINT [CK_Load_UpdatedBy] DEFAULT ((0))
go

drop TRIGGER dbo.ObjectVersion_InsteadOfInsert
go



alter table FieldTypeFusionLookupDefinition add ReferenceType int not null constraint DF_FieldTypeFusionLookupDefinition_ReferenceType default(2)
GO
alter table FieldTypeFusionLookupDefinition drop column [Display]
GO
--ALTER TABLE [dbo].[FieldTypeFusionLookupDefinition] DROP CONSTRAINT DF__FieldType__IsPar
--go
ALTER TABLE [dbo].[FieldTypeFusionLookupDefinition] DROP CONSTRAINT [DF_FieldTypeFusionLookupDefinition_IsParentChild]
GO
alter table FieldTypeFusionLookupDefinition drop column [IsParentChild]
GO
alter table FieldTypeFusionLookupDefinition alter column [TargetFusionAttributeTypeID] int null
GO

alter table FieldTypeFusionLookupDisplayField add FieldTypeName nvarchar(250) null
GO

ALTER TABLE [dbo].[FieldTypeFusionLookupDefinition] DROP CONSTRAINT [FK_FieldTypeFusionLookupDefinition_FieldType]
GO

ALTER TABLE [dbo].[FieldTypeFusionLookupDefinition]  WITH CHECK ADD  CONSTRAINT [FK_FieldTypeFusionLookupDefinition_FieldType] FOREIGN KEY([FieldTypeID]) REFERENCES [dbo].[FieldType] ([ID]) ON DELETE CASCADE
GO

ALTER TABLE [dbo].[FieldTypeFusionLookupDefinition] CHECK CONSTRAINT [FK_FieldTypeFusionLookupDefinition_FieldType]
GO

CREATE TABLE [dbo].[FieldTypeRelationLookupDefinition] (
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[FieldTypeID] [int] NOT NULL,
	[IntersectTypeID] [int] NOT NULL,
	[ReferenceType] [int] NOT NULL CONSTRAINT [DF_FieldTypeRelationLookupDefinition_ReferenceType]  DEFAULT ((2)),
	[ChildIntersectTypeID] [int] NULL,
	[HideHeader] BIT CONSTRAINT [DF_FieldTypeRelationLookupDefinition_HideHeader] DEFAULT ((1)) NOT NULL,
    [HideFooter] BIT CONSTRAINT [DF_FieldTypeRelationLookupDefinition_HideFooter] DEFAULT ((1)) NOT NULL,
	PRIMARY KEY CLUSTERED ( [ID] ASC )
)
GO

ALTER TABLE [dbo].[FieldTypeRelationLookupDefinition]  WITH CHECK ADD  CONSTRAINT [FK_FieldTypeRelationLookupDefinition_FieldType] FOREIGN KEY([FieldTypeID]) REFERENCES [dbo].[FieldType] ([ID]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[FieldTypeRelationLookupDefinition] CHECK CONSTRAINT [FK_FieldTypeRelationLookupDefinition_FieldType]
GO


--alter table [FieldTypeFusionLookupDefinition] add HideHeader bit not null constraint DF_FieldTypeFusionLookupDefinition_HideHeader default(1)
--GO
--alter table [FieldTypeFusionLookupDefinition] add HideFooter bit not null constraint DF_FieldTypeFusionLookupDefinition_HideFooter default(1)
--GO

alter table [FieldTypeRelationLookupDefinition] add HideHeader bit not null constraint DF_FieldTypeRelationLookupDefinition_HideHeader default(1)
GO
alter table [FieldTypeRelationLookupDefinition] add HideFooter bit not null constraint DF_FieldTypeRelationLookupDefinition_HideFooter default(1)
GO

CREATE TABLE [dbo].[FieldTypeRelationLookupDisplayField](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[FieldTypeRelationLookupDefinitionID] [int] NOT NULL,
	[FieldTypeID] [int] NOT NULL,
	[FieldTypeName] [nvarchar](250) NULL,
	PRIMARY KEY CLUSTERED ( [ID] ASC )
)
GO

ALTER TABLE [dbo].[FieldTypeRelationLookupDisplayField]  WITH CHECK ADD  CONSTRAINT [FK_FieldTypeRelationLookupDisplayField_FieldTypeRelationLookupDefinitionID] FOREIGN KEY([FieldTypeRelationLookupDefinitionID])
REFERENCES [dbo].[FieldTypeRelationLookupDefinition] ([ID])
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[FieldTypeRelationLookupDisplayField] CHECK CONSTRAINT [FK_FieldTypeRelationLookupDisplayField_FieldTypeRelationLookupDefinitionID]
GO

alter table SourceRule add IsTmplate bit not null constraint DF_SourceRule_IsTemplate default(0)
GO
alter table SourceRule drop column AppliesToObjectList
GO

alter table [fusion].[Execution] alter column QueueID uniqueidentifier null
go

alter view [cache].[Responsibilities]
as
	SELECT R.[ResponsibilityID]
		  ,R.[ResponsibilityTypeID]
		  ,R.[ResponsibilityType]
		  ,R.[AssigningItem]
		  ,R.[AssigningItemID]
		  ,A.TextPath as [AssigningItemName]
		  ,A.Url as [AssigningItemUrl]
		  ,A.ObjectType as [AssigningItemType]
		  ,A.ObjectTypeID as [AssigningItemTypeID]
		  ,A.ObjectTypeName as [AssigningTypeName]
		  ,R.[Object]
		  ,R.[ObjectID]
		  ,O.TextPath as [ObjectName]
		  ,O.[ObjectType]
		  ,O.[ObjectTypeID]
		  ,O.[ObjectTypeName]
		  ,O.Url as [ObjectUrl]
		  ,R.[ResponsibleObject]
		  ,R.[ResponsibleObjectID]
		  ,RO.Name as [ResponsibleObjectName]
		  ,RO.Url as [ResponsibleObjectUrl]
		  ,R.[ContextHash]
		  ,R.[ResponsibilityTypeGroup]
		  ,R.[Visible]
		  ,R.[TargetResponsibilityID]
	FROM	[cache].ResponsibilityItem R
			inner join cache.ObjectDetails A on A.[Object] = R.[AssigningItem] and A.[ObjectID] = R.[AssigningItemID]
			inner join cache.ObjectDetails O on O.[Object] = R.[Object] and O.[ObjectID] = R.[ObjectID]
			inner join cache.ObjectDetails RO on RO.[Object] = R.[ResponsibleObject] and RO.[ObjectID] = R.[ResponsibleObjectID]
GO

ALTER VIEW [dbo].[FieldWithRelation]
AS
	SELECT	F.FieldTypeID,
			T.Name,
			T.FriendlyName,
			T.Description,
			T.DisplayDescription,
			T.FormDescription,
			T.ValidationDescription,
			T.Type,
			T.LookupObjectType,
			T.LookupObjectID,
			T.LookupDisplayFormat,
			T.MinimumLength,
			T.MaximumLength,
			T.Length,
			T.Pattern,
			T.IsListable,
			T.IsRequired,
			T.SortOrder,
			F.ObjectType,
			F.ObjectID,
			F.Value,
			F.FormattedValue,
			LD.Url as LookupUrl
	FROM	FieldType T
			inner join Field F on F.FieldTypeID = T.ID and ( 
															(F.ObjectType + 'Type' = T.[Object] and F.ObjectType <> 'Event') OR 
															(T.[Object] = 'Rule' and F.ObjectType = 'Event') 
														   )
			left join cache.ObjectDetails D on D.[Object] = F.ObjectType and D.ObjectID = F.ObjectID
			left join Attribute AD on F.ObjectType = 'Attribute' and AD.ID = F.ObjectID
			left join cache.ObjectDetails LD on 
				LD.[Object] = case when T.LookupObjectType = 'Lookup' then 'LookupType' when T.LookupObjectType = 'DomainItem' then 'Domain' else T.LookupObjectType end
				and LD.ObjectID = case when T.LookupObjectType = 'Lookup' then T.LookupObjectID when T.LookupObjectType = 'DomainItem' then T.LookupObjectID when T.LookupObjectType = 'Resource' then T.LookupObjectID when T.LookupObjectType is null then NULL else F.Value end
	where	T.ObjectID = coalesce(D.ObjectTypeID, AD.AttributeTypeID)
			and coalesce(D.ObjectID, AD.ID) is not null 
GO

alter view [dbo].[ResponsibilityDetail]
as
	select	P.Visible,
			P.ResponsibilityID,
			P.ResponsibilityTypeID,
			P.AssigningItem as AssigningItemType,
			P.AssigningItemID,
			P.[Object] as ObjectType,
			P.ObjectID,
			P.ObjectName,
			P.ObjectTypeID,
			P.ObjectTypeName,
			P.ObjectUrl,
			P.ResponsibleObject as ResponsibleObjectType,
			P.ResponsibleObjectID,
			P.ResponsibleObjectName,
			P.ResponsibleObjectUrl,
			RODG.PrimaryOwnerResourceID,
			RES.FirstName + ' ' + RES.LastName as PrimaryOwnerResourceName,
			case 
				when RODG.PrimaryOwnerResourceID is null then ''
				else '#/resources/' + cast(RODG.PrimaryOwnerResourceID as varchar(10))
			end as PrimaryOwnerResourceUrl,
			P.ResponsibilityType as [Role],
			dbo.GetObjectStatisticScore(P.[Object], P.ObjectID) as CurrentScore,
			CI.ContextItems
	from	cache.Responsibilities P
			left join [Group] RODG on P.ResponsibleObject = 'Group' and RODG.ID = P.ResponsibleObjectID
			left join [reporting].[Global_Resource] RES on RES.ResourceID = RODG.PrimaryOwnerResourceID
			outer apply (
						select (
								select	D.Name + ': ' + I.Code + ' - ' + I.Name + '; '
								from	ResponsibilityContextItem C
										inner join DomainItem I on C.ObjectType = 'DomainItem' and C.ObjectID = I.ID
										inner join Domain D on D.ID = I.DomainID
								where	ResponsibilityID = P.ResponsibilityID
								for xml path ('')--, root('items')
								) as ContextItems
						) CI
	where	[ResponsibilityTypeGroup] = 1
go

alter view [dbo].[ResponsibilityDetailForResource]
as
	select	RD.Visible,
			RD.ResponsibilityID,
			RD.ResponsibilityTypeID,
			RD.ObjectType,
			RD.ObjectTypeID,
			RD.ObjectID,
			RD.ObjectName,
			RD.ObjectTypeName,
			RD.ObjectUrl,
			case 
				when RG.ResourceID is not null then 'Resource'
				else RD.ResponsibleObjectType
			end as ResponsibleObjectType,
			COALESCE(RG.ResourceID, RD.ResponsibleObjectID) as ResponsibleObjectID,
			case RD.ResponsibleObjectType
				when 'Group' then cast(1 as bit)
				else cast(0 as bit)
			end as FromGroup,
			RD.Role,
			RD.ContextItems,
			RD.CurrentScore
	from	ResponsibilityDetail RD
			left join [Group] G on RD.ResponsibleObjectType = 'Group' and G.ID = RD.ResponsibleObjectID
			left join ResourceGroup RG on RG.GroupID = G.ID
GO

CREATE TABLE [dbo].[IntersectMapGroup] (
    [IntersectMapID] INT NOT NULL,
    [GroupNumber]    INT NOT NULL,
    [ID]             INT IDENTITY (1, 1) NOT NULL
)
GO

alter procedure [dbo].[GetHierarchyByMapType]
	@type varchar(50),
	@id int,
	@mapType int
as
begin

 declare @results table (ID int, [Subject] varchar(150), SubjectID int, [Object] varchar(150),
	 ObjectID int, ObjectType varchar(150), ObjectTypeID int,
	 ParentID varchar(max), Name varchar(250), Path varchar(250), Url varchar(50),
	 ObjectTypeName varchar(100), [Level] int, PredicateID int, PredicatePhrase varchar(350), [Type] int, GroupNumber int, [UID] varchar(max));

 declare @results2 table (ID int, [Subject] varchar(150), SubjectID int, [Object] varchar(150),
	 ObjectID int, ObjectType varchar(150), ObjectTypeID int,
	 ParentID varchar(max), Name varchar(250), Path varchar(250), Url varchar(50),
	 ObjectTypeName varchar(100), [Level] int, PredicateID int, PredicatePhrase varchar(350), [Type] int, GroupNumber int, [UID] varchar(max));


with u as
(
	select  
		m.id as ID,
		n1.objecttype as [Subject], 
		n1.objectid as [SubjectID],
		n2.objecttype as [Object],
		n2.objectid as [ObjectID],
		d.ObjectType as ObjectType,
		d.ObjectTypeID as ObjectTypeID,
		cast('0' as varchar(max)) as ParentID,
		d.Name,
		cast(d.name as varchar(500)) as [Path],
		d.url as Url,
		d.objecttypename as ObjectTypeName, 
		-1 as [Level],
		m.PredicateID as PredicateID,
		coalesce(p.Name,'') + '/' + coalesce(p.Inverse,'') as PredicatePhrase,
		m.[Type] as [Type],
		-1 as GroupNumber,
		cast((n1.objecttype + cast(n1.objectid as varchar(10))) as varchar(max)) as [UID]
	from intersectmap m
	join intersectnode n1 on n1.id = m.subjectintersectnodeid
	join intersectnode n2 on n2.id = m.objectintersectnodeid
	join cache.objectdetails d on d.object = n1.objecttype and d.objectid = n1.objectid
	join predicate p on p.id = m.predicateid
	where n2.objecttype = @type and n2.objectid = @id and m.[type] = @mapType

	union all

	select 
		m.id as ID,
		n1.objecttype as [Subject], 
		n1.objectid as [SubjectID],
		n2.objecttype as [Object],
		n2.objectid as [ObjectID],
		d.ObjectType as ObjectType,
		d.ObjectTypeID as ObjectTypeID,
		u.UID as ParentID,
		d.Name, 
		cast(d.name + '/' + u.[Path] as varchar(500)) as [Path],
		d.url as Url,
		d.objecttypename as ObjectTypeName, 
		u.[Level]-1 as [Level],
		m.PredicateID as PredicateID,
		coalesce(p.Name,'') + '/' + coalesce(p.Inverse,'') as PredicatePhrase,
		m.[Type] as [Type],
		-1 as GroupNumber,
		cast((n1.objecttype + cast(n1.objectid as varchar(10))) as varchar(max)) as [UID]
	from intersectmap m
	join intersectnode n1 on n1.id = m.subjectintersectnodeid
	join intersectnode n2 on n2.id = m.objectintersectnodeid
	join cache.objectdetails d on d.object = n1.objecttype and d.objectid = n1.objectid
	join predicate p on p.id = m.predicateid
	join u on u.[Subject] = n2.objecttype and u.[SubjectID] = n2.objectid and (u.[subject] + cast(u.[subjectid] as varchar(10))) != (u.[object] + cast(u.[objectid] as varchar(10)))
	where m.[type] = @mapType
)
insert into @results
select distinct * from u order by u.uid asc;


declare @UID varchar(500);
select top 1 @UID = r.[UID] from @results r
join @results c on c.ParentID = r.[UID] 
where r.ParentID = '0' and c.[UID] != r.[UID] and r.[Level] < 0;

--select * from @results;


while (@UID is not null)
begin

	update @results
	set ParentID = (select top 1 [UID] from @results r where r.ParentID = @UID)
	where [UID] = @UID;

	update @results
	set ParentID = '0'
	where [UID] = (select ParentID from @results where [UID] = @UID and [Level] < 0);

	if (select count(*) from @results r
		join @results c on c.ParentID = @UID
		where r.ParentID = '0' and c.[UID] != r.[UID] and r.[Level] < 0) > 0
	begin
		select top 1 @UID = r.[UID] from @results r
		join @results c on c.ParentID = r.[UID] 
		where r.ParentID = '0' and c.[UID] != r.[UID] and r.[Level] < 0;
	end
	else
	begin
		select @UID = null;
	end

end

insert into @results
	select 
		r.ID as ID,
		t.[type] as [Subject], 
		t.[id] as SubjectID,
		t.[type]as [Object],
		t.[id] as [ObjectID],
		d.ObjectType as ObjectType,
		d.ObjectTypeID as ObjectTypeID,
		cast(r.[UID] as varchar(500)) as ParentID,
		d.Name,
		cast(d.name as varchar(500)) as [Path],
		d.url as Url,
		d.objecttypename as ObjectTypeName, 
		0 as [Level],
		r.PredicateID as PredicateID,
		r.PredicatePhrase as PredicatePhrase,
		t.mapType as [Type],
		r.GroupNumber,
		'root' + r.[UID] as [UID]
	from @results r 
	join (select @type as [type], @id as [id], @mapType as mapType) t on 1=1
	join cache.objectdetails d on d.[object] = t.[type] and d.objectid = t.id
	where r.[Level] = -1;

insert into @results
	select 
		r.ID as ID,
		t.[type] as [Subject], 
		t.[id] as SubjectID,
		t.[type]as [Object],
		t.[id] as [ObjectID],
		d.ObjectType as ObjectType,
		d.ObjectTypeID as ObjectTypeID,
		cast(null as varchar(max)) as ParentID,
		d.Name,
		cast(d.name as varchar(500)) as [Path],
		d.url as Url,
		d.objecttypename as ObjectTypeName, 
		0 as [Level],
		r.PredicateID as PredicateID,
		r.PredicatePhrase as PredicatePhrase,
		t.mapType as [Type],
		r.GroupNumber,
		'root' as [UID]
	from @results r 
	join (select @type as [type], @id as [id], @mapType as mapType) t on 1=1
	join cache.objectdetails d on d.[object] = t.[type] and d.objectid = t.id
	where r.[Level] = 1;

if (select count(*) from @results) < 1
begin
	insert into @results
	select 
		0 as ID,
		t.[type] as [Subject], 
		t.[id] as SubjectID,
		t.[type]as [Object],
		t.[id] as [ObjectID],
		d.ObjectType as ObjectType,
		d.ObjectTypeID as ObjectTypeID,
		cast(0 as varchar(max)) as ParentID,
		d.Name,
		cast(d.name as varchar(500)) as [Path],
		d.url as Url,
		d.objecttypename as ObjectTypeName, 
		0 as [Level],
		null as PredicateID,
		null as PredicatePhrase,
		t.mapType as [Type],
		-1 as GroupNumber,
		'root' as [UID]
	from (select @type as [type], @id as [id], @mapType as mapType) t
	join cache.objectdetails d on d.[object] = t.[type] and d.objectid = t.id;

end;

declare @parent int;
select @parent = min([Level]) from @results;

--select * from @results;

 with z as
(
	select 
		m.id as ID,
		n1.objecttype as [Subject], 
		n1.objectid as SubjectID,
		n2.objecttype as [Object],
		n2.objectid as [ObjectID],
		d.ObjectType as ObjectType,
		d.ObjectTypeID as ObjectTypeID,
		cast(r.[UID] as varchar(max)) as ParentID,
		d.Name,
		cast(d.name as varchar(500)) as [Path],
		d.url as Url,
		d.objecttypename as ObjectTypeName, 
		1 as [Level],
		m.PredicateID as PredicateID,
		coalesce(p.Name,'') + '/' + coalesce(p.Inverse,'') as PredicatePhrase,
		m.[Type] as [Type],
		-1 as GroupNumber,
		cast((n1.objecttype + cast(n1.objectid as varchar(10)) + n2.objecttype + cast(n2.objectid as varchar(10))) as varchar(max)) as [UID]
	from intersectmap m
	join intersectnode n1 on n1.id = m.subjectintersectnodeid
	join intersectnode n2 on n2.id = m.objectintersectnodeid
	join cache.objectdetails d on d.object = n2.objecttype and d.objectid = n2.objectid
	join predicate p on p.id = m.predicateid
	join @results r on r.[subject] = n1.objecttype and r.subjectid = n1.objectid and r.[Level] = @parent
	where m.[type] = @mapType
	
	union all
	
	select 
		m.id as ID,
		n1.objecttype as [Subject], 
		n1.objectid as [SubjectID],
		n2.objecttype as [Object],
		n2.objectid as [ObjectID],
		d.ObjectType as ObjectType,
		d.ObjectTypeID as ObjectTypeID,
		z.[UID] as ParentID,
		d.Name, 
		cast(z.[path] + '/' + d.name as varchar(500)) as [Path],
		d.url as Url,
		d.objecttypename as ObjectTypeName, 
		z.[Level]+1 as [Level],
		m.PredicateID as PredicateID,
		coalesce(p.Name,'') + '/' + coalesce(p.Inverse,'') as PredicatePhrase,
		m.[Type] as [Type],
		-1 as GroupNumber,
		cast((z.UID + n2.objecttype + cast(n2.objectid as varchar(10))) as varchar(max)) as [UID]
	from intersectmap m
	join intersectnode n1 on n1.id = m.subjectintersectnodeid
	join intersectnode n2 on n2.id = m.objectintersectnodeid
	join cache.objectdetails d on d.object = n2.objecttype and d.objectid = n2.objectid
	join predicate p on p.id = m.predicateid
	join z on z.[Object] = n1.objecttype and z.[ObjectID] = n1.objectid
	where m.[type] = @mapType
)
insert into @results2
select distinct * from z;

insert into @results2
select 
	r.[id],
	r.[subject],
	r.[subjectid],
	r.[object],
	r.[objectid],
	r.[objecttype],
	r.[objecttypeid],
	null as [ParentID],
	r.[name],
	r.[path],
	r.[url],
	r.[objecttypename],
	0 as [level],
	r.[predicateid],
	r.[predicatephrase],
	r.[type],
	r.[groupnumber],
	r.[uid]
from @results r
where r.[Level] = @parent;

update @results2
set predicatephrase = reverse(stuff(reverse(predicatephrase),1,1,''))
where reverse(predicatephrase) like '/%';


select * from @results2 
order by [level] asc;

end
GO

ALTER PROCEDURE [dbo].[GetRenderedTemplateBody]
--declare
	@TemplateType varchar(25),
	@Type varchar(50),
	@ID int,
	@Action varchar(50)
--set @TemplateType = 'Lookup'
--set @Type = 'Artifact'
--set @ID = 7004--16435
--set @Action = 'Preview'--'Certificate'
AS
BEGIN
	SET NOCOUNT ON;

	declare @html nvarchar(max),
			@link nvarchar(2500),
			@icon nvarchar(250),
			@hasDynamicFields bit = 0,
			@hasStats bit = 0,
			@typeID int,

			@showIcon bit = 1,

			@current int,
			@max int,
			@name nvarchar(250),
			@value nvarchar(max);

	declare @tbl table (ID int identity, Name nvarchar(250), Value nvarchar(max));

	if @TemplateType = 'Email'
	begin
		select	@html = TemplateBody
		from	EmailTemplate
		where	Name = @Type
				and [Action] = @Action
	end

	if @TemplateType = 'Tooltip'
	begin
		select	@html = TemplateBody
		from	TooltipTemplate
		where	Name = @Type
				and [Action] = @Action
	end

	-- Get the static tokens, depending on the type.
	declare @n nvarchar(250), @t nvarchar(250), @s nvarchar(25), @v int, @dc datetime, @du datetime, @d nvarchar(4000);

	-- Get common fields
	select	@typeID = ObjectTypeID,
			@icon = '<div title=''' + ObjectTypeName + ''' class=''tooltip-icon'' style=''background-color: ' + IconBackColor + '; color: ' + IconForeColor + '''><i class=''fa fa-' + IconText + '''></i></div>',
			@n = Name,
			@t = ObjectTypeName,
			@d = Description,
			@link = Url
	from	cache.ObjectDetails
	where	[Object] = @Type
			and ObjectID = @ID;

	if @n is not null
	begin
		if @link is null
		begin
			insert into @tbl values ('Name', @n)
		end
		else
		begin
			insert into @tbl values ('Name', '<a href="' + @link + '">' + @n + '</a>')
		end
		insert into @tbl values ('Description', @d)
	end
	insert into @tbl values ('Type', @t)

	if @Action = 'AssigningItemPreview'
	begin
		set @html = '<h3>{Name}</h3>'
	end

	if @Action = 'Certificate'
	begin
		set @html = '<h3>{Name}</h3>'

		declare @workflowID uniqueidentifier,
				@dateCertifiedOn varchar(10),
				@certifiers nvarchar(2500),
				@status varchar(50),
				@certIconColor varchar(10)

		select	@dateCertifiedOn = CONVERT(VARCHAR(10), DateLastCertified, 101),
				@status = Status
		from	Artifact A
		where	A.ID = @ID

		SELECT	@workflowID = W.ID,
				@certifiers = COALESCE(@certifiers + ', ', '') + R.FirstName + ' ' + R.LastName 
		from	(
				select		top 1
							ID,
							Data.value('(/fields/ArtifactID)[1]', 'int') as ArtifactID,
							DateCompleted
				from		Workflow
				where		WorkflowType = 2
							and Data.exist('/fields/ArtifactID[text() = sql:variable("@ID")]') = 1
				order by	DateCompleted desc
				) W
				inner join WorkflowResource WR on WR.WorkflowID = W.ID
				inner join reporting.Global_Resource R on R.ResourceID = WR.ResourceID

		if @dateCertifiedOn is null
			begin
				set @showIcon = 0

				set @html = @html + '<div><b>Not yet certified</b></div>'
				if @certifiers is not null
				begin
					set @html = @html + '<div>Certifying Users: {Certifiers}</div>'
				end
				if @workflowID is not null
				begin
					set @html = @html + '<div><a class=''btn btn-info'' href=''#/workflow/' + cast(@workflowID as varchar(50)) + '/status''>Go to this workflow status</a>.</div>'
				end
			end
		else
			begin
				if @status = 'Certified'
					begin
						set @certIconColor = '#EFC43D'
					end
				else 
					begin
						set @certIconColor = '#FFE183'
					end
				select	@icon = '<div style="background-color: transparent; color: ' + @certIconColor + '"><i class="fa fa-2x fa-certificate"></i></div>'

				set @html = @html + '<div>Last Certified On: {CertifiedOn}</div>'
				if @status = 'Certified'
					begin
						set @html = @html + '<div>Certified By: {Certifiers}</div>'
					end
				else 
					begin
						set @html = @html + '<div>Currently Under Certification Review</div>'
						set @html = @html + '<div>Certifying Users: {Certifiers}</div>'
						if @workflowID is not null
						begin
							set @html = @html + '<div><a class=''btn btn-info'' href=''#/workflow/' + cast(@workflowID as varchar(50)) + '/status''>Go to this workflow status</a>.</div>'
						end
					end
			end

		insert into @tbl values ('CertifiedOn', @dateCertifiedOn)
		insert into @tbl values ('Certifiers', @certifiers)
	end
	if @Action = 'JoinRequest'
	begin
		set @html = ''
	end
	if @Action = 'LookupPreview'
	begin
		set @html = '{Items}'

		if @Type = 'Domain' OR @Type = 'DomainItem'
		begin
			declare @MyDomainID int
			if @Type = 'DomainItem'
				begin
					select @MyDomainID = DomainID from [DomainItem] where ID = @ID 
				end
			else
				begin
					set @MyDomainID = @ID
				end

			-- BUILD Domain LIST HTML -----------------------------------------
			declare @domainItemsHtml nvarchar(max)
			declare @HasDescription bit

			select @HasDescription = case Cnt 
										when 0 then 0
										else 1
									 end 
									 from (
											select count(1) as Cnt
											from	(
												select		top 10 
															[Description]
												from		DomainItem
												where		DomainID = @MyDomainID
															and [Description] is not null and [Description] <> ''
												order by	Name asc
												) D
											) D

			set @domainItemsHtml = '<table class="hoverable bordered striped" style="width:100%"><thead>'
			set @domainItemsHtml = @domainItemsHtml + '<th style="margin-right: 15px">Code</th><th style="margin-right: 15px">Name</th>'
			if @HasDescription = 1
			begin
				set @domainItemsHtml = @domainItemsHtml + '<th>Description</th>'
			end
			set @domainItemsHtml = @domainItemsHtml + '</thead><tbody>'

			select		top 10 
						@domainItemsHtml = @domainItemsHtml + '<tr>' + 
											'<td>' + Code + '</td>' + 
											'<td>' + Name + '</td>' + 
											case 
												when @HasDescription = 1 then 
													'<td>' + [Description] + '</td>'
												else ''
											end
											+ '</tr>'
			from		DomainItem
			where		DomainID = @MyDomainID
			order by	Name asc

			set @domainItemsHtml = @domainItemsHtml + '</tbody>'
			set @domainItemsHtml = @domainItemsHtml + '</table>'
 
			insert into @tbl values ('Items', @domainItemsHtml)
			------------------------------------------------------------------
		end;

		if @Type = 'DomainGroup'
		begin
			-- BUILD Domain LIST HTML -----------------------------------------
			declare @domainsHtml nvarchar(max)

			set @domainsHtml = '<table class="hoverable bordered striped" style="width:100%">'
			set @domainsHtml = @domainsHtml + '<thead><th style="margin-right: 15px">Name</th></thead>'
			set @domainsHtml = @domainsHtml + '<tbody>'

			select		top 10 
						@domainsHtml = @domainsHtml + '<tr>' + '<td>' + Name + '</td>' + '</tr>'             
			from		Domain
			where		DomainGroupID = @ID
			order by	Name desc

			set @domainsHtml = @domainsHtml + '</tbody>'
			set @domainsHtml = @domainsHtml + '</table>'
 
			insert into @tbl values ('Items', @domainsHtml)
			------------------------------------------------------------------
		end;

		if @Type = 'FusionAttribute'
		begin
			-- BUILD Domain LIST HTML -----------------------------------------
			declare @fusionAttributeItemsHtml nvarchar(max)

			set @fusionAttributeItemsHtml = '<div style="height: 200px; overflow-y: scroll"><table class="hoverable bordered striped" style="width:100%"><thead>'
			set @fusionAttributeItemsHtml = @fusionAttributeItemsHtml + '<th style="margin-right: 15px">Name</th>'
			set @fusionAttributeItemsHtml = @fusionAttributeItemsHtml + '</thead><tbody>'

			select		--top 10 
						@fusionAttributeItemsHtml = @fusionAttributeItemsHtml + '<tr>' 
											+ '<td>' + Name + '</td>'
											+ '</tr>'
			from		FusionAttribute
			where		ParentID = @ID
			order by	Name asc

			set @fusionAttributeItemsHtml = @fusionAttributeItemsHtml + '</tbody>'
			set @fusionAttributeItemsHtml = @fusionAttributeItemsHtml + '</table></div>'
 
			insert into @tbl values ('Items', @fusionAttributeItemsHtml)
			------------------------------------------------------------------
		end;

		if @Type = 'LookupType' OR @Type = 'Lookup'
		begin
			-- BUILD LOOKUP LIST HTML -----------------------------------------
			declare @lookups table (RowID int identity, ID int)

			declare @MyLookupTypeID int
			if @Type = 'Lookup'
				begin
					select @MyLookupTypeID = LookupTypeID from [Lookup] where ID = @ID 
				end
			else
				begin
					set @MyLookupTypeID = @ID
				end

			insert into @lookups 
				select top 10 ID from [Lookup] where LookupTypeID = @MyLookupTypeID order by ID desc
		
			declare @lookupFieldTypes table (ID int identity, Name nvarchar(250))
			insert into @lookupFieldTypes
				select FriendlyName from FieldType where [Object] = 'LookupType' and ObjectID = @MyLookupTypeID order by SortOrder asc

			declare @lookupHtml nvarchar(max)

			set @lookupHtml = '<table class="hoverable bordered striped" style="width:100%">'

			-- Loop through field name list ---------
			set @lookupHtml = @lookupHtml + '<thead>'
			set		@current = 1
			select	@max = max(ID) from @lookupFieldTypes
			while @current <= @max
			begin
				select	@name = Name
				from	@lookupFieldTypes
				where	ID = @current

				set @lookupHtml = @lookupHtml + '<th style="margin-right: 15px">' + @name  + '</th>'

				set @current = @current + 1
			end
			set @lookupHtml = @lookupHtml + '</thead>'
			-----------------------------------------

			set @lookupHtml = @lookupHtml + '<tbody>'

			-- Loop through event list --------------
			select	@current = min(RowID) from @lookups
			select	@max = max(RowID) from @lookups

			while @current <= @max
			begin
				set @lookupHtml = @lookupHtml + '<tr>'	-- Open row for selected event.

				declare @lookupFields table (Name nvarchar(250), Value nvarchar(4000))
			
				declare @lookupID int

				select	@lookupID = ID from @lookups where RowID = @current

				insert into @lookupFields
					select		FriendlyName,
								FormattedValue
					from		FieldWithRelation
					where		ObjectType = 'Lookup' 
								and ObjectID = @lookupID

					-- Loop through each field for this selected event --
					declare @lfCurrent int,
							@lfMax int
					set		@lfCurrent = 1
					select	@lfMax = max(ID) from @lookupFieldTypes
					while @lfCurrent <= @lfMax
					begin
						select	@name = Name from @lookupFieldTypes where ID = @lfCurrent

						select @lookupHtml = @lookupHtml + '<td>' + coalesce(Value, '') + '</td>' from @lookupFields where Name = @name

						set @lfCurrent = @lfCurrent + 1
					end
					-----------------------------------------------------

				delete @lookupFields

				set @lookupHtml = @lookupHtml + '</tr>'	-- Close off row for selected lookup.

				set @current = @current + 1
			end
			-----------------------------------------

			set @lookupHtml = @lookupHtml + '</tbody>'

			set @lookupHtml = @lookupHtml + '</table>'

			insert into @tbl values ('Items', @lookupHtml)
			------------------------------------------------------------------
		end;

		if @Type = 'Resource' OR @Type = 'ResourceType'
		begin
			-- BUILD Resource LIST HTML -----------------------------------------
			declare @resourceItemsHtml nvarchar(max)

			set @resourceItemsHtml = '<table class="hoverable bordered striped" style="width:100%"><thead>'
			set @resourceItemsHtml = @resourceItemsHtml + '<th style="margin-right: 15px">First Name</th><th style="margin-right: 15px">Last Name</th><th>Email</th>'
			set @resourceItemsHtml = @resourceItemsHtml + '</thead><tbody>'

			select		top 10 
						@resourceItemsHtml = @resourceItemsHtml + '<tr>' + 
											'<td>' + FirstName + '</td>' + 
											'<td>' + LastName + '</td>' + 
											'<td>' + Email + '</td>'
											+ '</tr>'
			from		reporting.Global_Resource
			order by	LastName, FirstName asc

			set @resourceItemsHtml = @resourceItemsHtml + '</tbody>'
			set @resourceItemsHtml = @resourceItemsHtml + '</table>'
 
			insert into @tbl values ('Items', @resourceItemsHtml)
			------------------------------------------------------------------
		end;

	end
	
	if @Action = 'None'
	begin
		set @html = '<h3>{Name}</h3><div>'
	end

	if @Action = 'Preview'
	begin
		set @html = '<h3>{Name} <small>{Type}</small></h3><div>{Description}</div>'
		set @showIcon = 0

		if @Type = 'Artifact'
		begin
			insert into @tbl
			select	'Status', [Status]
			from	Artifact
			where	ID = @ID

			insert into @tbl
			select	'Path', TextPath
			from	Artifact
			where	ID = @ID

			set @html = @html + '<div><b>Status:</b> {Status}</div>'
			set @html = @html + '<div><b>Path:</b> {Path}</div>'

			set @hasDynamicFields = 1
		end;

		if @Type = 'DomainGroup'
		begin
			select	@n = Name,
					@link = dbo.GenerateObjectUrl('DomainGroup', DomainTypeID, ID)
			from	DomainGroup
			where	ID = @ID

			insert into @tbl values ('Name', '<a href="' + @link + '">' + @n + '</a>')
		end;

		if @Type = 'Event'
		begin
			declare @so nvarchar(250)
			select	@so = SourceID, 
					@s = [Status]
			from	[Event]
			where	ID = @ID

			insert into @tbl values ('Status', @s)
			insert into @tbl values ('SourceID', @so)

			set @html = @html + '<div><b>Status:</b> {Status}</div>'
			set @html = @html + '<div><b>SourceID:</b> {SourceID}</div>'

			set @hasDynamicFields = 1
		end;

		if @Type = 'EventGroup'
		begin
			insert into @tbl
				select	'Key', PublicID
				from	EventGroup
				where	ID = @ID

			-- BUILD EVENT LIST HTML -----------------------------------------
			declare @events table (ID int, SourceID nvarchar(250), Status varchar(25))
			insert into @events 
				select top 10 ID, SourceID, Status from [Event] where EventGroupID = @ID order by ID desc
		
			declare @eventFieldTypes table (ID int identity, Name nvarchar(250))
			insert into @eventFieldTypes
				select FriendlyName from FieldType where [Object] = 'Rule' and ObjectID = @typeID order by SortOrder asc
			insert into @eventFieldTypes values ('Source ID')
			insert into @eventFieldTypes values ('Status')

			declare @eventHtml nvarchar(max)

			set @eventHtml = '<table class="hoverable bordered striped" style="width:100%">'

			-- Loop through field name list ---------
			set @eventHtml = @eventHtml + '<thead>'
			set		@current = 1
			select	@max = max(ID) from @eventFieldTypes
			while @current <= @max
			begin
				select	@name = Name
				from	@eventFieldTypes
				where	ID = @current

				set @eventHtml = @eventHtml + '<th>' + @name  + '</th>'

				set @current = @current + 1
			end
			set @eventHtml = @eventHtml + '</thead>'
			-----------------------------------------

			set @eventHtml = @eventHtml + '<tbody>'

			-- Loop through event list --------------
			select	@current = min(ID) from @events
			select	@max = max(ID) from @events

			while @current <= @max
			begin
				set @eventHtml = @eventHtml + '<tr>'	-- Open row for selected event.

				declare @eventFields table (Name nvarchar(250), Value nvarchar(4000))
			
				insert into @eventFields
					select		FriendlyName,
								FormattedValue
					from		FieldWithRelation
					where		ObjectType = 'Event' 
								and ObjectID = @current

					-- Loop through each field for this selected event --
					declare @fCurrent int,
							@fMax int
					set		@fCurrent = 1
					select	@fMax = max(ID) from @eventFieldTypes
					while @fCurrent <= @fMax
					begin
						select	@name = Name from @eventFieldTypes where ID = @fCurrent

						select @eventHtml = @eventHtml + '<td>' + coalesce(Value, '') + '</td>' from @eventFields where Name = @name

						set @fCurrent = @fCurrent + 1
					end
					-----------------------------------------------------

					select @eventHtml = @eventHtml	+ 
										'<td>' + [SourceID] + '</td>' + 
										'<td>' + [Status] + '</td>' 
					from	@events 
					where	ID = @current

				delete @eventFields

				set @eventHtml = @eventHtml + '</tr>'	-- Close off row for selected event.

				set @current = @current + 1
			end
			-----------------------------------------

			set @eventHtml = @eventHtml + '</tbody>'

			set @eventHtml = @eventHtml + '</table>'

			insert into @tbl values ('Items', @eventHtml)

			set @html = @html + '<div><b>Key:</b> {Key}</div>'
			set @html = @html + '<div>Items: {Items}</div>'
			------------------------------------------------------------------
		end;

		if @Type = 'Intersect'
		begin
			insert into @tbl
				select	'Classification',
						case Classification
							when 1 then 'Critical'
							else 'Normal'
						end
				from	[Intersect]
				where	ID = @ID

			--declare @innerHtml nvarchar(max)
			---- Loop through context list ---------
			--declare @contexts table (
			--	ID int identity,
			--	ObjectCode nvarchar(50), 
			--	ObjectName nvarchar(250), 
			--	ObjectDescription nvarchar(4000),
			--	ListName nvarchar(250), 
			--	TypeName nvarchar(250)
			--)

			--insert into @contexts 
			--	select	D.Code, D.Name, coalesce(D.Description, ''), L.Name, T.Name
			--	from	IntersectContextNode C
			--			inner join DomainItem D on C.ObjectType = 'DomainItem' and D.ID = C.ObjectID
			--			inner join Domain L on L.ID = D.DomainID
			--			inner join DomainType T on T.ID = L.DomainTypeID

			--set		@innerHtml = '<h2>Context:</h2>'
			--set		@current = 1
			--select	@max = max(ID) from @contexts
			--while @current <= @max
			--begin
			--	select	@innerHtml = @innerHtml + '<b>' + ListName + ' = ' + ObjectName + '</b><br/>' --+ '<div>' + ObjectDescription + '</div>'
			--	from	@contexts
			--	where	ID = @current

			--	set @current = @current + 1
			--end

			--insert into @tbl values ('Contexts', @innerHtml)
			-------------------------------------------

			set @html = @html + '<div><b>Classification:</b> {Classification}</div>'
		end;

		if @Type = 'Responsibility'
		begin
			select	@n = T.Name, 
					@t = T.Name,
					@d = T.[Description]
			from	Responsibility O
					inner join ResponsibilityType T on T.ID = O.ResponsibilityTypeID
			where	O.ID = @ID

			declare @contextsHtml nvarchar(max)
			set @contextsHtml = '<table class="hoverable bordered striped" style="width:100%">' + 
								'<thead><th>List</th><th>Code</th><th>Name</th><th>Description</th></thead>' + 
								'<tbody>' + 
								(
								select		(select D.Name as 'td' for xml path(''), type),
											(select I.Code as 'td' for xml path(''), type),
											(select I.Name as 'td' for xml path(''), type),
											(select I.[Description] as 'td' for xml path(''), type)
								from		ResponsibilityContextItem R
											inner join DomainItem I on R.ResponsibilityID = @ID and R.ObjectType = 'DomainItem' and I.ID = R.ObjectID
											inner join Domain D on D.ID = I.DomainID
								FOR XML RAW('tr'), ELEMENTS
								) +
								'</tbody>' + 
								'</table>'

			insert into @tbl values ('Name', @n)
			insert into @tbl values ('Type', @t)
			insert into @tbl values ('Description', @d)
			insert into @tbl values ('Contexts', @contextsHtml)

			set @html = @html + '<div><b>Contexts:</b> {Contexts}</div>'
		end;

		if @Type = 'Resource'
		begin
			--declare @e nvarchar(500), @fn nvarchar(250), @ln nvarchar(250)
			--select	@e = Email, @fn = FirstName, @ln = LastName
			--from	reporting.Global_Resource
			--where	ResourceID = @ID

			--insert into @tbl values ('Email', @e)
			--insert into @tbl values ('FirstName', @fn)
			--insert into @tbl values ('LastName', @ln)
			--insert into @tbl values ('Role', '')

			--set @html = @html + '<div><b>Email:</b> {Email}</div>'
			--set @html = @html + '<div><b>First Name:</b> {FirstName}</div>'
			--set @html = @html + '<div><b>Last Name:</b> {LastName}</div>'

			set @hasDynamicFields = 1
		end;

		if @Type = 'Rule'
		begin
			insert into @tbl
				select	'TextPath', TextPath
				from	Taxonomy O
				where	ID = @ID

			set @html = @html + '<div><b>Path:</b> {TextPath}</div>'

			set @hasDynamicFields = 1
		end;

		if @Type = 'Taxonomy'
		begin
			insert into @tbl
				select	'TextPath', TextPath
				from	Taxonomy O
				where	ID = @ID

			set @html = @html + '<div><b>Path:</b> {TextPath}</div>'

			set @hasDynamicFields = 1
		end;

		if @Type = 'TaxonomyType'
		begin
			insert into @tbl
				select	'Name', Name
				from	TaxonomyType O
				where	ID = @ID

			set @hasDynamicFields = 1
		end;

		-- If required, get dynamic fields to add to list.
		if @hasDynamicFields = 1
		begin
			select	@html = @html + '<div><b>' + FriendlyName + '</b>: ' + '{' + Name + '}' + '</div>' 
			from	FieldWithRelation
			where	ObjectType = @Type
					and ObjectID = @ID
					and Name not in (select Name from @tbl)

			insert into @tbl
				select	Name,
						FormattedValue
				from	FieldWithRelation
				where	ObjectType = @Type
						and ObjectID = @ID
						and Name not in (select Name from @tbl)
		end;
	end

	if @Action = 'Statistics'
	begin
		set @html = '<h3>{Name}</h3><div>{Statistics}</div>'

		set @hasStats = case @Type
							when 'Artifact' then 1
							when 'Domain' then 1
							when 'Taxonomy' then 1
							else 0
						end

		-- If required, build statistics table
		if @hasStats = 1
		begin
			-- BUILD STATS LIST HTML -----------------------------------------
			declare @statsHtml nvarchar(max)

			declare @stats table (ID int identity, Name nvarchar(250), Score int)
			insert into @stats 
				select	T.Name,
						coalesce(S.SCore, 0) as Score
				from	StatisticType T
						inner join StatisticTypeRelation R	on R.StatisticTypeID = T.ID
															and R.ObjectType = @Type + 'Type' 
															and R.ObjectID = @typeID
															and T.PartOfScore = 1
						outer apply (
									select	top 1
											*
									from	Statistic
									where	StatisticTypeID = T.ID
											and ObjectType = @Type
											and ObjectID = @ID
									order by DateStart desc
									) S

			set @statsHtml = '<table class="hoverable bordered striped" style="width:100%">'

			-- Loop through field name list ---------
			set @statsHtml = @statsHtml + '<tbody>'
			set		@current = 1
			select	@max = max(ID) from @stats
			while @current <= @max
			begin
				select	@statsHtml = @statsHtml + '<tr><td>' + Name  + '</td>' + '<td>' + cast(Score as varchar(5))  + ' Points</td></tr>'
				from	@stats
				where	ID = @current

				set @current = @current + 1
			end
			set @statsHtml = @statsHtml + '</tbody>'
			-----------------------------------------

			insert into @tbl values ('Statistics', @statsHtml)

			------------------------------------------------------------------
		end;
	end

	-- Replace the fields in the template with the appropriate text value.
	set		@current = 1
	select	@max = max(ID) from @tbl

	while @current <= @max
	begin
		select	@name = '{' + Name + '}',
				@value = COALESCE(Value, '')
		from	@tbl 
		where	ID = @current

		if @showIcon = 1
		begin
			if @name = '{Name}' and @icon is not null
			begin
				update	@tbl 
				set		Value = '<div class="pull-left" style="width: 30px">' + @icon + '</div>' + '<div class="pull-right">' + @value + '</div>'
				where	ID = @current
				--set @usedIconAlready = 1
			end
		end

		set @html = REPLACE(@html, @name, @value)

		set @current = @current + 1
	end

	--if @showIcon = 1 and @icon is not null
	--begin
	--	set @html = @icon + '<br/>' + @html
	--end

	-- Return the properly formatted values.
	select	'' as Title,
			@html as Body;
END
go

alter procedure [dbo].[ProcessBulkLoad]
--declare
	@LoadID int
--set @LoadID = 4
as
begin
	set nocount on;

	declare @Object varchar(50),
			@ObjectID int,
			@Action varchar(1),
			@UpdatedBy int = 0

	select	@Object = [Object],
			@ObjectID = ObjectID,
			@Action = [Action],
			@UpdatedBy = UpdatedBy
	from	[Load]
	where	ID = @LoadID

	if @Action = 'P'	--PROMOTION
	begin
		-- PARSE any dynamic fields that are specifically lookups.
		update	T
		set		T.LookupObject = S.LookupObject,
				T.LookupObjectID = S.LookupObjectID
		from	LoadItemColumn T
				inner join	(
							select	IC.LoadID,
									IC.RowIndex,
									IC.ColumnIndex,
									case 
										when L_A.ID is not null then 'Artifact'
										when L_D.ID is not null then 'Domain'
										when L_DI.ID is not null then 'DomainItem'
										when L_F.ID is not null then 'FusionAttribute'
										when L_I.ID is not null then 'Intersect'
										when L_L.Value is not null then 'Lookup'
										when L_T.ID is not null then 'Taxonomy'
										else NULL
									end as LookupObject,
									coalesce(L_A.ID, L_D.ID, L_DI.ID, L_F.ID, L_I.ID, L_L.Value, L_T.ID) as LookupObjectID
							from	FieldType F
									inner join [Load] L on L.ID = @LoadID and L.[Object] = F.[Object] and L.ObjectID = F.ObjectID and F.[Type] = 'Lookup'
									inner join [LoadColumn] C on C.LoadID = L.ID and F.Name = C.Name
									inner join [LoadItemColumn] IC on IC.LoadID = C.LoadID and IC.ColumnIndex = C.ColumnIndex
								
									left join Artifact L_A on F.LookupObjectType in ('Artifact', 'ArtifactType') and L_A.ArtifactTypeID = F.LookupObjectID and (L_A.[Name] = IC.Value OR L_A.TextPath = IC.Value)
									left join Domain L_D on F.LookupObjectType in ('Domain', 'DomainType') and L_D.DomainTypeID = F.LookupObjectID and L_D.[Name] = IC.Value
									left join DomainItem L_DI on F.LookupObjectType = 'DomainItem' and L_DI.DomainID = F.LookupObjectID and L_DI.[Name] = IC.Value
									left join FusionAttribute L_F on F.LookupObjectType = 'FusionAttributeType' and L_F.FusionAttributeTypeID = F.LookupObjectID and (L_F.[Name] = IC.Value OR L_F.TextPath = IC.Value)
									left join [Intersect] L_I on F.LookupObjectType = 'IntersectType' and L_I.IntersectTypeID = F.LookupObjectID and L_I.[Name] = IC.Value
									left join [FieldLookupValue] L_L on F.ID = L_L.FieldTypeID and F.LookupObjectType = 'Lookup' and L_L.LookupObjectID = F.LookupObjectID and L_L.[Text] = IC.Value
									left join Taxonomy L_T on F.LookupObjectType in ('Taxonomy', 'TaxonomyType') and L_T.TaxonomyTypeID = F.LookupObjectID and (L_T.[Name] = IC.Value OR L_T.TextPath = IC.Value)
							where	F.[Type] = 'Lookup'
							) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex

		-- PARSE any Subject AREA fields.  This is only in the case of artifacts.
		update	T
		set		T.LookupObject = S.LookupObject,
				T.LookupObjectID = S.LookupObjectID
		from	LoadItemColumn T
				inner join	(
							select	IC.LoadID,
									IC.RowIndex,
									IC.ColumnIndex,
									'TaxonomyType' as LookupObject,
									T.ID as LookupObjectID
							from	[Load] L 
									inner join [LoadColumn] C on L.ID = @LoadID and L.[Object] = 'ArtifactType' and C.LoadID = L.ID and C.Name = 'Subject Area'
									inner join [LoadItemColumn] IC on IC.LoadID = C.LoadID and IC.ColumnIndex = C.ColumnIndex
									inner join TaxonomyType T on T.[Name] = IC.Value
							) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex

		-- PARSE any Domain Group fields.  This is only in the case of domains.
		update	T
		set		T.LookupObject = S.LookupObject,
				T.LookupObjectID = S.LookupObjectID
		from	LoadItemColumn T
				inner join	(
							select	IC.LoadID,
									IC.RowIndex,
									IC.ColumnIndex,
									'DomainGroup' as LookupObject,
									T.ID as LookupObjectID
							from	[Load] L 
									inner join [LoadColumn] C on L.ID = @LoadID and L.[Object] = 'DomainType' and C.LoadID = L.ID and C.Name = 'Domain Group'
									inner join [LoadItemColumn] IC on IC.LoadID = C.LoadID and IC.ColumnIndex = C.ColumnIndex
									inner join DomainGroup T on T.[Name] = IC.Value and T.DomainTypeID = @ObjectID
							) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex

		-- PARSE any Parent Artifact fields.  This is only in the case of artifacts.
		update	T
		set		T.LookupObject = S.LookupObject,
				T.LookupObjectID = S.LookupObjectID
		from	LoadItemColumn T
				inner join	(
							select	IC.LoadID,
									IC.RowIndex,
									IC.ColumnIndex,
									'Artifact' as LookupObject,
									P.ID as LookupObjectID
							from	[Load] L 
									inner join ArtifactType T on L.ID = @LoadID and L.[Object] = 'ArtifactType' and L.ObjectID = T.ID
									inner join ArtifactType PT on PT.ID = T.ParentID
									inner join [LoadColumn] C on C.LoadID = L.ID and C.Name = 'Parent ' + PT.Name
									inner join [LoadItemColumn] IC on IC.LoadID = C.LoadID and IC.ColumnIndex = C.ColumnIndex
									inner join Artifact P on P.ArtifactTypeID = PT.ID and P.[Name] = IC.Value
							) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex

		if @Object = 'AttributeType'
		begin
			-- Clean Owner Type field.
			update	LoadItemColumn
			set		Value = case when charindex('Type', Value) > 0 then Value else Value + 'Type' end
			where	LoadID = @LoadID and ColumnIndex = 1

			-- PARSE Owner Type fields.
			update	T
			set		T.LookupObject = S.LookupObject,
					T.LookupObjectID = S.LookupObjectID
			from	LoadItemColumn T
					inner join	(
								select	LI.LoadID,
										LI.RowIndex,
										C2.ColumnIndex,
										D.[Object] as LookupObject,
										D.ObjectID as LookupObjectID
								from	[Load] L
										inner join LoadItem LI on LI.LoadID = L.ID and L.ID = @LoadID
										inner join [LoadItemColumn] C1 on C1.LoadID = LI.LoadID and C1.RowIndex = LI.RowIndex and C1.ColumnIndex = 1 --'Owner Type' 
										inner join [LoadItemColumn] C2 on C2.LoadID = LI.LoadID and C2.RowIndex = LI.RowIndex and C2.ColumnIndex = 2 --'Owner Type Name'
										inner join cache.ObjectDetails D on D.[Object] = C1.Value and D.[Name] = C2.Value
								) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex

			-- PARSE Owner fields.
			update	T
			set		T.LookupObject = S.LookupObject,
					T.LookupObjectID = S.LookupObjectID
			from	LoadItemColumn T
					inner join	(
								select	LI.LoadID,
										LI.RowIndex,
										C3.ColumnIndex,
										D.[Object] as LookupObject,
										D.ObjectID as LookupObjectID
								from	[Load] L
										inner join LoadItem LI on LI.LoadID = L.ID and L.ID = @LoadID
										--inner join [LoadItemColumn] C1 on	C1.LoadID = LI.LoadID	and C1.RowIndex = LI.RowIndex	and C1.ColumnIndex = 1 --'Owner Type' 
										inner join [LoadItemColumn] C2 on C2.LoadID = LI.LoadID and C2.RowIndex = LI.RowIndex and C2.ColumnIndex = 2 --'Owner Type Name'
										inner join [LoadItemColumn] C3 on C3.LoadID = LI.LoadID	and C3.RowIndex = LI.RowIndex and C3.ColumnIndex = 3 --'Owner Name'
										inner join cache.ObjectDetails D on D.[ObjectType] = C2.[LookupObject] and D.ObjectTypeID = C2.LookupObjectID and D.[Name] = C3.Value
								) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex
		end

		declare @ResolvedObjects table ([Object] varchar(50), ObjectID int, [Action] varchar(25), LoadID int, RowIndex int)	--This captures the INSERTED/UPDATED objects from the merge statements below.

		if @Object = 'ArtifactType'
		begin
			merge	Artifact T
			using	(
					select	O.LoadID,
							O.RowIndex,
							O.ArtifactTypeID,
							O.Name,
							D.Description,
							O.ParentID,
							O.TaxonomyTypeID
					from	(
							select	LI.LoadID,
									MIN(LI.RowIndex) as RowIndex,
									@ObjectID as ArtifactTypeID,
									IC_N.Value as Name,
									P.ParentID,
									IC_T.LookupObjectID as TaxonomyTypeID
							from	[LoadItem] LI
									inner join [LoadItemColumn] IC_N on IC_N.LoadID = LI.LoadID and IC_N.RowIndex = LI.RowIndex inner join LoadColumn C_N on C_N.LoadID = LI.LoadID and C_N.ColumnIndex = IC_N.ColumnIndex and C_N.Name = 'Name'
									inner join [LoadItemColumn] IC_T on IC_T.LoadID = LI.LoadID and IC_T.RowIndex = LI.RowIndex inner join LoadColumn C_T on C_T.LoadID = LI.LoadID and C_T.ColumnIndex = IC_T.ColumnIndex and C_T.Name = 'Subject Area' and IC_T.LookupObjectID is not null
									outer apply (
												select	I.LookupObjectID as ParentID
												from	[LoadItemColumn] I
														inner join LoadColumn C on I.LoadID = LI.LoadID and I.RowIndex = LI.RowIndex 
																						and C.LoadID = LI.LoadID and C.ColumnIndex = I.ColumnIndex and C.Name like 'Parent %'
												) P
							where	LI.LoadID = @LoadID
							group by LI.LoadID,
									IC_N.Value,
									P.ParentID,
									IC_T.LookupObjectID
							) O
							outer apply (
								select	I.Value as Description
								from	[LoadItemColumn] I
										inner join LoadColumn C on I.LoadID = O.LoadID and I.RowIndex = O.RowIndex 
																		and C.LoadID = O.LoadID and C.ColumnIndex = I.ColumnIndex and C.Name = 'Description'
							) D
					) S
			on		(T.ArtifactTypeID = S.ArtifactTypeID and T.TaxonomyTypeID = S.TaxonomyTypeID and T.ParentID = S.ParentID and T.Name = S.Name)
			when	matched then
					update	set T.[Description] = S.[Description],
								T.[ParentID] = S.[ParentID],
								T.[Status] = 'Draft',
								T.TaxonomyTypeID = S.TaxonomyTypeID,
								T.UpdatedBy = @UpdatedBy,
								T.UpdatedOn = getutcdate()
			when	not matched then
					insert (ArtifactTypeID, TaxonomyTypeID, ParentID, Name, [Description], [Status], UpdatedOn, UpdatedBy)
					values (S.ArtifactTypeID, S.TaxonomyTypeID, S.ParentID, S.Name, S.[Description], 'Draft', getutcdate(), @UpdatedBy)
			output	'Artifact', inserted.ID, $action, S.LoadID, S.RowIndex into @ResolvedObjects;
		end
		else if @Object = 'AttributeType'
		begin
			merge	[Attribute] T
			using	(
					select	I.LoadID,
							I.RowIndex,
							@ObjectID as AttributeTypeID,
							C.LookupObject as [Object],
							C.LookupObjectID as ObjectID
					from	[LoadItem] I
							inner join [LoadItemColumn] C on I.LoadID = @LoadID and C.LoadID = I.LoadID and C.RowIndex = I.RowIndex and C.ColumnIndex = 3
							and C.LookupObject is not null
							and C.LookupObjectID is not null
					) S
			on		(T.AttributeTypeID = S.AttributeTypeID and T.[ObjectType] = S.[Object] and T.[ObjectID] = S.[ObjectID] and T.ParentID = NULL)-- and T.Name = S.Name)
			when	matched then
					update	set T.[UpdatedOn] = getutcdate(),
								T.UpdatedBy = @UpdatedBy
			when	not matched then
					insert (AttributeTypeID, ObjectType, ObjectID, UpdatedOn, UpdatedBy)
					values (S.AttributeTypeID, S.[Object], S.ObjectID, getutcdate(), @UpdatedBy)
			output	'Attribute', inserted.ID, $action, S.LoadID, S.RowIndex into @ResolvedObjects;		
		end
		else if @Object = 'Domain'
		begin
			merge	DomainItem T
			using	(
					select	distinct
							LI.LoadID,
							LI.RowIndex,
							@ObjectID as DomainID,
							IC_C.Value as Code,
							IC_N.Value as Name,
							D.[Description]
					from	[LoadItem] LI
							inner join [LoadItemColumn] IC_C on IC_C.LoadID = LI.LoadID and IC_C.RowIndex = LI.RowIndex inner join LoadColumn C_C on C_C.LoadID = LI.LoadID and C_C.ColumnIndex = IC_C.ColumnIndex and C_C.Name = 'Code'
							inner join [LoadItemColumn] IC_N on IC_N.LoadID = LI.LoadID and IC_N.RowIndex = LI.RowIndex inner join LoadColumn C_N on C_N.LoadID = LI.LoadID and C_N.ColumnIndex = IC_N.ColumnIndex and C_N.Name = 'Name'
							outer apply (
										select	I.Value as Description
										from	[LoadItemColumn] I
												inner join LoadColumn C on I.LoadID = LI.LoadID and I.RowIndex = LI.RowIndex 
																			 and C.LoadID = LI.LoadID and C.ColumnIndex = I.ColumnIndex and C.Name = 'Description'
										) D
					where	LI.LoadID = @LoadID
					) S
			on		(T.DomainID = S.DomainID and T.Code = S.Code)
			when	matched then
					update	set T.[Name] = S.[Name],
								T.[Description] = S.[Description],
								T.[DomainID] = S.[DomainID],
								T.UpdatedBy = @UpdatedBy,
								T.UpdatedOn = getutcdate()
			when	not matched then
					insert (DomainID, Code, Name, [Description], UpdatedOn, UpdatedBy)
					values (S.DomainID, S.Code, S.Name, S.[Description], getutcdate(), @UpdatedBy)
			output	'DomainItem', inserted.ID, $action, S.LoadID, S.RowIndex into @ResolvedObjects;
		end
		else if @Object = 'DomainType'
		begin
			merge	Domain T
			using	(
					select	distinct
							LI.LoadID,
							LI.RowIndex,
							@ObjectID as DomainTypeID,
							IC_N.Value as Name,
							D.[Description],
							IC_G.LookupObjectID as DomainGroupID
					from	[LoadItem] LI
							inner join [LoadItemColumn] IC_N on IC_N.LoadID = LI.LoadID and IC_N.RowIndex = LI.RowIndex inner join LoadColumn C_N on C_N.LoadID = LI.LoadID and C_N.ColumnIndex = IC_N.ColumnIndex and C_N.Name = 'Name'
							outer apply (
										select	I.Value as Description
										from	[LoadItemColumn] I
												inner join LoadColumn C on I.LoadID = LI.LoadID and I.RowIndex = LI.RowIndex 
																			 and C.LoadID = LI.LoadID and C.ColumnIndex = I.ColumnIndex and C.Name = 'Description'
										) D
							inner join [LoadItemColumn] IC_G on IC_G.LoadID = LI.LoadID and IC_G.RowIndex = LI.RowIndex inner join LoadColumn C_G on C_G.LoadID = LI.LoadID and C_G.ColumnIndex = IC_G.ColumnIndex and C_G.Name = 'Domain Group'
					where	LI.LoadID = @LoadID
					) S
			on		(T.DomainTypeID = S.DomainTypeID and T.Name = S.Name)
			when	matched then
					update	set T.[Description] = S.[Description],
								T.[DomainGroupID] = S.[DomainGroupID],
								T.UpdatedOn = getutcdate(),
								T.UpdatedBy = @UpdatedBy
			when	not matched then
					insert (DomainTypeID, DomainGroupID, Name, [Description], UpdatedOn, UpdatedBy)
					values (S.DomainTypeID, S.DomainGroupID, S.Name, S.[Description], getutcdate(), @UpdatedBy)
			output	'Domain', inserted.ID, $action, S.LoadID, S.RowIndex into @ResolvedObjects;
		end
		else if @Object = 'FusionAttributeType'
		begin
			select 1;
		end
		else if @Object = 'TaxonomyType'
		begin
		--begin tran

			declare @currentLevel int,
			@maxLevel int,
			@rowCount int,
			@rowCurr int;

			select 
				@currentLevel = 0
				,@maxLevel = max(
					case when isnumeric(replace(Name,'Level','')) = 1 then
						replace(Name,'Level','') 
					else 
						0 
					end) 
			from 
				LoadColumn 
			where 
				LoadID = @LoadID and Name like 'Level%';
			

			declare @levels table (id int, ColumnIndex int, RowIndex int, [Level] varchar(50), Value varchar(250),MaxLevel int, TaxonomyID int, ParentID int, [Status] varchar(50));
			with v as
			(
				select L.ID, L.Object, L.ObjectID, LC.Name, LC.ColumnIndex, IC.RowIndex, IC.Value, replace(LC.Name,'Level','') as [Level], T.ID as TaxonomyID from [Load] L
				join LoadColumn LC on LC.LoadID = L.ID
				join LoadItemColumn IC on IC.LoadID = LC.LoadID AND IC.ColumnIndex = LC.ColumnIndex
				left join Taxonomy T on T.TaxonomyTypeID = L.ObjectID and T.[Level] = replace(LC.Name,'Level','') and T.Name = IC.Value
				where L.ID = @LoadID AND ltrim(rtrim(IC.Value)) != '' and LC.Name like 'Level%'  
			)
			insert into @levels
			select distinct
				row_number() over (partition by 1 order by v.[Level]) as ID,
				v.ColumnIndex
				,v.RowIndex
				,v.[Level]
				,v.Value
				,m.[Level] as MaxLevel
				,v.TaxonomyID
				,p.TaxonomyID as ParentID 
				,'UPDATE' as [Status]
			from v
			left join v p 
				on p.RowIndex = v.RowIndex and v.TaxonomyID is null and p.ColumnIndex = (v.ColumnIndex - 1)
			inner join v m on m.RowIndex = v.RowIndex and m.[Level] = (select max([Level]) from v where RowIndex = m.RowIndex)
			order by v.[Level] asc;

			--calculate hierarchy
			while @currentLevel <= @maxLevel
			begin
				set @currentLevel = @currentLevel + 1;
				
				update LV
				set LV.ParentID = P.ID
				from @levels LV
				left join @levels P on P.[Level] = (LV.[Level] - 1) AND LV.RowIndex = P.RowIndex
				where LV.[Level] = @currentLevel;
			end 

			--delete records that have a level > 1 and no parentid, missing info
			--delete from @levels where parentid is null and level > 1;

			select @rowCurr = 0, @rowCount = count(*) from @levels;

			while @rowCurr <= @rowCount
			begin
				set @rowCurr = @rowCurr + 1;

				--parent does not exist or leading columns were not filled
				if (select ParentID from @levels where id = @rowCurr) IS NULL AND (select Level from @levels where id = @rowCurr) > 1
				begin
					update @levels set [Status] = 'ERROR' where rowIndex = (select rowindex from @levels where id = @rowCurr);
					continue;
				end


				--update the TaxonomyID for records that do not yet have it
				if (select level from @levels where id = @rowCurr) = 1
				begin
					update LV
					set TaxonomyID = T.ID
					from @levels LV
					join Load L on L.ID = @LoadID
					join Taxonomy T on T.Name = LV.Value and T.ParentID is NULL and T.Level = LV.Level and T.TaxonomyTypeID = L.ObjectID
					where LV.ID = @rowCurr;
				end
				else
				begin
					update LV
					set TaxonomyID = T.ID
					from @levels LV
					left join @levels P on P.ID = LV.ParentID
					join Taxonomy T on T.Name = LV.Value and T.ParentID = P.TaxonomyID and T.Level = LV.Level
					where LV.ID = @rowCurr;
				end

				if (select TaxonomyID from @levels where id = @rowCurr) IS NULL
				begin
					--insert the new taxonomy
					insert into Taxonomy (TaxonomyTypeID, ParentID, Name, [Description], UpdatedOn, UpdatedBy)
					select	distinct
							L.ObjectID as TaxonomyTypeID
						,LVP.TaxonomyID as ParentID
						,LV.Value as Name
						,case when LV.Level = LV.MaxLevel then
							LI.Value
						else
							''
						END as Description
						,getdate() as UpdatedOn
						,@UpdatedBy as UpdatedBy
					from 
						@levels LV
					left join @levels LVP on LVP.ID = LV.ParentID
					join [Load] L on L.ID = @LoadID
					inner join LoadColumn LC on LC.Name = 'Description' and LC.LoadID = @LoadID
					inner join LoadItemColumn LI on LI.RowIndex = LV.RowIndex AND LI.ColumnIndex = LC.ColumnIndex AND LI.LoadID = @LoadID
					where
						LV.ID = @rowCurr

					update @levels set [Status] = 'INSERT' where id = @rowCurr;

					--set the levels taxonomy id after insert
					update LV
					set TaxonomyID = T.ID
					from @levels LV
					left join @levels P on P.ID = LV.ParentID
					join Taxonomy T on T.Name = LV.Value and coalesce(T.ParentID,-1) = coalesce(P.TaxonomyID,-1) and T.Level = LV.Level
					where LV.ID = @rowCurr;
				end
				
				--if level = max, update the description
				if (select level from @levels where id = @rowCurr) = (select maxlevel from @levels where id = @rowCurr)
				begin
					update	T
					set		T.Description = case when LI.Value = '' then T.Description else LI.Value end,
							T.UpdatedOn = getutcdate(),
							T.UpdatedBy = @UpdatedBy
					from	Taxonomy T
							join @levels LV on LV.ID = @rowCurr and T.ID = LV.TaxonomyID
							inner join LoadColumn LC on LC.Name = 'Description' and LC.LoadID = @LoadID
							inner join LoadItemColumn LI on LI.RowIndex = LV.RowIndex AND LI.ColumnIndex = LC.ColumnIndex AND LI.LoadID = @LoadID;

				end
			end --end while
			

			--remove error rows
			delete from @levels
			where rowindex in (select rowindex from @levels where status is null or status = 'ERROR');

						--insert object statuses
			insert into @ResolvedObjects ([Object], ObjectID, [Action], LoadID, RowIndex)
			select
				'Taxonomy',
				TaxonomyID,
				[Status],
				@LoadID,
				RowIndex
			from 
			@levels;

		end

		-- Update the LoadItem table with the IDs we recieved in the merge statements above.
		update	T
		set		T.[Object] = S.[Object],
				T.ObjectID = S.ObjectID,
				T.[Status] = 1,
				T.StatusMessage = case S.[Action]
									when 'INSERT' then 'Added item'
									when 'UPDATE' then 'Updated item'
									else NULL
									end
		from	LoadItem T
				inner join	@ResolvedObjects S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex

		-- Update the LoadItems that were not successfully added or updated.
		update	LoadItem
		set		[Status] = 0,
				[StatusMessage] = 'Item could not be added nor updated.'
		where	[ObjectID] is null
		
		-- Load custom fields for the inserted/updated object above.
		merge	Field T
		using	(
				select	distinct
						FT.ID as FieldTypeID,
						L.[Object],
						L.ObjectID,
						IC.LookupObjectID--max(IC.LookupObjectID) as LookupObjectID
				from	LoadItem L
						inner join LoadColumn C on C.LoadID = L.LoadID
						inner join LoadItemColumn IC on IC.LoadID = C.LoadID and L.RowIndex = IC.RowIndex and IC.ColumnIndex = C.ColumnIndex and IC.LookupObjectID is not null
						inner join FieldType FT on FT.[Object] = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name
				where	L.ObjectID is not null
						and L.LoadID = @LoadID
				--group by	FT.ID,
				--			L.[Object],
				--			L.ObjectID
				) S
		on		(T.FieldTypeID = S.FieldTypeID and T.ObjectType = S.[Object] and T.ObjectID = S.ObjectID)
		when	matched then
				update	set Value = S.LookupObjectID
		when	not matched then
				insert (ObjectType, ObjectID, FieldTypeID, Value)
				values (S.[Object], S.ObjectID, S.FieldTypeID, S.LookupObjectID);

		merge	Field T
		using	(
				select	distinct
						FT.ID as FieldTypeID,
						L.[Object],
						L.ObjectID,
						case 
							when FT.[Type] = 'Boolean' and LOWER(IC.Value) in ('y', 'yes', 'true', 't', '1') then 'true'
							when FT.[Type] = 'Boolean' and LOWER(IC.Value) not in ('y', 'yes', 'true', 't', '1') then 'false'
							else IC.Value
						end as Value
				from	LoadItem L
						inner join LoadColumn C on C.LoadID = L.LoadID
						inner join LoadItemColumn IC on IC.LoadID = C.LoadID and L.RowIndex = IC.RowIndex and IC.ColumnIndex = C.ColumnIndex and IC.LookupObjectID is null
						inner join FieldType FT on FT.[Object] = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name and FT.[Type] <> 'Lookup'
				where	L.ObjectID is not null
						and L.LoadID = @LoadID
				) S
		on		(T.FieldTypeID = S.FieldTypeID and T.ObjectType = S.[Object] and T.ObjectID = S.ObjectID)
		when	matched then
				update	set Value = S.Value
		when	not matched then
				insert (ObjectType, ObjectID, FieldTypeID, Value)
				values (S.[Object], S.ObjectID, S.FieldTypeID, S.Value);
	end
	else
	begin
		-- This is for actions: R, U
		declare @current int,
				@max int,
				@sourceObject varchar(50),
				@sourceObjectID int,
				@sourceIntersectTypeNodeID int,
				@targetObject varchar(50),
				@targetObjectID int,
				@targetIntersectTypeNodeID int,
				@intersectID int = null,
				@date datetime = getutcdate()

		declare @Intersects IDTable

		if @Action = 'R' OR @Action = 'U'	--UNRELATION (Remove existing relation)
		begin
			-- PARSE both sides.
			update	T
			set		T.LookupObject = S.LookupObject,
					T.LookupObjectID = S.LookupObjectID
			from	LoadItemColumn T
					inner join	(
								select	IC.LoadID,
										IC.RowIndex,
										IC.ColumnIndex,
										T.[Object] as LookupObject,
										T.ObjectID as LookupObjectID
								from	[Load] L
										inner join [LoadColumn] C on C.LoadID = L.ID and L.ID = @LoadID
										inner join [LoadItemColumn] IC on IC.LoadID = C.LoadID and IC.ColumnIndex = C.ColumnIndex
										inner join IntersectTypeNode IT on IT.IntersectTypeID = @ObjectID and IT.[Order] = IC.[ColumnIndex]
										inner join cache.ObjectDetails T on T.[TextPath] = IC.Value and T.[ObjectType] = IT.[ObjectType] and T.ObjectTypeID = IT.ObjectID
								) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex
			update	T
			set		T.[Status] = 0,
					T.StatusMessage =	REPLACE(REPLACE(
											STUFF(
											(
											select	LIC.Value + ' could not be located in the <a href="' + T.Url + '">' + T.Name + '</a> list, '
											from	[Load] L
													inner join [IntersectTypeNode] ITN on ITN.IntersectTypeID = L.ObjectID and L.ID = @LoadID
													inner join [LoadItemColumn] LIC on LIC.LoadID = L.ID and LIC.ColumnIndex = ITN.[Order] and LIC.ColumnIndex = IC.ColumnIndex and LIC.RowIndex = IC.RowIndex and LIC.LookupObject is null
													inner join cache.ObjectDetails T on T.[Object] = ITN.[ObjectType] and T.ObjectID = ITN.ObjectID
											for xml path('')
											), 1, 0, ''),
										'&lt;', '<'), '&gt;', '>')
			from	[LoadItem] T
					inner join [LoadItemColumn] IC on T.LoadID = @LoadID and IC.LoadID = T.LoadID and IC.RowIndex = T.RowIndex and IC.LookupObject IS NULL and IC.LookupObjectID is null

			select	@current = min(I.RowIndex),
					@max = max(I.RowIndex)
			from	LoadItem I
					inner join LoadItemColumn S on S.LoadID = I.LoadID and S.RowIndex = I.RowIndex and S.ColumnIndex = 1 and S.LookupObject is not null
					inner join LoadItemColumn T on T.LoadID = I.LoadID and T.RowIndex = I.RowIndex and T.ColumnIndex = 2 and T.LookupObject is not null
			where	I.LoadID = @LoadID



		end

		while @current <= @max
		begin
			select	@sourceObject = S.LookupObject,
					@sourceObjectID = S.LookupObjectID,
					@targetObject = T.LookupObject,
					@targetObjectID = T.LookupObjectID
			from	LoadItem I
					inner join LoadItemColumn S on S.LoadID = I.LoadID and S.RowIndex = I.RowIndex and S.ColumnIndex = 1 and S.LookupObject is not null
					inner join LoadItemColumn T on T.LoadID = I.LoadID and T.RowIndex = I.RowIndex and T.ColumnIndex = 2 and T.LookupObject is not null
			where	I.LoadID = @LoadID and I.RowIndex = @current

			set		@intersectID = null

			select	@IntersectID = SN.IntersectID 
			from	[IntersectNode] SN 
					inner join IntersectNode TN on	SN.IntersectID = TN.IntersectID 
													and SN.ID <> TN.ID 
													and SN.ObjectType = @sourceObject 
													and SN.ObjectID = @sourceObjectID 
													and TN.ObjectType = @targetObject 
													and TN.ObjectID = @targetObjectID
			if @Action = 'R'	--RELATION
			begin
				if @intersectID is null
				begin
					-- Get the node type IDs
					select	@sourceIntersectTypeNodeID = S.ID,
							@targetIntersectTypeNodeID = T.ID
					from	IntersectTypeNode S 
							inner join IntersectTypeNode T on S.IntersectTypeID = T.IntersectTypeID and S.[Order] = 1 and T.[Order] = 2 and S.ID <> T.ID and S.IntersectTypeID = @ObjectID

					insert into [Intersect] (IntersectTypeID, Classification) values (@ObjectID, 2)
					set @intersectID = SCOPE_IDENTITY()

					insert into [IntersectNode] (IntersectTypeNodeID, IntersectID, ObjectType, ObjectID) 
					values						(@sourceIntersectTypeNodeID, @intersectID, @sourceObject, @sourceObjectID)
					insert into [IntersectNode] (IntersectTypeNodeID, IntersectID, ObjectType, ObjectID) 
					values						(@targetIntersectTypeNodeID, @intersectID, @targetObject, @targetObjectID)

					exec utility.AddAuditEntry @sourceObject, @sourceObjectID, 0, @date, 'Created', 'Intersect', @intersectID
					exec utility.AddAuditEntry @targetObject, @targetObjectID, 0, @date, 'Created', 'Intersect', @intersectID
				end

				if @intersectID is not null
				begin
					update	LoadItem
					set		[Object] = 'Intersect',
							ObjectID = @intersectID,
							[Status] = 1,
							StatusMessage = 'Successfully created/updated relationship'
					where	LoadID = @LoadID
							and RowIndex = @current
				end
				else
				begin
					update	LoadItem
					set		[Status] = 0,
							StatusMessage = 'Failed to create relationship'
					where	LoadID = @LoadID
							and RowIndex = @current
				end
			end --end R

			if @Action = 'U'	--UNRELATION
			begin
				if @intersectID is not null
				begin
					begin try
						if exists(	select 1 
									from	[cache].[Relationships] SR
											inner join Responsibility RE on RE.ResponsibleObjectType = SR.SourceObject and RE.ResponsibleObjectID = SR.SourceObjectID
											inner join [cache].[Relationships] TR on RE.ObjectType = 'Intersect' and RE.ObjectID = TR.IntersectID and TR.TargetObject = SR.TargetObject and TR.TargetObjectID = SR.TargetObjectID
									where	SR.IntersectID = @intersectID
								 )
						begin
							DECLARE @Targets VARCHAR(8000) 
							SELECT	@Targets = COALESCE(@Targets + ', ', '') + TR.SourceObjectName 
							from	[cache].[Relationships] SR
									inner join Responsibility RE on RE.ResponsibleObjectType = SR.SourceObject and RE.ResponsibleObjectID = SR.SourceObjectID
									inner join [cache].[Relationships] TR on RE.ObjectType = 'Intersect' and RE.ObjectID = TR.IntersectID and TR.TargetObject = SR.TargetObject and TR.TargetObjectID = SR.TargetObjectID
							where	SR.IntersectID = @intersectID

							update	LoadItem
							set		[Object] = 'Intersect',
									ObjectID = @intersectID,
									[Status] = 0,
									StatusMessage = 'Unable to remove relationship as it acts as a source for: ' + @Targets
							where	LoadID = @LoadID
									and RowIndex = @current
						end
						else
						begin
							delete [Intersect] where ID = @intersectID

							update	LoadItem
							set		[Object] = 'Intersect',
									ObjectID = @intersectID,
									[Status] = 1,
									StatusMessage = 'Successfully removed relationship'
							where	LoadID = @LoadID
									and RowIndex = @current
						end
					end try
					begin catch
							update	LoadItem
							set		[Object] = 'Intersect',
									ObjectID = @intersectID,
									[Status] = 0,
									StatusMessage = 'Unable to remove relationship due to the following error: ' + ERROR_MESSAGE()
							where	LoadID = @LoadID
									and RowIndex = @current
					end catch
				end
				else
				begin
					update	LoadItem
					set		[Object] = 'Intersect',
							ObjectID = NULL,
							[Status] = 0,
							StatusMessage = 'Relationship not found'
					where	LoadID = @LoadID
							and RowIndex = @current
				end
			end --end U

			insert into @Intersects values (@intersectID)

			set @current = @current + 1
		end

		if @Action = 'R'
		begin
			exec cache.SynchronizeRelationships @Intersects
		end

	end --end IF statement to check if action = P or NOT

	update	[Load] 
	set		DateCompleted = getutcdate()
	where	ID = @LoadID
end
GO

ALTER PROCEDURE [utility].[PromoteFusionAttributes]
AS
BEGIN
	SET NOCOUNT ON;

	declare @d datetime = getutcdate(),
			@r int = 0,
			@RuleID int,
			@PromotionObjectType varchar(50),
			@PromotionObjectID int,
			@PromotionParentObjectType varchar(50),
			@PromotionParentObjectID int,
			@FusionID int,
			@FusionAttributeID int,
			@ExecutionID int,
			@NumberOfRules int,			
			@NumberOfNewTaxonomies int,
			@NumberOfNewDomainItems int,
			@NumberOfNewDomains int,
			@NumberOfNewArtifacts int,
			@NumberOfAttributesTotal int,
			@NumberOfNewRelations int,
			@promotionNeedsToRun bit
	

	set	@NumberOfRules = 0;	
	set @NumberOfNewTaxonomies = 0;
	set @NumberOfNewDomainItems = 0;
	set @NumberOfNewDomains = 0;
	set @NumberOfNewArtifacts = 0;
	set @promotionNeedsToRun = 0;

	--First check if there is anything to do

	EXEC @promotionNeedsToRun = [utility].[ShouldPromotionRun]

	if(@promotionNeedsToRun <= 0)
	BEGIN
		PRINT 'NO REASON TO RUN THE PROMOTION RULES WAS DETECTED';
		return;
	END;


	--Log this run get a new id from the fusion.promotion table
	insert into [dbo].[FusionAttributePromotionLogSummary] ( DateStarted )
									values ( CURRENT_TIMESTAMP)

	select @ExecutionID =  SCOPE_IDENTITY()

	IF OBJECT_ID('tempdb..#rules') IS NOT NULL
		DROP TABLE #rules;

	create table #rules (
		ID int identity,
		RuleID int,
		FusionID int,
		ObjectType varchar(25),
		ObjectID int,
		PromotionObjectType varchar(25),
		PromotionObjectID int,
		PromotionParentObjectType varchar(25),
		PromotionParentObjectID int,
		FilterFusionAttributeID int,
		FilterFusionAttributeTypeID int
	);

	IF OBJECT_ID('tempdb..#attributes') IS NOT NULL
		DROP TABLE #attributes;

	create table #attributes (
		ID int identity,
		RuleID int,
		FusionAttributeID int
	);

	IF OBJECT_ID('tempdb..#fields') IS NOT NULL
		DROP TABLE #fields;

	create table #fields (
		ID int, 
		SourceFieldName nvarchar(250), 
		SourceFieldTypeID int, 
		TargetFieldName nvarchar(250), 
		TargetFieldTypeID int, 
		Value nvarchar(4000)
	);

	IF OBJECT_ID('tempdb..#fieldValues') IS NOT NULL
		DROP TABLE #fieldValues;

	create table #fieldValues (
		ObjectType varchar(50), 
		ObjectID int, 
		FieldTypeID int, 
		Value nvarchar(4000)
	);
	
	
	insert into #rules
		select	R.ID,
				R.FusionID,
				R.ObjectType,
				R.ObjectID,
				R.PromotionObjectType,
				R.PromotionObjectID,
				R.PromotionParentObjectType,
				R.PromotionParentObjectID,
				I.FusionAttributeID as FilterFusionAttributeID,
				coalesce(A.FusionAttributeTypeID, R.ObjectID) as FilterFusionAttributeTypeID
		from	FusionAttributePromotionRule R
				inner join FusionAttributePromotionRuleItem I on I.FusionAttributePromotionRuleID = R.ID and R.[Enabled] = 1
				left join FusionAttribute A on A.ID = I.FusionAttributeID

	
	declare	@currentID int,
			@maxID int

	set		@currentID = 1
	select	@maxID = MAX(ID) from #rules

	select @NumberOfRules = count(1) from FusionAttributePromotionRule where [Enabled] = 1;

	while (@currentID <= @maxID)
	begin
		declare @ObjectType varchar(25),
				@ObjectID int,
				@FilterFusionAttributeID int,
				@FilterFusionAttributeTypeID int


		select	@RuleID = RuleID,
				@ObjectType = ObjectType,
				@ObjectID = ObjectID,
				@PromotionObjectType = PromotionObjectType,
				@PromotionObjectID = PromotionObjectID,
				@PromotionParentObjectType = PromotionParentObjectType,
				@PromotionParentObjectID = PromotionParentObjectID,
				@FusionID = FusionID,
				@FilterFusionAttributeID = FilterFusionAttributeID,
				@FilterFusionAttributeTypeID = FilterFusionAttributeTypeID
		from	#rules
		where	ID = @currentID

		if @ObjectID = @FilterFusionAttributeTypeID AND @FilterFusionAttributeID is not null
			begin
				-- You are on a specific nodes of same type.  Just copy to target table.
				insert into #attributes values (@RuleID, @FilterFusionAttributeID)
			end
		else
			begin
				-- You are on an attribute higher up in hierarchy.
				if @FilterFusionAttributeID is null
					begin
						--  If there is NO filtered attribute ID, then you need to get every attribute in system for the partiular fusion instance.
						insert into #attributes
							select	@RuleID, FA.ID 
							from	FusionAttribute FA 
									left join #attributes A on A.FusionAttributeID = FA.ID and A.RuleID = @RuleID and A.ID is null
							where	FA.FusionID = @FusionID 
									and FA.FusionAttributeTypeID = @ObjectID
					end
				else
					begin
						-- If there is a filter attribute ID, then traverse the hierarchy and get all attributes of the specified type.
						with fa as	(
									select	ID,
											ParentID,
											FusionAttributeTypeID
									from	FusionAttribute
									where	ID = @FilterFusionAttributeID
									union all
									select	C.ID,
											C.ParentID,
											C.FusionAttributeTypeID
									from	FusionAttribute C
											inner join fa P on C.ParentID = P.ID --and P.ID <> C.ID
									)
	
						insert into #attributes
							select	@RuleID, fa.ID 
							from	fa 
									left join #attributes A on A.FusionAttributeID = fa.ID and A.RuleID = @RuleID and A.ID is null
							where	fa.FusionAttributeTypeID = @ObjectID
					end
			end

		set @currentID = @currentID + 1
	end


	-- Load field values we are working with, first starting with the Name.
	insert into #fields
		select	A.ID,
				M.SourceFieldName,
				M.SourceFieldTypeID,
				M.TargetFieldName,
				M.TargetFieldTypeID,
				case 
					when M.SourceFieldName = 'Name' then FA.Name					
					when M.IsConstantValue = 1 then M.ConstantValue
				end				
		from	FusionAttributePromotionRuleMapping M
				inner join #attributes A on A.RuleID = M.FusionAttributePromotionRuleID
				inner join FusionAttribute FA on FA.ID = A.FusionAttributeID 

	
	-- Update the fields table above with values for all dynamic fields.
	update	T
	set		T.Value = S.Value
	from	#fields T
			inner join #attributes A on A.ID = T.ID
			inner join Field S on S.ObjectType = 'FusionAttribute' and S.ObjectID = A.FusionAttributeID and S.FieldTypeID = T.SourceFieldTypeID 


--BEGIN: TESTING ---------------------------------------
/*
select * from #rules
select * from #attributes
select * from #fields

select	A.ID,
		R.RuleID,
		R.FusionID,
		R.ObjectID as FusionAttributeTypeID,
		R.PromotionObjectType,
		R.PromotionObjectID,
		R.PromotionParentObjectType,
		R.PromotionParentObjectID,
		A.FusionAttributeID
from	#rules R
		inner join #attributes A on A.RuleID = R.RuleID
*/
--END: TESTING ------------------------------------------
	set		@currentID = 1
	select	@maxID = MAX(ID) from #attributes

	set @NumberOfAttributesTotal = @maxID;
	
	while (@currentID <= @maxID)
	begin
		begin try

			declare @FusionAttributeTypeID int,
					@PromotedType varchar(50),
					@PromotedID int

			declare @fields table (SourceFieldName nvarchar(250), SourceFieldTypeID int, TargetFieldName nvarchar(250), TargetFieldTypeID int, Value nvarchar(4000))

			select	@RuleID = R.RuleID,
					@FusionID = R.FusionID,
					@FusionAttributeTypeID = R.ObjectID,
					@PromotionObjectType = R.PromotionObjectType,
					@PromotionObjectID = R.PromotionObjectID,
					@PromotionParentObjectType = R.PromotionParentObjectType,
					@PromotionParentObjectID = R.PromotionParentObjectID,
					@FusionAttributeID = A.FusionAttributeID,
					@PromotedType = P.ObjectType,
					@PromotedID = P.ObjectID
			from	#rules R
					inner join #attributes A on A.RuleID = R.RuleID and A.ID = @currentID
					left join FusionAttributePromotion P on P.FusionAttributeID = A.FusionAttributeID and P.FusionAttributePromotionRuleID = R.RuleID

			--Load up fields were are working with for this loop instance.
			insert into @fields
				select SourceFieldName, SourceFieldTypeID, TargetFieldName, TargetFieldTypeID, Value from #fields where ID = @currentID

			if exists(select 1 from @fields where TargetFieldName = 'Name')
				begin
					declare @code nvarchar(50) = null,
							@name nvarchar(250) = null,
							@description nvarchar(4000) = null

					select @code = Value from @fields where TargetFieldName = 'Code'
					select @name = Value from @fields where TargetFieldName = 'Name'
					select @description = coalesce(Value, '') from @fields where TargetFieldName = 'Description'

					if @PromotionObjectType = 'ArtifactType'
						begin
							set @PromotedType = 'Artifact'

							if @PromotedID is null
								begin
									select	@PromotedID = ID
									from	Artifact
									where	ArtifactTypeID = @PromotionObjectID
											and lower(Name) = lower(@name)
								end

							declare @modelTypeID int
							select @modelTypeID = min(ID) from TaxonomyType

							if @PromotedID is null
								begin
									insert into Artifact ( ParentID, ArtifactTypeID, TaxonomyTypeID, Name, Description, Status, UpdatedOn, UpdatedBy )
									values ( @PromotionParentObjectID, @PromotionObjectID, @modelTypeID, @name, @description, 'Draft', getutcdate(), 0 )

									select @PromotedID =  SCOPE_IDENTITY()

									set @NumberOfNewArtifacts = @NumberOfNewArtifacts +1;
								end
							else
							  begin
									declare @testArtifactName nvarchar(250) = null,
											@testArtifactDescription nvarchar(4000) = null,
											@testArtifactParentID int = null,
											@testArtifactTaxonomyTypeID int = null

									select	@testArtifactName = Name,
											@testArtifactDescription = Description,
											@testArtifactParentID = ParentID,
											@testArtifactTaxonomyTypeID = TaxonomyTypeID
									from	Artifact
									where	ID = @PromotedID

									if (@testArtifactName <> @name) 
										OR (@testArtifactDescription <> @description) 
										OR (@testArtifactParentID <> @PromotionParentObjectID) 
										OR (@testArtifactTaxonomyTypeID <> @modelTypeID)
									begin
										update	Artifact
										set		Name = @name,
												Description = @description,
												ParentID = @PromotionParentObjectID,
												TaxonomyTypeID = @modelTypeID
										where	ID = @PromotedID
									end
								end
						end
 
					if @PromotionObjectType = 'DomainType'
						begin
							if @PromotionParentObjectType is null and @PromotionParentObjectID is null
								begin
									set @PromotedType = 'Domain'
									
									-- You are promoting to a Domain (creating a list)
									if @PromotedID is null
										begin
											select	@PromotedID = ID
											from	Domain
											where	DomainTypeID = @PromotionObjectID
													and lower(Name) = lower(@name)
										end
 
									if @PromotedID is null
										begin
											insert into Domain  ( DomainTypeID, Name, Description ) 
											values ( @PromotionObjectID, @name, @description )

											select @PromotedID =  SCOPE_IDENTITY()

											set @NumberOfNewDomains = @NumberOfNewDomains +1;
										end
									else
										begin
											update	Domain
											set		Name = @name,
													Description = @description
											where	ID = @PromotedID
										end
								end
							else
								begin
									-- You are promoting domain items to a specific domain (list)
									set @PromotedType = 'DomainItem'

									if @PromotedType is null and @PromotedID is null
										begin
											select	@PromotedID = ID
											from	DomainItem
											where	DomainID = @PromotionParentObjectID
													and lower(Code) = lower(@code)
										end
 
									if @PromotedID is not null
										begin
											update	DomainItem
											set		Name = @name,
													Code = coalesce(@code, @name),
													Description = @description
											where	ID = @PromotedID
										end
									else
										begin
											insert into DomainItem ( DomainID, Name, Code, Description )
											values ( @PromotionParentObjectID, @name, coalesce(@code, @name), @description )

											select @PromotedID =  SCOPE_IDENTITY()

											set @NumberOfNewDomainItems = @NumberOfNewDomainItems +1;
										end
								end
						end

					if @PromotionObjectType = 'TaxonomyType'
						begin
							set @PromotedType = 'Taxonomy'

							if @PromotedID is null
								begin
									select	@PromotedID = ID
									from	Taxonomy
									where	TaxonomyTypeID = @PromotionObjectID
											and ParentID = @PromotionParentObjectID
											and lower(Name) = lower(@name)
								end

							if @PromotedID is null
								begin
									insert into Taxonomy	( ParentID, TaxonomyTypeID, Name, Description )
									values					( @PromotionParentObjectID, @PromotionObjectID, @name, @description )

									select @PromotedID =  SCOPE_IDENTITY()

									set @NumberOfNewTaxonomies = @NumberOfNewTaxonomies +1;
								end
							else
								begin
									update	Taxonomy
									set		Name = @Name,
											Description = @Description--,
											--ParentID = @PromotionParentObjectID
									where	ID = @PromotedID
 								end
						end

					-- Add/Update the promotion record to keep track of the auto-promotions
					if @PromotedType is not null and @PromotedID is not null
						begin
							-- Insert/Update the FusionAttributePromotion table to keep track of previously promoted objects.
							
							MERGE	FusionAttributePromotion AS T
							USING	(
									SELECT	@FusionAttributeID as FusionAttributeID, 
											@PromotedType as ObjectType, 
											@PromotedID as ObjectID, 
											@RuleID as RuleID,
											@PromotionObjectID as PromotedObjectTypeID
									) as S
							ON		T.FusionAttributeID = S.FusionAttributeID 
									and T.ObjectType = S.ObjectType 
									and T.ObjectID = S.ObjectID
							WHEN	MATCHED THEN
									UPDATE SET T.FusionAttributePromotionRuleID = S.RuleID, ObjectTypeID = S.PromotedObjectTypeID
							WHEN	NOT MATCHED THEN
									INSERT (FusionAttributeID, ObjectType, ObjectID, FusionAttributePromotionRuleID, ObjectTypeID) 
									VALUES (S.FusionAttributeID, S.ObjectType, S.ObjectID, S.RuleID, S.PromotedObjectTypeID);
						end

					-- Add/Update the dynamic fields involved.
					if @PromotedType is not null and @PromotedID is not null
						begin
							-- First, clean up fields table variable of static fields to prepare for dynamic field work below.
							delete @fields where TargetFieldTypeID = 0

							-- Now insert the dynamic fields
							while exists (select 1 from @fields)
								begin
									declare @targetFieldTypeID int,
											@field_Type varchar(25),
											@lookupObjectType varchar(25),
											@lookupObjectID int,
											@fieldValue nvarchar(4000),
											@shouldInsert bit = 0

									select	top 1 
											@targetFieldTypeID = TargetFieldTypeID,
											@fieldValue = Value
									from	@fields
									
									select	@field_Type = [Type],
											@lookupObjectType = LookupObjectType,
											@lookupObjectID = LookupObjectID									
									 from	FieldType 
									 where	ID = @targetFieldTypeID

									if @field_Type = 'Lookup'
										begin
											declare @objectResultID int

											if @lookupObjectType = 'Artifact'
												begin
													select	top 1
															@objectResultID = ID
													from	Artifact
													where	ArtifactTypeID = @lookupObjectID and Name = @fieldValue
												end
											if @lookupObjectType = 'Domain'
												begin
													select	top 1
															@objectResultID = ID
													from	DomainItem
													where	DomainID = @lookupObjectID and Name = @fieldValue
												end
											if @lookupObjectType = 'Lookup'
												begin
													select	top 1
															@objectResultID = L.ID
													from	[Lookup] L
															inner join Field F on F.ObjectType = @lookupObjectType and F.ObjectID = L.ID and L.LookupTypeID = @lookupObjectID and F.FieldTypeID = @targetFieldTypeID and F.FormattedValue = @fieldValue
												end
											
											if @PromotedID is not null and @objectResultID is not null
												begin
													-- Lookup values properly resolved, so you can now insert the Field record.
													
													set @shouldInsert = 1
													set @fieldValue = cast(@objectResultID as nvarchar(4000))
												end
										end									
									else
										begin
											-- This is a text value, so just insert it into the Field table for the promoted object.
											set @shouldInsert = 1
										end

									if @shouldInsert = 1
										begin
											If not EXISTS (SELECT 1 FROM #fieldValues where ObjectType = @PromotedType and ObjectID = @PromotedID and FieldTypeID = @targetFieldTypeID) --avoid duplicates this happens in gmo
											begin
												insert into #fieldValues (ObjectType, ObjectID, FieldTypeID, Value) values(@PromotedType, @PromotedID, @targetFieldTypeID, @fieldValue)
											end
										end
						
									-- Delete the field we just finished processing.
									delete @fields where TargetFieldTypeID = @targetFieldTypeID
								end 
						end
				end -- Check to see if Target Field called NAME is present
								
		end try
		begin catch
			--SELECT 
				--ERROR_NUMBER() AS ErrorNumber
				--,ERROR_MESSAGE() AS ErrorMessage;
		end catch

		set @currentID = @currentID + 1
	end


	-- write the field values from the temp table to the field table
	-- the field table has a trigger doing this once outside the loop causes the trigger to only fire this one time.
		
	If EXISTS (SELECT 1 FROM #fieldValues)		
	begin
		--debug shows values 
		--select * from #fieldValues

		merge	Field as T
				using	(
					select f.ObjectType as ObjectType,
							f.ObjectID as ObjectID,
							f.FieldTypeID as FieldTypeID,
							f.Value as Value
					from #fieldValues f inner join dbo.FieldType ft on (ft.ID = f.FieldTypeID)
				) as S
				on		T.ObjectType = S.ObjectType and T.ObjectID = S.ObjectID and T.FieldTypeID = S.FieldTypeID
				when	matched then
					update set T.Value = S.Value
				when	not matched then
					insert (ObjectTYpe, OBjectID, FieldTypeID, Value)
					values (S.ObjectType, S.ObjectID, S.FieldTypeID, S.Value);
	end

	-- Add new relations as needed
	exec [utility].[PromoteFusionAttributesRelations] @NumberOfNewRelations output

	-- Handle any fusionlookup fields
	exec [utility].[PromoteFusionAttributeLookups]
	
		
	--Log this run done
	update [dbo].[FusionAttributePromotionLogSummary]
	set DateCompleted = CURRENT_TIMESTAMP, 
		[PromotedTaxonomies] = @NumberOfNewTaxonomies, 
		[PromotedDomainItems] = @NumberOfNewDomainItems,  
		[PromotedDomains] = @NumberOfNewDomains,
		[PromotedArtifacts] = @NumberOfNewArtifacts,
		[TotalNewPromotions] = (@NumberOfNewTaxonomies + @NumberOfNewDomainItems + @NumberOfNewDomains + @NumberOfNewArtifacts),
		[AttributesConsidered]= @NumberOfAttributesTotal,
		[NumberOfRules] = @NumberOfRules ,
		[RelationshipsAdded] = @NumberOfNewRelations
	where ID = @ExecutionID;
	
END
GO

ALTER FUNCTION [utility].[GetVerticalResponsibilityList]
(
	@Object varchar(50),
	@ObjectID int,
	@Priority int
)
RETURNS 
@tbl TABLE 
(
	[Source] varchar(50), 
	Visible bit,
	ResponsibilityID int,
	ResponsibilityTypeID int,
	AssigningItem varchar(50),
	AssigningItemID int,
	[Object] varchar(50),
	ObjectID int,
	ContextHash varchar(50),
	[Priority] int
)
AS
BEGIN

	if @Object = 'ArtifactType' OR @Object = 'Artifact'
		begin
			insert into @tbl
				select	'Artifact Vertical' as [Source],
						R.Visible,
						R.ID,
						R.ResponsibilityTypeID,
						'ArtifactType' as AssigningItemType,
						T.ID as AssigningItemID,
						'Artifact' as ObjectType,
						A.ID as ObjectID,
						utility.GetResponsibilityContextHash(R.ID),
						@Priority as [Priority]
				from	ArtifactType T 
						inner join Responsibility R on R.ObjectType = 'ArtifactType' and R.ObjectID = T.ID
						inner join Artifact A on A.ArtifactTypeID = T.ID 
													and (
															(
																(
																(@Object = 'ArtifactType' and A.ArtifactTypeID = @ObjectID) OR 
																(@Object = 'Artifact' and A.ID = @ObjectID)
																)
																and @ObjectID is not null 
															)
															OR @ObjectID is null 
														);

			insert into @tbl
				select	'Taxonomy Vertical' as [Source],
						R.Visible,
						R.ID,
						R.ResponsibilityTypeID,
						'TaxonomyType' as AssigningItemType,
						T.ID as AssigningItemID,
						'Artifact' as ObjectType,
						A.ID as ObjectID,
						utility.GetResponsibilityContextHash(R.ID),
						@Priority+1 as [Priority]
				from	TaxonomyType T 
						inner join Responsibility R on R.ObjectType = 'TaxonomyType' and R.ObjectID = T.ID
						inner join Artifact A on A.TaxonomyTypeID = T.ID
												  and	(
															(
																(
																(@Object = 'ArtifactType' and A.ArtifactTypeID = @ObjectID) OR 
																(@Object = 'Artifact' and A.ID = @ObjectID)
																)
																and @ObjectID is not null 
															)
															OR @ObjectID is null 
														);
		end
	if @Object = 'DomainType' OR @Object = 'Domain'
		begin
			insert into @tbl
				select	'Domain Vertical' as [Source],
						R.Visible,
						R.ID,
						R.ResponsibilityTypeID,
						'DomainType' as AssigningItemType,
						T.ID as AssigningItemID,
						'Domain' as ObjectType,
						A.ID as ObjectID,
						utility.GetResponsibilityContextHash(R.ID),
						@Priority as [Priority]
				from	DomainType T 
						inner join Responsibility R on R.ObjectType = 'DomainType' and R.ObjectID = T.ID
						inner join Domain A on A.DomainTypeID = T.ID 
												and (
														(
															(
															(@Object = 'DomainType' and T.ID = @ObjectID) 
															OR (@Object = 'Domain' and A.ID = @ObjectID) 
															)
															and @ObjectID is not null
														)
														or (@ObjectID is null)
													);
		end
	if @Object = 'FusionType' OR @Object = 'Fusion'
		begin
			insert into @tbl
				select	'Fusion Vertical' as [Source],
						R.Visible,
						R.ID,
						R.ResponsibilityTypeID,
						'FusionType' as AssigningItemType,
						T.ID as AssigningItemID,
						'Fusion' as ObjectType,
						A.ID as ObjectID,
						utility.GetResponsibilityContextHash(R.ID),
						@Priority as [Priority]
				from	FusionType T 
						inner join Responsibility R on R.ObjectType = 'FusionType' and R.ObjectID = T.ID
						inner join Fusion A on A.FusionTypeID = T.ID 
												and (
														(
															(
															(@Object = 'FusionType' and T.ID = @ObjectID) 
															OR (@Object = 'Fusion' and A.ID = @ObjectID) 
															)
															and @ObjectID is not null
														)
														or (@ObjectID is null)
													);																		 
		end
	if @Object = 'TaxonomyType' OR @Object = 'Taxonomy'
		begin
			insert into @tbl
				select	'Taxonomy Vertical' as [Source],
						R.Visible,
						R.ID,
						R.ResponsibilityTypeID,
						'TaxonomyType' as AssigningItemType,
						T.ID as AssigningItemID,
						'Taxonomy' as ObjectType,
						A.ID as ObjectID,
						utility.GetResponsibilityContextHash(R.ID),
						@Priority as [Priority]
				from	TaxonomyType T 
						inner join Responsibility R on R.ObjectType = 'TaxonomyType' and R.ObjectID = T.ID
						inner join Taxonomy A on A.TaxonomyTypeID = T.ID 
												and (
														(
															(
															(@Object = 'TaxonomyType' and T.ID = @ObjectID) 
															OR (@Object = 'Taxonomy' and A.ID = @ObjectID)
															)
															and @ObjectID is not null
														)
														or (@ObjectID is null)
													);
		end
	RETURN 
END
GO

alter procedure AddMappingDependencies
--declare
	@ResourceID int,
	@MappingID int,
	@SourceSystem varchar(50),
	@SourceSystemID int,
	@SourceObject varchar(50),
	@SourceObjectID int,
	@SourceFusionAttributeID int,

	@TargetSystem varchar(50),
	@TargetSystemID int,
	@TargetObject varchar(50),
	@TargetObjectID int,
	@TargetFusionAttributeID int,

	@Contexts varchar(2500) = null

	--set @ResourceID = 1
	--set @MappingID = 1
	--set @SourceSystem = 'Artifact'
	--set @SourceSystemID = 733
	--set @SourceObject = 'Artifact'
	--set @SourceObjectID = 4651
	--set @SourceFusionAttributeID = 3613

	--set @TargetSystem = 'Artifact'
	--set @TargetSystemID = 772
	--set @TargetObject = 'Artifact'
	--set @TargetObjectID = 4651
	--set @TargetFusionAttributeID = 105572
as
begin
	set nocount on;
	declare @SourceIntersectID int,
			@SourceFusionIntersectID int,
			@TargetIntersectID int,
			@TargetFusionIntersectID int,
			@ResponsibilityID int,
			@Date datetime = getutcdate()

	-- create and get source intersect
	EXEC AddRelationship @ResourceID, @Date, @SourceSystem, @SourceSystemID, 1, NULL, @SourceObject, @SourceObjectID
	select	@SourceIntersectID = IntersectID
	from	[cache].[Relationships]
	where	SourceObject = @SourceSystem and SourceObjectID = @SourceSystemID and TargetObject = @SourceObject and TargetObjectID = @SourceObjectID

	-- create and get source fusion intersect
	EXEC AddRelationship @ResourceID, @Date, 'Intersect', @SourceIntersectID, 1, NULL, 'FusionAttribute', @SourceFusionAttributeID
	select	@SourceFusionIntersectID = IntersectID
	from	[cache].[Relationships]
	where	SourceObject = 'Intersect' and SourceObjectID = @SourceIntersectID and TargetObject = 'FusionAttribute' and TargetObjectID = @SourceFusionAttributeID

	-- create and get target intersect
	EXEC AddRelationship @ResourceID, @Date, @TargetSystem, @TargetSystemID, 1, NULL, @TargetObject, @TargetObjectID
	select	@TargetIntersectID = IntersectID
	from	[cache].[Relationships]
	where	SourceObject = @TargetSystem and SourceObjectID = @TargetSystemID and TargetObject = @TargetObject and TargetObjectID = @TargetObjectID

	-- create and get target fusion intersect
	EXEC AddRelationship @ResourceID, @Date, 'Intersect', @TargetIntersectID, 1, NULL, 'FusionAttribute', @TargetFusionAttributeID
	select	@TargetFusionIntersectID = IntersectID
	from	[cache].[Relationships]
	where	SourceObject = 'Intersect' and SourceObjectID = @TargetIntersectID and TargetObject = 'FusionAttribute' and TargetObjectID = @TargetFusionAttributeID


	select	@ResponsibilityID = ID
	from	Responsibility 
	where	ResponsibilityTypeID = 0 and ObjectType = 'Intersect' and ObjectID = @TargetIntersectID and ResponsibleObjectType = @SourceSystem and ResponsibleObjectID = @SourceSystemID
	if @ResponsibilityID is null
	begin
		insert into Responsibility	(ResponsibilityTypeID, ObjectType, ObjectID, ResponsibleObjectType, ResponsibleObjectID, UpdatedOn, UpdatedBy, Visible)
		values						(0, 'Intersect', @TargetIntersectID, @SourceSystem, @SourceSystemID, @Date, @ResourceID, 1)
		set @ResponsibilityID = SCOPE_IDENTITY()
	end

	if not exists(select 1 from MappingItem where MappingID = @MappingID and SourceIntersectID = @SourceFusionIntersectID and TargetIntersectID = @TargetFusionIntersectID and ResponsibilityID = @ResponsibilityID)
	begin
		insert into MappingItem (MappingID, SourceIntersectID, TargetIntersectID, ResponsibilityID, UpdatedOn, UpdatedBy) 
		values					(@MappingID, @SourceFusionIntersectID, @TargetFusionIntersectID, @ResponsibilityID, @date, @ResourceID) 
	end
end
GO

CREATE procedure [dbo].[GetGroupHierarchy]
	@type varchar(50),
	@id int
as
begin


declare @mapType int;
set @mapType = 4;

 declare @results table (ID int, [Subject] varchar(150), SubjectID int, [Object] varchar(150),
	 ObjectID int, ObjectType varchar(150), ObjectTypeID int,
	 ParentID varchar(max), Name varchar(250), Path varchar(250), Url varchar(50),
	 ObjectTypeName varchar(100), [Level] int, PredicateID int, PredicatePhrase varchar(350), [Type] int, GroupNumber int, [UID] varchar(max));

 declare @results2 table (ID int, [Subject] varchar(150), SubjectID int, [Object] varchar(150),
	 ObjectID int, ObjectType varchar(150), ObjectTypeID int,
	 ParentID varchar(max), Name varchar(250), Path varchar(250), Url varchar(50),
	 ObjectTypeName varchar(100), [Level] int, PredicateID int, PredicatePhrase varchar(350), [Type] int, GroupNumber int, [UID] varchar(max));



with u as
(
	select  
		m.id as ID,
		n1.objecttype as [Subject], 
		n1.objectid as [SubjectID],
		n2.objecttype as [Object],
		n2.objectid as [ObjectID],
		d.ObjectType as ObjectType,
		d.ObjectTypeID as ObjectTypeID,
		cast((g.groupnumber * -1) as varchar(max)) as ParentID,
		d.Name,
		cast(d.name as varchar(500)) as [Path],
		d.url as Url,
		d.objecttypename as ObjectTypeName, 
		-1 as [Level],
		m.PredicateID as PredicateID,
		coalesce(p.Name,'') + '/' + coalesce(p.Inverse,'') as PredicatePhrase,
		m.[Type] as [Type],
		coalesce(g.GroupNumber,-1) as GroupNumber,
		cast((n1.objecttype + cast(n1.objectid as varchar(10)) + '_' + cast(g.groupnumber as varchar(10))) as varchar(max)) as [UID]
	from intersectmap m
	join intersectnode n1 on n1.id = m.subjectintersectnodeid
	join intersectnode n2 on n2.id = m.objectintersectnodeid
	join cache.objectdetails d on d.object = n1.objecttype and d.objectid = n1.objectid
	join predicate p on p.id = m.predicateid
	join intersectmapgroup g on g.intersectmapid = m.id
	where n2.objecttype = @type and n2.objectid = @id and m.[type] = @mapType

	union all

	select 
		m.id as ID,
		n1.objecttype as [Subject], 
		n1.objectid as [SubjectID],
		n2.objecttype as [Object],
		n2.objectid as [ObjectID],
		d.ObjectType as ObjectType,
		d.ObjectTypeID as ObjectTypeID,
		u.UID as ParentID,
		d.Name, 
		cast(d.name + '/' + u.[Path] as varchar(500)) as [Path],
		d.url as Url,
		d.objecttypename as ObjectTypeName, 
		u.[Level]-1 as [Level],
		m.PredicateID as PredicateID,
		coalesce(p.Name,'') + '/' + coalesce(p.Inverse,'') as PredicatePhrase,
		m.[Type] as [Type],
		u.GroupNumber,
		cast((n1.objecttype + cast(n1.objectid as varchar(10)) + '_' + cast(g.groupnumber as varchar(10))) as varchar(max)) as [UID]
	from intersectmap m
	join intersectnode n1 on n1.id = m.subjectintersectnodeid
	join intersectnode n2 on n2.id = m.objectintersectnodeid
	join cache.objectdetails d on d.object = n1.objecttype and d.objectid = n1.objectid
	join predicate p on p.id = m.predicateid
	join u on u.[Subject] = n2.objecttype and u.[SubjectID] = n2.objectid and (u.[subject] + cast(u.[subjectid] as varchar(10))) != (u.[object] + cast(u.[objectid] as varchar(10)))
	join intersectmapgroup g on g.intersectmapid = m.id and g.groupnumber = u.groupnumber
	where m.[type] = @mapType
)
insert into @results
select distinct * from u order by u.uid asc;


declare @UID varchar(500);
select top 1 @UID = r.[UID] from @results r
join @results c on c.ParentID = r.[UID] 
where r.ParentID like '-%' and c.[UID] != r.[UID] and r.[Level] < 0;

--select * from @results;


while (@UID is not null)
begin

	update @results
	set ParentID = (select top 1 [UID] from @results r where r.ParentID = @UID)
	where [UID] = @UID;

	update @results
	set ParentID = cast((groupnumber * -1) as varchar(max))
	where [UID] = (select ParentID from @results where [UID] = @UID and [Level] < 0);

	if (select count(*) from @results r
		join @results c on c.ParentID = @UID
		where r.ParentID like '-%' and c.[UID] != r.[UID] and r.[Level] < 0) > 0
	begin
		select top 1 @UID = r.[UID] from @results r
		join @results c on c.ParentID = r.[UID] 
		where r.ParentID like '-%' and c.[UID] != r.[UID] and r.[Level] < 0;
	end
	else
	begin
		select @UID = null;
	end

end

insert into @results
	select 
		r.ID as ID,
		t.[type] as [Subject], 
		t.[id] as SubjectID,
		t.[type]as [Object],
		t.[id] as [ObjectID],
		d.ObjectType as ObjectType,
		d.ObjectTypeID as ObjectTypeID,
		cast(r.[UID] as varchar(500)) as ParentID,
		d.Name,
		cast(d.name as varchar(500)) as [Path],
		d.url as Url,
		d.objecttypename as ObjectTypeName, 
		0 as [Level],
		r.PredicateID as PredicateID,
		r.PredicatePhrase as PredicatePhrase,
		t.mapType as [Type],
		r.GroupNumber,
		'root' + r.[UID] as [UID]
	from @results r 
	join (select @type as [type], @id as [id], @mapType as mapType) t on 1=1
	join cache.objectdetails d on d.[object] = t.[type] and d.objectid = t.id
	where r.[Level] = -1;

insert into @results
	select 
		r.ID as ID,
		t.[type] as [Subject], 
		t.[id] as SubjectID,
		t.[type]as [Object],
		t.[id] as [ObjectID],
		d.ObjectType as ObjectType,
		d.ObjectTypeID as ObjectTypeID,
		cast(null as varchar(max)) as ParentID,
		d.Name,
		cast(d.name as varchar(500)) as [Path],
		d.url as Url,
		d.objecttypename as ObjectTypeName, 
		0 as [Level],
		r.PredicateID as PredicateID,
		r.PredicatePhrase as PredicatePhrase,
		t.mapType as [Type],
		r.GroupNumber,
		'root' as [UID]
	from @results r 
	join (select @type as [type], @id as [id], @mapType as mapType) t on 1=1
	join cache.objectdetails d on d.[object] = t.[type] and d.objectid = t.id
	where r.[Level] = 1;

if (select count(*) from @results) < 1
begin
	insert into @results
	select 
		0 as ID,
		t.[type] as [Subject], 
		t.[id] as SubjectID,
		t.[type]as [Object],
		t.[id] as [ObjectID],
		d.ObjectType as ObjectType,
		d.ObjectTypeID as ObjectTypeID,
		cast('-0' as varchar(max)) as ParentID,
		d.Name,
		cast(d.name as varchar(500)) as [Path],
		d.url as Url,
		d.objecttypename as ObjectTypeName, 
		0 as [Level],
		null as PredicateID,
		null as PredicatePhrase,
		t.mapType as [Type],
		-1 as GroupNumber,
		'root' as [UID]
	from (select @type as [type], @id as [id], @mapType as mapType) t
	join cache.objectdetails d on d.[object] = t.[type] and d.objectid = t.id;

end;


--select * from @results;


 with z as
(
	select 
		m.id as ID,
		n1.objecttype as [Subject], 
		n1.objectid as SubjectID,
		n2.objecttype as [Object],
		n2.objectid as [ObjectID],
		d.ObjectType as ObjectType,
		d.ObjectTypeID as ObjectTypeID,
		cast(r.[UID] as varchar(max)) as ParentID,
		d.Name,
		cast(d.name as varchar(500)) as [Path],
		d.url as Url,
		d.objecttypename as ObjectTypeName, 
		1 as [Level],
		m.PredicateID as PredicateID,
		coalesce(p.Name,'') + '/' + coalesce(p.Inverse,'') as PredicatePhrase,
		m.[Type] as [Type],
		coalesce(g.GroupNumber,-1) as GroupNumber,
		cast((n1.objecttype + cast(n1.objectid as varchar(10)) + n2.objecttype + cast(n2.objectid as varchar(10))) as varchar(max)) as [UID]
	from intersectmap m
	join intersectnode n1 on n1.id = m.subjectintersectnodeid
	join intersectnode n2 on n2.id = m.objectintersectnodeid
	join cache.objectdetails d on d.object = n2.objecttype and d.objectid = n2.objectid
	join predicate p on p.id = m.predicateid
	join @results r on r.[subject] = n1.objecttype and r.subjectid = n1.objectid and coalesce(r.ParentID,'0') like '-%'
	join intersectmapgroup g on g.intersectmapid = m.id
	where m.[type] = @mapType
	
	union all
	
	select 
		m.id as ID,
		n1.objecttype as [Subject], 
		n1.objectid as [SubjectID],
		n2.objecttype as [Object],
		n2.objectid as [ObjectID],
		d.ObjectType as ObjectType,
		d.ObjectTypeID as ObjectTypeID,
		z.[UID] as ParentID,
		d.Name, 
		cast(z.[path] + '/' + d.name as varchar(500)) as [Path],
		d.url as Url,
		d.objecttypename as ObjectTypeName, 
		z.[Level]+1 as [Level],
		m.PredicateID as PredicateID,
		coalesce(p.Name,'') + '/' + coalesce(p.Inverse,'') as PredicatePhrase,
		m.[Type] as [Type],
		z.GroupNumber,
		cast((z.UID + n2.objecttype + cast(n2.objectid as varchar(10))) as varchar(max)) as [UID]
	from intersectmap m
	join intersectnode n1 on n1.id = m.subjectintersectnodeid
	join intersectnode n2 on n2.id = m.objectintersectnodeid
	join cache.objectdetails d on d.object = n2.objecttype and d.objectid = n2.objectid
	join predicate p on p.id = m.predicateid
	join z on z.[Object] = n1.objecttype and z.[ObjectID] = n1.objectid
	join intersectmapgroup g on g.intersectmapid = m.id and g.groupnumber = z.groupnumber
	where m.[type] = @mapType
)
insert into @results2
select distinct * from z;

--select * from @results2;

insert into @results2
select 
	r.[id],
	r.[subject],
	r.[subjectid],
	r.[object],
	r.[objectid],
	r.[objecttype],
	r.[objecttypeid],
	null as [ParentID],
	r.[name],
	r.[path],
	r.[url],
	r.[objecttypename],
	0 as [level],
	r.[predicateid],
	r.[predicatephrase],
	r.[type],
	r.[groupnumber],
	r.[uid]
from @results r
where r.ParentID like '-%';

update r
set r.GroupNumber = p.GroupNumber
from @results2 r
join @results2 p on p.parentid = r.[uid]
where r.id = 0;

update @results2
set predicatephrase = reverse(stuff(reverse(predicatephrase),1,1,''))
where reverse(predicatephrase) like '/%';



select * from @results2 
--where (groupnumber > -1 and (select count(*) from @results2) > 1) or
--(groupnumber = -1 and (select count(*) from @results2) = 1)
--order by [level] asc;


/*


--select * from @results;


 */
end
GO

CREATE PROCEDURE [fusion].[ProcessEagleMCToBBMnemonic]
	@StagingFileID int,
	@FusionID int
AS
BEGIN	
	SET NOCOUNT ON;
		
	declare		@eagleStreamID int,
				@streamToFieldIntersectTypeID int,				
				@streamSourceIntersectTypeNodeID int,
				@streamTargetIntersectTypeNodeID int;

	declare		@IDList Table(IntersectID int,StageID Int);

	declare		@Intersects IDTable;

	declare		@MessageStreamFussionAttributeID int = 196,
				@BloombergMnemonicFusionID int = 301;
				
	-- load the stream that we want to add relations ships for    
	select @eagleStreamID = fusionattributeid from [fusion].[stagingfile] where id = @StagingFileID and fusionID = @FusionID
		
	if @eagleStreamID is null
	begin
		raiserror('ERROR : UNABLE TO LOCATE SPECIFIED STREAM INFORMATION FOR INPUT FUSION ID / STAGING ID', 15, 1);
		return;
	end;

	-- add relationships for Stream (196) to Eagle DB Columns (205)
	-- using star tag field that is a field for for fusionattribute type 205 lookup fields to add rels for
	-- todo pull to separate proc
	if @eagleStreamID is not null
	begin
			Declare @StreamToFieldList Table(FieldFusionAttributeID int, StreamFusionAttributeID int,IntersectTypeID int, ID int);
			
			-- load the intersect type ids
			select	@streamToFieldIntersectTypeID = IntersectTypeID,
					@streamSourceIntersectTypeNodeID = SourceIntersectTypeNodeID,
					@streamTargetIntersectTypeNodeID = TargetIntersectTypeNodeID
				 from	utility.RelationshipTypes
				where	SourceObjectType = 'FusionAttributeType' and SourceObjectID = @MessageStreamFussionAttributeID
					and TargetObjectType = 'FusionAttributeType' and TargetObjectID = @BloombergMnemonicFusionID

			if @streamToFieldIntersectTypeID is null or @streamSourceIntersectTypeNodeID is null or @streamTargetIntersectTypeNodeID is null
			begin
				raiserror('ERROR : UNABLE TO LOCATE INTERSECT TYPE IDS FOR EAGLE TO EAGLE MESSAGE STREAMS', 15, 1);
				return;
			end;

			-- insert into in memory table variable the values we want to add intersects for
			insert into @StreamToFieldList
				select fa.id, sf.FusionAttributeID, @streamToFieldIntersectTypeID, ROW_NUMBER() OVER (Order by fa.id) AS 'RowNumber'
					from 
						fusionAttribute fa
						inner join [fusion].[StagingFileItem] sfi on (sfi.value = fa.name)				
						inner join [fusion].[StagingFile] sf on (sfi.stagingfileid = sf.id)
						left join (select srcINode.ObjectID as SourceObjectID,
								   tgtINode.ObjectID as TargetObjectID,
								   1 as hasExisting
							from 
								[dbo].[intersect] isect inner join intersectnode srcINode on (isect.intersecttypeid = @streamToFieldIntersectTypeID and isect.id = srcINode.IntersectID and srcINode.IntersectTypeNodeID = @streamSourceIntersectTypeNodeID)
								inner join intersectnode tgtINode on(isect.intersecttypeid = @streamToFieldIntersectTypeID and isect.id = tgtINode.IntersectID and tgtINode.IntersectTypeNodeID = @streamTargetIntersectTypeNodeID)
								) existing
								  on existing.SourceObjectID = sf.FusionAttributeID and existing.TargetObjectID = fa.ID
					where fa.fusionattributetypeid = @BloombergMnemonicFusionID and sfi.stagingfileid = @StagingFileID and existing.hasExisting is null
					group by fa.id, sf.FusionAttributeID  -- grouping is used to eliminate duplicate star tag relations

			--insert intersect records and save there id's
			-- trick is to use merge to keep the sequence id and staging row ids
			-- http://stackoverflow.com/questions/15614261/using-output-clause-to-insert-value-not-in-inserted
			MERGE
				INTO    [Intersect] d
				USING   (
							SELECT sr.IntersectTypeID isectid , 2 as class,sr.ID as srID
							FROM @StreamToFieldList sr							
						) s
				ON      (1 = 0)
				WHEN NOT MATCHED THEN
				INSERT  (IntersectTypeID, Classification, Description)
				VALUES  (isectid, class, NULL)
				OUTPUT  INSERTED.ID, s.srID into @IDList;

			--insert start records into intersect node
			INSERT INTO IntersectNode	(IntersectTypeNodeID, IntersectID, ObjectType, ObjectID)
					select @streamSourceIntersectTypeNodeID, il.IntersectID, 'FusionAttribute',sr.StreamFusionAttributeID from @StreamToFieldList sr inner join @IDList il on (sr.ID = il.StageID);
						

			--insert end records into intersect node
			INSERT INTO IntersectNode	(IntersectTypeNodeID, IntersectID, ObjectType, ObjectID)
					select @streamTargetIntersectTypeNodeID, il.IntersectID, 'FusionAttribute',sr.FieldFusionAttributeID from @StreamToFieldList sr inner join @IDList il on (sr.ID = il.StageID);
					
	
										
			insert into @Intersects select idl.intersectid from @IDList idl;
			
			declare @IntersectCount int
			select @IntersectCount = count(1) from @Intersects
			
			if @IntersectCount > 0 
			begin				
				EXEC cache.SynchronizeRelationships @Intersects
			end
	end;
end;
GO

alter view [cache].[ObjectDetails]
as
	select	D.[Object],
			D.[ObjectID],
			coalesce(O1.Name, O2.Name, O3.Name, O4.Name, O5.Name, O6.Name, O7.Name, O8.Name, O9.Name, O10.Name, O11.Name, O12.Name, O13.Name, case when O14.ResourceID is not null then O14.FirstName + ' ' + O14.LastName else null end, O15.Name, O16.Name, O17.Name, O18.Name, O19.Name, null) as Name,
			coalesce(O1.TextPath, O2.TextPath, O3.Name, O4.TextPath, O5.Name, O6.Name, O7.Name, O8.Name, O9.Name, O10.Name, O11.Name, O12.Name, O13.TextPath, case when O14.ResourceID is not null then O14.FirstName + ' ' + O14.LastName else null end, O15.Name, O16.Name, O17.TextPath, O18.Name, O19.Name, '') as TextPath,
			coalesce(O1.Description, O2.Description, O6.Description, O7.Description, O8.Description, O9.Description, O10.Description, O12.Description, O13.Description, O19.Description,  NULL) as Description,
			dbo.GenerateObjectUrl(D.[Object], D.[ObjectTypeID], D.ObjectID) as Url,
			case 
				when P1.ID is not null then 'Artifact'
				when P2.ID is not null then 'Taxonomy'
				when P3.ID is not null then 'DomainGroup'
				when P4.ID is not null then 'FusionAttribute'
				when P4.ID is not null then 'FusionAttribute'
				when P7.ID is not null then 'ArtifactType'
				when P10.ID is not null then 'AttributeType'
				when P13.ID is not null then 'PolicyType'
				when P17.ID is not null then 'FusionAttributeType'
				else NULL
			end as Parent,
			coalesce(O1.ParentID, O2.ParentID, O3.ParentID, O4.ParentID, O7.ParentID, O10.ParentID, O13.ParentID, O17.ParentID, NULL) as ParentID,
			coalesce(P1.Name, P2.Name, P3.Name, P4.Name, P7.Name, P10.Name, P13.Name, P17.Name, NULL) as ParentName,
			D.[ObjectType],
			D.ObjectTypeID,
			coalesce(OT1.Name, OT2.Name, OT3.Name, OT4.TextPath, OT5.Name, OT12.Name, OT13.Name, OT14.Name, OT15.Name, NULL) as ObjectTypeName,
			coalesce(S.IconBackColor, '#000') as IconBackColor,
			coalesce(S.IconForeColor, '#fff') as IconForeColor,
			coalesce(S.IconText, 'leaf') as IconText
	from	cache.[Object] D with(nolock)
			left join Artifact O1 with(nolock) on D.[Object] = 'Artifact' and O1.ID = D.ObjectID
			left join ArtifactType OT1 with(nolock) on D.[Object] = 'Artifact' and OT1.ID = O1.ArtifactTypeID
			left join Artifact P1 with(nolock) on D.[Object] = 'Artifact' and P1.ID = O1.ParentID

			left join Taxonomy O2 with(nolock) on D.[Object] = 'Taxonomy' and O2.ID = D.ObjectID
			left join TaxonomyType OT2 with(nolock) on D.[Object] = 'Taxonomy' and OT2.ID = O2.TaxonomyTypeID
			left join Taxonomy P2 with(nolock) on D.[Object] = 'Taxonomy' and P2.ID = O2.ParentID

			left join Domain O3 with(nolock) on D.[Object] = 'Domain' and O3.ID = D.ObjectID
			left join DomainType OT3 with(nolock) on D.[Object] = 'Domain' and OT3.ID = O3.DomainTypeID
			left join DomainGroup P3 with(nolock) on D.[Object] = 'Domain' and P3.ID = O3.DomainGroupID

			left join FusionAttribute O4 with(nolock) on D.[Object] = 'FusionAttribute' and O4.ID = D.ObjectID
			left join FusionAttributeType OT4 with(nolock) on D.[Object] = 'FusionAttribute' and OT4.ID = O4.FusionAttributeTypeID
			left join FusionAttribute P4 with(nolock) on D.[Object] = 'FusionAttribute' and P4.ID = O4.ParentID

			left join Fusion O5 with(nolock) on D.[Object] = 'Fusion' and O5.ID = D.ObjectID
			left join FusionType OT5 with(nolock) on D.[Object] = 'Fusion' and OT5.ID = O5.FusionTypeID

			left join FusionType O6 with(nolock) on D.[Object] = 'FusionType' and O6.ID = D.ObjectID

			left join ArtifactType O7 with(nolock) on D.[Object] = 'ArtifactType' and O7.ID = D.ObjectID
			left join ArtifactType P7 with(nolock) on D.[Object] = 'ArtifactType' and P7.ID = O7.ParentID

			left join TaxonomyType O8 with(nolock) on D.[Object] = 'TaxonomyType' and O8.ID = D.ObjectID

			left join ResponsibilityType O9 with(nolock) on D.[Object] = 'ResponsibilityType' and O9.ID = D.ObjectID

			left join AttributeType O10 with(nolock) on D.[Object] = 'AttributeType' and O10.ID = D.ObjectID
			left join AttributeType P10 with(nolock) on D.[Object] = 'AttributeType' and P10.ID = O10.ParentID

			left join IntersectType O11 with(nolock) on D.[Object] = 'IntersectType' and O11.ID = D.ObjectID

			left join [Rule] O12 with(nolock) on D.[Object] = 'Rule' and O12.ID = D.ObjectID
			left join	(
						select 1 as ID, 'Informational Rule' as Name
						union
						select 2 as ID, 'Quality Check Rule' as Name
						union
						select 3 as ID, 'Metric Rule' as Name
						union
						select 4 as ID, 'Profile Rule' as Name
						) OT12 on D.[Object] = 'Rule' and OT12.ID = O12.RuleType

			left join [Policy] O13 with(nolock) on D.[Object] = 'Policy' and O13.ID = D.ObjectID
			left join PolicyType OT13 with(nolock) on D.[Object] = 'Policy' and OT13.ID = O13.PolicyTypeID
			left join [Policy] P13 with(nolock) on D.[Object] = 'Policy' and P13.ID = O13.ParentID

			left join reporting.Global_Resource O14 with(nolock) on D.[Object] = 'Resource' and O14.ResourceID = D.ObjectID
			left join (select 1 as ID, 'User' as Name) OT14 on D.[Object] = 'Resource' and OT14.ID = D.ObjectTypeID

			left join [Group] O15 with(nolock) on D.[Object] = 'Group' and O15.ID = D.ObjectID
			left join (select 0 as ID, 'Group' as Name) OT15 on D.[Object] = 'Group' and OT15.ID = D.ObjectTypeID

			left join PolicyType O16 with(nolock) on D.[Object] = 'PolicyType' and O16.ID = D.ObjectID

			left join FusionAttributeType O17 with(nolock) on D.[Object] = 'FusionAttributeType' and O17.ID = D.ObjectID
			left join FusionAttributeType P17 with(nolock) on D.[Object] = 'FusionAttributeType' and P17.ID = O17.ParentID

			left join	(
						select 1 as ID, 'Informational Rule' as Name
						union
						select 2 as ID, 'Quality Check Rule' as Name
						union
						select 3 as ID, 'Metric Rule' as Name
						union
						select 4 as ID, 'Profile Rule' as Name
						) O18 on D.[Object] = 'RuleType' and O18.ID = D.ObjectID

			left join DomainType O19 with(nolock) on D.[Object] = 'DomainType' and O19.ID = D.ObjectID

			left join ObjectStyle S with(nolock) on S.ObjectType = D.ObjectType and S.ObjectID = D.[ObjectTypeID]
GO


