CREATE view [dbo].[ResponsibilityTypeObjectClaimDetail]
as
	select	O.ID,
			O.ResponsibilityTypeID,
			RT.Name as ResponsibilityType,
			O.Claim,
			O.ClaimObject,
			O.ObjectType,
			O.ObjectID,
			D.Name as ObjectName,
			D.ObjectTypeName
	from	ResponsibilityTypeObjectClaim O
			inner join ResponsibilityType RT on RT.ID = O.ResponsibilityTypeID
			inner join cache.ObjectDetails D on D.[Object] = O.ObjectType and D.ObjectID = O.ObjectID
			--cross apply utility.ObjectDetail(O.ObjectType, O.ObjectID) D
