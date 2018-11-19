/*
alter table [dbo].[CompanyResource] add [LastLoggedInOn] datetime null
alter table [dbo].[CompanyResource] add [State] int constraint DF_CompanyResource_State default(1) not null
GO

ALTER TABLE [dbo].[Resource] ADD CONSTRAINT [DF_Resource_Status] DEFAULT ('Active') FOR [Status]
GO
*/

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