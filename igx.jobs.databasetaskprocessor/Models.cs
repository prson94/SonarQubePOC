using d360.core.queue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace igx.jobs.databasetaskprocessor
{
    public static class ThreadSafeRandom
    {
        [ThreadStatic]
        private static Random Local;

        public static Random ThisThreadsRandom
        {
            get { return Local ?? (Local = new Random(unchecked(System.Environment.TickCount * 31 + Thread.CurrentThread.ManagedThreadId))); }
        }
    }

    static class MyExtensions
    {
        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = ThreadSafeRandom.ThisThreadsRandom.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }

    public class ObjectIndexCollectionModel
    {
        public ObjectIndexCollectionModel()
        {
            Adds = new List<AddToIndexModel>();
            Deletes = new List<RemoveFromIndexModel>();
            Updates = new List<UpdateInIndexModel>();
        }

        public List<AddToIndexModel> Adds { get; set; }
        public List<RemoveFromIndexModel> Deletes { get; set; }
        public List<UpdateInIndexModel> Updates { get; set; }
    }

    public class QueueTask
    {
        public Guid ID { get; set; }
        public string Action { get; set; }
        public string Custom { get; set; }
        public string Object { get; set; }
        public int ObjectID { get; set; }
        public DateTime Date { get; set; }
        public string MachineAssigned { get; set; }
        public bool HasError { get; set; }
        public string ErrorMessage { get; set; }
        public int NumberOfRetries { get; set; }
        public short Priority { get; set; }
        public long AssetID { get; set; }
    }

    public static class Sql
    {
        #region Notification Task : SQL Statements

        public static string Comment = @"
select	C.ID,
		C.Body,
		C.DateCreated,
		R.FirstName + ' ' + R.LastName as Author,
		C.ParentID,
		P.Body as ParentBody,
		P.DateCreated as ParentDateCreated,
		PR.FirstName + ' ' + PR.LastName as ParentAuthor,
		utility.GetAssetDisplayValueWrapper(D.ID) as OwnerName,
		dbo.GenerateNgObjectUrl(T.Object, T.ObjectID,D.ObjectID) as OwnerUrl,
		T.Name as OwnerTypeName,
		case when C.ParentID is null then 'comment' else 'reply' end as OriginationType
from	Comment C
		inner join reporting.Global_Resource R on R.ResourceID = C.CreatingResourceID and C.ID = @CommentID
		inner join Asset D on D.[Object] = C.OwnerObjectType and D.ObjectID = C.OwnerObjectID
		inner join AssetType T on T.ID = D.AssetTypeID
		left join Comment P on P.ID = C.ParentID
		left join reporting.Global_Resource PR on PR.ResourceID = P.CreatingResourceID
where	(select count(*) from comment where parentID = @CommentID) > 0 OR C.DateCreated < (getdate() - (5 / 24.0 / 60.0)) ";

        public static string Resources = @"
select	F.ResourceID,
        R.FirstName + ' ' + R.LastName as Name,
        R.Email
from	CommentRelation CR
        inner join FollowDetail F on F.ObjectType = CR.ObjectType and F.ObjectID = CR.ObjectID  and CR.CommentID = @CommentID
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
select	R.ResourceID, 
        RE.ResourceName as Name, 
        RE.Email 
from	ResponsibilityDetail R 
        inner join reporting.Global_Resource RE on RE.ResourceID = R.ResourceID and RE.Email not like '%?subject=%' 
where   R.Object = 'Fusion' and R.ObjectID = @id;";

        public static string FusionInfo = @"
select  F.ID as FusionID, 
        F.Name as Fusion, 
        FT.ID as FusionTypeID,  
        FT.Name as FusionType 
from    Fusion F 
        inner join FusionType FT on FT.ID = F.FusionTypeID and F.ID = @id";

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

    public class CommentInfo
    {
        public int ID { get; set; }
        public string Body { get; set; }
        public DateTime DateCreated { get; set; }
        public string Author { get; set; }
        public int? ParentID { get; set; }
        public string ParentBody { get; set; }
        public DateTime? ParentDateCreated { get; set; }
        public string ParentAuthor { get; set; }
        public string OwnerName { get; set; }
        public string OwnerUrl { get; set; }
        public string OwnerTypeName { get; set; }
        public string OriginationType { get; set; }
    }
    public class CommentNotificationUser
    {
        public int ResourceID { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
    }
}
