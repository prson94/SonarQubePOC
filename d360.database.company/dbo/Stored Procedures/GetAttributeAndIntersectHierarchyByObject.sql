
CREATE PROC [dbo].[GetAttributeAndIntersectHierarchyByObject]
	@type varchar(25),
	@id int
as
begin
	select	'Attribute|' + cast(A.ID as varchar(25)) as ID,
			case 
				when A.ParentID is not null then  'Attribute|' + cast(A.ParentID as varchar(25)) 
				when A.ObjectType = @type and A.ObjectID = @id then NULL
				else A.ObjectType + '|' + cast(A.ObjectID as varchar(25)) 
			end	as ParentID,
			A.AttributeTypeID as TypeID,
			A.Name as ObjectTypeName,
			'Attribute' as ObjectType,
			A.ID as ObjectID,
			A.ObjectType as ParentObjectType,
			A.ObjectID as ParentObjectID,
			'Attribute' as TargetObjectType,
			A.ID as TargetObjectID,
			T.IsTechnical,
			A.FormattedValue as Name,
			A.AttributeTypeCategory,
			A.ShowNameInTree
	from	AttributeDetail A
			INNER JOIN	(
						SELECT	@type + '|' +cast(@id as varchar(25)) as ID,
								cast(0 as bit) as IsTechnical
						) T	ON	A.ObjectType + '|' + cast(A.ObjectID as varchar(25)) = T.ID
end