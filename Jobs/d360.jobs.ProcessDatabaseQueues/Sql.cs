namespace d360.jobs.ProcessDatabaseQueues
{
    public static class Sql
    {
        #region Notification Task : SQL Statements

        public static string Notification = @"select n.* from queue.Notification n
inner join Comment c on 
	n.[Object] = 'Comment' 
	AND ObjectId = c.ID 
	AND  (
			(select count(*) from comment r where r.ParentID = c.ID) > 0
			OR (
				c.ParentID IS NOT NULL
				OR C.DateCreated < (getdate() - (5 / 24.0 / 60.0))
			)
		 )
where n.MachineAssigned IS NULL
union all
select * from queue.Notification where [Object] != 'Comment' and MachineAssigned IS NULL";


        public static string Comment = @"select	C.ID,
C.Body,
C.DateCreated,
R.FirstName + ' ' + R.LastName as Author,
C.ParentID,
P.Body as ParentBody,
P.DateCreated as ParentDateCreated,
PR.FirstName + ' ' + PR.LastName as ParentAuthor,
D.Name as OwnerName,
D.Url as OwnerUrl,
D.ObjectTypeName as OwnerTypeName,
case when C.ParentID is null then 'comment' else 'reply' end as OriginationType
from	Comment C
inner join reporting.Global_Resource R on R.ResourceID = C.CreatingResourceID and C.ID = @CommentID
inner join cache.ObjectDetails D on D.[Object] = C.OwnerObjectType and D.ObjectID = C.OwnerObjectID
left join Comment P on P.ID = C.ParentID
left join reporting.Global_Resource PR on PR.ResourceID = P.CreatingResourceID
where (select count(*) from comment where parentID = @CommentID) > 0 OR C.DateCreated < (getdate() - (5 / 24.0 / 60.0)) ";

        public static string Resources = @"select	F.ResourceID,
R.FirstName + ' ' + R.LastName as Name,
R.Email
from	CommentRelation CR
inner join FollowWithChildren F on F.ObjectType = CR.ObjectType and F.ObjectID = CR.ObjectID  and CR.CommentID = @CommentID
inner join reporting.Global_Resource R on R.ResourceID = F.ResourceID and R.Email not like '%?subject=%'
union
select	coalesce(RG.ResourceID, R.ResponsibleObjectID) as ResourceID,
RE.FirstName + ' ' + RE.LastName as Name,
RE.Email
from	CommentRelation CR
inner join ResponsibilityDetail R on R.ObjectType = CR.ObjectType and R.ObjectID = CR.ObjectID and CR.CommentID = @CommentID
left join ResourceGroup RG on R.ResponsibleObjectType = 'Group' and RG.GroupID = R.ResponsibleObjectID
inner join reporting.Global_Resource RE on RE.ResourceID = coalesce(RG.ResourceID, R.ResponsibleObjectID) and RE.Email not like '%?subject=%'";

        public static string FusionResources = @"
select	coalesce(RG.ResourceID, R.ResponsibleObjectID) as ResourceID,
RE.FirstName + ' ' + RE.LastName as Name,
RE.Email
from	cache.Responsibilities CR
inner join ResponsibilityDetail R on R.ObjectType = CR.[Object] and R.ObjectID = CR.ObjectID and CR.[Object] = 'Fusion' and CR.ObjectID = @id
left join ResourceGroup RG on R.ResponsibleObjectType = 'Group' and RG.GroupID = R.ResponsibleObjectID
inner join reporting.Global_Resource RE on RE.ResourceID = coalesce(RG.ResourceID, R.ResponsibleObjectID) and RE.Email not like '%?subject=%'";

        public static string FusionInfo = @"select F.ID as FusionID, F.Name as Fusion, FT.ID as FusionTypeID, FT.Name as FusionType
from Fusion F inner join FusionType FT on FT.ID = F.FusionTypeID and F.ID = @id";

        #endregion

        #region Follow Children : SQL Statements

        public static string TaxonomyParents = @"with t as
                                            (
	                                            select t1.* from taxonomy t1 where t1.id = @id
	                                            union all
	                                            select t2.* from t
	                                            join taxonomy t2 on t2.id = t.parentid
                                            )
                                            select c.id from t 
                                            inner join FollowWithChildren c on c.objectid = t.id and c.objecttype = 'Taxonomy' and c.FollowTypeID = 3";

        #endregion

        #region Style Cache; SQL Statements

        public static string StyleCache = @"
update	T
set		T.IconBackColor = S.IconBackColor,
T.IconForeColor = S.IconForeColor,
T.IconText = S.IconText
from	cache.ObjectDetails T
inner join ObjectStyle S on S.ObjectType = @type and S.ObjectID = @id and T.ObjectType = S.ObjectType and T.ObjectTypeID = S.ObjectID;

update	T
set		T.IconBackColor = S.IconBackColor,
T.IconForeColor = S.IconForeColor,
T.IconText = S.IconText
from	cache.ObjectDetails T
inner join ObjectStyle S on S.ObjectType = @type and S.ObjectID = @id and T.[Object] = S.ObjectType and T.ObjectID = S.ObjectID;";

        #endregion
    }
}
