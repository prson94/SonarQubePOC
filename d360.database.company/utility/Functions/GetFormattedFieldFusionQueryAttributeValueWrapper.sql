CREATE FUNCTION [utility].[GetFormattedFieldFusionQueryAttributeValueWrapper]
(
	@FusionQueryAttributeID int,
	@FusionQueryAttributeTypeID int	
)
RETURNS nvarchar(max)
AS
BEGIN
	return utility.GetFormattedFieldFusionQueryAttributeValue(@FusionQueryAttributeID, @FusionQueryAttributeTypeID)
END