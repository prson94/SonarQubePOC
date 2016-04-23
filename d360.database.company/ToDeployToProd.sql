drop table [IntersectMapTransformation]
go
drop table TagRelation
go
drop table Tag
go
drop table Transformation
go
drop table [queue].Analytic
go
drop table [queue].FollowUpdate
go
drop table [queue].Fusion
go
drop table [queue].FusionCache
go
drop table [queue].[Notification]
go
drop table [queue].ObjectCache
go
drop table [queue].ObjectIndex
go
drop table [queue].ObjectStyleCache
go
drop table [queue].ObjectVersion
go
drop view EventHeader
go
drop view RelationshipAggregate
go
drop view fusion.LeafAttributes
go
drop procedure [tile].[GetChildArtifactStatisticsByObject]
go
drop procedure [tile].[GetRedFlagsByTypeAndResource]
go
drop procedure [tile].[GetRedFlagSummariesByResource]
go
drop procedure [tile].[GetSocialStatisticsByObject]
go


alter table StatisticType add [Object] varchar(50) NULL
go
alter table StatisticType add [ObjectID] int NULL
go
alter table StatisticType add Score int NULL
go

ALTER TABLE [dbo].[Statistic] DROP CONSTRAINT [FK_Statistic_StatisticType]
GO

set nocount on;
declare @tbl table (ID int identity, StatisticTypeID int, [Object] varchar(50), ObjectID int, Score int)
insert into @tbl
	select	S.ID,
			R.ObjectType,
			R.ObjectID,
			R.Score
	from	StatisticType S
			inner join StatisticTypeRelation R on R.StatisticTypeID = S.ID

declare @current int = 1,
		@max int
select @max = max(ID) from @tbl

while @current <= @max
begin
	declare @obj varchar(50),
			@objID int,
			@statisticTypeID int,
			@score int

	select	@statisticTypeID = StatisticTypeID,
			@obj = [Object],
			@objID = ObjectID,
			@score = Score
	from	@tbl	
	where	ID = @current

	declare @cObj varchar(50),
			@cObjID int,
			@nStatisticTypeID int

	select	@cObj = [Object],
			@cObjID = ObjectID
	from	StatisticType 
	where	ID = @statisticTypeID

	--select @statisticTypeID, @obj, @objID, @cObj, @cObjID
	if @cObj is null and (@cObjID is null or @cObjID = 0)
		begin
			update	StatisticType
			set		[Object] = @obj,
					ObjectID = @objID,
					Score = @score
			where	ID = @statisticTypeID
		end
	else
		begin
			insert into StatisticType (Name, Description, CheckType, PartOfScore, Configuration, UpdatedOn, UpdatedBy, [Object], ObjectID, Score)
				select	Name,
						Description,
						CheckType,
						PartOfScore,
						Configuration,
						UpdatedOn,
						UpdatedBy,
						@obj,
						@objID,
						@score
				from	StatisticType
				where	ID = @statisticTypeID

			set @nStatisticTypeID = SCOPE_IDENTITY()

			update	T
			set		T.StatisticTypeID = @nStatisticTypeID
			from	Statistic T
					inner join cache.[Object] S on S.[Object] = T.[ObjectType] and S.ObjectID = T.[ObjectID] and S.[ObjectType] = @obj and S.[ObjectTypeID] = @objID

		end

	set @current = @current + 1
end

update	T
set		T.StatisticTypeID = NTY.ID
from	Statistic T
		inner join cache.[Object] S on S.[Object] = T.[ObjectType] and S.ObjectID = T.[ObjectID]
		inner join StatisticType NTY on NTY.[Object] = S.ObjectType and NTY.ObjectID = S.ObjectTypeID
		inner join StatisticType OTY on OTY.Name = NTY.Name and OTY.ID = T.StatisticTypeID and NTY.ID <> OTY.ID
go

alter table FieldType add [Category] NVARCHAR (250) NULL
go

alter table SourceRule add [IsTemplate] BIT DEFAULT ((0)) NOT NULL
go

alter table SourceRule drop column [AppliesToObjectList]
go

alter VIEW [dbo].[FieldTypeWithRelation]
AS
	SELECT	T.ID,
			T.Name,
			T.FriendlyName,
			T.Category,
			T.Description,
			T.DisplayDescription,
			T.FormDescription,
			T.ValidationDescription,
			T.Type,
			T.LookupObjectType,
			T.LookupObjectID ,
			T.LookupDisplayFormat,
			T.Length,
			T.MinimumLength,
			T.MaximumLength,
			T.Pattern,
			T.[Object],
			T.ObjectID,
			D.Name as ObjectName,
			T.IsListable,
			T.IsRequired,
			T.SortOrder
	FROM	FieldType T
			inner join cache.ObjectDetails D on D.[Object] = T.[Object] and D.ObjectID = T.ObjectID
GO

alter VIEW [dbo].[FieldWithRelation]
AS
	SELECT	F.FieldTypeID,
			T.Name,
			T.FriendlyName,
			T.Category,
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

ALTER FUNCTION [utility].[DeriveIntersectTypeName] 
(
--declare
	@id int
--set @id = 67
)
RETURNS nvarchar(500)
AS
BEGIN
	DECLARE @result nvarchar(500)

	SET @result =	(
					SELECT	COALESCE(
									A.Name,
									T.Name,
									D.Name, 
									FA.TextPath,
									II.Name,
									G.Name,
									R.Name,
									P.Name,
									RE.Name,
									''
									) + ' / '
					FROM	IntersectTypeNode I
							LEFT OUTER JOIN ArtifactType A							ON	I.ObjectType = 'ArtifactType'			and A.ID = I.ObjectID
							LEFT OUTER JOIN [IntersectType] II						ON	I.ObjectType = 'IntersectType'			and II.ID = I.ObjectID and II.ID <> @id
							LEFT OUTER JOIN TaxonomyType T							ON	I.ObjectType = 'TaxonomyType'			and T.ID = I.ObjectID
							LEFT OUTER JOIN DomainType D							ON	I.ObjectType = 'DomainType'				and D.ID = I.ObjectID
							LEFT OUTER JOIN FusionAttributeType FA					ON  I.ObjectType = 'FusionAttributeType'	and FA.ID = I.ObjectID
							LEFT OUTER JOIN (select 'Group' as Name, 0 as ID) G		ON	I.ObjectType = 'Group'					and G.ID = I.ObjectID
							LEFT OUTER JOIN (select 'Resource' as Name, 1 as ID) RE	ON	I.ObjectType = 'Resource'				and RE.ID = I.ObjectID
							LEFT OUTER JOIN (
											select 1 as ID, 'Informational Rule' as Name
											union
											select 2 as ID, 'Quality Check Rule' as Name
											union
											select 3 as ID, 'Metric Rule' as Name
											union
											select 4 as ID, 'Profile Rule' as Name
											) R										ON	I.ObjectType = 'Rule'					and R.ID = I.ObjectID
							LEFT OUTER JOIN [PolicyType] P							ON	I.ObjectType = 'PolicyType'				and P.ID = I.ObjectID
					WHERE	I.IntersectTypeID = @id
							and @@NESTLEVEL < 6
					ORDER BY I.[Order]
					FOR XML PATH('')
					)

	IF @Result IS NULL 
		SET @result = 'Name cannot be resolved'
	ELSE
		SET @result = SUBSTRING(@result, 1, LEN(@result) - 2)

	RETURN @result
END
GO

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

			left join ObjectStyle S with(nolock) on S.ObjectType = D.ObjectType and S.ObjectID = D.[ObjectTypeID]
GO

drop TABLE [dbo].[StatisticTypeRelation]
go

drop VIEW [dbo].[StatisticTypeRelationDetail]
go

alter procedure [dbo].[AsyncDeleteObject]
	@Object varchar(50),
	@ObjectID int,
	@ParentObject varchar(50),
	@ParentObjectID int,
	@ResourceID int
as
begin
	set nocount on;
	
	declare @trans varchar(25) = 'Trans',
			@current int = 1,
			@max int,
			@date datetime = getutcdate()

	begin try
		exec [utility].[AddAuditEntry] @ParentObject, @ParentObjectID, @ResourceID, @date, 'Removed', @Object, @ObjectID
	end try
	begin catch

	end catch

	begin try
		begin transaction @trans

		--INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID], [Priority])
		--values ('ObjectIndex', 'D', @Object, @ObjectID, 4)

		--COMMON
		delete CommentRelation					where ObjectType = @Object and ObjectID = @ObjectID
		delete Field							where ObjectType = @Object and ObjectID = @ObjectID
		delete Follow							where ObjectType = @Object and ObjectID = @ObjectID
		delete Responsibility					where ObjectType = @Object and ObjectID = @ObjectID
		delete SurveyObjectCache				where ObjectType = @Object and ObjectID = @ObjectID
		delete cache.[Object]					where [Object] = @Object and ObjectID = @ObjectID

		if charindex('Type', @Object) > 0
		begin
			delete AttributeTypeRelation			where ObjectType = @Object AND ObjectID = @ObjectID
			delete FieldType						where [Object] = @Object AND ObjectID = @ObjectID
			delete ResponsibilityTypeRelation		where ObjectType = @Object and ObjectID = @ObjectID
			delete ResponsibilityTypeObjectClaim	where ObjectType = @Object and ObjectID = @ObjectID
			delete StatisticType					where [Object] = @Object and ObjectID = @ObjectID
			delete WorkflowTypeRelation				where [Object] = @Object and ObjectID = @ObjectID

			if @Object in ('AttributeTypeRelation', 'AttributeTypeRelation', 'ResponsibilityTypeRelation', 'ResponsibilityType')
			begin
				exec utility.CalculateStatistics
			end

			if @Object = 'ArtifactType'
			begin
				declare @ah table (ID int);
				with ah as	(
							select	ID, 
									ParentID
							from	Artifact
							where	ID = @ObjectID
							union all
							select	C.ID,
									C.ParentID
							from	Artifact C
									inner join ah P on P.ID = C.ParentID
							)
				insert into @ah 
					select ID from ah
			
				delete Artifact where ID in (select ID from @ah)
			end

			if @Object = 'AttributeType'
			begin
				delete AttributeTypeRelation		where AttributeTypeID = @ObjectID
			
				declare @ath table (RowID int identity, ID int, ParentID int null, [Level] int);
				with ath as	(
							select	ID,
									ParentID,
									1 as [Level]
							from	AttributeType
							where	ID = @ObjectID
							union all
							select	C.ID,
									C.ParentID,
									P.[Level] + 1 as [Level]
							from	AttributeType C
									inner join ath P on P.ID = C.ParentID
							)
				insert into @ath 
					select ID, ParentID, [Level] from ath order by [Level] desc

				select @max = max(RowID) from @ath

				while @current <= @max
				begin
					declare @attributeTypeID int
					select @attributeTypeID = ID from @ath where RowID = @current
					delete Attribute where AttributeTypeID = @attributeTypeID
					delete AttributeType where ID = @attributeTypeID
					set @current = @current + 1
				end
			end

			if @Object = 'DomainType'
			begin
				delete DomainItem where DomainID in (select ID from Domain where DomainTypeID = @ObjectID)
				delete Domain where DomainTypeID = @ObjectID
				delete DomainGroup where DomainTypeID = @ObjectID
			end

			if @Object = 'FieldType'
			begin
				delete Field where FieldTypeID = @ObjectID
				delete FieldTypeFusionLookupDisplayField where FieldTypeID = @ObjectID
				delete FieldTypeRelationLookupDisplayField where FieldTypeID = @ObjectID
				delete FieldType where ID = @ObjectID
			end

			if @Object = 'FusionAttributeType'
			begin
				declare @fath table (RowID int identity, ID int, ParentID int null, [Level] int);
				with fath as	(
							select	ID,
									ParentID,
									1 as [Level]
							from	FusionAttributeType
							where	ID = @ObjectID
							union all
							select	C.ID,
									C.ParentID,
									P.[Level] + 1 as [Level]
							from	FusionAttributeType C
									inner join fath P on P.ID = C.ParentID
							)
				insert into @fath 
					select ID, ParentID, [Level] from fath order by [Level] desc

				select @max = max(RowID) from @fath

				while @current <= @max
				begin
					declare @fusionAttributeTypeID int
					select @fusionAttributeTypeID = ID from @fath where RowID = @current
					delete FusionAttribute where FusionAttributeTypeID = @fusionAttributeTypeID
					delete FusionAttributeType where ID = @fusionAttributeTypeID
					set @current = @current + 1
				end
			end

			if @Object = 'FusionType'
			begin
				declare @fth table (RowID int identity, ID int, ParentID int null, [Level] int);
				with fth as	(
							select	ID,
									ParentID,
									1 as [Level]
							from	FusionAttributeType
							where	FusionTypeID = @ObjectID and ParentID is null
							union all
							select	C.ID,
									C.ParentID,
									P.[Level] + 1 as [Level]
							from	FusionAttributeType C
									inner join fth P on P.ID = C.ParentID
							)
				insert into @fth 
					select ID, ParentID, [Level] from fth order by [Level] desc

				select @max = max(RowID) from @fth

				while @current <= @max
				begin
					declare @fattributeTypeID int
					select @fattributeTypeID = ID from @fth where RowID = @current
					delete FusionAttribute where FusionAttributeTypeID = @fattributeTypeID
					delete FusionAttributeType where ID = @fattributeTypeID
					set @current = @current + 1
				end
				delete FusionType where ID = @ObjectID
			end

			if @Object = 'IntersectType'
			begin
				-- Stores the sources we have identified through the loop below.
				declare @tblRelationshipIDs table (ID int)

				--Seed initial tables values
				insert into @tblRelationshipIDs
					select	R.ID 
					from	Responsibility R
							inner join [Intersect] I on I.IntersectTypeID = 2 and R.ObjectType = 'Intersect' and R.ObjectID = I.ID 

				-- follow trail all the way back.
				while exists(
						select	1 
						from	Responsibility
						where	TargetResponsibilityID in (select ID from @tblRelationshipIDs)
								and ID not in (select ID from @tblRelationshipIDs)
				)
				begin
					insert into @tblRelationshipIDs
						select	ID
						from	Responsibility
						where	TargetResponsibilityID in (select ID from @tblRelationshipIDs)
								and ID not in (select ID from @tblRelationshipIDs)
				end

				delete Responsibility where ID in (select ID from @tblRelationshipIDs)

				delete [Intersect] where IntersectTypeID = @ObjectID
				delete IntersectType where ID = @ObjectID
			end

			if @Object = 'LookupType'
			begin
				delete [Lookup] where LookupTypeID = @ObjectID
			end

			if @Object = 'PolicyType'
			begin
				delete Policy where PolicyTypeID = @ObjectID
				delete PolicyTypeLevel where PolicyTypeID = @ObjectID
			end

			if @Object = 'ResponsibilityType'
			begin
				delete Responsibility where ResponsibilityTypeID = @ObjectID
				delete ResponsibilityType where ID = @ObjectID
			end

			if @Object = 'SurveyType'
			begin
				delete SurveyObjectCache where SurveyTypeID = @ObjectID
				delete Survey where SurveyTypeID = @ObjectID
				delete SurveyType where ID = @ObjectID
			end

			if @Object = 'TaxonomyType'
			begin
				delete Taxonomy where TaxonomyTypeID = @ObjectID
				delete TaxonomyTypeLevel where TaxonomyTypeID = @ObjectID
				delete TaxonomyType where ID = @ObjectID
			end

		end
		else
		begin
			delete Attribute							where ObjectType = @Object and ObjectID = @ObjectID
			delete cache.Relationship					where [SourceObject] = @Object and SourceObjectID = @ObjectID
			delete cache.Relationship					where [TargetObject] = @Object and TargetObjectID = @ObjectID

			BEGIN TRY
				DECLARE @tblIntersectIDs table (ID int)

				INSERT INTO @tblIntersectIDs
					SELECT	IntersectID
					FROM	IntersectNode
					WHERE	ObjectType = @Object and ObjectID = @ObjectID

				delete [Intersect] where ID in (select ID from @tblIntersectIDs)
			END TRY
			BEGIN CATCH

			END CATCH

			if @Object = 'Artifact'
			begin
				delete	RelatedArtifact where ArtifactID = @ObjectID
			end

			if @Object = 'Domain'
			begin
				delete DomainItem where DomainID = @ObjectID
			end

			if @Object = 'Responsibility'
			begin
				exec cache.SynchronizeResponsibilitiesForObject @ParentObject, @ParentObjectID 
			end

			if @Object = 'Taxonomy'
			begin
				declare @th table (ID int);
				with th as	(
							select	ID, 
									ParentID
							from	Taxonomy
							where	ID = @ObjectID
							union all
							select	C.ID,
									C.ParentID
							from	Taxonomy C
									inner join th P on P.ID = C.ParentID
							)
				insert into @th 
					select ID from th
			
				delete Taxonomy where ID in (select ID from @th)

				exec cache.SynchronizeResponsibilities
			end
		end
		
		commit transaction @trans
	end try
	begin catch
		DECLARE @ErrorMessage NVARCHAR(4000);
		DECLARE @ErrorSeverity INT;
		DECLARE @ErrorState INT;

		SELECT 
			@ErrorMessage = ERROR_MESSAGE(),
			@ErrorSeverity = ERROR_SEVERITY(),
			@ErrorState = ERROR_STATE();

		-- Use RAISERROR inside the CATCH block to return error
		-- information about the original error that caused
		-- execution to jump to the CATCH block.
		RAISERROR (@ErrorMessage, -- Message text.
				   @ErrorSeverity, -- Severity.
				   @ErrorState -- State.
				   );

		rollback transaction @trans
	end catch
end
GO

alter procedure [dbo].[DeleteObject]
	@Obj varchar(50),
	@ObjectID int,
	@ResourceID int
as
begin
	set nocount on;
	
	declare @Object varchar(50) = @Obj,
			@trans varchar(25) = 'Trans',
			@current int = 1,
			@max int

	begin try
		begin transaction @trans

		INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        VALUES (
				'ObjectVersion', 
				'<fields>
				 <Action>Removed</Action>
				 <ActionObject>' + @Obj + '</ActionObject>
				 <ActionObjectID>' + cast(@ObjectID as varchar) + '</ActionObjectID>
				 <ResourceID>' + cast(@ResourceID as varchar) + '</ResourceID>
				</fields>', 
				@Obj, 
				@ObjectID)

		INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		values ('ObjectIndex', 'D', @Obj, @ObjectID)

		--COMMON
		delete CommentRelation					where ObjectType = @Object and ObjectID = @ObjectID
		delete Field							where ObjectType = @Object and ObjectID = @ObjectID
		delete Follow							where ObjectType = @Object and ObjectID = @ObjectID
		delete Responsibility					where ObjectType = @Object and ObjectID = @ObjectID
		delete SurveyObjectCache				where ObjectType = @Object and ObjectID = @ObjectID
		delete cache.[Object]					where [Object] = @Object and ObjectID = @ObjectID

		if charindex('Type', @Object) > 0
		begin
			delete AttributeTypeRelation			where ObjectType = @Object AND ObjectID = @ObjectID
			delete FieldType						where [Object] = @Object AND ObjectID = @ObjectID
			delete ResponsibilityTypeRelation		where ObjectType = @Object and ObjectID = @ObjectID
			delete ResponsibilityTypeObjectClaim	where ObjectType = @Object and ObjectID = @ObjectID
			delete StatisticType					where [Object] = @Object and [ObjectID] = @ObjectID
			delete WorkflowTypeRelation				where [Object] = @Object and ObjectID = @ObjectID

			if @Object = 'ArtifactType'
			begin
				declare @ah table (ID int);
				with ah as	(
							select	ID, 
									ParentID
							from	Artifact
							where	ID = @ObjectID
							union all
							select	C.ID,
									C.ParentID
							from	Artifact C
									inner join ah P on P.ID = C.ParentID
							)
				insert into @ah 
					select ID from ah
			
				delete Artifact where ID in (select ID from @ah)
			end

			if @Object = 'AttributeType'
			begin
				delete AttributeTypeRelation		where AttributeTypeID = @ObjectID
			
				declare @ath table (RowID int identity, ID int, ParentID int null, [Level] int);
				with ath as	(
							select	ID,
									ParentID,
									1 as [Level]
							from	AttributeType
							where	ID = @ObjectID
							union all
							select	C.ID,
									C.ParentID,
									P.[Level] + 1 as [Level]
							from	AttributeType C
									inner join ath P on P.ID = C.ParentID
							)
				insert into @ath 
					select ID, ParentID, [Level] from ath order by [Level] desc

				select @max = max(RowID) from @ath

				while @current <= @max
				begin
					declare @attributeTypeID int
					select @attributeTypeID = ID from @ath where RowID = @current
					delete Attribute where AttributeTypeID = @attributeTypeID
					delete AttributeType where ID = @attributeTypeID
					set @current = @current + 1
				end
			end

			if @Object = 'DomainType'
			begin
				delete DomainItem where DomainID in (select ID from Domain where DomainTypeID = @ObjectID)
				delete Domain where DomainTypeID = @ObjectID
				delete DomainGroup where DomainTypeID = @ObjectID
			end

			if @Object = 'FieldType'
			begin
				delete Field where FieldTypeID = @ObjectID
				delete FieldType where ID = @ObjectID
			end

			if @Object = 'FusionAttributeType'
			begin
				declare @fath table (RowID int identity, ID int, ParentID int null, [Level] int);
				with fath as	(
							select	ID,
									ParentID,
									1 as [Level]
							from	FusionAttributeType
							where	ID = @ObjectID
							union all
							select	C.ID,
									C.ParentID,
									P.[Level] + 1 as [Level]
							from	FusionAttributeType C
									inner join fath P on P.ID = C.ParentID
							)
				insert into @fath 
					select ID, ParentID, [Level] from fath order by [Level] desc

				select @max = max(RowID) from @fath

				while @current <= @max
				begin
					declare @fusionAttributeTypeID int
					select @fusionAttributeTypeID = ID from @fath where RowID = @current
					delete FusionAttribute where FusionAttributeTypeID = @fusionAttributeTypeID
					delete FusionAttributeType where ID = @fusionAttributeTypeID
					set @current = @current + 1
				end
			end

			if @Object = 'FusionType'
			begin
				declare @fth table (RowID int identity, ID int, ParentID int null, [Level] int);
				with fth as	(
							select	ID,
									ParentID,
									1 as [Level]
							from	FusionAttributeType
							where	FusionTypeID = @ObjectID and ParentID is null
							union all
							select	C.ID,
									C.ParentID,
									P.[Level] + 1 as [Level]
							from	FusionAttributeType C
									inner join fth P on P.ID = C.ParentID
							)
				insert into @fth 
					select ID, ParentID, [Level] from fth order by [Level] desc

				select @max = max(RowID) from @fth

				while @current <= @max
				begin
					declare @fattributeTypeID int
					select @fattributeTypeID = ID from @fth where RowID = @current
					delete FusionAttribute where FusionAttributeTypeID = @fattributeTypeID
					delete FusionAttributeType where ID = @fattributeTypeID
					set @current = @current + 1
				end
				delete FusionType where ID = @ObjectID
			end

			if @Object = 'IntersectType'
			begin
				-- Stores the sources we have identified through the loop below.
				declare @tblRelationshipIDs table (ID int)

				--Seed initial tables values
				insert into @tblRelationshipIDs
					select	R.ID 
					from	Responsibility R
							inner join [Intersect] I on I.IntersectTypeID = 2 and R.ObjectType = 'Intersect' and R.ObjectID = I.ID 

				-- follow trail all the way back.
				while exists(
						select	1 
						from	Responsibility
						where	TargetResponsibilityID in (select ID from @tblRelationshipIDs)
								and ID not in (select ID from @tblRelationshipIDs)
				)
				begin
					insert into @tblRelationshipIDs
						select	ID
						from	Responsibility
						where	TargetResponsibilityID in (select ID from @tblRelationshipIDs)
								and ID not in (select ID from @tblRelationshipIDs)
				end

				delete Responsibility where ID in (select ID from @tblRelationshipIDs)

				delete [Intersect] where IntersectTypeID = @ObjectID
				delete IntersectType where ID = @ObjectID
			end

			if @Object = 'LookupType'
			begin
				delete [Lookup] where LookupTypeID = @ObjectID
			end

			if @Object = 'PolicyType'
			begin
				delete Policy where PolicyTypeID = @ObjectID
				delete PolicyTypeLevel where PolicyTypeID = @ObjectID
			end

			if @Object = 'ResponsibilityType'
			begin
				delete Responsibility where ResponsibilityTypeID = @ObjectID
				delete ResponsibilityType where ID = @ObjectID
			end

			--if @Object = 'StatisticType'
			--begin
			--	delete [Statistic] where StatisticTypeID = @ObjectID
			--end

			if @Object = 'SurveyType'
			begin
				delete SurveyObjectCache where SurveyTypeID = @ObjectID
				delete Survey where SurveyTypeID = @ObjectID
				delete SurveyType where ID = @ObjectID
			end

			if @Object = 'TaxonomyType'
			begin
				delete Taxonomy where TaxonomyTypeID = @ObjectID
				delete TaxonomyTypeLevel where TaxonomyTypeID = @ObjectID
				delete TaxonomyType where ID = @ObjectID
			end

		end
		else
		begin
			delete Attribute							where ObjectType = @Object and ObjectID = @ObjectID
			delete cache.Relationship					where [SourceObject] = @Object and SourceObjectID = @ObjectID
			delete cache.Relationship					where [TargetObject] = @Object and TargetObjectID = @ObjectID

			BEGIN TRY
				DECLARE @tblIntersectIDs table (ID int)

				INSERT INTO @tblIntersectIDs
					SELECT	IntersectID
					FROM	IntersectNode
					WHERE	ObjectType = @Object and ObjectID = @ObjectID

				delete [Intersect] where ID in (select ID from @tblIntersectIDs)
			END TRY
			BEGIN CATCH

			END CATCH

			if @Object = 'Artifact'
			begin
				delete	RelatedArtifact where ArtifactID = @ObjectID
			end

			if @Object = 'Domain'
			begin
				delete DomainItem where DomainID = @ObjectID
			end

			if @Object = 'Taxonomy'
			begin
				declare @th table (ID int);
				with th as	(
							select	ID, 
									ParentID
							from	Taxonomy
							where	ID = @ObjectID
							union all
							select	C.ID,
									C.ParentID
							from	Taxonomy C
									inner join th P on P.ID = C.ParentID
							)
				insert into @th 
					select ID from th
			
				delete Taxonomy where ID in (select ID from @th)
			end
		end
		
		commit transaction @trans
	end try
	begin catch
		 DECLARE @ErrorMessage NVARCHAR(4000);
    DECLARE @ErrorSeverity INT;
    DECLARE @ErrorState INT;

    SELECT 
        @ErrorMessage = ERROR_MESSAGE(),
        @ErrorSeverity = ERROR_SEVERITY(),
        @ErrorState = ERROR_STATE();

    -- Use RAISERROR inside the CATCH block to return error
    -- information about the original error that caused
    -- execution to jump to the CATCH block.
    RAISERROR (@ErrorMessage, -- Message text.
               @ErrorSeverity, -- Severity.
               @ErrorState -- State.
               );

		rollback transaction @trans
	end catch
end
GO

ALTER PROCEDURE [dbo].[GetAverageScoreByObjectType]
	@type varchar(50),
	@id int
AS
begin
	declare 
			@oName nvarchar(250),
			@oTypeName nvarchar(250),
			@oType varchar(50),
			@oID int,
			@AveragePoints int,
			@MaxPoints int,
			@AverageScore int,
			@ObjectScore varchar(250)--int

	select	@oName = Name,
			@oTypeName = ObjectTypeName,
			@oType = ObjectType,
			@oID = ObjectTypeID
	from	cache.ObjectDetails 
	where	[Object] = @type and ObjectID = @id

	select	@MaxPoints = SUM(Score)
	from	StatisticType
	where	[Object] = @oType
			and ObjectID = @oID
			and PartOfScore = 1

	select	@AveragePoints = AVG(S.Score)
	from	(
			select	S.ObjectType, S.ObjectID, SUM(S.Score) as Score
			from	(
					select		S.ObjectType, 
								S.ObjectID, 
								S.StatisticTypeID,
								MAX(S.DateEnd) as Date
					from		Statistic S
								inner join cache.[Object] O on O.[Object] = S.ObjectType and O.ObjectID = S.ObjectID and O.ObjectType = @oType
								inner join StatisticType T on S.StatisticTypeID = T.ID and T.[Object] = @oType and T.ObjectID = @oID and T.PartOfScore = 1
					group by	S.ObjectType, S.ObjectID, S.StatisticTypeID
					) FS
					inner join Statistic S on S.ObjectType = FS.ObjectType and S.ObjectID = FS.ObjectID and S.DateEnd = FS.Date and S.StatisticTypeID = FS.StatisticTypeID
			group by	S.ObjectType, S.ObjectID
			) S

	select @AverageScore = cast(round(round(cast(@AveragePoints as float) / cast(@MaxPoints as float), 2) * 100, 0) as int)	
	select @ObjectScore = dbo.GetObjectStatisticScore(@type, @id)*100

	select	@type as [Object], @id as ObjectID, @oName as ObjectName, @ObjectScore as ObjectScore, 
			@oType as ObjectType, @oID as ObjectTypeID, @oTypeName as ObjectTypeName, @AverageScore as AverageScore 
end
GO

ALTER PROCEDURE [dbo].[GetCommentCountByFollower]
--declare
	@resourceID int,
	@dateStart datetime = null,
	@dateEnd datetime = null,
	@searchPhrase varchar(100) = ''
--set @resourceID = 1
AS
BEGIN
	SELECT	i.CommentType, 
			u.[Count], 
			u.CommentTypeName 
	FROM	(
			select	count(1) as [All],
					sum(case when c.commenttypeid = 2 then 1 else 0 end) as [Discussions],
					sum(case when c.commenttypeid = 5 then 1 else 0 end) as Issues,
					sum(case when c.commenttypeid = 6 then 1 else 0 end) as Tasks,
					sum(case when c.commenttypeid = 7 then 1 else 0 end) as [Red Flags],
					sum(case when c.commenttypeid = 8 then 1 else 0 end) as [Data Events],
					sum(case when c.commenttypeid = 9 then 1 else 0 end) as [Challenges]
			from	Comment c
			where	c.ID in	(
					select	CommentID as ID
					from	FollowDetail f
							inner join CommentRelation cr on cr.ObjectID = f.ObjectID and cr.ObjectType = f.ObjectType
					where	f.ResourceID = @resourceId
					union all
					select	ID 
					from	Comment 
					where	CreatingResourceID = @resourceid
					union all
					select	ID 
					from	comment c2
							inner join	(
										select	r.[Object], r.ObjectID 
										from	ResourceGroup rg 
												inner join cache.ResponsibilityItem r on rg.GroupID = r.ResponsibleObjectID and r.ResponsibleObject = 'Group' and rg.ResourceID = @resourceID
										union
										select	[Object], ObjectID 
										from	cache.ResponsibilityItem
										where	ResponsibleObject = 'Resource' 
												and ResponsibleObjectID = @resourceID
										) o on o.[Object] = c2.OwnerObjectType and o.ObjectID = c2.OwnerObjectid
					)
			AND C.isdeleted = 0
			AND (
					(C.DateCreated between @dateStart and @dateEnd and @dateStart is not null and @dateEnd is not null) or
					(@dateStart is null and @dateEnd is null)
				)
			AND C.ParentID is null
			AND (
				coalesce(ltrim(rtrim(@searchPhrase)),'')='' or 
				lower(Body) like lower('%'+@searchPhrase+'%')
				)
			AND case 
					when c.CreatingResourceID = @resourceID then 1
					when c.VisibilityID = 2 then 1
					when c.VisibilityID = 3 then 1
					when coalesce(c.VisibilityID, 4) = 4  then 1
					else 0
				end = 1
		) t
		UNPIVOT
			(	[Count]
				for [CommentTypeName] in ([All], Discussions, Issues, Tasks, [Red Flags], [Data Events], [Challenges])
			) u
			inner join
			(
			select	* 
			from	(
					select	0 as [All],
							2 as Discussions,
							5 as Issues,
							6 as Tasks,
							7 as [Red Flags],
							8 as [Data Events],
							9 as [Challenges]
					)	t2
						unpivot
						(
						CommentType for CommentTypeName in ([All], Discussions, Issues, Tasks, [Red Flags], [Data Events], [Challenges])
						) u2
			) i on i.CommentTypeName = u.CommentTypeName
END
GO

ALTER PROCEDURE [dbo].[GetCommentCountByType]
	@type varchar(50), 
	@id int,
	@dateStart datetime = null,
	@dateEnd datetime = null,
	@searchPhrase varchar(100) = ''
AS
BEGIN
	SET NOCOUNT ON;

	WITH P
	AS
	(
		SELECT		C.*,
					coalesce(C.OwnerObjectType, CR.ObjectType) as ObjectType,
					coalesce(C.OwnerObjectID, CR.ObjectID) as ObjectID,
					(
					select	CRD.Object,
							CRD.ObjectID,
							CRD.TextPath,
							CRD.ObjectTypeName,
							CRD.Url
					from	CommentRelation CR
							inner join cache.ObjectDetails CRD on CR.CommentID = C.ID and CR.ObjectType = CRD.[Object] and CR.ObjectID = CRD.ObjectID
					for xml path('tag'), root('tags'), type
					) as TagsXml
		FROM		Comment C
					INNER JOIN CommentRelation CR	ON C.ID = CR.CommentID
													AND (
														1=1
														)
													AND CR.ObjectType = @type 
													AND CR.ObjectID = @id
													AND (
														(C.DateCreated between @dateStart and @dateEnd and @dateStart is not null and @dateEnd is not null) or
														(@dateStart is null and @dateEnd is null)
														)
													AND C.ParentID IS NULL				

		UNION ALL

		SELECT	C.*, 
				cast('Resource' as varchar(50)) as ObjectType,
				C.CreatingResourceID as ObjectID,
				NULL as TagsXml
		FROM	P
				INNER JOIN Comment C ON C.ParentID = P.ID
	)

	SELECT
		i.CommentType, 
		u.[Count], 
		u.CommentTypeName 
	FROM
	(
		SELECT		
					COUNT(*) as [All],
					SUM(CASE WHEN CommentTypeID = 2 THEN 1 ELSE 0 END) as [Discussions],
					SUM(CASE WHEN CommentTypeID = 5 THEN 1 ELSE 0 END) as Issues,
					SUM(CASE WHEN CommentTypeID = 6 THEN 1 ELSE 0 END) as Tasks,
					SUM(CASE WHEN CommentTypeID = 7 THEN 1 ELSE 0 END) as [Red Flags],
					SUM(CASE WHEN CommentTypeID = 8 THEN 1 ELSE 0 END) as [Data Events],
					SUM(CASE WHEN CommentTypeID = 9 THEN 1 ELSE 0 END) as [Challenges]

		from	P
				left join reporting.Global_Resource R on R.ResourceID = P.CreatingResourceID
				left join cache.ObjectDetails D on D.[Object] = P.ObjectType and D.ObjectID = P.ObjectID
		where
			P.isdeleted = 0
				) t
					UNPIVOT
				(
					[Count]
					for [CommentTypeName] in ([All], Discussions, Issues, Tasks, [Red Flags], [Data Events],[Challenges])
				) u
	join
	(
		select * from 
		(
			select 
				0 as [All],
				2 as Discussions,
				5 as Issues,
				6 as Tasks,
				7 as [Red Flags],
				8 as [Data Events],
				9 as [Challenges]
				) t2
			unpivot
				(
					CommentType
					for CommentTypeName in ([All], Discussions, Issues, Tasks, [Red Flags], [Data Events],[Challenges])
				) u2
		) i on i.CommentTypeName = u.CommentTypeName
		
END
GO

ALTER PROCEDURE [dbo].[GetScoreHistoryByObject]
	@type varchar(50),
	@id int
AS
begin
	declare @Points int = 14,
			@DateStart datetime, @DateEnd datetime, @DateCurrent datetime,
			@Increment int, @CurrentPoints int, @MaxPoints int, @current int,
			@oType varchar(25), @oTypeID int, @score float

	declare @dates table ([Date] datetime, Score float)

	set @DateEnd = DATEADD(dd, 0, DATEDIFF(dd, 0, GETUTCDATE()))
	select	@DateStart = coalesce(min(Date), DATEADD(d, -30, @DateEnd)) 
	from	ObjectVersion 
	where	ObjectType = @type 
			and ObjectID = @id
	
	select @Increment = DATEDIFF(hh, @DateStart, @DateEnd) / @Points
	insert into @dates values (@DateEnd, dbo.GetObjectStatisticScore(@type, @id)*100)

	select	@oType = Type,
			@oTypeID = TypeID
	from	utility.ObjectDetail(@type, @id)

	select	@MaxPoints = SUM(Score)
	from	StatisticType
	where	[Object] = @oType
			and ObjectID = @oTypeID
			and PartOfScore = 1

	set @current = 1
	while @current <= @Points
	begin
		set @DateCurrent = DATEADD(hh, -(@current * @Increment), @DateEnd)


		select	@CurrentPoints = SUM(S.Score)
		from	Statistic S
				inner join StatisticType T on S.StatisticTypeID = T.ID and T.PartOfScore = 1
				inner join	(
							select		StatisticTypeID,
										Max(DateStart) D
							from		Statistic S
										inner join StatisticType T on S.StatisticTypeID = T.ID and T.PartOfScore = 1
							where		S.ObjectType = @type
										and S.ObjectID = @id
										and @DateCurrent between S.DateStart and S.DateEnd
							group by	StatisticTypeID
							) M on M.StatisticTypeID = S.StatisticTypeID and M.D = S.DateStart
		where	S.ObjectType = @type
				and S.ObjectID = @id

		select	@score = round(cast(@CurrentPoints as float) / cast(@MaxPoints as float), 2)
		insert into @dates values (@DateCurrent, @score*100)
		set @current = @current + 1
	end
	select * from  @dates order by [Date]
end
GO

alter procedure [utility].[GetOwnersForWorkflow]
--declare 
	@workflowID uniqueidentifier
--set @workflowID = '387A8094-565E-45AF-B049-01329EEF2209' --=> wt 1
--set @workflowID = '0C573C9B-D237-4468-8822-7D515750675B'--'CEE2AF0D-DAB8-432B-AF08-00E52B808C52' --=> wt 2
--set @workflowID = 'FD3C4A3D-C9BB-477A-B5CD-BC99C62AF53F' --=> wt 3
as
begin
	declare @workflowType int,
			@fields xml
	declare @tbl table (ID int, FirstName nvarchar(250), LastName nvarchar(250), Email nvarchar(500), Username nvarchar(500), DateLastLoggedIn datetime null, ResourceTypeID int, Status nvarchar(25))

	select	@workflowType = WorkflowType,
			@fields = Data
	from	Workflow
	where	ID = @workflowID

	if @workflowType = 1
	begin
		--1. Check for vocabulary owners
		insert into @tbl
			select	R.ResourceID, R.FirstName, R.LastName, R.Email, R.Email, R.DateLastLoggedIn, 1 as ResourceTypeID, R.Status 
			from	ResponsibilityDetail RD 
					inner join WorkflowTypeRelation WTR on WTR.Parent = 'TaxonomyType' and WTR.ParentID = @fields.value('(/fields/TaxonomyTypeID)[1]', 'int') and WTR.WorkflowType = @workflowType and WTR.ResponsibilityTypeID = RD.ResponsibilityTypeID
					inner join reporting.Global_Resource R 
						on RD.ObjectType = 'TaxonomyType' 
						and RD.ObjectID = @fields.value('(/fields/TaxonomyTypeID)[1]', 'int')
						and	(
								(RD.ResponsibleObjectType = 'Group' and R.ResourceID = RD.PrimaryOwnerResourceID) or 
								(RD.ResponsibleObjectType = 'Resource' and R.ResourceID = RD.ResponsibleObjectID)
							)
						and R.Email not like '%?subject=%' and R.Status = 'Active'

		if not exists(select * from @tbl)
		begin
			insert into @tbl
				select	R.ResourceID, R.FirstName, R.LastName, R.Email, R.Email, R.DateLastLoggedIn, 1 as ResourceTypeID, R.Status 
				from	ResponsibilityDetail RD 
						inner join WorkflowTypeRelation WTR on WTR.[Object] = 'ArtifactType' and WTR.ObjectID = @fields.value('(/fields/ArtifactTypeID)[1]', 'int') and WTR.Parent is null and WTR.WorkflowType = @workflowType and WTR.ResponsibilityTypeID = RD.ResponsibilityTypeID
						inner join reporting.Global_Resource R 
							on RD.ObjectType = 'ArtifactType' 
							and RD.ObjectID = @fields.value('(/fields/ArtifactTypeID)[1]', 'int')
							and (
									(RD.ResponsibleObjectType = 'Group' and R.ResourceID = RD.PrimaryOwnerResourceID) or 
									(RD.ResponsibleObjectType = 'Resource' and R.ResourceID = RD.ResponsibleObjectID)
								)
							and R.Email not like '%?subject=%' and R.Status = 'Active'
		end
	end

	if @workflowType = 2
	begin
		insert into @tbl
			select	R.ResourceID, R.FirstName, R.LastName, R.Email, R.Email, R.DateLastLoggedIn, 1 as ResourceTypeID, R.Status 
			from	ResponsibilityDetail RD 
					inner join Artifact A on RD.ObjectType = 'Artifact' and RD.ObjectID = A.ID and A.ID = @fields.value('(/fields/ArtifactID)[1]', 'int')
					inner join WorkflowTypeRelation WTR		on WTR.[Object] = 'ArtifactType' and WTR.ObjectID = A.ArtifactTypeID 
															and WTR.Parent = 'TaxonomyType' and WTR.ParentID = A.TaxonomyTypeID
															and WTR.WorkflowType = @workflowType 
															and WTR.ResponsibilityTypeID = RD.ResponsibilityTypeID
					inner join reporting.Global_Resource R 
						on	(
								(RD.ResponsibleObjectType = 'Group' and R.ResourceID = RD.PrimaryOwnerResourceID) or 
								(RD.ResponsibleObjectType = 'Resource' and R.ResourceID = RD.ResponsibleObjectID)
							)
						and R.Email not like '%?subject=%' and R.Status = 'Active' 

		if not exists(select * from @tbl)
		begin
			insert into @tbl
				select	R.ResourceID, R.FirstName, R.LastName, R.Email, R.Email, R.DateLastLoggedIn, 1 as ResourceTypeID, R.Status 
				from	ResponsibilityDetail RD 
						inner join Artifact A on RD.ObjectType = 'Artifact' and RD.ObjectID = A.ID and A.ID = @fields.value('(/fields/ArtifactID)[1]', 'int')
						inner join WorkflowTypeRelation WTR		on WTR.[Object] = 'ArtifactType' and WTR.ObjectID = A.ArtifactTypeID 
																and WTR.WorkflowType = @workflowType 
																and WTR.ResponsibilityTypeID = RD.ResponsibilityTypeID
						inner join reporting.Global_Resource R 
							on	(
									(RD.ResponsibleObjectType = 'Group' and R.ResourceID = RD.PrimaryOwnerResourceID) or 
									(RD.ResponsibleObjectType = 'Resource' and R.ResourceID = RD.ResponsibleObjectID)
								)
							and R.Email not like '%?subject=%' and R.Status = 'Active' 
		end
	end

	if @workflowType = 3
	begin
		insert into @tbl
			select	distinct
					R.ResourceID, R.FirstName, R.LastName, R.Email, R.Email, R.DateLastLoggedIn, 1 as ResourceTypeID, R.Status 
			from	Comment C
					inner join CommentRelation CR on CR.CommentID = C.ID and C.ID = @fields.value('(fields/CommentID)[1]', 'int') and CR.ObjectType not in ('Resource', 'Group')
					inner join ResponsibilityDetail RD on RD.ObjectType = CR.ObjectType and RD.ObjectID = CR.ObjectID 
					inner join reporting.Global_Resource R 
						on	(
								(RD.ResponsibleObjectType = 'Group' and R.ResourceID = RD.PrimaryOwnerResourceID) or 
								(RD.ResponsibleObjectType = 'Resource' and R.ResourceID = RD.ResponsibleObjectID)
							) 
							and R.Email not like '%?subject=%' and R.Status = 'Active'

		if not exists (select 1 from @tbl)
		begin
			insert into @tbl
				select 
					R.ResourceID, R.FirstName, R.LastName, R.Email, R.Email, R.DateLastLoggedIn, 1 as ResourceTypeID, R.Status 
				from 
					reporting.Global_Resource R where isadministrator = 1
		end
	end

	if @workflowType = 4
	begin
		insert into @tbl
				select	R.ResourceID, R.FirstName, R.LastName, R.Email, R.Email, R.DateLastLoggedIn, 1 as ResourceTypeID, R.Status 
				from	ResponsibilityDetail RD 
						inner join Artifact A on RD.ObjectType = 'Artifact' and RD.ObjectID = A.ID and A.ID = @fields.value('(/fields/ArtifactID)[1]', 'int')
						inner join WorkflowTypeRelation WTR		on WTR.[Object] = 'ArtifactType' and WTR.ObjectID = A.ArtifactTypeID 
																and WTR.WorkflowType = @workflowType 
																and WTR.ResponsibilityTypeID = RD.ResponsibilityTypeID
																and WTR.[Enabled] = 1
						inner join reporting.Global_Resource R 
							on	(
									(RD.ResponsibleObjectType = 'Group' and R.ResourceID = RD.PrimaryOwnerResourceID) or 
									(RD.ResponsibleObjectType = 'Resource' and R.ResourceID = RD.ResponsibleObjectID)
								)
							and R.Email not like '%?subject=%' and R.Status = 'Active' 

		if not exists (select 1 from @tbl)
		begin
			insert into @tbl
				select	distinct
						R.ResourceID, R.FirstName, R.LastName, R.Email, R.Email, R.DateLastLoggedIn, 1 as ResourceTypeID, R.Status 
				from	Comment C
						inner join CommentRelation CR on CR.CommentID = C.ID and C.ID = @fields.value('(fields/CommentID)[1]', 'int') and CR.ObjectType not in ('Resource', 'Group')
						inner join ResponsibilityDetail RD on RD.ObjectType = CR.ObjectType and RD.ObjectID = CR.ObjectID 
						inner join reporting.Global_Resource R 
							on	(
									(RD.ResponsibleObjectType = 'Group' and R.ResourceID = RD.PrimaryOwnerResourceID) or 
									(RD.ResponsibleObjectType = 'Resource' and R.ResourceID = RD.ResponsibleObjectID)
								) 
								and R.Email not like '%?subject=%' and R.Status = 'Active'
		end

		if not exists (select 1 from @tbl)
		begin
			insert into @tbl
				select 
					R.ResourceID, R.FirstName, R.LastName, R.Email, R.Email, R.DateLastLoggedIn, 1 as ResourceTypeID, R.Status 
				from 
					reporting.Global_Resource R where isadministrator = 1
		end
	end

	select * from @tbl
end
GO

ALTER FUNCTION [dbo].[GetObjectStatisticScore]
(
--declare
	@type varchar(25) = 'Resource',
	@id int = 1
)
RETURNS float
AS
BEGIN
	declare @current int,
			@max int,
			@oType varchar(25),
			@oTypeID int,
			@score float

	select	@oType = ObjectType,
			@oTypeID = ObjectTypeID
	from	cache.[Object]
	where	[Object] = @type and ObjectID = @id

	select	@current = SUM(S.Score)
	from	Statistic S
			inner join StatisticType T on S.StatisticTypeID = T.ID and T.PartOfScore = 1 and T.[Object] = @oType and T.ObjectID = @oTypeID
			inner join	(
						select		StatisticTypeID,
									Max(DateStart) D
						from		Statistic S
									inner join StatisticType T on S.StatisticTypeID = T.ID and T.PartOfScore = 1
						where		S.ObjectType = @type
									and S.ObjectID = @id
						group by	StatisticTypeID
						) M on M.StatisticTypeID = S.StatisticTypeID and M.D = S.DateStart
	where	S.ObjectType = @type
			and S.ObjectID = @id

	select	@max = SUM(Score)
	from	StatisticType
	where	[Object] = @oType
			and ObjectID = @oTypeID
			and PartOfScore = 1

	select	@score = round(cast(cast(@current as float) / cast(@max as float) as float), 2)

	return @score
END
GO

ALTER procedure [utility].[CalculateStatistics]
--declare
	@Type varchar(50) = NULL,
	@ID int = NULL,
	@TargetStatisticTypeID int = NULL
as
begin
	SET NOCOUNT ON;

	declare @current int, @max int
	declare @relations table (ID int identity, [Object] varchar(50), ObjectID int)

	IF OBJECT_ID('tempdb..#StatisticTypes') IS NOT NULL
	BEGIN
		DROP TABLE #StatisticTypes
	END
	create table #StatisticTypes (ID int identity, StatisticTypeID int)

	insert into #StatisticTypes
		select ID from StatisticType where (@TargetStatisticTypeID is not null and ID = @TargetStatisticTypeID) OR @TargetStatisticTypeID is null order by ID

	set		@current	= 1
	select	@max		= MAX(ID) from #StatisticTypes

	IF OBJECT_ID('tempdb..#Statistics') IS NOT NULL
	BEGIN
		DROP TABLE #Statistics
	END
	create table #Statistics (StatisticTypeID int, ObjectType varchar(50), ObjectID int, Score int)

--select * from StatisticType

	while @current <= @max
	begin
		declare @StatisticTypeID int,
				@CheckType int,
				@CheckObjectType varchar(25),
				@CheckObjectID int,
				@Object varchar(25),
				@ObjectID int,
				@Score int,
				@PropertyName varchar(250),
				@Value nvarchar(4000),
				@PredicateID int,
				@Configuration xml

		select	@StatisticTypeID = S.ID,
				@CheckType = S.CheckType,
				@Configuration = S.Configuration,
				@Object = [Object],
				@ObjectID = ObjectID,
				@Score = Score 
		from	#StatisticTypes T
				inner join StatisticType S on S.ID = T.StatisticTypeID
		where	T.ID = @current

		delete @relations

		insert into @relations
			select	[Object],
					ObjectID
			from	cache.[Object]
			where	ObjectType = @Object
					and ObjectTypeID = @ObjectID
					and (
						(@Type is not null and [Object] = @Type and ObjectID = @ID) OR (@Type is null) 
						)
						
		-- EXISTENCE
		if (@CheckType = 1)
		begin
			select	@CheckObjectType = f.value('(ObjectType/text())[1]', 'varchar(25)'),
					@CheckObjectID = f.value('(ObjectID/text())[1]', 'int')
			from	@Configuration.nodes('/fields') as F(f)

			if @CheckObjectType = 'AttributeType'
			begin
				insert into #Statistics
					select	@StatisticTypeID as StatisticTypeID,
							R.[Object],
							R.ObjectID,
							case 
								when O.ValueExists <> 0 then @Score
								else 0
							end as Score
					from	@relations R
							outer apply (
										select		ISNULL(AttributeTypeID, 0) as ValueExists
										from		Attribute 
										where		ObjectType = R.[Object] and ObjectID = R.ObjectID and AttributeTypeID = @CheckObjectID
										group by	AttributeTypeID, ObjectType, ObjectID
										) O
			end

			if @CheckObjectType = 'ResponsibilityType'
			begin
				insert into #Statistics
					select	@StatisticTypeID as StatisticTypeID,
							R.[Object],
							R.ObjectID,
							case 
								when P.ValueExists <> 0 then @Score
								else 0
							end as Score
					from	@relations R
							outer apply (
										select		ISNULL(ResponsibilityTypeID, 0) as ValueExists
										from		[cache].[ResponsibilityItem]
										where		[Object] = R.[Object] and ObjectID = R.ObjectID and ResponsibilityTypeID = @CheckObjectID
										group by	ResponsibilityTypeID, [Object], ObjectID
										) P
			end
		end

		-- COUNT (instead of score)
		if (@CheckType = 2)	--COUNT
		begin
			select	@CheckObjectType = f.value('(ObjectType/text())[1]', 'varchar(25)'),
					@CheckObjectID = f.value('(ObjectID/text())[1]', 'int')
			from	@Configuration.nodes('/fields') as F(f)

			if @CheckObjectType = 'AttributeType'
			begin
				insert into #Statistics
					select	@StatisticTypeID as StatisticTypeID,
							R.[Object],
							R.ObjectID,
							COALESCE(O.Score, 0) as Score
					from	@relations R
							outer apply (
										select		COUNT(1) as Score
										from		Attribute 
										where		ObjectType = R.[Object] and ObjectID = R.ObjectID and AttributeTypeID = @CheckObjectID
										group by	AttributeTypeID, ObjectType, ObjectID
										) O
			end

			if @CheckObjectType = 'ResponsibilityType'
			begin
				insert into #Statistics
					select	@StatisticTypeID as StatisticTypeID,
							R.[Object],
							R.ObjectID,
							COALESCE(O.Score, 0) as Score
					from	@relations R
							outer apply (
										select		COUNT(1) as Score
										from		[cache].[ResponsibilityItem]
										where		[Object] = R.[Object] and ObjectID = R.ObjectID and ResponsibilityTypeID = @CheckObjectID
										group by	ResponsibilityTypeID, [Object], ObjectID
										) O
			end

			-- This does a count on relationships
			if @CheckObjectType <> 'AttributeType' and @CheckObjectType <> 'ResponsibilityType'
			begin
				insert into #Statistics
					select	@StatisticTypeID as StatisticTypeID,
							R.[Object],
							R.ObjectID,
							COALESCE(O.Score, 0) as Score
					from	@relations R
							outer apply (
										select		COUNT(1) as Score
										from		[cache].[Relationship] IR
													inner join cache.[Object] ID on ID.[Object] = IR.TargetObject and ID.ObjectID = IR.TargetObjectID 
																				and ID.ObjectType = @CheckObjectType and ID.ObjectTypeID = @CheckObjectID 
																				and IR.SourceObject = R.[Object] and IR.SourceObjectID = R.ObjectID
										group by	ID.ObjectType, ID.ObjectTypeID
										) O
			end
		end

		-- PROPERTY VALUE CHECK
		if (@CheckType = 3)
		begin
			select	@PropertyName = f.value('(PropertyName/text())[1]', 'varchar(250)'),
					@Value = f.value('(PropertyValue/text())[1]', 'nvarchar(4000)')
			from	@Configuration.nodes('/fields') as F(f)

			if @Object = 'ArtifactType' and @PropertyName = 'Status'
				begin
					insert into #Statistics
						select	@StatisticTypeID as StatisticTypeID,
								R.[Object],
								R.ObjectID,
								case 
									when O.ValueExists <> 0 then @Score
									else 0
								end as Score
						from	@relations R
								outer apply (
											select		CASE 
															when [Status] = @Value then 1
															else 0
														END as ValueExists
											from		Artifact
											where		R.[Object] = 'Artifact' and ID = R.ObjectID
											) O
				end
			else
				begin
					-- A dynamic field to check.
					insert into #Statistics
						select	@StatisticTypeID as StatisticTypeID,
								R.[Object],
								R.ObjectID,
								case 
									when O.ValueExists <> 0 then @Score
									else 0
								end as Score
						from	@relations R
								outer apply (
											select	CASE 
														when F.FormattedValue = @Value then 1
														else 0
													END as ValueExists									
											from	Field F
													inner join FieldType FT on FT.[Object] = @Object and FT.ObjectID = @ObjectID 
																			and F.[ObjectType] = R.[Object] and F.ObjectID = R.ObjectID
																			and FT.Name = @PropertyName 
											) O
				end
		end

		-- PROPERTY POPULATED
		if (@CheckType = 4)
		begin
			select	@PropertyName = f.value('(PropertyName/text())[1]', 'varchar(250)')
			from	@Configuration.nodes('/fields') as F(f)

			if @PropertyName = 'Description'
				begin
					insert into #Statistics
						select	@StatisticTypeID as StatisticTypeID,
								R.[Object],
								R.ObjectID,
								case 
									when D.Description is null then 0
									when LEN(D.Description) < 25 then 0
									else @Score
								end as Score
						from	@relations R
								left join cache.ObjectDetails D on D.[Object] = R.[Object] and D.ObjectID = R.ObjectID
				end
			else
				begin
					-- A dynamic field to check.
					insert into #Statistics
						select	@StatisticTypeID as StatisticTypeID,
								R.[Object],
								R.ObjectID,
								case 
									when O.ValueExists <> 0 then @Score
									else 0
								end as Score
						from	@relations R
								outer apply (
											select	case
														when F.FormattedValue is not null then 1
														else 0
													END as ValueExists
											from	Field F
													inner join FieldType FT on FT.[Object] = @Object and FT.ObjectID = @ObjectID 
																			and F.[ObjectType] = R.[Object] and F.ObjectID = R.ObjectID
																			and FT.Name = @PropertyName 
											) O
				end
		end

		-- RELATIONSHIP
		if (@CheckType = 5)
		begin
			declare @checkRelationshipObjects table (Object varchar(50), ObjectID int)

			-- first, check legacy format
			select	@CheckObjectType = f.value('(ObjectType/text())[1]', 'varchar(25)'),
					@CheckObjectID = f.value('(ObjectID/text())[1]', 'int')
			from	@Configuration.nodes('/fields') as F(f)

			if @CheckObjectType is not null and @CheckObjectID is not null
				begin
					insert into @checkRelationshipObjects values (@CheckObjectType, @CheckObjectID)
				end
			else
				begin
					--check new format of multiple options
					insert into @checkRelationshipObjects
						select	f.value('(Object/Type/text())[1]', 'varchar(50)'),
								f.value('(Object/ID/text())[1]', 'int')
						from	@Configuration.nodes('/fields/CheckObjects') as F(f)
				end


			insert into #Statistics
				select	@StatisticTypeID as StatisticTypeID,
						R.[Object],
						R.ObjectID,
						case 
							when O.[Count] > 0 then @Score
							else 0
						end as Score
				from	@relations R
						outer apply (
									select		COUNT(1) as [Count]
									from		[cache].[Relationship] IR
												inner join cache.[Object] D on D.[Object] = IR.TargetObject and D.ObjectID = IR.TargetObjectID 
																			and IR.SourceObject = R.[Object] and IR.SourceObjectID = R.ObjectID
												inner join @checkRelationshipObjects TT on TT.[Object] = D.ObjectType and TT.ObjectID = D.ObjectTypeID
									) O

		end

		-- FUSION OWNERSHIP
		if (@CheckType = 6)
		begin
			--select	@CheckObjectType = f.value('(ObjectType/text())[1]', 'varchar(25)'),
			--		@CheckObjectID = f.value('(ObjectID/text())[1]', 'int')
			--from	@Configuration.nodes('/fields') as F(f)

			insert into #Statistics
				select	@StatisticTypeID as StatisticTypeID,
						R.[Object],
						R.ObjectID,
						case 
							when O.ValueExists <> 0 then @Score
							else 0
						end as Score
				from	@relations R
						outer apply (
									select		ISNULL(RelationshipOwnerObjectID, 0) as ValueExists
									from		FusionAttributeOwnerRule
									where		RelationshipOwnerObjectType = R.[Object] and RelationshipOwnerObjectID = R.ObjectID
									group by	RelationshipOwnerObjectType, RelationshipOwnerObjectID
									) O
		end

		-- ROLLUP VIA RELATIONSHIPS
		if (@CheckType = 7)
		begin
			select	@CheckObjectType = f.value('(ObjectType/text())[1]', 'varchar(25)'),
					@CheckObjectID = f.value('(ObjectID/text())[1]', 'int')
			from	@Configuration.nodes('/fields') as F(f)

			insert into #Statistics
				select	@StatisticTypeID as StatisticTypeID,
						R.[Object],
						R.ObjectID,
						round((T.Total/C.[Count]) * @Score, 0) Score
				from	@relations R
						cross apply (
									select	count(1) as [Count] 
									from	cache.Relationships
									where	SourceObject = R.[Object] and SourceObjectID = R.ObjectID
											and TargetType = @CheckObjectType and TargetTypeID = @CheckObjectID
									) C
						outer apply (
									select	sum(dbo.GetObjectStatisticScore(TargetObject, TargetObjectID)) as Total
									from	cache.Relationships 
									where	SourceObject = R.[Object] and SourceObjectID = R.ObjectID 
											and TargetType = @CheckObjectType and TargetTypeID = @CheckObjectID
									) T
				where C.[Count] > 0
		end

		-- ROLLUP VIA OWNERSHIP
		if (@CheckType = 8)
		begin
			select	@CheckObjectType = f.value('(ObjectType/text())[1]', 'varchar(25)'),
					@CheckObjectID = f.value('(ObjectID/text())[1]', 'int')
			from	@Configuration.nodes('/fields') as F(f)

			insert into #Statistics
				select	@StatisticTypeID as StatisticTypeID,
						R.[Object],
						R.ObjectID,
						round((T.Total/C.[Count]) * @Score, 0) Score
				from	@relations R
						cross apply (
									select	count(1) as [Count] 
									from	cache.Responsibilities
									where	ResponsibleObject = R.[Object] and ResponsibleObjectID = R.ObjectID
											and ObjectType = @CheckObjectType and ObjectTypeID = @CheckObjectID
									) C
						outer apply (
									select	sum(dbo.GetObjectStatisticScore([Object], ObjectID)) as Total
									from	cache.Responsibilities 
									where	ResponsibleObject = R.[Object] and ResponsibleObjectID = R.ObjectID 
											and ObjectType = @CheckObjectType and ObjectTypeID = @CheckObjectID
									) T
				where C.[Count] > 0
		end
		

		-- EVENT METRIC CHECK
		if (@CheckType = 9)
		begin
			declare @ValidField nvarchar(250),-- = 'ValidCount',
					@InvalidField nvarchar(250),-- = 'InvalidCount',
					@Threshold decimal(9,2),-- = 0.10,
					@TotalValid float,
					@TotalInvalid float

			select	@ValidField = f.value('(ValidField/text())[1]', 'nvarchar(250)'),
					@InvalidField = f.value('(InvalidField/text())[1]', 'nvarchar(250)'),
					@Threshold = f.value('(Threshold/text())[1]', 'decimal(9,2)')
			from	@Configuration.nodes('/fields') as F(f)


			select	@TotalValid = sum(cast(V.ValidCount as int)),
					@TotalInvalid = sum(cast(I.InvalidCount as int))
			from	cache.Relationships REL
					inner join [Rule] R on R.ID = REL.TargetObjectID and REL.TargetObject = 'Rule' and R.RuleType in (3,4)
					inner join EventGroup EG on EG.RuleID = R.ID
					inner join [Event] E on E.EventGroupID = EG.ID 
					inner join (
								select	R.ID,
										max(E.Date) as [Date]
								from	cache.Relationships REL
										inner join [Rule] R on R.ID = REL.TargetObjectID and REL.TargetObject = 'Rule' and R.RuleType in (3,4)
										inner join EventGroup EG on EG.RuleID = R.ID
										inner join [Event] E on E.EventGroupID = EG.ID
								group by R.ID					
								) F on F.ID = R.ID and F.[Date] = E.[Date]
					cross apply (
								select	Value as ValidCount
								from	FieldWithRelation
								where	ObjectType = 'Event' and ObjectID = E.ID and Name = @ValidField
								) V
					cross apply (
								select	Value as InvalidCount
								from	FieldWithRelation
								where	ObjectType = 'Event' and ObjectID = E.ID and Name = @InvalidField
								) I

			insert into #Statistics
				select	@StatisticTypeID as StatisticTypeID,
						R.[Object],
						R.ObjectID,
						case 
							when cast(@TotalInvalid / @TotalValid as decimal(9,2)) < @Threshold then @Score
							else 0
						end as Score
				from	@relations R
		end

		-- PREDICATE CHECK
		if (@CheckType = 10)
		begin
			select	@PredicateID = f.value('(Predicate/text())[1]', 'int')
			from	@Configuration.nodes('/fields') as F(f)

			insert into #Statistics
				select	@StatisticTypeID as StatisticTypeID,
						R.[Object],
						R.ObjectID,
						case 
							when O.[Count] > 0 then @Score
							else 0
						end as Score
				from	@relations R
						outer apply (
									select	count(1) as [Count]
									from	IntersectMap M
											inner join cache.Relationship IR on IR.[SourceIntersectNodeID] = M.SubjectIntersectNodeID 
																			and IR.[TargetIntersectNodeID] = M.ObjectIntersectNodeID 
																			and M.PredicateID = @PredicateID
											inner join cache.Relationship T1 on T1.SourceObject = R.[Object] 
																			and T1.SourceObjectID = R.ObjectID
																			and T1.TargetObject = IR.SourceObject 
																			and T1.TargetObjectID = IR.SourceObjectID
											inner join cache.Relationship T2 on T2.SourceObject = R.[Object] 
																			and T2.SourceObjectID = R.ObjectID
																			and T2.TargetObject = IR.TargetObject 
																			and T2.TargetObjectID = IR.TargetObjectID
									) O
		end

		set @current = @current + 1
	end

	-- now merge the Statistics table
	MERGE	Statistic AS T
	USING	(
			select	S.*,
					MS.DateStart
			from	#Statistics S
					outer apply (
								select		StatisticTypeID,
											ObjectType,
											ObjectID,
											MAX(DateStart) as DateStart
								from		Statistic
								where		StatisticTypeID = S.StatisticTypeID
											and ObjectType = S.ObjectType
											and ObjectID = S.ObjectID
								group by	StatisticTypeID,
											ObjectType,
											ObjectID
								) MS
			) AS S
	ON		(
			T.StatisticTypeID = S.StatisticTypeID
			and T.ObjectType = S.ObjectType
			and T.ObjectID = S.ObjectID
			and T.DateStart = S.DateStart
			and T.Score = S.Score
			)
		WHEN MATCHED THEN 
			UPDATE SET T.DateEnd = getutcdate()
		WHEN NOT MATCHED THEN	
			INSERT	
			VALUES	(
					S.StatisticTypeID, 
					S.ObjectType, 
					S.ObjectID,
					getutcdate(), 
					getutcdate(), 
					S.Score
					);
end
go


alter procedure [dbo].[ProcessSchedule]
as
begin
	set nocount on;

	declare @FusionIDs table (ID int identity, FusionID int)
	insert into @FusionIDs
		select ID from Fusion WHERE Enabled = 1 and Manual = 0

	declare @current int,
			@max int,
			@FusionID int,
			@DateStarted datetime,
			@DateCompleted datetime,
			@LastRunComplete bit,
			@IntervalType int,
			@Interval int,
			@MinDateJobMustStartNext datetime,
			@ShouldTriggerJob bit

	select	@current = 1,
			@max = MAX(ID)
	from	@FusionIDs

	delete FusionStatusLog where Success = 0 and DateStarted < DATEADD(hh, -6, getutcdate()) and MachineQueuedOn is not null

	while	@current <= @max
	begin
		select	@FusionID = F.ID,
				@IntervalType = F.IntervalType,
				@DateStarted = S.DateStarted,
				@DateCompleted = C.DateCompleted,
				@Interval = F.Interval
		from	Fusion F
				inner join @FusionIDs I on I.FusionID = F.ID and I.ID = @current
				outer apply (
							select	MAX(DateStarted) as DateStarted
							from	FusionStatusLog 
							where	FusionID = F.ID
							) S
				outer apply (
							select	DateCompleted
							from	FusionStatusLog 
							where	FusionID = F.ID
									and DateStarted = S.DateStarted
							) C
			set @LastRunComplete = case 
									when @DateStarted is not null and @DateCompleted is not null then 1
									else 0
								   end
	
		if (@DateStarted is null or @LastRunComplete = 1)
		begin
			if @DateCompleted is not null
			begin
				-- Get the next date when the job should run, based on the previous completed date, plus the interval.
				set @MinDateJobMustStartNext = case @IntervalType
													when 4 then DATEADD(s, @Interval, @DateCompleted)		-- SECOND
													when 3 then DATEADD(n, @Interval, @DateCompleted)		-- MINUTE
													when 2 then DATEADD(hh, @Interval, @DateCompleted)		-- HOUR
													else DATEADD(d, @Interval, @DateCompleted)				-- DAY = 1
												end
				set @ShouldTriggerJob = case 
											when DATEDIFF(s, @MinDateJobMustStartNext, getutcdate()) > 0 then 1
											else 0
										end
			end
		
			if @DateStarted is null
			begin
				-- Job has never been triggered, so force an execution immediately.
				set @ShouldTriggerJob = 1
			end
			
			if @ShouldTriggerJob = 1
			begin
				select	@FusionID, @IntervalType, @DateStarted, @DateCompleted, @Interval, @LastRunComplete
				insert into		FusionStatusLog
								(ID,		FusionID,		DateStarted,	Success)
				values			(newid(),	@FusionID,		getutcdate(),	0)
			end
		end

		set @current = @current + 1
	end
end
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
														)
						inner join ResponsibilityTypeRelation RTR on RTR.ResponsibilityTypeID = R.ResponsibilityTypeID and RTR.ObjectType = 'ArtifactType' and RTR.ObjectID = A.ArtifactTypeID;
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
go

ALTER FUNCTION [utility].[GetHierarchyAssignedResponsibilityList]
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
	declare @tblModelHierarchy table (
		Visible bit,
		ResponsibilityID int,
		ResponsibilityTypeID int,
		AssigningItem varchar(50),
		AssigningItemID int,
		[Object] varchar(50),
		ObjectID int,
		ContextHash varchar(50),
		[Level] int
	);

	if @Object = 'Artifact'
		begin
			with ModelRelationHierarchy as
			(
			select	R.Visible,
					'Taxonomy' as AssigningItemType, 
					T.ID as AssigningItemID,
					T.ID,
					T.ParentID,
					T.TaxonomyTypeID,
					R.ID as ResponsibilityID,
					R.ResponsibilityTypeID,
					utility.GetResponsibilityContextHash(R.ID) as ContextHash,
					1 as [Level]
			from	Taxonomy T 
					left join Responsibility R on R.ObjectType = 'Taxonomy' and R.ObjectID = T.ID 
			union all
			select	
					COALESCE(R.Visible, P.Visible) as Visible,
					P.AssigningItemType,
					COALESCE(R.ObjectID, P.AssigningItemID) as AssigningItemID,
					C.ID,
					C.ParentID,
					C.TaxonomyTypeID,
					COALESCE(R.ID, P.ResponsibilityID) as ResponsibilityID,
					COALESCE(R.ResponsibilityTypeID, P.ResponsibilityTypeID) as ResponsibilityTypeID,
					coalesce(R.ContextHash, P.ContextHash) as ContextHash,
					P.[Level] + 1 as [Level]
			from	Taxonomy C
					inner join ModelRelationHierarchy P on P.TaxonomyTypeID = C.TaxonomyTypeID and C.ParentID = P.ID
					outer apply (
								select	*,
										utility.GetResponsibilityContextHash(ID) as ContextHash
								from	Responsibility 
								where	ResponsibilityTypeID = P.ResponsibilityTypeID
										and ObjectType = 'Taxonomy' 
										and ObjectID = C.ID
								) R
			)

			insert into @tblModelHierarchy
				select		P.Visible,
							P.ResponsibilityID,
							P.ResponsibilityTypeID,
							P.AssigningItemType,
							P.AssigningItemID,
							R.TargetObject, 
							R.TargetObjectID,
							P.ContextHash,
							P.[Level]
				from		ModelRelationHierarchy P
							inner join cache.Relationship R on 
								R.SourceObject = 'Taxonomy' and R.SourceObjectID = P.ID 
								and R.TargetObject = 'Artifact'
							inner join Artifact A on A.ID = R.TargetObjectID and A.TaxonomyTypeID = P.TaxonomyTypeID
								and (
									(A.ID = @ObjectID and @ObjectID is not null) or (@ObjectID is null)
									)
							inner join ResponsibilityTypeRelation RTR on RTR.ResponsibilityTypeID = P.ResponsibilityTypeID and RTR.ObjectType = 'ArtifactType' and RTR.ObjectID = A.ArtifactTypeID
				where		P.ResponsibilityID is not null;


			insert into @tbl
				select	'Hierarchy Assigned' as [Source],
						O.Visible,
						O.ResponsibilityID,
						O.ResponsibilityTypeID,
						O.AssigningItem,
						O.AssigningItemID,
						O.[Object],
						O.ObjectID,
						O.ContextHash,
						2 as [Priority]
				from	@tblModelHierarchy O
						inner join	(
									select		ResponsibilityTypeID,
												[Object],
												ObjectID,
												ContextHash,
												Max([Level]) as [Level]
									from		@tblModelHierarchy
									group by	ResponsibilityTypeID,
												[Object],
												ObjectID,
												ContextHash
									) M on M.ResponsibilityTypeID = O.ResponsibilityTypeID and M.[Object] = O.[Object] and M.ObjectID = O.ObjectID and M.ContextHash = O.ContextHash and M.[Level] = O.[Level];
		end

	if @Object = 'Policy'
		begin
			with PolicyHierarchy as
			(
			select	R.Visible,
					P.ID as AssigningItemID,
					P.ID,
					P.ParentID,
					R.ID as ResponsibilityID,
					R.ResponsibilityTypeID,
					utility.GetResponsibilityContextHash(R.ID) as ContextHash,
					1 as [Level]
			from	Policy P 
					left join Responsibility R on R.ObjectType = 'Policy' and R.ObjectID = P.ID
			where	P.ParentID is null
			union all
			select	
					COALESCE(R.Visible, P.Visible) as Visible,
					COALESCE(R.ObjectID, P.AssigningItemID) as AssigningItemID,
					C.ID,
					C.ParentID,
					COALESCE(R.ID, P.ResponsibilityID) as ResponsibilityID,
					COALESCE(R.ResponsibilityTypeID, P.ResponsibilityTypeID) as ResponsibilityTypeID,
					coalesce(R.ContextHash, P.ContextHash) as ContextHash,
					P.[Level] + 1 as [Level] 
			from	Policy C
					inner join PolicyHierarchy P on C.ParentID = P.ID
					outer apply (
								select	*,
										utility.GetResponsibilityContextHash(ID) as ContextHash
								from	Responsibility 
								where	ResponsibilityTypeID = P.ResponsibilityTypeID
										and ObjectType = 'Policy' 
										and ObjectID = C.ID
								) R
			)

			insert into @tblModelHierarchy
				select	Visible,
						ResponsibilityID,
						ResponsibilityTypeID,
						'Policy' as AssigningItemType,
						AssigningItemID,
						'Policy' as TargetObject, 
						ID,
						ContextHash,
						[Level]
				from	PolicyHierarchy
				where	ResponsibilityID is not null;

			insert into @tbl
				select	'Hierarchy Assigned' as [Source],
						O.Visible,
						O.ResponsibilityID,
						O.ResponsibilityTypeID,
						O.AssigningItem,
						O.AssigningItemID,
						O.[Object],
						O.ObjectID,
						O.ContextHash,
						1 as [Priority]
				from	@tblModelHierarchy O
						inner join	(
									select		ResponsibilityTypeID,
												[Object],
												ObjectID,
												ContextHash,
												Max([Level]) as [Level]
									from		@tblModelHierarchy
									group by	ResponsibilityTypeID,
												[Object],
												ObjectID,
												ContextHash
									) M on M.ResponsibilityTypeID = O.ResponsibilityTypeID and M.[Object] = O.[Object] and M.ObjectID = O.ObjectID and M.ContextHash = O.ContextHash and M.[Level] = O.[Level]
											and (
												(O.ObjectID = @ObjectID and @ObjectID is not null) or (@ObjectID is null)
												)
		end
	if @Object = 'Taxonomy'
		begin
			with ModelHierarchy as
			(
			select	R.Visible,
					T.ID as AssigningItemID,
					T.ID,
					T.ParentID,
					T.TaxonomyTypeID,
					R.ID as ResponsibilityID,
					R.ResponsibilityTypeID,
					utility.GetResponsibilityContextHash(R.ID) as ContextHash,
					1 as [Level]
			from	Taxonomy T 
					left join Responsibility R on R.ObjectType = 'Taxonomy' and R.ObjectID = T.ID
			union all
			select	COALESCE(R.Visible, P.Visible) as Visible,
					COALESCE(R.ObjectID, P.AssigningItemID) as AssigningItemID,
					C.ID,
					C.ParentID,
					C.TaxonomyTypeID,
					COALESCE(R.ID, P.ResponsibilityID) as ResponsibilityID,
					COALESCE(R.ResponsibilityTypeID, P.ResponsibilityTypeID) as ResponsibilityTypeID,
					coalesce(R.ContextHash, P.ContextHash) as ContextHash,
					P.[Level] + 1 as [Level]
			from	Taxonomy C
					inner join ModelHierarchy P on P.TaxonomyTypeID = C.TaxonomyTypeID and C.ParentID = P.ID
					outer apply (
								select	*,
										utility.GetResponsibilityContextHash(ID) as ContextHash
								from	Responsibility 
								where	ResponsibilityTypeID = P.ResponsibilityTypeID
										and ObjectType = 'Taxonomy' 
										and ObjectID = C.ID
								) R
			)

			insert into @tblModelHierarchy
				select	Visible,
						ResponsibilityID,
						ResponsibilityTypeID,
						'Taxonomy' as AssigningItemType,
						AssigningItemID,
						'Taxonomy' as TargetObject, 
						ID,
						ContextHash,
						[Level]
				from	ModelHierarchy
				where	ResponsibilityID is not null;


			insert into @tbl
				select	'Hierarchy Assigned' as [Source],
						O.Visible,
						O.ResponsibilityID,
						O.ResponsibilityTypeID,
						O.AssigningItem,
						O.AssigningItemID,
						O.[Object],
						O.ObjectID,
						O.ContextHash,
						@Priority as [Priority]
				from	@tblModelHierarchy O
						inner join	(
									select		ResponsibilityTypeID,
												[Object],
												ObjectID,
												ContextHash,
												Max([Level]) as [Level]
									from		@tblModelHierarchy
									group by	ResponsibilityTypeID,
												[Object],
												ObjectID,
												ContextHash
									) M on M.ResponsibilityTypeID = O.ResponsibilityTypeID and M.[Object] = O.[Object] and M.ObjectID = O.ObjectID and M.ContextHash = O.ContextHash and M.[Level] = O.[Level]
											and (
												(O.ObjectID = @ObjectID and @ObjectID is not null) or (@ObjectID is null)
												)
		end
	RETURN 
END
go




alter table IntersectTypePredicate add PredicateType int null
go


update	T
set		T.PredicateType = S.Type
from	IntersectTypePredicate T
		inner join Predicate S on S.ID = T.PredicateID
go

alter view [dbo].[Relationship]
as
	select	I.IntersectTypeID,
			R.IntersectID,
			case I.Classification
				when 0 then 2
				else I.Classification
			end as Classification,
			I.Description,
			substring((
						select	', ' + P.Name as [text()]
						from	IntersectMap IM
								inner join [Predicate] P on	P.ID = IM.PredicateID	
															and (
																(IM.SubjectIntersectNodeID = R.[SourceIntersectNodeID] and IM.ObjectIntersectNodeID = R.[TargetIntersectNodeID]) or
																(IM.SubjectIntersectNodeID = R.[TargetIntersectNodeID] and IM.ObjectIntersectNodeID = R.[SourceIntersectNodeID])
																)
						for xml path('')
						), 3, 1000) as [Role],
			--R.[Role],
			R.SourceIntersectTypeNodeID,
			R.SourceObject as SourceObjectType,
			R.SourceObjectID,
			coalesce(S.TextPath, S.Name) as SourceName, 
			S.Parent as SourceParent,
			S.ParentID as SourceParentID,
			S.ParentName as SourceParentName,
			S.ObjectTypeID as SourceTypeID,
			S.ObjectType as SourceType,
			S.ObjectTypeName as SourceTypeName,
			S.[Url] as SourceUrl,
			R.TargetIntersectTypeNodeID,
			T.Object as TargetObjectType,
			T.ObjectID as TargetObjectID,
			coalesce(T.TextPath, T.Name) as TargetName,
			T.Parent as TargetParent,
			T.ParentID as TargetParentID,
			T.ParentName as TargetParentName,
			T.ObjectTypeID as TargetTypeID,
			T.ObjectType as TargetType,
			T.ObjectTypeName as TargetTypeName,
			T.[Url] as TargetUrl,
			TR.[Exists] as HasTechnicalRelationships
	from	cache.Relationship R
			inner join [Intersect] I on I.ID = R.IntersectID
			left join [cache].[ObjectDetails] S on S.[Object] = R.SourceObject and S.ObjectID = R.SourceObjectID
			left join [cache].[ObjectDetails] T on T.[Object] = R.TargetObject and T.ObjectID = R.TargetObjectID
			cross apply (
						select	case 
									when count(1) > 0 then cast(1 as bit) 
									else cast(0 as bit) 
								end as [Exists]
						from	cache.Relationships
						where	SourceObject = 'Intersect' and SourceObjectID = R.IntersectID
						) TR
GO

alter procedure [dbo].[ProcessBulkLoad]
--declare
	@LoadID int
--set @LoadID = 29
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


	if @Action = 'P'	--PROMOTION
	begin
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
										inner join cache.ObjectDetails D on D.[ObjectType] = C2.[LookupObject] and D.ObjectTypeID = C2.LookupObjectID and (D.[Name] = C3.Value OR D.[TextPath] = C3.Value)
								) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex
		end

		declare @ResolvedObjects table ([Object] varchar(50), ObjectID int, [Action] varchar(25), LoadID int, RowIndex int)	--This captures the INSERTED/UPDATED objects from the merge statements below.

		if @Object = 'ArtifactType'
		begin
			declare @RequiresParent bit
			select	@RequiresParent =		case
												when ParentID is null then cast(0 as bit)
												else cast(1 as bit)
											end
									  from	ArtifactType 
									  where	ID = @ObjectID

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
									and (
											(@RequiresParent = 1 and P.ParentID is not null) or
											@RequiresParent = 0
										)
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
			on		(T.ArtifactTypeID = S.ArtifactTypeID and T.TaxonomyTypeID = S.TaxonomyTypeID and ((T.ParentID = S.ParentID and S.ParentID is not null) or (T.ParentID is null and S.ParentID is null)) and T.Name = S.Name)
			when	matched then
					update	set T.[Description] = IsNull(S.[Description], T.[Description]),
								T.[ParentID] = S.[ParentID],
								T.[Status] = 'Draft',
								T.TaxonomyTypeID = S.TaxonomyTypeID,
								T.UpdatedBy = @UpdatedBy,
								T.UpdatedOn = getutcdate()
			when	not matched then
					insert (ArtifactTypeID, TaxonomyTypeID, ParentID, Name, [Description], [Status], UpdatedOn, UpdatedBy)
					values (S.ArtifactTypeID, S.TaxonomyTypeID, S.ParentID, S.Name, S.[Description], 'Draft', getutcdate(), @UpdatedBy)
			output	'Artifact', inserted.ID, $action, S.LoadID, S.RowIndex into @ResolvedObjects;

			if @RequiresParent = 1
			begin
				-- Update the LoadItem table with the IDs we recieved in the merge statements above.
				update	T
				set		T.StatusMessage = 'Parent could not be found.'
				from	LoadItem T
						left join @ResolvedObjects S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex
				where	S.ObjectID is null
			end

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
								T.[Description] = IsNull(S.[Description],T.[Description]),
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
					update	set T.[Description] = IsNull(S.[Description],T.[Description]),
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
				[StatusMessage] = coalesce([StatusMessage], '') + ' Item could not be added nor updated.'
		where	LoadID = @LoadID
				and [ObjectID] is null
	end
	else
	begin
		-- This is for actions: R, U, L
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

		if @Action = 'L' -- LINEAGE (create lineage from input spreadsheet)
		begin
			declare @focalObject varchar(50),
					@focalObjectID int,
					@focalObjectTypeName nvarchar(1000),
					@focalName nvarchar(500),
					@sourceObjectTypeName nvarchar(1000),
					@sourceName nvarchar(500),
					@targetObjectTypeName nvarchar(1000),
					@targetName nvarchar(500),
					@intersectPredicate varchar(50),
					@predicateID int,
					@focalIntersectID int,
					@rundate datetime = CURRENT_TIMESTAMP,
					@focalSubject nvarchar(500),
					@sourceSubject nvarchar(500),
					@targetSubject nvarchar(500),
					@lineageErrorDetailMessage varchar(200)
			
			select	@current = min(I.RowIndex),
					@max = max(I.RowIndex)
			from	LoadItem I
					inner join LoadItemColumn FT on FT.LoadID = I.LoadID and FT.RowIndex = I.RowIndex and FT.ColumnIndex = 1  --focal point object type
						inner join LoadItemColumn FTN on FTN.LoadID = I.LoadID and FTN.RowIndex = I.RowIndex and FTN.ColumnIndex = 2   --focal point object type name
						inner join LoadItemColumn F on F.LoadID = I.LoadID and F.RowIndex = I.RowIndex and F.ColumnIndex = 4--focal point name		
						inner join LoadItemColumn ST on ST.LoadID = I.LoadID and ST.RowIndex = I.RowIndex and St.ColumnIndex = 5 --source object type
						inner join LoadItemColumn STN on STN.LoadID = I.LoadID and STN.RowIndex = I.RowIndex and StN.ColumnIndex = 6 --source object type name
						inner join LoadItemColumn S on S.LoadID = I.LoadID and S.RowIndex = I.RowIndex and S.ColumnIndex = 8 --source object name
						inner join LoadItemColumn TT on TT.LoadID = I.LoadID and TT.RowIndex = I.RowIndex and TT.ColumnIndex = 9 --target object type
						inner join LoadItemColumn TTN on TTN.LoadID = I.LoadID and TTN.RowIndex = I.RowIndex and TTN.ColumnIndex = 10 --target object type name
						inner join LoadItemColumn T on T.LoadID = I.LoadID and T.RowIndex = I.RowIndex and T.ColumnIndex = 12 --source object name
						inner join LoadItemColumn P on P.LoadID = I.LoadID and P.RowIndex = I.RowIndex and P.ColumnIndex = 13 --predicate
			where	I.LoadID = @LoadID
			
			-- go row by row
			while @current <= @max
			begin
				--load the objects / id's for the focal, source, and target objects
				select	
					@focalObject = FT.Value,
					@focalObjectTypeName = FTN.Value,
					@focalName = F.Value,
					@focalSubject = FS.Value,
					@sourceObject = ST.Value,
					@sourceObjectTypeName = STN.Value,
					@sourceName = S.Value,
					@sourceSubject = SS.Value,
					@targetObject = TT.Value,
					@targetObjectTypeName = TTN.Value,
					@targetName = T.Value,
					@targetSubject = TS.Value,
					@intersectPredicate = P.Value
				from	LoadItem I
						inner join LoadItemColumn FT on FT.LoadID = I.LoadID and FT.RowIndex = I.RowIndex and FT.ColumnIndex = 1  --focal point object type
						inner join LoadItemColumn FTN on FTN.LoadID = I.LoadID and FTN.RowIndex = I.RowIndex and FTN.ColumnIndex = 2  --focal point object type name
						inner join LoadItemColumn FS on FS.LoadID = I.LoadID and FS.RowIndex = I.RowIndex and FS.ColumnIndex = 3 --focal point subject area		
						inner join LoadItemColumn F on F.LoadID = I.LoadID and F.RowIndex = I.RowIndex and F.ColumnIndex = 4 --focal point name		
						inner join LoadItemColumn ST on ST.LoadID = I.LoadID and ST.RowIndex = I.RowIndex and St.ColumnIndex = 5 --source object type
						inner join LoadItemColumn STN on STN.LoadID = I.LoadID and STN.RowIndex = I.RowIndex and StN.ColumnIndex = 6 --source object type name
						inner join LoadItemColumn SS on SS.LoadID = I.LoadID and SS.RowIndex = I.RowIndex and SS.ColumnIndex = 7 --source object subject
						inner join LoadItemColumn S on S.LoadID = I.LoadID and S.RowIndex = I.RowIndex and S.ColumnIndex = 8 --source object name
						inner join LoadItemColumn TT on TT.LoadID = I.LoadID and TT.RowIndex = I.RowIndex and TT.ColumnIndex = 9 --target object type
						inner join LoadItemColumn TTN on TTN.LoadID = I.LoadID and TTN.RowIndex = I.RowIndex and TTN.ColumnIndex = 10 --target object type name
						inner join LoadItemColumn TS on TS.LoadID = I.LoadID and TS.RowIndex = I.RowIndex and TS.ColumnIndex = 11 --target object subject
						inner join LoadItemColumn T on T.LoadID = I.LoadID and T.RowIndex = I.RowIndex and T.ColumnIndex = 12 --source object name
						inner join LoadItemColumn P on P.LoadID = I.LoadID and P.RowIndex = I.RowIndex and P.ColumnIndex = 13 --predicate
				where	I.LoadID = @LoadID and I.RowIndex = @current

				select @focalObjectID = 0, @sourceObjectID = 0, @targetObjectID = 0, @predicateID = 0;

				select @predicateID = id from predicate where name = @intersectPredicate;				

				-- load focal object
				if @focalObject = 'Artifact'
				begin
					select top 1
						@focalObjectID = cod.objectid										
					from 
						[cache].objectdetails cod
						inner join artifact a on (cod.objectid = a.id)
						inner join taxonomytype t on (a.taxonomytypeid = t.id)
					where 
						cod.[object] = @focalObject and cod.textpath = @focalName and cod.objecttypename = @focalObjectTypeName and t.Name = @focalSubject
				end
				else
				begin
					select top 1
							@focalObjectID = cod.objectid										
					from 
						[cache].objectdetails cod
					where 
						cod.[object] = @focalObject and cod.textpath = @focalName and cod.objecttypename = @focalObjectTypeName
				end

				if @sourceObject = 'Artifact'
				begin
					select top 1
						@sourceObjectID = cod.objectid										
					from 
						[cache].objectdetails cod
						inner join artifact a on (cod.objectid = a.id)
						inner join taxonomytype t on (a.taxonomytypeid = t.id)
					where 
						cod.[object] = @sourceObject and cod.textpath = @sourceName and cod.objecttypename = @sourceObjectTypeName and t.Name = @sourceSubject
				end
				else
				begin
					-- load source object
					select top 1
							@sourceObjectID = cod.objectid						
					from 
						[cache].objectdetails cod
					where 
						cod.[object] = @sourceObject and cod.textpath = @sourceName and cod.objecttypename = @sourceObjectTypeName
				end

				if @targetObject = 'Artifact'
				begin
					-- load target object
					select top 1
							@targetObjectID = cod.objectid												
					from 
						[cache].objectdetails cod
						inner join artifact a on (cod.objectid = a.id)
						inner join taxonomytype t on (a.taxonomytypeid = t.id)
					where 
						cod.[object] = @targetObject and cod.textpath = @targetName and cod.objecttypename = @targetObjectTypeName and t.Name = @targetSubject
				end
				else
				begin
					-- load target object
					select top 1
							@targetObjectID = cod.objectid												
					from 
						[cache].objectdetails cod
					where 
						cod.[object] = @targetObject and cod.textpath = @targetName and cod.objecttypename = @targetObjectTypeName
				end

				--debug 
				--select @focalObjectID, @focalObject, @sourceObjectID, @sourceObject, @targetObjectID, @targetObject, @predicateID

				--if all are provided we are good otherwise error
				if @focalObjectID > 0 and @sourceObjectID > 0 and @targetObjectID > 0 and @predicateID > 0
					begin

					-- add intersect between focal object and source if one doesnt exist					
					exec [dbo].[AddRelationship] @UpdatedBy,@rundate,@focalObject,@focalObjectID,2,null,null,@sourceObject,@sourceObjectID;

					-- add intersect between focal object and target if one doesnt exist
					exec [dbo].[AddRelationship] @UpdatedBy,@rundate,@focalObject,@focalObjectID,2,null,null,@targetObject,@targetObjectID;
					
					-- add intersect between source / target if one doesnt exist
					exec [dbo].[AddRelationship] @UpdatedBy,@rundate,@sourceObject,@sourceObjectID,2,null,null,@targetObject,@targetObjectID;

					-- add intersect map between source / target if one doesnt exist for source to target intersect
					if not exists (select 1 from intersectmap map
							inner join intersectnode node1 on ( map.subjectintersectnodeid = node1.id and node1.objectid = @sourceObjectID and node1.objecttype = @sourceObject)
							inner join intersectnode node2 on ( map.objectintersectnodeid = node2.id and node2.objectid = @targetObjectID and node2.objecttype = @targetObject)
						where map.[type] = 1)
						begin							
							insert into intersectmap
								select 
									node1.ID as SubjectIntersectNode,
									node2.ID as ObjectIntersectNode,
									@predicateID as PredicateID,
									1 as [Type]
								from						
									intersectnode node1 
									inner join intersectnode node2 on (node1.objectid = @sourceObjectID and node1.objecttype =@sourceObject and node2.objectid = @targetObjectID and node2.objecttype = @targetObject and node1.intersectid = node2.intersectid);
						end


						update	LoadItem
						set		[Status] = 1,
								StatusMessage = 'Successfully added item to lineage'
						where	LoadID = @LoadID
								and RowIndex = @current
					end -- if valid
				else
					begin
						set @lineageErrorDetailMessage = '';

						if @focalObjectID = 0
						begin
							set @lineageErrorDetailMessage = '  Focal point is invalid.';
						end

						if @sourceObjectID = 0
						begin
							set @lineageErrorDetailMessage = @lineageErrorDetailMessage + '  Source object is invalid.';
						end

						if @targetObjectID = 0
						begin
							set @lineageErrorDetailMessage = @lineageErrorDetailMessage + '  Target object is invalid.';
						end

						update	LoadItem
						set		[Status] = 0,
								StatusMessage = 'Failed to add item to lineage.' + @lineageErrorDetailMessage + ' [focal id:' + convert(varchar(10), @focalObjectID) + ' type:' + @focalObject + '] [source id:' + convert(varchar(10),@sourceObjectID) + ' type:' + @sourceObject +'] [target id:' + convert(varchar(10), @targetObjectID) + ' type:' + @targetObject + ']'
						where	LoadID = @LoadID
								and RowIndex = @current
					end -- else not valid
				
				set @current = @current + 1
			end

		end

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
										inner join cache.ObjectDetails T on (T.[TextPath] = IC.Value or T.Name = IC.Value) and T.[ObjectType] = IT.[ObjectType] and T.ObjectTypeID = IT.ObjectID
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

	if @Action = 'P' or @Action = 'R'
	begin
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

	update	[Load] 
	set		DateCompleted = getutcdate()
	where	ID = @LoadID
end
GO


alter procedure [dbo].[ProcessSchedule]
as
begin
	set nocount on;

	declare @FusionIDs table (ID int identity, FusionID int)
	insert into @FusionIDs
		select ID from Fusion WHERE Enabled = 1 and Manual = 0

	declare @current int,
			@max int,
			@FusionID int,
			@DateStarted datetime,
			@DateCompleted datetime,
			@LastRunComplete bit,
			@IntervalType int,
			@Interval int,
			@MinDateJobMustStartNext datetime,
			@ShouldTriggerJob bit

	select	@current = 1,
			@max = MAX(ID)
	from	@FusionIDs

	delete FusionStatusLog where Success = 0 and DateStarted < DATEADD(hh, -6, getutcdate()) and MachineQueuedOn is not null

	while	@current <= @max
	begin
		select	@FusionID = F.ID,
				@IntervalType = F.IntervalType,
				@DateStarted = S.DateStarted,
				@DateCompleted = C.DateCompleted,
				@Interval = F.Interval
		from	Fusion F
				inner join @FusionIDs I on I.FusionID = F.ID and I.ID = @current
				outer apply (
							select	MAX(DateStarted) as DateStarted
							from	FusionStatusLog 
							where	FusionID = F.ID
							) S
				outer apply (
							select	DateCompleted
							from	FusionStatusLog 
							where	FusionID = F.ID
									and DateStarted = S.DateStarted
							) C
			set @LastRunComplete = case 
									when @DateStarted is not null and @DateCompleted is not null then 1
									else 0
								   end
	
		if (@DateStarted is null or @LastRunComplete = 1)
		begin
			if @DateCompleted is not null
			begin
				-- Get the next date when the job should run, based on the previous completed date, plus the interval.
				set @MinDateJobMustStartNext = case @IntervalType
													when 4 then DATEADD(s, @Interval, @DateCompleted)		-- SECOND
													when 3 then DATEADD(n, @Interval, @DateCompleted)		-- MINUTE
													when 2 then DATEADD(hh, @Interval, @DateCompleted)		-- HOUR
													else DATEADD(d, @Interval, @DateCompleted)				-- DAY = 1
												end
				set @ShouldTriggerJob = case 
											when DATEDIFF(s, @MinDateJobMustStartNext, getutcdate()) > 0 then 1
											else 0
										end
			end
		
			if @DateStarted is null
			begin
				-- Job has never been triggered, so force an execution immediately.
				set @ShouldTriggerJob = 1
			end
			
			if @ShouldTriggerJob = 1
			begin
				select	@FusionID, @IntervalType, @DateStarted, @DateCompleted, @Interval, @LastRunComplete
				insert into		FusionStatusLog
								(ID,		FusionID,		DateStarted,	Success)
				values			(newid(),	@FusionID,		getutcdate(),	0)
			end
		end

		set @current = @current + 1
	end
end
GO

alter procedure [tile].[GetObjectStatistics]
	@type varchar(50),
	@id int
AS
BEGIN
declare @table table (Name nvarchar(250), Value varchar(250), [Group] varchar(25), Url varchar(250))
	
	insert into @table
		select NULL, count(1), 'Followers', '/overlays/' + @type + '/' + cast(@id as varchar(10)) + '/followers'
		from	Follow F
		inner join reporting.Global_Resource R on R.ResourceID = F.ResourceID
		where	F.ObjectType = @type and F.ObjectID = @id
	
	insert into @table
		select	NULL, count(1), 'Comments', '/overlays/' + @type + '/' + cast(@id as varchar(10)) + '/comments'
		from	Comment C
				inner join CommentRelation R	on R.CommentID = C.ID and C.ParentID is null
												and R.ObjectType = @type and R.ObjectID = @id
                                                and C.ParentID is null
												and C.IsDeleted = 0
	insert into @table
		select NULL, count(1), 'Events', '/overlays/' + @type + '/' + cast(@id as varchar(10)) + '/events'
			FROM	    [Event] E
					    INNER JOIN EventGroup G ON E.EventGroupID = G.ID and E.Status in ('Active', 'Open')
					    INNER JOIN [Rule] R on R.ID = G.RuleID
					    inner join cache.Relationships CR on CR.SourceObject = @type and CR.SourceObjectID = @id and CR.TargetObject = 'Rule' and CR.TargetObjectID = R.ID

	insert into @table values (null, dbo.[GetObjectStatisticScore](@type, @id) * 100, 'Score', '/overlays/' + @type + '/' + cast(@id as varchar(10)) + '/score')

	if @type = 'Artifact'
	begin
		insert into @table 
			select		lower(T.Name),
						count(1),
						'Children',
						'/overlays/' + cast(@id as varchar(10)) + '/' + cast(T.ID as varchar(10)) + '/ChildArtifacts'
			from		Artifact A
						inner join ArtifactType T on T.ID = A.ArtifactTypeID and A.ParentID = @id
			group by	T.Name,
						T.ID
			order by	T.Name


		insert into @table
			select	
				'Issue',
				count(1),
				'Issues',
				'/overlays/Artifact/' + cast(@id as varchar(10)) + '/Issues'
			from	
					workflow w
					inner join Comment C on C.ID = w.data.value('(fields/CommentID)[1]', 'int')
					inner join CommentRelation CR on CR.CommentID = C.ID and CR.ObjectType = 'Artifact'
					inner join Artifact A on w.workflowtype = 3 and w.datecompleted is null and A.ID = cr.objectid
			where 
				a.id = @id			
	end


	select * from @table

END
GO

alter procedure [utility].[GetOwnersForWorkflow]
--declare 
	@workflowID uniqueidentifier
--set @workflowID = '387A8094-565E-45AF-B049-01329EEF2209' --=> wt 1
--set @workflowID = '0C573C9B-D237-4468-8822-7D515750675B'--'CEE2AF0D-DAB8-432B-AF08-00E52B808C52' --=> wt 2
--set @workflowID = 'FD3C4A3D-C9BB-477A-B5CD-BC99C62AF53F' --=> wt 3
as
begin
	declare @workflowType int,
			@fields xml
	declare @tbl table (ID int, FirstName nvarchar(250), LastName nvarchar(250), Email nvarchar(500), Username nvarchar(500), DateLastLoggedIn datetime null, ResourceTypeID int, Status nvarchar(25))

	select	@workflowType = WorkflowType,
			@fields = Data
	from	Workflow
	where	ID = @workflowID

	if @workflowType = 1
	begin
		--1. Check for vocabulary owners
		insert into @tbl
			select	R.ResourceID, R.FirstName, R.LastName, R.Email, R.Email, R.DateLastLoggedIn, 1 as ResourceTypeID, R.Status 
			from	ResponsibilityDetail RD 
					inner join WorkflowTypeRelation WTR on WTR.Parent = 'TaxonomyType' and WTR.ParentID = @fields.value('(/fields/TaxonomyTypeID)[1]', 'int') and WTR.WorkflowType = @workflowType and WTR.ResponsibilityTypeID = RD.ResponsibilityTypeID
					inner join reporting.Global_Resource R 
						on RD.ObjectType = 'TaxonomyType' 
						and RD.ObjectID = @fields.value('(/fields/TaxonomyTypeID)[1]', 'int')
						and	(
								(RD.ResponsibleObjectType = 'Group' and R.ResourceID = RD.PrimaryOwnerResourceID) or 
								(RD.ResponsibleObjectType = 'Resource' and R.ResourceID = RD.ResponsibleObjectID)
							)
						and R.Email not like '%?subject=%' and R.Status = 'Active'

		if not exists(select * from @tbl)
		begin
			insert into @tbl
				select	R.ResourceID, R.FirstName, R.LastName, R.Email, R.Email, R.DateLastLoggedIn, 1 as ResourceTypeID, R.Status 
				from	ResponsibilityDetail RD 
						inner join WorkflowTypeRelation WTR on WTR.[Object] = 'ArtifactType' and WTR.ObjectID = @fields.value('(/fields/ArtifactTypeID)[1]', 'int') and WTR.Parent is null and WTR.WorkflowType = @workflowType and WTR.ResponsibilityTypeID = RD.ResponsibilityTypeID
						inner join reporting.Global_Resource R 
							on RD.ObjectType = 'ArtifactType' 
							and RD.ObjectID = @fields.value('(/fields/ArtifactTypeID)[1]', 'int')
							and (
									(RD.ResponsibleObjectType = 'Group' and R.ResourceID = RD.PrimaryOwnerResourceID) or 
									(RD.ResponsibleObjectType = 'Resource' and R.ResourceID = RD.ResponsibleObjectID)
								)
							and R.Email not like '%?subject=%' and R.Status = 'Active'
		end
	end

	if @workflowType = 2
	begin
		insert into @tbl
			select	R.ResourceID, R.FirstName, R.LastName, R.Email, R.Email, R.DateLastLoggedIn, 1 as ResourceTypeID, R.Status 
			from	ResponsibilityDetail RD 
					inner join Artifact A on RD.ObjectType = 'Artifact' and RD.ObjectID = A.ID and A.ID = @fields.value('(/fields/ArtifactID)[1]', 'int')
					inner join WorkflowTypeRelation WTR		on WTR.[Object] = 'ArtifactType' and WTR.ObjectID = A.ArtifactTypeID 
															and WTR.Parent = 'TaxonomyType' and WTR.ParentID = A.TaxonomyTypeID
															and WTR.WorkflowType = @workflowType 
															and WTR.ResponsibilityTypeID = RD.ResponsibilityTypeID
					inner join reporting.Global_Resource R 
						on	(
								(RD.ResponsibleObjectType = 'Group' and R.ResourceID = RD.PrimaryOwnerResourceID) or 
								(RD.ResponsibleObjectType = 'Resource' and R.ResourceID = RD.ResponsibleObjectID)
							)
						and R.Email not like '%?subject=%' and R.Status = 'Active' 

		if not exists(select * from @tbl)
		begin
			insert into @tbl
				select	R.ResourceID, R.FirstName, R.LastName, R.Email, R.Email, R.DateLastLoggedIn, 1 as ResourceTypeID, R.Status 
				from	ResponsibilityDetail RD 
						inner join Artifact A on RD.ObjectType = 'Artifact' and RD.ObjectID = A.ID and A.ID = @fields.value('(/fields/ArtifactID)[1]', 'int')
						inner join WorkflowTypeRelation WTR		on WTR.[Object] = 'ArtifactType' and WTR.ObjectID = A.ArtifactTypeID 
																and WTR.WorkflowType = @workflowType 
																and WTR.ResponsibilityTypeID = RD.ResponsibilityTypeID
						inner join reporting.Global_Resource R 
							on	(
									(RD.ResponsibleObjectType = 'Group' and R.ResourceID = RD.PrimaryOwnerResourceID) or 
									(RD.ResponsibleObjectType = 'Resource' and R.ResourceID = RD.ResponsibleObjectID)
								)
							and R.Email not like '%?subject=%' and R.Status = 'Active' 
		end
	end

	if @workflowType = 3
	begin

		insert into @tbl
			select	distinct
						R.ResourceID, R.FirstName, R.LastName, R.Email, R.Email, R.DateLastLoggedIn, 1 as ResourceTypeID, R.Status 
				from	Comment C
						inner join CommentRelation CR on CR.CommentID = C.ID and C.ID = @fields.value('(fields/CommentID)[1]', 'int') and CR.ObjectType not in ('Resource', 'Group')
						inner join ResponsibilityDetail RD on RD.ObjectType = CR.ObjectType and RD.ObjectID = CR.ObjectID 
						inner join WorkflowTypeRelation WTR		on WTR.[Object] = RD.ObjectType +'Type' and WTR.ObjectID = RD.ObjectID 
																and WTR.WorkflowType = @workflowType 
																and WTR.ResponsibilityTypeID = RD.ResponsibilityTypeID
																and WTR.[Enabled] = 1
						inner join reporting.Global_Resource R 
							on	(
									(RD.ResponsibleObjectType = 'Group' and R.ResourceID = RD.PrimaryOwnerResourceID) or 
									(RD.ResponsibleObjectType = 'Resource' and R.ResourceID = RD.ResponsibleObjectID)
								) 
								and R.Email not like '%?subject=%' and R.Status = 'Active'

		if not exists (select 1 from @tbl)
		begin
			insert into @tbl
				select	distinct
						R.ResourceID, R.FirstName, R.LastName, R.Email, R.Email, R.DateLastLoggedIn, 1 as ResourceTypeID, R.Status 
				from	Comment C
						inner join CommentRelation CR on CR.CommentID = C.ID and C.ID = @fields.value('(fields/CommentID)[1]', 'int') and CR.ObjectType not in ('Resource', 'Group')
						inner join ResponsibilityDetail RD on RD.ObjectType = CR.ObjectType and RD.ObjectID = CR.ObjectID 
						inner join reporting.Global_Resource R 
							on	(
									(RD.ResponsibleObjectType = 'Group' and R.ResourceID = RD.PrimaryOwnerResourceID) or 
									(RD.ResponsibleObjectType = 'Resource' and R.ResourceID = RD.ResponsibleObjectID)
								) 
								and R.Email not like '%?subject=%' and R.Status = 'Active'
		end

		if not exists (select 1 from @tbl)
		begin
			insert into @tbl
				select 
					R.ResourceID, R.FirstName, R.LastName, R.Email, R.Email, R.DateLastLoggedIn, 1 as ResourceTypeID, R.Status 
				from 
					reporting.Global_Resource R where isadministrator = 1 and status = 'Active'
		end
	end

	if @workflowType = 4
	begin
		insert into @tbl
				select	R.ResourceID, R.FirstName, R.LastName, R.Email, R.Email, R.DateLastLoggedIn, 1 as ResourceTypeID, R.Status 
				from	ResponsibilityDetail RD 
						inner join Artifact A on RD.ObjectType = 'Artifact' and RD.ObjectID = A.ID and A.ID = @fields.value('(/fields/ArtifactID)[1]', 'int')
						inner join WorkflowTypeRelation WTR		on WTR.[Object] = 'ArtifactType' and WTR.ObjectID = A.ArtifactTypeID 
																and WTR.WorkflowType = @workflowType 
																and WTR.ResponsibilityTypeID = RD.ResponsibilityTypeID
																and WTR.[Enabled] = 1
						inner join reporting.Global_Resource R 
							on	(
									(RD.ResponsibleObjectType = 'Group' and R.ResourceID = RD.PrimaryOwnerResourceID) or 
									(RD.ResponsibleObjectType = 'Resource' and R.ResourceID = RD.ResponsibleObjectID)
								)
							and R.Email not like '%?subject=%' and R.Status = 'Active' 

		if not exists (select 1 from @tbl)
		begin
			insert into @tbl
				select	distinct
						R.ResourceID, R.FirstName, R.LastName, R.Email, R.Email, R.DateLastLoggedIn, 1 as ResourceTypeID, R.Status 
				from	Comment C
						inner join CommentRelation CR on CR.CommentID = C.ID and C.ID = @fields.value('(fields/CommentID)[1]', 'int') and CR.ObjectType not in ('Resource', 'Group')
						inner join ResponsibilityDetail RD on RD.ObjectType = CR.ObjectType and RD.ObjectID = CR.ObjectID 
						inner join reporting.Global_Resource R 
							on	(
									(RD.ResponsibleObjectType = 'Group' and R.ResourceID = RD.PrimaryOwnerResourceID) or 
									(RD.ResponsibleObjectType = 'Resource' and R.ResourceID = RD.ResponsibleObjectID)
								) 
								and R.Email not like '%?subject=%' and R.Status = 'Active'
		end

		if not exists (select 1 from @tbl)
		begin
			insert into @tbl
				select 
					R.ResourceID, R.FirstName, R.LastName, R.Email, R.Email, R.DateLastLoggedIn, 1 as ResourceTypeID, R.Status 
				from 
					reporting.Global_Resource R where isadministrator = 1 and status = 'Active'
		end
	end

	select * from @tbl
end
GO


ALTER PROCEDURE [utility].[PromoteFusionAttributeLookups]	 
AS
BEGIN
	SET NOCOUNT ON;

	declare @currentID int = 0,
			@maxID int = 0;
	

	IF OBJECT_ID('tempdb..#fieldValues') IS NOT NULL
		DROP TABLE #fieldValues;

	create table #fieldValues (
		ObjectType varchar(50), 
		ObjectID int, 
		FieldTypeID int, 
		Value int
	);


	insert into #fieldValues
		select 
			fap.ObjectType as ObjectType,
			fap.ObjectID as ObjectID,
			fusLook.FieldTypeID as FieldTypeID,						
			max(fap.fusionattributeid) as Value						
		from [dbo].[FusionAttributePromotionRule] pr
		inner join [dbo].[fieldtype] ft on (ft.[objectid] = pr.PromotionObjectID and ft.[object] = pr.PromotionObjectType)
		inner join [dbo].[FieldTypeFusionLookupDefinition] fusLook  on (ft.id = fusLook.fieldTypeid)
		inner join fusionattribute fa on (fa.fusionattributetypeid = pr.ObjectID and fa.fusionAttributetypeid = fusLook.SourceFusionAttributeTypeID)
		inner join fusionattributepromotion fap on (fa.id = fap.fusionattributeid)
		where pr.[enabled] = 1 and fap.ObjectType != 'Intersect' group by fap.ObjectType, fap.ObjectID, fusLook.FieldTypeID
	
		
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

END

--exec [utility].[PromoteFusionAttributeLookups]
GO


ALTER FUNCTION [utility].[GetHierarchyAssignedResponsibilityList]
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
	declare @tblModelHierarchy table (
		Visible bit,
		ResponsibilityID int,
		ResponsibilityTypeID int,
		AssigningItem varchar(50),
		AssigningItemID int,
		[Object] varchar(50),
		ObjectID int,
		ContextHash varchar(50),
		[Level] int
	);

	if @Object = 'Artifact'
		begin
			with ModelRelationHierarchy as
			(
			select	R.Visible,
					'Taxonomy' as AssigningItemType, 
					T.ID as AssigningItemID,
					T.ID,
					T.ParentID,
					T.TaxonomyTypeID,
					R.ID as ResponsibilityID,
					R.ResponsibilityTypeID,
					utility.GetResponsibilityContextHash(R.ID) as ContextHash,
					1 as [Level]
			from	Taxonomy T 
					left join Responsibility R on R.ObjectType = 'Taxonomy' and R.ObjectID = T.ID 
			union all
			select	
					COALESCE(R.Visible, P.Visible) as Visible,
					P.AssigningItemType,
					COALESCE(R.ObjectID, P.AssigningItemID) as AssigningItemID,
					C.ID,
					C.ParentID,
					C.TaxonomyTypeID,
					COALESCE(R.ID, P.ResponsibilityID) as ResponsibilityID,
					COALESCE(R.ResponsibilityTypeID, P.ResponsibilityTypeID) as ResponsibilityTypeID,
					coalesce(R.ContextHash, P.ContextHash) as ContextHash,
					P.[Level] + 1 as [Level]
			from	Taxonomy C
					inner join ModelRelationHierarchy P on P.TaxonomyTypeID = C.TaxonomyTypeID and C.ParentID = P.ID
					outer apply (
								select	*,
										utility.GetResponsibilityContextHash(ID) as ContextHash
								from	Responsibility 
								where	ResponsibilityTypeID = P.ResponsibilityTypeID
										and ObjectType = 'Taxonomy' 
										and ObjectID = C.ID
								) R
			)

			insert into @tblModelHierarchy
				select		P.Visible,
							P.ResponsibilityID,
							P.ResponsibilityTypeID,
							P.AssigningItemType,
							P.AssigningItemID,
							R.TargetObject, 
							R.TargetObjectID,
							P.ContextHash,
							P.[Level]
				from		ModelRelationHierarchy P
							inner join cache.Relationship R on 
								R.SourceObject = 'Taxonomy' and R.SourceObjectID = P.ID 
								and R.TargetObject = 'Artifact'
							inner join Artifact A on A.ID = R.TargetObjectID and A.TaxonomyTypeID = P.TaxonomyTypeID
								and (
									(A.ID = @ObjectID and @ObjectID is not null) or (@ObjectID is null)
									)
							inner join ResponsibilityTypeRelation RTR on RTR.ResponsibilityTypeID = P.ResponsibilityTypeID and RTR.ObjectType = 'ArtifactType' and RTR.ObjectID = A.ArtifactTypeID
				where		P.ResponsibilityID is not null;


			insert into @tbl
				select	'Hierarchy Assigned' as [Source],
						O.Visible,
						O.ResponsibilityID,
						O.ResponsibilityTypeID,
						O.AssigningItem,
						O.AssigningItemID,
						O.[Object],
						O.ObjectID,
						O.ContextHash,
						2 as [Priority]
				from	@tblModelHierarchy O
						inner join	(
									select		ResponsibilityTypeID,
												[Object],
												ObjectID,
												ContextHash,
												Max([Level]) as [Level]
									from		@tblModelHierarchy
									group by	ResponsibilityTypeID,
												[Object],
												ObjectID,
												ContextHash
									) M on M.ResponsibilityTypeID = O.ResponsibilityTypeID and M.[Object] = O.[Object] and M.ObjectID = O.ObjectID and M.ContextHash = O.ContextHash and M.[Level] = O.[Level];
		end

	if @Object = 'Policy'
		begin
			with PolicyHierarchy as
			(
			select	R.Visible,
					P.ID as AssigningItemID,
					P.ID,
					P.ParentID,
					R.ID as ResponsibilityID,
					R.ResponsibilityTypeID,
					utility.GetResponsibilityContextHash(R.ID) as ContextHash,
					1 as [Level]
			from	Policy P 
					left join Responsibility R on R.ObjectType = 'Policy' and R.ObjectID = P.ID
			where	P.ParentID is null
			union all
			select	
					COALESCE(R.Visible, P.Visible) as Visible,
					COALESCE(R.ObjectID, P.AssigningItemID) as AssigningItemID,
					C.ID,
					C.ParentID,
					COALESCE(R.ID, P.ResponsibilityID) as ResponsibilityID,
					COALESCE(R.ResponsibilityTypeID, P.ResponsibilityTypeID) as ResponsibilityTypeID,
					coalesce(R.ContextHash, P.ContextHash) as ContextHash,
					P.[Level] + 1 as [Level] 
			from	Policy C
					inner join PolicyHierarchy P on C.ParentID = P.ID
					outer apply (
								select	*,
										utility.GetResponsibilityContextHash(ID) as ContextHash
								from	Responsibility 
								where	ResponsibilityTypeID = P.ResponsibilityTypeID
										and ObjectType = 'Policy' 
										and ObjectID = C.ID
								) R
			)

			insert into @tblModelHierarchy
				select	Visible,
						ResponsibilityID,
						ResponsibilityTypeID,
						'Policy' as AssigningItemType,
						AssigningItemID,
						'Policy' as TargetObject, 
						ID,
						ContextHash,
						[Level]
				from	PolicyHierarchy
				where	ResponsibilityID is not null;

			insert into @tbl
				select	'Hierarchy Assigned' as [Source],
						O.Visible,
						O.ResponsibilityID,
						O.ResponsibilityTypeID,
						O.AssigningItem,
						O.AssigningItemID,
						O.[Object],
						O.ObjectID,
						O.ContextHash,
						1 as [Priority]
				from	@tblModelHierarchy O
						inner join	(
									select		ResponsibilityTypeID,
												[Object],
												ObjectID,
												ContextHash,
												Max([Level]) as [Level]
									from		@tblModelHierarchy
									group by	ResponsibilityTypeID,
												[Object],
												ObjectID,
												ContextHash
									) M on M.ResponsibilityTypeID = O.ResponsibilityTypeID and M.[Object] = O.[Object] and M.ObjectID = O.ObjectID and M.ContextHash = O.ContextHash and M.[Level] = O.[Level]
											and (
												(O.ObjectID = @ObjectID and @ObjectID is not null) or (@ObjectID is null)
												)
		end
	if @Object = 'Taxonomy'
		begin
			with ModelHierarchy as
			(
			select	R.Visible,
					T.ID as AssigningItemID,
					T.ID,
					T.ParentID,
					T.TaxonomyTypeID,
					R.ID as ResponsibilityID,
					R.ResponsibilityTypeID,
					utility.GetResponsibilityContextHash(R.ID) as ContextHash,
					1 as [Level]
			from	Taxonomy T 
					left join Responsibility R on R.ObjectType = 'Taxonomy' and R.ObjectID = T.ID
			union all
			select	COALESCE(R.Visible, P.Visible) as Visible,
					COALESCE(R.ObjectID, P.AssigningItemID) as AssigningItemID,
					C.ID,
					C.ParentID,
					C.TaxonomyTypeID,
					COALESCE(R.ID, P.ResponsibilityID) as ResponsibilityID,
					COALESCE(R.ResponsibilityTypeID, P.ResponsibilityTypeID) as ResponsibilityTypeID,
					coalesce(R.ContextHash, P.ContextHash) as ContextHash,
					P.[Level] + 1 as [Level]
			from	Taxonomy C
					inner join ModelHierarchy P on P.TaxonomyTypeID = C.TaxonomyTypeID and C.ParentID = P.ID
					outer apply (
								select	*,
										utility.GetResponsibilityContextHash(ID) as ContextHash
								from	Responsibility 
								where	ResponsibilityTypeID = P.ResponsibilityTypeID
										and ObjectType = 'Taxonomy' 
										and ObjectID = C.ID
								) R
			)

			insert into @tblModelHierarchy
				select	Visible,
						ResponsibilityID,
						ResponsibilityTypeID,
						'Taxonomy' as AssigningItemType,
						AssigningItemID,
						'Taxonomy' as TargetObject, 
						ID,
						ContextHash,
						[Level]
				from	ModelHierarchy
				where	ResponsibilityID is not null;


			insert into @tbl
				select	'Hierarchy Assigned' as [Source],
						O.Visible,
						O.ResponsibilityID,
						O.ResponsibilityTypeID,
						O.AssigningItem,
						O.AssigningItemID,
						O.[Object],
						O.ObjectID,
						O.ContextHash,
						@Priority as [Priority]
				from	@tblModelHierarchy O
						inner join	(
									select		ResponsibilityTypeID,
												[Object],
												ObjectID,
												ContextHash,
												Max([Level]) as [Level]
									from		@tblModelHierarchy
									group by	ResponsibilityTypeID,
												[Object],
												ObjectID,
												ContextHash
									) M on M.ResponsibilityTypeID = O.ResponsibilityTypeID and M.[Object] = O.[Object] and M.ObjectID = O.ObjectID and M.ContextHash = O.ContextHash and M.[Level] = O.[Level]
											and (
												(O.ObjectID = @ObjectID and @ObjectID is not null) or (@ObjectID is null)
												)
		end
	RETURN 
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
														)
						inner join ResponsibilityTypeRelation RTR on RTR.ResponsibilityTypeID = R.ResponsibilityTypeID and RTR.ObjectType = 'ArtifactType' and RTR.ObjectID = A.ArtifactTypeID;
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


ALTER TABLE [dbo].[IntersectMapSourceRule] DROP CONSTRAINT [FK_IntersectMapSourceRule_IntersectMap]
GO
ALTER TABLE [dbo].[IntersectMapSourceRule]  WITH CHECK ADD  CONSTRAINT [FK_IntersectMapSourceRule_IntersectMap] FOREIGN KEY([IntersectMapID]) REFERENCES [dbo].[IntersectMap] ([ID]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[IntersectMapSourceRule] CHECK CONSTRAINT [FK_IntersectMapSourceRule_IntersectMap]
GO

ALTER TABLE [dbo].[IntersectMapSourceRule] DROP CONSTRAINT [FK_IntersectMapSourceRule_SourceRule]
GO
ALTER TABLE [dbo].[IntersectMapSourceRule]  WITH CHECK ADD  CONSTRAINT [FK_IntersectMapSourceRule_SourceRule] FOREIGN KEY([SourceRuleID]) REFERENCES [dbo].[SourceRule] ([ID]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[IntersectMapSourceRule] CHECK CONSTRAINT [FK_IntersectMapSourceRule_SourceRule]
GO

ALTER TABLE [dbo].[IntersectMapSourceRuleContext] DROP CONSTRAINT [FK_IntersectMapSourceRuleContext_IntersectMapSourceRule]
GO
ALTER TABLE [dbo].[IntersectMapSourceRuleContext]  WITH CHECK ADD  CONSTRAINT [FK_IntersectMapSourceRuleContext_IntersectMapSourceRule] FOREIGN KEY([IntersectMapSourceRuleID]) REFERENCES [dbo].[IntersectMapSourceRule] ([ID]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[IntersectMapSourceRuleContext] CHECK CONSTRAINT [FK_IntersectMapSourceRuleContext_IntersectMapSourceRule]
GO

CREATE TABLE [dbo].[IntersectMapSourceTargetRule](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[RuleID] [int] NOT NULL,
	[IntersectMapID] [int] NOT NULL,
	CONSTRAINT [PK_IntersectMapSourceTargetRule] PRIMARY KEY CLUSTERED ( [ID] ASC )
)
GO

ALTER TABLE [dbo].[IntersectMapSourceTargetRule]  WITH CHECK ADD  CONSTRAINT [FK_IntersectMapSourceTargetRule_IntersectMap] FOREIGN KEY([IntersectMapID]) REFERENCES [dbo].[IntersectMap] ([ID]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[IntersectMapSourceTargetRule] CHECK CONSTRAINT [FK_IntersectMapSourceTargetRule_IntersectMap]
GO

CREATE TABLE [dbo].[SourceTargetRule](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[FocalObjectID] [int] NOT NULL,
	[FocalObject] [varchar](150) NOT NULL,
	[SourceObjectID] [int] NOT NULL,
	[SourceObject] [varchar](150) NOT NULL,
	[TargetObjectID] [int] NOT NULL,
	[TargetObject] [varchar](150) NOT NULL,
	[Transformation] [varchar](500) NULL,
	CONSTRAINT [PK_SourceTargetRule] PRIMARY KEY NONCLUSTERED ( [ID] ASC )
)
GO

ALTER TABLE [dbo].[IntersectMapSourceTargetRule]  WITH CHECK ADD  CONSTRAINT [FK_IntersectMapSourceTargetRule_SourceTargetRule] FOREIGN KEY([RuleID]) REFERENCES [dbo].[SourceTargetRule] ([ID]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[IntersectMapSourceTargetRule] CHECK CONSTRAINT [FK_IntersectMapSourceTargetRule_SourceTargetRule]
GO

CREATE NONCLUSTERED INDEX [IX_CacheResponsibilityItem_ResponsibilityTypeID__Object_ObjectID]
    ON [cache].[ResponsibilityItem]([ResponsibilityTypeID] ASC, [Object] ASC, [ObjectID] ASC);
GO

CREATE CLUSTERED INDEX [CIX_SourceTargetRule] ON [dbo].[SourceTargetRule] ( FocalObject ASC, FocalObjectID ASC, SourceObject ASC, SourceObjectID ASC, TargetObject ASC, TargetObjectID ASC )
GO



ALTER TABLE [dbo].[IntersectMap] DROP CONSTRAINT [FK_IntersectMap_ObjectIntersectNode]
GO
ALTER TABLE [dbo].[IntersectMap]  WITH CHECK ADD  CONSTRAINT [FK_IntersectMap_ObjectIntersectNode] FOREIGN KEY([ObjectIntersectNodeID]) REFERENCES [dbo].[IntersectNode] ([ID]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[IntersectMap] CHECK CONSTRAINT [FK_IntersectMap_ObjectIntersectNode]
GO

ALTER TABLE [dbo].[IntersectMap] DROP CONSTRAINT [FK_IntersectMap_Predicate]
GO
ALTER TABLE [dbo].[IntersectMap]  WITH CHECK ADD  CONSTRAINT [FK_IntersectMap_Predicate] FOREIGN KEY([PredicateID]) REFERENCES [dbo].[Predicate] ([ID]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[IntersectMap] CHECK CONSTRAINT [FK_IntersectMap_Predicate]
GO


delete IntersectMapGroup where IntersectMapID not in (select ID from IntersectMap)
go
ALTER TABLE [dbo].[IntersectMapGroup]  WITH CHECK ADD  CONSTRAINT [FK_IntersectMapGroup_IntersectMap] FOREIGN KEY([IntersectMapID]) REFERENCES [dbo].[IntersectMap] ([ID]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[IntersectMapGroup] CHECK CONSTRAINT [FK_IntersectMapGroup_IntersectMap]
GO

CREATE CLUSTERED INDEX CIX_IntersectMapGroup ON IntersectMapGroup ( GroupNumber asc )
GO

CREATE NONCLUSTERED INDEX [IX_IntersectMap_SubjectIntersectNodeID_ObjectIntersectNodeID] ON [dbo].[IntersectMap] ( [SubjectIntersectNodeID] ASC, [ObjectIntersectNodeID] ASC )
GO

CREATE NONCLUSTERED INDEX [IX_IntersectMapSourceRule_IntersectMap] ON [dbo].[IntersectMapSourceRule] ( [IntersectMapID] ASC )
GO
CREATE NONCLUSTERED INDEX [IX_IntersectMapSourceRule_SourceRule] ON [dbo].[IntersectMapSourceRule] ( [SourceRuleID] ASC )
GO

ALTER TABLE [dbo].[FusionAttribute] DROP CONSTRAINT [FK_FusionAttribute_ParentFusionAttribute]
GO

CREATE TABLE [dbo].[BusinessTransformationRule] (
    [ID]             INT           IDENTITY (1, 1) NOT NULL,
    [FocalObjectID]  INT           NOT NULL,
    [FocalObject]    VARCHAR (50)  NOT NULL,
    [SourceObjectID] INT           NOT NULL,
    [SourceObject]   VARCHAR (50)  NOT NULL,
    [TargetObjectID] INT           NOT NULL,
    [TargetObject]   VARCHAR (50)  NOT NULL,
    [Transformation] VARCHAR (MAX) NOT NULL,
    CONSTRAINT [PK_BusinessTransformationRule] PRIMARY KEY NONCLUSTERED ([ID] ASC)
)
GO
CREATE CLUSTERED INDEX [CIX_BusinessTransformationRule] ON [dbo].[BusinessTransformationRule]([FocalObject] ASC, [FocalObjectID] ASC, [SourceObject] ASC, [SourceObjectID] ASC, [TargetObject] ASC, [TargetObjectID] ASC)
GO

CREATE NONCLUSTERED INDEX IX_SourceRule_AppliesToObject_Object ON dbo.SourceRule ( AppliesToObject asc, AppliesToObjectID asc, [Object] asc, ObjectID asc )
GO

CREATE TABLE [dbo].[RuleDimension]
(
	[ID] INT IDENTITY (1, 1) NOT NULL, 
    [Name] NVARCHAR(250) NOT NULL, 
    [Description] NVARCHAR(4000) NULL,
	[IsSystemDefined] BIT NOT NULL DEFAULT(0),
    [UpdatedOn] DATETIME NOT NULL DEFAULT getUtcDate(), 
    [UpdatedBy] INT NOT NULL,
	CONSTRAINT [PK_RuleDimension] PRIMARY KEY CLUSTERED ([ID] ASC)
)
go

begin
	insert into [dbo].[RuleDimension] (Name,Description,IsSystemDefined,UpdatedBy) values(N'Completeness',N'Is all the requisite information available? Are data values missing, or in an unusable state? In some cases, missing data is irrelevant, but when the information that is missing is critical to a specific business process, completeness becomes an issue. ',1,0)
	insert into [dbo].[RuleDimension] (Name,Description,IsSystemDefined,UpdatedBy) values(N'Conformity',N'Are there expectations that data values conform to specified formats? If so, do all the values conform to those formats? Maintaining conformance to specific formats is important in data representation, presentation, aggregate reporting, search, and establishing key relationships.',1,0)
	insert into [dbo].[RuleDimension] (Name,Description,IsSystemDefined,UpdatedBy) values(N'Consistency',N'Do distinct data instances provide conflicting information about the same underlying data object? Are values consistent across data sets? Do interdependent attributes always appropriately reflect their expected consistency? Inconsistency between data values plagues organizations attempting to reconcile between different systems and applications.',1,0)
	insert into [dbo].[RuleDimension] (Name,Description,IsSystemDefined,UpdatedBy) values(N'Accuracy',N'Do data objects accurately represent the “real-world” values they are expected to model? Incorrect spellings of product or person names, addresses, and even untimely or not current data can impact operational and analytical applications.',1,0)
	insert into [dbo].[RuleDimension] (Name,Description,IsSystemDefined,UpdatedBy) values(N'Duplication',N'Are there multiple, unnecessary representations of the same data objects within your data set? The inability to maintain a single representation for each entity across your systems poses numerous vulnerabilities and risks.',1,0)
	insert into [dbo].[RuleDimension] (Name,Description,IsSystemDefined,UpdatedBy) values(N'Integrity',N'What data is missing important relationship linkages? The inability to link related records together may actually introduce duplication across your systems. Not only that, as more value is derived from analyzing connectivity and relationships, the inability to link related data instance together impedes this valuable analysis.',1,0)	
end
go

-- add column RuleDimensionID to [dbo].[rule]
ALTER TABLE [dbo].[Rule] ADD RuleDimensionID int NULL
GO

-- add fk so that rule dimension corresponds to an existing rule dimension
ALTER TABLE [dbo].[Rule] add constraint FK_Rule_RuleDimension FOREIGN KEY ( [RuleDimensionID] ) references [dbo].[RuleDimension] ([ID])
GO


alter procedure GetLineageDiagram
--declare 
	@type varchar(50),
	@id int

--set @type = 'Artifact'
--set @id = 11808
as
begin
	declare @tbl table	(
						IntersectID int, IntersectTypeID int, ID int, 
						SubjectNodeID int, SubjectTypeName nvarchar(1000), SourceType varchar(50), SourceTypeID int, SubjectObjectName nvarchar(1000), Subject varchar(50), SubjectID int, SubjectBackColor varchar(10), SubjectForeColor varchar(10),  
						ObjectNodeID int, ObjectTypeName nvarchar(1000), ObjectType varchar(50), ObjectTypeID int, ObjectObjectName nvarchar(1000), Object varchar(50), ObjectID int, ObjectBackColor varchar(10), ObjectForeColor varchar(10),
						PredicateID int, Predicate nvarchar(250), MappingRuleCount int, TransformationCount int
						)
    insert into @tbl
	    select	--distinct
			    R.IntersectID,
				R.IntersectTypeID,
			    M.ID,
			    M.SubjectIntersectNodeID,
			    R.SourceTypeName,
			    R.SourceType,
			    R.SourceTypeID,
			    R.SourceObjectName,
			    R.SourceObject,
			    R.SourceObjectID,
				coalesce(SD.IconBackColor, '#000') as SourceIconBackColor,
				coalesce(SD.IconForeColor, '#fff') as SourceIconForeColor,
			    M.ObjectIntersectNodeID,
			    R.TargetTypeName,
			    R.TargetType,
			    R.TargetTypeID,
			    R.TargetObjectName,
			    R.TargetObject,
			    R.TargetObjectID,
				coalesce(TD.IconBackColor, '#000') as TargetIconBackColor,
				coalesce(TD.IconForeColor, '#fff') as TargetIconForeColor,
			    M.PredicateID,
			    P.Name as Predicate,
			    0,
				0
	    from	IntersectMap M
			    inner join [cache].[Relationships] R on M.SubjectIntersectNodeID = R.SourceIntersectNodeID and M.ObjectIntersectNodeID = R.TargetintersectNodeID and M.[Type] = 1
			    left join ObjectStyle SD with(nolock) on SD.ObjectType = R.SourceType and SD.ObjectID = R.[SourceTypeID]
				left join ObjectStyle TD with(nolock) on TD.ObjectType = R.TargetType and TD.ObjectID = R.[TargetTypeID]
			    inner join Predicate P on P.ID = M.PredicateID
			    inner join [cache].[Relationship] SR on SR.SourceObject = @type and SR.SourceObjectID = @id and SR.TargetObject = R.SourceObject and SR.TargetObjectID = R.SourceObjectID
			    inner join [cache].[Relationship] TR on TR.SourceObject = @type and TR.SourceObjectID = @id and TR.TargetObject = R.TargetObject and TR.TargetObjectID = R.TargetObjectID
	    union
	    select	--distinct
			    R.IntersectID,
				R.IntersectTypeID,
			    M.ID,
			    M.SubjectIntersectNodeID,
			    R.SourceTypeName,
			    R.SourceType,
			    R.SourceTypeID,
			    R.SourceObjectName,
			    R.SourceObject,
			    R.SourceObjectID,
				coalesce(SD.IconBackColor, '#000') as SourceIconBackColor,
				coalesce(SD.IconForeColor, '#fff') as SourceIconForeColor,
			    M.ObjectIntersectNodeID,
			    R.TargetTypeName,
			    R.TargetType,
			    R.TargetTypeID,
			    R.TargetObjectName,
			    R.TargetObject,
			    R.TargetObjectID,
				coalesce(TD.IconBackColor, '#000') as TargetIconBackColor,
				coalesce(TD.IconForeColor, '#fff') as TargetIconForeColor,
			    M.PredicateID,
			    P.Name as Predicate,
			    0,
				0
	    from	IntersectMap M
			    inner join [cache].[Relationships] R on M.SubjectIntersectNodeID = R.SourceIntersectNodeID and M.ObjectIntersectNodeID = R.TargetintersectNodeID and R.SourceObject = @type and R.SourceObjectID = @id and M.[Type] = 1
			    left join ObjectStyle SD with(nolock) on SD.ObjectType = R.SourceType and SD.ObjectID = R.[SourceTypeID]
				left join ObjectStyle TD with(nolock) on TD.ObjectType = R.TargetType and TD.ObjectID = R.[TargetTypeID]
			    inner join Predicate P on P.ID = M.PredicateID
	    union
	    select	--distinct
			    R.IntersectID,
				R.IntersectTypeID,
			    M.ID,
			    M.SubjectIntersectNodeID,
			    R.SourceTypeName,
			    R.SourceType,
			    R.SourceTypeID,
			    R.SourceObjectName,
			    R.SourceObject,
			    R.SourceObjectID,
				coalesce(SD.IconBackColor, '#000') as SourceIconBackColor,
				coalesce(SD.IconForeColor, '#fff') as SourceIconForeColor,
			    M.ObjectIntersectNodeID,
			    R.TargetTypeName,
			    R.TargetType,
			    R.TargetTypeID,
			    R.TargetObjectName,
			    R.TargetObject,
			    R.TargetObjectID,
				coalesce(TD.IconBackColor, '#000') as TargetIconBackColor,
				coalesce(TD.IconForeColor, '#fff') as TargetIconForeColor,
			    M.PredicateID,
			    P.Name as Predicate,
			    0,
				0
	    from	IntersectMap M
			    inner join [cache].[Relationships] R on M.SubjectIntersectNodeID = R.SourceIntersectNodeID and M.ObjectIntersectNodeID = R.TargetintersectNodeID and R.TargetObject = @type and R.TargetObjectID = @id and M.[Type] = 1
			    left join ObjectStyle SD with(nolock) on SD.ObjectType = R.SourceType and SD.ObjectID = R.[SourceTypeID]
				left join ObjectStyle TD with(nolock) on TD.ObjectType = R.TargetType and TD.ObjectID = R.[TargetTypeID]
			    inner join Predicate P on P.ID = M.PredicateID

    update	r
    set		r.mappingrulecount = l.[Count],
			r.transformationcount = t.[Count]
    from	@tbl r
			cross apply (
							select count(1) as [Count]
							from SourceTargetRule
							where FocalObjectID = @id and FocalObject = @type and SourceObject = r.Subject and SourceObjectID = r.SubjectID and TargetObject = r.Object and TargetObjectID = r.ObjectID
						) l
			cross apply (
				            select count(1) as [Count]
				            from BusinessTransformationRule
				            where FocalObjectID = @id and FocalObject = @type and SourceObject = r.Subject and SourceObjectID = r.SubjectID and TargetObject = r.Object and TargetObjectID = r.ObjectID
			            ) t;

    declare @h table	(
					    ID int, [Type] varchar(1), IsStart bit, IsEnd bit,
					    [Level] int, NodeID int, TypeName nvarchar(1000), [ObjectType] varchar(50), ObjectTypeID int, ObjectName nvarchar(1000), O varchar(50), OID int, BackColor varchar(10), ForeColor varchar(10),
					    IntersectID int, IntersectTypeID int,  PredicateID int, Predicate nvarchar(250),
					    RawSourceRuleCount int, RawMappingRuleCount int, LinkMappingRuleCount int, ChallengeCount int, OpenEventCount int, OpenIssueCount int, RawTransformationCount int, LinkTransformationCount int
					    )

    insert into @h
	    select	ID, 'S', 0, 0, 0, 
				SubjectNodeID, 
				SubjectTypeName, SourceType, SourceTypeID, SubjectObjectName, 
				Subject, SubjectID, SubjectBackColor, SubjectForeColor, 
				IntersectID, IntersectTypeID, 
				PredicateID, Predicate, 
				R.[Count], M.[Count], S.MappingRuleCount, C.[Count], dbo.EventCountByObject(Subject, SubjectID, 'Open'), I.[Count],  T.[Count], S.TransformationCount
	    from	@tbl S
			    cross apply (
						    select	count(1) as [Count]
						    from	SourceRule
						    where	AppliesToObject = @type and AppliesToObjectID = @id and Object = S.Subject and ObjectID = S.SubjectID
						    ) R
			    cross apply (
						        select count(1) as [Count]
						        from SourceTargetRule
						        where FocalObjectID = @id and FocalObject = @type and SourceObject = S.Subject and SourceObjectID = S.SubjectID and TargetObject = S.Subject and TargetObjectID = S.SubjectID
						    ) M
				cross apply (
								select count(1) as [Count]     
								from Workflow W            			                          
								where W.WorkflowType = 4 and W.Data.exist('/fields/ArtifactID[text() = sql:column("S.SubjectID")]') = 1 and W.DateCompleted is null   
							) C
				cross apply (
								select count(1) as [Count]     
								from Workflow W            			                          
								where W.WorkflowType = 3 and W.Data.exist('/fields/ArtifactID[text() = sql:column("S.SubjectID")]') = 1 and W.DateCompleted is null   
							) I
				cross apply (
						        select count(1) as [Count]
						        from BusinessTransformationRule
						        where FocalObjectID = @id and FocalObject = @type and SourceObject = S.Subject and SourceObjectID = S.SubjectID and TargetObject = S.Subject and TargetObjectID = S.SubjectID
						    ) T

    insert into @h
        select	ID, 
				'O', 0, 0, 0, 
				ObjectNodeID, 
				ObjectTypeName, ObjectType, ObjectTypeID, ObjectObjectName, 
				Object, ObjectID, ObjectBackColor, ObjectForeColor, 
				IntersectID, IntersectTypeID, 
				PredicateID, Predicate, 
				R.[Count], M.[Count], S.MappingRuleCount, C.[Count], dbo.EventCountByObject(Object, ObjectID, 'Open'), I.[Count], T.[Count], S.TransformationCount
        from	@tbl S
                cross apply	(
                            select  count(1) as [Count]
                            from	SourceRule
                            where	AppliesToObject = @type 
									and AppliesToObjectID = @id 
									and Object = S.Object 
									and ObjectID = S.ObjectID
							) R
                cross apply	(
                            select	count(1) as [Count]
                            from	SourceTargetRule
                            where	FocalObjectID = @id 
									and FocalObject = @type 
									and SourceObject = S.Object 
									and SourceObjectID = S.ObjectID 
									and TargetObject = S.Object 
									and TargetObjectID = S.ObjectID
                            ) M
                cross apply	(
                            select	count(1) as [Count]
                            from	Workflow W
                            where	W.WorkflowType = 4 
									and W.Data.exist('/fields/ArtifactID[text() = sql:column("S.ObjectID")]') = 1 
									and W.DateCompleted is null
                            ) C
                cross apply	(
                            select	count(1) as [Count]
                            from	Workflow W
                            where	W.WorkflowType = 3 
									and W.Data.exist('/fields/ArtifactID[text() = sql:column("S.ObjectID")]') = 1 
									and W.DateCompleted is null
                            ) I
				cross apply(
                                select count(1) as [Count]
                                from BusinessTransformationRule
                                where FocalObjectID = @id and FocalObject = @type and SourceObject = S.Object and SourceObjectID = S.ObjectID and TargetObject = S.Object and TargetObjectID = S.ObjectID
                            ) T
    update  T
    set     T.[Level] = 1,
		    T.IsStart = 1
    from	@h T
            left join @h S on S.O = T.O and S.OID = T.OID and S.[Type] = 'O'
    where	T.[Type] = 'S' and S.ID is null

    update T
    set		T.IsEnd = 1
    from	@h T
            left join @h S on S.O = T.O and S.OID = T.OID and S.[Type] = 'S'
    where	T.[Type] = 'O' and S.ID is null

    select	*
	from	@h
end
go


CREATE FUNCTION EventsByObject
(
	@Type varchar(250),
	@ID int,
	@Status varchar(50) = NULL
)
RETURNS 
@tbl TABLE 
(
	EventID int,
	RuleID int,
	[Rule] nvarchar(250),
	EventName nvarchar(250),
	EventGroupID int,
	SourceID nvarchar(250),
	[Status] varchar(50),
	[Date] datetime
)
AS
BEGIN
	if @Type = 'Policy'
		begin
			with PH as	(
						select	ID,
								ParentID
						from	Policy
						where	ID = @ID
						union all
						select	C.ID,
								C.ParentID
						from	Policy C 
								inner join PH on C.ParentID = PH.ID
						)
			insert into @tbl
				SELECT	E.ID AS EventID,
						G.RuleID,
						R.Name as [Rule],
						G.Name as EventName,
						E.EventGroupID,
						E.SourceID,
						E.Status,
						E.Date
				FROM	[Event] E
						INNER JOIN EventGroup G ON E.EventGroupID = G.ID 
												AND (E.Status = @Status OR 1=1)
						INNER JOIN [Rule] R on R.ID = G.RuleID
				where	R.ID in (
								select	distinct
										CR.TargetObjectID
								from	PH
										inner join cache.Relationship CR on CR.SourceObject = 'Policy' and CR.SourceObjectID = PH.ID and CR.TargetObject = 'Rule'
								)
		end

	if @Type = 'Rule'
		begin
			insert into @tbl
				SELECT	E.ID AS EventID,
						G.RuleID,
						R.Name as [Rule],
						G.Name as EventName,
						E.EventGroupID,
						E.SourceID,
						E.Status,
						E.Date
				FROM	[Event] E
						INNER JOIN EventGroup G ON E.EventGroupID = G.ID 
												AND (E.Status = @Status OR 1=1)
						INNER JOIN [Rule] R on R.ID = G.RuleID and R.ID = @ID
		end

	if @Type = 'EventGroup'
		begin
			insert into @tbl
				SELECT	E.ID AS EventID,
						G.RuleID,
						R.Name as [Rule],
						G.Name as EventName,
						E.EventGroupID,
						E.SourceID,
						E.Status,
						E.Date
				FROM	[Event] E
						INNER JOIN EventGroup G ON E.EventGroupID = G.ID 
												AND E.EventGroupID = @ID
												AND (E.Status = @Status OR 1=1)
						INNER JOIN [Rule] R on R.ID = G.RuleID
		end

	if @Type <> 'EventGroup' and @Type <> 'Policy' 
		begin
			insert into @tbl
				SELECT	E.ID AS EventID,
						G.RuleID,
						R.Name as [Rule],
						G.Name as EventName,
						E.EventGroupID,
						E.SourceID,
						E.Status,
						E.Date
				FROM	[Event] E
						INNER JOIN EventGroup G ON E.EventGroupID = G.ID 
												and (E.Status = @Status OR 1=1)
						INNER JOIN [Rule] R on R.ID = G.RuleID
						inner join cache.Relationship CR on CR.SourceObject = @Type and CR.SourceObjectID = @ID and CR.TargetObject = 'Rule' and CR.TargetObjectID = R.ID
		end

	RETURN 
END
GO



CREATE FUNCTION EventCountByObject
(
	@Type varchar(250),
	@ID int,
	@Status varchar(50) = NULL
)
RETURNS int
AS
BEGIN
	DECLARE @count int

	if @Type = 'Policy'
	begin
		with PH as	(
					select	ID,
							ParentID
					from	Policy
					where	ID = @ID
					union all
					select	C.ID,
							C.ParentID
					from	Policy C 
							inner join PH on C.ParentID = PH.ID
					)

			SELECT	@count = count(1)
			FROM	[Event] E
					INNER JOIN EventGroup G ON E.EventGroupID = G.ID 
											AND (E.Status = @Status OR 1=1)
					INNER JOIN [Rule] R on R.ID = G.RuleID
			where	R.ID in (
							select	distinct
									CR.TargetObjectID
							from	PH
									inner join cache.Relationship CR on CR.SourceObject = 'Policy' and CR.SourceObjectID = PH.ID and CR.TargetObject = 'Rule'
							)
	end

	if @Type = 'Rule'
	begin
		SELECT	@count = count(1)
		FROM	[Event] E
				INNER JOIN EventGroup G ON E.EventGroupID = G.ID 
										AND (E.Status = @Status OR 1=1)
				INNER JOIN [Rule] R on R.ID = G.RuleID and R.ID = @ID
	end

	if @Type = 'EventGroup'
	begin
		SELECT	@count = count(1)
		FROM	[Event] E
				INNER JOIN EventGroup G ON E.EventGroupID = G.ID 
										AND E.EventGroupID = @ID
										AND (E.Status = @Status OR 1=1)
				INNER JOIN [Rule] R on R.ID = G.RuleID
	end

	if @Type <> 'EventGroup' and @Type <> 'Policy' 
	begin
		SELECT	@count = count(1)
		FROM	[Event] E
				INNER JOIN EventGroup G ON E.EventGroupID = G.ID 
										and (E.Status = @Status OR 1=1)
				INNER JOIN [Rule] R on R.ID = G.RuleID
				inner join cache.Relationship CR on CR.SourceObject = @Type and CR.SourceObjectID = @ID and CR.TargetObject = 'Rule' and CR.TargetObjectID = R.ID
	end

	RETURN @count
END
GO


--in prod only

-- DISABLE TRIGGER SO WE DONT ADD A TON OF RECORDS TO UPDATE THINGS IN THE QUEUE
ALTER TABLE [Artifact] DISABLE TRIGGER Artifact_AfterUpdate
go

-- add column CreatedOn to artifact table not nullable default to current_timestamp
alter table [Artifact] add CreatedOn datetime not null constraint DF_Artifact_CreatedOn default(CURRENT_TIMESTAMP)
go

-- update all created on to 1/1/2011 so they all dont show up as new
update [Artifact] set CreatedOn = '1/1/2011';
go

-- update all createdon dates with the updatedon date if the exist
update [Artifact] set CreatedOn = UpdatedOn where UpdatedOn is not null;
go

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
go


-- REENABLE TRIGGER AFTER UPDATES

ALTER TABLE [Artifact] ENABLE TRIGGER Artifact_AfterUpdate
go


ALTER PROCEDURE [dbo].[GetRenderedTemplateBody]-- 'Tooltip', 'Resource', 2, 'Preview'
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

		if @dateCertifiedOn is null and @status != 'Certified'
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
				set @html = @html + '<div>Last Certified On: ';
				if @dateCertifiedOn is null
					begin
						set @html = @html + 'Manually Certified';
					end
				else
					begin
						set @html = @html + '{CertifiedOn}';
					end
				set @html = @html + '</div>';
				if @status = 'Certified'
					begin
						if @Certifiers is not null
						begin
							set @html = @html + '<div>Certified By: {Certifiers}</div>'
						end
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

		if @Type = 'RuleDimension'
		begin
			insert into @tbl
				select	'Description', [Description]
				from	RuleDimension
				where	ID = @ID
			insert into @tbl
				select	'Name', [Name]
				from	RuleDimension
				where	ID = @ID

			--set @html = @html + '<div><b>Path:</b> {Description}</div>'
						
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
						outer apply (
									select	top 1
											*
									from	Statistic
									where	StatisticTypeID = T.ID
											and ObjectType = @Type
											and ObjectID = @ID
									order by DateStart desc
									) S
				where	T.[Object] = @Type + 'Type' 
						and T.ObjectID = @typeID
						and T.PartOfScore = 1

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



drop table Relation
go
drop table RelationType
go

CREATE TABLE [dbo].[RelationType] (
    [ID]                   INT          IDENTITY (1, 1) NOT NULL,
    [Subject]              VARCHAR (50) NOT NULL,
    [SubjectID]            INT          NOT NULL,
    [OldSubjectNodeTypeID] INT          NOT NULL,
    [Object]               VARCHAR (50) NOT NULL,
    [ObjectID]             INT          NOT NULL,
    [OldObjectNodeTypeID]  INT          NOT NULL,
    [PredicateType]        INT          NOT NULL,
    [OldIntersectTypeID]   INT          NOT NULL,
    [IsSystem]             BIT          NOT NULL,
    [CreatedBy]            INT          NULL,
    [CreatedOn]            DATETIME     NULL,
    [UpdatedBy]            INT          NULL,
    [UpdatedOn]            DATETIME     NULL,
    CONSTRAINT [PK_RelationType] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [UQ_RelationType] UNIQUE NONCLUSTERED ([Subject] ASC, [SubjectID] ASC, [Object] ASC, [ObjectID] ASC, [PredicateType] ASC)
);
GO

CREATE TABLE [dbo].[Relation] (
    [ID]             INT          IDENTITY (1, 1) NOT NULL,
    [Subject]        VARCHAR (50) NOT NULL,
    [SubjectID]      INT          NOT NULL,
    [Object]         VARCHAR (50) NOT NULL,
    [ObjectID]       INT          NOT NULL,
    [RelationTypeID] INT          NOT NULL,
    [PredicateID]    INT          NULL,
    [Deleted]        BIT          NOT NULL,
    [CreatedBy]      INT          NULL,
    [CreatedOn]      DATETIME     NULL,
    [UpdatedBy]      INT          NULL,
    [UpdatedOn]      DATETIME     NULL,
    [OldIntersectID] INT          NOT NULL,
    CONSTRAINT [PK_Relation] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_Relation_Predicate] FOREIGN KEY ([PredicateID]) REFERENCES [dbo].[Predicate] ([ID]) ON DELETE CASCADE,
    CONSTRAINT [FK_Relation_RelationType] FOREIGN KEY ([RelationTypeID]) REFERENCES [dbo].[RelationType] ([ID]) ON DELETE CASCADE
);
GO

alter procedure [cache].[ReSynchronizeAllObjectDetails]
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
		INSERT INTO #Recache VALUES ('RuleType', 1, 'RuleType', 1)
		INSERT INTO #Recache VALUES ('RuleType', 2, 'RuleType', 2)
		INSERT INTO #Recache VALUES ('RuleType', 3, 'RuleType', 3)
		INSERT INTO #Recache VALUES ('RuleType', 4, 'RuleType', 4)

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
GO

ALTER FUNCTION [utility].[GetDirectlyAssignedResponsibilityList]
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

	if @Object = 'Artifact'
		begin
			insert into @tbl
				select	'Artifact Direct' as [Source],
						R.Visible,
						R.ID,
						R.ResponsibilityTypeID,
						@Object as AssigningItemType,
						T.ID as AssigningItemID,
						@Object as ObjectType,
						T.ID as ObjectID,
						utility.GetResponsibilityContextHash(R.ID),
						@Priority as [Priority]
				from	Artifact T 
						inner join Responsibility R on R.ObjectType = @Object and R.ObjectID = T.ID 
							and (
									(T.ID = @ObjectID and @ObjectID is not null)
									or (@ObjectID is null)
								);
		end
	if @Object = 'ArtifactType'
		begin
			insert into @tbl
				select	'Artifact Type Direct' as [Source],
						R.Visible,
						R.ID,
						R.ResponsibilityTypeID,
						@Object as AssigningItemType,
						T.ID as AssigningItemID,
						@Object as ObjectType,
						T.ID as ObjectID,
						utility.GetResponsibilityContextHash(R.ID),
						@Priority as [Priority]
				from	ArtifactType T 
						inner join Responsibility R on R.ObjectType = @Object and R.ObjectID = T.ID 
							and (
									(T.ID = @ObjectID and @ObjectID is not null)
									or (@ObjectID is null)
								);
		end
	if @Object = 'Domain'
		begin
			insert into @tbl
				select	'Domain Direct' as [Source],
						R.Visible,
						R.ID,
						R.ResponsibilityTypeID,
						@Object as AssigningItemType,
						T.ID as AssigningItemID,
						@Object as ObjectType,
						T.ID as ObjectID,
						utility.GetResponsibilityContextHash(R.ID),
						@Priority as [Priority]
				from	Domain T 
						inner join Responsibility R on R.ObjectType = @Object and R.ObjectID = T.ID
							and (
									(T.ID = @ObjectID and @ObjectID is not null)
									or (@ObjectID is null)
								);
		end
	if @Object = 'DomainType'
		begin
			insert into @tbl
				select	'Domain Type Direct' as [Source],
						R.Visible,
						R.ID,
						R.ResponsibilityTypeID,
						@Object as AssigningItemType,
						T.ID as AssigningItemID,
						@Object as ObjectType,
						T.ID as ObjectID,
						utility.GetResponsibilityContextHash(R.ID),
						@Priority as [Priority]
				from	DomainType T 
						inner join Responsibility R on R.ObjectType = @Object and R.ObjectID = T.ID
							and (
									(T.ID = @ObjectID and @ObjectID is not null)
									or (@ObjectID is null)
								);
		end
	if @Object = 'Fusion'
		begin
			insert into @tbl
				select	'Fusion Direct' as [Source],
						R.Visible,
						R.ID,
						R.ResponsibilityTypeID,
						@Object as AssigningItemType,
						T.ID as AssigningItemID,
						@Object as ObjectType,
						T.ID as ObjectID,
						utility.GetResponsibilityContextHash(R.ID),
						@Priority as [Priority]
				from	Fusion T 
						inner join Responsibility R on R.ObjectType = @Object and R.ObjectID = T.ID
							and (
									(T.ID = @ObjectID and @ObjectID is not null)
									or (@ObjectID is null)
								);
		end
	if @Object = 'FusionType'
		begin
			insert into @tbl
				select	'Fusion Type Direct' as [Source],
						R.Visible,
						R.ID,
						R.ResponsibilityTypeID,
						@Object as AssigningItemType,
						T.ID as AssigningItemID,
						@Object as ObjectType,
						T.ID as ObjectID,
						utility.GetResponsibilityContextHash(R.ID),
						@Priority as [Priority]
				from	FusionType T 
						inner join Responsibility R on R.ObjectType = @Object and R.ObjectID = T.ID
							and (
									(T.ID = @ObjectID and @ObjectID is not null)
									or (@ObjectID is null)
								);
		end
	if @Object = 'Rule'
		begin
			insert into @tbl
				select	'Rule Direct' as [Source],
						R.Visible,
						R.ID,
						R.ResponsibilityTypeID,
						@Object as AssigningItemType,
						RU.ID as AssigningItemID,
						@Object as ObjectType,
						RU.ID as ObjectID,
						utility.GetResponsibilityContextHash(R.ID),
						@Priority as [Priority]
				from	[Rule] RU 
						inner join Responsibility R on R.ObjectType = @Object and R.ObjectID = RU.ID
							and (
								(RU.ID = @ObjectID and @ObjectID is not null) or (@ObjectID is null)
								);
		end
	if @Object = 'RuleType'
		begin
			insert into @tbl
				select	'Rule Type Direct' as [Source],
						R.Visible,
						R.ID,
						R.ResponsibilityTypeID,
						@Object as AssigningItemType,
						@ObjectID as AssigningItemID,
						@Object as ObjectType,
						@ObjectID as ObjectID,
						utility.GetResponsibilityContextHash(R.ID),
						@Priority as [Priority]
				from	Responsibility R where R.ObjectType = @Object and R.ObjectID = @ObjectID;				
		end
	if @Object = 'Taxonomy'
		begin
			insert into @tbl
				select	'Taxonomy Direct' as [Source],
						R.Visible,
						R.ID,
						R.ResponsibilityTypeID,
						@Object as AssigningItemType,
						T.ID as AssigningItemID,
						@Object as ObjectType,
						T.ID as ObjectID,
						utility.GetResponsibilityContextHash(R.ID),
						@Priority as [Priority]
				from	Taxonomy T 
						inner join Responsibility R on R.ObjectType = @Object and R.ObjectID = T.ID
							and (
								(T.ID = @ObjectID and @ObjectID is not null) or (@ObjectID is null)
								)
		end
	if @Object = 'TaxonomyType'
		begin
			insert into @tbl
				select	'Taxonomy Type Direct' as [Source],
						R.Visible,
						R.ID,
						R.ResponsibilityTypeID,
						@Object as AssigningItemType,
						T.ID as AssigningItemID,
						@Object as ObjectType,
						T.ID as ObjectID,
						utility.GetResponsibilityContextHash(R.ID),
						@Priority as [Priority]
				from	TaxonomyType T 
						inner join Responsibility R on R.ObjectType = @Object and R.ObjectID = T.ID
							and (
								(T.ID = @ObjectID and @ObjectID is not null) or (@ObjectID is null)
								)
		end
	RETURN 
END
GO


alter table cache.Object drop column Name
alter table cache.Object drop column TextPath
alter table cache.Object drop column Description
alter table cache.Object drop column Parent
alter table cache.Object drop column ParentID
alter table cache.Object drop column ParentName
alter table cache.Object drop column Url
alter table cache.Object drop column ObjectTypeName
alter table cache.Object drop column IconBackColor
alter table cache.Object drop column IconForeColor
alter table cache.Object drop column IconText
go

ALTER TABLE FieldTypeFusionLookupDefinition DROP CONSTRAINT [FK_FieldTypeFusionLookupDefinition_FieldType] 
go

ALTER TABLE [dbo].[IntersectTypePredicate] ADD  CONSTRAINT [DF_IntersectTypePredicate_PredicateID]  DEFAULT ((1)) FOR [PredicateID]
GO

ALTER TABLE IntersectTypePredicate drop constraint [FK_IntersectTypePredicate_Predicate]
go

alter table SourceRule drop constraint DF_SourceRule_IsTemplate
go
alter table SourceRule drop column [IsTmplate]
go

alter view [dbo].[Relationship]
as
	select	I.IntersectTypeID,
			R.IntersectID,
			case I.Classification
				when 0 then 2
				else I.Classification
			end as Classification,
			I.Description,
			substring((
						select	', ' + P.Name as [text()]
						from	IntersectMap IM
								inner join [Predicate] P on	P.ID = IM.PredicateID	
															and (
																(IM.SubjectIntersectNodeID = R.[SourceIntersectNodeID] and IM.ObjectIntersectNodeID = R.[TargetIntersectNodeID]) or
																(IM.SubjectIntersectNodeID = R.[TargetIntersectNodeID] and IM.ObjectIntersectNodeID = R.[SourceIntersectNodeID])
																)
						for xml path('')
						), 3, 1000) as [Role],
			--R.[Role],
			R.SourceIntersectTypeNodeID,
			R.SourceObject as SourceObjectType,
			R.SourceObjectID,
			coalesce(S.TextPath, S.Name) as SourceName, 
			S.Parent as SourceParent,
			S.ParentID as SourceParentID,
			S.ParentName as SourceParentName,
			S.ObjectTypeID as SourceTypeID,
			S.ObjectType as SourceType,
			S.ObjectTypeName as SourceTypeName,
			S.[Url] as SourceUrl,
			R.TargetIntersectTypeNodeID,
			T.Object as TargetObjectType,
			T.ObjectID as TargetObjectID,
			coalesce(T.TextPath, T.Name) as TargetName,
			T.Parent as TargetParent,
			T.ParentID as TargetParentID,
			T.ParentName as TargetParentName,
			T.ObjectTypeID as TargetTypeID,
			T.ObjectType as TargetType,
			T.ObjectTypeName as TargetTypeName,
			T.[Url] as TargetUrl,
			TR.[Exists] as HasTechnicalRelationships
	from	cache.Relationship R
			inner join [Intersect] I on I.ID = R.IntersectID
			left join [cache].[ObjectDetails] S on S.[Object] = R.SourceObject and S.ObjectID = R.SourceObjectID
			left join [cache].[ObjectDetails] T on T.[Object] = R.TargetObject and T.ObjectID = R.TargetObjectID
			cross apply (
						select	case 
									when count(1) > 0 then cast(1 as bit) 
									else cast(0 as bit) 
								end as [Exists]
						from	cache.Relationships
						where	SourceObject = 'Intersect' and SourceObjectID = R.IntersectID
						) TR
GO

alter procedure [tile].[GetObjectStatistics]
	@type varchar(50),
	@id int
AS
BEGIN
declare @table table (Name nvarchar(250), Value varchar(250), [Group] varchar(25), Url varchar(250))
	
	insert into @table
		select NULL, count(1), 'Followers', '/overlays/' + @type + '/' + cast(@id as varchar(10)) + '/followers'
		from	Follow F
		inner join reporting.Global_Resource R on R.ResourceID = F.ResourceID
		where	F.ObjectType = @type and F.ObjectID = @id
	
	insert into @table
		select	NULL, count(1), 'Comments', '/overlays/' + @type + '/' + cast(@id as varchar(10)) + '/comments'
		from	Comment C
				inner join CommentRelation R	on R.CommentID = C.ID and C.ParentID is null
												and R.ObjectType = @type and R.ObjectID = @id
                                                and C.ParentID is null
												and C.IsDeleted = 0
	insert into @table
		select NULL, count(1), 'Events', '/overlays/' + @type + '/' + cast(@id as varchar(10)) + '/events'
			FROM	    [Event] E
					    INNER JOIN EventGroup G ON E.EventGroupID = G.ID and E.Status in ('Active', 'Open')
					    INNER JOIN [Rule] R on R.ID = G.RuleID
					    inner join cache.Relationships CR on CR.SourceObject = @type and CR.SourceObjectID = @id and CR.TargetObject = 'Rule' and CR.TargetObjectID = R.ID

	insert into @table values (null, dbo.[GetObjectStatisticScore](@type, @id) * 100, 'Score', '/overlays/' + @type + '/' + cast(@id as varchar(10)) + '/score')

	if @type = 'Artifact'
	begin
		insert into @table 
			select		lower(T.Name),
						count(1),
						'Children',
						'/overlays/' + cast(@id as varchar(10)) + '/' + cast(T.ID as varchar(10)) + '/ChildArtifacts'
			from		Artifact A
						inner join ArtifactType T on T.ID = A.ArtifactTypeID and A.ParentID = @id
			group by	T.Name,
						T.ID
			order by	T.Name


		insert into @table
			select	
				'Issue',
				count(1),
				'Issues',
				'/overlays/Artifact/' + cast(@id as varchar(10)) + '/Issues'
			from	
					workflow w
					inner join Comment C on C.ID = w.data.value('(fields/CommentID)[1]', 'int')
					inner join CommentRelation CR on CR.CommentID = C.ID and CR.ObjectType = 'Artifact'
					inner join Artifact A on w.workflowtype = 3 and w.datecompleted is null and A.ID = cr.objectid
			where 
				a.id = @id			
	end


	select * from @table

END
GO

ALTER FUNCTION [utility].[GetFormattedFieldAttributeValue]
(
--declare
	@AttributeID int,-- = 67,
	@DisplayFormat nvarchar(250)-- = '{Position1}, {Position2}, {Position3}, {Position4}'
)
RETURNS nvarchar(4000)
AS
BEGIN
	declare @formattedValue nvarchar(4000)
	declare @tokens table(ID int identity(1,1), Token nvarchar(100), Field nvarchar(100))
	declare @fieldValues table(Field nvarchar(100), Value nvarchar(4000))

	set @formattedValue = @DisplayFormat
	while patindex('%{%',@formattedValue) > 0
	begin
		declare @txt nvarchar(100) = SUBSTRING(@formattedValue, patindex('%{%',@formattedValue), PATINDEX('%}%', @formattedValue))
		insert into @tokens Values (@txt, REPLACE(REPLACE(@txt,'{',''),'}',''))
		set @formattedValue = replace(@formattedValue, @txt, '')
	end

	insert into @fieldValues
		SELECT	Name,
				FormattedValue
		FROM	FieldWithRelation 
		WHERE	ObjectType = 'Attribute' 
				and ObjectID = @AttributeID

	declare @current int,
			@max int

	set @current = 1
	select @max = Max(ID) from @tokens
	set @formattedValue = @DisplayFormat

	while(@current <= @max)
	begin
		declare @currentToken nvarchar(100) = null,
				@currentField nvarchar(100) = null,
				@currentValue nvarchar(4000) = null,
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


ALTER FUNCTION [utility].[GetFormattedFieldLookupValue]
(
	@Type varchar(25),
	@DisplayFormat nvarchar(250),
	@LookupObjectType varchar(25),
	@LookupObjectID int,
	@Value nvarchar(4000)
)
RETURNS nvarchar(4000)
AS
BEGIN
	declare @formattedValue nvarchar(4000)

	if @LookupObjectType is null
	begin
		set @formattedValue  = @Value

		if @Type = 'Link' OR @Type = 'UncLink'
		begin
			declare @linkName nvarchar(4000),
					@linkUrl nvarchar(4000)

			if charindex('|', @Value, 1) > 0
				begin
					SELECT @linkName = SUBSTRING(@Value, 1, PATINDEX('%|%', @Value)-1)
					SELECT @linkUrl = SUBSTRING(@Value, PATINDEX('%|%', @Value)+1, LEN(@Value))

					set @formattedValue = '<a href="' + @linkUrl + '" target="_blank">' + @linkName + '</a>'
				end
			else
				begin
					if @Value <> '' AND @Value IS NOT NULL
						begin
							set @formattedValue = '<a href="' + @Value + '" target="_blank">' + @Value + '</a>'
						end
					else
						begin
							set @formattedValue = ''
						end
				end
		end

	end
	else
	begin
		declare @tokens table(ID int identity(1,1), Token nvarchar(100), Field nvarchar(100))
		declare @fieldValues table(Field nvarchar(100), Value nvarchar(4000), LookupObjectType nvarchar(250), LookupObjectID int, LookupDisplayFormat nvarchar(250))

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
							'Domain' as ObjectType
					FROM	DomainType
					WHERE	@LookupObjectType = 'Domain' and ID = @LookupObjectID
					UNION
					SELECT	ID,
							Name,
							'DomainItem' as ObjectType
					FROM	Domain
					WHERE	@LookupObjectType = 'DomainItem' and ID = @LookupObjectID
					UNION
					SELECT	ID,
							Name,
							'Lookup' as ObjectType
					FROM	[LookupType]
					WHERE	@LookupObjectType = 'Lookup' and ID = @LookupObjectID
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
																and [IF].ObjectID = case 
																						when dbo.IsInteger(@Value) = 1 then @Value
																						else 0
																					end
								
								UNION

								SELECT	P.FieldName as Name,
										p.FieldValue as Value,
										NULL as LookupObjectType,
										NULL as LookupObjectID,
										NULL as LookupDisplayFormat
								FROM	(
										SELECT	ID,
												CAST(Name as nvarchar(4000)) as Name,
												Description,
												CAST(TextPath as nvarchar(4000)) as TextPath
										FROM	Artifact A
										WHERE	A.ID = CAST(@Value as int)
												and L.ObjectType = 'Artifact'
										) A
										unpivot	(
												FieldValue for FieldName in (Name, Description, TextPath)
												) p

								UNION

								SELECT	P.FieldName as Name,
										p.FieldValue as Value,
										NULL as LookupObjectType,
										NULL as LookupObjectID,
										NULL as LookupDisplayFormat
								FROM	(
										SELECT	ID,
												CAST(Name as nvarchar(4000)) as Name,
												Description,
												CAST(TextPath as nvarchar(4000)) as TextPath
										FROM	Taxonomy A
										WHERE	A.ID = CAST(@Value as int)
												and L.ObjectType = 'Taxonomy'
										) A
										unpivot	(
												FieldValue for FieldName in (Name, Description, TextPath)
												) p

								UNION

								SELECT	P.FieldName as Name,
										p.FieldValue as Value,
										NULL as LookupObjectType,
										NULL as LookupObjectID,
										NULL as LookupDisplayFormat
								FROM	(
										SELECT	ID,
												CAST(Name as nvarchar(4000)) as Name,
												Description
										FROM	Domain A
										WHERE	A.ID = @Value
												and L.ObjectType = 'Domain'
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
										SELECT	ID,
												CAST(Code as nvarchar(4000)) as Code,
												CAST(Name as nvarchar(4000)) as Name,
												Description
										FROM	DomainItem A
										WHERE	A.ID = @Value
												and L.ObjectType = 'DomainItem'
										) A
										unpivot	(
												FieldValue for FieldName in (Code, Name, Description)
												) p

								UNION

								SELECT	P.FieldName as Name,
										p.FieldValue as Value,
										NULL as LookupObjectType,
										NULL as LookupObjectID,
										NULL as LookupDisplayFormat
								FROM	(
										SELECT	ResourceID as ID,
												CAST(FirstName as nvarchar(4000)) as FirstName,
												CAST(LastName as nvarchar(4000)) as LastName,
												CAST(Email as nvarchar(4000)) as Email
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
					@currentValue nvarchar(4000) = null,
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
					select @currentValue = utility.GetFormattedFieldLookupValue(@Type, @lkpFormat, @lkpType, @lkpID, @currentValue)
				end

				SET @formattedValue = REPLACE(@formattedValue, @currentToken, @currentValue)
			end

			SET @current = @current + 1
		end
	end

	return @formattedValue
END
GO

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
GO

alter table IntersectTypePredicate add [IntersectTypePredicate] int NULL
go

ALTER TRIGGER [dbo].[Field_AfterUpsert]
	ON [dbo].[Field]
	FOR INSERT, UPDATE
AS
	SET NOCOUNT ON;

	
	UPDATE	T
	SET		T.FormattedValue = utility.GetFormattedFieldLookupValue(FT.Type, FT.LookupDisplayFormat, FT.LookupObjectType, FT.LookupObjectID, F.Value)
	FROM	Field T 
			inner join inserted F on F.FieldTypeID = T.FieldTypeID and F.ObjectType = T.ObjectType and F.ObjectID = T.ObjectID
			INNER JOIN FieldType FT ON FT.ID = T.FieldTypeID


	UPDATE	TF
	SET		TF.FormattedValue = utility.GetFormattedFieldLookupValue(FT.Type, FT.LookupDisplayFormat, FT.LookupObjectType, FT.LookupObjectID, TF.Value)
	from	Field TF
			inner join FieldType FT on FT.ID = TF.FieldTypeID
			inner join	inserted SF on FT.LookupObjectType = SF.ObjectType and TF.Value = cast(SF.ObjectID as varchar(25))
GO

ALTER PROCEDURE [fusion].[ProcessEagleMCToBloombergRelations]	
	@StagingFileID int,
	@FusionID int
AS
BEGIN	
	SET NOCOUNT ON;
	
	
	declare		@eagleStreamID int;				
	declare @IntersectCount int;
	Declare @IDList Table(IntersectID int,StageID Int);
	declare @Intersects IDTable;
	declare		@fieldToBBIntersectTypeID int,
				@fieldSourceIntersectTypeNodeID int,
				@fieldTargetIntersectTypeNodeID int

	-- load the panel that we want to add relations ships for
    
	select @eagleStreamID = fusionattributeid from [fusion].[stagingfile] where id = @StagingFileID and fusionID = @FusionID
	
	if @eagleStreamID is null
	begin
		raiserror('ERROR : UNABLE TO LOCATE SPECIFIED STREAM INFORMATION FOR INPUT FUSION ID / STAGING ID', 15, 1);
		return;
	end;
			
	exec ProcessEagleMCToEagleFieldRelations @StagingFileID, @FusionID

	exec [fusion].[ProcessEagleMCToBBMnemonic] @StagingFileID, @FusionID


	-- add relations for Eagle Field (205) to Bloomberg mnemonic (301)
	if @eagleStreamID is not null
	begin
		Declare @BBToFieldList Table(FieldFusionAttributeID int, StreamFusionAttributeID int,IntersectTypeID int, ID int);
		
		-- load the intersect id's for message stream to bb mnemonic
		select	@fieldToBBIntersectTypeID = IntersectTypeID,
				@fieldSourceIntersectTypeNodeID = SourceIntersectTypeNodeID,
				@fieldTargetIntersectTypeNodeID = TargetIntersectTypeNodeID
		 from	utility.RelationshipTypes
				where	SourceObjectType = 'FusionAttributeType' and SourceObjectID = 301					
					and TargetObjectType = 'FusionAttributeType' and TargetObjectID = 205;

		if @fieldToBBIntersectTypeID is null or @fieldSourceIntersectTypeNodeID is null or @fieldTargetIntersectTypeNodeID is null
		begin
			raiserror('ERROR : UNABLE TO LOCATE INTERSECT TYPE IDS FOR EAGLE TO BLOOMBERG INTERSECT', 15, 1);
			return;
		end;

		-- load into memory the id's that we need to add intersects for
		insert into @BBToFieldList
			select fa.id as 'fieldID', faBB.id as 'bbID', @fieldToBBIntersectTypeID, ROW_NUMBER() OVER (Order by sfi.id) AS 'RowNumber'
					from 
						field f 
						inner join fusionAttribute fa on (f.ObjectID = fa.ID)
						inner join fieldtype ft on (f.fieldtypeid = ft.id)
						inner join [fusion].[StagingFileItem] sfi on (sfi.tag = f.value)				
						inner join [fusion].[StagingFile] sf on (sfi.stagingfileid = sf.id)						
						inner join fusionAttribute faBB on (faBB.Name = sfi.value and faBB.fusionattributetypeid = 301)						
						left join (select srcINode.ObjectID as SourceObjectID,
								   tgtINode.ObjectID as TargetObjectID,
								   1 as hasExisting
							from 
								[dbo].[intersect] isect inner join intersectnode srcINode on (isect.intersecttypeid = @fieldToBBIntersectTypeID and isect.id = srcINode.IntersectID and srcINode.IntersectTypeNodeID = @fieldSourceIntersectTypeNodeID)
								inner join intersectnode tgtINode on(isect.intersecttypeid = @fieldToBBIntersectTypeID and isect.id = tgtINode.IntersectID and tgtINode.IntersectTypeNodeID = @fieldTargetIntersectTypeNodeID)) existing
								on existing.SourceObjectID = faBB.ID and existing.TargetObjectID = fa.id
					where fa.fusionattributetypeid = 205 and ft.name = 'startag'  and sfi.stagingfileid = @StagingFileID and existing.hasExisting is null;


		--insert intersect records and save there id's
			-- trick is to use merge to keep the sequence id and staging row ids
			-- http://stackoverflow.com/questions/15614261/using-output-clause-to-insert-value-not-in-inserted
			MERGE
				INTO    [Intersect] d
				USING   (
							SELECT sr.IntersectTypeID isectid , 2 as class,sr.ID as srID
							FROM @BBToFieldList sr							
						) s
				ON      (1 = 0)
				WHEN NOT MATCHED THEN
				INSERT  (IntersectTypeID, Classification, Description)
				VALUES  (isectid, class, NULL)
				OUTPUT  INSERTED.ID, s.srID into @IDList;

			--insert start records into intersect node
			INSERT INTO IntersectNode	(IntersectTypeNodeID, IntersectID, ObjectType, ObjectID)
					select @fieldSourceIntersectTypeNodeID, il.IntersectID, 'FusionAttribute',sr.StreamFusionAttributeID from @BBToFieldList sr inner join @IDList il on (sr.ID = il.StageID);
						

			--insert end records into intersect node
			INSERT INTO IntersectNode	(IntersectTypeNodeID, IntersectID, ObjectType, ObjectID)
					select @fieldTargetIntersectTypeNodeID, il.IntersectID, 'FusionAttribute',sr.FieldFusionAttributeID from @BBToFieldList sr inner join @IDList il on (sr.ID = il.StageID);
					
	
										
			insert into @Intersects select idl.intersectid from @IDList idl;
						
			select @IntersectCount = count(1) from @Intersects
			if @IntersectCount > 0 
			begin
				EXEC cache.SynchronizeRelationships @Intersects
			end

	end;
END
GO

alter procedure [utility].[GetOwnersForWorkflow]
--declare 
	@workflowID uniqueidentifier
--set @workflowID = '387A8094-565E-45AF-B049-01329EEF2209' --=> wt 1
--set @workflowID = '0C573C9B-D237-4468-8822-7D515750675B'--'CEE2AF0D-DAB8-432B-AF08-00E52B808C52' --=> wt 2
--set @workflowID = 'FD3C4A3D-C9BB-477A-B5CD-BC99C62AF53F' --=> wt 3
as
begin
	declare @workflowType int,
			@fields xml
	declare @tbl table (ID int, FirstName nvarchar(250), LastName nvarchar(250), Email nvarchar(500), Username nvarchar(500), DateLastLoggedIn datetime null, ResourceTypeID int, Status nvarchar(25))

	select	@workflowType = WorkflowType,
			@fields = Data
	from	Workflow
	where	ID = @workflowID

	if @workflowType = 1
	begin
		--1. Check for vocabulary owners
		insert into @tbl
			select	R.ResourceID, R.FirstName, R.LastName, R.Email, R.Email, R.DateLastLoggedIn, 1 as ResourceTypeID, R.Status 
			from	ResponsibilityDetail RD 
					inner join WorkflowTypeRelation WTR on WTR.Parent = 'TaxonomyType' and WTR.ParentID = @fields.value('(/fields/TaxonomyTypeID)[1]', 'int') and WTR.WorkflowType = @workflowType and WTR.ResponsibilityTypeID = RD.ResponsibilityTypeID
					inner join reporting.Global_Resource R 
						on RD.ObjectType = 'TaxonomyType' 
						and RD.ObjectID = @fields.value('(/fields/TaxonomyTypeID)[1]', 'int')
						and	(
								(RD.ResponsibleObjectType = 'Group' and R.ResourceID = RD.PrimaryOwnerResourceID) or 
								(RD.ResponsibleObjectType = 'Resource' and R.ResourceID = RD.ResponsibleObjectID)
							)
						and R.Email not like '%?subject=%' and R.Status = 'Active'

		if not exists(select * from @tbl)
		begin
			insert into @tbl
				select	R.ResourceID, R.FirstName, R.LastName, R.Email, R.Email, R.DateLastLoggedIn, 1 as ResourceTypeID, R.Status 
				from	ResponsibilityDetail RD 
						inner join WorkflowTypeRelation WTR on WTR.[Object] = 'ArtifactType' and WTR.ObjectID = @fields.value('(/fields/ArtifactTypeID)[1]', 'int') and WTR.Parent is null and WTR.WorkflowType = @workflowType and WTR.ResponsibilityTypeID = RD.ResponsibilityTypeID
						inner join reporting.Global_Resource R 
							on RD.ObjectType = 'ArtifactType' 
							and RD.ObjectID = @fields.value('(/fields/ArtifactTypeID)[1]', 'int')
							and (
									(RD.ResponsibleObjectType = 'Group' and R.ResourceID = RD.PrimaryOwnerResourceID) or 
									(RD.ResponsibleObjectType = 'Resource' and R.ResourceID = RD.ResponsibleObjectID)
								)
							and R.Email not like '%?subject=%' and R.Status = 'Active'
		end
	end

	if @workflowType = 2
	begin
		insert into @tbl
			select	R.ResourceID, R.FirstName, R.LastName, R.Email, R.Email, R.DateLastLoggedIn, 1 as ResourceTypeID, R.Status 
			from	ResponsibilityDetail RD 
					inner join Artifact A on RD.ObjectType = 'Artifact' and RD.ObjectID = A.ID and A.ID = @fields.value('(/fields/ArtifactID)[1]', 'int')
					inner join WorkflowTypeRelation WTR		on WTR.[Object] = 'ArtifactType' and WTR.ObjectID = A.ArtifactTypeID 
															and WTR.Parent = 'TaxonomyType' and WTR.ParentID = A.TaxonomyTypeID
															and WTR.WorkflowType = @workflowType 
															and WTR.ResponsibilityTypeID = RD.ResponsibilityTypeID
					inner join reporting.Global_Resource R 
						on	(
								(RD.ResponsibleObjectType = 'Group' and R.ResourceID = RD.PrimaryOwnerResourceID) or 
								(RD.ResponsibleObjectType = 'Resource' and R.ResourceID = RD.ResponsibleObjectID)
							)
						and R.Email not like '%?subject=%' and R.Status = 'Active' 

		if not exists(select * from @tbl)
		begin
			insert into @tbl
				select	R.ResourceID, R.FirstName, R.LastName, R.Email, R.Email, R.DateLastLoggedIn, 1 as ResourceTypeID, R.Status 
				from	ResponsibilityDetail RD 
						inner join Artifact A on RD.ObjectType = 'Artifact' and RD.ObjectID = A.ID and A.ID = @fields.value('(/fields/ArtifactID)[1]', 'int')
						inner join WorkflowTypeRelation WTR		on WTR.[Object] = 'ArtifactType' and WTR.ObjectID = A.ArtifactTypeID 
																and WTR.WorkflowType = @workflowType 
																and WTR.ResponsibilityTypeID = RD.ResponsibilityTypeID
						inner join reporting.Global_Resource R 
							on	(
									(RD.ResponsibleObjectType = 'Group' and R.ResourceID = RD.PrimaryOwnerResourceID) or 
									(RD.ResponsibleObjectType = 'Resource' and R.ResourceID = RD.ResponsibleObjectID)
								)
							and R.Email not like '%?subject=%' and R.Status = 'Active' 
		end
	end

	if @workflowType = 3
	begin

		insert into @tbl
			select	distinct
						R.ResourceID, R.FirstName, R.LastName, R.Email, R.Email, R.DateLastLoggedIn, 1 as ResourceTypeID, R.Status 
				from	Comment C
						inner join CommentRelation CR on CR.CommentID = C.ID and C.ID = @fields.value('(fields/CommentID)[1]', 'int') and CR.ObjectType not in ('Resource', 'Group')
						inner join ResponsibilityDetail RD on RD.ObjectType = CR.ObjectType and RD.ObjectID = CR.ObjectID 
						inner join WorkflowTypeRelation WTR		on WTR.[Object] = RD.ObjectType +'Type' and WTR.ObjectID = RD.ObjectID 
																and WTR.WorkflowType = @workflowType 
																and WTR.ResponsibilityTypeID = RD.ResponsibilityTypeID
																and WTR.[Enabled] = 1
						inner join reporting.Global_Resource R 
							on	(
									(RD.ResponsibleObjectType = 'Group' and R.ResourceID = RD.PrimaryOwnerResourceID) or 
									(RD.ResponsibleObjectType = 'Resource' and R.ResourceID = RD.ResponsibleObjectID)
								) 
								and R.Email not like '%?subject=%' and R.Status = 'Active'

		if not exists (select 1 from @tbl)
		begin
			insert into @tbl
				select	distinct
						R.ResourceID, R.FirstName, R.LastName, R.Email, R.Email, R.DateLastLoggedIn, 1 as ResourceTypeID, R.Status 
				from	Comment C
						inner join CommentRelation CR on CR.CommentID = C.ID and C.ID = @fields.value('(fields/CommentID)[1]', 'int') and CR.ObjectType not in ('Resource', 'Group')
						inner join ResponsibilityDetail RD on RD.ObjectType = CR.ObjectType and RD.ObjectID = CR.ObjectID 
						inner join reporting.Global_Resource R 
							on	(
									(RD.ResponsibleObjectType = 'Group' and R.ResourceID = RD.PrimaryOwnerResourceID) or 
									(RD.ResponsibleObjectType = 'Resource' and R.ResourceID = RD.ResponsibleObjectID)
								) 
								and R.Email not like '%?subject=%' and R.Status = 'Active'
		end

		if not exists (select 1 from @tbl)
		begin
			insert into @tbl
				select 
					R.ResourceID, R.FirstName, R.LastName, R.Email, R.Email, R.DateLastLoggedIn, 1 as ResourceTypeID, R.Status 
				from 
					reporting.Global_Resource R where isadministrator = 1 and status = 'Active'
		end
	end

	if @workflowType = 4
	begin
		insert into @tbl
				select	R.ResourceID, R.FirstName, R.LastName, R.Email, R.Email, R.DateLastLoggedIn, 1 as ResourceTypeID, R.Status 
				from	ResponsibilityDetail RD 
						inner join Artifact A on RD.ObjectType = 'Artifact' and RD.ObjectID = A.ID and A.ID = @fields.value('(/fields/ArtifactID)[1]', 'int')
						inner join WorkflowTypeRelation WTR		on WTR.[Object] = 'ArtifactType' and WTR.ObjectID = A.ArtifactTypeID 
																and WTR.WorkflowType = @workflowType 
																and WTR.ResponsibilityTypeID = RD.ResponsibilityTypeID
																and WTR.[Enabled] = 1
						inner join reporting.Global_Resource R 
							on	(
									(RD.ResponsibleObjectType = 'Group' and R.ResourceID = RD.PrimaryOwnerResourceID) or 
									(RD.ResponsibleObjectType = 'Resource' and R.ResourceID = RD.ResponsibleObjectID)
								)
							and R.Email not like '%?subject=%' and R.Status = 'Active' 

		if not exists (select 1 from @tbl)
		begin
			insert into @tbl
				select	distinct
						R.ResourceID, R.FirstName, R.LastName, R.Email, R.Email, R.DateLastLoggedIn, 1 as ResourceTypeID, R.Status 
				from	Comment C
						inner join CommentRelation CR on CR.CommentID = C.ID and C.ID = @fields.value('(fields/CommentID)[1]', 'int') and CR.ObjectType not in ('Resource', 'Group')
						inner join ResponsibilityDetail RD on RD.ObjectType = CR.ObjectType and RD.ObjectID = CR.ObjectID 
						inner join reporting.Global_Resource R 
							on	(
									(RD.ResponsibleObjectType = 'Group' and R.ResourceID = RD.PrimaryOwnerResourceID) or 
									(RD.ResponsibleObjectType = 'Resource' and R.ResourceID = RD.ResponsibleObjectID)
								) 
								and R.Email not like '%?subject=%' and R.Status = 'Active'
		end

		if not exists (select 1 from @tbl)
		begin
			insert into @tbl
				select 
					R.ResourceID, R.FirstName, R.LastName, R.Email, R.Email, R.DateLastLoggedIn, 1 as ResourceTypeID, R.Status 
				from 
					reporting.Global_Resource R where isadministrator = 1 and status = 'Active'
		end
	end

	select * from @tbl
end
GO

drop procedure GetMapDiagram
go
drop table [tempResolvedRel]
go
drop table TestIntersect
go
drop table TestIntersectNode
go

alter table IntersectTypePredicate drop column [IntersectTypePredicate]
go

alter procedure [dbo].[AddMapRelationship]
--declare
	--@MapID int,
	@ResourceID int,
	@Date datetime,
	@ObjectType varchar(50),			-- The start object type.
	@ObjectID int,						-- The start object ID.
	@Classification int,
	@IntersectRole int,
	@Description nvarchar(4000),
	@SubjectType varchar(50),
	@SubjectID int,
	@PredicateID int
	
--set @ResourceID = 1
--set @Date = getutcdate()
--set @ObjectType = 'Artifact'
--set @ObjectID = 4651
--set @Classification = 1
--set @IntersectRole = NULL
--set @Description = ''
--insert into @Objects VALUES ('Artifact', 11808)


as
begin
	set nocount on;

	declare @Objects table (id int identity, ObjectType varchar(250), ObjectID int, StartType varchar(50), StartTypeID int, EndType varchar(50), EndTypeID int,IntersectTypeID int);

	insert into @Objects (ObjectType, ObjectID) values (@SubjectType, @SubjectID);

	if @IntersectRole = 0 
	begin
		set @IntersectRole = null
	end

	declare @MapID int;
	set @MapID = 1;

	declare @current int,
			@max int,
			@ErrorMessage nvarchar(2500),
			@IntersectID int,
			
			@StartType varchar(50),	@StartTypeID int,
			@EndType varchar(50),	@EndTypeID int,	
			@IntersectTypeID int,
			@SubjectNodeID int, @ObjectNodeID int
	
	declare @Intersects IDTable
	
	/*	Get the relationship types we need to check or create.	*/
	--declare @RelationTypes table (
	--	ID int identity, 
	--	StartType varchar(50), StartTypeID int, 
	--	EndType varchar(50), EndTypeID int, 
	--	IntersectTypeID int
	--)
	
	--insert into @RelationTypes
	--	select	distinct 
	--			S.ObjectType, S.ObjectTypeID, 
	--			E.ObjectType, E.ObjectTypeID, 
	--			RT.IntersectTypeID
	--	from	@Objects O
	--			inner join cache.ObjectDetails S on S.[Object] = @ObjectType and S.ObjectID = @ObjectID
	--			inner join cache.ObjectDetails E on E.[Object] = O.ObjectType and E.ObjectID = O.ObjectID
	--			left join utility.RelationshipTypes RT on RT.SourceObjectType = S.ObjectType and RT.SourceObjectID = S.ObjectTypeID and RT.TargetObjectType = E.ObjectType and RT.TargetObjectID = E.ObjectTypeID
		
	----remove existing relationship types
	--declare @RelationExisting table (id int);

	--		insert into @RelationExisting
	--		select distinct IntersectTypeID from (
	--			select 
	--				n.ID,
	--				n.IntersectTypeID, 
	--				n.ObjectType, 
	--				n.ObjectID, 
	--				n.[Order], 
	--				n2.ID as ID2, 
	--				n2.IntersectTypeID as IntersectTypeID2, 
	--				n2.ObjectType as ObjectType2, 
	--				n2.ObjectID as ObjectID2, 
	--				n2.[Order] as Order2 
	--			from 
	--				intersecttypenode n
	--			join 
	--				intersecttypenode n2 on n2.intersecttypeid = n.intersecttypeid and n2.[order] = 2
	--			where 
	--				n.[order] = 1
	--		) nt
	--			join 
	--				cache.ObjectDetails S on S.[Object] = @ObjectType and S.ObjectID = @ObjectID
	--			join 
	--				@Objects O on 1=1
	--			join 
	--				cache.ObjectDetails E on E.[Object] = O.ObjectType and E.ObjectID = @ObjectID
	--			where 
	--				(nt.objecttype = S.ObjectType and nt.objectID = S.ObjectTypeID and nt.objecttype2 = E.ObjectType and nt.objectID2 = E.ObjectTypeID) or  
	--				(nt.objecttype2 = S.ObjectType and nt.objectID2 = S.ObjectTypeID and nt.objecttype = E.ObjectType and nt.objectID = E.ObjectTypeID)
	
	--update object table with types and id if applicable
	update 
		obj
	set 
		 obj.IntersectTypeID = t.IntersectTypeID
		,obj.StartType = t.StartType
		,obj.StartTypeID = t.StartTypeID
		,obj.EndType = t.EndType
		,obj.EndTypeID = t.EndTypeID
	from
		@Objects obj
	join (
		select	distinct 
				S.ObjectType as StartType, S.ObjectTypeID as StartTypeID, 
				E.ObjectType as EndType, E.ObjectTypeID as EndTypeID,
				O.ObjectType, O.ObjectID,
				min(RT.IntersectTypeID) as IntersectTypeID
		from	@Objects O
				inner join cache.ObjectDetails S on S.[Object] = @ObjectType and S.ObjectID = @ObjectID
				inner join cache.ObjectDetails E on E.[Object] = O.ObjectType and E.ObjectID = O.ObjectID
				left join utility.RelationshipTypes RT on RT.SourceObjectType = S.ObjectType and RT.SourceObjectID = S.ObjectTypeID
				 and RT.TargetObjectType = E.ObjectType and RT.TargetObjectID = E.ObjectTypeID
				 group by s.objecttype,s.objecttypeid,e.objecttypeid,e.objecttype,o.objecttype,o.objectid
				 ) t on t.ObjectType = obj.ObjectType and t.ObjectID = obj.ObjectID;

	set @current = 1
	select @max = MAX(ID) from @Objects
	while @current <= @max
	begin
		select	@StartType = StartType,
				@StartTypeID = StartTypeID,	

				@EndType = EndType,
				@EndTypeID = EndTypeID,	

				@IntersectTypeID = IntersectTypeID
		from	@Objects
		where	ID = @current

		--create if it doesn't exist
		if (@IntersectTypeID = NULL)
		begin
			
			--get the object types
			select @StartType = c.ObjectType, @StartTypeID = c.ObjectTypeID from cache.ObjectDetails c
			where c.[Object] = @ObjectType and c.ObjectID = @ObjectID;
			
			select @EndType = c.ObjectType, @EndTypeID = c.ObjectTypeID from cache.ObjectDetails c
			join @Objects O on O.ObjectType = c.[Object] and O.ObjectID = c.ObjectID and O.ID = @current;

			-- Create the relationship type
			INSERT INTO [IntersectType] (UpdatedOn, UpdatedBy) VALUES (getutcdate(), 0)

			SELECT @IntersectTypeID = SCOPE_IDENTITY()

			INSERT INTO IntersectTypeNode	(IntersectTypeID, ObjectType, ObjectID, [Order]) 
			VALUES							(@IntersectTypeID, @StartType, @StartTypeID, 1)

			INSERT INTO IntersectTypeNode	(IntersectTypeID, ObjectType, ObjectID, [Order])
			VALUES							(@IntersectTypeID, @EndType, @EndTypeID, 2)
		end


		set @current = @current + 1
	end

	-- Now deal with the objects themselves.
	declare @Relations table (
		ID int identity, 
			
		StartObject varchar(50), StartObjectID int, StartName nvarchar(500), StartType varchar(50), StartTypeID int, StartIntersectNodeTypeID int,
		EndObject varchar(50), EndObjectID int, EndName nvarchar(500), EndType varchar(50), EndTypeID int, EndIntersectNodeTypeID int,

		IntersectTypeID int, IntersectID int, [Action] varchar(1)
	)

	insert into @Relations
		select	distinct 
				O.ObjectType, O.ObjectID, OD.Name, OD.ObjectType, OD.ObjectTypeID, RT.SourceIntersectTypeNodeID, 
				@ObjectType, @ObjectID, D.Name, D.ObjectType, D.ObjectTypeID, RT.TargetIntersectTypeNodeID,
				RT.IntersectTypeID, R.ID, CASE WHEN R.ID IS NULL THEN 'C' ELSE 'U' END
		from	@Objects O
				left join cache.ObjectDetails OD on OD.[Object] = @ObjectType and OD.ObjectID = @ObjectID
				left join cache.ObjectDetails D on D.[Object] = O.ObjectType and D.ObjectID = O.ObjectID
				left join utility.RelationshipTypes RT on RT.SourceObjectType = OD.ObjectType and RT.SourceObjectID = OD.ObjectTypeID and RT.TargetObjectType = D.ObjectType and RT.TargetObjectID = D.ObjectTypeID and RT.IntersectTypeID = @IntersectTypeID
				outer apply (
							select	i.ID
							from	[Intersect] I
									inner join IntersectNode N1 on N1.IntersectID = I.ID and N1.ObjectType = @ObjectType and N1.ObjectID = @ObjectID
									inner join IntersectNode N2 on N2.IntersectID = I.ID and N2.ObjectType = O.ObjectType and N2.ObjectID = O.ObjectID
							where	i.IntersectTypeID = RT.IntersectTypeID
							) R

	set @current = 1
	select @max = MAX(ID) from @Relations
	while @current <= @max
	begin
		declare @StartObject varchar(50),	@StartObjectID int, @StartName nvarchar(500),	@StartIntersectNodeTypeID int, 
				@EndObject varchar(50),		@EndObjectID int,	@EndName nvarchar(500),		@EndIntersectNodeTypeID int,
				@Action varchar(1)

		set @IntersectID = null	--reset here

		select	@StartObject = StartObject,
				@StartObjectID = StartObjectID,
				@StartName = StartName,	
				@StartTypeID = StartTypeID,	
				@StartIntersectNodeTypeID = StartIntersectNodeTypeID, 

				@EndObject = EndObject,
				@EndObjectID = EndObjectID,	
				@EndName = EndName,	
				@EndTypeID = EndTypeID,	
				@EndIntersectNodeTypeID = EndIntersectNodeTypeID,

				@IntersectTypeID = IntersectTypeID, 
				@IntersectID = IntersectID, 
				@Action = [Action]
		from	@Relations
		where	ID = @current
		
		if @ObjectID > 0
		begin			
			-- Relationship does not yet exist, so CREATE.
			if (@IntersectID is null and @StartIntersectNodeTypeID is not null and @EndIntersectNodeTypeID is not null)
				begin
					INSERT INTO [Intersect] (IntersectTypeID, Classification, [Description], [IntersectTypeRoleID]) VALUES (@IntersectTypeID, @Classification, @Description, @IntersectRole)

					SELECT @IntersectID = SCOPE_IDENTITY()

					INSERT INTO IntersectNode	(IntersectTypeNodeID, IntersectID, ObjectType, ObjectID) 
					VALUES						(@StartIntersectNodeTypeID, @IntersectID, @StartObject, @StartObjectID)

					SELECT @ObjectNodeID = SCOPE_IDENTITY();

					INSERT INTO IntersectNode	(IntersectTypeNodeID, IntersectID, ObjectType, ObjectID)
					VALUES						(@EndIntersectNodeTypeID, @IntersectID, @EndObject, @EndObjectID)
					
					SELECT @SubjectNodeID = SCOPE_IDENTITY();

					update	@Relations
					set		IntersectID = @IntersectID
					where	(StartObject = @StartObject and StartObjectID = @StartObjectID and EndObject = @EndObject and EndObjectID = @EndObjectID) 
							or (StartObject = @EndObject and StartObjectID = @EndObjectID and EndObject = @StartObject and EndObjectID = @StartObjectID)
							--ID = @current

					exec utility.AddAuditEntry @StartObject, @StartObjectID, @ResourceID, @Date, 'Created', 'Intersect', @IntersectID
					exec utility.AddAuditEntry @EndObject, @EndObjectID, @ResourceID, @Date, 'Created', 'Intersect', @IntersectID
					
					
				end
			else
				begin
					-- Update the Classification and Description only if the relationship already exists.
					if @IntersectID is not null
					begin
						update	[Intersect]
						set		Classification = @Classification,
								Description = @Description,
								IntersectTypeRoleID = @IntersectRole
						where	ID = @IntersectID

						exec utility.AddAuditEntry 'Intersect', @IntersectID, @ResourceID, @Date, 'Updated', 'Intersect', @IntersectID
					end

				end
				
			if (@IntersectID is not null and @SubjectNodeID is null and @ObjectNodeID is null)
			begin 
			
				select
					@ObjectNodeID = N.ID
				from
					IntersectNode N
				join
					@Relations R on R.IntersectID = N.IntersectID
				where
					N.IntersectID = @IntersectID and N.IntersectTypeNodeID = R.StartIntersectNodeTypeID;
				
				select
					@SubjectNodeID = N.ID
				from
					IntersectNode N
				join
					@Relations R on R.IntersectID = N.IntersectID
				where
					N.IntersectID = @IntersectID and N.IntersectTypeNodeID = R.EndIntersectNodeTypeID;
					
			end

			insert into IntersectMap (SubjectIntersectNodeID, ObjectIntersectNodeID, PredicateID, [Type])
			select	top 1
					@SubjectNodeID as SubjectIntersectNodeID,
					@ObjectNodeID as ObjectIntersectNodeID,				
					@PredicateID as PredicateID,
					1 as [Type]
			from IntersectMap m
			where not exists (select * from intersectmap where SubjectIntersectNodeID = @SubjectNodeID and ObjectIntersectNodeID = @ObjectNodeID and PredicateID = @PredicateID);

			if (@IntersectID is not null) and (not exists(select 1 from @Intersects where ObjectID = @IntersectID))
			begin
				insert into @Intersects VALUES (@IntersectID)
				exec [cache].[SynchronizeObjectDetails] 'Intersect', @IntersectID
			end
		end

		set @current = @current + 1
	end

	exec cache.SynchronizeRelationships @Intersects
end
GO

alter procedure [dbo].[AsyncUpdateObject]
	@Object varchar(50),
	@ObjectID int,
	@ParentObject varchar(50),
	@ParentObjectID int,
	@ResourceID int
as
begin
	set nocount on;
	declare @trans varchar(25) = 'Trans',
			@current int = 1,
			@max int,
			@date datetime = getutcdate()

	begin try
		begin transaction @trans
		
		--exec [cache].[SynchronizeObjectDetails] @Object, @ObjectID
		
		--INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID], [Priority]) values ('ObjectIndex', 'U', @Object, @ObjectID, 4)
		exec [utility].[AddAuditEntry] @ParentObject, @ParentObjectID, @ResourceID, @date, 'Updated', @Object, @ObjectID

		--if @Object = 'Artifact'
		--begin
		--	with h as	(
		--				select	ID,
		--						ParentID
		--				from	Artifact
		--				where	ID = @ObjectID
		--				union all
		--				select	A.ID,
		--						A.ParentID
		--				from	Artifact A
		--						inner join h P on P.ID = A.ParentID
		--				)
		--	update	T
		--	set		T.TextPath = utility.GetBreadcrumbStringWrapper(@Object, S.ID, '/')
		--	from	Artifact T
		--			inner join h S on S.ID = T.ID;
		--end

		if @Object in ('AttributeTypeRelation', 'AttributeTypeRelation', 'ResponsibilityTypeRelation', 'ResponsibilityType')
		begin
			exec utility.CalculateStatistics
		end
		else
		begin
			exec utility.CalculateStatistics @Object, @ObjectID
		end

		if @Object = 'Responsibility'
		begin
			exec cache.SynchronizeResponsibilitiesForObject @ParentObject, @ParentObjectID 
		end

		if @Object = 'Taxonomy'
		begin
			--with h as	(
			--			select	ID,
			--					ParentID
			--			from	Taxonomy
			--			where	ID = @ObjectID
			--			union all
			--			select	A.ID,
			--					A.ParentID
			--			from	Taxonomy A
			--					inner join h P on P.ID = A.ParentID
			--			)
			--update	T
			--set		T.TextPath = utility.GetBreadcrumbStringWrapper(@Object, S.ID, '/')
			--from	Taxonomy T
			--		inner join h S on S.ID = T.ID;

			UPDATE	F
			set		F.FormattedValue = utility.GetFormattedFieldLookupValue(FT.Type, FT.LookupDisplayFormat, FT.LookupObjectType, FT.LookupObjectID, F.Value)
			FROM	Field F
					inner join FieldType FT on FT.ID = F.FieldTypeID and FT.LookupObjectType = 'Taxonomy' 
					inner join Taxonomy A on A.ID = @ObjectID and A.TaxonomyTypeID = FT.LookupObjectID

			exec [cache].[SynchronizeResponsibilities]
		end

		commit transaction @trans
	end try
	begin catch
		DECLARE @ErrorMessage NVARCHAR(4000);
		DECLARE @ErrorSeverity INT;
		DECLARE @ErrorState INT;

		SELECT 
			@ErrorMessage = ERROR_MESSAGE(),
			@ErrorSeverity = ERROR_SEVERITY(),
			@ErrorState = ERROR_STATE();

		-- Use RAISERROR inside the CATCH block to return error
		-- information about the original error that caused
		-- execution to jump to the CATCH block.
		RAISERROR (@ErrorMessage, -- Message text.
				   @ErrorSeverity, -- Severity.
				   @ErrorState -- State.
				   );

		rollback transaction @trans
	end catch
end
GO

ALTER PROCEDURE [dbo].[GetAllowedIntersectionTypes]
(
--declare 
	@SourceType varchar(250),
	@SourceTypeID int,
	@IntersectID int = 0
--set @SourceType = 'ArtifactType'--'FusionAttributeType'
--set @SourceTypeID = 1--213
--set @IntersectID = 32
)
AS
BEGIN
	SET NOCOUNT ON;

	declare @tbl table (IntersectTypeID int, TargetType varchar(50), TargetTypeID int, TargetName nvarchar(500), ParentIntersectID int);

	insert into @tbl
		SELECT	RT.IntersectTypeID,
				RT.[TargetObjectType] AS TargetType,
				RT.[TargetObjectID] AS TargetTypeID, 
				(
				case RT.[TargetObjectType]
					when 'TaxonomyType' then 'Model: '
					when 'DomainType' then 'Reference: '
					when 'FusionType' then 'Fusion: '
					when 'FusionAttributeType' then 'Fusion: '
					when 'ArtifactType' then 'Glossary: '
					when 'RuleType' then 'Rules: '
					when 'PolicyType' then 'Policies: '
					else ''
				end + 
				case 
					when RT.[TargetMenuDisplayText] is null then coalesce(RTD.Name, RT.[TargetObjectType])
					when RT.[TargetMenuDisplayText] = '' then coalesce(RTD.Name, RT.[TargetObjectType])
					else RT.[TargetMenuDisplayText]
				end 
				) AS TargetName,
				NULL
		FROM	[utility].[RelationshipTypes] RT
				left join cache.ObjectDetails RTD on RTD.[Object] = RT.TargetObjectType and RTD.ObjectID = RT.TargetObjectID
		WHERE	RT.SourceObjectType = @SourceType and RT.SourceObjectID = @SourceTypeID 
		ORDER BY (
				case RT.[TargetObjectType]
					when 'TaxonomyType' then 'Model: '
					when 'DomainType' then 'Reference: '
					when 'FusionType' then 'Fusion: '
					when 'FusionAttributeType' then 'Fusion: '
					when 'ArtifactType' then 'Glossary: '
					when 'RuleType' then 'Rules: '
					when 'PolicyType' then 'Policies: '
					else ''
				end + 
				case 
					when RT.[TargetMenuDisplayText] is null then coalesce(RTD.Name, RT.[TargetObjectType])
					when RT.[TargetMenuDisplayText] = '' then coalesce(RTD.Name, RT.[TargetObjectType])
					else RT.[TargetMenuDisplayText]
				end 
				)

	if @IntersectID > 0
	begin
		select	@SourceType = 'IntersectType',
				@SourceTypeID = IntersectTypeID
		from	[Intersect]
		where	ID = @IntersectID;

		insert into @tbl
			SELECT	RT.IntersectTypeID,
					RT.[TargetObjectType] AS TargetType,
					RT.[TargetObjectID] AS TargetTypeID, 
					case 
						when RT.[TargetMenuDisplayText] is null then coalesce(RTD.Name, RT.[TargetObjectType])
						when RT.[TargetMenuDisplayText] = '' then coalesce(RTD.Name, RT.[TargetObjectType])
						else RT.[TargetMenuDisplayText]
					end AS TargetName,
					NULL
			FROM	[utility].[RelationshipTypes] RT
					left join cache.ObjectDetails RTD on RTD.[Object] = RT.TargetObjectType and RTD.ObjectID = RT.TargetObjectID
			WHERE	RT.SourceObjectType = @SourceType and RT.SourceObjectID = @SourceTypeID
			ORDER BY case 
						when RT.[TargetMenuDisplayText] is null then coalesce(RTD.Name, RT.[TargetObjectType])
						when RT.[TargetMenuDisplayText] = '' then coalesce(RTD.Name, RT.[TargetObjectType])
						else RT.[TargetMenuDisplayText]
					end

		-- Now figure out if we need to remove any fusion relationship types based on ownership.
		select	top 1
				@SourceType = ObjectType,
				@SourceTypeID = ObjectID
		from	IntersectNode N
				inner join Artifact A	on A.ID = N.ObjectID and N.ObjectType = 'Artifact'
										and N.IntersectID = @IntersectID
				inner join ArtifactType AT on AT.ID = A.ArtifactTypeID and AT.CanOwnFusion = 1

		delete	@tbl
		where	TargetType = 'FusionAttributeType'
				and TargetTypeID not in (
										select	ObjectID
										from	FusionAttributeOwnerRule
										where	ObjectType = 'FusionAttributeType' 
												and RelationshipOwnerObjectType = @SourceType 
												and RelationshipOwnerObjectID = @SourceTypeID
										)
	end

	select * from @tbl order by TargetName
END
GO

alter FUNCTION [utility].[ObjectDetail]
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
