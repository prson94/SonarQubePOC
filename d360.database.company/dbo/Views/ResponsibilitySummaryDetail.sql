CREATE view [dbo].[ResponsibilitySummaryDetail]
as
	select	ResponsibilityID,
			ResponsibilityTypeID,
			R.[Object] as ObjectType,
			R.ObjectID,
			R.ObjectName,
			R.ObjectUrl,
			R.ObjectTypeName,
			R.ResponsibleObject as ResponsibleObjectType,
			R.ResponsibleObjectID,
			R.ResponsibleObjectName,
			R.ResponsibleObjectUrl,
			ROD.ObjectTypeName as ResponsibleObjectTypeName,
			R.ResponsibilityType,
			ResponsibilityTypeGroup
	from	cache.Responsibilities R
			left join cache.ObjectDetails ROD on ROD.[Object] = R.ResponsibleObject and ROD.ObjectID = R.ResponsibleObjectID
GO