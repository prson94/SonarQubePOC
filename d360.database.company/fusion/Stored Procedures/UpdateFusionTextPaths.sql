create procedure [fusion].[UpdateFusionTextPaths]
	@FusionID int
as
begin
	set nocount on;

	WITH hierarchy (id, itempath) AS
	(
		SELECT id, cast(name as nvarchar(2500))
		FROM fusionattribute
		WHERE fusionid = @FusionID and parentid is null

		UNION ALL

		SELECT gp.id, cast(gps.itempath + '.' + gp.name as nvarchar(2500))
		FROM fusionattribute gp
		JOIN hierarchy gps ON gps.id = gp.parentid
	)
	UPDATE T
	set T.textpath = cte.itempath
	from fusionattribute T
	inner join 
		hierarchy cte
	on cte.id = T.id
end
