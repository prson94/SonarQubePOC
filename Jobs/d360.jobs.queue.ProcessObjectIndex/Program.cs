using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.WebJobs;
using d360.core;
using Dapper;
using System.Diagnostics;
using d360.core.entities;
using d360.core.queue;
using d360.extensions.search;

namespace d360.jobs.queue.ProcessObjectIndex
{
    #region Models

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

    #endregion

    class Program: FunctionsBase
    {
        static void Main()
        {
            var host = new JobHost(new JobHostConfiguration(constants.WEBJOBS_STORAGE_CONNECTION));

            var mex = new List<Exception>();

            try
            {
                var companies = GetActiveCompanyIDs();//.Where(i => i == 10).ToList();
                var domainPrefixes = GetCompanyDomainPrefixes();

                companies.AsParallel().WithDegreeOfParallelism(4).ForAll(companyID =>
                {
                    var companyConnection = GetCompanyConnection(companyID);
                    companyConnection.Open();

                    var queueItems = companyConnection.Query<dynamic>(@"select * from [queue].ObjectIndex where MachineAssigned is null and NumberOfRetries < 3 order by Date asc").ToList();
                    Console.WriteLine("Found {0} queue items for company {1}.  Starting to process them.", queueItems.Count, companyID);

                    queueItems.ForEach(q =>
                    {
                        companyConnection.Execute("update [queue].ObjectIndex set MachineAssigned = @m where ID = @queueID", new { m = Environment.MachineName, queueID = q.ID });
                    });

                    var search = new AzureSearchSource();

                    queueItems.ForEach(q =>
                    {
                        try
                        {
                            ObjectDetail detail = null;
                            Dictionary<string, string> fields = null;

                            #region Load Info for Object

                            detail = companyConnection.Query<ObjectDetail>("SELECT * FROM utility.ObjectDetail(@t, @i)", new { t = q.Object, i = q.ObjectID }).SingleOrDefault();
                            fields = companyConnection.Query<FieldWithRelation>(
                                "SELECT * from FieldWithRelation where ObjectType = @t and ObjectID = @i order by SortOrder",
                                new { t = q.Object, i = q.ObjectID }
                                ).ToDictionary(k => k.Name, v => v.FormattedValue);

                            if (detail != null)
                            {
                                if (fields.ContainsKey("Name")) fields["Name"] = detail.Name;
                                else fields.Add("Name", detail.Name);

                                if (fields.ContainsKey("Description")) fields["Description"] = detail.Description;
                                else fields.Add("Description", detail.Description);

                                if (fields.ContainsKey("TextPath")) fields["TextPath"] = detail.TextPath;
                                else fields.Add("TextPath", detail.TextPath);
                            } 

                            #endregion

                            switch ((string)q.Action)
                            {
                                case "A":   //Add
                                    var add = new AddToIndexModel { CompanyID = companyID, Fields = fields, Group = q.Object, ID = q.ObjectID, RelativeUrl = detail.Url, To = QueueAction.AddToIndex, Type = detail.TypeName };
                                    search.AddToIndex(add);
                                    break;
                                case "U":   //Update
                                    var update = new UpdateInIndexModel { CompanyID = companyID, Fields = fields, Group = q.Object, ID = q.ObjectID, RelativeUrl = detail.Url, To = QueueAction.UpdateInIndex, Type = detail.TypeName };
                                    search.UpdateInIndex(update);
                                    break;
                                case "D":   //Delete
                                    var delete = new RemoveFromIndexModel { CompanyID = companyID, Fields = fields, Group = q.Object, ID = q.ObjectID, RelativeUrl = "#", To = QueueAction.RemoveFromIndex }; //, Type = detail.TypeName
                                    search.RemoveFromIndex(delete);
                                    break;
                            }

                            companyConnection.Execute("delete [queue].ObjectIndex where ID = @queueID", new { queueID = q.ID }, null, 500);
                        }
                        catch (Exception ex)
                        {
                            mex.Add(ex);
                            companyConnection.Execute(@"update [queue].ObjectIndex set MachineAssigned = null, HasError = 1, NumberOfRetries = NumberOfRetries + 1, ErrorMessage = @error where ID = @queueID", new { queueID = q.ID, error = ex.GetFullExceptionData() }, null, 500);
                        }
                    });                    

                    companyConnection.Close();
                    companyConnection.Dispose();
                });
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.GetFullExceptionData());
            }

            if (mex.Count > 0) throw new AggregateException("One or more exceptions occurred", mex);
        }
    }
}
