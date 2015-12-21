CREATE FUNCTION [queue].WriteDeleteXml
(
	@ResourceID int
)
RETURNS varchar(250)
AS
BEGIN
	RETURN '<fields>
			<ResourceID>' + cast(@ResourceID as varchar) + '</ResourceID>
		</fields>'
END