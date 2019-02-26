using d360.core.queue;
using System;
using System.Collections.Concurrent;
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
            Adds = new ConcurrentBag<AddToIndexModel>(); 
            Deletes = new ConcurrentBag<RemoveFromIndexModel>();
            Updates = new ConcurrentBag<UpdateInIndexModel>();
        }

        public ConcurrentBag<AddToIndexModel> Adds { get; set; }
        public ConcurrentBag<RemoveFromIndexModel> Deletes { get; set; }
        public ConcurrentBag<UpdateInIndexModel> Updates { get; set; }
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
		dbo.GenerateAssetUrl(D.ID) as OwnerUrl,
		T.Name as OwnerTypeName,
		case when C.ParentID is null then 'comment' else 'reply' end as OriginationType
from	Comment C
		inner join reporting.Global_Resource R on R.ResourceID = C.CreatingResourceID and C.ID = @CommentID
		inner join Asset D on D.[Object] = C.OwnerObjectType and D.ObjectID = C.OwnerObjectID
		inner join AssetType T on T.ID = D.AssetTypeID
		left join Comment P on P.ID = C.ParentID
		left join reporting.Global_Resource PR on PR.ResourceID = P.CreatingResourceID
where	(select count(*) from comment where parentID = @CommentID) > 0 OR C.DateCreated < (getdate() - (5 / 24.0 / 60.0)) ";

        
        public static string FusionResources = @"
select	R.ResourceID, 
        RE.FirstName + ' ' + RE.LastName as Name, 
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
    }
}
