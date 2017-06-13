CREATE view [dbo].[OrganizationInvitationDetail]
as
select	O.ID,
		O.OrganizationID,
		coalesce(ORG.Name, 'Global') as OrganizationName,
		O.Email
from	OrganizationInvitation O
		left join Organization ORG on ORG.ID = O.OrganizationID
GO

