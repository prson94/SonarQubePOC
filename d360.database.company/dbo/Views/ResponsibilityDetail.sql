CREATE view [dbo].[ResponsibilityDetail]
as
	select	P.Visible,
			P.ResponsibilityID,
			P.ResponsibilityTypeID,
			P.AssigningItem as AssigningItemType,
			P.AssigningItemID,
			P.AssigningItemName,
			P.AssigningItemUrl,
			P.AssigningTypeName,
			AID.IconBackColor as AssigningIconBackColor,
			AID.IconForeColor as AssigningIconForeColor,
			AID.IconText as AssigningIconText,
			P.[Object] as ObjectType,
			P.ObjectID,
			P.ObjectName,
			P.ObjectTypeID,
			P.ObjectTypeName,
			P.ObjectUrl,
			P.ResponsibleObject as ResponsibleObjectType,
			P.ResponsibleObjectID,
			P.ResponsibleObjectName,
			P.ResponsibleObjectUrl,
			RODG.PrimaryOwnerResourceID,
			RES.FirstName + ' ' + RES.LastName as PrimaryOwnerResourceName,
			case 
				when RODG.PrimaryOwnerResourceID is null then ''
				else '#/resources/' + cast(RODG.PrimaryOwnerResourceID as varchar(10))
			end as PrimaryOwnerResourceUrl,
			P.ResponsibilityType as [Role],
			dbo.GetObjectStatisticScore(P.[Object], P.ObjectID) as CurrentScore,
			CI.ContextItems,
				case 
					when AF.Active is null then cast(0 as bit)
					else AF.Active
				end as RedFlagged
	from	cache.Responsibilities P
			left join [Group] RODG on P.ResponsibleObject = 'Group' and RODG.ID = P.ResponsibleObjectID
			left join [reporting].[Global_Resource] RES on RES.ResourceID = RODG.PrimaryOwnerResourceID
			inner join cache.ObjectDetails AID on AID.[Object] = P.AssigningItem and AID.ObjectID = P.AssigningItemID
			outer apply (
						select (
								select	D.Name + ': ' + I.Code + ' - ' + I.Name + '; '
								from	ResponsibilityContextItem C
										inner join DomainItem I on C.ObjectType = 'DomainItem' and C.ObjectID = I.ID
										inner join Domain D on D.ID = I.DomainID
								where	ResponsibilityID = P.ResponsibilityID
								for xml path ('')--, root('items')
								) as ContextItems
						) CI
			LEFT JOIN AlertFlag AF on AF.ObjectType = P.[Object] and AF.ObjectID = P.ObjectID and AF.Active = 1
	where	[ResponsibilityTypeGroup] = 1
	--select	P.ResponsibilityID,
	--		R.ResponsibilityTypeID,
	--		P.AssigningItemType,
	--		P.AssigningItemID,
	--		AID.Name as AssigningItemName,
	--		AID.Url as AssigningItemUrl,
	--		AID.ObjectTypeName as AssigningTypeName,
	--		AID.IconBackColor as AssigningIconBackColor,
	--		AID.IconForeColor as AssigningIconForeColor,
	--		AID.IconText as AssigningIconText,
	--		P.ObjectType,
	--		P.ObjectID,
	--		OD.Name as ObjectName,
	--		OD.ObjectTypeID as ObjectTypeID,
	--		OD.ObjectTypeName as ObjectTypeName,
	--		OD.Url as ObjectUrl,
	--		R.ResponsibleObjectType,
	--		R.ResponsibleObjectID,
	--		ROD.Name as ResponsibleObjectName,
	--		ROD.Url as ResponsibleObjectUrl,
	--		RODG.PrimaryOwnerResourceID,
	--		RES.FirstName + ' ' + RES.LastName as PrimaryOwnerResourceName,
	--		case 
	--			when RODG.PrimaryOwnerResourceID is null then ''
	--			else '#/resources/' + cast(RODG.PrimaryOwnerResourceID as varchar(10))
	--		end as PrimaryOwnerResourceUrl,
	--		RT.Name as [Role],
	--		dbo.GetObjectStatisticScore(P.ObjectType, P.ObjectID) as CurrentScore,
	--		CI.ContextItems,
	--			case 
	--				when AF.Active is null then cast(0 as bit)
	--				else AF.Active
	--			end as RedFlagged
	--from	utility.ResponsibilityHierarchy P
	--		inner join Responsibility R on R.ID = P.ResponsibilityID
	--		inner join ResponsibilityType RT on RT.ID = R.ResponsibilityTypeID and RT.ResponsibilityTypeGroup = 1
	--		left join cache.ObjectDetails ROD on ROD.[Object] = R.ResponsibleObjectType and ROD.ObjectID = R.ResponsibleObjectID
	--		left join [Group] RODG on R.ResponsibleObjectType = 'Group' and RODG.ID = R.ResponsibleObjectID
	--		left join [reporting].[Global_Resource] RES on RES.ResourceID = RODG.PrimaryOwnerResourceID
	--		inner join cache.ObjectDetails AID on AID.[Object] = P.AssigningItemType and AID.ObjectID = P.AssigningItemID
	--		inner join cache.ObjectDetails OD on OD.[Object] = P.ObjectType and OD.ObjectID = P.ObjectID
	--		outer apply (
	--					select (
	--							select	D.Name + ': ' + I.Code + ' - ' + I.Name + '; '
	--							from	ResponsibilityContextItem C
	--									inner join DomainItem I on C.ObjectType = 'DomainItem' and C.ObjectID = I.ID
	--									inner join Domain D on D.ID = I.DomainID
	--							where	ResponsibilityID = P.ResponsibilityID
	--							for xml path ('')--, root('items')
	--							) as ContextItems
	--					) CI
	--		LEFT JOIN AlertFlag AF on AF.ObjectType = P.ObjectType and AF.ObjectID = P.ObjectID and AF.Active = 1

