CREATE FUNCTION [dbo].[GetFusionPromotionsHierarchy]
(
)
RETURNS @promotions TABLE
(
		PromotionRuleID int,
		ObjectType varchar(25),
		ObjectID int,
		ParentObjectType varchar(25),
		ParentObjectID int,
		PromotionObjectType varchar(25),
		PromotionObjectID int,
		PromotionParentObjectType varchar(25),
		PromotionParentObjectID int,
		FusionID int,
		FusionAttributeID int,
		Name nvarchar(250),
		ParentFusionAttributeTypeID int
)
AS
BEGIN

	insert into @promotions
		select	R.ID,
				R.ObjectType,
				R.ObjectID,
				R.ParentObjectType,
				R.ParentObjectID,
				R.PromotionObjectType,
				R.PromotionObjectID,
				R.PromotionParentObjectType,
				R.PromotionParentObjectID,
				R.FusionID,
				A.ID,
				A.Name,
				A.FusionAttributeTypeID
		from	FusionAttributePromotionRule R
				inner join FusionAttribute A	on R.ObjectType = 'FusionAttributeType' 
												and R.FusionID = A.FusionID
												and R.ObjectID = A.FusionAttributeTypeID
												and R.ParentObjectType is null
												and R.[Enabled] = 1;

	declare @tb table (
		ID int, ParentID int, Name nvarchar(250), 
		FusionAttributeTypeID int, MyFusionAttributeTypeID int, 
		PromotionRuleID int,
		ParentObjectType varchar(25), ParentObjectID int,
		PromotionObjectType varchar(25), PromotionObjectID int,
		PromotionParentObjectType varchar(25), PromotionParentObjectID int,
		FusionID int
		);

	with h as
	(
		select	distinct 
				A.ID,
				A.ParentID,
				A.Name,
				A.FusionAttributeTypeID,
				A.FusionAttributeTypeID as MyFusionAttributeTypeID,
				R.ID as PromotionRuleID,
				R.ParentObjectType,
				R.ParentObjectID,
				R.PromotionObjectType,
				R.PromotionObjectID,
				R.PromotionParentObjectType,
				R.PromotionParentObjectID,
				R.FusionID
		from	FusionAttribute A
				inner join FusionAttributePromotionRule R	on R.ParentObjectType is not null
															and R.ObjectType = 'FusionAttributeType' 
															and R.ObjectID = A.FusionAttributeTypeID
		union all
		select	A.ID,
				A.ParentID,
				A.Name,
				h.FusionAttributeTypeID,
				A.FusionAttributeTypeID as MyFusionAttributeTypeID,
				h.PromotionRuleID,
				h.ParentObjectType,
				h.ParentObjectID,
				h.PromotionObjectType,
				h.PromotionObjectID,
				h.PromotionParentObjectType,
				h.PromotionParentObjectID,
				h.FusionID
		from	FusionAttribute A
				inner join h on h.ParentID = A.ID
	)

	insert into @tb
		select distinct * from h;

	with h as (
		select	*
		from	@tb
		where	ID = ParentObjectID
		union all
		select	C.*
		from	@tb C
				inner join h on C.FusionID = h.FusionID and C.ParentID = h.ID

	)

	merge	@promotions as t
	using	(select * from h where FusionAttributeTypeID = MyFusionAttributeTypeID) as h
	on		(t.FusionID = h.FusionID and t.FusionAttributeID = h.ID)
	when	matched then
	update	set
			t.ParentObjectType			= h.ParentObjectType,
			t.ParentObjectID			= h.ParentObjectID,
			t.PromotionObjectType		= h.PromotionObjectType,
			t.PromotionObjectID			= h.PromotionObjectID,
			t.PromotionParentObjectType = h.PromotionParentObjectType,
			t.PromotionParentObjectID	= h.PromotionParentObjectID
	when	not matched then
	insert	(
			PromotionRuleID, 
			ObjectType, ObjectID, 
			ParentObjectType, ParentObjectID, 
			PromotionObjectType, PromotionObjectID, 
			PromotionParentObjectType, PromotionParentObjectID, 
			FusionID, FusionAttributeID, Name, ParentFusionAttributeTypeID
			)
	values	(
			h.PromotionRuleID,
			'FusionAttributeType', h.FusionAttributeTypeID,
			h.ParentObjectType, h.ParentObjectID,
			h.PromotionObjectType, h.PromotionObjectID,
			h.PromotionParentObjectType,	h.PromotionParentObjectID,
			h.FusionID,	h.ID, h.Name, h.FusionAttributeTypeID
			);

	RETURN
END
