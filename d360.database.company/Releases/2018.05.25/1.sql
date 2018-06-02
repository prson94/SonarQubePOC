CREATE TABLE cache.AssetResponsibility (
	ID uniqueidentifier constraint DF_CacheAssetResponsibility_ID default(newid()) NOT NULL,
	RuleID int NOT NULL,
	ResponsibilityTypeID int NOT NULL,
	AssetID bigint NOT NULL,
	Object varchar(50) NOT NULL,
	ObjectID int NOT NULL,
	AssetTypeID int NOT NULL,
	Type varchar(50) NOT NULL,
	TypeID int NOT NULL,
	SecurityAsset varchar(1) NOT NULL,
	SecurityAssetID int NOT NULL,
	Context nvarchar(max) NULL,
	ApplyToType bit NOT NULL,
	IsVisible bit NOT NULL,
	Overriden bit NOT NULL,
	OverrideItemID bigint NULL,
	CONSTRAINT PK_CacheAssetResponsibility PRIMARY KEY NONCLUSTERED ( ID )
)
GO

CREATE INDEX IX_CacheAssetResponsibility_Asset ON cache.AssetResponsibility ( AssetID ASC, Overriden ASC ) INCLUDE ( ResponsibilityTypeID, SecurityAsset, SecurityAssetID )
GO
CREATE INDEX IX_CacheAssetResponsibility_SecurityAsset ON cache.AssetResponsibility ( SecurityAsset ASC, SecurityAssetID ASC, Overriden ASC ) INCLUDE ( ResponsibilityTypeID, AssetID )
GO

create procedure cache.SecurityProcessor
	@CacheObject int,
	@Source int,
	@SourceID bigint
as
begin
	set nocount on;

	if @CacheObject = 1	--AssetDelete cache table
	begin
		merge	cache.AssetDelete as T
		using	(
				select		RD.AssetID,
							RD.ResourceID
				from		ResponsibilityDetails RD with (NOLOCK, NOWAIT)
							inner join ResponsibilityTypeObjectClaim RTC with (NOLOCK, NOWAIT)	on RTC.ResponsibilityTypeID = RD.ResponsibilityTypeID 
																			and RTC.ObjectType = RD.Type 
																			and RTC.ObjectID = RD.TypeID
																			and RTC.Claim = 4
																			and RTC.ClaimObject = 1
				group by	RD.AssetID,
							RD.ResourceID
				) as S
		on		(T.AssetID = S.AssetID and T.ResourceID = S.ResourceID)
		when	not matched by source then
				delete
		when	not matched by target then
				insert (AssetID, ResourceID)
				values (S.AssetID, S.ResourceID);
	end

	if @CacheObject = 2	--AssetEdit cache table
	begin
		merge	cache.AssetEdit as T
		using	(
				select		RD.AssetID,
							RD.ResourceID
				from		ResponsibilityDetails RD with (NOLOCK, NOWAIT)
							inner join ResponsibilityTypeObjectClaim RTC with (NOLOCK, NOWAIT)	on RTC.ResponsibilityTypeID = RD.ResponsibilityTypeID 
																			and RTC.ObjectType = RD.Type 
																			and RTC.ObjectID = RD.TypeID
																			and RTC.Claim = 3 
																			and RTC.ClaimObject = 1
				group by	RD.AssetID,
							RD.ResourceID
				) as S
		on		(T.AssetID = S.AssetID and T.ResourceID = S.ResourceID)
		when	not matched by source then
				delete
		when	not matched by target then
				insert (AssetID, ResourceID)
				values (S.AssetID, S.ResourceID);
	end

	if @CacheObject = 3	--NoRead cache table
	begin
		merge	cache.NoRead as T
		using	(
				select		RD.AssetID,
							RD.Object,
							RD.ObjectID,
							RD.ResourceID
				from		ResponsibilityDetails RD with (NOLOCK, NOWAIT)
							left join ResponsibilityTypeObjectClaim RTC with (NOLOCK, NOWAIT)	on RTC.ResponsibilityTypeID = RD.ResponsibilityTypeID 
																			and RTC.ObjectType = RD.Type 
																			and RTC.ObjectID = RD.TypeID
																			and RTC.Claim = 1 
																			and RTC.ClaimObject = 1
				where		RTC.ObjectID is null 
				group by	RD.AssetID,
							RD.Object,
							RD.ObjectID,
							RD.ResourceID
				) as S
		on		(T.AssetID = S.AssetID and T.ResourceID = S.ResourceID)
		when	not matched by source then
				delete
		when	not matched by target then
				insert (AssetID, Object, ObjectID, ResourceID)
				values (S.AssetID, S.Object, S.ObjectID, S.ResourceID);
	end

	if @CacheObject = 4	--AssetResponsibility cache table
	begin
		if @Source = 1	--None
			or @Source = 2	--GroupBulkLoad
			or @Source = 3	--UserBulkLoad
		begin
			-- 1. Load Rule assignments
			merge	cache.AssetResponsibility as T
			using	(
					select	R.ID as RuleID,
							R.ResponsibilityTypeID,
							A.ID as AssetID,
							A.Object,
							A.ObjectID,
							A.AssetTypeID,
							T.Object as Type,
							T.ObjectID as TypeID,
							coalesce(TI.SecurityAsset, I.SecurityAsset) as SecurityAsset,
							coalesce(TI.SecurityAssetID, I.SecurityAssetID) as SecurityAssetID,
							R.Context,
							R.ApplyToType,
							coalesce(R.IsVisible, cast(1 as bit)) as IsVisible
					from	Asset A
							inner join AssetType T on T.ID = A.AssetTypeID
							inner join ResponsibilityTypeRelationRule R on R.Object = T.Object and R.ObjectID = T.ObjectID
							left join ResponsibilityTypeRelationTypeItem TI on TI.RuleID = R.ID and R.ApplyToType = 1
							left join ResponsibilityTypeRelationItem I on I.RuleID = R.ID and I.AssetID = A.ID and R.ApplyToType = 0
					where	coalesce(TI.RuleID, I.RuleID) is not null
					) as S 
			on		(
					S.RuleID = T.RuleID
					and S.AssetID = T.AssetID
					and S.SecurityAsset = T.SecurityAsset
					and S.SecurityAssetID = T.SecurityAssetID
					)
			when	not matched by source and T.RuleID <> 0 then
					delete
			when	not matched by target then
					insert (RuleID, ResponsibilityTypeID, AssetID, Object, ObjectID, AssetTypeID, Type, TypeID, SecurityAsset, SecurityAssetID, Context, ApplyToType, IsVisible, Overriden)
					values (S.RuleID, S.ResponsibilityTypeID, S.AssetID, S.Object, S.ObjectID, S.AssetTypeID, S.Type, S.TypeID, S.SecurityAsset, S.SecurityAssetID, S.Context, S.ApplyToType, S.IsVisible, 0);

			-- 2. Override rule assignments
			update	T
			set		T.Overriden = 1,
					T.OverrideItemID = S.ID
			from	cache.AssetResponsibility T
					inner join ResponsibilityTypeRelationOverrideItem S on S.AssetID = T.AssetID and S.ResponsibilityTypeID = T.ResponsibilityTypeID and T.RuleID <> 0;

			-- 3. Load Override assignments
			merge	cache.AssetResponsibility as T
			using	(
					select	0 as RuleID,
							I.ID,
							I.ResponsibilityTypeID,
							A.ID as AssetID,
							A.Object,
							A.ObjectID,
							A.AssetTypeID,
							T.Object as Type,
							T.ObjectID as TypeID,
							I.SecurityAsset,
							I.SecurityAssetID,
							I.Context
					from	Asset A
							inner join AssetType T on T.ID = A.AssetTypeID
							inner join ResponsibilityTypeRelationOverrideItem I on I.AssetID = A.ID
					) as S 
			on		(
					S.RuleID = T.RuleID
					and S.AssetID = T.AssetID
					and S.SecurityAsset = T.SecurityAsset
					and S.SecurityAssetID = T.SecurityAssetID
					)
			when	not matched by source and T.RuleID = 0 then
					delete
			when	not matched by target then
					insert (RuleID, ResponsibilityTypeID, AssetID, Object, ObjectID, AssetTypeID, Type, TypeID, SecurityAsset, SecurityAssetID, Context, ApplyToType, IsVisible, Overriden, OverrideItemID)
					values (S.RuleID, S.ResponsibilityTypeID, S.AssetID, S.Object, S.ObjectID, S.AssetTypeID, S.Type, S.TypeID, S.SecurityAsset, S.SecurityAssetID, S.Context, 0, 1, 0, S.ID);
		end				--None
		if @Source = 4	--UserFirstLogin
		begin
			-- 1. Load Rule assignments
			merge	cache.AssetResponsibility as T
			using	(
					select	R.ID as RuleID,
							R.ResponsibilityTypeID,
							A.ID as AssetID,
							A.Object,
							A.ObjectID,
							A.AssetTypeID,
							T.Object as Type,
							T.ObjectID as TypeID,
							coalesce(TI.SecurityAsset, I.SecurityAsset) as SecurityAsset,
							coalesce(TI.SecurityAssetID, I.SecurityAssetID) as SecurityAssetID,
							R.Context,
							R.ApplyToType,
							coalesce(R.IsVisible, cast(1 as bit)) as IsVisible
					from	Asset A
							inner join AssetType T on T.ID = A.AssetTypeID
							inner join ResponsibilityTypeRelationRule R on R.Object = T.Object and R.ObjectID = T.ObjectID
							left join ResponsibilityTypeRelationTypeItem TI on TI.RuleID = R.ID and R.ApplyToType = 1
							left join ResponsibilityTypeRelationItem I on I.RuleID = R.ID and I.AssetID = A.ID and R.ApplyToType = 0
					where	coalesce(TI.RuleID, I.RuleID) is not null
					) as S 
			on		(
					S.RuleID = T.RuleID
					and S.AssetID = T.AssetID
					and S.SecurityAsset = T.SecurityAsset
					and S.SecurityAssetID = T.SecurityAssetID
					)
			when	not matched by source and T.RuleID <> 0 then
					delete
			when	not matched by target then
					insert (RuleID, ResponsibilityTypeID, AssetID, Object, ObjectID, AssetTypeID, Type, TypeID, SecurityAsset, SecurityAssetID, Context, ApplyToType, IsVisible, Overriden)
					values (S.RuleID, S.ResponsibilityTypeID, S.AssetID, S.Object, S.ObjectID, S.AssetTypeID, S.Type, S.TypeID, S.SecurityAsset, S.SecurityAssetID, S.Context, S.ApplyToType, S.IsVisible, 0);

			-- 2. Override rule assignments
			update	T
			set		T.Overriden = 1,
					T.OverrideItemID = S.ID
			from	cache.AssetResponsibility T
					inner join ResponsibilityTypeRelationOverrideItem S on S.AssetID = T.AssetID and S.ResponsibilityTypeID = T.ResponsibilityTypeID and T.RuleID <> 0;
		end				--UserFirstLogin
		if @Source = 5	--ResponsibilityRuleChange
		begin
			-- 1. Load Rule assignments
			merge	cache.AssetResponsibility as T
			using	(
					select	R.ID as RuleID,
							R.ResponsibilityTypeID,
							A.ID as AssetID,
							A.Object,
							A.ObjectID,
							A.AssetTypeID,
							T.Object as Type,
							T.ObjectID as TypeID,
							coalesce(TI.SecurityAsset, I.SecurityAsset) as SecurityAsset,
							coalesce(TI.SecurityAssetID, I.SecurityAssetID) as SecurityAssetID,
							R.Context,
							R.ApplyToType,
							coalesce(R.IsVisible, cast(1 as bit)) as IsVisible
					from	Asset A
							inner join AssetType T on T.ID = A.AssetTypeID
							inner join ResponsibilityTypeRelationRule R on R.Object = T.Object and R.ObjectID = T.ObjectID and R.ID = @SourceID
							left join ResponsibilityTypeRelationTypeItem TI on TI.RuleID = R.ID and R.ApplyToType = 1
							left join ResponsibilityTypeRelationItem I on I.RuleID = R.ID and I.AssetID = A.ID and R.ApplyToType = 0
					where	coalesce(TI.RuleID, I.RuleID) is not null
					) as S 
			on		(
					S.RuleID = T.RuleID
					and S.AssetID = T.AssetID
					and S.SecurityAsset = T.SecurityAsset
					and S.SecurityAssetID = T.SecurityAssetID
					)
			when	not matched by source and T.RuleID = @SourceID then
					delete
			when	not matched by target then
					insert (RuleID, ResponsibilityTypeID, AssetID, Object, ObjectID, AssetTypeID, Type, TypeID, SecurityAsset, SecurityAssetID, Context, ApplyToType, IsVisible, Overriden)
					values (S.RuleID, S.ResponsibilityTypeID, S.AssetID, S.Object, S.ObjectID, S.AssetTypeID, S.Type, S.TypeID, S.SecurityAsset, S.SecurityAssetID, S.Context, S.ApplyToType, S.IsVisible, 0);

			-- 2. Override rule assignments
			update	T
			set		T.Overriden = 1,
					T.OverrideItemID = S.ID
			from	cache.AssetResponsibility T
					inner join ResponsibilityTypeRelationOverrideItem S on S.AssetID = T.AssetID 
						and S.ResponsibilityTypeID = T.ResponsibilityTypeID 
						and T.RuleID = @SourceID;
		end	--ResponsibilityRuleChange
	end 
end
go

--cache.SecurityProcessor 4,1,0

ALTER view [responsibility].[Core]
as
	select	distinct 
			RuleID,
			ResponsibilityTypeID,
			Object,
			ObjectID,
			AssetID,
			AssetTypeID,
			Type,
			TypeID,
			SecurityAsset,
			SecurityAssetID,
			Context,
			IsVisible,
			--Overriden,
			OverrideItemID
	from	cache.AssetResponsibility
	where	Overriden = 0
GO

ALTER VIEW [dbo].[ResponsibilityDetails]
AS 
select	O.AssetID,
		O.Object,
		O.ObjectID,
		O.Type,
		O.TypeID,
		O.Context,
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
		O.IsVisible,
		--O.ApplyToType,
		O.OverrideItemID
from	responsibility.Core O
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
GO

ALTER TRIGGER [dbo].[ResponsibilityTypeRelationOverrideItem_AfterDelete]
   ON  [dbo].[ResponsibilityTypeRelationOverrideItem]
   AFTER DELETE
AS 
BEGIN
	SET NOCOUNT ON;

	delete	T
	from	cache.AssetResponsibility T
			inner join deleted S on T.RuleID = 0 and T.OverrideItemID = S.ID;

	update	T
	set		T.Overriden = 0,
			T.OverrideItemID = null
	from	cache.AssetResponsibility T
			inner join deleted S on T.RuleID <> 0 and T.OverrideItemID = S.ID;

	--update	T
	--set		T.Overriden = 0
	--from	ResponsibilityTypeRelationItem T
	--		inner join deleted S on T.RuleID > 0 and S.AssetID = T.AssetID and S.ResponsibilityTypeID = T.ResponsibilityTypeID and T.Overriden = 1
	--		left join ResponsibilityTypeRelationItem E on E.RuleID = 0 and E.AssetID = S.AssetID and E.ResponsibilityTypeID = S.ResponsibilityTypeID and E.OverrideItemID <> S.ID
	--where	E.AssetID is null;

	--delete	T
	--from	ResponsibilityTypeRelationItem T
	--		inner join deleted S on T.OverrideItemID = S.ID;
END
GO

ALTER TRIGGER [dbo].[ResponsibilityTypeRelationOverrideItem_AfterInsert]
   ON  [dbo].[ResponsibilityTypeRelationOverrideItem]
   AFTER INSERT
AS 
BEGIN
	SET NOCOUNT ON;

	-- 1. Override rule assignments
	update	T
	set		T.Overriden = 1,
			T.OverrideItemID = S.ID
	from	cache.AssetResponsibility T
			inner join inserted S on S.AssetID = T.AssetID and S.ResponsibilityTypeID = T.ResponsibilityTypeID and T.RuleID <> 0;

	-- 2. Load Override assignments
	merge	cache.AssetResponsibility as T
	using	(
			select	0 as RuleID,
					I.ID,
					I.ResponsibilityTypeID,
					A.ID as AssetID,
					A.Object,
					A.ObjectID,
					A.AssetTypeID,
					T.Object as Type,
					T.ObjectID as TypeID,
					I.SecurityAsset,
					I.SecurityAssetID,
					I.Context
			from	Asset A
					inner join AssetType T on T.ID = A.AssetTypeID
					inner join inserted I on I.AssetID = A.ID
			) as S 
	on		(
			S.RuleID = T.RuleID
			and S.AssetID = T.AssetID
			and S.SecurityAsset = T.SecurityAsset
			and S.SecurityAssetID = T.SecurityAssetID
			)
	when	not matched by target then
			insert (RuleID, ResponsibilityTypeID, AssetID, Object, ObjectID, AssetTypeID, Type, TypeID, SecurityAsset, SecurityAssetID, Context, ApplyToType, IsVisible, Overriden, OverrideItemID)
			values (S.RuleID, S.ResponsibilityTypeID, S.AssetID, S.Object, S.ObjectID, S.AssetTypeID, S.Type, S.TypeID, S.SecurityAsset, S.SecurityAssetID, S.Context, 0, 1, 0, S.ID);

	--insert into ResponsibilityTypeRelationItem (RuleID, ResponsibilityTypeID, AssetID, SecurityAsset, SecurityAssetID, OverrideItemID) 
	--	select	0, 
	--			ResponsibilityTypeID, 
	--			AssetID, 
	--			SecurityAsset, 
	--			SecurityAssetID, 
	--			ID
	--	from	inserted;

	--update	T
	--set		T.Overriden = 1
	--from	ResponsibilityTypeRelationItem T
	--		inner join inserted S on T.RuleID > 0 and S.AssetID = T.AssetID and S.ResponsibilityTypeID = T.ResponsibilityTypeID and T.Overriden = 0;
END
GO

ALTER TRIGGER [dbo].[ResponsibilityTypeRelationOverrideItem_AfterUpdate]
   ON  [dbo].[ResponsibilityTypeRelationOverrideItem]
   AFTER UPDATE
AS 
BEGIN
	SET NOCOUNT ON;

	-- 2. Load Override assignments
	merge	cache.AssetResponsibility as T
	using	(
			select	0 as RuleID,
					I.ID,
					I.ResponsibilityTypeID,
					A.ID as AssetID,
					A.Object,
					A.ObjectID,
					A.AssetTypeID,
					T.Object as Type,
					T.ObjectID as TypeID,
					I.SecurityAsset,
					I.SecurityAssetID,
					I.Context
			from	Asset A
					inner join AssetType T on T.ID = A.AssetTypeID
					inner join inserted I on I.AssetID = A.ID
			) as S 
	on		(
			S.ID = T.OverrideItemID
			and S.RuleID = T.RuleID
			)
	when	matched then
	update	set T.SecurityAsset = S.SecurityAsset,
				T.SecurityAssetID = S.SecurityAssetID,
				T.ResponsibilityTypeID = S.ResponsibilityTypeID,
				T.Context = S.Context
	when	not matched by target then
			insert (RuleID, ResponsibilityTypeID, AssetID, Object, ObjectID, AssetTypeID, Type, TypeID, SecurityAsset, SecurityAssetID, Context, ApplyToType, IsVisible, Overriden, OverrideItemID)
			values (S.RuleID, S.ResponsibilityTypeID, S.AssetID, S.Object, S.ObjectID, S.AssetTypeID, S.Type, S.TypeID, S.SecurityAsset, S.SecurityAssetID, S.Context, 0, 1, 0, S.ID);
END
GO

ALTER TABLE [dbo].[ResponsibilityTypeRelationItem] DROP CONSTRAINT [DF_ResponsibilityTypeRelationItem_Overriden]
GO

DROP INDEX [IX_ResponsibilityTypeRelationItem_AssetID_SecurityAsset_Overriden_include] ON [dbo].[ResponsibilityTypeRelationItem]
GO

CREATE NONCLUSTERED INDEX [IX_ResponsibilityTypeRelationItem_AssetID_SecurityAsset_Include] ON [dbo].[ResponsibilityTypeRelationItem] ( [AssetID] ASC, [SecurityAsset] ASC ) INCLUDE ( [SecurityAssetID]) 
GO

DROP INDEX [IX_ResponsibilityTypeRelationItem_OverrideItemID] ON [dbo].[ResponsibilityTypeRelationItem]
GO

alter table [dbo].[ResponsibilityTypeRelationItem] drop column Overriden
alter table [dbo].[ResponsibilityTypeRelationItem] drop column OverrideItemID
GO


ALTER FUNCTION [lineage].[GetTrailForObject]
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
		

	declare @level int = 1;
	declare @i int;
	select @i = count(*) from @tbl where Visited = 0;

	while @i != 0 and @level <= 10
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
		set @level = @level + 1
	end

	RETURN
END
GO

CREATE TRIGGER [dbo].[Intersect_AfterUpdate]
	ON [dbo].[Intersect]
	FOR UPDATE
AS
BEGIN
	SET NOCOUNT ON;
		
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Update', [queue].WriteIndexXml('', Subject, SubjectID, UpdatedBy), 'Intersect', ID from inserted;

	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Update', [queue].WriteIndexXml('', Object, ObjectID, UpdatedBy), 'Intersect', ID from inserted;
END
GO

CREATE NONCLUSTERED INDEX [IX_ReferenceItem_ReferenceItemType_Visible]
    ON [dbo].[ReferenceItem]([ReferenceItemTypeID] ASC, [Visible] ASC)
    INCLUDE([ID]);
GO

DROP INDEX [IX_ReferenceItem_Visible] ON [dbo].[ReferenceItem];
GO

CREATE NONCLUSTERED INDEX [IX_ReferenceItem_Visible]
    ON [dbo].[ReferenceItem]([Visible] ASC)
    INCLUDE([ReferenceItemTypeID], [ID]);
GO

ALTER procedure [bulkload].[GetLoadColumns]
--declare	
	@action varchar(2),-- = 'P', --P = Promotion, R = Relation, O = Responsibilities, BL = Business Lineage, TL = Technical Lineage
	@type varchar(50),-- = 'ArtifactType',--'ArtifactType',--'IntersectType',--'ArtifactType',
	@id int,-- = 33,
	@getLookups bit = 1
as
begin
	declare @fields table (ID int identity, FieldTypeID int, Name nvarchar(250), Required bit, PartOfKey bit, AllowMultipleValues bit, IsLookup bit)
	declare @lookups table (ID int identity, FieldID int, Value nvarchar(max))
	declare @current int = 1,
			@max int,
			@isLookup bit = 0,
			@fieldTypeID int

	if @action = 'M'
	begin
		if @id = 0 -- Group membership
		begin
			insert into @fields values (1, 'Action', 1, 0, 0, 1)
			insert into @fields values (0, 'Group Name', 1, 1, 0, 0)
			insert into @fields values (0, 'User Email', 1, 1, 0, 0)

			insert into @lookups values (1, 'Add')
			insert into @lookups values (1, 'Remove')
		end

		if @id = 1 -- Add users
		begin
			set @type = 'ResourceType'

			insert into @fields values (1, 'Status', 1, 1, 0, 0)
			insert into @fields values (0, 'User Email', 1, 1, 0, 0)
			insert into @fields values (0, 'First Name', 1, 1, 0, 0)
			insert into @fields values (0, 'Last Name', 1, 1, 0, 0)

			insert into @lookups values (1, 'Active')
			insert into @lookups values (1, 'Inactive')
		end
	end

	if @action = 'O'
	begin
		--	insert into @fields 
			--	select	-1, 'Owner Type', 1, 1, 1

		/*	insert into @lookups
				select	-1,
						'Glossary: ' + Name from ArtifactType order by Name

			insert into @lookups
				select	-1,
						'Model: ' + Name from TaxonomyType order by Name

			insert into @lookups
				select	-1,
						'Policy: ' + Name from PolicyType order by Name*/

		--	insert into @fields 
			--	select	0, 'Owner ID', 1, 1, 0

			insert into @fields 
				select	1, 'Responsibility', 1, 1, 0, 1

			insert into @lookups
				select	1,
						Name from ResponsibilityType order by Name

			insert into @fields 
				select	2, 'Resource', 1, 1, 0, 1

			insert into @lookups
				select	2,
						'User:' + email from reporting.Global_Resource order by email

			insert into @lookups
				select	2,
						'Group:' + Name from [Group] order by Name

			
			begin
				insert into @fields
					select		0,
								'Asset ID', 
								1,
								1,
								0,
								0	
			end
	end

	if @action = 'P'
	begin
		if @type = 'AttributeType'
		begin
			insert into @fields 
				select	-1, 'Owner Type', 1, 1, 0, 1

			insert into @lookups
				select	-1,
						'Glossary: ' + Name from ArtifactType order by Name
			insert into @lookups
				select	-1,
						'Model: ' + Name from TaxonomyType order by Name

			insert into @fields 
				select	0, 'Owner ID', 1, 1, 0, 0
		end --AttributeType

		if @type = 'IntersectType'
		begin
			declare @s varchar(50),
					@sid int,
					@o varchar(50),
					@oid int

			select	@s = Subject,
					@sid = SubjectID,
					@o = Object,
					@oid = ObjectID
			from	IntersectType
			where	ID = @id


			if @s = 'TaxonomyType'
			begin
				insert into @fields
					select	0, 
							'Subject Path', 
							1, 
							1, 
							0,
							0
			end
			else
			begin
				insert into @fields
					select	FT.ID, 
							'Subject ' + FT.Name, 
							FT.IsRequired, 
							FT.IsPartOfKey, 
							FT.AllowMultipleValues,
							case FT.Type
								when 'Lookup' then cast(1 as bit)
								else cast(0 as bit)
							end as IsLookup
					from	FieldType FT 
					where	FT.IsPartOfKey = 1 and FT.Object = @s and FT.ObjectID = @sid
			end

			if @s = 'TaxonomyType'
			begin
				insert into @fields
					select	0, 
							'Object Path', 
							1, 
							1, 
							0,
							0
			end
			else
			begin
				insert into @fields
					select	FT.ID, 
							'Object ' + FT.Name, 
							FT.IsRequired, 
							FT.IsPartOfKey, 
							FT.AllowMultipleValues,
							case FT.Type
								when 'Lookup' then cast(1 as bit)
								else cast(0 as bit)
							end as IsLookup
					from	FieldType FT 
					where	FT.IsPartOfKey = 1 and FT.Object = @o and FT.ObjectID = @oid
			end

		end --IntersectType

		if @type = 'ArtifactType'
		begin
			declare @parentTypeID int = null,
					@parentTypeName nvarchar(250) = null
			
			/*select	@parentTypeID = T.ParentID,
					@parentTypeName = P.Name
			from	ArtifactType T 
					inner join ArtifactType P on P.ID = T.ParentID
			where	T.ID = @id*/

			select 
				@parentTypeID = I.SubjectID,
				@parentTypeName = I.SubjectName
			from 
				intersecttypedetail I                
			where I.[PredicateType] = 3 and [Object] = @type and ObjectID = @id;

			if @parentTypeID is not null
			begin
				insert into @fields 
					values(	0, 
							@parentTypeName, 
							cast(1 as bit), 
							cast(1 as bit), 
							cast(0 as bit),
							cast(1 as bit) );
				
				insert into @lookups
					select	(select id from @fields where fieldtypeid = 0), DisplayValue from AssetDetail where typeid = @parentTypeID and [object] = 'Artifact' order by DisplayValue;

			end
		end --ArtifactType

		if @type = 'ReferenceItemType'
		begin
			insert into @fields values (0, 'Code', 1, 1, 0, 0)
		end --ReferenceItemType

		if @type = 'TaxonomyType'
		begin
			declare @initialDepth int = 1,
					@maxDepth int = 1
			select @maxDepth = MaximumDepth from TaxonomyType where ID = @id
			declare @levels table (Value int)
			while  @initialDepth <= @maxDepth
			begin
				insert into @levels values (@initialDepth)
				set @initialDepth = @initialDepth + 1
			end

			insert into @fields 
				select	FT.ID, 
						case
							when TTL.Name is not null then TTL.Name + ' ' + FT.Name
							else 'Level ' + cast(L.Value as nvarchar)  + ' ' + FT.Name
						end, 
						FT.IsRequired, 
						FT.IsPartOfKey, 
						FT.AllowMultipleValues,
						case FT.Type
							when 'Lookup' then cast(1 as bit)
							else cast(0 as bit)
						end as IsLookup
				from	@levels L 
						inner join FieldType FT on FT.IsPartOfKey = 1 and FT.Object = @type and FT.ObjectID = @id
						left join TaxonomyTypeLevel TTL on TTL.[Level] = L.Value and TaxonomyTypeID = @id
		end --TaxonomyType		
	end -- P	
	else if (@action = 'R' or @action = 'U')
	begin
		--relate / unrelate
		print 'relate / unrelate'
				
		-- look up the intersect type and get the source / target type
		
		declare @subjectType varchar(50),
				@subjectTypeName nvarchar(500),
				@subjectTypeID int,
				@objectType varchar(50),
				@objectTypeName nvarchar(500),
				@objectTypeID int
		select	@subjectType = Subject,
				@subjectTypeName = SubjectName,
				@subjectTypeID = SubjectID,
				@objectTypeName = ObjectName,
				@objectType = Object,
				@objectTypeID = ObjectID
		from	IntersectTypeDetail
		where	ID = @id
		

		-- if its a fusion attribute type we just use the name

		-- get the key fields for the target / source		

		if @objectType = 'FusionAttributeType' or @objectType = 'IntersectType'
		begin
			insert into @fields values (0, @objectTypeName, 1, 1, 0, 0)
		end		
		else if @objectType = 'ReferenceItemType' and @objectTypeID = 0
		begin
			insert into @fields values (0, @objectTypeName + ' Asset Type ID', 1, 1, 0, 0)
		end		
		else
		begin
			--select * from fieldtype where [object] = 'ArtifactType' and objectid = 1 and IsPartOfKey = 1
			insert into @fields
				select		0,
							@objectTypeName + ' Asset ID', 
							1,
							1,
							0,
							0				
		end

		if @subjectType = 'FusionAttributeType' or @subjectType = 'IntersectType'
		begin
			insert into @fields values (0, @subjectTypeName, 1, 1, 0, 0)
		end
		else if @subjectType = 'ReferenceItemType' and @subjectTypeID = 0
		begin
			insert into @fields values (0, @subjectTypeName + ' Asset Type ID', 1, 1, 0, 0)
		end		
		else
		begin
			insert into @fields
				select		0,
							@subjectTypeName + ' Asset ID', 
							1,
							1,
							0,
							0				
		end
	end -- R or U

	-- fields on the item
	if ((@action = 'M' and @id = 1 ) or @action = 'R' or @action = 'P')
	begin
		insert into @fields
			select		ID,
						Name, 
						IsRequired,
						IsPartOfKey,
						AllowMultipleValues,
						case Type
							when 'Lookup' then cast(1 as bit)
							else cast(0 as bit)
						end as IsLookup
			from		FieldType 
			where		Object = @type 
						and ObjectID = @id 
						and Type not in ('Attribute', 'ComplexRelationLookup', 'FieldFromRelationship', 'FilteredLookup', 'FusionLookup', 'OwnershipLookup', 'RefListRelationship')
						and ( (@type = 'IntersectType' and IsPartOfKey = 0) OR (@type = 'TaxonomyType' and IsPartOfKey = 0) OR (@type <> 'TaxonomyType') )
						and IsEditable = 1
			order by	ColumnOrder
		
		select @max = max(ID) from @fields

		while @current <= @max
		begin
			select	@isLookup = IsLookup, 
					@fieldTypeID = FieldTypeID
			from	@fields 
			where	ID = @current

			if @isLookup = 1 and @getLookups = 1
			begin
				insert into @lookups
					select		@current,
								[Text]
					from		FieldLookupValue
					where		FieldTypeID = @fieldTypeID
					order by	[Text]
			end
			
			set @current = @current + 1
		end
	end

	
	if @action = 'BL'
	begin

			insert into @fields values (-4, 'Action', 1, 0, 0, 1)
			insert into @fields values (-2, 'Source Relation', 1, 1, 0, 1)
			insert into @fields values (0, 'Source Subject Asset ID', 1, 1, 0, 0)
			insert into @fields values (0, 'Source Object Asset ID', 1, 1, 0, 0)
			insert into @fields values (-3, 'Source Fusion Configuration', 1, 0, 0, 1)
			insert into @fields values (0, 'Source Fusion Path', 1, 0, 0, 0)

			insert into @fields values (-2, 'Target Relation', 1, 1, 0, 1)
			insert into @fields values (0, 'Target Subject Asset ID', 1, 1, 0, 0)
			insert into @fields values (0, 'Target Object Asset ID', 1, 1, 0, 0)
			insert into @fields values (-3, 'Target Fusion Configuration', 1, 0, 0, 1)
			insert into @fields values (0, 'Target Fusion Path', 1, 0, 0, 0)

			insert into @fields values (0, 'Transformation', 1, 0, 0, 0)

			insert into @lookups values (-4, 'Add')
			insert into @lookups values (-4, 'Remove')

			insert into @lookups
				select		-1,
							Name 
				from		TaxonomyType 
				order by	Name

			insert into @lookups
				select		-2,
							Name 
				from		IntersectType
				where		IsSystem = 0
				order by	Name

			insert into @lookups
				select		-3,
							Name 
				from		Fusion
				order by	Name
	end

	if @action = 'TL'
	begin
		insert into @fields values (-1, 'Source Fusion Configuration', 0, 0, 0, 1)
		insert into @fields values (0, 'Source Fusion Path', 0, 0, 0, 0)

		insert into @fields values (-1, 'Target Fusion Configuration', 0, 0, 0, 1)
		insert into @fields values (0, 'Target Fusion Path', 0, 0, 0, 0)

		insert into @fields values (0, 'Group', 0, 0, 0, 0)

		insert into @lookups
			select		-1,
						Name 
			from		Fusion
			order by	Name
	end

	--Return the data
	select	Name,
			Required,
			PartOfKey,
			AllowMultipleValues,
			IsLookup,
			(
			select	Value
			from	@lookups
			where	FieldID = F.ID
			for json path
			) as Lookups
	from	@fields F
	for json path
end
GO

alter table integration.ExecutionAssetType add [IsFullRefresh] BIT CONSTRAINT [DF_IntegrationExecutionAssetType_IsFullRefresh] DEFAULT ((0)) NOT NULL
GO

alter procedure [bulkload].[Promotions]
--declare
	@id int
--set @id = 84
as
begin
	set nocount on;

	declare @levels table (rowIndex int, [level] int, processed bit);

	declare @Object varchar(50),
			@ObjectID int,
			@Action varchar(1),
			@UpdatedOn datetime = getutcdate(),
			@UpdatedBy int = 0
				
	select	@Object = [Object], 
			@ObjectID = ObjectID,
			@Action = [Action],
			@UpdatedBy = UpdatedBy
	from	[Load]
	where	ID = @id;

	update	LoadItem
	set		Object = null, 
			ObjectID = null, 
			Status = null,
			StatusMessage = null
	where	LoadID = @id;

	-- Process hashes for Load Items
	if @Object = 'ReferenceItemType'
	begin		
		update	T
		set		T.KeyHash = CONVERT(
									varchar(32), 
									SUBSTRING(HASHBYTES('SHA1', substring(ltrim(rtrim(IC.Value)), 1, 250)), 3, 32), 
									2),
				T.FieldHash = V.FieldHash
		from	LoadItem T
				inner join LoadColumn C on C.LoadID = T.LoadID and C.Name = 'Code'
				inner join LoadItemColumn IC on IC.LoadID = C.LoadID and IC.RowIndex = T.RowIndex and IC.ColumnIndex = C.ColumnIndex
				inner join	(
							select		RowIndex,
										CONVERT(
											varchar(32), 
											SUBSTRING(HASHBYTES('SHA1', STRING_AGG(cast(FieldTypeID as nvarchar) + ':' + Value, char(59))), 3, 32), 
											2) as FieldHash
							from		(
										select		top 100 percent
													I.RowIndex,
													FT.ID as FieldTypeID,
													coalesce(IC.Value, '') as Value
										from		LoadItem I
													inner join LoadItemColumn IC on IC.LoadID = I.LoadID and IC.RowIndex = I.RowIndex and I.LoadID = @id
													inner join LoadColumn C on C.LoadID = I.LoadID and C.ColumnIndex = IC.ColumnIndex													
													left join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name and C.Name !='Code'
													left join dbo.ReferenceItem RI on C.Name = 'Code' and RI.ID = @ObjectID
										order by	I.RowIndex,
													FT.ID
										) A
							group by	A.RowIndex	
							) V on V.RowIndex = T.RowIndex
		where	T.LoadID = @id;
	end
	else if @Object = 'TaxonomyType'
	begin
		declare @currRow int, @maxRow int, @currLevel int;
		set @currRow = 1;
		set @currLevel = 0;
		set @maxRow = (select max(RowIndex) from LoadItem where LoadID = @id);	

		while @currRow < @maxRow
		begin
			set @currRow = @currRow + 1;

			--get level for current row
			select		@currLevel = coalesce(max(L.[Level]), 1) 
			from		TaxonomyTypeLevel L
						inner join LoadColumn LC on LC.LoadID = @id and L.Name = substring(LC.[Name], 1, len(LC.[Name]) - charindex(' ', reverse(LC.[Name])))
						inner join LoadItemColumn LI on LI.LoadID = @id and LI.RowIndex = @currRow and LI.ColumnIndex = LC.ColumnIndex and LI.[Value] is not null
			where		L.TaxonomyTypeID = @ObjectID

			insert into @levels (rowIndex, level, processed) values (@currRow, @currLevel, 0);

			--update the key hash based on the current level
			update	T
			set		T.KeyHash = K.KeyHash,
					T.FieldHash = V.FieldHash
			from	LoadItem T
					left join	(
								select		RowIndex,
											CONVERT(
												varchar(32), 
												SUBSTRING(HASHBYTES('SHA1', STRING_AGG(cast(FieldTypeID as nvarchar) + ':' + Value, char(59))), 3, 32), 
												2) as KeyHash
								from		(
												select top 100 percent
													IC.RowIndex, 
													FT.ID as FieldTypeID, 
													coalesce(IC.[Value],'') as [Value] 
												from LoadColumn LC
												inner join LoadItemColumn IC on IC.LoadID = @id and IC.RowIndex = @currRow and IC.ColumnIndex = LC.ColumnIndex
												inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.IsPartOfKey = 1 and FT.Name = reverse(substring(reverse(LC.[Name]), 0, charindex(' ',reverse(LC.[Name]))))			
												where LC.LoadID = @id and LC.ColumnIndex in (
			 										select		LC.ColumnIndex 
													from		TaxonomyTypeLevel L
																inner join LoadColumn LC on LC.LoadID = @id and L.Name = substring(LC.[Name], 1, len(LC.[Name]) - charindex(' ', reverse(LC.[Name])))
																inner join LoadItemColumn LI on LI.LoadID = @id and LI.RowIndex = @currRow and LI.ColumnIndex = LC.ColumnIndex and LI.[Value] is not null
													where		L.TaxonomyTypeID = @ObjectID and L.[Level] = @currLevel
													)
											) A
								group by	A.RowIndex
								) K on K.RowIndex = T.RowIndex
					inner join	(
								select		RowIndex,
											CONVERT(
												varchar(32), 
												SUBSTRING(HASHBYTES('SHA1', STRING_AGG(cast(FieldTypeID as nvarchar) + ':' + Value, char(59))), 3, 32), 
												2) as FieldHash
								from		(
											select		top 100 percent
														I.RowIndex,
														FT.ID as FieldTypeID,
														coalesce(IC.Value, '') as Value
											from		LoadItem I
														inner join LoadItemColumn IC on IC.LoadID = I.LoadID and IC.RowIndex = I.RowIndex and I.LoadID = @id
														inner join LoadColumn C on C.LoadID = I.LoadID and C.ColumnIndex = IC.ColumnIndex
														inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name
											order by	I.RowIndex,
														FT.ID
											) A
								group by	A.RowIndex	
								) V on V.RowIndex = T.RowIndex
			where	T.LoadID = @id and T.RowIndex = @currRow;
		end
	end
	else
	begin
		update	T
		set		T.KeyHash = K.KeyHash,
				T.FieldHash = V.FieldHash
		from	LoadItem T
				inner join	(
							select		RowIndex,
										CONVERT(
											varchar(32), 
											SUBSTRING(HASHBYTES('SHA1', STRING_AGG(cast(FieldTypeID as nvarchar) + ':' + Value, char(59))), 3, 32), 
											2) as KeyHash
							from		(
										select		top 1000000000 --percent
													I.RowIndex,
													FT.ID as FieldTypeID,
													coalesce(IC.Value, '') as Value
										from		LoadItem I
													inner join LoadItemColumn IC on IC.LoadID = I.LoadID and IC.RowIndex = I.RowIndex and I.LoadID = @id and IC.Value is not null
													inner join LoadColumn C on C.LoadID = I.LoadID and C.ColumnIndex = IC.ColumnIndex
													inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.IsPartOfKey = 1 and FT.Name = C.Name
										order by	I.RowIndex,
													FT.ID
										) A
							group by	A.RowIndex
							) K on K.RowIndex = T.RowIndex
				inner join	(
							select		RowIndex,
										CONVERT(
											varchar(32), 
											SUBSTRING(HASHBYTES('SHA1', STRING_AGG(cast(FieldTypeID as nvarchar) + ':' + Value, char(59))), 3, 32), 
											2) as FieldHash
							from		(
										select		top 100 percent
													I.RowIndex,
													FT.ID as FieldTypeID,
													coalesce(IC.Value, '') as Value
										from		LoadItem I
													inner join LoadItemColumn IC on IC.LoadID = I.LoadID and IC.RowIndex = I.RowIndex and I.LoadID = @id
													inner join LoadColumn C on C.LoadID = I.LoadID and C.ColumnIndex = IC.ColumnIndex
													inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name
										order by	I.RowIndex,
													FT.ID
										) A
							group by	A.RowIndex	
							) V on V.RowIndex = T.RowIndex
		where	T.LoadID = @id;
	end
	-- -----------------------------
	
	-- Resolve Single-value LOOKUP fields
	exec [bulkload].[UpdateDynamicLookupFieldColumns] @id

	-- Resolve Multi-value LOOKUP fields
	update	IC
	set		IC.LookupObject = MV.LookupObject,
			IC.LookupValue = MV.LookupValue
	from	LoadItemColumn IC
			inner join	(
						select		IC.LoadID,
									IC.RowIndex,
									IC.ColumnIndex,
									'ReferenceItem' as LookupObject,
									string_agg(AD.ID, ',') as LookupValue
						from		LoadItem LI
									inner join LoadItemColumn IC on LI.LoadID = @id and LI.LoadID = IC.LoadID and IC.RowIndex = LI.RowIndex
									inner join LoadColumn C on C.LoadID = IC.LoadID and C.ColumnIndex = IC.ColumnIndex
									inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name and FT.AllowMultipleValues = 1
									cross apply string_split(IC.Value, ',') VS									
									left join ReferenceItem AD on AD.ReferenceITemTypeID = FT.LookupObjectID
									CROSS APPLY [dbo].[GetReferenceItemDisplayValue](AD.ID, FT.ID) GRIDV
						where GRIDV.DisplayValue = ltrim(rtrim(VS.Value))
						group by	IC.LoadID,
									IC.RowIndex,
									IC.ColumnIndex			
						) MV on MV.LoadID = IC.LoadID and MV.RowIndex = IC.RowIndex and MV.ColumnIndex = IC.ColumnIndex

	-- Log error messages for reference list resolution.
	update	LI
	set		LI.StatusMessage = coalesce(LI.StatusMessage,'') + FT.Name + ' could not be resolved to an existing reference item.' 
	from	LoadItem LI
			inner join LoadItemColumn IC on LI.LoadID = @id and IC.LoadID = LI.LoadID and IC.RowIndex = LI.RowIndex
			inner join LoadColumn C on C.ColumnIndex = IC.ColumnIndex and C.LoadID = IC.LoadID
			inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID 
										and FT.Name = C.Name 
										and FT.Type = 'Lookup' 
										and FT.LookupObjectType = 'ReferenceItem' 
										and (FT.IsRequired = 1 or FT.IsPartOfKey = 1) 
										and ( 
												(FT.AllowMultipleValues = 0 AND IC.LookupObjectID is null) OR 
												(FT.AllowMultipleValues = 1 AND IC.LookupValue is null)
											);

	-- Resolve Allow All LOOKUP field values
	update	IC
	set		IC.LookupObject = REPLACE(FT.LookupObjectType, 'Type', ''),
			IC.LookupObjectID = 0,
			IC.LookupValue = 0
	from	LoadItemColumn IC
			inner join LoadColumn C on C.ColumnIndex = IC.ColumnIndex and C.LoadID = IC.LoadID and C.LoadID = @id
			inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name and FT.Type = 'Lookup' and FT.AllowAllValue = 1 and IC.Value = FT.AllowAllLabel;

	-- Resolve RELATIONSHIP fields
	declare @relFieldLookups table (LoadID int, RowIndex int, ColumnIndex int, Object varchar(50), ObjectID int )

	insert into @relFieldLookups
		select	IC.LoadID,
				Ic.RowIndex,
				IC.ColumnIndex,
				D.Object,
				D.ObjectID
		from	LoadItemColumn IC
				inner join LoadColumn C on C.ColumnIndex = IC.ColumnIndex and C.LoadID = IC.LoadID and C.LoadID = @id
				inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name and FT.Type = 'Relationship'
				inner join IntersectType IT on FT.LookupObjectType = 'IntersectType' and FT.LookupObjectID = IT.ID
				inner join AssetType DT on DT.Object = case when IT.Subject = @Object and IT.SubjectID = @ObjectID then IT.Object else IT.Subject end
											and DT.ObjectID = case when IT.Subject = @Object and IT.SubjectID = @ObjectID then IT.ObjectID else IT.SubjectID end
				inner join dbo.GetAssetDisplayValue() D on D.AssetTypeID = DT.ID and D.DisplayValue = ltrim(rtrim(IC.Value));

	update	T
	set		T.LookupObject = S.Object,
			T.LookupObjectID = S.ObjectID
	from	LoadItemColumn T
			inner join @relFieldLookups S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex;


	-- Capture changes for logging purposes.
	--declare @tbl table (ObjectID int, RowIndex int, [Action] varchar(1), [FieldsLoaded] bit null, [RelationshipsLoaded] bit null);

	IF OBJECT_ID('tempdb..#tbl') IS NOT NULL
			DROP TABLE #tbl;

	create table #tbl (ObjectID int, RowIndex int, [Action] varchar(1), [FieldsLoaded] bit null, [RelationshipsLoaded] bit null);
	
	CREATE CLUSTERED INDEX PK_tempTbl ON #tbl ([RowIndex] ASC,[Action] ASC);

	--declare @insertToPerform table (RowID int identity, KeyHash varchar(250));
	IF OBJECT_ID('tempdb..#insertToPerform') IS NOT NULL
			DROP TABLE #insertToPerform;

	create table #insertToPerform (RowID int identity, KeyHash varchar(250));
	
	CREATE CLUSTERED INDEX PK_tempinsertToPerform ON #insertToPerform ([KeyHash] ASC);

	--declare @insertOutputID table (RowID int identity, ObjectID int);
	IF OBJECT_ID('tempdb..#insertOutputID') IS NOT NULL
			DROP TABLE #insertOutputID;

	create table #insertOutputID (RowID int identity, ObjectID int);
	
	-- COMMON ------------------
	-- Identify which load items already exist based on key hash.
	-- oddly wonky
	/*update	T
	set		T.Object = A.Object,
			T.ObjectID = A.ObjectID
	from	LoadItem T
			inner join AssetType ST on ST.Object = @Object and ST.ObjectID = @ObjectID
			inner join GetAssetKeyHash() S on S.AssetTypeID = ST.ID and S.KeyHash = T.KeyHash and T.LoadID = @id
			inner join Asset A on A.ID = S.ID;*/

	/*update	T
	set		T.Object = A.Object,
			T.ObjectID = A.ObjectID
	from	LoadItem T
			inner join AssetType ST on ST.Object = @Object and ST.ObjectID = @ObjectID
			cross apply GetAssetKeyHashById(ST.ID) S 
			inner join Asset A on A.ID = S.ID
	where S.KeyHash = T.KeyHash and T.LoadID = @id*/

	update	T
	set		T.Object = A.Object,
			T.ObjectID = A.ObjectID
	from	LoadItem T
			inner join AssetType ST on ST.Object = @Object and ST.ObjectID = @ObjectID			
			inner join Asset A on A.AssetTypeID = ST.ID
			cross apply GetAssetKeyHashById(A.ID) S 
	where S.KeyHash = T.KeyHash and T.LoadID = @id
	
	-- ARTIFACTS ---------------
	if @Object = 'ArtifactType'
	begin
		-- Mark the existing artifacts as being updated.
		update	T
		set		T.UpdatedBy = @UpdatedBy,
				T.UpdatedOn = @UpdatedOn
		from	Artifact T
				inner join LoadItem S on S.LoadID = @id and S.Object = 'Artifact' and S.ObjectID = T.ID and T.ArtifactTypeID = @ObjectID;

		-- Insert the updated records into temp table for logging.
		insert into #tbl 
			select	ObjectID,
					RowIndex,
					'U', null, null
			from	LoadItem
			where	LoadID = @id 
					and ObjectID is not null;

		-- Insert new items into the Artifact table.
		insert into #insertToPerform
			select	distinct
					KeyHash
			from	LoadItem
			where	LoadID = @id
					and ObjectID is null
					and KeyHash is not null;

		--declare @insertOutputID table (RowID int identity, ObjectID int);
		insert Artifact (ArtifactTypeID, UpdatedOn, UpdatedBy, CreatedOn, CreatedBy)
		output inserted.ID into #insertOutputID
			select	@ObjectID, 
					@UpdatedOn, 
					@UpdatedBy, 
					@UpdatedOn, 
					@UpdatedBy
			from	#insertToPerform;

		-- Insert the added records into temp table for logging.
		insert into #tbl 
			select	N.ObjectID,
					I.RowIndex,
					'A', null, null
			from	LoadItem I
					inner join #insertToPerform P on P.KeyHash = I.KeyHash and I.LoadID = @id 
					inner join #insertOutputID N on N.RowID = P.RowID;

		-- Update the LoadItem table with the Object and ObjectID generated from the insert above.
		update	T
		set		T.Object = 'Artifact',
				T.ObjectID = S.ObjectID
		from	LoadItem T
				inner join #tbl S on T.LoadID = @id and S.RowIndex = T.RowIndex and S.[Action] = 'A';
	end
	-------------------------

	-- MODEL ----------------
   if @Object = 'TaxonomyType'
   begin
		declare 
			@row int, 
			@level int, 
			@rows int, 
			@rowObject varchar(50), 
			@rowObjectId int, 
			@parentKeyHash varchar(50),
			@intersectTypeid int,
			@parentObjectId int;

		declare @ids table (id int);

		set @row = 0;
		set @level = 0;

		while (select count(*) from @levels where processed = 0) > 0
		begin
			set @parentKeyHash = null;
			set @parentObjectId = null;
			delete from @ids;

			--need to process rows in order of level (low to high) to make sure parent items are added or exist
			select		top 1
						@row = L.RowIndex, 
						@level = L.[Level], 
						@rowObject = LC.[Object], 
						@rowObjectId = LC.ObjectID 
			from		@levels L
						inner join LoadItem LC on LC.RowIndex = L.RowIndex and LC.LoadID = @id
			where		L.processed = 0
			order by	L.[Level] asc;
			
			if @rowObjectId is not null
			begin
				update	Taxonomy
				set		UpdatedOn = @UpdatedOn,
						UpdatedBy = @UpdatedBy
				where	ID = @rowObjectId;
			end
			else
			begin
				if @level > 1
				begin
					--hash key fields at (level - 1) and check against asset or LoadItem
					select @parentKeyHash = CONVERT(
									varchar(32), 
									SUBSTRING(HASHBYTES('SHA1', STRING_AGG(cast(FieldTypeID as nvarchar) + ':' + Value, char(59))), 3, 32), 
									2)
					from		(
									select		top 100 percent
												FT.ID as FieldTypeID, 
												coalesce(IC.[Value],'') as [Value] 
									from		LoadColumn LC
												inner join LoadItemColumn IC on IC.LoadID = @id and IC.RowIndex = @row and IC.ColumnIndex = LC.ColumnIndex
												inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.IsPartOfKey = 1 
													and FT.Name = reverse(substring(reverse(LC.[Name]), 0, charindex(' ',reverse(LC.[Name]))))			
									where		LC.LoadID = @id and LC.ColumnIndex in (
			 										select	LC.ColumnIndex 
													from	TaxonomyTypeLevel L
															inner join LoadColumn LC on LC.LoadID = @id and L.Name = substring(LC.[Name], 1, len(LC.[Name]) - charindex(' ', reverse(LC.[Name])))
															inner join LoadItemColumn LI on LI.LoadID = @id and LI.RowIndex = @row and LI.ColumnIndex = LC.ColumnIndex and LI.[Value] is not null
													where	L.TaxonomyTypeID = @ObjectID and L.[Level] = (@level-1)
													)
								) A;

					select @parentObjectId = coalesce(
							(
							select		top 1 
										a.ObjectID 
							from		Asset A
										inner join AssetType T on T.Object = @Object and T.ObjectID = @ObjectID and A.AssetTypeID = T.ID
										inner join GetAssetKeyHash() H on H.ID = A.ID
							where		H.KeyHash = @parentKeyHash
							),
							(
							select		top 1 
										a.ObjectID 
							from		LoadItem L
										inner join Asset A on A.[Object] = L.[Object] and A.ObjectID = L.ObjectID
							where		LoadID = @id and L.KeyHash = @parentKeyHash
							)
						);
					
					if @parentObjectId is not null
					begin
						insert Taxonomy (TaxonomyTypeID, UpdatedOn, UpdatedBy)
						output inserted.ID into @ids
							select	@ObjectID, 
									@UpdatedOn, 
									@UpdatedBy;

						insert into #tbl
						select	id,
								@row,
								'A', null, null
						from	@ids
					
						select  @intersectTypeId = id 
						from	intersecttypedetail 
						where	[subject] = @Object and subjectid = @ObjectID 
								and [object] = @Object and objectid = @objectID
								and predicatetype = 4;
						
						if @intersectTypeId is not null 
							and not exists (
								select		1 
								from		[Intersect] 
								where		IntersectTypeID = @intersectTypeId 
											and ObjectID = (select id from @ids) 
											and SubjectID = @parentObjectId)
						begin						
							insert into [Intersect] (IntersectTypeId, [Subject], [Object], SubjectID, ObjectID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner])
							select	@intersectTypeId as IntersectTypeId,
									'Taxonomy' as [Subject],
									'Taxonomy' as [Object],
									@parentObjectId as SubjectID,
									(select id from @ids) as ObjectID,
									@UpdatedBy as CreatedBy,
									@UpdatedOn as CreatedOn,
									@UpdatedBy as UpdatedBy,
									@UpdatedOn as UpdatedOn,
									'BulkLoad' as [Owner];
						end
					end
				end
				else --root item
				begin			
					insert Taxonomy (TaxonomyTypeID, UpdatedOn, UpdatedBy)
					output inserted.ID into @ids
						select	@ObjectID, 
								@UpdatedOn, 
								@UpdatedBy;

					insert into #tbl
					select	id,
							@row,
							'A', null, null
					from	@ids;									
				end
			end

			update	@levels 
			set		processed = 1 
			where	rowIndex = @row 
					and [level] = @level;

			update	T
			set		T.Object = 'Taxonomy',
					T.ObjectID = S.ObjectID
			from	LoadItem T
					inner join #tbl S on T.LoadID = @id and S.RowIndex = T.RowIndex and S.[Action] = 'A';
		end
	
	end
	--------------------------

	-- REFERENCE ------------
	if @Object = 'ReferenceItemType'
	begin
		declare @ri_insertToPerform table (RowID int identity, Code nvarchar(250), KeyHash varchar(250));
		declare @ri_insertOutputID table (RowID int identity, ObjectID int);

		-- Mark the existing items as being updated.
		update	T
		set		T.UpdatedBy = @UpdatedBy,
				T.UpdatedOn = @UpdatedOn
		from	ReferenceItem T
				inner join LoadItem S on S.LoadID = @id and S.Object = 'ReferenceItem' and S.ObjectID = T.ID and T.ReferenceItemTypeID = @ObjectID;

		-- Insert the updated records into temp table for logging.
		insert into #tbl 
			select	ObjectID,
					RowIndex,
					'U', null, null
			from	LoadItem
			where	LoadID = @id 
					and ObjectID is not null;

		-- Insert new items into the ReferenceItem table.
		insert into @ri_insertToPerform
			select	distinct
					substring(ltrim(rtrim(IC.Value)), 1, 250),
					I.KeyHash
			from	LoadItem I
					inner join LoadColumn C on C.LoadID = I.LoadID and C.Name = 'Code'
					inner join LoadItemColumn IC on C.ColumnIndex = IC.ColumnIndex and IC.LoadID = I.LoadID and IC.RowIndex = I.RowIndex 
			where	I.LoadID = @id
					and I.ObjectID is null
					and I.KeyHash is not null;

		insert ReferenceItem (ReferenceItemTypeID, Code, UpdatedOn, UpdatedBy, CreatedOn, CreatedBy)
		output inserted.ID into @ri_insertOutputID
			select	@ObjectID, 
					Code,
					@UpdatedOn, 
					@UpdatedBy, 
					@UpdatedOn, 
					@UpdatedBy
			from	@ri_insertToPerform;

		-- Insert the added records into temp table for logging.
		insert into #tbl 
			select	N.ObjectID,
					I.RowIndex,
					'A', null, null
			from	LoadItem I
					inner join @ri_insertToPerform P on P.KeyHash = I.KeyHash and I.LoadID = @id 
					inner join @ri_insertOutputID N on N.RowID = P.RowID;

		-- Update the LoadItem table with the Object and ObjectID generated from the insert above.
		update	T
		set		T.Object = 'ReferenceItem',
				T.ObjectID = S.ObjectID
		from	LoadItem T
				inner join #tbl S on T.LoadID = @id and S.RowIndex = T.RowIndex and S.[Action] = 'A';
	end
	-------------------------
	

	-- Capture field logs	
	IF OBJECT_ID('tempdb..#fields') IS NOT NULL
			DROP TABLE #fields;

	create table #fields (RowIndex int, ColumnIndex int, [Action] varchar(25));

	--CREATE CLUSTERED INDEX PK_tempFields ON #fields ([RowIndex] ASC,[ColumnIndex] ASC);

	-- Non-relationship fields
	merge	Field as T
	using	(
			select	I.FieldTypeID,
					I.Type,
					I.AllowMultipleValues,
					I.Object,
					I.ObjectID,
					case 
						when I.Type = 'Lookup' and I.AllowMultipleValues = 0 then cast(C.LookupObjectID as nvarchar)
						when I.Type = 'Lookup' and I.AllowMultipleValues = 1 then C.LookupValue
						else C.Value
					end as [Value],
					C.RowIndex,
					C.ColumnIndex
			from	(
					select		I.LoadID,
								FT.ID as FieldTypeID,
								FT.Type,
								FT.AllowMultipleValues,
								I.Object,
								I.ObjectID,
								min(I.RowIndex) as RowIndex,
								C.ColumnIndex
					from		LoadItem I
								inner join LoadItemColumn C on C.LoadID = I.LoadID and C.RowIndex = I.RowIndex and I.LoadID = @id
								inner join LoadColumn LC on LC.LoadID = I.LoadID and LC.ColumnIndex = C.ColumnIndex
								inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID 
														and  (
															FT.Name = LC.Name or
																(
																	@Object = 'TaxonomyType'
																	 and LC.ColumnIndex in (
																		select LC2.ColumnIndex from TaxonomyTypeLevel L2
																		inner join LoadColumn LC2 on LC2.LoadID = @id and L2.[Name] = substring(LC2.[Name], 1, len(LC2.[Name]) - charindex(' ', reverse(LC2.[Name])))
																		inner join LoadItemColumn LI2 on LI2.LoadID = @id and LI2.RowIndex = C.RowIndex and LI2.ColumnIndex = LC2.ColumnIndex and LI2.[Value] is not null
																		where L2.TaxonomyTypeID = @ObjectID and L2.[Level] = (select [level] from @levels where rowIndex = C.RowIndex)
																	 )
																	 and FT.Name = reverse(substring(reverse(LC.[Name]), 0, charindex(' ',reverse(LC.[Name])))) 
																)
															)
														and FT.Type <> 'Relationship' 
														and ( 
																(FT.Type <> 'Lookup' and C.Value is not null) OR 
																(FT.Type = 'Lookup' and FT.AllowMultipleValues = 0 and C.LookupObjectID is not null) OR
																(FT.Type = 'Lookup' and FT.AllowMultipleValues = 1 and C.LookupValue is not null)
															)
					where		I.ObjectID is not null
					group by	I.LoadID,
								FT.ID,
								FT.Type,
								FT.AllowMultipleValues,
								I.Object,
								I.ObjectID,
								C.ColumnIndex
					) I
					inner join LoadItemColumn C on C.LoadID = I.LoadID and C.RowIndex = I.RowIndex and C.ColumnIndex = I.ColumnIndex
			) S on (T.FieldTypeID = S.FieldTypeID and S.Object = T.ObjectType and S.ObjectID = T.ObjectID)
	when matched then
		update	set
				Value = S.Value
	when not matched then
		insert (FieldTypeID, ObjectType, ObjectID, Value)
		values (S.FieldTypeID, S.Object, S.ObjectID, S.Value)
	output S.RowIndex, S.ColumnIndex, $action into #fields;

	delete	T
	from	FieldValue T
			left join (
				select		FT.ID as FieldTypeID,
							I.Object,
							I.ObjectID,
							VS.Value
				from		LoadItem I
							inner join LoadItemColumn C on C.LoadID = I.LoadID and C.RowIndex = I.RowIndex and I.LoadID = @id and I.ObjectID is not null and C.LookupValue is not null
							cross apply string_split(C.LookupValue, ',') VS
							inner join LoadColumn LC on LC.LoadID = I.LoadID and LC.ColumnIndex = C.ColumnIndex
							inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID 
													and FT.Name = LC.Name 
													and FT.Type = 'Lookup' 
													and FT.AllowMultipleValues = 1
			) S on S.FieldTypeID = T.FieldTypeID and S.Object = T.ObjectType and S.ObjectID = T.ObjectID and S.value = T.Value
			inner join (	--LIMITS THE IMPACT OF THIS STATEMENT
				select		FT.ID as FieldTypeID,
							I.Object,
							I.ObjectID
				from		LoadItem I
							inner join LoadItemColumn C on C.LoadID = I.LoadID and C.RowIndex = I.RowIndex and I.LoadID = @id and I.ObjectID is not null and C.LookupValue is not null
							inner join LoadColumn LC on LC.LoadID = I.LoadID and LC.ColumnIndex = C.ColumnIndex
							inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID 
													and FT.Name = LC.Name 
													and FT.Type = 'Lookup' 
													and FT.AllowMultipleValues = 1			
			) L on L.FieldTypeID = T.FieldTypeID and L.Object = T.ObjectType and L.ObjectID = T.ObjectID
	where	S.FieldTypeID is null;

	insert into FieldValue (FieldTypeID, ObjectType, ObjectID, Value)
		select		FT.ID,
					I.Object,
					I.ObjectID,
					VS.Value
		from		LoadItem I
					inner join LoadItemColumn C on C.LoadID = I.LoadID and C.RowIndex = I.RowIndex and I.LoadID = @id and I.ObjectID is not null and C.LookupValue is not null
					cross apply string_split(C.LookupValue, ',') VS
					inner join LoadColumn LC on LC.LoadID = I.LoadID and LC.ColumnIndex = C.ColumnIndex
					inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID 
											and FT.Name = LC.Name 
											and FT.Type = 'Lookup' 
											and FT.AllowMultipleValues = 1
					left join FieldValue FV on FV.FieldTypeID = FT.ID and FV.ObjectType = I.Object and FV.ObjectID = I.ObjectID and FV.Value = VS.Value
		where		FV.ID is null;

	update	T
	set		T.FieldsLoaded = 1
	from	#tbl T
			inner join	(
						select		RowIndex,
									[Action]
						from		#fields
						group by	RowIndex, 
									[Action]
						) S on S.RowIndex = T.RowIndex;

	truncate table #fields;

	-- Parent fields
	declare @parentTypeID int = null,
			@parentTypeName nvarchar(250) = null;
	declare @parentIntersectTypeId int = null;

	select 
		@parentTypeID = I.SubjectID,
		@parentTypeName = I.SubjectName,
		@parentIntersectTypeId = I.ID
	from 
		intersecttypedetail I                
	where I.[PredicateType] = 3 and [Object] = @Object and ObjectID = @ObjectId;
	
	if @parentTypeID is not null
	begin
	
		-- look for column with the parent type name this contains the parent 
		merge	[Intersect] as T
		using	(
				select	distinct
						AD.ObjectID as ParentObjectID,
						AD.[Object] as ParentObject,
						AD.[TypeID] as ParentTypeID,
						AD.[Type] as ParentType,
						@parentIntersectTypeId as IntersectTypeID,
						LI.[Object] as ItemObject,
						LI.ObjectID as ItemObjectID
				from	LoadItem I
						inner join LoadItemColumn C on C.LoadID = I.LoadID and C.RowIndex = I.RowIndex and I.LoadID = @id
						inner join LoadColumn LC on LC.LoadID = I.LoadID and LC.ColumnIndex = C.ColumnIndex and LC.Name = @parentTypeName
						inner join AssetDetail AD on AD.TypeID = @parentTypeID and AD.DisplayValue = C.Value and AD.[Type] = @Object	
						inner join LoadItem LI on LI.RowIndex = C.RowIndex and LI.LoadID = C.LoadID					
				where	I.ObjectID is not null
				) S on (T.IntersectTypeID = S.IntersectTypeID and T.Subject = S.ParentObject and T.SubjectID = S.ParentObjectID and T.Object = S.ItemObject and T.ObjectID = S.ItemObjectID)

		when matched then
			update	set
					T.Subject	= S.ParentObject,
					T.SubjectID = S.ParentObjectID,
					T.Object	= S.ItemObject,
					T.ObjectID	= S.ItemObjectID
		when not matched then
			insert (IntersectTypeID, Subject, SubjectID, Object, ObjectID, Deleted, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner], Visible)
			values (
					S.IntersectTypeID,
					S.ParentObject, 
					S.ParentObjectID,
					S.ItemObject, 
					S.ItemObjectID, 
					0, @UpdatedBy, @UpdatedOn, @UpdatedBy, @UpdatedOn, 'BulkLoad', 1
					);

	end

	-- Relationship fields
	merge	[Intersect] as T
	using	(
			select	distinct
					FT.LookupObjectID as IntersectTypeID,
					case 
						when IT.Subject = @Object and IT.SubjectID = @ObjectID then cast(1 as bit)
						else cast(0 as bit)
					end as IsSubject,
					case 
						when IT.Subject = @Object and IT.SubjectID = @ObjectID then I.Object
						else C.LookupObject
					end as Subject,
					case 
						when IT.Subject = @Object and IT.SubjectID = @ObjectID then I.ObjectID
						else C.LookupObjectID
					end as SubjectID,
					case 
						when IT.Subject = @Object and IT.SubjectID = @ObjectID then C.LookupObject
						else I.Object
					end as Object,
					case 
						when IT.Subject = @Object and IT.SubjectID = @ObjectID then C.LookupObjectID
						else I.ObjectID
					end as ObjectID
			from	LoadItem I
					inner join LoadItemColumn C on C.LoadID = I.LoadID and C.RowIndex = I.RowIndex and I.LoadID = @id
					inner join LoadColumn LC on LC.LoadID = I.LoadID and LC.ColumnIndex = C.ColumnIndex
					inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID 
											and FT.Name = LC.Name and FT.Type = 'Relationship' 
											and C.LookupObject is not null and C.LookupObjectID is not null
					inner join IntersectType IT on FT.LookupObjectType = 'IntersectType' and FT.LookupObjectID = IT.ID
			where	I.ObjectID is not null
			) S on (
					T.IntersectTypeID = S.IntersectTypeID 
					and (
							(S.IsSubject = 1 and S.Subject = T.Subject and S.SubjectID = T.SubjectID) OR
							(S.IsSubject = 0 and S.Object = T.Object and S.ObjectID = T.ObjectID)
						)
					)
	when matched then
		update	set
				T.Subject	= S.Subject,
				T.SubjectID = S.SubjectID,
				T.Object	= S.Object,
				T.ObjectID	= S.ObjectID
	when not matched then
		insert (IntersectTypeID, Subject, SubjectID, Object, ObjectID, Deleted, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner], Visible)
		values (
				S.IntersectTypeID,
				S.Subject, 
				S.SubjectID,
				S.Object, 
				S.ObjectID, 
				0, @UpdatedBy, @UpdatedOn, @UpdatedBy, @UpdatedOn, 'BulkLoad', 1
				);
	
	-- Capture logs and update load status. -----
	update	T
	set		T.Status = 1,
			T.StatusMessage = 'Item successfully ' + case S.[Action] when 'A' then 'added' else 'updated' end + '.'
	from	LoadItem T
			inner join #tbl S on T.LoadID = @id and S.RowIndex = T.RowIndex and T.[Object] is not null and T.ObjectID is not null;

	update	LoadItem
	set		Status = 0,
			StatusMessage = 'Item load failed. ' + coalesce(StatusMessage, '')
	where	([Object] is null or ObjectID is null)
			and LoadID = @id;

	----Finally, close out the Load.
	update	[Load] 
	set		DateCompleted = getutcdate()
	where	ID = @id
	---------------------------------------------
end
GO

alter procedure [fusion].[GenerateFoundationLineage]
@fusionId int
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

	--	select	ST.FormattedValue as SourceFusionAttributeTypeID,
	--			TT.FormattedValue as TargetFusionAttributeTypeID
	--from		FusionAttribute MA
	--			inner join FieldWithRelation ST on ST.ObjectType = 'FusionAttribute' and ST.ObjectID = MA.ID and ST.Name = 'SourceTypeID'
	--			inner join FieldWithRelation S on S.ObjectType = 'FusionAttribute' and S.ObjectID = MA.ID and S.Name = 'SourceObjectID'
	--			inner join FieldWithRelation TT on TT.ObjectType = 'FusionAttribute' and TT.ObjectID = MA.ID and TT.Name = 'TargetTypeID'
	--			inner join FieldWithRelation T on T.ObjectType = 'FusionAttribute' and T.ObjectID = MA.ID and T.Name = 'TargetObjectID'
	--			inner join FusionAttribute SA on SA.FusionID = MA.FusionID and SA.SourceID = S.FormattedValue --and SA.FusionAttributeTypeID = ST.FormattedValue
	--			inner join FusionAttribute TA on TA.FusionID = MA.FusionID and TA.SourceID = T.FormattedValue --and TA.FusionAttributeTypeID = TT.FormattedValue
	--			left join IntersectTypeDetail IT on IT.Subject = 'FusionAttributeType' and IT.SubjectID = SA.FusionAttributeTypeID and IT.Object = 'FusionAttributeType' and IT.ObjectID = TA.FusionAttributeTypeID and IT.PredicateType = 1
	--where		MA.FusionAttributeTypeID = 1476 --and SA.ID <> TA.ID (slows down query quite a bit)
	--group by	ST.FormattedValue, TT.FormattedValue

	--select * from Predicate
	--update IntersectType set PredicateID = 10 where Subject = 'FusionAttributeType' and Object = 'FusionAttributeType' and PredicateID is null and SubjectID > 1400
	--insert into IntersectType (Subject, SubjectID, Object, ObjectID, IsSystem, State,PredicateID, SubjectCardinality,ObjectCardinality) values ('FusionAttributeType', 1492, 'FusionAttributeType', 1490, 1, 1, 10, 2,2)

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
				inner join FusionAttribute SA on SA.FusionID = MA.FusionID and SA.SourceID = S.FormattedValue --and SA.FusionAttributeTypeID = ST.FormattedValue
				inner join FusionAttribute TA on TA.FusionID = MA.FusionID and TA.SourceID = T.FormattedValue --and TA.FusionAttributeTypeID = TT.FormattedValue
				left join IntersectTypeDetail IT on IT.Subject = 'FusionAttributeType' and IT.SubjectID = SA.FusionAttributeTypeID and IT.Object = 'FusionAttributeType' and IT.ObjectID = TA.FusionAttributeTypeID and IT.PredicateType = 1
	where		MA.FusionAttributeTypeID = 1476 and MA.FusionID = @fusionId --and SA.ID <> TA.ID (slows down query quite a bit)
	group by	SA.ID, TA.ID, IT.ID

--	select * from FusionAttribute where ID in (64479)
--select top 1000 * from [Intersect] where [Owner] = 'FOUNDATION'

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

alter procedure [fusion].[ProcessFusionCacheInQueue]
--declare
	@FusionID int
--set @FusionID = 15
as
begin
	SET NOCOUNT, ANSI_PADDING ON;
	SET ANSI_WARNINGS ON;

	UPDATE  FusionAttribute
	SET		TextPath = utility.GetBreadcrumbStringWrapper('FusionAttribute', ID, '.')
	FROM	FusionAttribute 
	WHERE	FusionID = @FusionID and deleted = 0

	-- if this is a markit fusion update the lineage
	declare @fusionTypeId int;

	select 
		@fusionTypeId = fusiontypeid
	from
		fusion
	where
		id = @FusionID;

	if @fusionTypeId = 13
	begin		
		exec fusion.GenerateMarkitMapLineageData @FusionID
	end
	else if @fusionTypeId = 16
	begin
		exec [fusion].[GenerateEagleLineageData] @FusionID
	end
	else if @fusionTypeId = 111
	begin
		exec [fusion].[GenerateFoundationLineage] @FusionID
	end
	
end
GO

CREATE PROCEDURE [dbo].[GetAssetHierarchy]
  ( @ID int,
    @Type varchar(50) )
	AS
	Begin
	-- declare  @ID int =2430947 ;--4832;-- 16113;
	--declare @Type varchar(50) ='FusionAttribute';--'Taxonomy'; --'Artifact'
    if @Type = 'Artifact'  
	  begin  
	   with Sub as (  
		select O.ID,  
		  O.ParentID,  
		  D.TextPath as [Path],  
		  D.TypeName as LevelName,  
		  D.Url as Url,  
		  1 as [Level]  
		from Artifact O  
		 cross apply [utility].[ObjectDetail](@Type,O.ID) as D  
		where O.ID = @ID  
		union all  
		select O.ID,  
		  O.ParentID,  
		  D.TextPath as [Path], 
		  D.TypeName as LevelName,
		  D.Url as Url,   
		  C.[Level] + 1 as [Level]  
		from Artifact O  
		  inner join Sub as C on C.ParentID = O.ID  
		   cross apply [utility].[ObjectDetail](@Type,O.ID) as D  
	   )  
	  select ID, ParentID,[Path],LevelName,Url,rank() over (Order by Level desc) as [Level] from Sub;
  end
  else if @Type = 'Taxonomy'  
  begin
	   with Sub as (  
		select O.ID,  
		  O.ParentID,  
		  D.TextPath as [Path],  
		  D.TypeName as LevelName,  
		  D.Url as Url,  
		  1 as [Level]  
		from [Taxonomy] O  
		 cross apply [utility].[ObjectDetail](@Type,O.ID) as D  
		where O.ID = @ID  
		union all  
		select O.ID,  
		  O.ParentID,  
		  D.TextPath as [Path], 
		  D.TypeName as LevelName,
		  D.Url as Url,   
		  C.[Level] + 1 as [Level]  
		from [Taxonomy] O  
		  inner join Sub as C on C.ParentID = O.ID  
		   cross apply [utility].[ObjectDetail](@Type,O.ID) as D  
	   )  
	  select ID, ParentID,[Path],LevelName,Url,rank() over (Order by Level desc) as [Level] from Sub;
  end
  else if @Type = 'FusionAttribute'   
  begin  
	   with Sub as (  
		select O.ID,  
		  O.ParentID,  
		  D.TextPath as [Path],  
		  D.TypeName as LevelName,  
		  D.Url as Url,  
		  1 as [Level]  
		from FusionAttribute O  
		 cross apply [utility].[ObjectDetail](@Type,O.ID) as D  
		where O.ID = @ID  
		union all  
		select O.ID,  
		  O.ParentID,  
		  D.TextPath as TextPath, 
		  D.TypeName as LevelName,
		  D.Url as Url,   
		  C.[Level] + 1 as [Level]  
		from FusionAttribute O  
		  inner join Sub as C on C.ParentID = O.ID  
		   cross apply [utility].[ObjectDetail](@Type,O.ID) as D  
	   )  
	  select ID, ParentID,[Path],LevelName,Url,rank() over (Order by Level desc) as [Level] from Sub;
  end
  end
GO

alter table integration.ExecutionAssetType add ProcessedDelete bit constraint DF_IntegrationExecutionAssetType_ProcessedDelete default(0) not null
GO

create procedure integration.ProcessDeletions
as
begin
	DROP TABLE IF EXISTS #fullSynched

	create table #fullSynched (ExecutionID bigint, SynchedAssetTypeID int, CurrentSourceAssetCount int, SourceProcessedCount int)
	insert into #fullSynched
		select		E.ExecutionID,
					E.SynchedAssetTypeID,
					E.CurrentSourceAssetCount,
					count(1) as SourceProcessedCount
		from		integration.ExecutionAssetType E
					inner join	(
								select		Max(ExecutionID) as ExecutionID,
											SynchedAssetTypeID
								from		integration.ExecutionAssetType
								where		IsFullRefresh = 1
											and CompletedOn is not null
								group by	SynchedAssetTypeID
								) ME on ME.ExecutionID = E.ExecutionID and ME.SynchedAssetTypeID = E.SynchedAssetTypeID
					inner join integration.ExecutionAsset A on A.ExecutionID = E.ExecutionID and A.SynchedAssetTypeID = E.SynchedAssetTypeID and E.ProcessedDelete = 0
	where		E.SynchedAssetTypeID <> 20
		group by	E.ExecutionID,
					E.SynchedAssetTypeID,
					E.CurrentSourceAssetCount
	/*
	select	* 
	from	#fullSynched
	*/

	-- Get the full list of assets, whether processed in the last full-synch executions or not.
	DROP TABLE IF EXISTS #targetAssets
	create table #targetAssets (ExecutionID bigint, SynchedAssetTypeID int, AssetID bigint, [Level] int)
	-- First, get ones where there is no level to deal with.
	insert into #targetAssets
		select		F.ExecutionID,
					F.SynchedAssetTypeID,
					A.ID,
					null
		from		#fullSynched F
					inner join integration.SynchedAssetType T on T.ID = F.SynchedAssetTypeID
					inner join Asset A on A.AssetTypeID = T.AssetTypeID
		where		T.[Level] is null
		order by	F.SynchedAssetTypeID

	-- Next, get ones where there is a level to deal with.
	insert into #targetAssets
		select		F.ExecutionID,
					F.SynchedAssetTypeID,
					A.ID,
					L.Level
		from		#fullSynched F
					inner join integration.SynchedAssetType T on T.ID = F.SynchedAssetTypeID
					inner join Asset A on A.AssetTypeID = T.AssetTypeID
					cross apply dbo.GetAssetLevelById(A.ID) L
		where		L.[Level] = T.[Level]
		order by	F.SynchedAssetTypeID,
					L.Level
	/*
	update	T
	set		T.CurrentTargetAssetCount = S.[Count]
	from	integration.ExecutionAssetType T
			inner join ( 
						select		T.ExecutionID,
									T.SynchedAssetTypeID,
									T.[Level],
									Count(1) as [Count]
						from		#targetAssets T
						group by	T.ExecutionID,
									T.SynchedAssetTypeID,
									T.[Level]
						) S on S.ExecutionID = T.ExecutionID and S.SynchedAssetTypeID = T.SynchedAssetTypeID
	*/
	--select * from #targetAssets

	-- Get the full list of assets that were not present in the last successful full synch, so we can delete them.
	DROP TABLE IF EXISTS #deletes
	create table #deletes (ID int identity, AssetID bigint, Object varchar(50), ObjectID int)

	--First, get the deletes where there is no level.
	insert into #deletes
		select		A.ID,
					A.Object,
					A.ObjectID
		from		#targetAssets T
					inner join Asset A on A.ID = T.AssetID
					left join integration.ExecutionAsset EA on EA.ExecutionID = T.ExecutionID and EA.SynchedAssetTypeID = T.SynchedAssetTypeID and A.SourceID = EA.SourceID
		where		T.Level is null
					and EA.SourceID is null

	--Next, get the deletes where there is a valid level.
	insert into #deletes
		select		A.ID,
					A.Object,
					A.ObjectID
		from		#targetAssets T
					inner join Asset A on A.ID = T.AssetID
					cross apply dbo.GetAssetLevelById(A.ID) L
					left join integration.ExecutionAsset EA on EA.ExecutionID = T.ExecutionID and EA.SynchedAssetTypeID = T.SynchedAssetTypeID and A.SourceID = EA.SourceID
		where		EA.SourceID is null
					and T.Level is not null
					and L.Level = T.Level
		order by	T.[Level] desc

	declare @current int = 1,
			@max int,
			@o varchar(50),
			@oID int
	select	@max = max(ID) from #deletes
	while	@current <= @max
	begin
		select	@o = Object, @oID = ObjectID from #deletes where ID  = @current
		exec DeleteObject @o, @oID, 0
		set		@current = @current + 1
	end
	--select * from Asset where AssetTypeID = 13

	--Finally, mark these full refreshed records as having been processed for deletes.
	update	T
	set		T.ProcessedDelete = 1
	from	integration.ExecutionAssetType T
			inner join #fullSynched S on S.ExecutionID = T.ExecutionID and S.SynchedAssetTypeID = T.SynchedAssetTypeID

	--update	integration.ExecutionAssetType
	--set		ProcessedDelete = 1
	--where	IsFullRefresh = 1

	--select		T.ExecutionID,
	--			T.SynchedAssetTypeID,
	--			T.[Level],
	--			Count(1) as [Count]
	--from		#targetAssets T
	--group by	T.ExecutionID,
	--			T.SynchedAssetTypeID,
	--			T.[Level]

	--select		F.SynchedAssetTypeID,
	--			F.CurrentSourceAssetCount,
	--			L.Level,
	--			count(1) as [Count]
	--from		#fullSynched F
	--			inner join integration.SynchedAssetType T on T.ID = F.SynchedAssetTypeID
	--			inner join Asset A on A.AssetTypeID = T.AssetTypeID
	--			cross apply dbo.GetAssetLevelById(A.ID) L
	--group by	F.SynchedAssetTypeID,
	--			F.CurrentSourceAssetCount,
	--			L.Level
	--order by	F.SynchedAssetTypeID,
	--			L.Level
end
GO

CREATE NONCLUSTERED INDEX [IX_Asset_State_AssetTypeID]
    ON [dbo].[Asset]([State] ASC, [AssetTypeID] ASC)
    INCLUDE([ID], [ObjectID]);
GO

CREATE NONCLUSTERED INDEX [IX_Field_ObjectID]
    ON [dbo].[Field]([ObjectID] ASC);
GO

ENABLE TRIGGER [dbo].[IntersectType_AfterInsert]
    ON [dbo].[IntersectType];
GO

DROP INDEX [IX_ReferenceItem_ReferenceItemType_Visible] ON [dbo].[ReferenceItem]
GO

CREATE NONCLUSTERED INDEX [IX_ReferenceItem_ReferenceItemType_Visible]
    ON [dbo].[ReferenceItem]([ReferenceItemTypeID] ASC, [Visible] ASC)
    INCLUDE([ID]);
GO

DROP INDEX [IX_ReferenceItem_Visible] ON [dbo].[ReferenceItem]
GO

CREATE NONCLUSTERED INDEX [IX_ReferenceItem_Visible]
    ON [dbo].[ReferenceItem]([Visible] ASC)
    INCLUDE([ReferenceItemTypeID], [ID]);
GO

ALTER TABLE metrics.[Group] add [State] INT CONSTRAINT [DF_MetricGroup_State] DEFAULT (1) NOT NULL
GO

ALTER TABLE metrics.[Map] add [State] INT CONSTRAINT [DF_MetricGroup_State] DEFAULT (1) NOT NULL
GO

alter procedure [bulkload].[GetLoadColumns]
--declare	
	@action varchar(2),-- = 'P', --P = Promotion, R = Relation, O = Responsibilities, BL = Business Lineage, TL = Technical Lineage
	@type varchar(50),-- = 'ArtifactType',--'ArtifactType',--'IntersectType',--'ArtifactType',
	@id int,-- = 33,
	@getLookups bit = 1
as
begin
	declare @fields table (ID int identity, FieldTypeID int, Name nvarchar(250), Required bit, PartOfKey bit, AllowMultipleValues bit, IsLookup bit)
	declare @lookups table (ID int identity, FieldID int, Value nvarchar(max))
	declare @current int = 1,
			@max int,
			@isLookup bit = 0,
			@fieldTypeID int

	if @action = 'M'
	begin
		if @id = 0 -- Group membership
		begin
			insert into @fields values (-4, 'Action', 1, 0, 0, 1)
			insert into @fields values (0, 'Group Name', 1, 1, 0, 0)
			insert into @fields values (0, 'User Email', 1, 1, 0, 0)

			insert into @lookups values (-4, 'Add')
			insert into @lookups values (-4, 'Remove')
		end

		if @id = 1 -- Add users
		begin
			set @type = 'ResourceType'

			insert into @fields values (-4, 'Status', 1, 1, 0, 0)
			insert into @fields values (0, 'User Email', 1, 1, 0, 0)
			insert into @fields values (0, 'First Name', 1, 1, 0, 0)
			insert into @fields values (0, 'Last Name', 1, 1, 0, 0)

			insert into @lookups values (-4, 'Active')
			insert into @lookups values (-4, 'Inactive')
		end
	end

	if @action = 'O'
	begin
		--	insert into @fields 
			--	select	-1, 'Owner Type', 1, 1, 1

		/*	insert into @lookups
				select	-1,
						'Glossary: ' + Name from ArtifactType order by Name

			insert into @lookups
				select	-1,
						'Model: ' + Name from TaxonomyType order by Name

			insert into @lookups
				select	-1,
						'Policy: ' + Name from PolicyType order by Name*/

		--	insert into @fields 
			--	select	0, 'Owner ID', 1, 1, 0

			insert into @fields 
				select	1, 'Responsibility', 1, 1, 0, 1

			insert into @lookups
				select	1,
						Name from ResponsibilityType order by Name

			insert into @fields 
				select	2, 'Resource', 1, 1, 0, 1

			insert into @lookups
				select	2,
						'User:' + email from reporting.Global_Resource order by email

			insert into @lookups
				select	2,
						'Group:' + Name from [Group] order by Name

			
			begin
				insert into @fields
					select		0,
								'Asset ID', 
								1,
								1,
								0,
								0	
			end
	end

	if @action = 'P'
	begin
		if @type = 'AttributeType'
		begin
			insert into @fields 
				select	-1, 'Owner Type', 1, 1, 0, 1

			insert into @lookups
				select	-1,
						'Glossary: ' + Name from ArtifactType order by Name
			insert into @lookups
				select	-1,
						'Model: ' + Name from TaxonomyType order by Name

			insert into @fields 
				select	0, 'Owner ID', 1, 1, 0, 0
		end --AttributeType

		if @type = 'IntersectType'
		begin
			declare @s varchar(50),
					@sid int,
					@o varchar(50),
					@oid int

			select	@s = Subject,
					@sid = SubjectID,
					@o = Object,
					@oid = ObjectID
			from	IntersectType
			where	ID = @id


			if @s = 'TaxonomyType'
			begin
				insert into @fields
					select	0, 
							'Subject Path', 
							1, 
							1, 
							0,
							0
			end
			else
			begin
				insert into @fields
					select	FT.ID, 
							'Subject ' + FT.Name, 
							FT.IsRequired, 
							FT.IsPartOfKey, 
							FT.AllowMultipleValues,
							case FT.Type
								when 'Lookup' then cast(1 as bit)
								else cast(0 as bit)
							end as IsLookup
					from	FieldType FT 
					where	FT.IsPartOfKey = 1 and FT.Object = @s and FT.ObjectID = @sid
			end

			if @s = 'TaxonomyType'
			begin
				insert into @fields
					select	0, 
							'Object Path', 
							1, 
							1, 
							0,
							0
			end
			else
			begin
				insert into @fields
					select	FT.ID, 
							'Object ' + FT.Name, 
							FT.IsRequired, 
							FT.IsPartOfKey, 
							FT.AllowMultipleValues,
							case FT.Type
								when 'Lookup' then cast(1 as bit)
								else cast(0 as bit)
							end as IsLookup
					from	FieldType FT 
					where	FT.IsPartOfKey = 1 and FT.Object = @o and FT.ObjectID = @oid
			end

		end --IntersectType

		if @type = 'ArtifactType'
		begin
			declare @parentTypeID int = null,
					@parentTypeName nvarchar(250) = null
			
			/*select	@parentTypeID = T.ParentID,
					@parentTypeName = P.Name
			from	ArtifactType T 
					inner join ArtifactType P on P.ID = T.ParentID
			where	T.ID = @id*/

			select 
				@parentTypeID = I.SubjectID,
				@parentTypeName = I.SubjectName
			from 
				intersecttypedetail I                
			where I.[PredicateType] = 3 and [Object] = @type and ObjectID = @id;

			if @parentTypeID is not null
			begin
				insert into @fields 
					values(	0, 
							@parentTypeName, 
							cast(1 as bit), 
							cast(1 as bit), 
							cast(0 as bit),
							cast(1 as bit) );
				
				insert into @lookups
					select	(select id from @fields where fieldtypeid = 0), DisplayValue from AssetDetail where typeid = @parentTypeID and [object] = 'Artifact' order by DisplayValue;

			end
		end --ArtifactType

		if @type = 'ReferenceItemType'
		begin
			insert into @fields values (0, 'Code', 1, 1, 0, 0)
		end --ReferenceItemType

		if @type = 'TaxonomyType'
		begin
			declare @initialDepth int = 1,
					@maxDepth int = 1
			select @maxDepth = MaximumDepth from TaxonomyType where ID = @id
			declare @levels table (Value int)
			while  @initialDepth <= @maxDepth
			begin
				insert into @levels values (@initialDepth)
				set @initialDepth = @initialDepth + 1
			end

			insert into @fields 
				select	FT.ID, 
						case
							when TTL.Name is not null then TTL.Name + ' ' + FT.Name
							else 'Level ' + cast(L.Value as nvarchar)  + ' ' + FT.Name
						end, 
						FT.IsRequired, 
						FT.IsPartOfKey, 
						FT.AllowMultipleValues,
						case FT.Type
							when 'Lookup' then cast(1 as bit)
							else cast(0 as bit)
						end as IsLookup
				from	@levels L 
						inner join FieldType FT on FT.IsPartOfKey = 1 and FT.Object = @type and FT.ObjectID = @id
						left join TaxonomyTypeLevel TTL on TTL.[Level] = L.Value and TaxonomyTypeID = @id
		end --TaxonomyType		
	end -- P	
	else if (@action = 'R' or @action = 'U')
	begin
		--relate / unrelate
		print 'relate / unrelate'
				
		-- look up the intersect type and get the source / target type
		
		declare @subjectType varchar(50),
				@subjectTypeName nvarchar(500),
				@subjectTypeID int,
				@objectType varchar(50),
				@objectTypeName nvarchar(500),
				@objectTypeID int
		select	@subjectType = Subject,
				@subjectTypeName = SubjectName,
				@subjectTypeID = SubjectID,
				@objectTypeName = ObjectName,
				@objectType = Object,
				@objectTypeID = ObjectID
		from	IntersectTypeDetail
		where	ID = @id
		

		-- if its a fusion attribute type we just use the name

		-- get the key fields for the target / source		

		if @objectType = 'FusionAttributeType' or @objectType = 'IntersectType'
		begin
			insert into @fields values (0, @objectTypeName, 1, 1, 0, 0)
		end		
		else if @objectType = 'ReferenceItemType' and @objectTypeID = 0
		begin
			insert into @fields values (0, @objectTypeName + ' Asset Type ID', 1, 1, 0, 0)
		end		
		else
		begin
			--select * from fieldtype where [object] = 'ArtifactType' and objectid = 1 and IsPartOfKey = 1
			insert into @fields
				select		0,
							@objectTypeName + ' Asset ID', 
							1,
							1,
							0,
							0				
		end

		if @subjectType = 'FusionAttributeType' or @subjectType = 'IntersectType'
		begin
			insert into @fields values (0, @subjectTypeName, 1, 1, 0, 0)
		end
		else if @subjectType = 'ReferenceItemType' and @subjectTypeID = 0
		begin
			insert into @fields values (0, @subjectTypeName + ' Asset Type ID', 1, 1, 0, 0)
		end		
		else
		begin
			insert into @fields
				select		0,
							@subjectTypeName + ' Asset ID', 
							1,
							1,
							0,
							0				
		end
	end -- R or U

	-- fields on the item
	if ((@action = 'M' and @id = 1 ) or @action = 'R' or @action = 'P')
	begin
		insert into @fields
			select		ID,
						Name, 
						IsRequired,
						IsPartOfKey,
						AllowMultipleValues,
						case Type
							when 'Lookup' then cast(1 as bit)
							else cast(0 as bit)
						end as IsLookup
			from		FieldType 
			where		Object = @type 
						and ObjectID = @id 
						and Type not in ('Attribute', 'ComplexRelationLookup', 'FieldFromRelationship', 'FilteredLookup', 'FusionLookup', 'OwnershipLookup', 'RefListRelationship')
						and ( (@type = 'IntersectType' and IsPartOfKey = 0) OR (@type = 'TaxonomyType' and IsPartOfKey = 0) OR (@type <> 'TaxonomyType') )
						and IsEditable = 1
			order by	ColumnOrder
		
		select @max = max(ID) from @fields

		while @current <= @max
		begin
			select	@isLookup = IsLookup, 
					@fieldTypeID = FieldTypeID
			from	@fields 
			where	ID = @current

			if @isLookup = 1 and @getLookups = 1
			begin
				insert into @lookups
					select		@current,
								[Text]
					from		FieldLookupValue
					where		FieldTypeID = @fieldTypeID
					order by	[Text]
			end
			
			set @current = @current + 1
		end
	end

	
	if @action = 'BL'
	begin

			insert into @fields values (-4, 'Action', 1, 0, 0, 1)
			insert into @fields values (-2, 'Source Relation', 1, 1, 0, 1)
			insert into @fields values (0, 'Source Subject Asset ID', 1, 1, 0, 0)
			insert into @fields values (0, 'Source Object Asset ID', 1, 1, 0, 0)
			insert into @fields values (-3, 'Source Fusion Configuration', 1, 0, 0, 1)
			insert into @fields values (0, 'Source Fusion Path', 1, 0, 0, 0)

			insert into @fields values (-2, 'Target Relation', 1, 1, 0, 1)
			insert into @fields values (0, 'Target Subject Asset ID', 1, 1, 0, 0)
			insert into @fields values (0, 'Target Object Asset ID', 1, 1, 0, 0)
			insert into @fields values (-3, 'Target Fusion Configuration', 1, 0, 0, 1)
			insert into @fields values (0, 'Target Fusion Path', 1, 0, 0, 0)

			insert into @fields values (0, 'Transformation', 1, 0, 0, 0)

			insert into @lookups values (-4, 'Add')
			insert into @lookups values (-4, 'Remove')

			insert into @lookups
				select		-1,
							Name 
				from		TaxonomyType 
				order by	Name

			insert into @lookups
				select		-2,
							Name 
				from		IntersectType
				where		IsSystem = 0
				order by	Name

			insert into @lookups
				select		-3,
							Name 
				from		Fusion
				order by	Name
	end

	if @action = 'TL'
	begin
		insert into @fields values (-1, 'Source Fusion Configuration', 0, 0, 0, 1)
		insert into @fields values (0, 'Source Fusion Path', 0, 0, 0, 0)

		insert into @fields values (-1, 'Target Fusion Configuration', 0, 0, 0, 1)
		insert into @fields values (0, 'Target Fusion Path', 0, 0, 0, 0)

		insert into @fields values (0, 'Group', 0, 0, 0, 0)

		insert into @lookups
			select		-1,
						Name 
			from		Fusion
			order by	Name
	end

	--Return the data
	select	Name,
			Required,
			PartOfKey,
			AllowMultipleValues,
			IsLookup,
			(
			select	Value
			from	@lookups
			where	FieldID = F.ID
			for json path
			) as Lookups
	from	@fields F
	for json path
end
GO

alter procedure [bulkload].[Promotions]
--declare
	@id int
--set @id = 84
as
begin
	set nocount on;

	declare @levels table (rowIndex int, [level] int, processed bit);

	declare @Object varchar(50),
			@ObjectID int,
			@Action varchar(1),
			@UpdatedOn datetime = getutcdate(),
			@UpdatedBy int = 0
				
	select	@Object = [Object], 
			@ObjectID = ObjectID,
			@Action = [Action],
			@UpdatedBy = UpdatedBy
	from	[Load]
	where	ID = @id;

	update	LoadItem
	set		Object = null, 
			ObjectID = null, 
			Status = null,
			StatusMessage = null
	where	LoadID = @id;

	-- Process hashes for Load Items
	if @Object = 'ReferenceItemType'
	begin		
		update	T
		set		T.KeyHash = CONVERT(
									varchar(32), 
									SUBSTRING(HASHBYTES('SHA1', substring(ltrim(rtrim(IC.Value)), 1, 250)), 3, 32), 
									2),
				T.FieldHash = V.FieldHash
		from	LoadItem T
				inner join LoadColumn C on C.LoadID = T.LoadID and C.Name = 'Code'
				inner join LoadItemColumn IC on IC.LoadID = C.LoadID and IC.RowIndex = T.RowIndex and IC.ColumnIndex = C.ColumnIndex
				inner join	(
							select		RowIndex,
										CONVERT(
											varchar(32), 
											SUBSTRING(HASHBYTES('SHA1', STRING_AGG(cast(FieldTypeID as nvarchar) + ':' + Value, char(59))), 3, 32), 
											2) as FieldHash
							from		(
										select		top 100 percent
													I.RowIndex,
													FT.ID as FieldTypeID,
													coalesce(IC.Value, '') as Value
										from		LoadItem I
													inner join LoadItemColumn IC on IC.LoadID = I.LoadID and IC.RowIndex = I.RowIndex and I.LoadID = @id
													inner join LoadColumn C on C.LoadID = I.LoadID and C.ColumnIndex = IC.ColumnIndex													
													left join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name and C.Name !='Code'
													left join dbo.ReferenceItem RI on C.Name = 'Code' and RI.ID = @ObjectID
										order by	I.RowIndex,
													FT.ID
										) A
							group by	A.RowIndex	
							) V on V.RowIndex = T.RowIndex
		where	T.LoadID = @id;
	end
	else if @Object = 'TaxonomyType'
	begin
		declare @currRow int, @maxRow int, @currLevel int;
		set @currRow = 1;
		set @currLevel = 0;
		set @maxRow = (select max(RowIndex) from LoadItem where LoadID = @id);	

		while @currRow < @maxRow
		begin
			set @currRow = @currRow + 1;

			--get level for current row
			select		@currLevel = coalesce(max(L.[Level]), 1) 
			from		TaxonomyTypeLevel L
						inner join LoadColumn LC on LC.LoadID = @id and L.Name = substring(LC.[Name], 1, len(LC.[Name]) - charindex(' ', reverse(LC.[Name])))
						inner join LoadItemColumn LI on LI.LoadID = @id and LI.RowIndex = @currRow and LI.ColumnIndex = LC.ColumnIndex and LI.[Value] is not null
			where		L.TaxonomyTypeID = @ObjectID

			insert into @levels (rowIndex, level, processed) values (@currRow, @currLevel, 0);

			--update the key hash based on the current level
			update	T
			set		T.KeyHash = K.KeyHash,
					T.FieldHash = V.FieldHash
			from	LoadItem T
					left join	(
								select		RowIndex,
											CONVERT(
												varchar(32), 
												SUBSTRING(HASHBYTES('SHA1', STRING_AGG(cast(FieldTypeID as nvarchar) + ':' + Value, char(59))), 3, 32), 
												2) as KeyHash
								from		(
												select top 100 percent
													IC.RowIndex, 
													FT.ID as FieldTypeID, 
													coalesce(IC.[Value],'') as [Value] 
												from LoadColumn LC
												inner join LoadItemColumn IC on IC.LoadID = @id and IC.RowIndex = @currRow and IC.ColumnIndex = LC.ColumnIndex
												inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.IsPartOfKey = 1 and FT.Name = reverse(substring(reverse(LC.[Name]), 0, charindex(' ',reverse(LC.[Name]))))			
												where LC.LoadID = @id and LC.ColumnIndex in (
			 										select		LC.ColumnIndex 
													from		TaxonomyTypeLevel L
																inner join LoadColumn LC on LC.LoadID = @id and L.Name = substring(LC.[Name], 1, len(LC.[Name]) - charindex(' ', reverse(LC.[Name])))
																inner join LoadItemColumn LI on LI.LoadID = @id and LI.RowIndex = @currRow and LI.ColumnIndex = LC.ColumnIndex and LI.[Value] is not null
													where		L.TaxonomyTypeID = @ObjectID and L.[Level] = @currLevel
													)
											) A
								group by	A.RowIndex
								) K on K.RowIndex = T.RowIndex
					inner join	(
								select		RowIndex,
											CONVERT(
												varchar(32), 
												SUBSTRING(HASHBYTES('SHA1', STRING_AGG(cast(FieldTypeID as nvarchar) + ':' + Value, char(59))), 3, 32), 
												2) as FieldHash
								from		(
											select		top 100 percent
														I.RowIndex,
														FT.ID as FieldTypeID,
														coalesce(IC.Value, '') as Value
											from		LoadItem I
														inner join LoadItemColumn IC on IC.LoadID = I.LoadID and IC.RowIndex = I.RowIndex and I.LoadID = @id
														inner join LoadColumn C on C.LoadID = I.LoadID and C.ColumnIndex = IC.ColumnIndex
														inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name
											order by	I.RowIndex,
														FT.ID
											) A
								group by	A.RowIndex	
								) V on V.RowIndex = T.RowIndex
			where	T.LoadID = @id and T.RowIndex = @currRow;
		end
	end
	else
	begin
		update	T
		set		T.KeyHash = K.KeyHash,
				T.FieldHash = V.FieldHash
		from	LoadItem T
				inner join	(
							select		RowIndex,
										CONVERT(
											varchar(32), 
											SUBSTRING(HASHBYTES('SHA1', STRING_AGG(cast(FieldTypeID as nvarchar) + ':' + Value, char(59))), 3, 32), 
											2) as KeyHash
							from		(
										select		top 1000000000 --percent
													I.RowIndex,
													FT.ID as FieldTypeID,
													coalesce(IC.Value, '') as Value
										from		LoadItem I
													inner join LoadItemColumn IC on IC.LoadID = I.LoadID and IC.RowIndex = I.RowIndex and I.LoadID = @id and IC.Value is not null
													inner join LoadColumn C on C.LoadID = I.LoadID and C.ColumnIndex = IC.ColumnIndex
													inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.IsPartOfKey = 1 and FT.Name = C.Name
										order by	I.RowIndex,
													FT.ID
										) A
							group by	A.RowIndex
							) K on K.RowIndex = T.RowIndex
				inner join	(
							select		RowIndex,
										CONVERT(
											varchar(32), 
											SUBSTRING(HASHBYTES('SHA1', STRING_AGG(cast(FieldTypeID as nvarchar) + ':' + Value, char(59))), 3, 32), 
											2) as FieldHash
							from		(
										select		top 100 percent
													I.RowIndex,
													FT.ID as FieldTypeID,
													coalesce(IC.Value, '') as Value
										from		LoadItem I
													inner join LoadItemColumn IC on IC.LoadID = I.LoadID and IC.RowIndex = I.RowIndex and I.LoadID = @id
													inner join LoadColumn C on C.LoadID = I.LoadID and C.ColumnIndex = IC.ColumnIndex
													inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name
										order by	I.RowIndex,
													FT.ID
										) A
							group by	A.RowIndex	
							) V on V.RowIndex = T.RowIndex
		where	T.LoadID = @id;
	end
	-- -----------------------------
	
	-- Resolve Single-value LOOKUP fields
	exec [bulkload].[UpdateDynamicLookupFieldColumns] @id

	-- Resolve Multi-value LOOKUP fields
	update	IC
	set		IC.LookupObject = MV.LookupObject,
			IC.LookupValue = MV.LookupValue
	from	LoadItemColumn IC
			inner join	(
						select		IC.LoadID,
									IC.RowIndex,
									IC.ColumnIndex,
									'ReferenceItem' as LookupObject,
									string_agg(AD.ID, ',') as LookupValue
						from		LoadItem LI
									inner join LoadItemColumn IC on LI.LoadID = @id and LI.LoadID = IC.LoadID and IC.RowIndex = LI.RowIndex
									inner join LoadColumn C on C.LoadID = IC.LoadID and C.ColumnIndex = IC.ColumnIndex
									inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name and FT.AllowMultipleValues = 1
									cross apply string_split(IC.Value, ',') VS									
									left join ReferenceItem AD on AD.ReferenceITemTypeID = FT.LookupObjectID
									CROSS APPLY [dbo].[GetReferenceItemDisplayValue](AD.ID, FT.ID) GRIDV
						where GRIDV.DisplayValue = ltrim(rtrim(VS.Value))
						group by	IC.LoadID,
									IC.RowIndex,
									IC.ColumnIndex			
						) MV on MV.LoadID = IC.LoadID and MV.RowIndex = IC.RowIndex and MV.ColumnIndex = IC.ColumnIndex

	-- Log error messages for reference list resolution.
	update	LI
	set		LI.StatusMessage = coalesce(LI.StatusMessage,'') + FT.Name + ' could not be resolved to an existing reference item.' 
	from	LoadItem LI
			inner join LoadItemColumn IC on LI.LoadID = @id and IC.LoadID = LI.LoadID and IC.RowIndex = LI.RowIndex
			inner join LoadColumn C on C.ColumnIndex = IC.ColumnIndex and C.LoadID = IC.LoadID
			inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID 
										and FT.Name = C.Name 
										and FT.Type = 'Lookup' 
										and FT.LookupObjectType = 'ReferenceItem' 
										and (FT.IsRequired = 1 or FT.IsPartOfKey = 1) 
										and ( 
												(FT.AllowMultipleValues = 0 AND IC.LookupObjectID is null) OR 
												(FT.AllowMultipleValues = 1 AND IC.LookupValue is null)
											);

	-- Resolve Allow All LOOKUP field values
	update	IC
	set		IC.LookupObject = REPLACE(FT.LookupObjectType, 'Type', ''),
			IC.LookupObjectID = 0,
			IC.LookupValue = 0
	from	LoadItemColumn IC
			inner join LoadColumn C on C.ColumnIndex = IC.ColumnIndex and C.LoadID = IC.LoadID and C.LoadID = @id
			inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name and FT.Type = 'Lookup' and FT.AllowAllValue = 1 and IC.Value = FT.AllowAllLabel;

	-- Resolve RELATIONSHIP fields
	declare @relFieldLookups table (LoadID int, RowIndex int, ColumnIndex int, Object varchar(50), ObjectID int )

	insert into @relFieldLookups
		select	IC.LoadID,
				Ic.RowIndex,
				IC.ColumnIndex,
				D.Object,
				D.ObjectID
		from	LoadItemColumn IC
				inner join LoadColumn C on C.ColumnIndex = IC.ColumnIndex and C.LoadID = IC.LoadID and C.LoadID = @id
				inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name and FT.Type = 'Relationship'
				inner join IntersectType IT on FT.LookupObjectType = 'IntersectType' and FT.LookupObjectID = IT.ID
				inner join AssetType DT on DT.Object = case when IT.Subject = @Object and IT.SubjectID = @ObjectID then IT.Object else IT.Subject end
											and DT.ObjectID = case when IT.Subject = @Object and IT.SubjectID = @ObjectID then IT.ObjectID else IT.SubjectID end
				inner join dbo.GetAssetDisplayValue() D on D.AssetTypeID = DT.ID and D.DisplayValue = ltrim(rtrim(IC.Value));

	update	T
	set		T.LookupObject = S.Object,
			T.LookupObjectID = S.ObjectID
	from	LoadItemColumn T
			inner join @relFieldLookups S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex;


	-- Capture changes for logging purposes.
	--declare @tbl table (ObjectID int, RowIndex int, [Action] varchar(1), [FieldsLoaded] bit null, [RelationshipsLoaded] bit null);

	IF OBJECT_ID('tempdb..#tbl') IS NOT NULL
			DROP TABLE #tbl;

	create table #tbl (ObjectID int, RowIndex int, [Action] varchar(1), [FieldsLoaded] bit null, [RelationshipsLoaded] bit null);
	
	CREATE CLUSTERED INDEX PK_tempTbl ON #tbl ([RowIndex] ASC,[Action] ASC);

	--declare @insertToPerform table (RowID int identity, KeyHash varchar(250));
	IF OBJECT_ID('tempdb..#insertToPerform') IS NOT NULL
			DROP TABLE #insertToPerform;

	create table #insertToPerform (RowID int identity, KeyHash varchar(250));
	
	CREATE CLUSTERED INDEX PK_tempinsertToPerform ON #insertToPerform ([KeyHash] ASC);

	--declare @insertOutputID table (RowID int identity, ObjectID int);
	IF OBJECT_ID('tempdb..#insertOutputID') IS NOT NULL
			DROP TABLE #insertOutputID;

	create table #insertOutputID (RowID int identity, ObjectID int);
	
	-- COMMON ------------------
	-- Identify which load items already exist based on key hash.
	-- oddly wonky
	/*update	T
	set		T.Object = A.Object,
			T.ObjectID = A.ObjectID
	from	LoadItem T
			inner join AssetType ST on ST.Object = @Object and ST.ObjectID = @ObjectID
			inner join GetAssetKeyHash() S on S.AssetTypeID = ST.ID and S.KeyHash = T.KeyHash and T.LoadID = @id
			inner join Asset A on A.ID = S.ID;*/

	/*update	T
	set		T.Object = A.Object,
			T.ObjectID = A.ObjectID
	from	LoadItem T
			inner join AssetType ST on ST.Object = @Object and ST.ObjectID = @ObjectID
			cross apply GetAssetKeyHashById(ST.ID) S 
			inner join Asset A on A.ID = S.ID
	where S.KeyHash = T.KeyHash and T.LoadID = @id*/

	update	T
	set		T.Object = A.Object,
			T.ObjectID = A.ObjectID
	from	LoadItem T
			inner join AssetType ST on ST.Object = @Object and ST.ObjectID = @ObjectID			
			inner join Asset A on A.AssetTypeID = ST.ID
			cross apply GetAssetKeyHashById(A.ID) S 
	where S.KeyHash = T.KeyHash and T.LoadID = @id
	
	-- ARTIFACTS ---------------
	if @Object = 'ArtifactType'
	begin
		-- Mark the existing artifacts as being updated.
		update	T
		set		T.UpdatedBy = @UpdatedBy,
				T.UpdatedOn = @UpdatedOn
		from	Artifact T
				inner join LoadItem S on S.LoadID = @id and S.Object = 'Artifact' and S.ObjectID = T.ID and T.ArtifactTypeID = @ObjectID;

		-- Insert the updated records into temp table for logging.
		insert into #tbl 
			select	ObjectID,
					RowIndex,
					'U', null, null
			from	LoadItem
			where	LoadID = @id 
					and ObjectID is not null;

		-- Insert new items into the Artifact table.
		insert into #insertToPerform
			select	distinct
					KeyHash
			from	LoadItem
			where	LoadID = @id
					and ObjectID is null
					and KeyHash is not null;

		--declare @insertOutputID table (RowID int identity, ObjectID int);
		insert Artifact (ArtifactTypeID, UpdatedOn, UpdatedBy, CreatedOn, CreatedBy)
		output inserted.ID into #insertOutputID
			select	@ObjectID, 
					@UpdatedOn, 
					@UpdatedBy, 
					@UpdatedOn, 
					@UpdatedBy
			from	#insertToPerform;

		-- Insert the added records into temp table for logging.
		insert into #tbl 
			select	N.ObjectID,
					I.RowIndex,
					'A', null, null
			from	LoadItem I
					inner join #insertToPerform P on P.KeyHash = I.KeyHash and I.LoadID = @id 
					inner join #insertOutputID N on N.RowID = P.RowID;

		-- Update the LoadItem table with the Object and ObjectID generated from the insert above.
		update	T
		set		T.Object = 'Artifact',
				T.ObjectID = S.ObjectID
		from	LoadItem T
				inner join #tbl S on T.LoadID = @id and S.RowIndex = T.RowIndex and S.[Action] = 'A';
	end
	-------------------------

	-- MODEL ----------------
   if @Object = 'TaxonomyType'
   begin
		declare 
			@row int, 
			@level int, 
			@rows int, 
			@rowObject varchar(50), 
			@rowObjectId int, 
			@parentKeyHash varchar(50),
			@intersectTypeid int,
			@parentObjectId int;

		declare @ids table (id int);

		set @row = 0;
		set @level = 0;

		while (select count(*) from @levels where processed = 0) > 0
		begin
			set @parentKeyHash = null;
			set @parentObjectId = null;
			delete from @ids;

			--need to process rows in order of level (low to high) to make sure parent items are added or exist
			select		top 1
						@row = L.RowIndex, 
						@level = L.[Level], 
						@rowObject = LC.[Object], 
						@rowObjectId = LC.ObjectID 
			from		@levels L
						inner join LoadItem LC on LC.RowIndex = L.RowIndex and LC.LoadID = @id
			where		L.processed = 0
			order by	L.[Level] asc;
			
			if @rowObjectId is not null
			begin
				update	Taxonomy
				set		UpdatedOn = @UpdatedOn,
						UpdatedBy = @UpdatedBy
				where	ID = @rowObjectId;
			end
			else
			begin
				if @level > 1
				begin
					--hash key fields at (level - 1) and check against asset or LoadItem
					select @parentKeyHash = CONVERT(
									varchar(32), 
									SUBSTRING(HASHBYTES('SHA1', STRING_AGG(cast(FieldTypeID as nvarchar) + ':' + Value, char(59))), 3, 32), 
									2)
					from		(
									select		top 100 percent
												FT.ID as FieldTypeID, 
												coalesce(IC.[Value],'') as [Value] 
									from		LoadColumn LC
												inner join LoadItemColumn IC on IC.LoadID = @id and IC.RowIndex = @row and IC.ColumnIndex = LC.ColumnIndex
												inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID and FT.IsPartOfKey = 1 
													and FT.Name = reverse(substring(reverse(LC.[Name]), 0, charindex(' ',reverse(LC.[Name]))))			
									where		LC.LoadID = @id and LC.ColumnIndex in (
			 										select	LC.ColumnIndex 
													from	TaxonomyTypeLevel L
															inner join LoadColumn LC on LC.LoadID = @id and L.Name = substring(LC.[Name], 1, len(LC.[Name]) - charindex(' ', reverse(LC.[Name])))
															inner join LoadItemColumn LI on LI.LoadID = @id and LI.RowIndex = @row and LI.ColumnIndex = LC.ColumnIndex and LI.[Value] is not null
													where	L.TaxonomyTypeID = @ObjectID and L.[Level] = (@level-1)
													)
								) A;

					select @parentObjectId = coalesce(
							(
							select		top 1 
										a.ObjectID 
							from		Asset A
										inner join AssetType T on T.Object = @Object and T.ObjectID = @ObjectID and A.AssetTypeID = T.ID
										inner join GetAssetKeyHash() H on H.ID = A.ID
							where		H.KeyHash = @parentKeyHash
							),
							(
							select		top 1 
										a.ObjectID 
							from		LoadItem L
										inner join Asset A on A.[Object] = L.[Object] and A.ObjectID = L.ObjectID
							where		LoadID = @id and L.KeyHash = @parentKeyHash
							)
						);
					
					if @parentObjectId is not null
					begin
						insert Taxonomy (TaxonomyTypeID, UpdatedOn, UpdatedBy)
						output inserted.ID into @ids
							select	@ObjectID, 
									@UpdatedOn, 
									@UpdatedBy;

						insert into #tbl
						select	id,
								@row,
								'A', null, null
						from	@ids
					
						select  @intersectTypeId = id 
						from	intersecttypedetail 
						where	[subject] = @Object and subjectid = @ObjectID 
								and [object] = @Object and objectid = @objectID
								and predicatetype = 4;
						
						if @intersectTypeId is not null 
							and not exists (
								select		1 
								from		[Intersect] 
								where		IntersectTypeID = @intersectTypeId 
											and ObjectID = (select id from @ids) 
											and SubjectID = @parentObjectId)
						begin						
							insert into [Intersect] (IntersectTypeId, [Subject], [Object], SubjectID, ObjectID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner])
							select	@intersectTypeId as IntersectTypeId,
									'Taxonomy' as [Subject],
									'Taxonomy' as [Object],
									@parentObjectId as SubjectID,
									(select id from @ids) as ObjectID,
									@UpdatedBy as CreatedBy,
									@UpdatedOn as CreatedOn,
									@UpdatedBy as UpdatedBy,
									@UpdatedOn as UpdatedOn,
									'BulkLoad' as [Owner];
						end
					end
				end
				else --root item
				begin			
					insert Taxonomy (TaxonomyTypeID, UpdatedOn, UpdatedBy)
					output inserted.ID into @ids
						select	@ObjectID, 
								@UpdatedOn, 
								@UpdatedBy;

					insert into #tbl
					select	id,
							@row,
							'A', null, null
					from	@ids;									
				end
			end

			update	@levels 
			set		processed = 1 
			where	rowIndex = @row 
					and [level] = @level;

			update	T
			set		T.Object = 'Taxonomy',
					T.ObjectID = S.ObjectID
			from	LoadItem T
					inner join #tbl S on T.LoadID = @id and S.RowIndex = T.RowIndex and S.[Action] = 'A';
		end
	
	end
	--------------------------

	-- REFERENCE ------------
	if @Object = 'ReferenceItemType'
	begin
		declare @ri_insertToPerform table (RowID int identity, Code nvarchar(250), KeyHash varchar(250));
		declare @ri_insertOutputID table (RowID int identity, ObjectID int);

		-- Mark the existing items as being updated.
		update	T
		set		T.UpdatedBy = @UpdatedBy,
				T.UpdatedOn = @UpdatedOn
		from	ReferenceItem T
				inner join LoadItem S on S.LoadID = @id and S.Object = 'ReferenceItem' and S.ObjectID = T.ID and T.ReferenceItemTypeID = @ObjectID;

		-- Insert the updated records into temp table for logging.
		insert into #tbl 
			select	ObjectID,
					RowIndex,
					'U', null, null
			from	LoadItem
			where	LoadID = @id 
					and ObjectID is not null;

		-- Insert new items into the ReferenceItem table.
		insert into @ri_insertToPerform
			select	distinct
					substring(ltrim(rtrim(IC.Value)), 1, 250),
					I.KeyHash
			from	LoadItem I
					inner join LoadColumn C on C.LoadID = I.LoadID and C.Name = 'Code'
					inner join LoadItemColumn IC on C.ColumnIndex = IC.ColumnIndex and IC.LoadID = I.LoadID and IC.RowIndex = I.RowIndex 
			where	I.LoadID = @id
					and I.ObjectID is null
					and I.KeyHash is not null;

		insert ReferenceItem (ReferenceItemTypeID, Code, UpdatedOn, UpdatedBy, CreatedOn, CreatedBy)
		output inserted.ID into @ri_insertOutputID
			select	@ObjectID, 
					Code,
					@UpdatedOn, 
					@UpdatedBy, 
					@UpdatedOn, 
					@UpdatedBy
			from	@ri_insertToPerform;

		-- Insert the added records into temp table for logging.
		insert into #tbl 
			select	N.ObjectID,
					I.RowIndex,
					'A', null, null
			from	LoadItem I
					inner join @ri_insertToPerform P on P.KeyHash = I.KeyHash and I.LoadID = @id 
					inner join @ri_insertOutputID N on N.RowID = P.RowID;

		-- Update the LoadItem table with the Object and ObjectID generated from the insert above.
		update	T
		set		T.Object = 'ReferenceItem',
				T.ObjectID = S.ObjectID
		from	LoadItem T
				inner join #tbl S on T.LoadID = @id and S.RowIndex = T.RowIndex and S.[Action] = 'A';
	end
	-------------------------
	

	-- Capture field logs	
	IF OBJECT_ID('tempdb..#fields') IS NOT NULL
			DROP TABLE #fields;

	create table #fields (RowIndex int, ColumnIndex int, [Action] varchar(25));

	--CREATE CLUSTERED INDEX PK_tempFields ON #fields ([RowIndex] ASC,[ColumnIndex] ASC);

	-- Non-relationship fields
	merge	Field as T
	using	(
			select	I.FieldTypeID,
					I.Type,
					I.AllowMultipleValues,
					I.Object,
					I.ObjectID,
					case 
						when I.Type = 'Lookup' and I.AllowMultipleValues = 0 then cast(C.LookupObjectID as nvarchar)
						when I.Type = 'Lookup' and I.AllowMultipleValues = 1 then C.LookupValue
						else C.Value
					end as [Value],
					C.RowIndex,
					C.ColumnIndex
			from	(
					select		I.LoadID,
								FT.ID as FieldTypeID,
								FT.Type,
								FT.AllowMultipleValues,
								I.Object,
								I.ObjectID,
								min(I.RowIndex) as RowIndex,
								C.ColumnIndex
					from		LoadItem I
								inner join LoadItemColumn C on C.LoadID = I.LoadID and C.RowIndex = I.RowIndex and I.LoadID = @id
								inner join LoadColumn LC on LC.LoadID = I.LoadID and LC.ColumnIndex = C.ColumnIndex
								inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID 
														and  (
															FT.Name = LC.Name or
																(
																	@Object = 'TaxonomyType'
																	 and LC.ColumnIndex in (
																		select LC2.ColumnIndex from TaxonomyTypeLevel L2
																		inner join LoadColumn LC2 on LC2.LoadID = @id and L2.[Name] = substring(LC2.[Name], 1, len(LC2.[Name]) - charindex(' ', reverse(LC2.[Name])))
																		inner join LoadItemColumn LI2 on LI2.LoadID = @id and LI2.RowIndex = C.RowIndex and LI2.ColumnIndex = LC2.ColumnIndex and LI2.[Value] is not null
																		where L2.TaxonomyTypeID = @ObjectID and L2.[Level] = (select [level] from @levels where rowIndex = C.RowIndex)
																	 )
																	 and FT.Name = reverse(substring(reverse(LC.[Name]), 0, charindex(' ',reverse(LC.[Name])))) 
																)
															)
														and FT.Type <> 'Relationship' 
														and ( 
																(FT.Type <> 'Lookup' and C.Value is not null) OR 
																(FT.Type = 'Lookup' and FT.AllowMultipleValues = 0 and C.LookupObjectID is not null) OR
																(FT.Type = 'Lookup' and FT.AllowMultipleValues = 1 and C.LookupValue is not null)
															)
					where		I.ObjectID is not null
					group by	I.LoadID,
								FT.ID,
								FT.Type,
								FT.AllowMultipleValues,
								I.Object,
								I.ObjectID,
								C.ColumnIndex
					) I
					inner join LoadItemColumn C on C.LoadID = I.LoadID and C.RowIndex = I.RowIndex and C.ColumnIndex = I.ColumnIndex
			) S on (T.FieldTypeID = S.FieldTypeID and S.Object = T.ObjectType and S.ObjectID = T.ObjectID)
	when matched then
		update	set
				Value = S.Value
	when not matched then
		insert (FieldTypeID, ObjectType, ObjectID, Value)
		values (S.FieldTypeID, S.Object, S.ObjectID, S.Value)
	output S.RowIndex, S.ColumnIndex, $action into #fields;

	delete	T
	from	FieldValue T
			left join (
				select		FT.ID as FieldTypeID,
							I.Object,
							I.ObjectID,
							VS.Value
				from		LoadItem I
							inner join LoadItemColumn C on C.LoadID = I.LoadID and C.RowIndex = I.RowIndex and I.LoadID = @id and I.ObjectID is not null and C.LookupValue is not null
							cross apply string_split(C.LookupValue, ',') VS
							inner join LoadColumn LC on LC.LoadID = I.LoadID and LC.ColumnIndex = C.ColumnIndex
							inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID 
													and FT.Name = LC.Name 
													and FT.Type = 'Lookup' 
													and FT.AllowMultipleValues = 1
			) S on S.FieldTypeID = T.FieldTypeID and S.Object = T.ObjectType and S.ObjectID = T.ObjectID and S.value = T.Value
			inner join (	--LIMITS THE IMPACT OF THIS STATEMENT
				select		FT.ID as FieldTypeID,
							I.Object,
							I.ObjectID
				from		LoadItem I
							inner join LoadItemColumn C on C.LoadID = I.LoadID and C.RowIndex = I.RowIndex and I.LoadID = @id and I.ObjectID is not null and C.LookupValue is not null
							inner join LoadColumn LC on LC.LoadID = I.LoadID and LC.ColumnIndex = C.ColumnIndex
							inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID 
													and FT.Name = LC.Name 
													and FT.Type = 'Lookup' 
													and FT.AllowMultipleValues = 1			
			) L on L.FieldTypeID = T.FieldTypeID and L.Object = T.ObjectType and L.ObjectID = T.ObjectID
	where	S.FieldTypeID is null;

	insert into FieldValue (FieldTypeID, ObjectType, ObjectID, Value)
		select		FT.ID,
					I.Object,
					I.ObjectID,
					VS.Value
		from		LoadItem I
					inner join LoadItemColumn C on C.LoadID = I.LoadID and C.RowIndex = I.RowIndex and I.LoadID = @id and I.ObjectID is not null and C.LookupValue is not null
					cross apply string_split(C.LookupValue, ',') VS
					inner join LoadColumn LC on LC.LoadID = I.LoadID and LC.ColumnIndex = C.ColumnIndex
					inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID 
											and FT.Name = LC.Name 
											and FT.Type = 'Lookup' 
											and FT.AllowMultipleValues = 1
					left join FieldValue FV on FV.FieldTypeID = FT.ID and FV.ObjectType = I.Object and FV.ObjectID = I.ObjectID and FV.Value = VS.Value
		where		FV.ID is null;

	update	T
	set		T.FieldsLoaded = 1
	from	#tbl T
			inner join	(
						select		RowIndex,
									[Action]
						from		#fields
						group by	RowIndex, 
									[Action]
						) S on S.RowIndex = T.RowIndex;

	truncate table #fields;

	-- Parent fields
	declare @parentTypeID int = null,
			@parentTypeName nvarchar(250) = null;
	declare @parentIntersectTypeId int = null;

	select 
		@parentTypeID = I.SubjectID,
		@parentTypeName = I.SubjectName,
		@parentIntersectTypeId = I.ID
	from 
		intersecttypedetail I                
	where I.[PredicateType] = 3 and [Object] = @Object and ObjectID = @ObjectId;
	
	if @parentTypeID is not null
	begin
	
		-- look for column with the parent type name this contains the parent 
		merge	[Intersect] as T
		using	(
				select	distinct
						AD.ObjectID as ParentObjectID,
						AD.[Object] as ParentObject,
						AD.[TypeID] as ParentTypeID,
						AD.[Type] as ParentType,
						@parentIntersectTypeId as IntersectTypeID,
						LI.[Object] as ItemObject,
						LI.ObjectID as ItemObjectID
				from	LoadItem I
						inner join LoadItemColumn C on C.LoadID = I.LoadID and C.RowIndex = I.RowIndex and I.LoadID = @id
						inner join LoadColumn LC on LC.LoadID = I.LoadID and LC.ColumnIndex = C.ColumnIndex and LC.Name = @parentTypeName
						inner join AssetDetail AD on AD.TypeID = @parentTypeID and AD.DisplayValue = C.Value and AD.[Type] = @Object	
						inner join LoadItem LI on LI.RowIndex = C.RowIndex and LI.LoadID = C.LoadID					
				where	I.ObjectID is not null
				) S on (T.IntersectTypeID = S.IntersectTypeID and T.Subject = S.ParentObject and T.SubjectID = S.ParentObjectID and T.Object = S.ItemObject and T.ObjectID = S.ItemObjectID)

		when matched then
			update	set
					T.Subject	= S.ParentObject,
					T.SubjectID = S.ParentObjectID,
					T.Object	= S.ItemObject,
					T.ObjectID	= S.ItemObjectID
		when not matched then
			insert (IntersectTypeID, Subject, SubjectID, Object, ObjectID, Deleted, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner], Visible)
			values (
					S.IntersectTypeID,
					S.ParentObject, 
					S.ParentObjectID,
					S.ItemObject, 
					S.ItemObjectID, 
					0, @UpdatedBy, @UpdatedOn, @UpdatedBy, @UpdatedOn, 'BulkLoad', 1
					);

	end

	-- Relationship fields
	merge	[Intersect] as T
	using	(
			select	distinct
					FT.LookupObjectID as IntersectTypeID,
					case 
						when IT.Subject = @Object and IT.SubjectID = @ObjectID then cast(1 as bit)
						else cast(0 as bit)
					end as IsSubject,
					case 
						when IT.Subject = @Object and IT.SubjectID = @ObjectID then I.Object
						else C.LookupObject
					end as Subject,
					case 
						when IT.Subject = @Object and IT.SubjectID = @ObjectID then I.ObjectID
						else C.LookupObjectID
					end as SubjectID,
					case 
						when IT.Subject = @Object and IT.SubjectID = @ObjectID then C.LookupObject
						else I.Object
					end as Object,
					case 
						when IT.Subject = @Object and IT.SubjectID = @ObjectID then C.LookupObjectID
						else I.ObjectID
					end as ObjectID
			from	LoadItem I
					inner join LoadItemColumn C on C.LoadID = I.LoadID and C.RowIndex = I.RowIndex and I.LoadID = @id
					inner join LoadColumn LC on LC.LoadID = I.LoadID and LC.ColumnIndex = C.ColumnIndex
					inner join FieldType FT on FT.Object = @Object and FT.ObjectID = @ObjectID 
											and FT.Name = LC.Name and FT.Type = 'Relationship' 
											and C.LookupObject is not null and C.LookupObjectID is not null
					inner join IntersectType IT on FT.LookupObjectType = 'IntersectType' and FT.LookupObjectID = IT.ID
			where	I.ObjectID is not null
			) S on (
					T.IntersectTypeID = S.IntersectTypeID 
					and (
							(S.IsSubject = 1 and S.Subject = T.Subject and S.SubjectID = T.SubjectID) OR
							(S.IsSubject = 0 and S.Object = T.Object and S.ObjectID = T.ObjectID)
						)
					)
	when matched then
		update	set
				T.Subject	= S.Subject,
				T.SubjectID = S.SubjectID,
				T.Object	= S.Object,
				T.ObjectID	= S.ObjectID
	when not matched then
		insert (IntersectTypeID, Subject, SubjectID, Object, ObjectID, Deleted, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner], Visible)
		values (
				S.IntersectTypeID,
				S.Subject, 
				S.SubjectID,
				S.Object, 
				S.ObjectID, 
				0, @UpdatedBy, @UpdatedOn, @UpdatedBy, @UpdatedOn, 'BulkLoad', 1
				);
	
	-- Capture logs and update load status. -----
	update	T
	set		T.Status = 1,
			T.StatusMessage = 'Item successfully ' + case S.[Action] when 'A' then 'added' else 'updated' end + '.'
	from	LoadItem T
			inner join #tbl S on T.LoadID = @id and S.RowIndex = T.RowIndex and T.[Object] is not null and T.ObjectID is not null;

	update	LoadItem
	set		Status = 0,
			StatusMessage = 'Item load failed. ' + coalesce(StatusMessage, '')
	where	([Object] is null or ObjectID is null)
			and LoadID = @id;

	----Finally, close out the Load.
	update	[Load] 
	set		DateCompleted = getutcdate()
	where	ID = @id
	---------------------------------------------
end
GO

alter procedure [bulkload].[UpdateDynamicLookupFieldColumns]
	@id int	
as
begin
	set nocount on;
	declare @startColumnIndex int = 0;
	declare @endColumnIndex int = 0;

	-- Artifact lookups
	update	T
	set		T.LookupObject = S.LookupObject,
			T.LookupObjectID = S.LookupObjectID
	from	LoadItemColumn T
			inner join	(
						select	IC.LoadID,
								IC.RowIndex,
								IC.ColumnIndex,
								case 
									when ( (L_A.ID is not null) OR (F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel AND F.LookupObjectType in ('Artifact', 'ArtifactType')) ) then 'Artifact'									
									else NULL
								end as LookupObject,
								case 
									when L_A.ObjectID is not null then L_A.ObjectID
									when F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel then 0
									else NULL
								end as LookupObjectID --coalesce(L_A.ID, L_D.ID, L_DI.ID, L_F.ID, L_L.Value, L_T.ID) as LookupObjectID -- L_I.ID,
						from	FieldType F
								inner join [Load] L on L.ID = @id and L.[Object] = F.[Object] and L.ObjectID = F.ObjectID and F.[Type] = 'Lookup'
								inner join [LoadColumn] C on C.LoadID = L.ID and F.Name = C.Name
								inner join [LoadItemColumn] IC on IC.LoadID = C.LoadID and IC.ColumnIndex = C.ColumnIndex								
								inner join AssetDetail L_A on L_A.[Object] = 'Artifact' and L_A.TypeID = F.LookupObjectID and (L_A.DisplayValue = IC.Value)								
						where	F.AllowMultipleValues = 0 and F.LookupObjectType in ('Artifact', 'ArtifactType')
						) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex;

	-- Reference Item Type lookups
	update	T
	set		T.LookupObject = S.LookupObject,
			T.LookupObjectID = S.LookupObjectID
	from	LoadItemColumn T
			inner join	(
						select	IC.LoadID,
								IC.RowIndex,
								IC.ColumnIndex,
								case 									
									when ( (L_D.ID is not null) OR (F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel AND F.LookupObjectType = 'ReferenceItemType') ) then 'ReferenceItemType'									
									else NULL
								end as LookupObject,
								case 									
									when L_D.ID is not null then L_D.ID
									when F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel then 0																		
									else NULL
								end as LookupObjectID --coalesce(L_A.ID, L_D.ID, L_DI.ID, L_F.ID, L_L.Value, L_T.ID) as LookupObjectID -- L_I.ID,
						from	FieldType F
								inner join [Load] L on L.ID = @id and L.[Object] = F.[Object] and L.ObjectID = F.ObjectID and F.[Type] = 'Lookup'
								inner join [LoadColumn] C on C.LoadID = L.ID and F.Name = C.Name
								inner join [LoadItemColumn] IC on IC.LoadID = C.LoadID and IC.ColumnIndex = C.ColumnIndex
								
								inner join ReferenceItemType L_D on L_D.[Name] = IC.Value								
						where	F.AllowMultipleValues = 0 and F.LookupObjectType = 'ReferenceItemType'
						) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex

	-- Reference item
		update	T
	set		T.LookupObject = S.LookupObject,
			T.LookupObjectID = S.LookupObjectID
	from	LoadItemColumn T
			inner join	(
						select	IC.LoadID,
								IC.RowIndex,
								IC.ColumnIndex,								
								case
									when ( (L_DI.ID is not null) OR (F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel AND F.LookupObjectType = 'ReferenceItem') ) then 'ReferenceItem'									
									else NULL
								end as LookupObject,
								case 									
									when L_DI.ID is not null then L_DI.ID
									when F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel then 0
									else NULL
								end as LookupObjectID --coalesce(L_A.ID, L_D.ID, L_DI.ID, L_F.ID, L_L.Value, L_T.ID) as LookupObjectID -- L_I.ID,
						from	FieldType F
								inner join [Load] L on L.ID = @id and L.[Object] = F.[Object] and L.ObjectID = F.ObjectID and F.[Type] = 'Lookup'
								inner join [LoadColumn] C on C.LoadID = L.ID and F.Name = C.Name
								inner join [LoadItemColumn] IC on IC.LoadID = C.LoadID and IC.ColumnIndex = C.ColumnIndex
								inner join ReferenceItem L_DI on L_DI.ReferenceItemTypeID = F.LookupObjectID and L_DI.[DisplayValue] = IC.Value							
						where	F.AllowMultipleValues = 0 and F.LookupObjectType = 'ReferenceItem'
						) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex

-- fusion attribute type
update	T
	set		T.LookupObject = S.LookupObject,
			T.LookupObjectID = S.LookupObjectID
	from	LoadItemColumn T
			inner join	(
						select	IC.LoadID,
								IC.RowIndex,
								IC.ColumnIndex,
								case
									when ( (L_F.ID is not null) OR (F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel AND F.LookupObjectType = 'FusionAttributeType') ) then 'FusionAttribute'									
									else NULL
								end as LookupObject,
								case 									
									when L_F.ID is not null then L_F.ID
									when F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel then 0

									else NULL
								end as LookupObjectID --coalesce(L_A.ID, L_D.ID, L_DI.ID, L_F.ID, L_L.Value, L_T.ID) as LookupObjectID -- L_I.ID,
						from	FieldType F
								inner join [Load] L on L.ID = @id and L.[Object] = F.[Object] and L.ObjectID = F.ObjectID and F.[Type] = 'Lookup'
								inner join [LoadColumn] C on C.LoadID = L.ID and F.Name = C.Name
								inner join [LoadItemColumn] IC on IC.LoadID = C.LoadID and IC.ColumnIndex = C.ColumnIndex
								
								inner join FusionAttribute L_F on L_F.FusionAttributeTypeID = F.LookupObjectID and (L_F.[Name] = IC.Value OR L_F.TextPath = IC.Value)								
						where	F.AllowMultipleValues = 0 and F.LookupObjectType = 'FusionAttributeType'
						) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex
-- Lookup 

update	T
	set		T.LookupObject = S.LookupObject,
			T.LookupObjectID = S.LookupObjectID
	from	LoadItemColumn T
			inner join	(
						select	IC.LoadID,
								IC.RowIndex,
								IC.ColumnIndex,
								case 									
									when ( (L_L.Value is not null) OR (F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel AND F.LookupObjectType = 'Lookup') ) then 'Lookup'									
									else NULL
								end as LookupObject,
								case 									
									when L_L.Value is not null then L_L.Value
									when F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel then 0

									else NULL
								end as LookupObjectID 
						from	FieldType F
								inner join [Load] L on L.ID = @id and L.[Object] = F.[Object] and L.ObjectID = F.ObjectID and F.[Type] = 'Lookup'
								inner join [LoadColumn] C on C.LoadID = L.ID and F.Name = C.Name
								inner join [LoadItemColumn] IC on IC.LoadID = C.LoadID and IC.ColumnIndex = C.ColumnIndex
																
								left join [FieldLookupValue] L_L on F.ID = L_L.FieldTypeID and L_L.LookupObjectType = 'Lookup' and L_L.LookupObjectID = F.LookupObjectID and L_L.[Text] = IC.Value								
						where	F.AllowMultipleValues = 0 and F.LookupObjectType = 'Lookup'
						) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex

-- Resource 

update	T
	set		T.LookupObject = S.LookupObject,
			T.LookupObjectID = S.LookupObjectID
	from	LoadItemColumn T
			inner join	(
						select	IC.LoadID,
								IC.RowIndex,
								IC.ColumnIndex,
								case 									
									when ( (L_L.Value is not null) OR (F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel AND F.LookupObjectType = 'Resource') ) then 'Resource'									
									else NULL
								end as LookupObject,
								case 									
									when L_L.Value is not null then L_L.Value
									when F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel then 0

									else NULL
								end as LookupObjectID 
						from	FieldType F
								inner join [Load] L on L.ID = @id and L.[Object] = F.[Object] and L.ObjectID = F.ObjectID and F.[Type] = 'Lookup'
								inner join [LoadColumn] C on C.LoadID = L.ID and F.Name = C.Name
								inner join [LoadItemColumn] IC on IC.LoadID = C.LoadID and IC.ColumnIndex = C.ColumnIndex
																
								left join [FieldLookupValue] L_L on F.ID = L_L.FieldTypeID and L_L.LookupObjectType = 'Resource' and L_L.LookupObjectID = F.LookupObjectID and L_L.[Text] = IC.Value								
						where	F.AllowMultipleValues = 0 and F.LookupObjectType = 'Resource'
						) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex

-- taxonomy
update	T
	set		T.LookupObject = S.LookupObject,
			T.LookupObjectID = S.LookupObjectID
	from	LoadItemColumn T
			inner join	(
						select	IC.LoadID,
								IC.RowIndex,
								IC.ColumnIndex,
								case
									when ( (L_T.ID is not null) OR (F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel ) ) then 'Taxonomy'
									else NULL
								end as LookupObject,
								case 									
									when L_T.ObjectID is not null then L_T.ObjectID
									when F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel then 0
									else NULL
								end as LookupObjectID --coalesce(L_A.ID, L_D.ID, L_DI.ID, L_F.ID, L_L.Value, L_T.ID) as LookupObjectID -- L_I.ID,
						from	FieldType F
								inner join [Load] L on L.ID = @id and L.[Object] = F.[Object] and L.ObjectID = F.ObjectID and F.[Type] = 'Lookup'
								inner join [LoadColumn] C on C.LoadID = L.ID and F.Name = C.Name
								inner join [LoadItemColumn] IC on IC.LoadID = C.LoadID and IC.ColumnIndex = C.ColumnIndex
																
								inner join AssetDetail L_T on L_T.[Object] = 'Taxonomy' and L_T.TypeID = F.LookupObjectID and (L_T.[DisplayValue] = IC.Value /*OR L_T.TextPath = IC.Value*/)
						where	F.AllowMultipleValues = 0 and F.LookupObjectType in ('Taxonomy', 'TaxonomyType')
						) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex

-- taxonomy type
	update	T
	set		T.LookupObject = S.LookupObject,
			T.LookupObjectID = S.LookupObjectID
	from	LoadItemColumn T
			inner join	(
						select	IC.LoadID,
								IC.RowIndex,
								IC.ColumnIndex,
								case
									when ( (L_T.ID is not null) OR (F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel ) ) then 'Taxonomy'
									else NULL
								end as LookupObject,
								case 									
									when L_T.ObjectID is not null then L_T.ObjectID
									when F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel then 0
									else NULL
								end as LookupObjectID --coalesce(L_A.ID, L_D.ID, L_DI.ID, L_F.ID, L_L.Value, L_T.ID) as LookupObjectID -- L_I.ID,
						from	FieldType F
								inner join [Load] L on L.ID = @id and L.[Object] = F.[Object] and L.ObjectID = F.ObjectID and F.[Type] = 'Lookup'
								inner join [LoadColumn] C on C.LoadID = L.ID and F.Name = C.Name
								inner join [LoadItemColumn] IC on IC.LoadID = C.LoadID and IC.ColumnIndex = C.ColumnIndex
																
								inner join AssetType L_T on L_T.[Object] = 'TaxonomyType'  and (L_T.Name = IC.Value )
						where	F.AllowMultipleValues = 0 and F.LookupObjectType = 'TaxonomyType' and F.LookupObjectID = 0
						) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex
	
	select @endColumnIndex = max(ColumnIndex) from LoadItemColumn where loadid = @id;

	while @startColumnIndex <= @endColumnIndex
	begin
		update	T
		set		T.StatusMessage = coalesce(T.StatusMessage, '') + S.StatusMessage
		from	LoadItem T
				inner join	(
							select	IC.LoadID,
									IC.RowIndex,
									case 
										when IC.LookupObjectID is null and IC.Value is not null and IC.Value <> '' then ' ' + F.Name + ' does not contain a valid value.'
										else ''
									end StatusMessage
							from	FieldType F
									inner join [Load] L on L.ID = @id and L.[Object] = F.[Object] and L.ObjectID = F.ObjectID and F.[Type] = 'Lookup'
									inner join [LoadColumn] C on C.LoadID = L.ID and F.Name = C.Name
									inner join [LoadItemColumn] IC on IC.LoadID = C.LoadID and IC.ColumnIndex = C.ColumnIndex and IC.columnIndex = @startColumnIndex and IC.LookupObjectID is null
							) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex
		set @startColumnIndex = @startColumnIndex + 1
	end
end
GO

alter procedure [fusion].[GenerateFoundationLineage]
	@fusionId int
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

	--	select	ST.FormattedValue as SourceFusionAttributeTypeID,
	--			TT.FormattedValue as TargetFusionAttributeTypeID
	--from		FusionAttribute MA
	--			inner join FieldWithRelation ST on ST.ObjectType = 'FusionAttribute' and ST.ObjectID = MA.ID and ST.Name = 'SourceTypeID'
	--			inner join FieldWithRelation S on S.ObjectType = 'FusionAttribute' and S.ObjectID = MA.ID and S.Name = 'SourceObjectID'
	--			inner join FieldWithRelation TT on TT.ObjectType = 'FusionAttribute' and TT.ObjectID = MA.ID and TT.Name = 'TargetTypeID'
	--			inner join FieldWithRelation T on T.ObjectType = 'FusionAttribute' and T.ObjectID = MA.ID and T.Name = 'TargetObjectID'
	--			inner join FusionAttribute SA on SA.FusionID = MA.FusionID and SA.SourceID = S.FormattedValue --and SA.FusionAttributeTypeID = ST.FormattedValue
	--			inner join FusionAttribute TA on TA.FusionID = MA.FusionID and TA.SourceID = T.FormattedValue --and TA.FusionAttributeTypeID = TT.FormattedValue
	--			left join IntersectTypeDetail IT on IT.Subject = 'FusionAttributeType' and IT.SubjectID = SA.FusionAttributeTypeID and IT.Object = 'FusionAttributeType' and IT.ObjectID = TA.FusionAttributeTypeID and IT.PredicateType = 1
	--where		MA.FusionAttributeTypeID = 1476 --and SA.ID <> TA.ID (slows down query quite a bit)
	--group by	ST.FormattedValue, TT.FormattedValue

	--select * from Predicate
	--update IntersectType set PredicateID = 10 where Subject = 'FusionAttributeType' and Object = 'FusionAttributeType' and PredicateID is null and SubjectID > 1400
	--insert into IntersectType (Subject, SubjectID, Object, ObjectID, IsSystem, State,PredicateID, SubjectCardinality,ObjectCardinality) values ('FusionAttributeType', 1492, 'FusionAttributeType', 1490, 1, 1, 10, 2,2)

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
				inner join FusionAttribute SA on SA.FusionID = MA.FusionID and SA.SourceID = S.FormattedValue --and SA.FusionAttributeTypeID = ST.FormattedValue
				inner join FusionAttribute TA on TA.FusionID = MA.FusionID and TA.SourceID = T.FormattedValue --and TA.FusionAttributeTypeID = TT.FormattedValue
				left join IntersectTypeDetail IT on IT.Subject = 'FusionAttributeType' and IT.SubjectID = SA.FusionAttributeTypeID and IT.Object = 'FusionAttributeType' and IT.ObjectID = TA.FusionAttributeTypeID and IT.PredicateType = 1
	where		MA.FusionAttributeTypeID = 1476 and MA.FusionID = @fusionId --and SA.ID <> TA.ID (slows down query quite a bit)
	group by	SA.ID, TA.ID, IT.ID

--	select * from FusionAttribute where ID in (64479)
--select top 1000 * from [Intersect] where [Owner] = 'FOUNDATION'

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

alter procedure [fusion].[GenerateMarkitMapLineageData]
	@fusionID int
as
begin
	SET NOCOUNT, ANSI_PADDING ON;
	SET ANSI_WARNINGS ON;

	declare @databaseName varchar(100);
	declare @sourceFieldTypeID int;
	declare @targetFieldTypeID int;		
	declare @mapFusionAttributeTypeID int = 710; -- this is fixed for all clients
	declare @viewColumnFusionAttributeTypeID int = 715; -- this is fixed for all clients
	
	-- load the field ids for the source / target from mappings
	select @sourceFieldTypeID = id from fieldtype where [object] = 'FusionAttributeType' and [objectid] = @mapFusionAttributeTypeID and name = 'source';
	select @targetFieldTypeID = id from fieldtype where [object] = 'FusionAttributeType' and [objectid] = @mapFusionAttributeTypeID and name = 'target';
	
	IF @sourceFieldTypeID IS NULL
	begin		
		raiserror('ERROR - Cannot find the Markit Fusion Map Source Field.  Please make sure the latest markit fusion attribute types have been pushed to this environment', 16, -1);
		return;
	end

	IF @targetFieldTypeID IS NULL
	begin		
		raiserror('ERROR - Cannot find the Markit Fusion Map Target Field.  Please make sure the latest markit fusion attribute types have been pushed to this environment', 16, -1);
		return;
	end

	-- determine the database name
	--select top 1 @databaseName = replace(sourceid, name,'') from fusionattribute where fusionid = @fusionID and fusionattributetypeid = 711 and sourceid like '%.%';	
	--substring(sourceid, 0,charindex('.',sourceid))
	select top 1 @databaseName = substring(sourceid, 0,charindex('.',sourceid)+1) from fusionattribute where fusionid = @fusionID and fusionattributetypeid = 711 and sourceid like '%.%';	

	if @databaseName is null
	begin
		raiserror('ERROR - Cannot determine the database name to strip from markit fusion attribute data', 16, -1);
		return;
	end

	-- dont run if this is not a markit fusion
	declare @fusionTypeId int;
	select @fusionTypeId = FusionTypeID from [dbo].[Fusion] where ID = @fusionID;
	if @fusionTypeId != 13
	begin
		raiserror('ERROR - The fusion lineage generation process may only be run for the Markit Fusion Type', 16, -1);
		return;
	end

	-- dont run if no map records exist for this fusion
	if not exists( select 1 from fusionattribute where fusionid = @fusionID and fusionattributetypeid = @mapFusionAttributeTypeID )
	begin
		raiserror('ERROR - No Markit Fusion Map records exist for the specified Fusion ID', 16, -1);
		return;
	end

	-- figure out the database prefix from some markit data

	-- some logging
	declare @fusionName nvarchar(250);
	select @fusionName = name from [dbo].[fusion] where id = @fusionID;

	begin
		print 'Running For Fusion:' + @fusionName;
		print 'Using Target Field ID:' + cast(@targetFieldTypeID as varchar(100));
		print 'Using Source Field ID:' + cast(@sourceFieldTypeID as varchar(100));
		print 'Using Database prefix:' + @databaseName;
	end
	-- end logging

	-- get the intersecttypeid for view -> table intersects
	declare @viewTableIntersectTypeId int;
	select @viewTableIntersectTypeId = id from intersecttype where [object] = 'FusionAttributeType' and [Subject] = 'FusionAttributeType' and [subjectid] = 714 and [objectid] = 712
	if @viewTableIntersectTypeId is null
	begin
		raiserror('ERROR - Cannot identify the intersecttypeid for markit view/table relations', 16, -1);
		return;
	end

	-- get the intersecttypeid for view -> view intersects
	declare @viewViewIntersectTypeId int;
	select @viewViewIntersectTypeId = id from intersecttype where [object] = 'FusionAttributeType' and [Subject] = 'FusionAttributeType' and [subjectid] = 714 and [objectid] = 714
	if @viewViewIntersectTypeId is null
	begin
		raiserror('ERROR - Cannot identify the intersecttypeid for markit view/view relations', 16, -1);
		return;
	end

	IF OBJECT_ID('tempdb..#maps') IS NOT NULL
		DROP TABLE #maps;

	create table #maps (	
		ID int identity primary key,			
		MapRuleItemID int,
		[ParentID] int,
		[UltimateParentID] int,
		[Level] int,
		SourceFusionAttributeID int,
		SourceFusionAttributeTypeID int,
		SourceObject nvarchar(500),		
		SourceParentObject nvarchar(max),
		SourceParentObjectFusionAttributeID int,
		SourceParentObjectFusionAttributeTypeID int,
		TargetFusionAttributeID int,
		TargetFusionAttributeTypeID int,
		TargetObject nvarchar(500),
		TargetParentObject nvarchar(max),
		TargetParentObjectFusionAttributeID int,
		TargetParentObjectFusionAttributeTypeID int,					
		[Source] varchar(50),
		[SourceID] int,	
		[Target] varchar(50),
		[TargetID] int,
	);

	CREATE NONCLUSTERED INDEX [CIX_TempMaps] ON #maps ( SourceFusionAttributeID ASC, TargetFusionAttributeID ASC );

	IF OBJECT_ID('tempdb..#objectmap') IS NOT NULL
		DROP TABLE #objectmap;

	create table #objectmap (
		MapID int,
		MapItemID int,
		[Object] varchar(50),
		[ObjectID] int,	
		[SourceIntersectID] int,		
		[TargetIntersectID] int		
	)

	CREATE NONCLUSTERED INDEX [CIX_TempObjectMap] ON #objectmap ( MapID ASC, [Object] ASC, [ObjectID] ASC );
	
	insert into #maps
		(SourceObject, TargetObject)
		select distinct
			replace(cast(F_source.formattedValue as nvarchar(500)), @databaseName, '') as SourceObject						
			, replace(cast(F_target.formattedValue as nvarchar(500)), @databaseName, '') as TargetObject			
		from 
			FusionAttribute FA
			inner join Field F_source on F_source.ObjectType = 'FusionAttribute' and F_source.ObjectID = FA.ID and F_source.FieldTypeID = @sourceFieldTypeID -- MAP SOURCE FIELD VALUE
			inner join Field F_target on F_target.ObjectType = 'FusionAttribute' and F_target.ObjectID = FA.ID and F_target.FieldTypeID = @targetFieldTypeID -- TARGET SOURCE FIELD VALUE
		where 
			FA.FusionID = @fusionID
				and
			FA.FusionAttributeTypeID = @mapFusionAttributeTypeID
			--	and
			--F_source.formattedValue like '%.cusip' or F_source.formattedValue like '%.ticker' or F_source.formattedValue like '%.cntry_of%' -- **for testing to limit to just cusip**;
	
	-- check how many map records we have
	declare @mapRecordCount int;
	select @mapRecordCount = count(1) from #maps
	if @fusionTypeId > 0
		begin
			print 'Loaded [' + cast(@mapRecordCount as varchar) + '] map records';			
		end
	else
		begin
			raiserror('ERROR - Could not load any map records this is most likely because there are no corresponding fusionattributes for the markit source/target mappings.', 16, -1);
			return;
		end

			
	--set the Source objects 
	update	T
	set		T.SourceFusionAttributeID = S.ID, T.SourceFusionAttributeTypeID = S.FusionAttributeTypeID
	from	#maps T			
			inner join fusionattribute S on (S.TextPath = T.SourceObject and S.FusionID = @fusionID)

	--set the Target Objects
	update	T
	set		T.TargetFusionAttributeID = S.ID, T.TargetFusionAttributeTypeID = S.FusionAttributeTypeID
	from	#maps T			
			inner join fusionattribute S on (S.TextPath = T.TargetObject and S.FusionID = @fusionID)

	--remove any source objects that we cant find the fusion attribute for
	delete from #maps where SourceFusionAttributeID is null or TargetFusionAttributeID is null		
	
	--set the source parent objects
	update T
	set T.SourceParentObject = FA_p.TextPath, T.SourceParentObjectFusionAttributeID = FA_p.ID, T.SourceParentObjectFusionAttributeTypeID = FA_p.FusionAttributeTypeID
	from #maps T
		inner join fusionattribute FA on (FA.ID = T.SourceFusionAttributeID)
		inner join fusionattribute FA_p on (FA_p.ID = FA.ParentID)

	--set the target parent objects
	update T
	set T.TargetParentObject = FA_p.TextPath, T.TargetParentObjectFusionAttributeID = FA_p.ID, T.TargetParentObjectFusionAttributeTypeID = FA_p.FusionAttributeTypeID
	from #maps T
		inner join fusionattribute FA on (FA.ID = T.TargetFusionAttributeID)
		inner join fusionattribute FA_p on (FA_p.ID = FA.ParentID)

	-- remove any maps that reference same fusionattribute both sides
	delete from #maps where SourceFusionAttributeID = TargetFusionAttributeID;
	
	--this query adds in the view to table mapings
	-- add in any view column to table column records
	-- table / view maps for targets that are missing connection
	insert into #maps
		(SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, SourceParentObjectFusionAttributeID, SourceParentObject, SourceParentObjectFusionAttributeTypeID, TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject, TargetParentObjectFusionAttributeID, TargetParentObject, TargetParentObjectFusionAttributeTypeID)
		select 	distinct
			m.TargetFusionAttributeID as SourceFusionAttributeID,
			m.TargetFusionAttributeTypeID as SourceFusionAttributeTypeID,
			m.TargetObject as SourceObject,
			m.TargetParentObjectFusionAttributeID as SourceParentObjectFusionAttributeID,
			m.TargetParentObject as SourceParentObject,
			m.TargetParentObjectFusionAttributeTypeID as SourceParentObjectFusionAttributeTypeID,
			T.id as TargetFusionAttributeID,
			T.fusionattributetypeid as TargetFusionAttributeTypeID,
			T.textpath as TargetObject,
			i.objectid as TargetParentObjectFusionAttributeID,
			T_p.name as TargetParentObject,
			T_p.fusionattributetypeid as TargetParentObjectFusionAttributeTypeID			
		 from 
			#maps m			
			inner join [intersect] i on (i.subjectid = m.TargetParentObjectFusionAttributeID and i.[subject] = 'FusionAttribute')	
			inner join fusionattribute T_p on (T_p.id = i.objectid)
			inner join fusionattribute T on(T.parentid = T_p.id and T.Textpath = T_p.TextPath + replace(m.TargetObject,m.TargetParentObject,'')) -- we are doing this to avoid messing with the name column that doesnt have an index
		where 
			m.TargetFusionAttributeTypeID = @viewColumnFusionAttributeTypeID
				and
			i.intersecttypeid = @viewTableIntersectTypeId
				and
			m.id not in(select m_2.id from #maps m_2 where (m_2.SourceFusionAttributeID = m.TargetFusionAttributeID and m_2.TargetFusionAttributeID = T.Id) or (m_2.TargetFusionAttributeID = m.TargetFusionAttributeID and m_2.SourceFusionAttributeID = T.id)) -- dont insert duplicates
	
	-- table / view maps for sources that are missing connection
	insert into #maps
		(TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject, TargetParentObjectFusionAttributeID, TargetParentObject, TargetParentObjectFusionAttributeTypeID, SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, SourceParentObjectFusionAttributeID, SourceParentObject, SourceParentObjectFusionAttributeTypeID)
		select 	distinct
			m.SourceFusionAttributeID as TargetFusionAttributeID,
			m.SourceFusionAttributeTypeID as TargetFusionAttributeTypeID,
			m.SourceObject as TargetObject,
			m.SourceParentObjectFusionAttributeID as TargetParentObjectFusionAttributeID,
			m.SourceParentObject as TargetParentObject,
			m.SourceParentObjectFusionAttributeTypeID as TargetParentObjectFusionAttributeTypeID,
			T.id as SourceFusionAttributeID,
			T.fusionattributetypeid as SourceFusionAttributeTypeID,
			T.textpath as SourceObject,
			i.objectid as SourceParentObjectFusionAttributeID,
			T_p.name as SourceParentObject,
			T_p.fusionattributetypeid as SourceParentObjectFusionAttributeTypeID			
		 from 
			#maps m			
			inner join [intersect] i on (i.subjectid = m.SourceParentObjectFusionAttributeID and i.[subject] = 'FusionAttribute')	
			inner join fusionattribute T_p on (T_p.id = i.objectid)
			inner join fusionattribute T on(T.parentid = T_p.id and T.Textpath = T_p.TextPath + replace(m.SourceObject,m.SourceParentObject,'')) -- we are doing this to avoid messing with the name column that doesnt have an index
		where 
			m.SourceFusionAttributeTypeID = @viewColumnFusionAttributeTypeID
				and
			i.intersecttypeid = @viewTableIntersectTypeId
				and
			m.id not in(select m_2.id from #maps m_2 where (m_2.SourceFusionAttributeID = T.Id and m_2.TargetFusionAttributeID = m.SourceFusionAttributeID) or (m_2.TargetFusionAttributeID = T.id  and m_2.SourceFusionAttributeID = m.SourceFusionAttributeID)) -- dont insert duplicates
					
	-- end table / view maps

	

	--this query adds in the view to view mapings
	-- add in any view column to view column records
	-- view / view maps for targets that are missing connection
	/*insert into #maps
		(SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, SourceParentObjectFusionAttributeID, SourceParentObject, SourceParentObjectFusionAttributeTypeID, TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject, TargetParentObjectFusionAttributeID, TargetParentObject, TargetParentObjectFusionAttributeTypeID)
		select 	distinct
			m.TargetFusionAttributeID as SourceFusionAttributeID,
			m.TargetFusionAttributeTypeID as SourceFusionAttributeTypeID,
			m.TargetObject as SourceObject,
			m.TargetParentObjectFusionAttributeID as SourceParentObjectFusionAttributeID,
			m.TargetParentObject as SourceParentObject,
			m.TargetParentObjectFusionAttributeTypeID as SourceParentObjectFusionAttributeTypeID,
			T.id as TargetFusionAttributeID,
			T.fusionattributetypeid as TargetFusionAttributeTypeID,
			T.textpath as TargetObject,
			i.objectid as TargetParentObjectFusionAttributeID,
			T_p.name as TargetParentObject,
			T_p.fusionattributetypeid as TargetParentObjectFusionAttributeTypeID			
		 from 
			#maps m			
			inner join [intersect] i on (i.subjectid = m.TargetParentObjectFusionAttributeID and i.[subject] = 'FusionAttribute')	
			inner join fusionattribute T_p on (T_p.id = i.objectid)
			inner join fusionattribute T on(T.parentid = T_p.id and T.Textpath = T_p.TextPath + replace(m.TargetObject,m.TargetParentObject,'')) -- we are doing this to avoid messing with the name column that doesnt have an index
		where 
			m.TargetFusionAttributeTypeID = @viewColumnFusionAttributeTypeID
				and
			i.intersecttypeid = @viewViewIntersectTypeId
				and
			m.id not in(select m_2.id from #maps m_2 where (m_2.SourceFusionAttributeID = m.TargetFusionAttributeID and m_2.TargetFusionAttributeID = T.Id) or (m_2.TargetFusionAttributeID = m.TargetFusionAttributeID and m_2.SourceFusionAttributeID = T.id)) -- dont insert duplicates
	*/
	insert into #maps
		(SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, SourceParentObjectFusionAttributeID, SourceParentObject, SourceParentObjectFusionAttributeTypeID, TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject, TargetParentObjectFusionAttributeID, TargetParentObject, TargetParentObjectFusionAttributeTypeID)
		select 	distinct
			m.TargetFusionAttributeID as SourceFusionAttributeID,
			m.TargetFusionAttributeTypeID as SourceFusionAttributeTypeID,
			m.TargetObject as SourceObject,
			m.TargetParentObjectFusionAttributeID as SourceParentObjectFusionAttributeID,
			m.TargetParentObject as SourceParentObject,
			m.TargetParentObjectFusionAttributeTypeID as SourceParentObjectFusionAttributeTypeID,
			T.id as TargetFusionAttributeID,
			T.fusionattributetypeid as TargetFusionAttributeTypeID,
			T.textpath as TargetObject,
			i.objectid as TargetParentObjectFusionAttributeID,
			T_p.name as TargetParentObject,
			T_p.fusionattributetypeid as TargetParentObjectFusionAttributeTypeID			
		 from 
			#maps m			
			inner join [intersect] i on (i.objectid = m.TargetParentObjectFusionAttributeID and i.[object] = 'FusionAttribute')	
			inner join fusionattribute T_p on (T_p.id = i.subjectid)
			inner join fusionattribute T on(T.FusionId = @fusionId and T.deleted = 0 and T.parentid = T_p.id and T.Textpath = T_p.TextPath + replace(m.TargetObject,m.TargetParentObject,'')) -- we are doing this to avoid messing with the name column that doesnt have an index
		where 
			m.TargetFusionAttributeTypeID = @viewColumnFusionAttributeTypeID
				and
			i.intersecttypeid = @viewViewIntersectTypeId
				and
			m.id not in(select m_2.id from #maps m_2 where (m_2.SourceFusionAttributeID = m.TargetFusionAttributeID and m_2.TargetFusionAttributeID = T.Id) or (m_2.TargetFusionAttributeID = m.TargetFusionAttributeID and m_2.SourceFusionAttributeID = T.id)) -- dont insert duplicates

	-- view / view maps for sources that are missing connection
	insert into #maps
		(TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject, TargetParentObjectFusionAttributeID, TargetParentObject, TargetParentObjectFusionAttributeTypeID, SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, SourceParentObjectFusionAttributeID, SourceParentObject, SourceParentObjectFusionAttributeTypeID)
		select 	distinct
			m.SourceFusionAttributeID as TargetFusionAttributeID,
			m.SourceFusionAttributeTypeID as TargetFusionAttributeTypeID,
			m.SourceObject as TargetObject,
			m.SourceParentObjectFusionAttributeID as TargetParentObjectFusionAttributeID,
			m.SourceParentObject as TargetParentObject,
			m.SourceParentObjectFusionAttributeTypeID as TargetParentObjectFusionAttributeTypeID,
			T.id as SourceFusionAttributeID,
			T.fusionattributetypeid as SourceFusionAttributeTypeID,
			T.textpath as SourceObject,
			i.objectid as SourceParentObjectFusionAttributeID,
			T_p.name as SourceParentObject,
			T_p.fusionattributetypeid as SourceParentObjectFusionAttributeTypeID			
		 from 
			#maps m			
			inner join [intersect] i on (i.subjectid = m.SourceParentObjectFusionAttributeID and i.[subject] = 'FusionAttribute')	
			inner join fusionattribute T_p on (T_p.id = i.objectid)
			inner join fusionattribute T on(T.FusionId = @fusionId and T.deleted = 0 and T.parentid = T_p.id and T.Textpath = T_p.TextPath + replace(m.SourceObject,m.SourceParentObject,'')) -- we are doing this to avoid messing with the name column that doesnt have an index
		where 
			m.SourceFusionAttributeTypeID = @viewColumnFusionAttributeTypeID
				and
			i.intersecttypeid = @viewViewIntersectTypeId
				and
			m.id not in(select m_2.id from #maps m_2 where (m_2.SourceFusionAttributeID = T.Id and m_2.TargetFusionAttributeID = m.SourceFusionAttributeID) or (m_2.TargetFusionAttributeID = T.id  and m_2.SourceFusionAttributeID = m.SourceFusionAttributeID)) -- dont insert duplicates

	/*	insert into #maps
		(TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject, TargetParentObjectFusionAttributeID, TargetParentObject, TargetParentObjectFusionAttributeTypeID, SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, SourceParentObjectFusionAttributeID, SourceParentObject, SourceParentObjectFusionAttributeTypeID)
		select 	distinct
			m.SourceFusionAttributeID as TargetFusionAttributeID,
			m.SourceFusionAttributeTypeID as TargetFusionAttributeTypeID,
			m.SourceObject as TargetObject,
			m.SourceParentObjectFusionAttributeID as TargetParentObjectFusionAttributeID,
			m.SourceParentObject as TargetParentObject,
			m.SourceParentObjectFusionAttributeTypeID as TargetParentObjectFusionAttributeTypeID,
			T.id as SourceFusionAttributeID,
			T.fusionattributetypeid as SourceFusionAttributeTypeID,
			T.textpath as SourceObject,
			i.objectid as SourceParentObjectFusionAttributeID,
			T_p.name as SourceParentObject,
			T_p.fusionattributetypeid as SourceParentObjectFusionAttributeTypeID			
		 from 
			#maps m			
			inner join [intersect] i on (i.objectid = m.SourceParentObjectFusionAttributeID and i.[object] = 'FusionAttribute')	
			inner join fusionattribute T_p on (T_p.id = i.subjectid)
			inner join fusionattribute T on(T.parentid = T_p.id and T.Textpath = T_p.TextPath + replace(m.SourceObject,m.SourceParentObject,'')) -- we are doing this to avoid messing with the name column that doesnt have an index
		where 
			m.SourceFusionAttributeTypeID = @viewColumnFusionAttributeTypeID
				and
			i.intersecttypeid = @viewViewIntersectTypeId
				and
			m.id not in(select m_2.id from #maps m_2 where (m_2.SourceFusionAttributeID = T.Id and m_2.TargetFusionAttributeID = m.SourceFusionAttributeID) or (m_2.TargetFusionAttributeID = T.id  and m_2.SourceFusionAttributeID = m.SourceFusionAttributeID)) -- dont insert duplicates
		*/				
	-- end view / view maps


	-- populate the previous step id this also duplicates items that have multiple paths and is very important
	update m_S
	set m_S.ParentID = m_T.ID
	from #maps m_T
	left outer join #maps m_S on (m_T.TargetFusionAttributeID = m_S.SourceFusionAttributeID)

	IF OBJECT_ID('tempdb..#levelMap') IS NOT NULL
		DROP TABLE #levelMap;
	
	;with C as
			(
			  select
				ID,
				SourceFusionAttributeID as SourceID,
				TargetFusionAttributeID as TargetID,
				ID as [UltimateParentID],
				0 as [level] 
			  from 
					#maps
			  where ParentID is null
			  union all
			  select 
					T.ID,
					T.SourceFusionAttributeID as SourceID,			 
					 T.TargetFusionAttributeID as TargetID,
					 C.[UltimateParentID] as [UltimateParentID],
					 C.[level] + 1
			  from #maps as T
				inner join C  
					on T.ParentID = C.ID				  
			)
			select C.ID, C.[level], C.[UltimateParentID]
			into #levelMap
			from C
			OPTION (MAXRECURSION 25) 

	update T
	set T.[level] = S.[level], T.[UltimateParentID] = S.[UltimateParentID]
	from #maps T
	inner join #levelMap S on S.ID = T.ID;
	
	--remove any that we cant find the level for
	--delete from #maps where [level] is null		


	-- find any object related to column as the object	
	insert into #objectmap (MapID, [Object], [ObjectID])
		select T.ID, OI.[subject], OI.[subjectid]
		from #maps T
		inner join [IntersectDetail] OI on OI.Subject <> 'FusionAttribute' and OI.Object = 'FusionAttribute' and OI.ObjectID in (T.SourceFusionAttributeID, T.TargetFusionAttributeID)  and OI.PredicateType = 8-- look for relation between non fusion object and source/target column

	-- find any business terms related to source
	update T
	set T.[source] = OI.[subject], T.[sourceid] = OI.[subjectid]--, T.sourceintersectid = OI.ID
	from #maps T
		inner join [IntersectDetail] OI on OI.Subject <> 'FusionAttribute' and OI.Object = 'FusionAttribute' and OI.ObjectID = T.SourceParentObjectFusionAttributeID  and OI.PredicateType = 8 

	
	-- find any business terms related to target
	update T
	set T.[target] = OI.[subject], T.[targetid] = OI.[subjectid]--, T.targetintersectid = OI.ID
	from #maps T
		inner join [IntersectDetail] OI on OI.Subject <> 'FusionAttribute' and OI.Object = 'FusionAttribute' and OI.ObjectID = T.TargetParentObjectFusionAttributeID and OI.PredicateType = 8
		
	-- update the objects for each path to be the same	
	insert into #objectmap (MapID, [Object], [ObjectID])
		select T.ID, SO.[object], SO.[objectID]
		from #maps T		
		inner join #maps S on T.UltimateParentID = S.UltimateParentID
		inner join #objectmap SO on S.ID = SO.MapID
		left join #objectmap T_O on (T.ID = T_O.MapID and T_O.[object] is null);
	
	
	--take any sources with null targets find the next target

	WITH hierarchy (id, [target], [targetid], [source], [sourceid]) AS
	(
		SELECT id, [target], [targetid], [source], [sourceid]
		FROM #maps
		WHERE [parentid] is null

		UNION ALL

		SELECT mc.id, coalesce(mc.[target], mc.[source], gps.[target]) as [target], coalesce(mc.targetid, mc.sourceid, gps.targetid) as [targetid], coalesce(mc.[source], gps.[target], gps.[source]) as [source], coalesce(mc.sourceid, gps.targetid, gps.sourceid) as [targetid]
		FROM #maps mc
		JOIN hierarchy gps ON gps.id = mc.parentid
	)
	UPDATE T
	set T.[target] = cte.[target], T.[targetid] = cte.[targetid], T.[source] = cte.[source], T.[sourceid] = cte.[sourceid]
	from #maps T
	inner join 
		hierarchy cte
	on cte.id = T.id
	OPTION (MAXRECURSION 50)
			
	-- generate relationships for each unique object / source that dont exist

	update T
	set T.[sourceintersectid] = OI.ID
	from #objectmap T
		inner join #maps M on (T.MapID = M.ID)
		inner join [IntersectDetail] OI on OI.[Subject] = M.[Source] and OI.SubjectID = M.[SourceID] and OI.[Object] = T.[Object] and OI.[ObjectID] = T.[ObjectID];

	update T
	set T.[sourceintersectid] = OI.ID
	from #objectmap T
		inner join #maps M on (T.MapID = M.ID)
		inner join [IntersectDetail] OI on OI.[Object] = M.[Source] and OI.ObjectID = M.[SourceID] and OI.[Subject] = T.[Object] and OI.[SubjectID] = T.[ObjectID] and T.sourceintersectid is null
	
	-- add any missing relations to source / object
	insert into [intersect] (IntersectTypeID, [Subject], SubjectID, [Object], ObjectID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner])
		select distinct
			(select top 1 i_t.ID from [intersecttype] i_t where (i_t.[object] = c_s.objecttype and i_t.[subject] = c_t.objecttype and i_t.objectid = c_s.objecttypeid and i_t.subjectid = c_t.objecttypeid))				
			,T.[Source]
			,T.[SourceID]
			,OM.[Object]
			,OM.[ObjectID]
			,0,getutcdate(),0,getutcdate(),'MARKIT LINEAGE'
		from #maps T
		inner join #objectmap OM on (T.ID = OM.MapID)
		inner join [cache].[objectdetails] c_s on (c_s.[object] = OM.[object] and c_s.[objectid] = OM.[objectid])
		inner join [cache].[objectdetails] c_t on (c_t.[object] = T.[source] and c_t.[objectid] = T.[sourceid])		
		where OM.sourceIntersectID is null;
	
	update OM
	set OM.[sourceintersectid] = OI.ID
	from #objectmap OM
		inner join #maps T on (OM.MapID = T.ID)		
		inner join [IntersectDetail] OI on OI.[Subject] = T.[Source] and OI.SubjectID = T.[SourceID] and OI.[Object] = OM.[Object] and OI.[ObjectID] = OM.[ObjectID] and OM.sourceintersectid is null;

	
	-- generate relationships for each unique object / target that dont exist	
	update OM
	set OM.[targetintersectid] = OI.ID
	from #objectmap OM
		inner join #maps T on (OM.MapID = T.ID)
		inner join [IntersectDetail] OI on OI.[Subject] = T.[Target] and OI.SubjectID = T.[TargetID] and OI.[Object] = OM.[Object] and OI.[ObjectID] = OM.[ObjectID]
		
	update OM
	set OM.[targetintersectid] = OI.ID
	from #objectmap OM
		inner join #maps T on (OM.MapID = T.ID)
		inner join [IntersectDetail] OI on OI.[Object] = T.[Target] and OI.ObjectID = T.[TargetID] and OI.[Subject] = OM.[Object] and OI.[SubjectID] = OM.[ObjectID] and OM.targetintersectid is null;

	-- add any missing relations to source / object
	insert into [intersect] (IntersectTypeID, [Subject], SubjectID, [Object], ObjectID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner])
		select distinct
			(select top 1 i_t.ID from [intersecttype] i_t where (i_t.[object] = c_s.objecttype and i_t.[subject] = c_t.objecttype and i_t.objectid = c_s.objecttypeid and i_t.subjectid = c_t.objecttypeid))			
			,T.[target]
			,T.[targetID]
			,OM.[Object]
			,OM.[ObjectID]
			,0,getutcdate(),0,getutcdate(),'MARKIT LINEAGE'
		from #maps T
		inner join #objectmap OM on (T.ID = OM.MapID)
		inner join [cache].[objectdetails] c_s on (c_s.[object] = OM.[object] and c_s.[objectid] = OM.[objectid])
		inner join [cache].[objectdetails] c_t on (c_t.[object] = T.[target] and c_t.[objectid] = T.[targetid])		
		where OM.targetintersectid is null;
		
	update OM
	set OM.[targetintersectid] = OI.ID
	from #objectmap OM
		inner join #maps T on (OM.MapID = T.ID)
		inner join [IntersectDetail] OI on OI.[Subject] = T.[Target] and OI.SubjectID = T.[TargetID] and OI.[Object] = OM.[Object] and OI.[ObjectID] = OM.[ObjectID] and OM.targetintersectid is null;
	

	/*testing only!!*/			
--	select * from #maps order by [ultimateparentid], [level]
	/*end testing only*/

	print 'Removing any prior generated Markit Lineage map records';

	-- clear any previous values from map rule item map item table
	--delete from mapitem where [owner] = 'MARKIT LINEAGE';
	--delete from mapruleitem where [owner] = 'MARKIT LINEAGE';
	delete from mapruleitemmapitem where [owner] = 'MARKIT LINEAGE';

	print 'Inserting new map records';
	-- insert mapping data
	
	Declare @MapItemIDList Table(MapItemID int, sourceintersectid int, targetintersectid int);
	Declare @MapRuleItemIDList Table(MapRuleItemID int, MapID Int);
	
	-- load any existing map item instances
	update T
	set T.MapItemID = mi.ID
	from #objectmap T
		inner join mapitem mi on(T.sourceintersectid = mi.SourceIntersectID and T.targetintersectid = mi.TargetIntersectID and mi.[Owner] = 'MARKIT LINEAGE'); 

	-- insert map records
	MERGE
	INTO    mapitem mi
	USING   (			
			select distinct sourceintersectid, targetintersectid FROM #objectmap where (sourceintersectid is not null and targetintersectid is not null) and sourceintersectid != targetintersectid and mapitemid is null
			) S
	ON      (1 = 0)
	WHEN NOT MATCHED THEN
	INSERT  (SourceIntersectID, TargetIntersectID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner])
	VALUES  (S.sourceintersectid, S.targetintersectid, 0, getutcdate(), 0, getutcdate(), 'MARKIT LINEAGE')
	OUTPUT  INSERTED.ID, S.sourceintersectid, S.targetintersectid into @MapItemIDList;

	--update map item id from main temp table
	update T
	set T.mapitemid = MI.MapItemID
	from #objectmap T
		inner join @MapItemIDList MI on (MI.sourceintersectid = T.sourceintersectid and MI.targetintersectid = T.targetintersectid)
		
	-- delete any mapitem records that are not in objectmap that are markit lineage
	delete from mapitem where [owner] = 'MARKIT LINEAGE' and id not in (select mapitemid from #objectmap);
	
	-- load id's of existing mapruleitems
	update T
	set T.mapruleitemid = S.id
	from #maps T
		inner join [dbo].[mapruleitem] S on (S.[owner] = 'MARKIT LINEAGE' and S.SourceFusionAttributeID = T.SourceFusionAttributeID and S.TargetFusionAttributeID = T.TargetFusionAttributeID);
	
	-- insert the mapruleitem records
	MERGE
	INTO    mapruleitem mri
	USING   (
			select SourceFusionAttributeID, TargetFusionAttributeID, ID from #maps where mapruleitemid is null
			) S
	ON      (1 = 0)
	WHEN NOT MATCHED THEN
	INSERT  (SourceFusionAttributeID, TargetFusionAttributeID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner])
	VALUES  (S.SourceFusionAttributeID, S.TargetFusionAttributeID, 0, getutcdate(), 0, getutcdate(), 'MARKIT LINEAGE')
	OUTPUT  INSERTED.ID, S.ID into @MapRuleItemIDList;
	
	--update map rule item id from main temp table
	update T
	set T.MapRuleItemID = MI.MapRuleItemID
	from #maps T
		inner join @MapRuleItemIDList MI on (MI.MapID = T.ID);

	-- delete any mapitem records that are not in objectmap that are markit lineage
	delete from mapruleitem where [owner] = 'MARKIT LINEAGE' and id not in (select MapRuleItemID from #maps);
			
	--insert mapruleitemmapitem records
	insert into mapruleitemmapitem 
		(MapRuleItemID, MapItemID, [Owner])
		SELECT distinct M.MapRuleItemID, OM.MapItemID , 'MARKIT LINEAGE'
		FROM #maps M 
		inner join #objectmap OM on(M.ID = OM.MapID)
		where M.MapRuleItemID is not null and OM.MapItemID is not null;	

	declare @mapruleitemmapitemCount int;
	select @mapruleitemmapitemCount = count(1) from mapruleitemmapitem where [owner] = 'MARKIT LINEAGE'
	print 'Inserted [' + cast(@mapruleitemmapitemCount as varchar) + '] mapruleitemmapitem records';			

	declare @mapruleitemCount int;
	select @mapruleitemCount = count(1) from mapruleitem where [owner] = 'MARKIT LINEAGE'
	print 'Inserted [' + cast(@mapruleitemCount as varchar) + '] mapruleitem records';			

	declare @mapitemCount int;
	select @mapitemCount = count(1) from mapitem where [owner] = 'MARKIT LINEAGE'
	print 'Inserted [' + cast(@mapitemCount as varchar) + '] mapitem records';
			
end
GO

alter procedure [fusion].[ProcessFusionCacheInQueue]
--declare
	@FusionID int
--set @FusionID = 15
as
begin
	SET NOCOUNT, ANSI_PADDING ON;
	SET ANSI_WARNINGS ON;

	UPDATE  FusionAttribute
	SET		TextPath = utility.GetBreadcrumbStringWrapper('FusionAttribute', ID, '.')
	FROM	FusionAttribute 
	WHERE	FusionID = @FusionID and deleted = 0

	-- if this is a markit fusion update the lineage
	declare @fusionTypeId int;

	select 
		@fusionTypeId = fusiontypeid
	from
		fusion
	where
		id = @FusionID;

	if @fusionTypeId = 13
	begin		
		exec fusion.GenerateMarkitMapLineageData @FusionID
	end
	else if @fusionTypeId = 16
	begin
		exec [fusion].[GenerateEagleLineageData] @FusionID
	end
	else if @fusionTypeId = 111
	begin
		exec [fusion].[GenerateFoundationLineage] @FusionID
	end
	
end
GO

ALTER PROCEDURE [dbo].[GetAssetHierarchy]
  ( @ID int,
    @Type varchar(50) )
	AS
	Begin
	-- declare  @ID int =2430947 ;--4832;-- 16113;
	--declare @Type varchar(50) ='FusionAttribute';--'Taxonomy'; --'Artifact'
    if @Type = 'Artifact'  
	  begin  
	   with Sub as (  
		select O.ID,  
		  O.ParentID,  
		  D.TextPath as [Path],  
		  D.TypeName as LevelName,  
		  D.Url as Url,  
		  1 as [Level]  
		from Artifact O  
		 cross apply [utility].[ObjectDetail](@Type,O.ID) as D  
		where O.ID = @ID  
		union all  
		select O.ID,  
		  O.ParentID,  
		  D.TextPath as [Path], 
		  D.TypeName as LevelName,
		  D.Url as Url,   
		  C.[Level] + 1 as [Level]  
		from Artifact O  
		  inner join Sub as C on C.ParentID = O.ID  
		   cross apply [utility].[ObjectDetail](@Type,O.ID) as D  
	   )  
	  select ID, ParentID,[Path],LevelName,Url,rank() over (Order by Level desc) as [Level] from Sub;
  end
  else if @Type = 'Taxonomy'  
  begin
	   with Sub as (  
		select O.ID,  
		  O.ParentID,  
		  D.TextPath as [Path],  
		  D.TypeName as LevelName,  
		  D.Url as Url,  
		  1 as [Level]  
		from [Taxonomy] O  
		 cross apply [utility].[ObjectDetail](@Type,O.ID) as D  
		where O.ID = @ID  
		union all  
		select O.ID,  
		  O.ParentID,  
		  D.TextPath as [Path], 
		  D.TypeName as LevelName,
		  D.Url as Url,   
		  C.[Level] + 1 as [Level]  
		from [Taxonomy] O  
		  inner join Sub as C on C.ParentID = O.ID  
		   cross apply [utility].[ObjectDetail](@Type,O.ID) as D  
	   )  
	  select ID, ParentID,[Path],LevelName,Url,rank() over (Order by Level desc) as [Level] from Sub;
  end
  else if @Type = 'FusionAttribute'   
  begin  
	   with Sub as (  
		select O.ID,  
		  O.ParentID,  
		  D.TextPath as [Path],  
		  D.TypeName as LevelName,  
		  D.Url as Url,  
		  1 as [Level]  
		from FusionAttribute O  
		 cross apply [utility].[ObjectDetail](@Type,O.ID) as D  
		where O.ID = @ID  
		union all  
		select O.ID,  
		  O.ParentID,  
		  D.TextPath as TextPath, 
		  D.TypeName as LevelName,
		  D.Url as Url,   
		  C.[Level] + 1 as [Level]  
		from FusionAttribute O  
		  inner join Sub as C on C.ParentID = O.ID  
		   cross apply [utility].[ObjectDetail](@Type,O.ID) as D  
	   )  
	  select ID, ParentID,[Path],LevelName,Url,rank() over (Order by Level desc) as [Level] from Sub;
  end
  end
GO

ALTER TABLE [api].[EntityFieldType] ADD  CONSTRAINT [PK_Api_EntityFieldType] PRIMARY KEY NONCLUSTERED 
(
	[EntityID] ASC,
	[FieldTypeID] ASC
)
GO

CREATE NONCLUSTERED INDEX [IX_Asset_ObjectID_Include]
    ON [dbo].[Asset]([ObjectID] ASC)
    INCLUDE([ID]);
GO

--alter table FieldType add [UpdatedBy] INT CONSTRAINT [DF_FieldType_UpdatedBy] DEFAULT ((0)) NOT NULL
--GO

alter table integration.ExecutionAssetType add [ErrorMessage]            NVARCHAR (2500) NULL
GO

alter table integration.Setting add [PageSize]          INT             CONSTRAINT [IntegrationSetting_PageSize] DEFAULT ((500)) NOT NULL
GO
