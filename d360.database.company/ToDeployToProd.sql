CREATE TABLE [dbo].[FieldTypeFilteredLookupDefinition](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[FieldTypeID] [int] NOT NULL,
	[Object] [varchar](50) NOT NULL,
	[ObjectID] [int] NOT NULL,
	[HideHeader] [bit] NOT NULL,
	[HideFooter] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)
)

GO

ALTER TABLE [dbo].[FieldTypeFilteredLookupDefinition] ADD  CONSTRAINT [DF_FieldTypeFilteredLookupDefinition_HideHeader]  DEFAULT ((1)) FOR [HideHeader]
GO

ALTER TABLE [dbo].[FieldTypeFilteredLookupDefinition] ADD  CONSTRAINT [DF_FieldTypeFilteredLookupDefinition_HideFooter]  DEFAULT ((1)) FOR [HideFooter]
GO

ALTER TABLE [dbo].[FieldTypeFilteredLookupDefinition]  WITH CHECK ADD  CONSTRAINT [FK_FieldTypeFilteredLookupDefinition_FieldType] FOREIGN KEY([FieldTypeID])
REFERENCES [dbo].[FieldType] ([ID])
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[FieldTypeFilteredLookupDefinition] CHECK CONSTRAINT [FK_FieldTypeFilteredLookupDefinition_FieldType]
GO

CREATE TABLE [dbo].[FieldTypeFilteredLookupDisplayField](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[FieldTypeFilteredLookupDefinitionID] [int] NOT NULL,
	[FieldTypeID] [int] NOT NULL,
	[FieldTypeName] [nvarchar](250) NULL,
	[Show] [bit] NOT NULL,
	[SortOrder] [int] NULL,
	[Filter] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)
)

GO

ALTER TABLE [dbo].[FieldTypeFilteredLookupDisplayField] ADD  CONSTRAINT [DF_FieldTypeFilteredLookupDisplayField_Show]  DEFAULT ((1)) FOR [Show]
GO

ALTER TABLE [dbo].[FieldTypeFilteredLookupDisplayField]  WITH CHECK ADD  CONSTRAINT [FK_FieldTypeFilteredLookupDisplayField_FieldTypeFilteredLookupDefinition] FOREIGN KEY([FieldTypeFilteredLookupDefinitionID])
REFERENCES [dbo].[FieldTypeFilteredLookupDefinition] ([ID])
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[FieldTypeFilteredLookupDisplayField] CHECK CONSTRAINT [FK_FieldTypeFilteredLookupDisplayField_FieldTypeFilteredLookupDefinition]
GO


alter table [dbo].[FieldTypeFusionLookupDisplayField] add [Show] [bit] NOT NULL constraint DF_FieldTypeFusionLookupDisplayField_Show default(1)
go
alter table [dbo].[FieldTypeFusionLookupDisplayField] add [SortOrder] [int] NULL
go
alter table [dbo].[FieldTypeFusionLookupDisplayField] add [FilterValue] [nvarchar](250) NULL
go

--ALTER TABLE [Rule] ADD [Definition] [nvarchar](max) NULL
ALTER TABLE [Rule] ADD [Status] [int] NOT NULL CONSTRAINT [DF_Rule_Status] DEFAULT (1)
ALTER TABLE [Rule] ADD [Threshold] [decimal](3, 3) NOT NULL CONSTRAINT [DF_Rule_Threshold] DEFAULT (0)
ALTER TABLE [Rule] ADD [Purpose] [nvarchar](max) NULL
ALTER TABLE [Rule] ADD [Measurement] [nvarchar](max) NULL
ALTER TABLE [Rule] ADD [Resolution] [nvarchar](max) NULL
ALTER TABLE [Rule] ADD [CreatedOn] [datetime] NULL
ALTER TABLE [Rule] ADD [CreatedBy] [int] NULL
ALTER TABLE [Rule] DROP COLUMN SourceID
GO

DROP TABLE [quality].[RuleResult]
GO
DROP TABLE [quality].[RuleMap]
GO
DROP TABLE [quality].[Rule]
GO
DROP TABLE [quality].[Dimension]
GO
DROP FUNCTION [quality].[CalculatePassedWrapper]
GO
DROP FUNCTION [quality].[CalculatePassed]
GO

CREATE FUNCTION [utility].[CalculatePassed]
(
	@PassFraction decimal(4,3),
	@RuleID int
)
RETURNS bit
AS
BEGIN
	DECLARE @Passed bit

	select	top 1
			@Passed = case 
						when @PassFraction >= Threshold then cast(1 as bit)
						else cast(0 as bit)
					end
	from	[Rule] 
	where	ID = @RuleID

	RETURN @Passed
END
GO

create FUNCTION [utility].[CalculatePassedWrapper]
(
	@PassFraction decimal(4,3),
	@RuleID int
)
RETURNS bit
AS
BEGIN
	RETURN [utility].CalculatePassed(@PassFraction, @RuleID)
END
GO



CREATE TABLE [dbo].[RuleResult](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[RuleID] [int] NULL,
	[EffectiveDate] [datetime] NOT NULL,
	[RowsPassed] [int] NOT NULL,
	[RowsFailed] [int] NOT NULL,
	[PassFraction]  AS (CONVERT([decimal](4,3),CONVERT([decimal](18,3),[RowsPassed],(0))/(CONVERT([decimal](18,3),[RowsPassed],(0))+CONVERT([decimal](18,3),[RowsFailed],(0))),(0))),
	[FailFraction]  AS (CONVERT([decimal](4,3),CONVERT([decimal](3,3),CONVERT([decimal](18,3),(1),(0))-CONVERT([decimal](18,3),[RowsPassed],(0))/(CONVERT([decimal](18,3),[RowsPassed],(0))+CONVERT([decimal](18,3),[RowsFailed],(0))),(0)),(0))),
	[Passed]  AS ([utility].[CalculatePassedWrapper](CONVERT([decimal](4,3),CONVERT([decimal](18,3),[RowsPassed],(0))/(CONVERT([decimal](18,3),[RowsPassed],(0))+CONVERT([decimal](18,3),[RowsFailed],(0))),(0)),[RuleID])),
	[CreatedOn] [datetime] NULL CONSTRAINT [DF_RuleResult_CreatedOn] DEFAULT (getutcdate()),
	[CreatedBy] [int] NULL CONSTRAINT [DF_RuleResult_CreatedBy] DEFAULT (0),
	[FusionAttributeID] [int] NULL,
	CONSTRAINT [PK_RuleResult] PRIMARY KEY NONCLUSTERED ( [ID] ASC )
)
GO

ALTER TABLE [dbo].[RuleResult]  WITH CHECK ADD  CONSTRAINT [FK_RuleResult_FusionAttribute] FOREIGN KEY([FusionAttributeID]) REFERENCES [dbo].[FusionAttribute] ([ID])
GO

ALTER TABLE [dbo].[RuleResult] CHECK CONSTRAINT [FK_RuleResult_FusionAttribute]
GO

ALTER TABLE [dbo].[RuleResult]  WITH CHECK ADD  CONSTRAINT [FK_RuleResult_Rule] FOREIGN KEY([RuleID]) REFERENCES [dbo].[Rule] ([ID])
GO

ALTER TABLE [dbo].[RuleResult] CHECK CONSTRAINT [FK_RuleResult_Rule]
GO

--select * from [Rule]
--select * from quality.[Rule]
--insert into RuleResult (RuleID, EffectiveDate,
--		RowsPassed,
--		RowsFailed,
--		CreatedOn,
--		CreatedBy,
--		FusionAttributeID)
--select	case QualityRuleID
--			when 2 then 50
--			when 3 then 51
--			when 5 then 52
--		end,
--		EffectiveDate,
--		RowsPassed,
--		RowsFailed,
--		CreatedOn,
--		CreatedBy,
--		FusionAttributeID
--from	quality.RuleResult


CREATE TABLE [dbo].[RuleMap](
	[RuleID] [int] NOT NULL,
	[SourceID] [varchar](50) NOT NULL,
	[SourceName] [varchar](250) NULL,
	[SourceURI] [varchar](1000) NULL,
	CONSTRAINT [PK_RuleMap] PRIMARY KEY CLUSTERED ( [RuleID] ASC, [SourceID] ASC )
)
GO

ALTER TABLE [dbo].[RuleMap]  WITH CHECK ADD  CONSTRAINT [FK_RuleMap_Rule] FOREIGN KEY([RuleID]) REFERENCES [dbo].[Rule] ([ID])
GO

ALTER TABLE [dbo].[RuleMap] CHECK CONSTRAINT [FK_RuleMap_Rule]
GO




ALTER TABLE [Intersect] ADD CONSTRAINT [DF_Intersect_CreatedOn] DEFAULT (getutcdate()) FOR [CreatedOn];
ALTER TABLE [Intersect] ADD CONSTRAINT [DF_Intersect_UpdatedOn] DEFAULT (getutcdate()) FOR [UpdatedOn];
ALTER TABLE [Intersect] ADD CONSTRAINT [DF_Intersect_CreatedBy] DEFAULT (0) FOR [CreatedBy];
ALTER TABLE [Intersect] ADD CONSTRAINT [DF_Intersect_UpdatedBy] DEFAULT (0) FOR [UpdatedBy];
ALTER TABLE [Intersect] ADD CONSTRAINT [DF_Intersect_Deleted] DEFAULT (0) FOR [Deleted];
GO

CREATE TRIGGER [dbo].[FusionQueryAttributeType_AfterUpsert]
   ON  [dbo].[FusionQueryAttributeType] 
   AFTER INSERT, UPDATE
AS 
	SET NOCOUNT ON;
	merge	[cache].[Object] as T
	using	(
			select	'FusionQueryAttributeType' as [Object],			ID as ObjectID,
					'Fusion' as ObjectType,					FusionID as ObjectTypeID
			from	inserted
			) as S
	on		T.[Object] = S.[Object] and T.[ObjectID] = S.[ObjectID]
	when	matched then
			update set	T.[ObjectType] = S.[ObjectType],
						T.[ObjectTypeID] = S.[ObjectTypeID]
	when	not matched then
			insert	( [Object],		[ObjectID],		[ObjectType],	[ObjectTypeID]		)
			values	( S.[Object],	S.[ObjectID],	S.[ObjectType], S.[ObjectTypeID]	);

GO

CREATE TRIGGER [dbo].[FusionQueryAttributeType_AfterDelete]
   ON  [dbo].[FusionQueryAttributeType] 
   AFTER DELETE
AS 
	SET NOCOUNT ON;
	delete	T
	from	[cache].[Object] T
			inner join deleted S on T.Object = 'FusionQueryAttributeType' and T.ObjectID = S.ID
GO


ALTER FUNCTION [utility].[ObjectDetail]
(
--declare
	@type varchar(50), 
	@id int
--set @type = 'Domain'
--set @id = 1
)
RETURNS @tbl TABLE 
(
	ID int,
	Name nvarchar(250),
	TextPath nvarchar(2500),
	Description nvarchar(4000),
	ParentID int null,
	ParentType nvarchar(250),
	Url nvarchar(2500),
	TypeID int,
	[Type] varchar(25),
	[TypeName] nvarchar(250),
	IconBackColor varchar(15),
	IconForeColor varchar(15),
	IconText varchar(15)
) 
AS
BEGIN
	if @type = 'Artifact'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,													TypeID,				[Type],			TypeName)
			SELECT			O.ID,	O.Name,	O.TextPath,	O.Description,	O.ParentID,	@type,		dbo.GenerateObjectUrl(@type, O.ArtifactTypeID, O.ID),	O.ArtifactTypeID,	'ArtifactType',	T.Name
			FROM	Artifact O
					INNER JOIN ArtifactType T ON O.ArtifactTypeID = T.ID and O.ID = @id
	end

	if @type = 'ArtifactType'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID, [Type], TypeName)
			SELECT			ID,		Name,	Name,		Description,	NULL,		NULL,		dbo.GenerateObjectUrl(@type, 0, ID),	ID,		@type,	'Artifact Type'
			FROM	ArtifactType O
			WHERE	ID = @id
	end

	if @type = 'Attribute'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,													TypeID,				[Type],				TypeName)
			SELECT			O.ID,	'',		'',			'',				O.ParentID,	@type,		D.Url,	O.AttributeTypeID,	'AttributeType',	T.Name
			FROM	[Attribute] O
					INNER JOIN AttributeType T ON O.AttributeTypeID = T.ID and O.ID = @id
					cross apply  utility.ObjectDetail(O.ObjectType, O.ObjectID) D
	end

	if @type = 'AttributeType'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID, [Type], TypeName)
			SELECT			ID,		Name,	Name,		Description,	ParentID,	@type,		dbo.GenerateObjectUrl(@type, 0, ID),	ID,		@type,	'Attribute Type'
			FROM	AttributeType
			WHERE	ID = @id
	end

	if @type = 'Domain'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,												TypeID,			[Type],			TypeName)
			SELECT			O.ID,	O.Name,	O.Name,		O.Description,	NULL,		@type,		dbo.GenerateObjectUrl(@type, O.DomainTypeID, O.ID),	O.DomainTypeID,	'DomainType',	T.Name
			FROM	Domain O
					INNER JOIN DomainType T ON O.DomainTypeID = T.ID and O.ID = @id
	end

	if @type = 'DomainGroup'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,												TypeID,			[Type],			TypeName)
			SELECT			O.ID,	O.Name,	O.Name,		O.Description,	NULL,		@type,		dbo.GenerateObjectUrl(@type, O.DomainTypeID, O.ID),	O.DomainTypeID,	'DomainType',	T.Name
			FROM	DomainGroup O
					INNER JOIN DomainType T ON O.DomainTypeID = T.ID and O.ID = @id
	end

	if @type = 'DomainType'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID, [Type], TypeName)
			SELECT			ID,		Name,	Name,		Description,	NULL,		NULL,		dbo.GenerateObjectUrl(@type, 0, ID),	ID,		@type,	'Domain Type'
			FROM	DomainType
			WHERE	ID = @id
	end

	if @type = 'Group'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID, [Type], TypeName)
			SELECT			ID,		Name,	Name,		Description,	NULL,		NULL,		dbo.GenerateObjectUrl(@type, 0, ID),	0,		@type,	'Group'
			FROM	[Group]
			WHERE	ID = @id
	end

	if @type = 'Intersect'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,													TypeID,				[Type],				TypeName)
			SELECT			O.ID,	O.Name,	O.Name,		'',				NULL,		@type,		dbo.GenerateObjectUrl(@type, O.IntersectTypeID, O.ID),	O.IntersectTypeID,	'IntersectType',	T.Name
			FROM	[Intersect] O
					INNER JOIN IntersectType T ON O.IntersectTypeID = T.ID and O.ID = @id
	end

	if @type = 'IntersectType'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID, [Type], TypeName)
			SELECT			ID,		Name,	Name,		'',				NULL,		NULL,		dbo.GenerateObjectUrl(@type, 0, ID),	ID,		@type,	'Intersect Type'
			FROM	IntersectType
			WHERE	ID = @id
	end

	if @type = 'Event'
	begin
		insert into @tbl (	ID,		Name,				TextPath,	[Description],	ParentID,	ParentType, Url,												TypeID,			[Type],			TypeName)
			SELECT			O.ID,	T.Name + ' event',	T.Name,		'',				NULL,		NULL,		dbo.GenerateObjectUrl(@type, T.RuleID, O.ID),	T.RuleID,	'Rule',	T.Name
			FROM	[Event] O
					INNER JOIN EventGroup T ON O.EventGroupID = T.ID AND O.ID = @id
	end

	if @type = 'EventGroup'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID,			[Type], TypeName)
			SELECT			ID,		Name,	Name,		'',				NULL,		@type,		dbo.GenerateObjectUrl(@type, 0, ID),	RuleID,	'Rule',	'Rule'
			FROM	EventGroup O
			WHERE	ID = @id
	end

	if @type = 'Lookup'
	begin
		insert into @tbl (	ID,		Name,				TextPath,	[Description],	ParentID,	ParentType, Url,												TypeID,			[Type],			TypeName)
			SELECT			O.ID,	T.Name + ' Item',	T.Name,		'',				NULL,		NULL,		dbo.GenerateObjectUrl(@type, O.LookupTypeID, O.ID),	O.LookupTypeID,	'LookupType',	T.Name
			FROM	[Lookup] O
					INNER JOIN LookupType T ON O.LookupTypeID = T.ID AND O.ID = @id
	end

	if @type = 'LookupType'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID, [Type], TypeName)
			SELECT			ID,		Name,	Name,		'',				0,			@type,		dbo.GenerateObjectUrl(@type, ID, 0),	ID,		@type,	'Lookup Type'
			FROM	LookupType O
			WHERE	ID = @id
	end

	if @type = 'Fusion'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,												TypeID,			[Type],			TypeName)
			SELECT			O.ID,	O.Name,	O.Name,		'',				NULL,		@type,		dbo.GenerateObjectUrl(@type, O.FusionTypeID, O.ID),	O.FusionTypeID,	'FusionType',	T.Name
			FROM	Fusion O
					INNER JOIN FusionType T ON O.FusionTypeID = T.ID and O.ID = @id
	end

	if @type = 'FusionType'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID, [Type], TypeName)
			SELECT			ID,		Name,	Name,		'',				NULL,		NULL,		dbo.GenerateObjectUrl(@type, 0, ID),	ID,		@type,	'Fusion Type'
			FROM	FusionType O
			WHERE	ID = @id
	end

	if @type = 'FusionAttribute'
	begin
		insert into @tbl (	ID,		Name,		TextPath,	[Description],	ParentID,	ParentType, Url,	TypeID,						[Type],					TypeName)
			SELECT			O.ID,	coalesce(O.TextPath, O.Name),	O.TextPath,	'',				O.ParentID,	@type,		dbo.GenerateObjectUrl(@type, FT.ID, O.ID),
																											O.FusionAttributeTypeID,	'FusionAttributeType',	T.Name
			FROM	FusionAttribute O
					INNER JOIN FusionAttributeType T ON O.FusionAttributeTypeID = T.ID and O.ID = @id
					INNER JOIN FusionType FT ON T.FusionTypeID = FT.ID
	end

	if @type = 'FusionAttributeType'
	begin
		insert into @tbl (	ID, Name,		TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID, [Type], TypeName)
			SELECT			ID,	O.Name,	O.TextPath,	'',				NULL,		NULL,		dbo.GenerateObjectUrl(@type, 0, ID),	ID,		@type,	'Fusion Attribute Type'
			FROM	FusionAttributeType O
			WHERE	ID = @id
	end

	if @type = 'FusionQueryAttributeType'
	begin
		insert into @tbl (	ID, Name,		TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID, [Type], TypeName)
			SELECT			ID,	O.Name,	O.Name,	'',				NULL,		NULL,		dbo.GenerateObjectUrl(@type, 0, ID),	ID,		@type,	'Fusion Query Attribute Type'
			FROM	FusionQueryAttributeType O
			WHERE	ID = @id
	end

	if @type = 'Policy'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,	TypeID,				[Type],			TypeName)
			SELECT			O.ID,	O.Name,	O.TextPath,	O.Description,	NULL,		@type,		dbo.GenerateObjectUrl(@type, 0, O.ID),	T.ID,	'PolicyType',	T.Name
			FROM	[Policy] O
					INNER JOIN PolicyType T ON O.PolicyTypeID = T.ID AND O.ID = @id
	end

	if @type = 'PolicyType'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID,	[Type],	TypeName)
			SELECT			O.ID,	O.Name,	O.Name,		O.Description,	NULL,		NULL,		dbo.GenerateObjectUrl(@type, O.ID, O.ID),	C.ID,	@type,	C.Name
			FROM	PolicyType O
					inner join PolicyTypeClass C on C.ID = O.PolicyTypeClassID
			WHERE	O.ID = @id
	end

	if @type = 'Report'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,	TypeID,				[Type],			TypeName)
			SELECT			O.ID,	O.Name,	O.Name,	O.Description,	NULL,		@type,		'#',	0,	'Report',	'Report'
			FROM	Report O
			WHERE	O.ID = @id
	end

	if @type = 'Resource'
	begin
		insert into @tbl (ID, Name, Url, TypeID, [Type], TypeName)
			select	ResourceID, FirstName + ' ' + LastName, dbo.GenerateObjectUrl(@type, 1, @id), 1, 'ResourceType', 'Employee'
			from	reporting.Global_Resource 
			where	ResourceID = @id
	end

		if @type = 'ResponsibilityType'
	begin
		insert into @tbl (	ID, Name,	TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID, [Type], TypeName)
			SELECT			ID,	O.Name,	NULL,		Description,	NULL,		NULL,		dbo.GenerateObjectUrl(@type, 0, ID),	ID,		@type,	'Responsibility Type'
			FROM	ResponsibilityType O
			WHERE	ID = @id
	end

	if @type = 'ResourceType'
	begin
		insert into @tbl (ID, Name, Url, TypeID, [Type], TypeName)
		values			(@id, 'Resource Type', '#/resources/administration', @id, @type, 'Resource Type')
	end

	if @type = 'Rule'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,	TypeID,				[Type],			TypeName)
			SELECT			O.ID,	O.Name,	O.Name,	O.Description,	NULL,		@type,		dbo.GenerateObjectUrl(@type, 0, O.ID),	O.RuleType,	'RuleType',	'Rule'
			FROM	[Rule] O
			WHERE	O.ID = @id
	end

	if @type = 'StatisticType'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID, [Type], TypeName)
			SELECT			ID,		Name,	Name,		Description,	NULL,		NULL,		dbo.GenerateObjectUrl(@type, 0, ID),	ID,		@type,	'Analytic Type'
			FROM	StatisticType O
			WHERE	ID = @id
	end

	if @type = 'Taxonomy'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,													TypeID,				[Type],			TypeName)
			SELECT			O.ID,	O.Name,	O.TextPath,	O.Description,	O.ParentID,	@type,		dbo.GenerateObjectUrl(@type, O.TaxonomyTypeID, O.ID),	O.TaxonomyTypeID,	'TaxonomyType',	C.Name + ' Model'
			FROM	Taxonomy O
					INNER JOIN TaxonomyType T ON O.TaxonomyTypeID = T.ID AND O.ID = @id
					inner join TaxonomyTypeClass C on C.ID = T.TaxonomyTypeClassID
	end

	if @type = 'TaxonomyType'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID,	[Type],	TypeName)
			SELECT			O.ID,	O.Name,	O.Name,		O.Description,	NULL,		NULL,		dbo.GenerateObjectUrl(@type, 0, O.ID),	C.ID,	@type,	C.Name
			FROM	TaxonomyType O
					inner join TaxonomyTypeClass C on C.ID = O.TaxonomyTypeClassID
			WHERE	O.ID = @id
	end

	update	T
	set		T.IconBackColor = coalesce(S.IconBackColor, '#000000'),
			T.IconForeColor = coalesce(S.IconForeColor, '#ffffff'),
			T.IconText =	--case @type
							--	when 'Taxonomy' then 'IM'
							--	when 'TaxonomyType' then 'IM'
								--else 
								COALESCE(S.IconText, 'leaf') 
							--end
	from	@tbl T
			left join ObjectStyle S ON S.ObjectType = T.[Type] and S.ObjectID = T.TypeID

	RETURN
END
GO


alter view [cache].[ObjectDetails]
as
	select	D.[Object],
			D.[ObjectID],
			coalesce(O1.Name, O2.Name, O3.Name, O4.Name, O5.Name, O6.Name, O7.Name, O8.Name, O9.Name, O10.Name, O11.Name, O12.Name, O13.Name, case when O14.ResourceID is not null then O14.FirstName + ' ' + O14.LastName else null end, O15.Name, O16.Name, O17.Name, O18.Name, O19.Name, O21.Name, O22.Name, O23.Name, O24.Name, O25.DisplayValue, O26.Name, O27.Name, null) as Name,
			coalesce(O1.TextPath, O2.TextPath, O3.Name, O4.TextPath, O5.Name, O6.Name, O7.Name, O8.Name, O9.Name, O10.Name, O11.Name, O12.Name, O13.TextPath, case when O14.ResourceID is not null then O14.FirstName + ' ' + O14.LastName else null end, O15.Name, O16.Name, O17.TextPath, O18.Name, O19.Name, O21.Name, O22.Name, O23.Name, O24.Name, O25.DisplayValue, O26.Name, O27.Name, '') as TextPath,
			coalesce(O1.Description, O2.Description, O6.Description, O7.Description, O8.Description, O9.Description, O10.Description, O12.Description, O13.Description, O19.Description, O26.Description, NULL) as Description,
			case D.[Object]
				when 'Lookup' then dbo.GenerateObjectUrl('Lookup', O20.LookupTypeID, O20.ID)
				when 'LookupType' then dbo.GenerateObjectUrl('LookupType', O21.ID, 0)
				else dbo.GenerateObjectUrl(D.[Object], D.[ObjectTypeID], D.ObjectID) 
			end as Url,
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
			coalesce(OT1.Name, OT2.Name, OT3.Name, OT4.TextPath, OT5.Name, OT12.Name, OT13.Name, OT14.Name, OT15.Name, OT20.Name, OT24.Name, NULL) as ObjectTypeName,
			coalesce(S.IconBackColor, '#000') as IconBackColor,
			coalesce(S.IconForeColor, '#fff') as IconForeColor,
			coalesce(S.IconText, 'leaf') as IconText,
			case D.[Object]
				when 'Lookup' then dbo.GenerateNgObjectUrl('Lookup', O20.LookupTypeID, O20.ID)
				when 'LookupType' then dbo.GenerateNgObjectUrl('LookupType', O21.ID, 0)
				else dbo.GenerateNgObjectUrl(D.[Object], D.[ObjectTypeID], D.ObjectID) 
			end as NgUrl
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

			left join reporting.Global_Resource O14 with(nolock) on D.[Object] = 'Resource' and O14.ResourceID = D.ObjectID --and O14.Status = 'Active'
			left join (select 1 as ID, 'User' as Name) OT14 on D.[Object] = 'Resource' and OT14.ID = D.ObjectTypeID

			left join [Group] O15 with(nolock) on D.[Object] = 'Group' and O15.ID = D.ObjectID
			left join (
						select 0 as ID, 'Group' as Name
						union
						select 1 as ID, 'Group' as Name
					  ) OT15 on D.[Object] = 'Group' and OT15.ID = D.ObjectTypeID

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

			left join [Lookup] O20 with(nolock) on D.[Object] = 'Lookup' and O20.ID = D.ObjectID
			left join LookupType OT20 with(nolock) on D.[Object] = 'Lookup' and OT20.ID = O20.LookupTypeID

			left join [LookupType] O21 with(nolock) on D.[Object] = 'LookupType' and O21.ID = D.ObjectID

			left join	(
						select 0 as ID, 'User' as Name
						union
						select 1 as ID, 'User' as Name
						) O22 on D.[Object] = 'ResourceType' and O22.ID = D.ObjectID

			left join	(
						select 0 as ID, 'Group' as Name
						union
						select 1 as ID, 'Group' as Name
						) O23 on D.[Object] = 'GroupType' and O22.ID = D.ObjectID

			left join [Intersect] O24 with(nolock) on D.[Object] = 'Intersect' and O24.ID = D.ObjectID
			left join IntersectType OT24 with(nolock) on D.[Object] = 'Intersect' and OT24.ID = O24.IntersectTypeID

			left join ReferenceItem O25 with(nolock) on D.[Object] = 'ReferenceItem' and O25.ID = D.ObjectID
			left join ReferenceItemType OT25 with(nolock) on D.[Object] = 'ReferenceItem' and OT25.ID = O25.ReferenceItemTypeID

			left join ReferenceItemType O26 with(nolock) on D.[Object] = 'ReferenceItemType' and O26.ID = D.ObjectID

			left join FusionQueryAttributeType O27 with(nolock) on D.[Object] = 'FusionQueryAttributeType' and O27.ID = D.ObjectID

			left join ObjectStyle S with(nolock) on S.ObjectType = D.ObjectType and S.ObjectID = D.[ObjectTypeID]

GO

CREATE NONCLUSTERED INDEX [IX_Field_FieldTypeID] 
ON [dbo].[Field] ([FieldTypeID]) INCLUDE ([Value]) 
WITH (ONLINE = ON)
GO


CREATE NONCLUSTERED INDEX [IX_FusionRulePromotion_FusionAttribute_Rule_RuleStep_Object] 
ON [fusion].[RulePromotion] ([FusionAttributeID], [RuleID], [RuleStepID], [ObjectID], [ObjectType]) 
WITH (ONLINE = ON)
GO


update	[Rule]
set CreatedOn = coalesce(CreatedOn, getutcdate()),
	CreatedBy = coalesce(CreatedBy, 0),
	UpdatedOn = coalesce(UpdatedOn, getutcdate()),
	UpdatedBy = coalesce(UpdatedBy, 0)






--ALTER TRIGGER [dbo].[Intersect_AfterInsert]
--	ON [dbo].[Intersect]
--	FOR INSERT
--AS
--BEGIN
--	SET NOCOUNT ON;

--	--declare @tbl table(ID int identity, IntersectID int, ResourceID int, Subject varchar(50), SubjectID int, Object varchar(50), ObjectID int)
--	--insert into @tbl
--	--	select ID, UpdatedBy, Subject, SubjectID, Object, ObjectID from inserted;

--	--declare @current int = 1,
--	--		@max int,
--	--		@id int,
--	--		@r int,
--	--		@s varchar(50),
--	--		@sid int,
--	--		@o varchar(50),
--	--		@oid int,
--	--		@date datetime = getutcdate()

--	--select @max =max(ID) from @tbl

--	--while @current <= @max
--	--begin
--	--	select	@id = IntersectID,
--	--			@r = ResourceID,
--	--			@s = coalesce(Subject, 'Intersect'),
--	--			@sid = coalesce(SubjectID, IntersectID),
--	--			@o = coalesce(Object, 'Intersect'),
--	--			@oid = coalesce(ObjectID, IntersectID)
--	--	from	@tbl
--	--	where	ID = @current

--	--	exec [cache].[SynchronizeObjectDetails] 'Intersect', @id
--	--	exec [utility].[AddAuditEntry] @s, @sid, @r, @date, 'Created', 'Intersect', @id
--	--	exec [utility].[AddAuditEntry] @o, @oid, @r, @date, 'Created', 'Intersect', @id

--	--	exec cache.SynchronizeResponsibilitiesForObject @s, @sid
--	--	--exec cache.SynchronizeResponsibilitiesForObject @o, @oid

--	--	merge cache.Relationship as T
--	--	using (
--	--			select	distinct
--	--					S.IntersectID,
--	--					S.IntersectTypeNodeID as SourceIntersectTypeNodeID, 
--	--					S.ID as SourceIntersectNodeID,
--	--					S.ObjectType as SourceObject,
--	--					S.ObjectID as SourceObjectID,
--	--					T.IntersectTypeNodeID as TargetIntersectTypeNodeID,
--	--					T.ID as TargetIntersectNodeID,
--	--					T.ObjectType as TargetObject,
--	--					T.ObjectID as TargetObjectID
--	--			from	dbo.IntersectNode S
--	--					inner join dbo.IntersectNode T on T.IntersectID = S.IntersectID and T.ID <> S.ID
--	--			where	S.IntersectID = @id
--	--			) as S (
--	--				IntersectID, 
--	--				SourceIntersectTypeNodeID, SourceIntersectNodeID, SourceObject, SourceObjectID, 
--	--				TargetIntersectTypeNodeID, TargetIntersectNodeID, TargetObject, TargetObjectID
--	--				)
--	--	on    (T.IntersectID = S.IntersectID and T.SourceObject = S.SourceObject and T.SourceObjectID = S.SourceObjectID)
--	--	when not matched then
--	--		insert (
--	--				IntersectID, 
--	--				SourceIntersectTypeNodeID, SourceIntersectNodeID, SourceObject, SourceObjectID, 
--	--				TargetIntersectTypeNodeID, TargetIntersectNodeID, TargetObject, TargetObjectID
--	--				)
--	--		values (
--	--				S.IntersectID, 
--	--				S.SourceIntersectTypeNodeID, S.SourceIntersectNodeID, S.SourceObject, S.SourceObjectID, 
--	--				S.TargetIntersectTypeNodeID, S.TargetIntersectNodeID, S.TargetObject, S.TargetObjectID
--	--				);

--	--	set @current = @current +1
--	--end;
--END
--GO

--ALTER TRIGGER [dbo].[Intersect_AfterUpdate]
--	ON [dbo].[Intersect]
--	FOR UPDATE
--AS
--BEGIN
--	SET NOCOUNT ON;

--	--declare @tbl table(ID int identity, IntersectID int, ResourceID int, Subject varchar(50), SubjectID int, Object varchar(50), ObjectID int)
--	--insert into @tbl
--	--	select ID, UpdatedBy, Subject, SubjectID, Object, ObjectID from inserted;

--	--declare @current int = 1,
--	--		@max int,
--	--		@id int,
--	--		@r int,
--	--		@s varchar(50),
--	--		@sid int,
--	--		@o varchar(50),
--	--		@oid int,
--	--		@date datetime = getutcdate()

--	--select @max =max(ID) from @tbl

--	--while @current <= @max
--	--begin
--	--	select	@id = IntersectID,
--	--			@r = ResourceID,
--	--			@s = coalesce(Subject, 'Intersect'),
--	--			@sid = coalesce(SubjectID, IntersectID),
--	--			@o = coalesce(Object, 'Intersect'),
--	--			@oid = coalesce(ObjectID, IntersectID)
--	--	from	@tbl
--	--	where	ID = @current

--	--	exec [cache].[SynchronizeObjectDetails] 'Intersect', @id
--	--	exec [utility].[AddAuditEntry] @s, @sid, @r, @date, 'Updated', 'Intersect', @id
--	--	exec [utility].[AddAuditEntry] @o, @oid, @r, @date, 'Updated', 'Intersect', @id

--	--	merge cache.Relationship as T
--	--	using (
--	--			select	distinct
--	--					S.IntersectID,
--	--					S.IntersectTypeNodeID as SourceIntersectTypeNodeID, 
--	--					S.ID as SourceIntersectNodeID,
--	--					S.ObjectType as SourceObject,
--	--					S.ObjectID as SourceObjectID,
--	--					T.IntersectTypeNodeID as TargetIntersectTypeNodeID,
--	--					T.ID as TargetIntersectNodeID,
--	--					T.ObjectType as TargetObject,
--	--					T.ObjectID as TargetObjectID
--	--			from	dbo.IntersectNode S
--	--					inner join dbo.IntersectNode T on T.IntersectID = S.IntersectID and T.ID <> S.ID
--	--			where	S.IntersectID = @id
--	--			) as S (
--	--				IntersectID, 
--	--				SourceIntersectTypeNodeID, SourceIntersectNodeID, SourceObject, SourceObjectID, 
--	--				TargetIntersectTypeNodeID, TargetIntersectNodeID, TargetObject, TargetObjectID
--	--				)
--	--	on    (T.IntersectID = S.IntersectID and T.SourceObject = S.SourceObject and T.SourceObjectID = S.SourceObjectID)
--	--	when not matched then
--	--		insert (
--	--				IntersectID, 
--	--				SourceIntersectTypeNodeID, SourceIntersectNodeID, SourceObject, SourceObjectID, 
--	--				TargetIntersectTypeNodeID, TargetIntersectNodeID, TargetObject, TargetObjectID
--	--				)
--	--		values (
--	--				S.IntersectID, 
--	--				S.SourceIntersectTypeNodeID, S.SourceIntersectNodeID, S.SourceObject, S.SourceObjectID, 
--	--				S.TargetIntersectTypeNodeID, S.TargetIntersectNodeID, S.TargetObject, S.TargetObjectID
--	--				);

--	--	set @current = @current +1
--	--end;
--END
--GO