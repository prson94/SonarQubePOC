
CREATE VIEW [dbo].[StatisticTypeRelationDetail]
AS
	SELECT	R.StatisticTypeID,
			R.ObjectID,
			case R.ObjectType
				when 'ResourceType' then 'Resource'
				else D.Name
			end as ObjectName,
			case R.ObjectType
				when 'ResourceType' then 'ResourceType'
				else R.ObjectType
			end as ObjectType,
			R.Score
	FROM	StatisticTypeRelation R
			left join cache.ObjectDetails D on D.[Object] = R.ObjectType and D.ObjectID = R.ObjectID
			--CROSS APPLY utility.ObjectDetail(R.ObjectType, R.ObjectID) D


