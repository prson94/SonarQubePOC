using System;

namespace d360.workers.ProcessDatabaseTasksWorkerRole
{
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
