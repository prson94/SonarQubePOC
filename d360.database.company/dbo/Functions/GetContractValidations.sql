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
		select o.ResourceID, o.OrganizationID, 
		case when (select count(*) from Organization where ID = o.OrganizationID and Accepted = 0) > 0 then
			1
		else
			0
		end as IsFirstUser 
		from OrganizationResource o
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
		case when (C.ContractType = 2 and H.ID is null) or (C.ContractType = 1 and (H2.AcceptedOn is null or H2.AcceptedOn < C.PublishedOn)) then 
			0 
		else 
			1 
		end as Accepted,
		case when (C.ContractType = 2 and H.ID is null) or (C.ContractType = 1 and (H2.AcceptedOn is null or H2.AcceptedOn < C.PublishedOn)) then
			1
		else
			0
		end  as IsFirstUser 
	from [Contract] C 
	left join ContractAcceptance H on H.ContractID = C.ID and H.OrganizationID is null
		and H.AcceptedOn > C.PublishedOn and H.ResourceID = @ResourceID
	left join (
		select max(AcceptedOn) as AcceptedOn, ContractID from ContractAcceptance A
		where organizationid is null and A.ResourceID in (
			select x.ResourceID from 
			(
				select i.OrganizationID, r.ResourceID from OrganizationInvitation i
				inner join reporting.Global_resource r on r.Email = i.Email
				union all
				select o.OrganizationID, o.ResourceID from OrganizationResource o
			) x
			inner join OrganizationResource rr on rr.ResourceID = @ResourceID and x.OrganizationID = rr.OrganizationID
		)
		group by ContractID
	) H2 on H2.ContractID = C.ID
	where 
		C.[State] = 1 and C.OrganizationID is null and C.PublishedOn is not null
		and @ResourceID in ( --if the user isn't in an org or invited, they don't need to accept the default contracts
			select r.ResourceID from OrganizationInvitation i
			inner join reporting.Global_resource r on r.Email = i.Email
			union all
			select o.ResourceID from OrganizationResource o
		)
)