CREATE FUNCTION [utility].[GetFormattedFieldLookupValueWrapper]
(
	@Type varchar(25),
	@DisplayFormat nvarchar(250),
	@LookupObjectType varchar(25),
	@LookupObjectID int,
	@Value nvarchar(max)	
)
RETURNS nvarchar(max)
AS
BEGIN
	RETURN utility.GetFormattedFieldLookupValue(@Type, @DisplayFormat, @LookupObjectType, @LookupObjectID, @Value)
END