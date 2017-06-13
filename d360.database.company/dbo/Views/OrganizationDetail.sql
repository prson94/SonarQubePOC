create view [dbo].[OrganizationDetail]
as
select 
	o.ID,
	o.Name,
	o.Accepted,
	o.AcceptedBy,
	o.DateAccepted,
	o.AdministratorEmail,
	r.FirstName + ' ' + r.LastName as AcceptedByName 
from Organization o
left join reporting.Global_Resource r on r.ResourceID = o.AcceptedBy
GO