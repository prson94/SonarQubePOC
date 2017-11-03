CREATE VIEW [dbo].[AttributeDetail]
AS
	select	A.ObjectType,
			A.ObjectID,
			A.AttributeTypeID,
			A.ID,
			A.ParentID,
			T.Name,
			C.Name as AttributeTypeCategory,
			T.ShowNameInTree,
			A.DisplayValue as FormattedValue
	from	Attribute A
			inner join AttributeType T on A.AttributeTypeID = T.ID
			left join AttributeTypeCategory C on C.ID = T.AttributeTypeCategoryID
