
CREATE VIEW [dbo].[DomainAllocationDetail]
as
	select	F.Value as DomainID,
			A.AttributeTypeID,
			F.ObjectType as LocationType,
			AT.Name as Location,
			D.ObjectTypeName as [Type],
			D.Name
	from	(select * from FieldWithRelation where LookupObjectType = 'Domain') F
			INNER JOIN Attribute A on F.ObjectType = 'Attribute' and A.ID = F.ObjectID 
			inner join AttributeType AT on AT.ID = A.AttributeTypeID
			inner join cache.ObjectDetails D on D.[Object] = A.ObjectType and D.ObjectID = A.ObjectID
			--CROSS APPLY utility.ObjectDetail(A.ObjectType, A.ObjectID) D
