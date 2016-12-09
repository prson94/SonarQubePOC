CREATE view [dbo].[ResponsibilityDetail]
as
	select	P.Visible,
			P.ResponsibilityID,
			P.ResponsibilityTypeID,
			P.AssigningItem as AssigningItemType,
			P.AssigningItemID,
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
			CI.ContextItems
	from	cache.Responsibilities P
			left join [Group] RODG on P.ResponsibleObject = 'Group' and RODG.ID = P.ResponsibleObjectID
			left join [reporting].[Global_Resource] RES on RES.ResourceID = RODG.PrimaryOwnerResourceID
			outer apply (
						select (
								select	D.Name + ': ' + I.Code + '; '
								from	ResponsibilityContextItem C
										inner join ReferenceItem I on C.ObjectType = 'ReferenceItem' and C.ObjectID = I.ID
										inner join ReferenceItemType D on D.ID = I.ReferenceItemTypeID
								where	ResponsibilityID = P.ResponsibilityID
								for xml path ('')--, root('items')
								) as ContextItems
						) CI
	where	[ResponsibilityTypeGroup] = 1

