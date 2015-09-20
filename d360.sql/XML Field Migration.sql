-- Create these in Legacy database
CREATE TABLE [dbo].[StagingXMLField](
	[CompanyID] [int] NOT NULL,
	[ObjectType] [varchar](25) NOT NULL,
	[ObjectID] [int] NOT NULL,
	TypeID int NOT NULL,
	FieldName varchar(250) NOT NULL,
	[Value] [nvarchar](max) NOT NULL
)
GO

CREATE TABLE [dbo].[StagingXMLFieldType](
	[CompanyID] [int] NOT NULL,
	[ID] [int] identity(1,1) NOT NULL,
	[ObjectType] [varchar](25) NOT NULL,
	[ObjectID] [int] NOT NULL,
	[Name] [nvarchar](250) NOT NULL,
	[FriendlyName] [nvarchar](250) NOT NULL,
	[Description] [nvarchar](max) NULL,
	[Type] [varchar](25) NOT NULL,
	[LookupObjectType] [varchar](25) NULL,
	[LookupObjectID] [int] NULL,
	[LookupDisplayFormat] [nvarchar](250) NULL,
	IsRequired bit not null,
	[DateCreated] [datetime] NOT NULL,
	[CreatingResourceID] [int] NOT NULL,
	[DateUpdated] [datetime] NOT NULL,
	[UpdatingResourceID] [int] NOT NULL
)
GO

CREATE TABLE [dbo].[StagingField](
	[CompanyID] [int] NOT NULL,
	[ObjectType] [varchar](25) NOT NULL,
	[ObjectID] [int] NOT NULL,
	[FieldTypeID] [int] NOT NULL,
	[Value] [nvarchar](max) NOT NULL,
	CONSTRAINT [PK_StagingField] PRIMARY KEY CLUSTERED 
	(
		[CompanyID] ASC,
		[ObjectType] ASC,
		[ObjectID] ASC,
		[FieldTypeID] ASC
	)
)
GO

CREATE TABLE [dbo].[StagingFieldType](
	[CompanyID] [int] NOT NULL,
	[ID] [int] NOT NULL,
	[Name] [nvarchar](250) NOT NULL,
	[FriendlyName] [nvarchar](250) NOT NULL,
	[Description] [nvarchar](max) NULL,
	[Type] [varchar](25) NOT NULL,
	[LookupObjectType] [varchar](25) NULL,
	[LookupObjectID] [int] NULL,
	[LookupDisplayFormat] [nvarchar](250) NULL,
	MinimumLength int,
	MaximumLength int,
	Length int,
	Pattern varchar(500),
	IsRequired bit not null,
	[DateCreated] [datetime] NOT NULL,
	[CreatingResourceID] [int] NOT NULL,
	[DateUpdated] [datetime] NOT NULL,
	[UpdatingResourceID] [int] NOT NULL
)
GO

CREATE TABLE StagingFieldTypeRelation
(
	[CompanyID] [int] NOT NULL,
	[FieldTypeID] [int] NOT NULL,
	[ObjectType] [varchar](50) NOT NULL,
	[ObjectID] [int] NOT NULL,
	[SortOrder] [int] NOT NULL,
	[IsRequired] [bit] NOT NULL,
	[IsListable] [bit] NOT NULL,
	 CONSTRAINT [PK_StagingFieldTypeRelation] PRIMARY KEY CLUSTERED 
	(
		[CompanyID] ASC,
		[FieldTypeID] ASC,
		[ObjectType] ASC,
		[ObjectID] ASC
	)
)
GO

INSERT INTO StagingXMLField
(
	[CompanyID], [ObjectType], [ObjectID], [TypeID], [FieldName], [Value]
)
SELECT		2 as CompanyID,
			*
FROM		(
			SELECT	'FusionAttribute' as ObjectType,
					A.ID as ObjectID,
					T.ID as TypeID,
					F.N.value('local-name(.)', 'varchar(50)') as FieldName,
					F.N.value('(.)[1]', 'varchar(250)') as Value
			FROM	FusionAttribute A
					INNER JOIN FusionAttributeType T ON A.FusionAttributeTypeID = T.ID
					CROSS APPLY Value.nodes('fields/*') as F(N)
			UNION
			SELECT	'Fusion' as ObjectType,
					A.ID as ObjectID,
					T.ID as TypeID,
					F.N.value('local-name(.)', 'varchar(50)') as FieldName,
					F.N.value('(.)[1]', 'varchar(250)') as Value
			FROM	Fusion A
					INNER JOIN FusionType T ON A.FusionTypeID = T.ID
					CROSS APPLY Value.nodes('fields/*') as F(N)
			UNION
			SELECT	'Artifact' as ObjectType,
					A.ID as ObjectID,
					T.ID as TypeID,
					F.N.value('local-name(.)', 'varchar(50)') as FieldName,
					F.N.value('(.)[1]', 'varchar(250)') as Value
			FROM	Artifact A
					INNER JOIN ArtifactType T ON A.ArtifactTypeID = T.ID
					CROSS APPLY Value.nodes('fields/*') as F(N)
			UNION
			SELECT	'Attribute' as ObjectType,
					A.ID as ObjectID,
					T.ID as TypeID,
					F.N.value('local-name(.)', 'varchar(50)') as FieldName,
					F.N.value('(.)[1]', 'varchar(250)') as Value
			FROM	Attribute A
					INNER JOIN AttributeType T ON A.AttributeTypeID = T.ID
					CROSS APPLY Value.nodes('fields/*') as F(N)
			UNION
			SELECT	'Taxonomy' as ObjectType,
					A.ID as ObjectID,
					T.ID as TypeID,
					F.N.value('local-name(.)', 'varchar(50)') as FieldName,
					F.N.value('(.)[1]', 'varchar(250)') as Value
			FROM	Taxonomy A
					INNER JOIN TaxonomyType T ON A.TaxonomyTypeID = T.ID
					CROSS APPLY Value.nodes('fields/*') as F(N)
			UNION
			SELECT	'Lookup' as ObjectType,
					A.ID as ObjectID,
					T.ID as TypeID,
					F.N.value('local-name(.)', 'varchar(50)') as FieldName,
					F.N.value('(.)[1]', 'varchar(250)') as Value
			FROM	Lookup A
					INNER JOIN LookupType T ON A.LookupTypeID = T.ID
					CROSS APPLY Value.nodes('fields/*') as F(N)
			UNION
			SELECT	'ArtifactModule' as ObjectType,
					A.ID as ObjectID,
					T.ID as TypeID,
					F.N.value('local-name(.)', 'varchar(50)') as FieldName,
					F.N.value('(.)[1]', 'varchar(250)') as Value
			FROM	ArtifactModule A
					INNER JOIN ArtifactModuleType T ON A.ArtifactModuleTypeID = T.ID
					CROSS APPLY Value.nodes('fields/*') as F(N)
			UNION
			SELECT	'Event' as ObjectType,
					A.ID as ObjectID,
					T.ID as TypeID,
					F.N.value('local-name(.)', 'varchar(50)') as FieldName,
					F.N.value('(.)[1]', 'varchar(250)') as Value
			FROM	Event A
					INNER JOIN EventType T ON A.EventTypeID = T.ID
					CROSS APPLY Value.nodes('fields/*') as F(N)
			) V
ORDER BY	ObjectType,
			ObjectID
GO


			--UNION
			--SELECT	'Resource' as ObjectType,
			--		A.ID as ObjectID,
			--		T.ID as TypeID,
			--		F.N.value('local-name(.)', 'varchar(50)') as FieldName,
			--		F.N.value('(.)[1]', 'varchar(250)') as Value
			--FROM	Resource A
			--		INNER JOIN ResourceType T ON A.ResourceTypeID = T.ID
			--		CROSS APPLY Value.nodes('fields/*') as F(N)


--TRUNCATE TABLE [StagingXMLFieldType]
--DBCC CHECKIDENT ([StagingXMLFieldType], reseed, 1)
WITH XMLNAMESPACES (
					'http://data3sixty.com/schemas' as d360,
					'http://www.w3.org/2001/XMLSchema' as xsd
					)

INSERT INTO [dbo].[StagingXMLFieldType]
(
	[CompanyID], [ObjectType], [ObjectID], [Name], [FriendlyName], [Description], [Type], [LookupObjectType], [LookupObjectID], [LookupDisplayFormat], [IsRequired],
	[DateCreated], [CreatingResourceID], [DateUpdated], [UpdatingResourceID]
)
SELECT	2 as CompanyID,
		*
FROM	(
	SELECT	'FusionAttributeType' as [ObjectType],
			ID as ObjectID,
			T.E.value('@name', 'varchar(250)') as Name,
			COALESCE(F.N.value('.', 'varchar(250)'), T.E.value('@name', 'varchar(250)')) as FriendlyName,
			E.D.value('.', 'varchar(max)') as Description,
			case 
				when I.H.value('.', 'bit') = 1 then 'Html'
				else
					case 
						when T.E.value('@type', 'varchar(250)') = 'xsd:integer' then 'Integer'
						when T.E.value('@type', 'varchar(250)') = 'xsd:string' then 'Text'
						when T.E.value('@type', 'varchar(250)') = 'xsd:boolean' then 'Boolean'
						when T.E.value('@type', 'varchar(250)') = 'xsd:decimal' then 'Decimal'
						when T.E.value('@type', 'varchar(250)') = 'xsd:date' then 'Date'
						when T.E.value('@type', 'varchar(250)') = 'xsd:time' then 'DateTime'
						else T.E.value('@type', 'varchar(250)')
					end
			end as [Type],
			L.V.value('@type', 'varchar(250)') as LookupObjectType,
			L.V.value('@id', 'int') as LookupObjectID,
			L.V.value('@textFormatString', 'varchar(250)') as LookupDisplayFormatString,
			case 
				when T.E.value('@minOccurs', 'int') > 0 then 1
				else 0
			end as IsRequired,
			--T.E.value('@minOccurs', 'int') as MinOccurs,
			--T.E.value('@maxOccurs', 'int') as MaxOccurs,
			DateCreated,
			CreatingResourceID,
			DateUpdated,
			UpdatingResourceID
	FROM	FusionAttributeType--ArtifactType
			CROSS APPLY [Schema].nodes('//xsd:sequence/xsd:element') as T(E)
			OUTER APPLY T.E.nodes('xsd:annotation[1]/xsd:documentation[1]') as E(D)
			OUTER APPLY T.E.nodes('xsd:annotation[1]/xsd:appinfo[1]/d360:friendlyName[1]') as F(N)
			OUTER APPLY T.E.nodes('xsd:annotation[1]/xsd:appinfo[1]/d360:isHtml[1]') as I(H)
			OUTER APPLY T.E.nodes('xsd:annotation[1]/xsd:appinfo[1]/d360:lookup[1]') as L(V)
	UNION
	SELECT	'FusionType' as [ObjectType],
			ID as ObjectID,
			T.E.value('@name', 'varchar(250)') as Name,
			COALESCE(F.N.value('.', 'varchar(250)'), T.E.value('@name', 'varchar(250)')) as FriendlyName,
			E.D.value('.', 'varchar(max)') as Description,
			case 
				when I.H.value('.', 'bit') = 1 then 'Html'
				else
					case
						when T.E.value('@type', 'varchar(250)') = 'xsd:integer' then 'Integer'
						when T.E.value('@type', 'varchar(250)') = 'xsd:string' then 'Text'
						when T.E.value('@type', 'varchar(250)') = 'xsd:boolean' then 'Boolean'
						when T.E.value('@type', 'varchar(250)') = 'xsd:decimal' then 'Decimal'
						when T.E.value('@type', 'varchar(250)') = 'xsd:date' then 'Date'
						when T.E.value('@type', 'varchar(250)') = 'xsd:time' then 'DateTime'
						else T.E.value('@type', 'varchar(250)')
					end
			end as [Type],
			L.V.value('@type', 'varchar(250)') as LookupObjectType,
			L.V.value('@id', 'int') as LookupObjectID,
			L.V.value('@textFormatString', 'varchar(250)') as LookupDisplayFormatString,
			case 
				when T.E.value('@minOccurs', 'int') > 0 then 1
				else 0
			end as IsRequired,
			--T.E.value('@minOccurs', 'int') as MinOccurs,
			--T.E.value('@maxOccurs', 'int') as MaxOccurs,
			getutcdate() as DateCreated,
			1 as CreatingResourceID,
			getutcdate() as DateUpdated,
			1 as UpdatingResourceID
	FROM	FusionType
			CROSS APPLY [Schema].nodes('//xsd:sequence/xsd:element') as T(E)
			OUTER APPLY T.E.nodes('xsd:annotation[1]/xsd:documentation[1]') as E(D)
			OUTER APPLY T.E.nodes('xsd:annotation[1]/xsd:appinfo[1]/d360:friendlyName[1]') as F(N)
			OUTER APPLY T.E.nodes('xsd:annotation[1]/xsd:appinfo[1]/d360:isHtml[1]') as I(H)
			OUTER APPLY T.E.nodes('xsd:annotation[1]/xsd:appinfo[1]/d360:lookup[1]') as L(V)
	UNION
	SELECT	'ArtifactType' as [ObjectType],
			ID as ObjectID,
			T.E.value('@name', 'varchar(250)') as Name,
			COALESCE(F.N.value('.', 'varchar(250)'), T.E.value('@name', 'varchar(250)')) as FriendlyName,
			E.D.value('.', 'varchar(max)') as Description,
			case 
				when I.H.value('.', 'bit') = 1 then 'Html'
				else
					case 
						when T.E.value('@type', 'varchar(250)') = 'xsd:integer' then 'Integer'
						when T.E.value('@type', 'varchar(250)') = 'xsd:string' then 'Text'
						when T.E.value('@type', 'varchar(250)') = 'xsd:boolean' then 'Boolean'
						when T.E.value('@type', 'varchar(250)') = 'xsd:decimal' then 'Decimal'
						when T.E.value('@type', 'varchar(250)') = 'xsd:date' then 'Date'
						when T.E.value('@type', 'varchar(250)') = 'xsd:time' then 'DateTime'
						else T.E.value('@type', 'varchar(250)')
					end
			end as [Type],
			L.V.value('@type', 'varchar(250)') as LookupObjectType,
			L.V.value('@id', 'int') as LookupObjectID,
			L.V.value('@textFormatString', 'varchar(250)') as LookupDisplayFormatString,
			case 
				when T.E.value('@minOccurs', 'int') > 0 then 1
				else 0
			end as IsRequired,
			--T.E.value('@minOccurs', 'int') as MinOccurs,
			--T.E.value('@maxOccurs', 'int') as MaxOccurs,
			DateCreated,
			CreatingResourceID,
			DateUpdated,
			UpdatingResourceID
	FROM	ArtifactType
			CROSS APPLY [Schema].nodes('//xsd:sequence/xsd:element') as T(E)
			OUTER APPLY T.E.nodes('xsd:annotation[1]/xsd:documentation[1]') as E(D)
			OUTER APPLY T.E.nodes('xsd:annotation[1]/xsd:appinfo[1]/d360:friendlyName[1]') as F(N)
			OUTER APPLY T.E.nodes('xsd:annotation[1]/xsd:appinfo[1]/d360:isHtml[1]') as I(H)
			OUTER APPLY T.E.nodes('xsd:annotation[1]/xsd:appinfo[1]/d360:lookup[1]') as L(V)
	UNION
	SELECT	'AttributeType' as [ObjectType],
			ID as ObjectID,
			T.E.value('@name', 'varchar(250)') as Name,
			COALESCE(F.N.value('.', 'varchar(250)'), T.E.value('@name', 'varchar(250)')) as FriendlyName,
			E.D.value('.', 'varchar(max)') as Description,
			case 
				when I.H.value('.', 'bit') = 1 then 'Html'
				else
					case 
						when T.E.value('@type', 'varchar(250)') = 'xsd:integer' then 'Integer'
						when T.E.value('@type', 'varchar(250)') = 'xsd:string' then 'Text'
						when T.E.value('@type', 'varchar(250)') = 'xsd:boolean' then 'Boolean'
						when T.E.value('@type', 'varchar(250)') = 'xsd:decimal' then 'Decimal'
						when T.E.value('@type', 'varchar(250)') = 'xsd:date' then 'Date'
						when T.E.value('@type', 'varchar(250)') = 'xsd:time' then 'DateTime'
						else T.E.value('@type', 'varchar(250)')
					end
			end as [Type],
			L.V.value('@type', 'varchar(250)') as LookupObjectType,
			L.V.value('@id', 'int') as LookupObjectID,
			L.V.value('@textFormatString', 'varchar(250)') as LookupDisplayFormatString,
			case 
				when T.E.value('@minOccurs', 'int') > 0 then 1
				else 0
			end as IsRequired,
			--T.E.value('@minOccurs', 'int') as MinOccurs,
			--T.E.value('@maxOccurs', 'int') as MaxOccurs,
			DateCreated,
			CreatingResourceID,
			DateUpdated,
			UpdatingResourceID
	FROM	AttributeType
			CROSS APPLY [Schema].nodes('//xsd:sequence/xsd:element') as T(E)
			OUTER APPLY T.E.nodes('xsd:annotation[1]/xsd:documentation[1]') as E(D)
			OUTER APPLY T.E.nodes('xsd:annotation[1]/xsd:appinfo[1]/d360:friendlyName[1]') as F(N)
			OUTER APPLY T.E.nodes('xsd:annotation[1]/xsd:appinfo[1]/d360:isHtml[1]') as I(H)
			OUTER APPLY T.E.nodes('xsd:annotation[1]/xsd:appinfo[1]/d360:lookup[1]') as L(V)
	UNION
	SELECT	'TaxonomyType' as [ObjectType],
			ID as ObjectID,
			T.E.value('@name', 'varchar(250)') as Name,
			COALESCE(F.N.value('.', 'varchar(250)'), T.E.value('@name', 'varchar(250)')) as FriendlyName,
			E.D.value('.', 'varchar(max)') as Description,
			case 
				when I.H.value('.', 'bit') = 1 then 'Html'
				else
					case 
						when T.E.value('@type', 'varchar(250)') = 'xsd:integer' then 'Integer'
						when T.E.value('@type', 'varchar(250)') = 'xsd:string' then 'Text'
						when T.E.value('@type', 'varchar(250)') = 'xsd:boolean' then 'Boolean'
						when T.E.value('@type', 'varchar(250)') = 'xsd:decimal' then 'Decimal'
						when T.E.value('@type', 'varchar(250)') = 'xsd:date' then 'Date'
						when T.E.value('@type', 'varchar(250)') = 'xsd:time' then 'DateTime'
						else T.E.value('@type', 'varchar(250)')
					end
			end as [Type],
			L.V.value('@type', 'varchar(250)') as LookupObjectType,
			L.V.value('@id', 'int') as LookupObjectID,
			L.V.value('@textFormatString', 'varchar(250)') as LookupDisplayFormatString,
			case 
				when T.E.value('@minOccurs', 'int') > 0 then 1
				else 0
			end as IsRequired,
			--T.E.value('@minOccurs', 'int') as MinOccurs,
			--T.E.value('@maxOccurs', 'int') as MaxOccurs,
			DateCreated,
			CreatingResourceID,
			DateUpdated,
			UpdatingResourceID
	FROM	TaxonomyType
			CROSS APPLY [Schema].nodes('//xsd:sequence/xsd:element') as T(E)
			OUTER APPLY T.E.nodes('xsd:annotation[1]/xsd:documentation[1]') as E(D)
			OUTER APPLY T.E.nodes('xsd:annotation[1]/xsd:appinfo[1]/d360:friendlyName[1]') as F(N)
			OUTER APPLY T.E.nodes('xsd:annotation[1]/xsd:appinfo[1]/d360:isHtml[1]') as I(H)
			OUTER APPLY T.E.nodes('xsd:annotation[1]/xsd:appinfo[1]/d360:lookup[1]') as L(V)
	UNION
	SELECT	'ResourceType' as [ObjectType],
			ID as ObjectID,
			T.E.value('@name', 'varchar(250)') as Name,
			COALESCE(F.N.value('.', 'varchar(250)'), T.E.value('@name', 'varchar(250)')) as FriendlyName,
			E.D.value('.', 'varchar(max)') as Description,
			case 
				when I.H.value('.', 'bit') = 1 then 'Html'
				else
					case 
						when T.E.value('@type', 'varchar(250)') = 'xsd:integer' then 'Integer'
						when T.E.value('@type', 'varchar(250)') = 'xsd:string' then 'Text'
						when T.E.value('@type', 'varchar(250)') = 'xsd:boolean' then 'Boolean'
						when T.E.value('@type', 'varchar(250)') = 'xsd:decimal' then 'Decimal'
						when T.E.value('@type', 'varchar(250)') = 'xsd:date' then 'Date'
						when T.E.value('@type', 'varchar(250)') = 'xsd:time' then 'DateTime'
						else T.E.value('@type', 'varchar(250)')
					end
			end as [Type],
			L.V.value('@type', 'varchar(250)') as LookupObjectType,
			L.V.value('@id', 'int') as LookupObjectID,
			L.V.value('@textFormatString', 'varchar(250)') as LookupDisplayFormatString,
			case 
				when T.E.value('@minOccurs', 'int') > 0 then 1
				else 0
			end as IsRequired,
			--T.E.value('@minOccurs', 'int') as MinOccurs,
			--T.E.value('@maxOccurs', 'int') as MaxOccurs,
			DateCreated,
			1 as CreatingResourceID,
			DateUpdated,
			1 as UpdatingResourceID
	FROM	CommunityHub.dbo.ResourceType
			CROSS APPLY [Schema].nodes('//xsd:sequence/xsd:element') as T(E)
			OUTER APPLY T.E.nodes('xsd:annotation[1]/xsd:documentation[1]') as E(D)
			OUTER APPLY T.E.nodes('xsd:annotation[1]/xsd:appinfo[1]/d360:friendlyName[1]') as F(N)
			OUTER APPLY T.E.nodes('xsd:annotation[1]/xsd:appinfo[1]/d360:isHtml[1]') as I(H)
			OUTER APPLY T.E.nodes('xsd:annotation[1]/xsd:appinfo[1]/d360:lookup[1]') as L(V)
	UNION
	SELECT	'LookupType' as [ObjectType],
			ID as ObjectID,
			T.E.value('@name', 'varchar(250)') as Name,
			COALESCE(F.N.value('.', 'varchar(250)'), T.E.value('@name', 'varchar(250)')) as FriendlyName,
			E.D.value('.', 'varchar(max)') as Description,
			case 
				when I.H.value('.', 'bit') = 1 then 'Html'
				else
					case 
						when T.E.value('@type', 'varchar(250)') = 'xsd:integer' then 'Integer'
						when T.E.value('@type', 'varchar(250)') = 'xsd:string' then 'Text'
						when T.E.value('@type', 'varchar(250)') = 'xsd:boolean' then 'Boolean'
						when T.E.value('@type', 'varchar(250)') = 'xsd:decimal' then 'Decimal'
						when T.E.value('@type', 'varchar(250)') = 'xsd:date' then 'Date'
						when T.E.value('@type', 'varchar(250)') = 'xsd:time' then 'DateTime'
						else T.E.value('@type', 'varchar(250)')
					end
			end as [Type],
			L.V.value('@type', 'varchar(250)') as LookupObjectType,
			L.V.value('@id', 'int') as LookupObjectID,
			L.V.value('@textFormatString', 'varchar(250)') as LookupDisplayFormatString,
			case 
				when T.E.value('@minOccurs', 'int') > 0 then 1
				else 0
			end as IsRequired,
			--T.E.value('@minOccurs', 'int') as MinOccurs,
			--T.E.value('@maxOccurs', 'int') as MaxOccurs,
			DateCreated,
			CreatingResourceID,
			DateUpdated,
			UpdatingResourceID
	FROM	LookupType
			CROSS APPLY [Schema].nodes('//xsd:sequence/xsd:element') as T(E)
			OUTER APPLY T.E.nodes('xsd:annotation[1]/xsd:documentation[1]') as E(D)
			OUTER APPLY T.E.nodes('xsd:annotation[1]/xsd:appinfo[1]/d360:friendlyName[1]') as F(N)
			OUTER APPLY T.E.nodes('xsd:annotation[1]/xsd:appinfo[1]/d360:isHtml[1]') as I(H)
			OUTER APPLY T.E.nodes('xsd:annotation[1]/xsd:appinfo[1]/d360:lookup[1]') as L(V)
	UNION
	SELECT	'ArtifactModuleType' as [ObjectType],
			ID as ObjectID,
			T.E.value('@name', 'varchar(250)') as Name,
			COALESCE(F.N.value('.', 'varchar(250)'), T.E.value('@name', 'varchar(250)')) as FriendlyName,
			E.D.value('.', 'varchar(max)') as Description,
			case 
				when I.H.value('.', 'bit') = 1 then 'Html'
				else
					case 
						when T.E.value('@type', 'varchar(250)') = 'xsd:integer' then 'Integer'
						when T.E.value('@type', 'varchar(250)') = 'xsd:string' then 'Text'
						when T.E.value('@type', 'varchar(250)') = 'xsd:boolean' then 'Boolean'
						when T.E.value('@type', 'varchar(250)') = 'xsd:decimal' then 'Decimal'
						when T.E.value('@type', 'varchar(250)') = 'xsd:date' then 'Date'
						when T.E.value('@type', 'varchar(250)') = 'xsd:time' then 'DateTime'
						else T.E.value('@type', 'varchar(250)')
					end
			end as [Type],
			L.V.value('@type', 'varchar(250)') as LookupObjectType,
			L.V.value('@id', 'int') as LookupObjectID,
			L.V.value('@textFormatString', 'varchar(250)') as LookupDisplayFormatString,
			case 
				when T.E.value('@minOccurs', 'int') > 0 then 1
				else 0
			end as IsRequired,
			--T.E.value('@minOccurs', 'int') as MinOccurs,
			--T.E.value('@maxOccurs', 'int') as MaxOccurs,
			DateCreated,
			CreatingResourceID,
			DateUpdated,
			UpdatingResourceID
	FROM	ArtifactModuleType
			CROSS APPLY [Schema].nodes('//xsd:sequence/xsd:element') as T(E)
			OUTER APPLY T.E.nodes('xsd:annotation[1]/xsd:documentation[1]') as E(D)
			OUTER APPLY T.E.nodes('xsd:annotation[1]/xsd:appinfo[1]/d360:friendlyName[1]') as F(N)
			OUTER APPLY T.E.nodes('xsd:annotation[1]/xsd:appinfo[1]/d360:isHtml[1]') as I(H)
			OUTER APPLY T.E.nodes('xsd:annotation[1]/xsd:appinfo[1]/d360:lookup[1]') as L(V)
	UNION
	SELECT	'EventType' as [ObjectType],
			ID as ObjectID,
			T.E.value('@name', 'varchar(250)') as Name,
			COALESCE(F.N.value('.', 'varchar(250)'), T.E.value('@name', 'varchar(250)')) as FriendlyName,
			E.D.value('.', 'varchar(max)') as Description,
			case 
				when I.H.value('.', 'bit') = 1 then 'Html'
				else
					case 
						when T.E.value('@type', 'varchar(250)') = 'xsd:integer' then 'Integer'
						when T.E.value('@type', 'varchar(250)') = 'xsd:string' then 'Text'
						when T.E.value('@type', 'varchar(250)') = 'xsd:boolean' then 'Boolean'
						when T.E.value('@type', 'varchar(250)') = 'xsd:decimal' then 'Decimal'
						when T.E.value('@type', 'varchar(250)') = 'xsd:date' then 'Date'
						when T.E.value('@type', 'varchar(250)') = 'xsd:time' then 'DateTime'
						else T.E.value('@type', 'varchar(250)')
					end
			end as [Type],
			L.V.value('@type', 'varchar(250)') as LookupObjectType,
			L.V.value('@id', 'int') as LookupObjectID,
			L.V.value('@textFormatString', 'varchar(250)') as LookupDisplayFormatString,
			case 
				when T.E.value('@minOccurs', 'int') > 0 then 1
				else 0
			end as IsRequired,
			--T.E.value('@minOccurs', 'int') as MinOccurs,
			--T.E.value('@maxOccurs', 'int') as MaxOccurs,
			DateCreated,
			CreatingResourceID,
			DateUpdated,
			UpdatingResourceID
	FROM	EventType
			CROSS APPLY [Schema].nodes('//xsd:sequence/xsd:element') as T(E)
			OUTER APPLY T.E.nodes('xsd:annotation[1]/xsd:documentation[1]') as E(D)
			OUTER APPLY T.E.nodes('xsd:annotation[1]/xsd:appinfo[1]/d360:friendlyName[1]') as F(N)
			OUTER APPLY T.E.nodes('xsd:annotation[1]/xsd:appinfo[1]/d360:isHtml[1]') as I(H)
			OUTER APPLY T.E.nodes('xsd:annotation[1]/xsd:appinfo[1]/d360:lookup[1]') as L(V)
	) FT
ORDER BY ObjectType, ObjectID
GO

INSERT INTO StagingFieldType
	SELECT	[CompanyID]
			,[ID]
			,[Name]
			,[FriendlyName]
			,[Description]
			,[Type]
			,[LookupObjectType]
			,[LookupObjectID]
			,[LookupDisplayFormat]
			,NULL	--MIN 
			,NULL	--MAX
			,NULL	--LEN
			,NULL	--PAT
			,0
			,[DateCreated]
			,[CreatingResourceID]
			,[DateUpdated]
			,[UpdatingResourceID]
	FROM	[StagingXMLFieldType]
GO

INSERT INTO StagingFieldTypeRelation
SELECT	distinct
		[CompanyID]
      ,[ID]
      ,[ObjectType]
	  ,ObjectID
	  ,1 -- SortOrder
	  ,1 -- IsRequired
	  ,1 -- IsListable
  FROM [StagingXMLFieldType]
GO

--truncate table [StagingField]
INSERT INTO [StagingField]
	SELECT f.[CompanyID]
		  ,f.[ObjectType]
		  ,f.[ObjectID]
		  ,t.ID
		  ,f.[Value]
	  FROM	[StagingXMLField] f
			inner join [StagingXMLFieldType] t on f.TypeID = t.ObjectID and f.ObjectType + 'Type' = t.ObjectType and f.FieldName = t.Name

INSERT INTO [StagingField]
	SELECT f.[CompanyID]
		  ,f.[ObjectType]
		  ,f.[ObjectID]
		  ,COALESCE(LFT.ID, NFT.ID, 0)
		  ,f.[Value]
	  FROM	[StagingXMLField] f
			left join [StagingXMLFieldType] t on f.TypeID = t.ObjectID and f.ObjectType + 'Type' = t.ObjectType and f.FieldName = t.Name
			left join StagingFieldType LFT on f.FieldName = LFT.Name and ISNUMERIC(f.[Value] + '.0e0') = 1 and LFT.LookupObjectType is not null
			left join StagingFieldType NFT on f.FieldName = NFT.Name and ((ISNUMERIC(f.[Value] + '.0e0') = 0) OR (f.FieldName = 'scale')) and NFT.LookupObjectType is null
where t.ID is null
GO

select * from StagingFieldType where Name = 'scale'
select max(ID) from StagingFieldType
--select count(1) from [StagingXMLField]

/*
select * from [StagingXMLField] 
WHERE ObjectType + cast(ObjectID as varchar(10)) NOT in
(
	SELECT f.[ObjectType] + cast(f.[ObjectID] as varchar(10))
	  FROM	[StagingXMLField] f
			inner join [StagingXMLFieldType] t on f.TypeID = t.ObjectID and f.ObjectType + 'Type' = t.ObjectType and f.FieldName = t.Name
)

select * from [StagingXMLField] where ObjectType = 'FusionAttribute'
select * from [StagingXMLFieldType] where ObjectType = 'FusionAttributeType'

update [StagingXMLFieldType]
set ObjectType = 'FusionAttributeType'
where ObjectType = 'FusionAtributeType'
*/
delete D3S..FieldType
insert into D3S..FieldType
SELECT [CompanyID]
      ,[ID]
      ,[Name]
      ,[FriendlyName]
      ,[Description]
      ,[Type]
      ,[LookupObjectType]
      ,[LookupObjectID]
      ,[LookupDisplayFormat]
      ,[MinimumLength]
      ,[MaximumLength]
      ,[Length]
      ,[Pattern]
      ,[DateCreated]
      ,[CreatingResourceID]
      ,[DateUpdated]
      ,[UpdatingResourceID]
  FROM [dbo].[StagingFieldType]
GO

delete D3S..Field
insert into D3S..Field
SELECT [CompanyID]
      ,[ObjectType]
      ,[ObjectID]
      ,[FieldTypeID]
      ,[Value]
  FROM [dbo].[StagingField]
  where [FieldTypeID] in (select ID FROM D3S..FieldType)
GO

delete D3S..[FieldTypeRelation]
INSERT INTO D3S..[FieldTypeRelation]
           ([CompanyID]
           ,[FieldTypeID]
           ,[ObjectType]
           ,[ObjectID]
           ,[SortOrder]
           ,[IsRequired]
           ,[IsListable])
SELECT [CompanyID]
      ,[FieldTypeID]
      ,[ObjectType]
      ,[ObjectID]
      ,[SortOrder]
      ,[IsRequired]
      ,[IsListable]
  FROM [dbo].[StagingFieldTypeRelation]
GO

delete D3S..ArtifactType
INSERT INTO D3S..ArtifactType
SELECT 2
	  ,[ID]
      ,[ParentID]
      ,[Name]
      ,[Description]
      ,[IsHierarchical]
      ,[AllowModules]
      ,[DateCreated]
      ,[CreatingResourceID]
      ,[DateUpdated]
      ,[UpdatingResourceID]
  FROM [ArtifactType]
select count(1) from [ArtifactType]

delete D3S..ArtifactModuleType
INSERT INTO D3S..ArtifactModuleType
SELECT 2
	  ,[ID]
      ,[ArtifactTypeID]
      ,[Name]
      ,[Description]
      ,[DateCreated]
      ,[CreatingResourceID]
      ,[DateUpdated]
      ,[UpdatingResourceID]
  FROM [dbo].[ArtifactModuleType]
GO

delete D3S..TaxonomyType
INSERT INTO D3S..TaxonomyType
SELECT 2,[ID]
      ,[Name]
      ,[Description]
      ,[DateCreated]
      ,[CreatingResourceID]
      ,[DateUpdated]
      ,[UpdatingResourceID]
  FROM [dbo].[TaxonomyType]
GO


delete D3S..Artifact
go

ALTER TABLE D3S..[Artifact] DROP CONSTRAINT [FK_Artifact_Artifact]
GO
declare @c int
set @c = 2
INSERT INTO D3S..Artifact (CompanyID, ID, ParentID, ArtifactTypeID, TaxonomyTypeID, Name, Description, Status, Version, DateCreated, CreatingResourceID, DateUpdated, UpdatingResourceID)
SELECT @c,[ID]
      ,[ParentID]
      ,[ArtifactTypeID]
      ,[TaxonomyTypeID]
      ,[Name]
      ,[Description]
      ,'Open'
      ,[Version]
      ,[DateCreated]
      ,[CreatingResourceID]
      ,[DateUpdated]
      ,[UpdatingResourceID]
  FROM [dbo].[Artifact] 
  where 
		ArtifactTypeID in (SELECT ID FROM D3S..ArtifactType where CompanyID = @c)
		AND TaxonomyTypeID in (SELECT ID FROM D3S..TaxonomyType where CompanyID = @c)
GO
ALTER TABLE D3S..[Artifact]  WITH NOCHECK ADD  CONSTRAINT [FK_Artifact_Artifact] FOREIGN KEY([CompanyID], [ParentID])
REFERENCES D3S..[Artifact] ([CompanyID], [ID])
GO
ALTER TABLE D3S..[Artifact] CHECK CONSTRAINT [FK_Artifact_Artifact]
GO

--select count(1) from [Artifact] 

delete D3S..ArtifactModule
go

declare @c int
set @c = 2
INSERT INTO D3S..ArtifactModule
SELECT @c,[ID]
      ,[ArtifactModuleTypeID]
      ,[ArtifactID]
      ,[Name]
      ,[Description]
      ,[DateCreated]
      ,[CreatingResourceID]
      ,[DateUpdated]
      ,[UpdatingResourceID]
  FROM [dbo].[ArtifactModule]
  where 
		ArtifactID in (SELECT ID FROM D3S..Artifact where CompanyID = @c)
GO

ALTER TABLE D3S..[AttributeType] DROP CONSTRAINT [FK_AttributeType_AttributeType]
GO

delete D3S..AttributeType
GO

declare @c int
set @c = 2
INSERT INTO D3S..AttributeType
SELECT @c,[ID]
      ,[ParentID]
      ,[Name]
      ,[Description]
      ,[TextFormatString]
      ,[DateCreated]
      ,[CreatingResourceID]
      ,[DateUpdated]
      ,[UpdatingResourceID]
  FROM [dbo].[AttributeType]
GO
ALTER TABLE D3S..[AttributeType]  WITH NOCHECK ADD  CONSTRAINT [FK_AttributeType_AttributeType] FOREIGN KEY([CompanyID], [ParentID])
REFERENCES D3S..[AttributeType] ([CompanyID], [ID])
GO
ALTER TABLE D3S..[AttributeType] CHECK CONSTRAINT [FK_AttributeType_AttributeType]
GO
select count(1) from D3S..[AttributeType]
select count(1) from [AttributeType]

ALTER TABLE D3S..[Attribute] DROP CONSTRAINT [FK_Attribute_Attribute]
GO

delete D3S..[Attribute]

declare @c int
set @c = 2
INSERT INTO D3S..Attribute
SELECT @c,[ID]
      ,[ParentID]
      ,[AttributeTypeID]
      ,[ObjectType]
      ,[ObjectID]
      ,[DateCreated]
      ,[CreatingResourceID]
      ,[DateUpdated]
      ,[UpdatingResourceID]
  FROM [dbo].[Attribute]
  where AttributeTypeID in (select ID from D3S..AttributeType where CompanyID = @c)
GO
ALTER TABLE D3S..[Attribute]  WITH NOCHECK ADD  CONSTRAINT [FK_Attribute_Attribute] FOREIGN KEY([CompanyID], [ParentID])
REFERENCES D3S..[Attribute] ([CompanyID], [ID])
GO
ALTER TABLE D3S..[Attribute] CHECK CONSTRAINT [FK_Attribute_Attribute]
GO

select * from  [dbo].[Attribute]
  where AttributeTypeID not in (select ID from D3S..AttributeType where CompanyID = 2)
select count(1) from [Attribute]

delete D3S..AttributeTypeRelation
go

declare @c int
set @c = 2
INSERT INTO D3S..AttributeTypeRelation
SELECT @c,[ID]
      ,[ObjectType]
      ,[ObjectID]
  FROM [dbo].[AttributeTypeRelation]
  where ID in (select ID from D3S..AttributeType where CompanyID = @c)
GO

delete D3S..DomainListType

declare @c int
set @c = 2
INSERT INTO D3S..DomainListType
SELECT @c,[ID]
      ,[Name]
      ,[Description]
      ,[DateCreated]
      ,[CreatingResourceID]
      ,[DateUpdated]
      ,[UpdatingResourceID]
  FROM [dbo].[DomainListType]
GO

delete D3S..DomainListGroup
go

declare @c int
set @c = 2
INSERT INTO D3S..DomainListGroup
SELECT @c,[ID]
      ,[Name]
      ,[DomainListTypeID]
      ,[MasterListID]
	  ,getutcdate()
	  ,1
	  ,getutcdate()
	  ,1
  FROM [dbo].[DomainListGroup]
GO


ALTER TABLE D3S..[DomainList] DROP CONSTRAINT [FK_DomainList_DomainList]
GO

delete D3S..DomainList 
go

declare @c int
set @c = 2
INSERT INTO D3S..DomainList (CompanyID, ID, ParentID, DomainListTypeID, EnforceParentItemSelection, Name, Description, DomainListGroupID, DateCreated, CreatingResourceID, DateUpdated, UpdatingResourceID)
SELECT @c,[ID]
      ,[ParentID]
      ,[DomainListTypeID]
      ,[EnforceParentItemSelection]
      ,[Name]
      ,[Description]
      ,[DomainListGroupID]
      ,[DateCreated]
      ,[CreatingResourceID]
      ,[DateUpdated]
      ,[UpdatingResourceID]
  FROM [dbo].[DomainList]
GO
ALTER TABLE D3S..[DomainList]  WITH NOCHECK ADD  CONSTRAINT [FK_DomainList_DomainList] FOREIGN KEY([CompanyID], [ParentID])
REFERENCES D3S..[DomainList] ([CompanyID], [ID])
GO
ALTER TABLE D3S..[DomainList] CHECK CONSTRAINT [FK_DomainList_DomainList]
GO

delete D3S..DomainListItem
go

declare @c int
set @c = 2
INSERT INTO D3S..DomainListItem
SELECT @c,[ID]
      ,[Parents]
      ,[DomainListID]
      ,[Code]
      ,[Name]
      ,[Description]
      ,[DateCreated]
      ,[CreatingResourceID]
      ,[DateUpdated]
      ,[UpdatingResourceID]
  FROM [dbo].[DomainListItem]
  where DomainListID in (select ID from D3S..DomainList where CompanyID = @c)
GO

delete D3S..IntersectType
go

declare @c int
set @c = 2
INSERT INTO D3S..IntersectType (CompanyID, ID, DateCreated, CreatingResourceID, DateUpdated, UpdatingResourceID)
SELECT @c,[ID]
      ,[DateCreated]
      ,[CreatingResourceID]
      ,[DateUpdated]
      ,[UpdatingResourceID]
  FROM [dbo].[IntersectType]
GO

delete D3S..IntersectTypeNode
go

declare @c int
set @c = 2
INSERT INTO D3S..IntersectTypeNode
SELECT @c,[ID]
      ,[IntersectTypeID]
      ,[ObjectType]
      ,[ObjectID]
      ,[IsHierarchical]
      ,[Order]
  FROM [dbo].[IntersectTypeNode]
  where IntersectTypeID in (select ID from D3S..IntersectType)
GO

delete D3S..[Intersect] 
go

declare @c int
set @c = 2
INSERT INTO D3S..[Intersect] (CompanyID, ID, IntersectTypeID, ParentID, IsUsed, DateCreated, CreatingResourceID, DateUpdated, UpdatingResourceID)
SELECT @c,[ID]
	  ,[IntersectTypeID]
	  ,[ParentID]
	  ,[IsUsed]
      ,[DateCreated]
      ,[CreatingResourceID]
      ,[DateUpdated]
      ,[UpdatingResourceID]
  FROM [dbo].[Intersect]
  where IntersectTypeID in (select ID from D3S..IntersectType)
GO

select count(1) from [Intersect]
select count(1) from D3S..[Intersect]

delete D3S..IntersectNode
go

declare @c int
set @c = 2
INSERT INTO D3S..IntersectNode
SELECT @c,[ID]
      ,[IntersectTypeNodeID]
      ,[IntersectID]
      ,[ParentID]
      ,[ObjectType]
      ,[ObjectID]
  FROM [dbo].[IntersectNode]
  where --IntersectTypeNodeID in (select ID from D3S..IntersectTypeNode)
	--and 
	IntersectID in (select ID from D3S..[Intersect])
GO

delete D3S..LookupType
go

declare @c int
set @c = 2
INSERT INTO D3S..LookupType
SELECT @c,[ID]
      ,Name
	  ,[DateCreated]
      ,[CreatingResourceID]
      ,[DateUpdated]
      ,[UpdatingResourceID]
  FROM [dbo].LookupType
GO

delete D3S..Lookup
go

declare @c int
set @c = 2
INSERT INTO D3S..Lookup
SELECT @c
	  ,[ID]
      ,[LookupTypeID]
      ,[DateCreated]
      ,[CreatingResourceID]
      ,[DateUpdated]
      ,[UpdatingResourceID]
  FROM [dbo].[Lookup] where [LookupTypeID] in (SELECT ID FROM D3S..LookupType)
GO

delete D3S..Taxonomy
go

declare @c int
set @c = 2
INSERT INTO D3S..Taxonomy (CompanyID, ID, ParentID, TaxonomyTypeID, Name, DateCreated, CreatingResourceID ,DateUpdated, UpdatingResourceID)
SELECT @c
	  ,[ID]
	  ,ParentID
      ,[TaxonomyTypeID]
	  ,Name
      ,[DateCreated]
      ,[CreatingResourceID]
      ,[DateUpdated]
      ,[UpdatingResourceID]
  FROM [dbo].Taxonomy where [TaxonomyTypeID] in (SELECT ID FROM D3S..TaxonomyType) and ID <> PArentID 
GO

declare @c int
set @c = 2
INSERT INTO D3S..FusionType
SELECT @c
	  ,[ID]
	  ,Name
	  ,Description
	  ,FusionExtensionName
      ,getutcdate()
      ,1
      ,getutcdate()
      ,1
  FROM FusionType 
GO

declare @c int
set @c = 2
INSERT INTO D3S..FusionAttributeType (CompanyID, ID, ParentID, FusionTypeID, Name, Tab, Assignable, DateCreated, CreatingResourceID, DateUpdated, UpdatingResourceID)
SELECT @c
      ,[ID]
      ,[ParentID]
      ,[FusionTypeID]
      ,[Name]
      ,[Tab]
      ,[Assignable]
      ,[DateCreated]
      ,[CreatingResourceID]
      ,[DateUpdated]
      ,[UpdatingResourceID]
  FROM [FusionAttributeType]
GO

declare @c int
set @c = 2
INSERT INTO D3S..Fusion (CompanyID, ID, FusionTypeID, Name, Description, Enabled, Manual, DateCreated, CreatingResourceID, DateUpdated, UpdatingResourceID)
SELECT @c
      ,[ID]
      ,FusionTypeID
	  ,Name
	  ,Description
	  ,Enabled
	  ,Manual
      ,getutcdate()
      ,1
      ,getutcdate()
      ,1
  FROM [Fusion]
GO

declare @c int
set @c = 2
INSERT INTO D3S..FusionAttribute (CompanyID, ID, ParentID, Name, FusionID, FusionAttributeTypeID, DateCreated, CreatingResourceID, DateUpdated, UpdatingResourceID)
SELECT @c
      ,[ID]
      ,[ParentID]
	  ,Name
	  ,FusionID
      ,[FusionAttributeTypeID]
      ,[DateCreated]
      ,[CreatingResourceID]
      ,[DateUpdated]
      ,[UpdatingResourceID]
  FROM [FusionAttribute]
GO

declare @c int
set @c = 2
INSERT INTO D3S..FusionIntersectType
SELECT @c
      ,[ID]
      ,[Name]
      ,[ObjectType]
      ,[ObjectID]
      ,getutcdate()
      ,1
      ,getutcdate()
      ,1
  FROM [dbo].[FusionIntersectType]
GO

declare @c int
set @c = 2
INSERT INTO D3S..[FusionIntersect]
SELECT @c
      ,[ID]
      ,[FusionIntersectTypeID]
      ,[ObjectType]
      ,[ObjectID]
      ,[FusionAttributeID]
      ,getutcdate()
      ,1
      ,getutcdate()
      ,1
  FROM [dbo].[FusionIntersect]
GO


declare @c int
set @c = 2
INSERT INTO D3S..[TooltipTemplate]
SELECT @c
      ,[ID]
      ,[Name]
      ,[Action]
      ,[Description]
      ,[TemplateBody]
      ,[DateCreated]
      ,[CreatingResourceID]
      ,[DateUpdated]
      ,[UpdatingResourceID]
  FROM [dbo].[TooltipTemplate]
GO


declare @c int
set @c = 2
INSERT INTO D3S..EventType
SELECT @c
	  ,[ID]
      ,[ParentID]
      ,[Name]
      ,[Description]
      ,[MarkAsResolvedOnSynch]
      ,[CreatingResourceID]
      ,[DateCreated]
      ,[UpdatingResourceID]
      ,[DateUpdated]
  FROM [dbo].[EventType]
GO

declare @c int
set @c = 2
INSERT INTO D3S..EventGroup
SELECT @c
	  ,[ID]
      ,[EventTypeID]
      ,[RootEventTypeID]
      ,[Name]
      ,[PublicID]
      ,[CreatingResourceID]
      ,[DateCreated]
  FROM [dbo].[EventGroup]
GO

declare @c int
set @c = 2
INSERT INTO D3S..Event
SELECT @c
	  ,[ID]
      ,[EventTypeID]
      ,[RootEventTypeID]
      ,[EventGroupID]
      ,[SourceID]
      ,[Status]
      ,[CreatingResourceID]
      ,[DateCreated]
  FROM [dbo].[Event]
GO

select count(1) from FusionAttribute