using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using d360.extensions.search;
using System.Xml.Linq;
using d360.core.queue;
using Newtonsoft.Json;
using Dapper;
using d360.core.entities;
using System.Diagnostics;
using SpreadsheetLight;

namespace d360.jobs.ProcessAzureQueues
{
    public class Functions: FunctionsBase
    {

        public static void ProcessActionsQueueMessage([QueueTrigger("d3s-actions")] string message)
        {
            AzureSearchSource search = null;

            var obj = JsonConvert.DeserializeObject<QueueObject>(message);

            var cnn = GetCompanyConnection(obj.CompanyID);
            cnn.Open();

            ObjectDetail detail = null;
            Dictionary<string, string> fields = null;

            #region Get detail and fields if a certain action

            var type = "";
            var id = 0;

            if (obj.To == QueueAction.AddToIndex || obj.To == QueueAction.UpdateInIndex)
            {
                var ix = JsonConvert.DeserializeObject<IndexObjectModel>(message);
                type = ix.Group;
                id = ix.ID;
                ix = null;
            }

            if (obj.To == QueueAction.AddVersion)
            {
                var ver = JsonConvert.DeserializeObject<AddVersionModel>(message);
                type = ver.Type;
                id = ver.ID;
                ver = null;
            }

            if (id > 0)
            {
                detail = cnn.Query<ObjectDetail>("SELECT * FROM utility.ObjectDetail(@t, @i)", new { t = type, i = id }).SingleOrDefault();
                fields = cnn.Query<FieldWithRelation>(
                    "SELECT * from FieldWithRelation where ObjectType = @t and ObjectID = @i order by SortOrder",
                    new { t = type, i = id }
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
            }

            #endregion

            switch (obj.To)
            {
                case QueueAction.AddToIndex:
                    #region
                    cnn.Close();
                    if (detail != null)
                    {
                        var obj1 = JsonConvert.DeserializeObject<AddToIndexModel>(message);
                        obj1.Fields = fields;
                        obj1.RelativeUrl = detail.Url;
                        obj1.Type = detail.TypeName;
                        search = new AzureSearchSource();
                        search.AddToIndex(obj1);                        
                    }
                    break;
                    #endregion
                case QueueAction.UpdateInIndex:
                    #region
                    cnn.Close();
                    if (detail != null)
                    {
                        var obj2 = JsonConvert.DeserializeObject<UpdateInIndexModel>(message);
                        obj2.Fields = fields;
                        obj2.RelativeUrl = detail.Url;
                        obj2.Type = detail.TypeName;
                        search = new AzureSearchSource();
                        search.UpdateInIndex(obj2);
                    }
                    break;
                    #endregion
                case QueueAction.RemoveFromIndex:
                    #region
                    cnn.Close();
                    var obj3 = JsonConvert.DeserializeObject<RemoveFromIndexModel>(message);
                    search = new AzureSearchSource();
                    search.RemoveFromIndex(obj3);
                    break;
                    #endregion
                case QueueAction.AddVersion:
                    #region
                    var obj4 = JsonConvert.DeserializeObject<AddVersionModel>(message);
                    var value = new XElement("fields");

                    if (detail != null)
                    {
                        foreach (var f in fields)
                        { 
                            value.Add(new XElement(f.Key, f.Value));
                        }
                    }

                    try
                    {
                        cnn.Execute(
                            "INSERT INTO ObjectVersion (ObjectType, ObjectID, [Version], [Action], ResourceID, [Date], Value) VALUES (@t, @i, 0, @a, @r, @d, @v)",
                            new
                            {
                                t = obj4.Type,
                                i = obj4.ID,
                                a = obj4.Action,
                                r = obj4.ResourceID,
                                d = obj4.Date,
                                v = value.ToString()
                            }
                        );
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError("An Error occured when trying to add a version for Object {0} {1}.  The error is: {2} {3}", obj4.Type, obj4.ID, ex.Message, (ex.InnerException != null) ? ex.InnerException.Message : "");
                    }

                    cnn.Close();
                    break;
                    #endregion
                case QueueAction.BulkLoad:
                    #region
                    var obj5 = JsonConvert.DeserializeObject<AddVersionModel>(message);
                    var load = cnn.Query<Load>("select * from Load where ID = @id", new { id = obj5.ID }).SingleOrDefault();

                    var loadTypeFields = cnn.Query<LoadTypeField>(
                        "select * from LoadTypeField where LoadTypeID = @id order by SortOrder",
                        new { id = load.LoadTypeID }
                    ).ToList();

                    var memoryStream = new MemoryStream(load.File);
                    var xls = new SLDocument(memoryStream);

                    var stats = xls.GetWorksheetStatistics();
                    var rowIndex = stats.StartRowIndex+1;
                    while (rowIndex <= stats.EndRowIndex)
                    {
                        var loadItemID = cnn.ExecuteScalar<int>("insert into LoadItem (LoadID, RowIndex) values (@l, @r); select SCOPE_IDENTITY()", new { l = load.ID, r = rowIndex });
                        var columnIndex = stats.StartColumnIndex;
                                        
                        while (columnIndex <= stats.EndColumnIndex)
                        {
                            var field = loadTypeFields[columnIndex-1];
                            if (field != null)
                            {
                                cnn.Execute("insert into LoadItemField (LoadItemID, LoadTypeFieldID, Value) values (@l, @f, @v)", new { l = loadItemID, f = field.ID, v = xls.GetCellValueAsString(rowIndex, columnIndex) });
                            }
                            columnIndex++;
                        }
                                        
                        rowIndex++;
                    }
                                    
                    cnn.Execute("exec ProcessBulkLoad @LoadID", new { LoadID = load.ID }, null, 1800);    // 30 minute timeout.;
                    cnn.Close();
                    break;
                    #endregion
            }

            cnn.Dispose();
            fields = null;
            detail = null;
            search = null;
        }
    }
}
