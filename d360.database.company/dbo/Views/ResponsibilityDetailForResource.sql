
CREATE view [dbo].[ResponsibilityDetailForResource]
as
	select	RD.Visible,
			RD.ResponsibilityID,
			RD.ResponsibilityTypeID,
			RD.ObjectType,
			RD.ObjectTypeID,
			RD.ObjectID,
			RD.ObjectName,
			RD.ObjectTypeName,
			RD.ObjectUrl,
			case 
				when RG.ResourceID is not null then 'Resource'
				else RD.ResponsibleObjectType
			end as ResponsibleObjectType,
			COALESCE(RG.ResourceID, RD.ResponsibleObjectID) as ResponsibleObjectID,
			case RD.ResponsibleObjectType
				when 'Group' then cast(1 as bit)
				else cast(0 as bit)
			end as FromGroup,
			RD.Role,
			RD.ContextItems,
			RD.CurrentScore
	from	ResponsibilityDetail RD
			left join [Group] G on RD.ResponsibleObjectType = 'Group' and G.ID = RD.ResponsibleObjectID
			left join ResourceGroup RG on RG.GroupID = G.ID
