CREATE FUNCTION [utility].[GetAssetHashWrapper]
(
--declare
	@ID bigint,-- = 733,
	@KeyFieldOnly bit-- = 1	
)
RETURNS varchar(50)
AS
BEGIN
	return utility.GetAssetHash(@ID, @KeyFieldOnly)
END