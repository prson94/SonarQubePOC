CREATE view [dbo].[SecurityDetail]
as
	select	'Resource' as ResponsibleObjectType,
			RD.ResourceID as ResponsibleObjectID,
			RD.Object as ObjectType,
			RD.ObjectID,
			RTC.Claim,
			RTC.ClaimObject
	from	ResponsibilityDetails RD
			inner join ResponsibilityTypeObjectClaim RTC	on RTC.ResponsibilityTypeID = RD.ResponsibilityTypeID 
															and RTC.ObjectType = RD.Type 
															and RTC.ObjectID = RD.TypeID
