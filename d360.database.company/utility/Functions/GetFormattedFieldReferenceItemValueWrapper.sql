create FUNCTION [utility].[GetFormattedFieldReferenceItemValueWrapper]
(
	@ReferenceItemID int,
	@ReferenceItemTypeID int	
)
RETURNS nvarchar(4000)
AS
BEGIN
	return utility.GetFormattedFieldReferenceItemValue(@ReferenceItemID, @ReferenceItemTypeID)
END