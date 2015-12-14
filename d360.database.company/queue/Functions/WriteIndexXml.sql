CREATE FUNCTION [queue].[WriteIndexXml]
(
	@Action varchar(15),
	@ActionObject varchar(50),
	@ActionObjectID int,
	@ResourceID int
)
RETURNS varchar(250)
AS
BEGIN
	RETURN '<fields>
			<Action>' + @Action + '</Action>
			<ActionObject>' + @ActionObject + '</ActionObject>
			<ActionObjectID>' + cast(@ActionObjectID as varchar) + '</ActionObjectID>
			<ResourceID>' + cast(@ResourceID as varchar) + '</ResourceID>
		</fields>'
END
GO