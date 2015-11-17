CREATE PROCEDURE [dbo].[GetRandomSurveyQuestionForUser]
(
--declare
	@ResourceID int,
	@ObjectType varchar(50),
	@ObjectID int
--set @ResourceID = 1
--set @ObjectType = 'Artifact'
--set @ObjectID = 1
)
AS
BEGIN
	declare @tbl TABLE(ID int, Name nvarchar(500))

	insert into @tbl 
		SELECT	T.ID AS SurveyTypeID,
				CD.Name
		FROM	SurveyType T
				INNER JOIN	cache.ObjectDetails CD on 
					CD.[Object] = @ObjectType and CD.ObjectID = @ObjectID 
					and CD.ObjectType = T.ObjectType and CD.ObjectTypeID = T.ObjectID
				--[utility].[ObjectDetail](@ObjectType, @ObjectID) CD ON T.ObjectID = CD.TypeID AND T.ObjectType = CD.Type

	SELECT	(
			SELECT	TOP 1
					QT.ID,
					QT.Name,
					QT.Description,
					@ObjectType AS ObjectType,
					@ObjectID AS ObjectID,
					DT.Name AS ObjectName,
					QT.SurveyTypeID,
					(
					SELECT	Name,
							Value
					FROM	ResponseTypeOption RTO
					WHERE	RTO.ResponseTypeID = QT.ResponseTypeID
					FOR XML PATH('Option'), Type
					)
			FROM	QuestionType QT
					INNER JOIN SurveyType ST ON ST.ID = QT.SurveyTypeID
					INNER JOIN @tbl DT ON ST.ID = DT.ID
			WHERE	QT.ID NOT IN (
								SELECT	QuestionTypeID 
								FROM	Question Q 
										INNER JOIN Survey S ON Q.SurveyID = S.ID 
															AND S.ResourceID = @ResourceID 
															AND S.ObjectType = @ObjectType 
															AND S.ObjectID = @ObjectID
								)
			ORDER BY newid()
			FOR XML PATH(''), Type
			)
	FOR XML PATH('Question')
END
