/*
--------------------------------------------------------------------------------------
 This file contains a list of SQL files that need to be executed when releasing 
 to production in the next cycle.
--------------------------------------------------------------------------------------
*/


--Add CreatedOn column to Artifact

-- add column CreatedOn to artifact table not nullable default to current_timestamp
alter table [Artifact] add CreatedOn datetime not null constraint DF_Artifact_CreatedOn default(CURRENT_TIMESTAMP)
go

-- DISABLE TRIGGER SO WE DONT ADD A TON OF RECORDS TO UPDATE THINGS IN THE QUEUE
ALTER TABLE [Artifact] DISABLE TRIGGER Artifact_AfterUpdate
go

-- update all created on to 1/1/2011 so they all dont show up as new
update [Artifact] set CreatedOn = '1/1/2011';

-- update all createdon dates with the updatedon date if the exist
update [Artifact] set CreatedOn = UpdatedOn where UpdatedOn is not null;

-- go to audit table and get items created date and use this.
UPDATE
	artifact
SET
    artifact.CreatedOn = a.[date]
FROM
    [dbo].[artifact] at
INNER JOIN
    [reporting].[Global_Audit] a
ON 
    (a.[object] = 'Artifact' and a.actionobject = 'Artifact' and a.actionobjectid = at.id and a.objectid = at.id and a.[action] = 'Created');


-- REENABLE TRIGGER AFTER UPDATES

ALTER TABLE [Artifact] ENABLE TRIGGER Artifact_AfterUpdate
go


CREATE procedure [dbo].[AddSingleIntersect]
	@ResourceID int,
	@IntersectTypeID int,
	@Subject varchar(50),			-- The start object type.
	@SubjectID int,					-- The start object ID.
	@Object varchar(50),			-- The end object type.
	@ObjectID int,					-- The end object ID.	
	@Classification int,
	@Description nvarchar(4000)
as
begin
	set nocount on;

	declare @Date datetime = getutcdate(),
			@ErrorMessage nvarchar(2500),
			@IntersectID int,
			@SubjectIntersectTypeNodeID int,
			@SubjectIntersectNodeID int,
			@ObjectIntersectTypeNodeID int,
			@ObjectIntersectNodeID int

	select	@IntersectID = I.ID,
			@SubjectIntersectTypeNodeID = N1.IntersectTypeNodeID,	@SubjectIntersectNodeID = N1.ID,
			@ObjectIntersectTypeNodeID = N2.IntersectTypeNodeID,	@ObjectIntersectNodeID = N2.ID
	from	[Intersect] I
			inner join IntersectNode N1 on N1.IntersectID = I.ID and N1.ObjectType = @Subject and N1.ObjectID = @SubjectID
			inner join IntersectNode N2 on N2.IntersectID = I.ID and N2.ObjectType = @Object and N2.ObjectID = @ObjectID

	if @IntersectID is not null and @IntersectID > 0
		begin
			-- Update

			update	[Intersect]
			set		Classification = @Classification,
					Description = @Description
			where	ID = @IntersectID

			exec utility.AddAuditEntry 'Intersect', @IntersectID, @ResourceID, @Date, 'Updated', 'Intersect', @IntersectID
		end
	else
		begin
			-- Create

			declare @SubjectType varchar(50),
					@SubjectTypeID int,
					@ObjectType varchar(50),
					@ObjectTypeID int

			select	@SubjectType = ObjectType, @SubjectTypeID = ObjectTypeID	from cache.[Object] where [Object] = @Subject and ObjectID = @SubjectID 
			select	@ObjectType = ObjectType, @ObjectTypeID = ObjectTypeID		from cache.[Object] where [Object] = @Object and ObjectID = @ObjectID 

			select	distinct 
					@SubjectIntersectTypeNodeID = SourceIntersectTypeNodeID, 
					@ObjectIntersectTypeNodeID = TargetIntersectTypeNodeID
			from	utility.RelationshipTypes R 
			where	SourceObjectType = @SubjectType and SourceObjectID = @SubjectTypeID 
					and TargetObjectType = @ObjectType and TargetObjectID = @ObjectTypeID

			if @SubjectIntersectTypeNodeID is not null and @ObjectIntersectTypeNodeID is not null
				begin
					INSERT INTO [Intersect] (IntersectTypeID, Classification, [Description]) VALUES (@IntersectTypeID, @Classification, @Description)

					SELECT @IntersectID = SCOPE_IDENTITY()

					INSERT INTO IntersectNode	(IntersectTypeNodeID, IntersectID, ObjectType, ObjectID) 
					VALUES						(@SubjectIntersectTypeNodeID, @IntersectID, @Subject, @SubjectID)

					SELECT @SubjectIntersectNodeID = SCOPE_IDENTITY()

					INSERT INTO IntersectNode	(IntersectTypeNodeID, IntersectID, ObjectType, ObjectID)
					VALUES						(@ObjectIntersectTypeNodeID, @IntersectID, @Object, @ObjectID)

					SELECT @ObjectIntersectNodeID = SCOPE_IDENTITY()

					insert into cache.[Object] ( [Object], [ObjectID], [ObjectType], [ObjectTypeID] )
					values	( 'Intersect', @IntersectID, 'IntersectType', @IntersectTypeID );

					insert into cache.Relationship ( IntersectID, SourceIntersectTypeNodeID, SourceIntersectNodeID, SourceObject, SourceObjectID, TargetIntersectTypeNodeID, TargetIntersectNodeID, TargetObject, TargetObjectID )
					values	( @IntersectID, @SubjectIntersectTypeNodeID, @SubjectIntersectNodeID, @Subject, @SubjectID, @ObjectIntersectTypeNodeID, @ObjectIntersectNodeID, @Object, @ObjectID );
					insert into cache.Relationship ( IntersectID, SourceIntersectTypeNodeID, SourceIntersectNodeID, SourceObject, SourceObjectID, TargetIntersectTypeNodeID, TargetIntersectNodeID, TargetObject, TargetObjectID )
					values	( @IntersectID, @ObjectIntersectTypeNodeID, @ObjectIntersectNodeID, @Object, @ObjectID, @SubjectIntersectTypeNodeID, @SubjectIntersectNodeID, @Subject, @SubjectID );

					--Update the responsibilities of the object that should inherit form the other (Taxonomy can push relationships down to artifact)
					if ( (@Subject = 'Taxonomy' and @Object = 'Artifact') OR (@Subject = 'Artifact' and @Object = 'Taxonomy') )
						begin
							if @Subject = 'Artifact'
							begin
								exec [cache].[SynchronizeResponsibilitiesForObject] @Subject, @SubjectID
							end
							if @Object = 'Artifact'
							begin
								exec [cache].[SynchronizeResponsibilitiesForObject] @Object, @ObjectID
							end
						end

					exec utility.AddAuditEntry @Subject, @SubjectID, @ResourceID, @Date, 'Created', 'Intersect', @IntersectID
					exec utility.AddAuditEntry @Object, @ObjectID, @ResourceID, @Date, 'Created', 'Intersect', @IntersectID
				end
		end

	select * from [Intersect] where ID = @IntersectID
end
go

delete cache.Object where Object = 'Artifact' and ObjectID not in (select ID from Artifact)
delete cache.Object where Object = 'ArtifactType' and ObjectID not in (select ID from ArtifactType)
delete cache.Object where Object = 'Domain' and ObjectID not in (select ID from Domain)
delete cache.Object where Object = 'DomainType' and ObjectID not in (select ID from DomainType)
delete cache.Object where Object = 'Intersect' and ObjectID not in (select ID from [Intersect])
delete cache.Object where Object = 'IntersectType' and ObjectID not in (select ID from [IntersectType])
delete cache.Object where Object = 'LookupType' and ObjectID not in (select ID from [LookupType])
delete cache.Object where Object = 'Lookup' and ObjectID not in (select ID from [Lookup])
delete cache.Object where Object = 'AttributeType' and ObjectID not in (select ID from [AttributeType])
delete cache.Object where Object = 'Taxonomy' and ObjectID not in (select ID from Taxonomy)
delete cache.Object where Object = 'TaxonomyType' and ObjectID not in (select ID from TaxonomyType)
go

alter table cache.[Object] drop column [Name]
alter table cache.[Object] drop column [TextPath]
alter table cache.[Object] drop column [Description]
alter table cache.[Object] drop column [Parent]
alter table cache.[Object] drop column [ParentID]
alter table cache.[Object] drop column [ParentName]
alter table cache.[Object] drop column [Url]
alter table cache.[Object] drop column [ObjectTypeName]
alter table cache.[Object] drop column [IconBackColor]
alter table cache.[Object] drop column [IconForeColor]
alter table cache.[Object] drop column [IconText]

[cache].[ReSynchronizeAllObjectDetails]


alter view [cache].[ObjectDetails]
as
	select	D.[Object],
			D.[ObjectID],
			coalesce(O1.Name, O2.Name, O3.Name, O4.Name, O5.Name, O6.Name, O7.Name, O8.Name, O9.Name, O10.Name, O11.Name, O12.Name, O13.Name, case when O14.ResourceID is not null then O14.FirstName + ' ' + O14.LastName else null end, O15.Name, O16.Name, O17.Name, O18.Name, O19.Name, O21.Name, O22.Name, O23.Name, O24.Name, null) as Name,
			coalesce(O1.TextPath, O2.TextPath, O3.Name, O4.TextPath, O5.Name, O6.Name, O7.Name, O8.Name, O9.Name, O10.Name, O11.Name, O12.Name, O13.TextPath, case when O14.ResourceID is not null then O14.FirstName + ' ' + O14.LastName else null end, O15.Name, O16.Name, O17.TextPath, O18.Name, O19.Name, O21.Name, O22.Name, O23.Name, O24.Name, '') as TextPath,
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
			coalesce(OT1.Name, OT2.Name, OT3.Name, OT4.TextPath, OT5.Name, OT12.Name, OT13.Name, OT14.Name, OT15.Name, OT20.Name, OT24.Name, NULL) as ObjectTypeName,
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

			left join reporting.Global_Resource O14 with(nolock) on D.[Object] = 'Resource' and O14.ResourceID = D.ObjectID and O14.Status = 'Active'
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

			left join ObjectStyle S with(nolock) on S.ObjectType = D.ObjectType and S.ObjectID = D.[ObjectTypeID]

GO





ALTER procedure [cache].[ReSynchronizeAllObjectDetails]
as
begin
	set nocount on;

	IF OBJECT_ID('tempdb..#Recache') IS NOT NULL
    DROP TABLE #Recache

	create table #Recache (
		[Object] varchar(50) not null,
		ObjectID int not null,
		ObjectType varchar(25) not null,
		ObjectTypeID int not null
	);

	declare @type varchar(50);
	
	begin
		set @type = 'Artifact'
		insert into #Recache
			SELECT	@type, ID, 'ArtifactType', ArtifactTypeID FROM Artifact;
	end;

	begin
		set @type = 'ArtifactType'
		insert into #Recache
			SELECT	@type, ID, @type, ID FROM ArtifactType;
	end;

	begin
		set @type = 'AttributeType';
		insert into #Recache
			SELECT	@type, ID, 'AttributeType', ID FROM AttributeType;
	end;

	begin
		set @type = 'Domain';
		insert into #Recache
			SELECT	@type, ID, 'DomainType', DomainTypeID FROM Domain;
	end;

	begin
		set @type = 'DomainType';
		insert into #Recache
			SELECT	@type, ID, @type, ID FROM DomainType;
	end;

	begin
		set @type = 'Group';
		insert into #Recache
			SELECT	@type, ID, 'GroupType', 1 FROM [Group];
	end;

	begin
		set @type = 'Intersect';
		insert into #Recache
			SELECT	@type, ID, 'IntersectType', IntersectTypeID FROM [Intersect];
	end;

	begin
		set @type = 'IntersectType';
		insert into #Recache
			SELECT	@type, ID, @type, ID FROM IntersectType;
	end;

	begin
		set @type = 'Event';
		insert into #Recache
			SELECT	@type, O.ID, 'Rule', R.ID
			FROM	[Event] O
					INNER JOIN EventGroup G on G.ID = O.EventGroupID
					INNER JOIN [Rule] R on R.ID = G.RuleID;
	end;

	begin
		set @type = 'EventGroup';
		insert into #Recache
			SELECT	@type, ID, 'Rule', RuleID FROM EventGroup;
	end;

	begin
		set @type = 'Lookup';
		insert into #Recache
			SELECT	@type, ID, 'LookupType', LookupTypeID FROM [Lookup];
	end;

	begin
		set @type = 'LookupType';
		insert into #Recache
			SELECT	@type, ID, @type, ID FROM LookupType;
	end;

	begin
		set @type = 'Fusion';
		insert into #Recache
			SELECT	@type, ID, 'FusionType', FusionTypeID FROM Fusion;
	end;

	begin
		set @type = 'FusionType';
		insert into #Recache
			SELECT	@type, ID, @type, ID FROM FusionType;
	end;

	begin
		set @type = 'FusionAttribute';
		insert into #Recache
			SELECT	@type, ID, 'FusionAttributeType', FusionAttributeTypeID FROM FusionAttribute;
	end;
 
	begin
		set @type = 'FusionAttributeType';
		insert into #Recache
			SELECT	@type, ID, 'FusionType', FusionTypeID FROM FusionAttributeType;
	end;

	begin
		set @type = 'GroupType';
		insert into #Recache values (@type, 0, @type, 0);
		insert into #Recache values (@type, 1, @type, 0);
	end;

	begin
		set @type = 'Policy';
		insert into #Recache
			SELECT	@type, ID, 'PolicyType', PolicyTypeID FROM [Policy];
	end;

	begin
		set @type = 'PolicyType';
		insert into #Recache
			SELECT	@type, ID, 'PolicyType', ID FROM [PolicyType];
	end;

	begin
		set @type = 'Resource';
		insert into #Recache
			select	@type, ResourceID, 'ResourceType', 1 from reporting.Global_Resource;
	end;

	begin
		set @type = 'ResourceType';
		insert into #Recache values (@type, 0, @type, 0)
		insert into #Recache values (@type, 1, @type, 0)
	end;

	begin
		set @type = 'ResponsibilityType';
		insert into #Recache
			SELECT	@type, ID, @type, 0 FROM ResponsibilityType;
	end;

	begin
		INSERT INTO #Recache VALUES ('RuleType', 1, 'RuleType', 0)
		INSERT INTO #Recache VALUES ('RuleType', 2, 'RuleType', 0)
		INSERT INTO #Recache VALUES ('RuleType', 3, 'RuleType', 0)
		INSERT INTO #Recache VALUES ('RuleType', 4, 'RuleType', 0)

		set @type = 'Rule';
		insert into #Recache
			SELECT	@type, ID, 'RuleType', RuleType FROM [Rule];
	end;

	begin
		set @type = 'Taxonomy';
		insert into #Recache
			SELECT	@type, ID, 'TaxonomyType', TaxonomyTypeID FROM Taxonomy
	end;

	begin
		set @type = 'TaxonomyType';
		insert into #Recache
			SELECT	@type, ID, @type, ID FROM TaxonomyType;
	end;

	-- upsert the individual object into the cache table.
	merge	cache.[Object] as T
	using	(
			SELECT	*
			FROM	#Recache
			) as S
	on		(
			T.[Object] = S.[Object] and T.[ObjectID] = S.[ObjectID]
			)
	when matched then
			update	
			set		T.ObjectType = S.ObjectType,
					T.ObjectTypeID = S.ObjectTypeID
	when not matched then
			insert ( [Object], ObjectID, ObjectType, ObjectTypeID )
			values ( S.[Object], S.ObjectID, S.ObjectType, S.ObjectTypeID );
end
go