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

namespace igx.jobs.apiexecutionprocessor.helpers
{
	public class FieldValueState
	{
		public string FieldName { get; set; }
		public object Value { get; set; }
	}

	public class ChangeLogTracker<T> where T : class
	{
		private readonly SqlConnection _connection;
		private readonly T lastState;

		private T originalState;
		private ChangeLogType action;

		public bool ShouldBeLogged { get; set; }

		private Audit _audit = new Audit();
		public ChangeLogTracker(T lastState, SqlConnection connection, ChangeLogType action)
		{
			_connection = connection;
			this.lastState = lastState;
			this.action = action;
		}

		public void ParseAndSaveAuditRecord()
		{
			try
			{
				SetInitialState();
				ParseAuditRecord();
				SaveIntoDatabase();
			}
			catch (Exception ex)
			{
				var a = ex;
			}
		}

		private void SetInitialState()
		{
			if (lastState is AssetType)
			{
				var obj = lastState as AssetType;
				var originalValues = _connection.Query<FieldValueState>(_lastVersionFieldLogsSQL, new { obj.Object, obj.ObjectID });
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
		}

		private void ParseAuditRecord()
		{
			if (this.lastState is AssetType)
			{
				AssetType at = this.lastState as AssetType;

				DateTime updatedOn = at.UpdatedOn ?? at.CreatedOn ?? DateTime.UtcNow;
				int updatedBy = at.UpdatedBy ?? at.CreatedBy ?? 0;

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
					ResourceID = updatedBy,
					AuditFields = new List<AuditField>()
				};
			}

			if (lastState is FieldType)
			{
				FieldType ft = lastState as FieldType;

				var actionObject = "";
				var actionObjectId = 0;

				if (ft.AssetTypeID.HasValue)
				{
					var at = _connection.Query("select ObjectId, Object from AssetType where ID = @AssetTypeID", new { ft.AssetTypeID }).FirstOrDefault();
					actionObject = at.Object;
					actionObjectId = at.ObjectID;
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
					ObjectName = ft.FriendlyName,
					ActionObjectName = ft.FriendlyName,
					ActionObjectTypeName = "Field Type",
					ActionDescription = $"This field type has been {action.ToString().ToLowerInvariant()}",
					Date = DateTime.UtcNow,
					ResourceID = ft.UpdatedBy,
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

		private readonly string _lastVersionFieldLogsSQL = $@"
				drop table if exists #changelogs

				select gfa.FieldName, gfa.Value, ga.Version
				into #changelogs
				from reporting.Global_Audit GA
				inner join reporting.Global_FieldAudit gfa on gfa.AuditID = ga.ID
				where ActionObject = @object and ActionObjectID = @objectId and Object = @object and ObjectID = @objectId

				select * from #changelogs logs
				outer apply (select max(version) from #changelogs where fieldname = logs.FieldName)L(MaxVersion)
				where logs.Version = l.MaxVersion;";

	}


}
