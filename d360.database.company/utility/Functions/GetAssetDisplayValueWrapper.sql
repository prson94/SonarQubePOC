CREATE FUNCTION [utility].[GetAssetDisplayValueWrapper]
(
	@ID bigint
)
RETURNS nvarchar(max)
AS
BEGIN
	return utility.GetAssetDisplayValue(@ID)
END