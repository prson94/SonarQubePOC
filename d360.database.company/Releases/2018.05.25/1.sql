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
				T.ResponsibilityTypeID = S.ResponsibilityTypeID
	when	not matched by target then
			insert (RuleID, ResponsibilityTypeID, AssetID, Object, ObjectID, AssetTypeID, Type, TypeID, SecurityAsset, SecurityAssetID, Context, ApplyToType, IsVisible, Overriden, OverrideItemID)
			values (S.RuleID, S.ResponsibilityTypeID, S.AssetID, S.Object, S.ObjectID, S.AssetTypeID, S.Type, S.TypeID, S.SecurityAsset, S.SecurityAssetID, S.Context, 0, 1, 0, S.ID);

	--update	T
	--set		T.AssetID = S.AssetID,
	--		T.ResponsibilityTypeID = S.ResponsibilityTypeID,
	--		T.SecurityAsset = S.SecurityAsset,
	--		T.SecurityAssetID = S.SecurityAssetID
	--from	ResponsibilityTypeRelationItem T
	--		inner join inserted S on S.ID = T.OverrideItemID
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