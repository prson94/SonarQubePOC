CREATE FUNCTION dbo.SurveyReportGenerator
(
	@SurveyTypeID int,
	@ObjectType varchar(25),
	@ObjectID int
)
RETURNS XML
AS
BEGIN
	DECLARE @xml xml
	SET @xml =	 (SELECT (
					SELECT	ST.Name + ' Results' AS Title,
							(
							SELECT
								(
								SELECT		QT.ID,
											QT.Name AS Title,
											COALESCE(S.Score, 0) AS Score,
											COALESCE(S.Responses, 0) AS TotalResponses,
											(
											SELECT	(
														SELECT		IQRTO.Name,
																	COUNT(IQRTO.Value) AS Value
														FROM		Question IQ
																	INNER JOIN ResponseTypeOption IQRTO ON IQ.ResponseTypeOptionID = IQRTO.ID
														WHERE		IQ.QuestionTypeID = QT.ID
														GROUP BY	IQ.QuestionTypeID, 
																	IQRTO.Name
														ORDER BY	IQ.QuestionTypeID
														FOR XML PATH('Result'), Type
													) FOR XML PATH('Results'), Type
											)
								FROM		QuestionType QT
											LEFT JOIN	(
														SELECT		QT.ID AS QuestionTypeID,
																	COALESCE(AVG(RTO.Value), 0) * 20 AS Score,
																	COALESCE(COUNT(Q.ID), 0) AS Responses
														FROM		QuestionType QT
																	LEFT JOIN Question Q ON QT.ID = Q.QuestionTypeID
																	LEFT JOIN ResponseTypeOption RTO ON Q.ResponseTypeOptionID = RTO.ID
														GROUP BY	QT.ID
														) AS S ON S.QuestionTypeID = QT.ID
								WHERE		QT.SurveyTypeID = ST.ID
								ORDER BY	QT.ID
								FOR XML PATH('Chart'), Type
								)
							FOR XML PATH('Charts'), Type--as Charts
							)
					FROM		SurveyType ST
								INNER JOIN Survey S ON ST.ID = S.SurveyTypeID AND S.ObjectType = @ObjectType AND S.ObjectID = @ObjectID
					WHERE		ST.ID = @SurveyTypeID
					GROUP BY ST.Name, ST.ID
					FOR XML PATH(''), Type
					)
					FOR XML PATH('Report'))
	RETURN @xml
END