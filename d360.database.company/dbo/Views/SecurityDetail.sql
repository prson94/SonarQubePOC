CREATE view [dbo].[SecurityDetail]
as
	select	case 
				when RG.ResourceID is not null then 'Resource'
				else RD.ResponsibleObjectType
			end as ResponsibleObjectType,
			COALESCE(RG.ResourceID, RD.ResponsibleObjectID) as ResponsibleObjectID,
			--COALESCE(CR.FirstName + ' ' + CR.LastName, RD.ResponsibleObjectName) as ResponsibleObjectName,
			RD.ObjectType,
			RD.ObjectID,
			RTC.Claim,
			RTC.ClaimObject
	from	ResponsibilityDetail RD
			inner join cache.ObjectDetails D on D.[Object] = RD.ObjectType and D.ObjectID = RD.ObjectID
			--cross apply utility.ObjectDetail(RD.ObjectType, RD.ObjectID) D
			inner join ResponsibilityTypeObjectClaim RTC	on RTC.ResponsibilityTypeID = RD.ResponsibilityTypeID 
															and RTC.ObjectType = D.ObjectType 
															and RTC.ObjectID = D.ObjectTypeID
			left join [Group] G on RD.ResponsibleObjectType = 'Group' and G.ID = RD.ResponsibleObjectID
			left join ResourceGroup RG on RG.GroupID = G.ID
