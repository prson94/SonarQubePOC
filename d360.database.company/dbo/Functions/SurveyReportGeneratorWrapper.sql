CREATE FUNCTION dbo.SurveyReportGeneratorWrapper
(
	@SurveyTypeID int,
	@ObjectType varchar(25),
	@ObjectID int
)
RETURNS XML
AS
BEGIN
	RETURN dbo.SurveyReportGenerator(@SurveyTypeID, @ObjectType, @ObjectID)
END