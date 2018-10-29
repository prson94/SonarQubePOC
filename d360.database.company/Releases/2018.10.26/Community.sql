update	T
set		T.LastLoggedInOn = S.DateLastLoggedIn
from	CompanyResource T
		inner join	(
					select	C.*,
							R.DateLastLoggedIn
					from	[Resource] R
							inner join CompanyResource C on C.ResourceID = R.ID 
															--and C.CompanyID = 193 
															and C.LastLoggedInOn is null 
															and R.DateLastLoggedIn is not null
															and R.Email not like '%@infogix.com'
							inner join Company E on E.ID = C.CompanyID and E.EnvironmentLevel = 3
					) S on S.CompanyID = T.CompanyID and S.ResourceID = T.ResourceID