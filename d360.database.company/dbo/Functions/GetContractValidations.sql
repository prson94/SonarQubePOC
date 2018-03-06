CREATE FUNCTION [dbo].[GetContractValidations] 
(	
	@ResourceID int
)
RETURNS TABLE 
AS
RETURN 
(
--declare @ResourceID int;
--select @ResourceID = 3243;

select 
		C.ID as ContractID, 
		C.OrganizationID, 
		case when H.ID is null then 
			0 
		else 
			1 
		end as Accepted 
	from [Contract] C 
	inner join 
	(
		select r.ResourceID, i.OrganizationID from OrganizationInvitation i
		inner join reporting.Global_resource r on r.Email = i.Email
		union all
		select o.ResourceID, o.OrganizationID from OrganizationResource o
	) R on R.OrganizationID = C.organizationID and R.ResourceID = @ResourceID
	left join ContractAcceptance H on H.ContractID = C.ID and H.OrganizationID = C.OrganizationID 
		and H.AcceptedOn > C.PublishedOn and H.ResourceID = R.ResourceID
	where 
		C.[State] = 1 and C.PublishedOn is not null and C.OrganizationID is not null

	union all

	select 
		C.ID as ContractID, 
		null as OrganizationID, 
		case when H.ID is null then 
			0 
		else 
			1 
		end as Accepted 
	from [Contract] C 
	left join ContractAcceptance H on H.ContractID = C.ID and H.OrganizationID is null
		and H.AcceptedOn > C.PublishedOn and H.ResourceID = @ResourceID
	where 
		C.[State] = 1 and C.OrganizationID is null and C.PublishedOn is not null
)
