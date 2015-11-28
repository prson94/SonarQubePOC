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
	--where	R.Visible = 0
	--select	R.ID as ResponsibilityID,
	--		R.ResponsibilityTypeID,
	--		R.ObjectType,
	--		R.ObjectID,
	--		OD.Name as ObjectName,
	--		OD.Url as ObjectUrl,
	--		OD.ObjectTypeName,
	--		R.ResponsibleObjectType,
	--		R.ResponsibleObjectID,
	--		ROD.Name as ResponsibleObjectName,
	--		ROD.Url as ResponsibleObjectUrl,
	--		ROD.ObjectTypeName as ResponsibleObjectTypeName,
	--		RT.Name as ResponsibilityType,
	--		RT.ResponsibilityTypeGroup
	--from	Responsibility R
	--		inner join ResponsibilityType RT on RT.ID = R.ResponsibilityTypeID
	--		left join cache.ObjectDetails ROD on ROD.[Object] = R.ResponsibleObjectType and ROD.ObjectID = R.ResponsibleObjectID
	--		inner join cache.ObjectDetails OD on OD.[Object] = R.ObjectType and OD.ObjectID = R.ObjectID
GO