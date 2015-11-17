CREATE VIEW [dbo].[FollowDetail]
AS
	SELECT		F.ResourceID,
				R.Email,
				R.Email as FollowerEmail,
				R.FirstName + ' ' + R.LastName as FollowerName,
				R.FirstName as FollowerFirstName,
				R.LastName as FollowerLastName,
				'Resource' as FollowerObjectType,
				F.ResourceID as FollowerObjectID,
				dbo.GenerateObjectUrl('Resource', 1, F.ResourceID) as FollowerUrl,
				F.ObjectID,
				F.ObjectType,
				O.ObjectID as ID,
				O.Name,
				O.TextPath,
				O.Description,
				O.ParentID,
				O.Parent as ParentType,
				O.Url,
				O.ObjectTypeID as TypeID,
				O.ObjectType as [Type],
				O.ObjectTypeName as [TypeName],
				O.IconBackColor,
				O.IconForeColor,
				O.IconText,
				0 AS OpenEventCount,
				dbo.GetObjectStatisticScore(F.ObjectType, F.ObjectID) as CurrentScore,
				case 
					when AF.Active is null then cast(0 as bit)
					else AF.Active
				end as RedFlagged
	FROM		Follow F
				left join reporting.Global_Resource R on R.ResourceID = F.ResourceID
				left join cache.ObjectDetails O on O.[Object] = F.ObjectType and O.ObjectID = F.ObjectID
				LEFT JOIN AlertFlag AF on AF.ObjectType = F.ObjectType and AF.ObjectID = F.ObjectID and AF.Active = 1

