CREATE procedure [tile].[GetRedFlagSummariesByResource]
--declare
	@resourceID int
--set @resourceID = 1
as
begin
	with f as	(
				select	ObjectType, 
						ObjectID 
				from	Follow 
				where	ResourceID = @resourceID				
				),
		rg as	(
				select	ObjectType,
						ObjectID
				from	ResponsibilityDetail rd
						inner join ResourceGroup rg on rd.ResponsibleObjectType = 'Group' and rg.GroupID = rd.ResponsibleObjectID and rg.ResourceID = @resourceID
				),
		r as	(
				select	ObjectType,
						ObjectID
				from	ResponsibilityDetail
				where	ResponsibleObjectType = 'Resource'
						and ResponsibleObjectID = @resourceID
				)

	select		FD.ObjectTypeID as TypeID,
				FD.ObjectType as Type,
				FD.ObjectTypeName as TypeName,
				count(1) as RedFlagCount,
				SUM(CR.[Count]) as CriticalRelationshipCount
	from		(
				select	distinct 
						* 
				from	(
						select	* from f
						union
						select	* from r
						union
						select	* from rg
						) i
				) F
				inner join AlertFlag A on A.ObjectType = F.ObjectType and A.ObjectID = F.ObjectID and A.Active = 1
				inner join cache.ObjectDetails FD on FD.[Object] = F.ObjectType and FD.ObjectID = F.ObjectID --cross apply utility.ObjectDetail(F.ObjectType, F.ObjectID) FD
				outer apply (
							select		count(1) as [Count] 
							from		IntersectNode S
										inner join [Intersect] I on S.ObjectType = F.ObjectType 
																	and S.ObjectID = F.ObjectID
																	and I.ID = S.IntersectID 
																	and I.Classification = 1
							) CR
	group by	FD.ObjectTypeID,
				FD.ObjectType,
				FD.ObjectTypeName
end
