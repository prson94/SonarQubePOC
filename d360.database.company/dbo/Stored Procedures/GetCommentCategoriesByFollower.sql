CREATE PROCEDURE [dbo].[GetCommentCategoriesByFollower]
	@FollowingResourceID int
AS
BEGIN	
	SELECT		CR.ObjectID,
				CR.ObjectType,
				O.Name,
				O.ObjectTypeName as Category
	FROM		Follow F
				INNER JOIN CommentRelation CR ON	(
													(CR.ObjectType = F.ObjectType AND CR.ObjectID = F.ObjectID) OR 
													(CR.ObjectType = 'Resource' AND CR.ObjectID = @FollowingResourceID)
													)
												 AND F.ResourceID = @FollowingResourceID
				inner join cache.ObjectDetails O on O.[Object] = CR.ObjectType and O.ObjectID = CR.ObjectID
				--CROSS APPLY utility.ObjectDetail(CR.ObjectType, CR.ObjectID) O
	GROUP BY	CR.ObjectID,
				O.ObjectTypeName,
				CR.ObjectType,
				O.Name
END
