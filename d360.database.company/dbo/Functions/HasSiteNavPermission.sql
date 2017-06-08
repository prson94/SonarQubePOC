CREATE FUNCTION [dbo].[HasSiteNavPermission]
(
	@SiteNavID int,
	@ResourceID int
)
RETURNS bit
AS
BEGIN

	if not exists (select 1 from SiteNavPermission where SiteNavID = @SiteNavID)
		return 1;
	else if exists (select 1 from SiteNavPermission where [Object] = 'Resource' and ObjectID = @ResourceID and SiteNavID = @SiteNavID)
		return 1;
	else if exists (select 1 from SiteNavPermission p
				inner join [ResourceGroup] g on g.GroupID = p.ObjectID and p.[Object] = 'Group'
				where ResourceID = @ResourceID and p.SiteNavID = @SiteNavID)
		return 1;

	return 0;

END
