create procedure [dbo].GetContractValidations
	@ResourceID int
as
begin

		select 
			C.ID as ContractID, 
			case when H.ID is null then
				0
			else
				1
			end as Accepted,
			dbo.OrganizationAccepted(O.ID) as OrgActive
		from [Contract] c
		inner join Organization O on O.ID = C.OrganizationID
		inner join Organizationresource R on R.OrganizationID = O.ID and R.ResourceID = @ResourceID
		left join ContractAcceptanceHistory H on H.ContractID = c.ID and H.OrganizationID = C.OrganizationID 
			and H.AcceptedOn >= C.PublishedOn and H.ResourceID = @ResourceID
		where C.[State] = 1 and C.ContractType = 2 and C.PublishedOn is not null
		union all
		select 
		C.ID as ContractID, 
			case when H.ID is null then
				0
			else
				1
			end as Accepted,
			1 as OrgActive
		from [Contract] c
		inner join (
			select top 1 
				R.* 
			from OrganizationResource R
			inner join Organization O on O.ID = R.Organizationid and O.[State] = 1
			where R.resourceid = @ResourceID
		) R on 1=1
		left join ContractAcceptanceHistory H on H.ContractID = C.ID and H.OrganizationID is null
			and H.AcceptedOn >= C.PublishedOn and H.ResourceID = @ResourceID
		where C.[State] = 1  and C.ContractType = 2 and C.OrganizationID is null and C.PublishedOn is not null;

end
go