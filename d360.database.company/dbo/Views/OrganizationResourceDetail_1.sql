create view OrganizationResourceDetail
as
select	O.OrganizationID,
		coalesce(ORG.Name, 'Global') as OrganizationName,
		O.ResourceID,
		R.FirstName,
		R.LastName,
		R.DateLastLoggedIn,
		R.Email,
		R.Status,
		O.Accepted,
		O.DateAccepted
from	OrganizationResource O
		left join Organization ORG on ORG.ID = O.OrganizationID
		inner join reporting.Global_Resource R on R.ResourceID = O.ResourceID