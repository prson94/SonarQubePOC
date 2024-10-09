using d360.core.entities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using d360.core;
using d360.core.enums;
using d360.model;
using System.Data.SqlClient;
using Dapper;
using System.Text;
using Dapper.Contrib.Extensions;
using System.Data;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace igx.jobs.apiexecutionprocessor.helpers
{
	public class FieldValueState
	{
		public string FieldName { get; set; }
		public object Value { get; set; }
	}

	public class ChangeLogTracker<T> where T : class
	{
		private readonly ILogger Log;
		private SqlConnection _connection;
		private T lastState;
		private int resourceId;

		private T originalState;
		private ChangeLogType action;

		public bool ShouldBeLogged { get; set; }

		private Audit _audit = new Audit();

		private bool isSet;
		public ChangeLogTracker(ILogger log)
		{
			isSet = false;
			Log = log;
		}

		public void Set(T lastState, int resourceId, SqlConnection connection, ChangeLogType action)
		{
			_connection = connection;
			this.lastState = lastState;
			this.action = action;
			this.resourceId = resourceId;
			isSet = true;
		}

		public void ParseAndSaveAuditRecord()
		{
			try
			{
				if (!isSet)
				{
					throw new Exception("Use Set before calling ParseAndSaveAuditRecord");
				}
				SetInitialState();
				ParseAuditRecord();
				SaveIntoDatabase();
			}
			catch (Exception ex)
			{
				Log.LogError(exception: ex, "Error on ParseAndSaveAuditRecord");
			}
		}

		private void SetInitialState()
		{
			List<FieldValueState> originalValues = new List<FieldValueState>();
			if (lastState is AssetType)
			{
				var obj = lastState as AssetType;
				originalValues = _connection.Query<FieldValueState>(GetLastVersionSQL(), new { obj.Object, obj.ObjectID }).ToList();
			}
			else if (lastState is FieldType)
			{
				var obj = lastState as FieldType;
				originalValues = _connection.Query<FieldValueState>(GetLastVersionSQL(true), new { Object = SystemObjects.FieldType.ToString(), ObjectID = obj.ID }).ToList();
			}

			SetOriginalValuesGeneric(originalValues);
		}

		private void SetOriginalValuesGeneric(IEnumerable<FieldValueState> originalValues)
		{
			originalState = (T)Activator.CreateInstance(typeof(T));

			var properties = originalState.GetType().GetProperties().Where(prop => prop.IsDefined(typeof(TrackInChangeLog), false));
			foreach (var prop in properties)
			{
				var valueState = originalValues.FirstOrDefault(x => x.FieldName == prop.Name);
				if (valueState != null && valueState.Value != null)
				{
					Type t = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

					object safeValue = (valueState.Value == null) ? null : Convert.ChangeType(valueState.Value, t);

					prop.SetValue(originalState, safeValue, null);
				}
			}
		}

		private void ParseAuditRecord()
		{
			if (this.lastState is AssetType)
			{
				AssetType at = this.lastState as AssetType;

				DateTime updatedOn = at.UpdatedOn ?? at.CreatedOn ?? DateTime.UtcNow;

				_audit = new Audit
				{
					Object = at.Object,
					ObjectID = at.ObjectID,
					ActionObject = at.Object,
					ActionObjectID = at.ObjectID,
					Action = action.ToString(),
					ObjectName = at.Name,
					ActionObjectName = at.Name,
					ActionObjectTypeName = at.Class.GetDisplayName(),
					ActionDescription = $"This asset type has been {action.ToString()}",
					Date = updatedOn,
					ResourceID = this.resourceId,
					AuditFields = new List<AuditField>()
				};
			}
			else if (lastState is FieldType)
			{
				FieldType ft = lastState as FieldType;
				FieldType original = originalState as FieldType;

				var actionObject = "";
				var actionObjectId = 0;

				if (ft.AssetTypeID.HasValue)
				{
					var at = _connection.Query("select ObjectId, Object from AssetType where ID = @AssetTypeID", new { ft.AssetTypeID }).FirstOrDefault();
					actionObject = at.Object;
					actionObjectId = at.ObjectId;
				}
				else if (ft.IssueTypeID.HasValue)
				{
					actionObject = "IssueType";
					actionObjectId = ft.IssueTypeID.Value;

				}
				else if (ft.IntersectTypeID.HasValue)
				{
					actionObject = "IntersectType";
					actionObjectId = ft.IntersectTypeID.Value;
				}

				_audit = new Audit
				{
					Object = "FieldType",
					ObjectID = ft.ID,
					ActionObject = actionObject,
					ActionObjectID = actionObjectId,
					Action = action.ToString(),
					ObjectName = ft.FriendlyName ?? original.FriendlyName ?? "",
					ActionObjectName = ft.FriendlyName ?? original.FriendlyName ?? "",
					ActionObjectTypeName = "Field Type",
					ActionDescription = $"This field type has been {action.ToString().ToLowerInvariant()}",
					Date = DateTime.UtcNow,
					ResourceID = this.resourceId,
					AuditFields = new List<AuditField>()
				};
			}


			var currentVersion = _connection.Query<int>("select Version from reporting.Global_Audit where ActionObject = @ActionObject and ActionObjectID = @ActionObjectID order by Version desc", new { _audit.ActionObject, _audit.ActionObjectID }).FirstOrDefault();
			_audit.Version = currentVersion + 1;

			if (action != ChangeLogType.Removed)
			{
				HandleFieldUpdates();
			}
			ShouldBeLogged = action == ChangeLogType.Removed || _audit.AuditFields.Count > 0;
		}

		private void HandleFieldUpdates()
		{
			var properties = originalState.GetType().GetProperties()
									.Where(prop => prop.IsDefined(typeof(TrackInChangeLog), false));

			foreach (var prop in properties)
			{
				var _val = prop.GetValue(this.lastState, null);
				string oldValue = null;
				string newValue = _val != null ? _val.ToString() : null;

				if (action == ChangeLogType.Updated)
				{
					var oldValAsObj = prop.GetValue(this.originalState, null);
					oldValue = oldValAsObj != null ? oldValAsObj.ToString() : null;
				}

				if ((oldValue ?? "") != (newValue ?? ""))
				{
					if (newValue == "True" || newValue == "False")
					{
						//convert boolean values ToLower()
						newValue = newValue.ToLowerInvariant();
					}

					_audit.AuditFields.Add(
						new AuditField
						{
							FieldName = prop.Name,
							FieldTypeID = 0,
							PreviousValue = oldValue,
							Value = newValue
						});
				}
			}
		}

		private void SaveIntoDatabase()
		{
			//Generate insert query
			string insertQuery = @"
			insert into reporting.Global_Audit 
			(Object, ObjectID, ActionObject, ActionObjectID,Action,ActionDescription,ActionObjectName,ActionObjectTypeName,Date,ObjectName,ResourceID,Version)
			VALUES (@Object, @ObjectID, @ActionObject, @ActionObjectID,@Action,@ActionDescription,@ActionObjectName,@ActionObjectTypeName,@Date,@ObjectName,@ResourceID,@Version)
		
			select SCOPE_IDENTITY()";
			int auditId = _connection.Query<int>(insertQuery, new
			{
				_audit.Object,
				_audit.ObjectID,
				_audit.ActionObject,
				_audit.ActionObjectID,
				_audit.Action,
				_audit.ActionDescription,
				_audit.ActionObjectName,
				_audit.ActionObjectTypeName,
				_audit.Date,
				_audit.ObjectName,
				_audit.ResourceID,
				_audit.Version
			}).FirstOrDefault();

			if (_audit.AuditFields.Count > 0)
			{
				DataTable fieldAuditTable = new DataTable();
				fieldAuditTable.Columns.Add("AuditID", typeof(long));
				fieldAuditTable.Columns.Add("FieldTypeID", typeof(int));
				fieldAuditTable.Columns.Add("FieldName", typeof(string));
				fieldAuditTable.Columns.Add("Value", typeof(string));
				fieldAuditTable.Columns.Add("PreviousValue", typeof(string));

				foreach (var fieldAudit in _audit.AuditFields)
				{
					DataRow row = fieldAuditTable.NewRow();
					row["AuditID"] = auditId;
					row["FieldTypeID"] = fieldAudit.FieldTypeID;
					row["FieldName"] = fieldAudit.FieldName;
					row["Value"] = fieldAudit.Value;
					row["PreviousValue"] = fieldAudit.PreviousValue;

					fieldAuditTable.Rows.Add(row);
				}

				using (SqlBulkCopy bulk = _connection.CreateBulkCopy("reporting.Global_FieldAudit"))
				{
					bulk.ColumnMappings.Add("AuditID", "AuditID");
					bulk.ColumnMappings.Add("FieldTypeID", "FieldTypeID");
					bulk.ColumnMappings.Add("FieldName", "FieldName");
					bulk.ColumnMappings.Add("Value", "Value");
					bulk.ColumnMappings.Add("PreviousValue", "PreviousValue");

					bulk.WriteToServer(fieldAuditTable);
				}
			}
		}

		private string GetLastVersionSQL(bool useOnlyObjectCheck = false)
		{
			return $@"
				drop table if exists #changelogs;
				drop table if exists #FieldMaxVersion;

				select gfa.FieldName,0 FieldTypeID, max(gfa.AuditID) AuditID, max(ga.Version) MaxVersion
				into #changelogs
				from reporting.Global_Audit GA
				inner join reporting.Global_FieldAudit gfa on gfa.AuditID = ga.ID and gfa.FieldTypeID = 0
				where Object = @object and ObjectID = @objectId {(useOnlyObjectCheck ? "" : "and ActionObject = @object and ActionObjectID = @objectId")}
				group by gfa.FieldName;

				create clustered index cx_changelogs on #changelogs (AuditID, FieldTypeID, FieldName);

				select  logs.FieldName, 
						gfa.Value,
						logs.MaxVersion [Version], 
						logs.MaxVersion
				from #changelogs logs
				inner join reporting.Global_FieldAudit gfa on gfa.AuditID = logs.AuditID and gfa.FieldTypeID = logs.FieldTypeID and gfa.fieldname = logs.FieldName;
				";
		}
	}


}
