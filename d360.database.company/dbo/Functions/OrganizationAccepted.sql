CREATE FUNCTION [dbo].[OrganizationAccepted]
(
    @OrganizationID int
)
RETURNS bit
AS
BEGIN

declare @accepted bit;
set @accepted = 1;

	select 
		@accepted = 
		case when count(*) > 0 then
			0
		else
			1
		end
	from (
		select 
			C.ID as ContractID, 
			case when H.ID is null then
				0
			else
				1
			end as Accepted
		from [Contract] C
		inner join Organization O on O.ID = C.OrganizationID and O.ID = 1
		inner join reporting.Global_resource R on R.Email = O.AdministratorEmail
		left join ContractAcceptanceHistory H on H.ContractID = C.ID and H.ResourceID = R.ResourceID and H.OrganizationID = O.ID 
			and H.AcceptedOn >= C.PublishedOn
		where 
			C.[State] = 1 and C.ContractType = 1 and C.PublishedOn is not null

		union all

		select 
			C.ID as ContractID, 
			case when H.ID is null then
				0
			else
				1
			end as Accepted
		from [Contract] C
		inner join reporting.Global_resource R on R.Email = (select AdministratorEmail from Organization where ID = @OrganizationID)
		left join ContractAcceptanceHistory H on H.ContractID = C.ID and H.ResourceID = R.ResourceID and H.OrganizationID is null 
			and H.AcceptedOn >= C.PublishedOn
		where 
			C.[State] = 1 and C.ContractType = 1 and C.PublishedOn is not null and C.OrganizationID is null
		) X
	where 
		X.Accepted = 0

	return (@accepted);

END