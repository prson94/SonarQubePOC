CREATE FUNCTION dbo.GetFusionOwnershipHierarchy
(
--declare
	@FusionID int,
	@ObjectType varchar(50),
	@ObjectID int
--set @FusionID = 0
--set @ObjectType = 'Artifact'
--set @ObjectID = 15
)
RETURNS 
--declare
@owners TABLE
(
		OwnerRuleID int,
		ObjectType varchar(25),
		ObjectID int,
		ParentObjectType varchar(25),
		ParentObjectID int,
		RelationshipOwnerObjectType varchar(25),
		RelationshipOwnerObjectID int,
		FusionID int,
		FusionAttributeID int,
		Name nvarchar(250),
		ParentFusionAttributeTypeID int
)
AS
BEGIN

	insert into @owners
		select	R.ID,
				R.ObjectType,
				R.ObjectID,
				R.ParentObjectType,
				R.ParentObjectID,
				R.RelationshipOwnerObjectType,
				R.RelationshipOwnerObjectID,
				R.FusionID,
				A.ID,
				A.Name,
				A.FusionAttributeTypeID
		from	FusionAttributeOwnerRule R
				inner join FusionAttribute A	on R.ObjectType = 'FusionAttributeType' 
												and R.ObjectID = A.FusionAttributeTypeID
												and R.ParentObjectType is null
												and ( 
													(R.FusionID = @FusionID and @FusionID > 0) OR 
													(R.RelationshipOwnerObjectType = @ObjectType and R.RelationshipOwnerObjectID = @ObjectID and @ObjectID > 0)
													);

	declare @tb table (
		ID int, ParentID int, Name nvarchar(250), 
		FusionAttributeTypeID int, MyFusionAttributeTypeID int, 
		OwnerRuleID int,
		ParentObjectType varchar(25), ParentObjectID int,
		RelationshipOwnerObjectType varchar(25), RelationshipOwnerObjectID int,
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
				R.ID as OwnerRuleID,
				R.ParentObjectType,
				R.ParentObjectID,
				R.RelationshipOwnerObjectType,
				R.RelationshipOwnerObjectID,
				R.FusionID
		from	FusionAttribute A
				inner join FusionAttributeOwnerRule R	on R.ParentObjectType is not null
														and R.ObjectType = 'FusionAttributeType' 
														and R.ObjectID = A.FusionAttributeTypeID
														and R.FusionID = A.FusionID
														and ( 
															(A.FusionID = @FusionID and @FusionID > 0) OR 
															(R.RelationshipOwnerObjectType = @ObjectType and R.RelationshipOwnerObjectID = @ObjectID and @ObjectID > 0)
															)
		union all
		select	A.ID,
				A.ParentID,
				A.Name,
				h.FusionAttributeTypeID,
				A.FusionAttributeTypeID as MyFusionAttributeTypeID,
				h.OwnerRuleID,
				h.ParentObjectType,
				h.ParentObjectID,
				h.RelationshipOwnerObjectType,
				h.RelationshipOwnerObjectID,
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

	merge	@owners as t
	using	(select * from h where FusionAttributeTypeID = MyFusionAttributeTypeID) as h
	on		(t.FusionID = h.FusionID and t.FusionAttributeID = h.ID)
	when	matched then
	update	set
			t.ParentObjectType				= h.ParentObjectType,
			t.ParentObjectID				= h.ParentObjectID,
			t.RelationshipOwnerObjectType	= h.RelationshipOwnerObjectType,
			t.RelationshipOwnerObjectID		= h.RelationshipOwnerObjectID
	when	not matched then
	insert	(
			OwnerRuleID, 
			ObjectType, ObjectID, 
			ParentObjectType, ParentObjectID, 
			RelationshipOwnerObjectType, RelationshipOwnerObjectID, 
			FusionID, FusionAttributeID, Name, ParentFusionAttributeTypeID
			)
	values	(
			h.OwnerRuleID,
			'FusionAttributeType', h.FusionAttributeTypeID,
			h.ParentObjectType, h.ParentObjectID,
			h.RelationshipOwnerObjectType, h.RelationshipOwnerObjectID,
			h.FusionID,	h.ID, h.Name, h.FusionAttributeTypeID
			);

	--select * from @owners 
	RETURN
END