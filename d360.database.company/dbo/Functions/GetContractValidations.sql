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
		C.ContractType,
		case when H.ID is null then 
			0 
		else 
			1 
		end as Accepted,
		R.IsFirstUser 
	from [Contract] C 
	inner join 
	( 
		select r.ResourceID, i.OrganizationID, 
		case when (select count(*) from OrganizationResource where OrganizationID = i.OrganizationID and Accepted = 1) > 0 then
			0
		else
			1
		end as IsFirstUser
		from OrganizationInvitation i
		inner join reporting.Global_resource r on r.Email = i.Email
		union all
		select o.ResourceID, o.OrganizationID, 0 as IsFirstUser from OrganizationResource o
	) R on R.OrganizationID = C.organizationID and R.ResourceID = @ResourceID
	left join ContractAcceptance H on H.ContractID = C.ID and H.OrganizationID = C.OrganizationID 
		and H.AcceptedOn > C.PublishedOn and H.ResourceID = R.ResourceID
	where 
		C.[State] = 1 and C.PublishedOn is not null and C.OrganizationID is not null

	union all

	select 
		C.ID as ContractID, 
		null as OrganizationID, 
		C.ContractType,
		case when H.ID is null then 
			0 
		else 
			1 
		end as Accepted,
		0 as IsFirstUser 
	from [Contract] C 
	left join ContractAcceptance H on H.ContractID = C.ID and H.OrganizationID is null
		and H.AcceptedOn > C.PublishedOn and H.ResourceID = @ResourceID
	where 
		C.[State] = 1 and C.OrganizationID is null and C.PublishedOn is not null
		and @ResourceID in ( --if the user isn't in an org or invited, they don't need to accept the default contracts
			select r.ResourceID from OrganizationInvitation i
			inner join reporting.Global_resource r on r.Email = i.Email
			union all
			select o.ResourceID from OrganizationResource o
		)
)
